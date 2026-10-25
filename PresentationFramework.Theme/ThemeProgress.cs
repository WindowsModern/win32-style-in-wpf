using System;
using System.Windows;

namespace Microsoft.Windows.Themes
{
    /// <summary>Progress bar rendering mode.</summary>
    public enum ThemeProgressMode
    {
        Chunked,
        Smooth
    }

    /// <summary>Progress bar native fill state.</summary>
    public enum ThemeProgressState
    {
        Normal,
        Paused,
        Error
    }

    /// <summary>
    /// Attached properties for the themed ProgressBar template.
    /// </summary>
    public static class ThemeProgress
    {

        /// <summary>True on Vista/Windows 7+; XP uses the legacy 5-chunk path.</summary>
        /// <summary>
        /// True for Vista/7 and early Windows 8 builds that still use the
        /// Aero progress animation (fixed 100 DIP glow rectangle).
        /// </summary>
        public static bool IsAero
        {
            get
            {
                Version version = Environment.OSVersion.Version;
                return version.Major >= 6 && version < new Version(6, 2, 9200);
            }
        }

        /// <summary>
        /// True for Windows 8 RTM and later, which use the Aero2 animation
        /// (25% of the indicator width, animated transform origin).
        /// </summary>
        public static bool IsAero2OrLater
        {
            get { return Environment.OSVersion.Version >= new Version(6, 2, 9200); }
        }
        public static bool IsVistaOrLater
        {
            get { return Environment.OSVersion.Version.Major >= 6; }
        }
        public static readonly DependencyProperty IsVistaProperty = DependencyProperty.RegisterAttached(
            "IsVista", typeof(bool), typeof(ThemeProgress),
            new FrameworkPropertyMetadata(Environment.OSVersion.Version.Major >= 6));

        public static bool GetIsVista(DependencyObject element)
        {
            return (bool)element.GetValue(IsVistaProperty);
        }

        public static void SetIsVista(DependencyObject element, bool value)
        {
            element.SetValue(IsVistaProperty, value);
        }
        public static readonly DependencyProperty AnimateValueProperty = DependencyProperty.RegisterAttached(
            "AnimateValue", typeof(bool), typeof(ThemeProgress),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty ValueTransitionDurationProperty = DependencyProperty.RegisterAttached(
            "ValueTransitionDuration", typeof(Duration), typeof(ThemeProgress),
            new FrameworkPropertyMetadata(new Duration(TimeSpan.FromMilliseconds(220))));

        public static bool GetAnimateValue(DependencyObject element)
        {
            return (bool)element.GetValue(AnimateValueProperty);
        }

        public static void SetAnimateValue(DependencyObject element, bool value)
        {
            element.SetValue(AnimateValueProperty, value);
        }

        public static Duration GetValueTransitionDuration(DependencyObject element)
        {
            return (Duration)element.GetValue(ValueTransitionDurationProperty);
        }

        public static void SetValueTransitionDuration(DependencyObject element, Duration value)
        {
            element.SetValue(ValueTransitionDurationProperty, value);
        }
        public static readonly DependencyProperty ModeProperty = DependencyProperty.RegisterAttached(
            "Mode", typeof(ThemeProgressMode), typeof(ThemeProgress),
            new FrameworkPropertyMetadata(ThemeProgressMode.Chunked));

        public static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
            "State", typeof(ThemeProgressState), typeof(ThemeProgress),
            new FrameworkPropertyMetadata(ThemeProgressState.Normal));

        public static ThemeProgressMode GetMode(DependencyObject element)
        {
            return (ThemeProgressMode)element.GetValue(ModeProperty);
        }

        public static void SetMode(DependencyObject element, ThemeProgressMode value)
        {
            element.SetValue(ModeProperty, value);
        }

        public static ThemeProgressState GetState(DependencyObject element)
        {
            return (ThemeProgressState)element.GetValue(StateProperty);
        }

        public static void SetState(DependencyObject element, ThemeProgressState value)
        {
            element.SetValue(StateProperty, value);
        }
    }
}
