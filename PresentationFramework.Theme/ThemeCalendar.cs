using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Replaces Calendar's previous/next month buttons with MonthCal theme parts.
    /// The parts are alpha reconstructed like the rest of the ScrollBar/Progress
    /// overlays.
    /// </summary>
    public static class ThemeCalendar
    {
        public static readonly DependencyProperty EnableProperty = DependencyProperty.RegisterAttached(
            "Enable", typeof(bool), typeof(ThemeCalendar),
            new FrameworkPropertyMetadata(false, OnEnableChanged));

        public static bool GetEnable(DependencyObject element)
        {
            return (bool)element.GetValue(EnableProperty);
        }

        public static void SetEnable(DependencyObject element, bool value)
        {
            element.SetValue(EnableProperty, value);
        }

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var calendar = d as Calendar;
            if (calendar == null)
                return;

            if ((bool)e.NewValue)
            {
                calendar.Loaded += OnCalendarLoaded;
                if (calendar.IsLoaded)
                    Apply(calendar);
            }
            else
            {
                calendar.Loaded -= OnCalendarLoaded;
            }
        }

        private static void OnCalendarLoaded(object sender, RoutedEventArgs e)
        {
            Apply((Calendar)sender);
        }

        private static void Apply(Calendar calendar)
        {
            CalendarItem item = FindVisualChild<CalendarItem>(calendar);
            if (item == null)
            {
                calendar.Dispatcher.BeginInvoke(new Action(() => Apply(calendar)),
                    System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            ApplyButton(item, "PART_PreviousButton", "ThemeCalendarPreviousButtonTemplate");
            ApplyButton(item, "PART_NextButton", "ThemeCalendarNextButtonTemplate");
        }

        private static void ApplyButton(CalendarItem item, string partName, string resourceKey)
        {
            if (item.Template == null)
                return;

            var button = item.Template.FindName(partName, item) as Button;
            if (button == null)
                return;

            var template = item.TryFindResource(resourceKey) as ControlTemplate;
            if (template != null)
                button.Template = template;
        }

        private static T FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                T typed = child as T;
                if (typed != null)
                    return typed;
                T descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }
    }
}