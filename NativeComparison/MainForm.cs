using System;
using System.Drawing;
using System.Windows.Forms;

namespace NativeComparison
{
    internal sealed class MainForm : Form
    {
        private readonly ToolTip _toolTip = new ToolTip();

        internal MainForm()
        {
            Text = "Native Win32 controls - UXTHEME";
            ClientSize = new Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;

            var buttons = new GroupBox
            {
                Text = "Buttons / selection",
                Location = new Point(16, 12),
                Size = new Size(420, 150)
            };

            var normal = new Button { Text = "Button", Location = new Point(16, 28), Size = new Size(100, 30) };
            var def = new Button { Text = "Default", Location = new Point(124, 28), Size = new Size(100, 30) };
            var disabled = new Button { Text = "Disabled", Location = new Point(232, 28), Size = new Size(100, 30), Enabled = false };
            var check = new CheckBox { Text = "Checked", Location = new Point(16, 72), AutoSize = true, Checked = true };
            var mixed = new CheckBox { Text = "Indeterminate", Location = new Point(120, 72), AutoSize = true, ThreeState = true, CheckState = CheckState.Indeterminate };
            var radio = new RadioButton { Text = "Option A", Location = new Point(16, 102), AutoSize = true, Checked = true };
            var radio2 = new RadioButton { Text = "Option B", Location = new Point(120, 102), AutoSize = true };
            buttons.Controls.AddRange(new Control[] { normal, def, disabled, check, mixed, radio, radio2 });
            AcceptButton = def;

            var text = new GroupBox
            {
                Text = "Text / combo",
                Location = new Point(16, 174),
                Size = new Size(420, 120)
            };
            var textBox = new TextBox { Text = "TextBox", Location = new Point(16, 28), Width = 180 };
            var readOnly = new TextBox { Text = "Read only", Location = new Point(208, 28), Width = 180, ReadOnly = true };
            var password = new TextBox { Location = new Point(16, 64), Width = 180, UseSystemPasswordChar = true, Text = "secret" };
            var combo = new ComboBox { Location = new Point(208, 64), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            combo.Items.AddRange(new object[] { "Item one", "Item two", "Item three" });
            combo.SelectedIndex = 0;
            text.Controls.AddRange(new Control[] { textBox, readOnly, password, combo });

            var range = new GroupBox
            {
                Text = "Range / progress",
                Location = new Point(16, 306),
                Size = new Size(420, 150)
            };
            var track = new TrackBar { Minimum = 0, Maximum = 100, Value = 40, Location = new Point(16, 24), Width = 300, TickStyle = TickStyle.None };
            var progress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 40, Location = new Point(16, 82), Width = 300, Height = 18 };
            var hscroll = new HScrollBar { Minimum = 0, Maximum = 100, Value = 30, Location = new Point(16, 112), Width = 300, Height = 17 };
            range.Controls.AddRange(new Control[] { track, progress, hscroll });

            var containers = new GroupBox
            {
                Text = "Tab / group / menu",
                Location = new Point(452, 12),
                Size = new Size(432, 240)
            };
            var tabs = new TabControl { Location = new Point(12, 22), Size = new Size(408, 140) };
            var tab1 = new TabPage("Tab one");
            tab1.Controls.Add(new Label { Text = "First page", Location = new Point(12, 12), AutoSize = true });
            var tab2 = new TabPage("Tab two");
            tab2.Controls.Add(new Label { Text = "Second page", Location = new Point(12, 12), AutoSize = true });
            tabs.TabPages.Add(tab1);
            tabs.TabPages.Add(tab2);

            var group = new GroupBox { Text = "Group", Location = new Point(12, 170), Size = new Size(200, 58) };
            group.Controls.Add(new Label { Text = "Content", Location = new Point(12, 22), AutoSize = true });

            var tipButton = new Button { Text = "ToolTip / ContextMenu", Location = new Point(224, 180), Size = new Size(180, 30) };
            _toolTip.SetToolTip(tipButton, "Native Win32 tooltip");
            var menu = new ContextMenuStrip();
            menu.Items.Add("Context item");
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Another item");
            tipButton.ContextMenuStrip = menu;

            containers.Controls.AddRange(new Control[] { tabs, group, tipButton });

            var list = new ListView
            {
                Location = new Point(452, 262),
                Size = new Size(432, 160),
                View = View.Details,
                FullRowSelect = true
            };
            list.Columns.Add("Name", 160);
            list.Columns.Add("Value", 100);
            list.Items.Add(new ListViewItem(new[] { "Alpha", "1" }));
            list.Items.Add(new ListViewItem(new[] { "Beta", "2" }));
            list.Items.Add(new ListViewItem(new[] { "Gamma", "3" }));

            var separator = new Panel { Location = new Point(452, 434), Size = new Size(432, 2), BorderStyle = BorderStyle.Fixed3D };

            var status = new StatusStrip();
            status.Items.Add(new ToolStripStatusLabel("Ready"));
            status.Items.Add(new ToolStripStatusLabel("Native Win32 / UXTHEME"));

            Controls.AddRange(new Control[] { buttons, text, range, containers, list, separator, status });
        }
    }
}
