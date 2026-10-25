using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NativeComparison
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string output = null;
            string formKind = "controls";
            foreach (string arg in args)
            {
                if (arg.StartsWith("--out=", StringComparison.OrdinalIgnoreCase))
                    output = arg.Substring("--out=".Length);
                else if (arg.StartsWith("--form=", StringComparison.OrdinalIgnoreCase))
                    formKind = arg.Substring("--form=".Length);
            }

            Form form;
            if (string.Equals(formKind, "sliders", StringComparison.OrdinalIgnoreCase))
                form = new SliderCasesForm();
            else if (string.Equals(formKind, "scrollbars", StringComparison.OrdinalIgnoreCase))
                form = new ScrollBarCasesForm();
            else if (string.Equals(formKind, "comboboxes", StringComparison.OrdinalIgnoreCase))
                form = new ComboBoxCasesForm();
            else if (string.Equals(formKind, "progress", StringComparison.OrdinalIgnoreCase))
                form = new ProgressCasesForm();
            else
                form = new MainForm();
            if (output == null)
            {
                Application.Run(form);
                return;
            }

            form.Show();
            form.Activate();
            for (int i = 0; i < 30; i++)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(25);
            }

            // DrawToBitmap sends WM_PRINT and commonly loses visual styles on XP.
            // Capture the real screen pixels instead, including the title bar.
            Rectangle bounds = form.Bounds;
            using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                bitmap.Save(output);
            }

            form.Close();
        }
    }
}
