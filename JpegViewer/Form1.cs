using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Local.Windows.Forms;

namespace JpegViewer
{
    public class Form1 : Form
    {
        private Button button1, button2;
        private VScrollBar vscrollBar1;
        private string dir;
        private string[] files;
        private Image img;

        public Form1()
        {
            button1 = new Button { Text = "×", TabIndex = 9 };
            button2 = new Button { Text = "□", TabIndex = 0 };
            vscrollBar1 = new VScrollBar { Maximum = 0, LargeChange = 10 };

            Text = "JPEG Viewer";
            if (Environment.OSVersion.Platform == PlatformID.WinCE)
            {
                TopMost = true;
                FormBorderStyle = FormBorderStyle.None;
                Bounds = Screen.PrimaryScreen.Bounds;
            }
            else
            {
                ClientSize = new Size(800, 480);
            }

            button1.Click += (sender, e) => Close();
            button2.Click += (sender, e) =>
            {
                using (var fd = new FolderDialog())
                {
                    if (fd.ShowDialog(this) == DialogResult.OK)
                    {
                        dir = fd.SelectedPath;
                        files = Directory.GetFiles(dir, "*.jpg");
                        if (img != null) img.Dispose();
                        img = null;
                        if (files.Length == 0)
                        {
                            vscrollBar1.Value = vscrollBar1.Maximum = 0;
                            Invalidate();
                        }
                        else
                        {
                            vscrollBar1.Maximum = files.Length + 8;
                            vscrollBar1.Value = files.Length - 1;
                            if (img == null) SetImage(0);
                        }
                    }
                }
            };
            vscrollBar1.ValueChanged += (sender, e) =>
            {
                SetImage((vscrollBar1.Maximum - 9) - vscrollBar1.Value);
            };

            Controls.Add(button1);
            Controls.Add(button2);
            Controls.Add(vscrollBar1);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            var cs = ClientSize;

            var h = cs.Height / 2;
            button1.Bounds = new Rectangle(0, 0, 32, h);
            button2.Bounds = new Rectangle(0, h, 32, h);

            var w = 32; // vscrollBar1.Width;
            vscrollBar1.Bounds = new Rectangle(cs.Width - w, 0, w, cs.Height);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (img == null) return;

            var s = ClientSize;
            e.Graphics.DrawImage(img, (s.Width - img.Width) / 2, (s.Height - img.Height) / 2);
        }

        private void SetImage(int index)
        {
            if (files == null || files.Length == 0) return;

            if (img != null) img.Dispose();
            img = new Bitmap(files[index]);
            Invalidate();
        }
    }
}
