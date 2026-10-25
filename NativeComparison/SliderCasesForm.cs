using System;
using System.Drawing;
using System.Windows.Forms;

namespace NativeComparison
{
    internal sealed class SliderCasesForm : Form
    {
        internal SliderCasesForm()
        {
            Text = "Native TrackBar cases - UXTHEME";
            ClientSize = new Size(820, 430);
            StartPosition = FormStartPosition.CenterScreen;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;

            AddHeader("Horizontal", 20, 10);
            AddTrack("None", 20, 42, Orientation.Horizontal, TickStyle.None, true, 220, 45);
            AddTrack("TopLeft", 270, 42, Orientation.Horizontal, TickStyle.TopLeft, true, 220, 45);
            AddTrack("BottomRight", 520, 42, Orientation.Horizontal, TickStyle.BottomRight, true, 220, 45);
            AddTrack("Both", 20, 130, Orientation.Horizontal, TickStyle.Both, true, 220, 45);
            AddTrack("Disabled", 270, 130, Orientation.Horizontal, TickStyle.None, false, 220, 45);

            AddHeader("Vertical", 20, 205);
            AddTrack("None", 20, 238, Orientation.Vertical, TickStyle.None, true, 45, 150);
            AddTrack("Left", 100, 238, Orientation.Vertical, TickStyle.TopLeft, true, 45, 150);
            AddTrack("Right", 180, 238, Orientation.Vertical, TickStyle.BottomRight, true, 45, 150);
            AddTrack("Both", 260, 238, Orientation.Vertical, TickStyle.Both, true, 45, 150);
            AddTrack("Disabled", 340, 238, Orientation.Vertical, TickStyle.None, false, 45, 150);

            var note = new Label
            {
                Text = "WinForms TrackBar = native msctls_trackbar32.",
                Location = new Point(430, 260),
                AutoSize = true
            };
            Controls.Add(note);
            var note2 = new Label
            {
                Text = "Vertical TopLeft = left ticks; BottomRight = right ticks.",
                Location = new Point(430, 282),
                AutoSize = true
            };
            Controls.Add(note2);
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

        private void AddTrack(string label, int x, int y, Orientation orientation,
            TickStyle tickStyle, bool enabled, int width, int height)
        {
            Controls.Add(new Label
            {
                Text = label,
                Location = new Point(x, y),
                AutoSize = true
            });

            var track = new TrackBar
            {
                Location = new Point(x, y + 20),
                Size = new Size(width, height),
                Minimum = 0,
                Maximum = 100,
                Value = 40,
                TickFrequency = 10,
                SmallChange = 1,
                LargeChange = 10,
                TickStyle = tickStyle,
                Orientation = orientation,
                AutoSize = false,
                Enabled = enabled
            };
            Controls.Add(track);
        }
    }
}
