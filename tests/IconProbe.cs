using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

internal static class IconProbe
{
    private static int Main(string[] args)
    {
        string exe = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        Assembly asm = Assembly.Load(File.ReadAllBytes(exe));
        Console.WriteLine("RESOURCES");
        foreach (string name in asm.GetManifestResourceNames()) Console.WriteLine("  " + name);

        string[] names = new string[] { "FloatingTodo.app.ico", "FloatingTodo.app.alert.ico" };
        foreach (string resource in names)
        {
            using (Stream stream = asm.GetManifestResourceStream(resource))
            {
                if (stream == null) { Console.WriteLine("MISSING " + resource); return 2; }
                foreach (int size in new int[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 })
                {
                    stream.Position = 0;
                    using (Icon icon = new Icon(stream, new Size(size, size)))
                    using (Bitmap bmp = icon.ToBitmap())
                    {
                        string file = Path.Combine(output, resource.Replace('.', '-') + "-" + size + ".png");
                        bmp.Save(file, ImageFormat.Png);
                        Console.WriteLine("OK " + resource + " " + size + " actual=" + bmp.Width + "x" + bmp.Height);
                    }
                }
            }
        }

        using (Icon exeIcon = Icon.ExtractAssociatedIcon(exe))
        using (Bitmap bmp = exeIcon.ToBitmap())
        {
            bmp.Save(Path.Combine(output, "exe-associated-32.png"), ImageFormat.Png);
            Console.WriteLine("OK associated " + bmp.Width + "x" + bmp.Height);
        }
        return 0;
    }
}
