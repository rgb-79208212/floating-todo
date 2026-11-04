using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace FloatingTodo
{
    internal static class Program
    {
        private const string ShowMessageName = "FloatingTodo.Activate.2A7C";
        private const string QuitMessageName = "FloatingTodo.Quit.2A7C";

        [STAThread]
        private static int Main(string[] args)
        {
            bool wantsQuit = HasArg(args, "--quit");
            bool devMode = HasArg(args, "--dev");
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            string suffix = devMode ? ".Dev" : "";
            uint showMessage = Native.RegisterWindowMessage(ShowMessageName + suffix);
            uint quitMessage = Native.RegisterWindowMessage(QuitMessageName + suffix);

            bool createdNew;
            Mutex mutex;
            try
            {
                mutex = new Mutex(true, @"Local\FloatingTodo.SingleInstance.2A7C" + suffix, out createdNew);
            }
            catch
            {
                mutex = null;
                createdNew = true;
            }

            if (!createdNew)
            {
                uint message = wantsQuit ? quitMessage : showMessage;
                int repeats = wantsQuit ? 3 : 2;
                for (int i = 0; i < repeats; i++)
                {
                    IntPtr target = Native.FindWindow(null, "浮光待办");
                    if (target != IntPtr.Zero)
                    {
                        Native.PostMessage(target, message, wantsQuit ? (IntPtr)2 : (IntPtr)1, IntPtr.Zero);
                    }
                    else
                    {
                        Native.Broadcast(message, false);
                    }
                    Thread.Sleep(wantsQuit ? 260 : 140);
                }
                if (wantsQuit) Thread.Sleep(500);
                return 0;
            }

            if (wantsQuit)
            {
                try { if (mutex != null) mutex.ReleaseMutex(); } catch { }
                return 0;
            }

            StartupOptions options = StartupOptions.Parse(args);
            string dataDirectory = StateStore.DefaultDataDirectory();
            StateStore store = new StateStore(dataDirectory);
            store.Load();
            if (options.DevMode) store.Log("[debug] Program main, data=" + dataDirectory, null);
            if (options.SeedDemo)
            {
                DemoData.SeedIfEmpty(store);
            }

            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                store.Log("界面线程出现未处理异常。", e.Exception);
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                store.Log("应用出现未处理异常。", e.ExceptionObject as Exception);
            };

            MainWindow window;
            try
            {
                if (options.DevMode) store.Log("[debug] creating MainWindow", null);
                window = new MainWindow(store, options, showMessage, quitMessage);
                app.MainWindow = window;
                if (options.DevMode) store.Log("[debug] showing initial window", null);
                window.ShowInitial();
                if (options.DevMode) store.Log("[debug] ShowInitial returned", null);
            }
            catch (Exception ex)
            {
                store.Log("创建窗口失败。", ex);
                MessageBox.Show("浮光待办无法启动，错误已记录到：\n" + store.DataDirectory + "\n\n" + ex.Message,
                    "浮光待办", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }

            try
            {
                app.Run();
            }
            catch (Exception ex)
            {
                store.Log("应用运行失败。", ex);
                MessageBox.Show("浮光待办遇到问题，错误已记录到：\n" + store.DataDirectory + "\n\n" + ex.Message,
                    "浮光待办", MessageBoxButton.OK, MessageBoxImage.Error);
                return 1;
            }
            finally
            {
                try { if (mutex != null) mutex.ReleaseMutex(); } catch { }
                try { if (mutex != null) mutex.Dispose(); } catch { }
            }
            return 0;
        }

        private static bool HasArg(string[] args, string value)
        {
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
    }
}
