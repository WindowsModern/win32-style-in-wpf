using System;
using System.Drawing;
using System.Windows.Forms;

namespace NativeComparison
{
    internal sealed class ComboBoxCasesForm : Form
    {
        internal ComboBoxCasesForm()
        {
            Text = "Native ComboBox cases - UXTHEME";
            ClientSize = new Size(820, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;

            AddHeader("ComboBox styles", 20, 10);
            AddCombo("DropDownList", 20, 40, ComboBoxStyle.DropDownList, true, false);
            AddCombo("DropDown (editable)", 20, 100, ComboBoxStyle.DropDown, true, false);
            AddCombo("Disabled", 20, 160, ComboBoxStyle.DropDownList, false, false);
            AddCombo("Focused", 20, 220, ComboBoxStyle.DropDownList, true, true);
            AddCombo("DropDownList 2", 20, 300, ComboBoxStyle.DropDownList, true, false);

            var note = new Label
            {
                Text = "Native Win32 ComboBox (comctl32 v6); compare Normal/Hot/Pressed/Disabled/Focused/Open.",
                Location = new Point(20, 390),
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

        private ComboBox AddCombo(string label, int x, int y, ComboBoxStyle style, bool enabled, bool focused)
        {
            Controls.Add(new Label
            {
                Text = label,
                Location = new Point(x, y),
                AutoSize = true
            });

            var combo = new ComboBox
            {
                Location = new Point(x, y + 20),
                Width = 240,
                DropDownStyle = style,
                Enabled = enabled,
                DropDownHeight = 90
            };
            combo.Items.Add("Item one");
            combo.Items.Add("Item two");
            combo.Items.Add("Item three");
            combo.SelectedIndex = 0;
            Controls.Add(combo);

            if (focused)
            {
                Shown += delegate
                {
                    Activate();
                    combo.Focus();
                };
            }

            return combo;
        }
    }
}
