using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace JpegViewer
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.Run(new Form1());
        }
    }
}
