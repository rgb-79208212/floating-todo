using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FloatingTodo
{
    internal static class LanguageManager
    {
        private static readonly Dictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Regex AttributeRegex = new Regex("=\"([^\"]*)\"", RegexOptions.Compiled);
        private static readonly Regex TextNodeRegex = new Regex(">([^<>]+)<", RegexOptions.Compiled);

        public static AppLanguage Current { get; private set; }

        public static bool IsEnglish
        {
            get { return Current == AppLanguage.English; }
        }

        static LanguageManager()
        {
            Current = AppLanguage.Chinese;
            Add("浮光待办", "FloatingTodo");
            Add("浮光待办无法启动，错误已记录到：", "FloatingTodo could not start. Details were logged to:");
            Add("浮光待办遇到问题，错误已记录到：", "FloatingTodo ran into a problem. Details were logged to:");
            Add("暂无待办", "No tasks");
            Add("已全部完成", "All done");
            Add("添加待办…", "Add a task…");
            Add("添加待办", "Add task");
            Add("待办", "Tasks");
            Add("已完成", "Completed");
            Add("已完成 0", "Completed 0");
            Add("选择", "Select");
            Add("取消", "Cancel");
            Add("全选", "Select all");
            Add("清空", "Clear");
            Add("完成", "Complete");
            Add(" 完成", " done");
            Add("恢复", "Restore");
            Add("删除", "Delete");
            Add("清除", "Clear");
            Add("截止", "Due");
            Add("保存", "Save");
            Add("添加", "Add");
            Add("设置", "Settings");
            Add("时间", "Time");
            Add("标题", "Title");
            Add("备注", "Note");
            Add("标签", "Tags");
            Add("月份", "Months");
            Add("确定", "Done");
            Add("退出", "Quit");
            Add("语言", "Language");
            Add("中文", "中文");
            Add("English", "English");
            Add("周三 / WED", "WED");
            Add("0 / 0 完成", "0 / 0 done");
            Add("已选 0 项", "0 selected");
            Add("已完成 0", "Completed 0");
            Add("切换深浅外观", "Switch appearance");
            Add("切换到深色", "Switch to dark");
            Add("切换到浅色", "Switch to light");
            Add("收进系统托盘", "Minimize to tray");
            Add("最小化到系统托盘", "Minimize to system tray");
            Add("退出浮光待办", "Quit FloatingTodo");
            Add("退出浮光待办？", "Quit FloatingTodo?");
            Add("关闭应用", "Close app");
            Add("新的待办事项", "New task");
            Add("为下一件待办设置截止时间", "Set due time for the next task");
            Add("设置截止时间", "Set due time");
            Add("修改下一件待办的截止时间", "Change the next task's due time");
            Add("下一件待办截止时间", "Next task due time");
            Add("清除下一件待办的截止时间", "Clear the next task's due time");
            Add("批量选择", "Select tasks");
            Add("批量完成", "Complete selected");
            Add("批量设置截止", "Set due for selected");
            Add("批量删除", "Delete selected");
            Add("清除已完成", "Clear completed");
            Add("清除所有已完成待办", "Clear all completed tasks");
            Add("在上方输入即可添加", "Type above to add a task");
            Add("同时关闭开机自动显示", "Also turn off launch at login");
            Add("编辑待办", "Edit task");
            Add("关闭编辑", "Close editor");
            Add("编辑待办标题", "Edit task title");
            Add("待办备注", "Task note");
            Add("新标签名称", "New tag name");
            Add("编辑截止时间", "Edit due time");
            Add("保存待办", "Save task");
            Add("拖动右下角调整窗口大小", "Drag the bottom-right corner to resize");
            Add("调整窗口大小", "Resize window");
            Add("截止时间", "Due time");
            Add("清除截止时间", "Clear due time");
            Add("关闭截止时间选择", "Close due picker");
            Add("上个月", "Previous month");
            Add("选择年份和月份", "Choose year and month");
            Add("下个月", "Next month");
            Add("上一组年份", "Previous year range");
            Add("下一组年份", "Next year range");
            Add("选择小时", "Hour");
            Add("选择分钟", "Minute");
            Add(" 时", " h");
            Add(" 分", " min");
            Add("24 小时", "24-hour");
            Add("取消截止时间设置", "Cancel due time");
            Add("确定截止时间", "Confirm due time");
            Add("一", "M");
            Add("二", "T");
            Add("三", "W");
            Add("四", "T");
            Add("五", "F");
            Add("六", "S");
            Add("日", "S");
            Add("位置已锁定，可在设置中解除", "Position locked. Unlock it in Settings.");
            Add("已收进系统托盘", "Minimized to tray");
            Add("浮光待办仍在运行，单击右下角托盘图标即可恢复。",
                "FloatingTodo is still running. Click the tray icon to restore it.");
            Add("待办临期", "Task due soon");
            Add("返回日历", "Back to calendar");
            Add("请选择一个日期。", "Please choose a date.");
            Add("该时间已经过去，请选择稍后的时间。", "That time has passed. Choose a later time.");
            Add("标题不能为空。", "Title cannot be empty.");
            Add("还没有标签，输入名称后添加", "No tags yet. Type a name to add one.");
            Add("取消标签：", "Remove tag: ");
            Add("添加标签：", "Add tag: ");
            Add("截止：", "Due: ");
            Add("设置截止时间", "Set due time");
            Add("钉在桌面", "Pin to desktop");
            Add("锁定位置，防止误拖动", "Lock the position to prevent accidental drags");
            Add("固定最上层", "Always on top");
            Add("始终显示在其他窗口之上", "Keep above other windows");
            Add("开机自动显示", "Launch at login");
            Add("登录 Windows 后自动出现", "Open automatically after Windows sign-in");
            Add("到期前标红", "Mark due before");
            Add("窗口大小", "Window size");
            Add("紧凑", "Compact");
            Add("标准", "Standard");
            Add("宽屏", "Wide");
            Add("编辑内容", "Edit");
            Add("更改截止时间", "Change due time");
            Add("标记为已完成", "Mark complete");
            Add("恢复为待办", "Mark as to-do");
            Add("删除待办…", "Delete task…");
            Add("退出后小组件会关闭。只要仍开启「开机自动显示」，下次登录仍会出现。",
                "The widget will close. If launch at login is still on, it will appear again next time.");
            Add("确认操作失败。", "Could not confirm the operation.");
            Add("全部", "All");
            Add("标签筛选：", "Filter by tag: ");
            Add("取消选择", "Deselect");
            Add("取消选择：", "Deselect: ");
            Add("选择：", "Select: ");
            Add("恢复为待办：", "Mark as to-do: ");
            Add("标记为已完成：", "Mark complete: ");
            Add("备注：", "Note: ");
            Add("更多操作", "More actions");
            Add("更多操作 · ", "More actions · ");
            Add("更改截止时间", "Change due time");
            Add("添加时间", "Add time");
            Add("已选 ", "Selected ");
            Add("删除这条待办？", "Delete this task?");
            Add("删除后无法恢复。", "This cannot be undone.");
            Add("这些待办将被永久删除，此操作无法撤销。",
                "These tasks will be permanently deleted. This cannot be undone.");
            Add("显示浮光待办", "Show FloatingTodo");
            Add("收进任务栏", "Minimize to tray");
            Add("退出浮光待办…", "Quit FloatingTodo…");
            Add("浮光待办 · ", "FloatingTodo · ");
            Add(" 项待办", " tasks");
            Add(" 项临近", " due soon");
            Add(" 项待办即将截止。", " tasks are due soon.");
            Add("项待办即将截止。", "tasks are due soon.");
            Add("下一件 · ", "Next · ");
            Add("已选 {0} 项", "Selected {0}");
            Add("已完成 {0}", "Completed {0}");
            Add("下一件 · {0}", "Next · {0}");
            Add("截止：{0}", "Due: {0}");
            Add("删除选中的 {0} 项？", "Delete {0} selected tasks?");
            Add("清除 {0} 项已完成待办？", "Clear {0} completed tasks?");
            Add("到期前 {0} 标红", "Mark due {0} before");
            Add("窗口大小 {0}", "Window size {0}");
            Add("标签筛选：{0}", "Filter by tag: {0}");
            Add("更多操作 · {0}", "More actions · {0}");
        }

        public static void Initialize(string language)
        {
            Current = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.English : AppLanguage.Chinese;
        }

        public static void SetLanguage(AppLanguage language)
        {
            Current = language;
        }

        public static string T(string chinese)
        {
            if (!IsEnglish || chinese == null) return chinese;
            string value;
            return English.TryGetValue(chinese, out value) ? value : chinese;
        }

        public static string Format(string chinese, params object[] args)
        {
            string template = T(chinese);
            try { return string.Format(template, args); }
            catch { return template; }
        }

        public static string TransformXaml(string xaml)
        {
            if (!IsEnglish || string.IsNullOrEmpty(xaml)) return xaml;
            xaml = AttributeRegex.Replace(xaml, delegate(Match m)
            {
                string value = m.Groups[1].Value;
                if (value.StartsWith("{", StringComparison.Ordinal)) return m.Value;
                string translated;
                if (English.TryGetValue(value, out translated))
                    return "=\"" + translated + "\"";
                return m.Value;
            });
            xaml = TextNodeRegex.Replace(xaml, delegate(Match m)
            {
                string raw = m.Groups[1].Value;
                string trimmed = raw.Trim();
                string translated;
                if (trimmed.Length == 0 || !English.TryGetValue(trimmed, out translated)) return m.Value;
                int start = raw.IndexOf(trimmed, StringComparison.Ordinal);
                return ">" + raw.Substring(0, start) + translated + raw.Substring(start + trimmed.Length) + "<";
            });
            return xaml;
        }

        private static void Add(string chinese, string english)
        {
            English[chinese] = english;
        }
    }
}
