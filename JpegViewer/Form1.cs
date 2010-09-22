using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Local;

namespace JpegViewer
{
    public class Form1 : Form
    {
        private Button button1, button2, button3;
        private VScrollBar vscrollBar1;
        private BinaryReader br;
        private ZipDirHeader[] files;
        private Image img;
        private Timer timer = new Timer();
        private OpenFileDialog ofd = new OpenFileDialog();

        public Form1()
        {
            var back = BackColor;
            BackColor = Color.Black;
            button1 = new Button { Text = "×", TabIndex = 8, BackColor = back };
            button2 = new Button { Text = "|", TabIndex = 9, BackColor = back };
            button3 = new Button { Text = "O", TabIndex = 0, BackColor = back };
            vscrollBar1 = new VScrollBar { Maximum = 0, LargeChange = 10, BackColor = back };
            ofd.Filter = "無圧縮 ZIP ファイル (*.zip)|*.zip|すべてのファイル (*.*)|*.*";

            Text = "JPEG Viewer";
            if (Utils.IsWinCE)
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
            button2.Click += (sender, e) => this.Minimize();
            button3.Click += button3_Click;
            vscrollBar1.ValueChanged += (sender, e) =>
                setImage((vscrollBar1.Maximum - 9) - vscrollBar1.Value);

            Controls.Add(button1);
            Controls.Add(button2);
            Controls.Add(button3);
            Controls.Add(vscrollBar1);

            timer.Tick += timer_Tick;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (ofd.ShowDialog() != DialogResult.OK) return;

            FileStream fs = null;
            BinaryReader br = null;
            ZipDirHeader[] files = null;
            try
            {
                fs = new FileStream(ofd.FileName, FileMode.Open);
                br = new BinaryReader(fs);
                files = Zip.GetFiles(br, zipdh =>
                {
                    if (zipdh.Header.Compression != 0) return false;
                    if ((zipdh.Attrs & (uint)FileAttributes.Directory) != 0) return false;

                    var fb = zipdh.Filename;
                    var fn = Encoding.Default.GetString(fb, 0, fb.Length);
                    return Path.GetExtension(fn).ToLower() == ".jpg";
                });
            }
            catch { }
            if (fs == null)
                Utils.Warning("ファイルが開けません。", Text);
            else if (files == null || files.Length == 0)
            {
                fs.Close();
                if (files == null)
                    Utils.Warning("ZIP ファイルではありません。", Text);
                else
                    Utils.Warning("JPEG ファイルが無圧縮で含まれていません。", Text);
            }
            else
            {
                if (img != null) img.Dispose();
                img = null;
                if (this.br != null) this.br.Close();
                this.br = br;
                this.files = files;
                vscrollBar1.Maximum = files.Length + 8;
                vscrollBar1.Value = files.Length - 1;
                setImage(0);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            var cs = ClientSize;

            var w = 50;
            var h = cs.Height / 3;
            button1.Bounds = new Rectangle(0, 0, w, h);
            button2.Bounds = new Rectangle(0, h, w, h);
            button3.Bounds = new Rectangle(0, h * 2, w, h);

            vscrollBar1.Bounds = new Rectangle(cs.Width - w, 0, w, cs.Height);
            DrawImage();
        }

        private void DrawImage()
        {
            using (var g = CreateGraphics()) DrawImage(g);
        }

        private void DrawImage(Graphics g)
        {
            using (var brush = new SolidBrush(BackColor))
            {
                var s = ClientSize;
                if (img == null)
                    g.FillRectangle(brush, 0, 0, s.Width, s.Height);
                else
                {
                    int l = (s.Width - img.Width) / 2;
                    int t = (s.Height - img.Height) / 2;
                    int r = l + img.Width;
                    int b = t + img.Height;
                    if (l > 0) g.FillRectangle(brush, 0, 0, l, s.Height);
                    if (t > 0) g.FillRectangle(brush, l, 0, img.Width, t);
                    g.DrawImage(img, (s.Width - img.Width) / 2, (s.Height - img.Height) / 2);
                    if (r < s.Width) g.FillRectangle(brush, r, 0, s.Width - r, s.Height);
                    if (b < s.Height) g.FillRectangle(brush, l, b, img.Width, s.Height - b);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (img != null) DrawImage(e.Graphics);
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

            try
            {
                using (var ss = files[index].GetSubStream(br))
                {
                    var img = new Bitmap(ss);
                    if (this.img != null) this.img.Dispose();
                    this.img = img;
                }
                DrawImage();
            }
            catch { }
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
