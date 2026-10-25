using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Animates the determinate ProgressBar indicator when Value changes.
    /// Updates are coalesced to one Dispatcher render pass so high-frequency
    /// Value assignments do not queue Storyboards.
    /// </summary>
    internal static class ProgressBarAnimator
    {
        private static bool _initialized;
        private static readonly ConditionalWeakTable<ProgressBar, State> States =
            new ConditionalWeakTable<ProgressBar, State>();

        internal static void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;
            EventManager.RegisterClassHandler(typeof(ProgressBar),
                FrameworkElement.LoadedEvent, new RoutedEventHandler(OnLoaded));
        }

        private sealed class State
        {
            public ProgressBar Bar;
            public Storyboard Storyboard;
            public bool Pending;
            public bool Attached;
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            var bar = sender as ProgressBar;
            if (bar == null)
                return;

            State state;
            if (!States.TryGetValue(bar, out state))
            {
                state = new State { Bar = bar };
                States.Add(bar, state);
                state.Attached = true;
                bar.ValueChanged += OnValueChanged;
                bar.SizeChanged += OnSizeChanged;
                bar.Unloaded += OnUnloaded;
                DependencyPropertyDescriptor
                    .FromProperty(ProgressBar.IsIndeterminateProperty, typeof(ProgressBar))
                    .AddValueChanged(bar, OnIndeterminateChanged);
            }

            Schedule(state);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            var bar = sender as ProgressBar;
            if (bar == null)
                return;

            State state;
            if (!States.TryGetValue(bar, out state))
                return;

            if (state.Attached)
            {
                bar.ValueChanged -= OnValueChanged;
                bar.SizeChanged -= OnSizeChanged;
                bar.Unloaded -= OnUnloaded;
                DependencyPropertyDescriptor
                    .FromProperty(ProgressBar.IsIndeterminateProperty, typeof(ProgressBar))
                    .RemoveValueChanged(bar, OnIndeterminateChanged);
                state.Attached = false;
            }

            Stop(state);
            States.Remove(bar);
        }

        private static void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            State state;
            if (States.TryGetValue((ProgressBar)sender, out state))
                Schedule(state);
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            State state;
            if (States.TryGetValue((ProgressBar)sender, out state))
                Schedule(state);
        }

        private static void OnIndeterminateChanged(object sender, EventArgs e)
        {
            var bar = sender as ProgressBar;
            State state;
            if (bar == null || !States.TryGetValue(bar, out state))
                return;

            if (bar.IsIndeterminate)
                Stop(state);
            else
                Schedule(state);
        }

        private static void Schedule(State state)
        {
            if (state.Pending)
                return;

            state.Pending = true;
            state.Bar.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                state.Pending = false;
                Flush(state);
            }));
        }

        private static void Flush(State state)
        {
            ProgressBar bar = state.Bar;
            if (!bar.IsLoaded || bar.IsIndeterminate || !ThemeProgress.GetAnimateValue(bar))
            {
                Stop(state);
                return;
            }

            FrameworkElement track = FindTemplateChild(bar, "PART_Track");
            FrameworkElement indicator = FindTemplateChild(bar, "PART_Indicator")
                ?? FindTemplateChild(bar, "Indicator");
            if (track == null || indicator == null)
                return;

            double trackLength = bar.Orientation == Orientation.Vertical
                ? track.ActualHeight : track.ActualWidth;
            if (!IsFinite(trackLength) || trackLength <= 0)
                return;

            double minimum = bar.Minimum;
            double maximum = bar.Maximum;
            double value = bar.Value;
            if (!IsFinite(minimum) || !IsFinite(maximum) || !IsFinite(value))
                return;

            if (maximum <= minimum)
            {
                Stop(state);
                return;
            }

            double fraction = (value - minimum) / (maximum - minimum);
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;
            double target = trackLength * fraction;

            double current = bar.Orientation == Orientation.Vertical
                ? indicator.ActualHeight : indicator.ActualWidth;
            if (!IsFinite(current) || current < 0)
                current = 0;
            if (current > trackLength)
                current = trackLength;

            Duration duration = ThemeProgress.GetValueTransitionDuration(bar);
            if (!duration.HasTimeSpan || duration.TimeSpan <= TimeSpan.Zero ||
                Math.Abs(target - current) < 0.1)
            {
                Stop(state);
                return;
            }

            var animation = new DoubleAnimation(current, target, duration)
            {
                FillBehavior = FillBehavior.Stop,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(animation, indicator);
            Storyboard.SetTargetProperty(animation,
                new PropertyPath(bar.Orientation == Orientation.Vertical
                    ? FrameworkElement.HeightProperty
                    : FrameworkElement.WidthProperty));

            if (state.Storyboard != null)
                state.Storyboard.Stop();

            state.Storyboard = new Storyboard();
            state.Storyboard.Children.Add(animation);
            state.Storyboard.Begin(indicator, HandoffBehavior.SnapshotAndReplace, true);
        }

        private static void Stop(State state)
        {
            if (state.Storyboard != null)
            {
                state.Storyboard.Stop();
                state.Storyboard = null;
            }
        }

        private static FrameworkElement FindTemplateChild(Control control, string name)
        {
            if (control.Template == null)
                return null;
            return control.Template.FindName(name, control) as FrameworkElement;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}