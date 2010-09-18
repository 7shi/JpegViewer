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
        private Timer timer = new Timer();

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
                            if (img == null) setImage(0);
                        }
                    }
                }
            };
            vscrollBar1.ValueChanged += (sender, e) =>
            {
                setImage((vscrollBar1.Maximum - 9) - vscrollBar1.Value);
            };

            Controls.Add(button1);
            Controls.Add(button2);
            Controls.Add(vscrollBar1);

            timer.Tick += timer_Tick;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            var cs = ClientSize;

            var w = 50;
            var h = cs.Height / 2;
            button1.Bounds = new Rectangle(0, 0, w, h);
            button2.Bounds = new Rectangle(0, h, w, h);

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

        bool nextPage;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            var h3 = ClientSize.Height / 3;
            if (e.Y < h3)
                nextPage = true;
            else if (e.Y > h3 * 2)
                nextPage = false;
            else
                return;
            scroll(nextPage);
            timer.Interval = 500;
            timer.Enabled = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            timer.Enabled = false;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            timer.Enabled = false;
            if (timer.Interval > 20)
                timer.Interval = 20;
            scroll(nextPage);
            timer.Enabled = true;
        }

        private void setImage(int index)
        {
            if (files == null || files.Length == 0) return;

            if (img != null) img.Dispose();
            img = new Bitmap(files[index]);
            Invalidate();
        }

        private bool scroll(bool next)
        {
            bool f;
            if (next)
            {
                f = vscrollBar1.Value > 0;
                if (f) vscrollBar1.Value--;
            }
            else
            {
                f = vscrollBar1.Value < vscrollBar1.Maximum - 9;
                if (f) vscrollBar1.Value++;
            }
            return f;
        }
    }
}
