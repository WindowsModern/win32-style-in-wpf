using System;
using System.Drawing;
using System.Windows.Forms;

namespace NativeComparison
{
    internal sealed class ProgressCasesForm : Form
    {
        internal ProgressCasesForm()
        {
            Text = "Native ProgressBar cases - UXTHEME";
            ClientSize = new Size(820, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;

            AddHeader("Horizontal", 20, 10);
            AddProgress("Blocks (chunked), 40%", 20, 42, ProgressBarStyle.Blocks, 40, true);
            AddProgress("Continuous, 40%", 20, 92, ProgressBarStyle.Continuous, 40, true);
            AddProgress("Blocks, 100%", 20, 142, ProgressBarStyle.Blocks, 100, true);
            AddProgress("Blocks, 0%", 20, 192, ProgressBarStyle.Blocks, 0, true);
            AddProgress("Disabled", 20, 242, ProgressBarStyle.Blocks, 40, false);
            AddProgress("Continuous, 100%", 20, 292, ProgressBarStyle.Continuous, 100, true);
            AddProgress("Blocks, indeterminate", 20, 342, ProgressBarStyle.Marquee, 0, true);

            AddLabel("Vertical", 380, 10);
            AddVertical("Blocks, 40%", 380, 42, ProgressBarStyle.Blocks, 40, true);
            AddVertical("Continuous, 40%", 430, 42, ProgressBarStyle.Continuous, 40, true);
            AddVertical("Indeterminate", 480, 42, ProgressBarStyle.Marquee, 0, true);

            var note = new Label
            {
                Text = "Native msctls_progress32: horizontal/vertical, blocks/continuous/marquee on this OS.",
                Location = new Point(20, 355),
                AutoSize = true
            };
            Controls.Add(note);
        }

        private void AddHeader(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            });
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

        private void AddVertical(string label, int x, int y, ProgressBarStyle style, int value, bool enabled)
        {
            Controls.Add(new Label
            {
                Text = label,
                Location = new Point(x, y),
                AutoSize = true
            });

            Controls.Add(new NativeVerticalProgressBar
            {
                Location = new Point(x, y + 20),
                Size = new Size(24, 160),
                AutoSize = false,
                Style = style,
                Minimum = 0,
                Maximum = 100,
                Value = value,
                Enabled = enabled,
                MarqueeAnimationSpeed = 30
            });
        }

        private void AddProgress(string label, int x, int y, ProgressBarStyle style, int value, bool enabled)
        {
            Controls.Add(new Label
            {
                Text = label,
                Location = new Point(x, y),
                AutoSize = true
            });

            Controls.Add(new ProgressBar
            {
                Location = new Point(x + 220, y),
                Size = new Size(320, 22),
                Style = style,
                Minimum = 0,
                Maximum = 100,
                Value = value,
                Enabled = enabled
            });
        }
    }
}
