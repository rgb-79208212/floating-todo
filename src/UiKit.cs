using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FloatingTodo
{
    internal static class UiKit
    {
        public static Brush Primary { get { return Brush("#FFF9F7FD"); } }
        public static Brush Secondary { get { return Brush("#FFAAA4B8"); } }
        public static Brush Muted { get { return Brush("#FF7F798C"); } }
        public static Brush Accent { get { return Brush("#FFB7A5FF"); } }
        public static Brush AccentStrong { get { return Brush("#FFA48EFF"); } }
        public static Brush Danger { get { return Brush("#FFFF7A86"); } }
        public static Brush DangerSoft { get { return Brush("#FFFFADB4"); } }

        public static SolidColorBrush Brush(string hex)
        {
            return ThemeManager.Brush(ThemeManager.MapHex(hex, ThemeManager.Palette.TextPrimary));
        }

        public static SolidColorBrush BrushExact(string hex)
        {
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        public static Color ColorFromHex(string hex)
        {
            return ThemeManager.MapHex(hex, ThemeManager.Palette.TextPrimary);
        }

        public static FrameworkElement Icon(string data, double size, string color, double thickness)
        {
            return Icon(data, size, color, thickness, false);
        }

        public static FrameworkElement Icon(string data, double size, string color, double thickness, bool filled)
        {
            Canvas canvas = new Canvas();
            canvas.Width = 16;
            canvas.Height = 16;
            Path path = new Path();
            path.Data = Geometry.Parse(data);
            path.Stroke = Brush(color);
            path.StrokeThickness = thickness;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.StrokeLineJoin = PenLineJoin.Round;
            if (filled)
            {
                path.Fill = Brush(color);
            }
            canvas.Children.Add(path);
            Viewbox box = new Viewbox();
            box.Width = size;
            box.Height = size;
            box.Child = canvas;
            return box;
        }

        public static FrameworkElement MoreIcon(double size, string color)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            panel.VerticalAlignment = VerticalAlignment.Center;
            for (int i = 0; i < 3; i++)
            {
                Ellipse dot = new Ellipse();
                dot.Width = size * 0.15;
                dot.Height = size * 0.15;
                dot.Fill = Brush(color);
                dot.Margin = new Thickness(i == 0 ? 0 : size * 0.12, 0, 0, 0);
                panel.Children.Add(dot);
            }
            return panel;
        }

        public static FrameworkElement ClockIcon(double size, string color, double thickness)
        {
            Canvas canvas = new Canvas();
            canvas.Width = 16;
            canvas.Height = 16;
            Ellipse circle = new Ellipse();
            circle.Width = 12;
            circle.Height = 12;
            circle.Stroke = Brush(color);
            circle.StrokeThickness = thickness;
            Canvas.SetLeft(circle, 2);
            Canvas.SetTop(circle, 2);
            canvas.Children.Add(circle);
            Path hands = new Path();
            hands.Data = Geometry.Parse("M8,4.6 L8,8.2 L10.3,9.7");
            hands.Stroke = Brush(color);
            hands.StrokeThickness = thickness;
            hands.StrokeStartLineCap = PenLineCap.Round;
            hands.StrokeEndLineCap = PenLineCap.Round;
            canvas.Children.Add(hands);
            Viewbox box = new Viewbox();
            box.Width = size;
            box.Height = size;
            box.Child = canvas;
            return box;
        }

        public static FrameworkElement CheckIcon(double size, string color, double thickness)
        {
            return Icon("M3.2,8.3 L6.6,11.6 L12.9,4.7", size, color, thickness);
        }

        public static FrameworkElement PinIcon(double size, string color)
        {
            return Icon("M8,1.6 C5.7,1.6 3.9,3.4 3.9,5.7 C3.9,8.8 8,13.1 8,13.1 C8,13.1 12.1,8.8 12.1,5.7 C12.1,3.4 10.3,1.6 8,1.6 Z M8,7.5 C7,7.5 6.2,6.7 6.2,5.7 C6.2,4.7 7,3.9 8,3.9 C9,3.9 9.8,4.7 9.8,5.7 C9.8,6.7 9,7.5 8,7.5 Z",
                size, color, 0, true);
        }

        public static Border MakeSwitch(bool on)
        {
            Border track = new Border();
            track.Width = 38;
            track.Height = 22;
            track.CornerRadius = new CornerRadius(11);
            track.Background = Brush(on ? "#D9B8A8FF" : "#18FFFFFF");
            track.BorderBrush = Brush(on ? "#55D3C6FF" : "#20FFFFFF");
            track.BorderThickness = new Thickness(1);
            track.VerticalAlignment = VerticalAlignment.Center;

            Ellipse knob = new Ellipse();
            knob.Width = 16;
            knob.Height = 16;
            knob.Fill = Brush(on ? "#FF1B1626" : "#FFE9E4F0");
            knob.HorizontalAlignment = on ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            knob.Margin = new Thickness(2.5, 0, 2.5, 0);
            knob.VerticalAlignment = VerticalAlignment.Center;
            knob.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 6,
                ShadowDepth = 1,
                Opacity = 0.24,
                Color = Colors.Black
            };
            track.Child = knob;
            return track;
        }

        public static Border Hairline()
        {
            Border border = new Border();
            border.Height = 1;
            border.Background = Brush("#15FFFFFF");
            border.Margin = new Thickness(0, 7, 0, 7);
            return border;
        }

        public static FrameworkElement Dot(string color, double size)
        {
            Ellipse dot = new Ellipse();
            dot.Width = size;
            dot.Height = size;
            dot.Fill = Brush(color);
            dot.VerticalAlignment = VerticalAlignment.Center;
            return dot;
        }

        public static TextBlock Text(string text, double size, string color)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
            block.FontSize = size;
            block.Foreground = Brush(color);
            block.FontFamily = new FontFamily("Microsoft YaHei UI");
            block.VerticalAlignment = VerticalAlignment.Center;
            return block;
        }

        public static TextBlock TextExact(string text, double size, string color)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
            block.FontSize = size;
            block.Foreground = BrushExact(color);
            block.FontFamily = new FontFamily("Microsoft YaHei UI");
            block.VerticalAlignment = VerticalAlignment.Center;
            return block;
        }

        public static string RelativeTime(DateTime when)
        {
            TimeSpan span = when - DateTime.Now;
            if (span.TotalSeconds < 0) return HumanDuration(-span);
            return HumanDuration(span);
        }

        public static string HumanDuration(TimeSpan span)
        {
            double totalMinutes = span.TotalMinutes;
            if (totalMinutes < 1) return "不到 1 分钟";
            if (totalMinutes < 60) return ((int)Math.Ceiling(totalMinutes)).ToString() + " 分钟";
            if (totalMinutes < 60 * 24)
            {
                int hours = (int)Math.Floor(totalMinutes / 60);
                int minutes = (int)Math.Round(totalMinutes % 60);
                if (minutes >= 58)
                {
                    hours += 1;
                    minutes = 0;
                }
                if (minutes == 0) return hours.ToString() + " 小时";
                return hours.ToString() + " 小时 " + minutes.ToString() + " 分钟";
            }
            int days = (int)Math.Floor(totalMinutes / (60 * 24));
            if (days == 1) return "1 天";
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

        public static string DueDateText(DateTime due)
        {
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            string time = due.ToString("HH:mm");
            if (due.Date == today) return "今天 " + time;
            if (due.Date == today.AddDays(1)) return "明天 " + time;
            if (due.Date == today.AddDays(2)) return "后天 " + time;
            if (due.Date > today && due.Date < today.AddDays(7))
            {
                return WeekdayName(due.DayOfWeek) + " " + time;
            }
            if (due.Year == now.Year) return due.ToString("M月d日") + " " + time;
            return due.ToString("yyyy年M月d日") + " " + time;
        }

        public static string Greeting(DateTime now)
        {
            int hour = now.Hour;
            if (hour >= 5 && hour < 8) return "早上好";
            if (hour >= 8 && hour < 11) return "上午好";
            if (hour >= 11 && hour < 13) return "中午好";
            if (hour >= 13 && hour < 17) return "下午好";
            if (hour >= 17 && hour < 19) return "傍晚好";
            if (hour >= 19 && hour < 23) return "晚上好";
            return "夜深了";
        }

        public static string DateHeadline(DateTime now)
        {
            return now.ToString("M月d日") + "  ·  " + WeekdayName(now.DayOfWeek);
        }
    }
}
