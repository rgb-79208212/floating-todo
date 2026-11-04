using System;
using System.Collections.Generic;

namespace FloatingTodo
{
    public class DuePresentation
    {
        public bool HasDue;
        public bool Urgent;
        public bool Overdue;
        public string Primary;
        public string Secondary;
    }

    public static class TaskService
    {
        public static readonly int[] LeadMinuteOptions = new int[] { 15, 30, 60, 180, 1440 };

        public static TodoTask Add(AppState state, string title, DateTime? due, DateTime now)
        {
            if (state == null) throw new ArgumentNullException("state");
            string clean = NormalizeTitle(title);
            if (clean.Length == 0) return null;
            TodoTask task = new TodoTask();
            task.Id = Guid.NewGuid().ToString("N");
            task.Title = clean;
            task.DueAt = NormalizeDue(due);
            task.Done = false;
            task.CreatedAt = now;
            task.Note = "";
            task.TagIds = new List<string>();
            state.Tasks.Add(task);
            return task;
        }

        public static string NormalizeTitle(string title)
        {
            if (title == null) return "";
            string clean = title.Trim();
            if (clean.Length > 500) clean = clean.Substring(0, 500);
            return clean;
        }

        public static DateTime? NormalizeDue(DateTime? due)
        {
            if (!due.HasValue) return null;
            DateTime value = due.Value;
            return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);
        }

        public static void SetDone(TodoTask task, bool done, DateTime now)
        {
            if (task == null) return;
            task.Done = done;
            task.CompletedAt = done ? (DateTime?)now : null;
        }

        public static int MarkAll(IList<TodoTask> tasks, bool done, DateTime now)
        {
            if (tasks == null) return 0;
            int count = 0;
            for (int i = 0; i < tasks.Count; i++)
            {
                SetDone(tasks[i], done, now);
                if (tasks[i] != null) count++;
            }
            return count;
        }

        public static int SetDueAll(IList<TodoTask> tasks, DateTime? due)
        {
            if (tasks == null) return 0;
            DateTime? normalized = NormalizeDue(due);
            int count = 0;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i] == null) continue;
                tasks[i].DueAt = normalized;
                count++;
            }
            return count;
        }

        public static int Remove(AppState state, ICollection<string> ids)
        {
            if (state == null || ids == null) return 0;
            HashSet<string> set = ids as HashSet<string>;
            if (set == null) set = new HashSet<string>(ids, StringComparer.Ordinal);
            int removed = 0;
            for (int i = state.Tasks.Count - 1; i >= 0; i--)
            {
                if (state.Tasks[i] != null && set.Contains(state.Tasks[i].Id))
                {
                    state.Tasks.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        public static int ClearCompleted(AppState state)
        {
            if (state == null) return 0;
            int removed = 0;
            for (int i = state.Tasks.Count - 1; i >= 0; i--)
            {
                if (state.Tasks[i] != null && state.Tasks[i].Done)
                {
                    state.Tasks.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        public static List<TodoTask> ActiveSorted(AppState state)
        {
            List<TodoTask> tasks = new List<TodoTask>();
            for (int i = 0; i < state.Tasks.Count; i++)
            {
                if (state.Tasks[i] != null && !state.Tasks[i].Done) tasks.Add(state.Tasks[i]);
            }
            tasks.Sort(CompareActive);
            return tasks;
        }

        public static List<TodoTask> CompletedSorted(AppState state)
        {
            List<TodoTask> tasks = new List<TodoTask>();
            for (int i = 0; i < state.Tasks.Count; i++)
            {
                if (state.Tasks[i] != null && state.Tasks[i].Done) tasks.Add(state.Tasks[i]);
            }
            tasks.Sort(CompareCompleted);
            return tasks;
        }

        public static int CompareActive(TodoTask a, TodoTask b)
        {
            if (a.DueAt.HasValue && b.DueAt.HasValue)
            {
                int cmp = a.DueAt.Value.CompareTo(b.DueAt.Value);
                if (cmp != 0) return cmp;
                return a.CreatedAt.CompareTo(b.CreatedAt);
            }
            if (a.DueAt.HasValue) return -1;
            if (b.DueAt.HasValue) return 1;
            return a.CreatedAt.CompareTo(b.CreatedAt);
        }

        public static int CompareCompleted(TodoTask a, TodoTask b)
        {
            DateTime ad = a.CompletedAt.HasValue ? a.CompletedAt.Value : a.CreatedAt;
            DateTime bd = b.CompletedAt.HasValue ? b.CompletedAt.Value : b.CreatedAt;
            return bd.CompareTo(ad);
        }

        public static bool IsUrgent(TodoTask task, DateTime now, int leadMinutes)
        {
            if (task == null || task.Done || !task.DueAt.HasValue) return false;
            if (leadMinutes < 1) leadMinutes = 1;
            return task.DueAt.Value <= now.AddMinutes(leadMinutes);
        }

        public static bool IsOverdue(TodoTask task, DateTime now)
        {
            return task != null && !task.Done && task.DueAt.HasValue && task.DueAt.Value <= now;
        }

        public static DuePresentation DescribeDue(TodoTask task, DateTime now, int leadMinutes)
        {
            DuePresentation result = new DuePresentation();
            if (task == null || !task.DueAt.HasValue || task.Done) return result;
            result.HasDue = true;
            DateTime due = task.DueAt.Value;
            result.Overdue = due <= now;
            result.Urgent = due <= now.AddMinutes(Math.Max(1, leadMinutes));
            result.Primary = FormatDue(due, now);
            if (result.Overdue) result.Secondary = "已逾期 " + Humanize(now - due);
            else if (result.Urgent) result.Secondary = "还剩 " + Humanize(due - now);
            else result.Secondary = "";
            return result;
        }

        public static string FormatDue(DateTime due, DateTime now)
        {
            string time = due.ToString("HH:mm");
            DateTime today = now.Date;
            if (due.Date == today) return "今天 " + time;
            if (due.Date == today.AddDays(1)) return "明天 " + time;
            if (due.Date == today.AddDays(2)) return "后天 " + time;
            if (due.Date == today.AddDays(-1)) return "昨天 " + time;
            if (due.Date == today.AddDays(-2)) return "前天 " + time;
            if (due.Date > today && due.Date < today.AddDays(7)) return WeekdayName(due.DayOfWeek) + " " + time;
            if (due.Year == now.Year) return due.ToString("M/d") + " " + time;
            return due.ToString("yyyy/M/d") + " " + time;
        }

        public static string Humanize(TimeSpan span)
        {
            if (span.TotalSeconds < 0) span = span.Negate();
            double minutes = span.TotalMinutes;
            if (minutes < 1) return "不到 1 分钟";
            if (minutes < 60) return ((int)Math.Ceiling(minutes)).ToString() + " 分钟";
            if (minutes < 60 * 24)
            {
                int h = (int)Math.Floor(minutes / 60);
                int m = (int)Math.Round(minutes % 60);
                if (m >= 58) { h++; m = 0; }
                return m == 0 ? h.ToString() + " 小时" : h.ToString() + " 小时 " + m.ToString() + " 分钟";
            }
            int days = (int)Math.Floor(minutes / (60 * 24));
            return days.ToString() + " 天";
        }

        public static string WeekdayName(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return "周一";
                case DayOfWeek.Tuesday: return "周二";
                case DayOfWeek.Wednesday: return "周三";
                case DayOfWeek.Thursday: return "周四";
                case DayOfWeek.Friday: return "周五";
                case DayOfWeek.Saturday: return "周六";
                default: return "周日";
            }
        }

        public static string DateHeadline(DateTime now)
        {
            return now.ToString("M月d日");
        }

        public static string WeekdayLine(DateTime now)
        {
            return "星期" + WeekdayName(now.DayOfWeek).Replace("周", "");
        }

        public static string Summary(AppState state, DateTime now)
        {
            int active = 0;
            int done = 0;
            int urgent = 0;
            for (int i = 0; i < state.Tasks.Count; i++)
            {
                TodoTask t = state.Tasks[i];
                if (t == null) continue;
                if (t.Done) { done++; continue; }
                active++;
                if (IsUrgent(t, now, state.Settings.LeadMinutes)) urgent++;
            }
            if (active == 0 && done == 0) return "暂无待办";
            if (active == 0) return "已全部完成";
            string text = active.ToString() + " 项未完成";
            if (urgent > 0) text += " · " + urgent.ToString() + " 项临近截止";
            if (done > 0) text += " · " + done.ToString() + " 项已完成";
            return text;
        }

        public static string LeadLabel(int minutes)
        {
            if (minutes < 60) return minutes.ToString() + " 分钟";
            if (minutes == 60) return "1 小时";
            if (minutes < 1440) return (minutes / 60).ToString() + " 小时";
            if (minutes % 1440 == 0) return (minutes / 1440).ToString() + " 天";
            return minutes.ToString() + " 分钟";
        }
    }
}
