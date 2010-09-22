using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Local;

namespace Local.Windows.Forms
{
    public class FolderDialog : IDisposable
    {
        private Form form = new Form();
        private TreeView tree = new TreeView();
        private Button OK = new Button { Text = "OK" };
        private Button Cancel = new Button { Text = "キャンセル" };
        public string SelectedPath =
                Utils.IsWin32
                ? Environment.GetFolderPath(Environment.SpecialFolder.Personal)
                : "" + Path.DirectorySeparatorChar;

        public FolderDialog()
        {
            form.Text = "フォルダの選択";
            form.MinimizeBox = false;
            form.Resize += (sender, e) => SetLayout();
            if (Utils.IsWinCE)
            {
                var r = Screen.PrimaryScreen.Bounds;
                if (r.Width > r.Height)
                {
                    r.Width /= 2;
                    r.X = r.Width / 2;
                    r.Height -= 64;
                    r.Y += 32;
                }
                else
                {
                    r.X += 32;
                    r.Y += 32;
                    r.Width -= 64;
                    r.Height -= 64;
                }
                form.TopMost = true;
                form.Bounds = r;
            }

            tree.BeforeExpand += (sender, e) => ListDirectory(e.Node);
            tree.AfterSelect += (sender, e) =>
            {
                ListDirectory(e.Node);
                SelectedPath = e.Node.Tag.ToString();
            };
            form.Controls.Add(tree);

            int h = Utils.ControlHeight;
            OK.Height = Cancel.Height = h;
            var size = Utils.MeasureString(form, Cancel.Text);
            OK.Width = Cancel.Width = size.Width + h;
            OK.DialogResult = DialogResult.OK;
            Cancel.DialogResult = DialogResult.Cancel;
            form.Controls.Add(OK);
            form.Controls.Add(Cancel);

            SetLayout();
        }

        public void Dispose()
        {
            form.Dispose();
        }

        public DialogResult ShowDialog(Form parent)
        {
            if (!Utils.IsWinCE)
            {
                int x = parent.Left + (parent.Width - form.Width) / 2;
                int y = parent.Top + (parent.Height - form.Height) / 2;
                form.Location = new Point(x, y);
            }
            var di = new DirectoryInfo(SelectedPath);
            var drives = Utils.GetLogicalDrives();
            if (drives != null)
            {
                foreach (var drive in drives)
                {
                    var tag = new DirectoryInfo(drive);
                    var n = new TreeNode(drive) { Tag = tag };
                    tree.Nodes.Add(n);
                }
            }
            else
                ListDirectory(tree.Nodes, di.Root);
            SelectPath(di);
            return form.ShowDialog();
        }

        private void SelectPath(DirectoryInfo dir)
        {
            var path = dir.FullName;
            var dirs = new Stack<DirectoryInfo>();
            for (; dir.Parent != null; dir = dir.Parent) dirs.Push(dir);
            if (Utils.IsWin32) dirs.Push(dir.Root);
            TreeNode node = null;
            while (dirs.Count > 0)
            {
                var di = dirs.Pop();
                var nodes = node == null ? tree.Nodes : node.Nodes;
                TreeNode nn = null;
                foreach (TreeNode n in nodes)
                {
                    if (n.Text == di.Name)
                    {
                        nn = n;
                        break;
                    }
                }
                if (nn == null) return;
                ListDirectory(nn);
                tree.SelectedNode = nn;
                node = nn;
            }
        }

        private void ListDirectory(TreeNode node)
        {
            var tag = node.Tag as DirectoryInfo;
            if (tag == null) return;

            node.Tag = tag.FullName;
            node.Nodes.Clear();
            ListDirectory(node.Nodes, tag);
            node.Expand();
        }

        private void ListDirectory(TreeNodeCollection nodes, DirectoryInfo dir)
        {
            DirectoryInfo[] dirs;
            try
            {
                dirs = dir.GetDirectories();
            }
            catch
            {
                return;
            }
            foreach (var di in dirs)
            {
                if ((di.Attributes & FileAttributes.Hidden) != 0)
                    continue;
                var n = new TreeNode(di.Name);
                n.Nodes.Add(new TreeNode("..."));
                n.Tag = di;
                nodes.Add(n);
            }
        }

        private void SetLayout()
        {
            var size = form.ClientSize;
            int w = OK.Width, h = OK.Height, h2 = h / 2;
            tree.Bounds = new Rectangle(h2, h2, size.Width - h, size.Height - h * 2 - h2);
            Cancel.Location = new Point(size.Width - w - h2, size.Height - h - h2);
            OK.Location = new Point(Cancel.Left - w - h2, Cancel.Top);
        }
    }
}
