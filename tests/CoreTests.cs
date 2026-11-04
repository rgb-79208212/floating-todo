using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FloatingTodo;

internal static class CoreTests
{
    private static int _passed;
    private static int _failed;

    private static void Check(bool condition, string name)
    {
        if (condition) { _passed++; Console.WriteLine("PASS  " + name); }
        else { _failed++; Console.WriteLine("FAIL  " + name); }
    }

    private static TodoTask Task(string title, DateTime? due, bool done, DateTime created)
    {
        TodoTask t = new TodoTask();
        t.Id = Guid.NewGuid().ToString("N");
        t.Title = title;
        t.DueAt = due;
        t.Done = done;
        t.CreatedAt = created;
        if (done) t.CompletedAt = created.AddMinutes(10);
        return t;
    }

    private static int Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        DateTime now = new DateTime(2026, 11, 4, 14, 0, 0);

        // Task creation and normalization
        AppState state = new AppState();
        state.Settings.LeadMinutes = 60;
        TodoTask added = TaskService.Add(state, "  买牛奶  ", new DateTime(2026, 11, 4, 20, 30, 45), now);
        Check(added != null && added.Title == "买牛奶", "新增任务会去除首尾空格");
        Check(added.DueAt.HasValue && added.DueAt.Value.Second == 0, "截止时间归一化到分钟");
        Check(TaskService.Add(state, "   ", null, now) == null, "空白标题不能新增");
        string longTitle = new string('测', 600);
        Check(TaskService.Add(state, longTitle, null, now).Title.Length == 500, "标题长度上限 500");

        // Completion / restore
        TaskService.SetDone(added, true, now);
        Check(added.Done && added.CompletedAt.HasValue && added.CompletedAt.Value == now, "完成时记录完成时间");
        TaskService.SetDone(added, false, now);
        Check(!added.Done && !added.CompletedAt.HasValue, "恢复时清除完成时间");

        // Sorting
        AppState sortState = new AppState();
        DateTime baseTime = now.AddHours(-5);
        TodoTask noDue = Task("无时间", null, false, baseTime);
        TodoTask later = Task("稍后", now.AddHours(3), false, baseTime.AddMinutes(1));
        TodoTask soon = Task("很快", now.AddMinutes(20), false, baseTime.AddMinutes(2));
        TodoTask overdue = Task("逾期", now.AddHours(-2), false, baseTime.AddMinutes(3));
        sortState.Tasks.Add(noDue); sortState.Tasks.Add(later); sortState.Tasks.Add(soon); sortState.Tasks.Add(overdue);
        List<TodoTask> active = TaskService.ActiveSorted(sortState);
        Check(active[0] == overdue && active[1] == soon && active[2] == later && active[3] == noDue, "按截止时间排序，无截止放最后");

        // Urgency and display
        DuePresentation overdueDisplay = TaskService.DescribeDue(overdue, now, 60);
        Check(overdueDisplay.Urgent && overdueDisplay.Overdue, "已逾期任务为紧急状态");
        Check(overdueDisplay.Secondary.StartsWith("已逾期"), "逾期文案正确");
        DuePresentation soonDisplay = TaskService.DescribeDue(soon, now, 60);
        Check(soonDisplay.Urgent && !soonDisplay.Overdue, "60 分钟内任务为临近状态");
        Check(soonDisplay.Primary == "今天 14:20", "今天时间显示正确");
        Check(soonDisplay.Secondary == "还剩 20 分钟", "倒计时文案正确");
        DuePresentation laterDisplay = TaskService.DescribeDue(later, now, 60);
        Check(!laterDisplay.Urgent, "较晚任务不标红");
        Check(TaskService.DescribeDue(noDue, now, 60).Primary == null, "无截止任务没有时间文案");
        Check(TaskService.FormatDue(now.Date.AddDays(1).AddHours(9), now) == "明天 09:00", "明天时间格式");
        Check(TaskService.FormatDue(now.Date.AddDays(4).AddHours(9), now).StartsWith("周日"), "本周未来时间格式");
        Check(TaskService.FormatDue(new DateTime(2027, 1, 2, 9, 30, 0), now) == "2027/1/2 09:30", "跨年时间格式");
        Check(TaskService.IsUrgent(Task("边界", now.AddMinutes(60), false, now), now, 60), "提醒边界包含等号");

        // Batch operations
        AppState batch = new AppState();
        TodoTask a = Task("A", null, false, now);
        TodoTask b = Task("B", null, false, now);
        TodoTask c = Task("C", null, true, now);
        batch.Tasks.Add(a); batch.Tasks.Add(b); batch.Tasks.Add(c);
        int marked = TaskService.MarkAll(new List<TodoTask> { a, b }, true, now);
        Check(marked == 2 && a.Done && b.Done, "批量完成");
        TaskService.MarkAll(new List<TodoTask> { a, b }, false, now);
        Check(!a.Done && !b.Done, "批量恢复");
        DateTime batchDue = now.AddDays(2).AddHours(8);
        int dueCount = TaskService.SetDueAll(new List<TodoTask> { a, b }, batchDue);
        Check(dueCount == 2 && a.DueAt == batchDue && b.DueAt == batchDue, "批量改截止时间");
        Check(TaskService.Remove(batch, new string[] { a.Id, c.Id }) == 2 && batch.Tasks.Count == 1, "批量删除");
        batch.Tasks.Add(Task("D", null, true, now));
        Check(TaskService.ClearCompleted(batch) == 1, "清除已完成");

        // Summary
        AppState summaryState = new AppState();
        summaryState.Settings.LeadMinutes = 60;
        summaryState.Tasks.Add(Task("临期", now.AddMinutes(15), false, now));
        summaryState.Tasks.Add(Task("完成", null, true, now));
        Check(TaskService.Summary(summaryState, now).Contains("1 项未完成"), "概览显示未完成数量");
        Check(TaskService.Summary(summaryState, now).Contains("1 项临近截止"), "概览显示临近数量");

        // Settings migration
        AppState legacy = new AppState();
        legacy.Settings = new AppSettings();
        legacy.Normalize();
        Check(!legacy.Settings.DesktopPinned, "旧数据默认不锁定桌面位置");
        Check(legacy.Settings.AutoStart, "开机自启默认开启");
        Check(legacy.Settings.Theme == "light", "旧数据默认浅色外观");

        AppState themed = new AppState();
        themed.Settings.Theme = "dark";
        themed.Normalize();
        Check(themed.Settings.Theme == "dark", "深色外观设置保留");

        AppState badTheme = new AppState();
        badTheme.Settings.Theme = "sepia";
        badTheme.Normalize();
        Check(badTheme.Settings.Theme == "light", "未知外观回退浅色");

        // Async storage roundtrip
        string temp = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-data-" + Guid.NewGuid().ToString("N"));
        try
        {
            StateStore store = new StateStore(temp);
            store.Load();
            store.State.Settings.AlwaysOnTop = false;
            store.State.Settings.DesktopPinned = true;
            store.State.Settings.Theme = "dark";
            store.State.Tasks.Add(Task("持久化测试", now.AddHours(1), false, now));
            store.SaveAsync();
            System.Threading.Thread.Sleep(350);
            StateStore loaded = new StateStore(temp);
            loaded.Load();
            Check(loaded.State.Tasks.Count == 1 && loaded.State.Tasks[0].Title == "持久化测试", "异步保存后可以重新读取");
            Check(loaded.State.Settings.DesktopPinned && !loaded.State.Settings.AlwaysOnTop, "设置持久化");
            Check(loaded.State.Settings.Theme == "dark", "外观设置持久化");
            loaded.State.Tasks.Add(Task("同步保存", null, false, now));
            loaded.SaveNow();
            StateStore loaded2 = new StateStore(temp);
            loaded2.Load();
            Check(loaded2.State.Tasks.Count == 2, "退出前同步保存");
        }
        finally
        {
            try { if (Directory.Exists(temp)) Directory.Delete(temp, true); } catch { }
        }

        Console.WriteLine();
        Console.WriteLine("CORE TESTS: " + _passed + " passed, " + _failed + " failed");
        return _failed == 0 ? 0 : 1;
    }
}
