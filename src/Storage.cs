using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;

namespace FloatingTodo
{
    public class StateStore
    {
        private readonly object _sync = new object();
        private readonly object _queueLock = new object();
        private readonly object _logLock = new object();
        private readonly string _directory;
        private readonly string _dataPath;
        private readonly string _backupPath;
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        private Thread _worker;
        private byte[] _pendingBytes;
        private bool _stopWorker;

        public AppState State;
        public string DataDirectory { get { return _directory; } }

        public StateStore(string directory)
        {
            _directory = directory;
            _dataPath = Path.Combine(directory, "data.json");
            _backupPath = Path.Combine(directory, "data.bak.json");
        }

        public static string DefaultDataDirectory()
        {
            string custom = Environment.GetEnvironmentVariable("FLOATING_TODO_DATA_DIR");
            if (!string.IsNullOrEmpty(custom)) return custom;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FloatingTodo");
        }

        public void Load()
        {
            lock (_sync)
            {
                State = new AppState();
                try
                {
                    Directory.CreateDirectory(_directory);
                    if (File.Exists(_dataPath))
                    {
                        using (FileStream fs = new FileStream(_dataPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppState));
                            AppState loaded = serializer.ReadObject(fs) as AppState;
                            if (loaded != null) State = loaded;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("读取数据失败，已回退到空白清单。", ex);
                    try
                    {
                        if (File.Exists(_dataPath))
                        {
                            string corrupt = Path.Combine(_directory, "data.corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                            File.Copy(_dataPath, corrupt, true);
                        }
                    }
                    catch { }
                    State = new AppState();
                }
                State.Normalize();
            }
        }

        // Serialize on the UI thread (fast, keeps the snapshot consistent), then
        // write to disk on a dedicated background thread. This keeps clicks and
        // completion/deletion instant even when the filesystem stalls.
        public void SaveAsync()
        {
            byte[] bytes = SerializeState();
            lock (_queueLock)
            {
                _pendingBytes = bytes;
                if (_worker == null)
                {
                    _stopWorker = false;
                    _worker = new Thread(WriterLoop);
                    _worker.IsBackground = true;
                    _worker.Name = "FloatingTodo.StateWriter";
                    _worker.Start();
                }
                _signal.Set();
            }
        }

        // Kept for older call sites/tests that expect a blocking save.
        public void Save()
        {
            SaveNow();
        }

        public void SaveNow()
        {
            byte[] bytes = SerializeState();
            Thread worker;
            lock (_queueLock)
            {
                _pendingBytes = null;
                _stopWorker = true;
                _signal.Set();
                worker = _worker;
            }
            if (worker != null)
            {
                try { worker.Join(2500); }
                catch { }
            }
            WriteBytes(bytes);
            lock (_queueLock)
            {
                _worker = null;
                _stopWorker = false;
            }
        }

        private void WriterLoop()
        {
            while (true)
            {
                _signal.WaitOne();
                bool stop;
                byte[] bytes = null;
                lock (_queueLock)
                {
                    stop = _stopWorker;
                    bytes = _pendingBytes;
                    _pendingBytes = null;
                }
                if (bytes != null) WriteBytes(bytes);
                if (stop) break;
            }
        }

        private byte[] SerializeState()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppState));
                serializer.WriteObject(stream, State);
                return stream.ToArray();
            }
        }

        private void WriteBytes(byte[] bytes)
        {
            lock (_sync)
            {
                try
                {
                    Directory.CreateDirectory(_directory);
                    string temp = _dataPath + ".tmp";
                    using (FileStream fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Flush(true);
                    }
                    if (File.Exists(_dataPath))
                    {
                        try
                        {
                            File.Replace(temp, _dataPath, _backupPath, true);
                        }
                        catch
                        {
                            try { File.Copy(_dataPath, _backupPath, true); } catch { }
                            try { File.Delete(_dataPath); } catch { }
                            File.Move(temp, _dataPath);
                        }
                    }
                    else
                    {
                        File.Move(temp, _dataPath);
                    }
                }
                catch (Exception ex)
                {
                    Log("保存数据失败。", ex);
                }
            }
        }

        public void Log(string message, Exception ex)
        {
            lock (_logLock)
            {
                try
                {
                    Directory.CreateDirectory(_directory);
                    string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message;
                    if (ex != null) line += Environment.NewLine + ex.ToString();
                    File.AppendAllText(Path.Combine(_directory, "app.log"), line + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }
        }
    }

    public static class DemoData
    {
        public static void SeedIfEmpty(StateStore store)
        {
            if (store.State.Tasks.Count > 0) return;
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime tomorrow = today.AddDays(1);

            Add(store, "把报销单交给财务", now.AddHours(-2.5), false, now.AddHours(-6));
            Add(store, "补完周报的最后一页", now.AddMinutes(42), false, now.AddHours(-2));
            Add(store, "预约下周的牙医复诊", today.AddHours(20).AddMinutes(30), false, now.AddHours(-18));
            Add(store, "给妈妈回个电话", tomorrow.AddHours(10), false, now.AddHours(-20));
            Add(store, "取回洗好的白衬衫", tomorrow.AddHours(18).AddMinutes(30), false, now.AddHours(-21));
            Add(store, "读完《献给阿尔吉侬的花束》", null, false, now.AddHours(-30));
            Add(store, "整理周五的汇报材料", null, true, now.AddDays(-1));
            Add(store, "买一束白色郁金香", tomorrow.AddHours(12), true, now.AddHours(-5));
            store.SaveNow();
        }

        private static void Add(StateStore store, string title, DateTime? due, bool done, DateTime created)
        {
            TodoTask task = TodoTask.Create(title, due);
            task.CreatedAt = created;
            if (done)
            {
                task.Done = true;
                task.CompletedAt = DateTime.Now.AddHours(-1);
            }
            store.State.Tasks.Add(task);
        }
    }

    public static class AutoStartManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "FloatingTodo";

        public static bool TrySetEnabled(bool enabled, string exePath, out string error)
        {
            error = null;
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (key == null)
                    {
                        error = "无法打开开机启动设置。";
                        return false;
                    }
                    if (enabled)
                    {
                        key.SetValue(ValueName, "\"" + exePath + "\" --autostart", Microsoft.Win32.RegistryValueKind.String);
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "开机启动设置失败：" + ex.Message;
                return false;
            }
        }

        public static bool IsEnabled()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (key == null) return false;
                    string value = key.GetValue(ValueName) as string;
                    return !string.IsNullOrEmpty(value);
                }
            }
            catch { return false; }
        }
    }
}
