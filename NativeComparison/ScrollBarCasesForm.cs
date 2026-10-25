using System;
using System.Drawing;
using System.Windows.Forms;

namespace NativeComparison
{
    internal sealed class ScrollBarCasesForm : Form
    {
        internal ScrollBarCasesForm()
        {
            Text = "Native ScrollBar cases - UXTHEME";
            ClientSize = new Size(820, 430);
            StartPosition = FormStartPosition.CenterScreen;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;

            AddLabel("Horizontal, enabled", 20, 16);
            AddScroll(new HScrollBar
            {
                Location = new Point(20, 40),
                Size = new Size(240, 17),
                Minimum = 0,
                Maximum = 100,
                Value = 30,
                LargeChange = 10
            });

            AddLabel("Horizontal, disabled", 300, 16);
            AddScroll(new HScrollBar
            {
                Location = new Point(300, 40),
                Size = new Size(240, 17),
                Minimum = 0,
                Maximum = 100,
                Value = 30,
                LargeChange = 10,
                Enabled = false
            });

            AddLabel("V enabled", 20, 90);
            AddScroll(new VScrollBar
            {
                Location = new Point(20, 114),
                Size = new Size(17, 180),
                Minimum = 0,
                Maximum = 100,
                Value = 30,
                LargeChange = 10
            });

            AddLabel("V disabled", 70, 90);
            AddScroll(new VScrollBar
            {
                Location = new Point(70, 114),
                Size = new Size(17, 180),
                Minimum = 0,
                Maximum = 100,
                Value = 30,
                LargeChange = 10,
                Enabled = false
            });

            AddLabel("Horizontal, maximum value", 200, 190);
            AddScroll(new HScrollBar
            {
                Location = new Point(200, 214),
                Size = new Size(240, 17),
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                LargeChange = 10
            });

            var note = new Label
            {
                Text = "Arrows: part 1 (up/down/left/right state groups)",
                Location = new Point(20, 330),
                AutoSize = true
            };
            Controls.Add(note);
            var note2 = new Label
            {
                Text = "Thumb: part 2/3; track: part 4-7.",
                Location = new Point(20, 352),
                AutoSize = true
            };
            Controls.Add(note2);
        }

        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true
            });
        }

        private void AddScroll(ScrollBar bar)
        {
            Controls.Add(bar);
        }
    }
}
