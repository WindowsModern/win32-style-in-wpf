using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ThemeDemo
{
    public sealed class SampleRow
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _progressTimer;
        private int _progressValue;

        public MainWindow(bool transition)
        {
            InitializeComponent();

            ModeText.Text = transition
                ? "Theme: with transition (cross-fade 150 ms)"
                : "Theme: plain (transition disabled)";

            SamplePassword.Password = "secret";

            var rows = new List<SampleRow>
            {
                new SampleRow { Name = "Alpha", Value = "1" },
                new SampleRow { Name = "Beta", Value = "2" },
                new SampleRow { Name = "Gamma", Value = "3" }
            };

            SampleList.ItemsSource = rows;
            SampleGrid.ItemsSource = rows;

            _progressValue = 0;
            Loaded += delegate { SetProgressValues(_progressValue); };
            _progressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _progressTimer.Tick += OnProgressTick;
            _progressTimer.Start();
            Closed += delegate { _progressTimer.Stop(); };
        }

        private void OnProgressTick(object sender, EventArgs e)
        {
            _progressValue += 10;
            if (_progressValue > 100)
                _progressValue = 0;
            SetProgressValues(_progressValue);
        }

        private void SetProgressValues(int value)
        {
            VisitProgressBars(this, delegate(ProgressBar bar)
            {
                if (!bar.IsIndeterminate)
                    bar.Value = value;
            });
        }

        private static void VisitProgressBars(DependencyObject parent, Action<ProgressBar> action)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var bar = child as ProgressBar;
                if (bar != null)
                    action(bar);
                VisitProgressBars(child, action);
            }
        }
    }
}
