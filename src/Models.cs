using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace FloatingTodo
{
    public enum AppLanguage
    {
        Chinese,
        English
    }

    [DataContract]
    public class TodoTask
    {
        [DataMember(Order = 1)] public string Id;
        [DataMember(Order = 2)] public string Title;
        [DataMember(Order = 3)] public DateTime? DueAt;
        [DataMember(Order = 4)] public bool Done;
        [DataMember(Order = 5)] public DateTime CreatedAt;
        [DataMember(Order = 6)] public DateTime? CompletedAt;
        [DataMember(Order = 7)] public string Note;
        [DataMember(Order = 8)] public List<string> TagIds;

        public static TodoTask Create(string title, DateTime? dueAt)
        {
            TodoTask task = new TodoTask();
            task.Id = Guid.NewGuid().ToString("N");
            task.Title = title;
            task.DueAt = dueAt;
            task.Done = false;
            task.CreatedAt = DateTime.Now;
            task.Note = "";
            task.TagIds = new List<string>();
            return task;
        }
    }

    [DataContract]
    public class TagDefinition
    {
        [DataMember(Order = 1)] public string Id;
        [DataMember(Order = 2)] public string Name;
        [DataMember(Order = 3)] public string Color;

        public TagDefinition() { }
        public TagDefinition(string id, string name, string color)
        {
            Id = id; Name = name; Color = color;
        }
    }

    [DataContract]
    public class AppSettings
    {
        [DataMember(Order = 1)] public bool AutoStart = true;
        [DataMember(Order = 2)] public bool AlwaysOnTop = true;
        [DataMember(Order = 3)] public int LeadMinutes = 60;
        [DataMember(Order = 4)] public bool CompletedExpanded = false;
        [DataMember(Order = 5)] public bool DesktopPinned = false;
        [DataMember(Order = 6)] public string Theme = "light";
        [DataMember(Order = 7)] public string Language = "";
    }

    [DataContract]
    public class AppState
    {
        [DataMember(Order = 1)] public int Version = 3;
        [DataMember(Order = 2)] public List<TodoTask> Tasks;
        [DataMember(Order = 3)] public AppSettings Settings;
        [DataMember(Order = 4)] public bool HasWindowPosition;
        [DataMember(Order = 5)] public double WindowLeft;
        [DataMember(Order = 6)] public double WindowTop;
        [DataMember(Order = 7)] public List<TagDefinition> Tags;
        [DataMember(Order = 8)] public double WindowWidth = 372;
        [DataMember(Order = 9)] public double WindowHeight = 566;

        public AppState()
        {
            Version = 3;
            Tasks = new List<TodoTask>();
            Settings = new AppSettings();
            Tags = new List<TagDefinition>();
        }

        public void Normalize()
        {
            if (Tasks == null) Tasks = new List<TodoTask>();
            if (Settings == null) Settings = new AppSettings();
            if (Tags == null) Tags = new List<TagDefinition>();
            if (Settings.LeadMinutes < 5 || Settings.LeadMinutes > 2880) Settings.LeadMinutes = 60;
            if (!string.Equals(Settings.Theme, "dark", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Settings.Theme, "light", StringComparison.OrdinalIgnoreCase))
                Settings.Theme = "light";
            if (string.IsNullOrEmpty(Settings.Language))
                Settings.Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? "zh" : "en";
            if (!string.Equals(Settings.Language, "zh", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(Settings.Language, "en", StringComparison.OrdinalIgnoreCase))
                Settings.Language = "zh";
            if (double.IsNaN(WindowLeft) || double.IsInfinity(WindowLeft)) WindowLeft = 0;
            if (double.IsNaN(WindowTop) || double.IsInfinity(WindowTop)) WindowTop = 0;
            if (double.IsNaN(WindowWidth) || double.IsInfinity(WindowWidth) || WindowWidth < 330) WindowWidth = 372;
            if (double.IsNaN(WindowHeight) || double.IsInfinity(WindowHeight) || WindowHeight < 430) WindowHeight = 566;

            List<TagDefinition> tags = new List<TagDefinition>();
            Dictionary<string, bool> tagIds = new Dictionary<string, bool>(StringComparer.Ordinal);
            HashSet<string> tagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Tags.Count; i++)
            {
                TagDefinition tag = Tags[i];
                if (tag == null) continue;
                if (string.IsNullOrEmpty(tag.Id) || tagIds.ContainsKey(tag.Id)) tag.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(tag.Name)) continue;
                tag.Name = tag.Name.Trim();
                if (tag.Name.Length > 20) tag.Name = tag.Name.Substring(0, 20);
                if (tagNames.Contains(tag.Name)) continue;
                if (string.IsNullOrEmpty(tag.Color)) tag.Color = "#A88BFF";
                tagIds[tag.Id] = true;
                tagNames.Add(tag.Name);
                tags.Add(tag);
            }
            Tags = tags;

            List<TodoTask> clean = new List<TodoTask>();
            Dictionary<string, bool> seenIds = new Dictionary<string, bool>(StringComparer.Ordinal);
            for (int i = 0; i < Tasks.Count; i++)
            {
                TodoTask t = Tasks[i];
                if (t == null) continue;
                if (string.IsNullOrEmpty(t.Id) || seenIds.ContainsKey(t.Id)) t.Id = Guid.NewGuid().ToString("N");
                seenIds[t.Id] = true;
                if (t.Title == null) t.Title = "";
                t.Title = t.Title.Trim();
                if (t.Title.Length == 0) continue;
                if (t.Title.Length > 500) t.Title = t.Title.Substring(0, 500);
                if (t.Done && !t.CompletedAt.HasValue) t.CompletedAt = DateTime.Now;
                if (t.CreatedAt == DateTime.MinValue) t.CreatedAt = DateTime.Now;
                if (t.Note == null) t.Note = "";
                if (t.Note.Length > 3000) t.Note = t.Note.Substring(0, 3000);
                if (t.TagIds == null) t.TagIds = new List<string>();
                List<string> validTags = new List<string>();
                HashSet<string> seenTags = new HashSet<string>(StringComparer.Ordinal);
                for (int j = 0; j < t.TagIds.Count; j++)
                {
                    string id = t.TagIds[j];
                    if (!string.IsNullOrEmpty(id) && tagIds.ContainsKey(id) && seenTags.Add(id)) validTags.Add(id);
                }
                t.TagIds = validTags;
                clean.Add(t);
            }
            Tasks = clean;
        }
    }

    public class StartupOptions
    {
        public bool DevMode;
        public bool AutoStartLaunch;
        public bool SeedDemo;

        public static StartupOptions Parse(string[] args)
        {
            StartupOptions options = new StartupOptions();
            if (args == null) return options;
            for (int i = 0; i < args.Length; i++)
            {
                string a = (args[i] ?? "").Trim().ToLowerInvariant();
                if (a == "--dev") options.DevMode = true;
                else if (a == "--autostart") options.AutoStartLaunch = true;
                else if (a == "--seed-demo") options.SeedDemo = true;
            }
            return options;
        }
    }
}
