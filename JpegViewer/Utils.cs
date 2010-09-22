//#define QVGA

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace System
{
    public class STAThreadAttribute : Attribute { }
    public delegate void Action();
    public delegate void Action<T1, T2, T3>(T1 a, T2 b, T3 c);
}

namespace System.Runtime.CompilerServices
{
    public class ExtensionAttribute : Attribute { }
}

namespace Local
{
    public static class Utils
    {
        public static readonly bool IsWin32 =
            Environment.OSVersion.Platform.ToString().StartsWith("Win32");

        public static readonly bool IsWinCE =
            Environment.OSVersion.Platform == PlatformID.WinCE;

        public static string ExecutablePath
        {
            get
            {
                return Assembly.GetExecutingAssembly()
                    .ManifestModule.FullyQualifiedName;
            }
        }

        public static String ExecutableDirectory
        {
            get
            {
                return Path.GetDirectoryName(ExecutablePath);
            }
        }

        public static bool ParseIntBool(string s, bool value)
        {
            return ParseInt(s, value ? 1 : 0) != 0;
        }

        public static int ParseInt(string s)
        {
            return ParseInt(s, 0);
        }

        public static int ParseInt(string s, int value)
        {
            try
            {
                return int.Parse(s);
            }
            catch
            {
                return value;
            }
        }

        private static int fontHeight, controlHeight;

        private static void Init()
        {
            if (fontHeight > 0) return;

            using (var f = new Form())
            {
                var t = new TextBox();
                f.Controls.Add(t);
                controlHeight = t.Height;
                using (var g = f.CreateGraphics())
                    fontHeight = g.MeasureString("pg!あ", f.Font).ToSize().Height + 1;
            }
        }

        public static int FontHeight
        {
            get
            {
                Init();
                return fontHeight;
            }
        }

        public static int ControlHeight
        {
            get
            {
                Init();
                return controlHeight;
            }
        }

        public static int Distance(int a, int b)
        {
            return (int)(Math.Sqrt(a * a + b * b) + 0.5);
        }

        public static Size MeasureString(Control c, string s)
        {
            Size ret;
            using (var g = c.CreateGraphics())
                ret = g.MeasureString(s, c.Font).ToSize();
            return ret;
        }

        public static readonly PixelFormat PixelFormat =
            IsWinCE ? PixelFormat.Format16bppRgb565 : PixelFormat.Format32bppRgb;

        public static Bitmap CreateTextBitmap(string s, Font f, Color fore, Color back)
        {
            Bitmap ret;
            using (var brush = new SolidBrush(fore))
            {
                Size size;
                using (var temp = new Bitmap(1, 1))
                using (var g = Graphics.FromImage(temp))
                    size = g.MeasureString(s, f).ToSize();
                ret = new Bitmap(size.Width + 1, size.Height + 1, PixelFormat);
                using (var g = Graphics.FromImage(ret))
                {
                    g.Clear(back);
                    g.DrawString(s, f, brush, 0, 0);
                }
            }
            return ret;
        }

        public static Bitmap CreateFitBitmap(Image src, int width, Color back)
        {
            var ret = new Bitmap(width, src.Height, PixelFormat);
            if (ret.Width >= src.Width)
            {
                using (var g = Graphics.FromImage(ret))
                {
                    g.Clear(back);
                    g.DrawImage(src, (ret.Width - src.Width) / 2, 0);
                }
            }
            else
            {
                var dr = new Rectangle(0, 0, ret.Width, ret.Height);
                var sr = new Rectangle(0, 0, src.Width, src.Height);
                using (var g = Graphics.FromImage(ret))
                {
                    g.Clear(back);
                    g.DrawImage(src, dr, sr, GraphicsUnit.Pixel);
                }
            }
            return ret;
        }

        public static Point GetZoomOffset(Point offset, int x, int y, int zold, int znew)
        {
            int x1 = x - offset.X;
            int y1 = y - offset.Y;
            int x2 = x1 * znew / zold;
            int y2 = y1 * znew / zold;
            return new Point(offset.X + x1 - x2, offset.Y + y1 - y2);
        }

        private static bool GetIsQVGA()
        {
#if QVGA
            return true;
#else
            var r = Screen.PrimaryScreen.Bounds;
            return Math.Max(r.Width, r.Height) <= 320;
#endif
        }

        public static readonly bool IsQVGA = GetIsQVGA();

        private static Size GetDefaultFormSize()
        {
#if QVGA
            return new Size(240, 320);
#else
            var r = Screen.PrimaryScreen.Bounds;
            var ret = new Size(1024, 768);
            if (r.Width <= ret.Width || r.Height <= ret.Height)
                ret = new Size(800, 600);
            if (r.Width <= ret.Width || r.Height <= ret.Height)
                ret = new Size(640, 480);
            if (r.Width <= ret.Width || r.Height <= ret.Height)
                ret = new Size(480, 320);
            return ret;
#endif
        }

        public static Size DefaultFormSize = GetDefaultFormSize();

        public static Rectangle Inscribed(Size s1, Size s2)
        {
            int w = s2.Width * s1.Height / s2.Height;
            if (w < s1.Width)
                return new Rectangle((s1.Width - w) / 2, 0, w, s1.Height);

            int h = s2.Height * s1.Width / s2.Width;
            return new Rectangle(0, (s1.Height - h) / 2, s1.Width, h);
        }

        public static Rectangle Scale(Rectangle r, int zoom)
        {
            return new Rectangle(
                r.X * zoom, r.Y * zoom,
                r.Width * zoom, r.Height * zoom);
        }

        public static Rectangle GetRectangle(int x1, int y1, int x2, int y2)
        {
            if (x1 > x2) Swap(ref x1, ref x2);
            if (y1 > y2) Swap(ref y1, ref y2);
            return Rectangle.FromLTRB(x1, y1, x2, y2);
        }

        public static void Swap(ref int a, ref int b)
        {
            int c = a;
            a = b;
            b = c;
        }

        public static void ReadIni(string path, Action<string, string, string> delg)
        {
            try
            {
                var sr = new StreamReader(path);
                string line, section = "";
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        section = line.Substring(1, line.Length - 2);
                        continue;
                    }
                    var p = line.IndexOf('=');
                    if (p < 0) continue;

                    var key = line.Substring(0, p).Trim();
                    var value = line.Substring(p + 1).Trim();
                    delg(section, key, value);
                }
                sr.Close();
            }
            catch { }
        }

        private static class Win32
        {
            [DllImport("kernel32.dll")]
            public static extern int GetLogicalDrives();
        }

        public static string[] GetLogicalDrives()
        {
            if (!IsWin32) return null;

            var d = Win32.GetLogicalDrives();
            var list = new List<string>();
            for (int i = 0; i < 26; i++)
            {
                if ((d & (1 << i)) != 0)
                    list.Add(((char)('A' + i)) + ":\\");
            }
            return list.ToArray();
        }
    }
}
