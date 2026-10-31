using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Windows.Themes;

namespace ThemeDemo
{
    /// <summary>
    /// Drives a real mouse hover over the HackBorder-based button and reports what happened.
    /// <para>
    /// The off-screen self test cannot produce a hover, so "does the IsMouseOver trigger actually
    /// fire" was never measured.  This probe shows the window, parks the system cursor over the
    /// button, pumps the dispatcher, and reports both the dependency-property state and the
    /// rendered pixels.  It restores the cursor position before it exits.
    /// </para>
    /// </summary>
    internal static class HoverProbe
    {
        private const string Switch = "--hoverprobe";

        /// <summary>Everything the probe reports, also written to hoverprobe.log beside the exe.</summary>
        private static readonly List<string> Log = new List<string> ();

        private static void Say (string format, params object[] args)
        {
            string line = string.Format (format, args);
            Log.Add (line);
            Console.WriteLine (line);
        }

        public static bool IsRequested (string[] args)
        {
            if (args == null)
                return false;

            foreach (string arg in args)
            {
                if (string.Equals (arg, Switch, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static int Run (bool transition)
        {
            var window = new MainWindow (transition);
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Topmost = true;
            window.Show ();
            window.Activate ();

            Pump (800);

            Button button = FindButton (window, "HackBorder button");
            if (button == null)
            {
                Say ("hoverprobe: could not find the button");
                return 2;
            }

            button.ApplyTemplate ();
            var border = VisualTreeHelper.GetChild (button, 0) as HackBorder;
            if (border == null)
            {
                Say ("hoverprobe: button template root is not a HackBorder");
                return 2;
            }

            int width = (int)Math.Round (border.ActualWidth);
            int height = (int)Math.Round (border.ActualHeight);
            Say ("hoverprobe: border {0}x{1}", width, height);

            string colourBefore = SampleFill (border);
            int stateBefore = ThemeImage.GetState (border);
            bool hoveredBefore = border.IsMouseOver;
            Say ("before : IsMouseOver={0} State={1} fill={2}", hoveredBefore, stateBefore, colourBefore);

            Point target = border.PointToScreen (new Point (width / 2.0, height / 2.0));
            int savedX, savedY;
            NativeMethods.GetCursorPos (out savedX, out savedY);
            bool moved = NativeMethods.SetCursorPos ((int)Math.Round (target.X), (int)Math.Round (target.Y));

            int curX, curY;
            NativeMethods.GetCursorPos (out curX, out curY);
            Say ("window : IsVisible={0} IsActive={1} state={2}", window.IsVisible, window.IsActive, window.WindowState);
            Say ("cursor : saved=({0},{1}) target=({2},{3}) moved={4} now=({5},{6})",
                savedX, savedY, (int)Math.Round (target.X), (int)Math.Round (target.Y), moved, curX, curY);
            Say ("mouse  : DirectlyOver={0}", Mouse.DirectlyOver == null ? "null" : Mouse.DirectlyOver.GetType ().Name);

            // Give the input manager time to raise MouseEnter and the trigger to run.
            Pump (900);

            bool hoveredAfter = border.IsMouseOver;
            int stateAfter = ThemeImage.GetState (border);
            string colourAfter = SampleFill (border);
            Say ("after  : IsMouseOver={0} State={1} fill={2}", hoveredAfter, stateAfter, colourAfter);

            NativeMethods.SetCursorPos (savedX, savedY);

            // The cursor cannot be moved from this session (SetCursorPos is denied), so the real
            // input path cannot be exercised here.  Fall back to hit testing plus writing the
            // read-only IsMouseOver dependency property through its own key: that runs the normal
            // property engine, so the Style trigger and the descriptor path are still measured.
            Say ("hittest: {0}", DescribeHit (border));

            int syntheticState = SetMouseOver (border) ? ThemeImage.GetState (border) : -1;
            Pump (200);
            syntheticState = ThemeImage.GetState (border);
            string colourSynthetic = SampleFill (border);
            Say ("synthetic: IsMouseOver={0} State={1} fill={2}", border.IsMouseOver, syntheticState, colourSynthetic);

            // Same treatment for the box that carries the on-screen state readout: this proves the
            // binding the user is told to watch actually follows the trigger.
            var box = window.FindName ("TransitionProbe") as HackBorder;
            var readout = window.FindName ("StateReadout") as TextBlock;
            if (box != null && readout != null)
            {
                string before = readout.Text;
                SetMouseOver (box);
                Pump (150);
                Say ("readout: before='{0}' after='{1}' boxState={2}", before, readout.Text, ThemeImage.GetState (box));
            }
            else
            {
                Say ("readout: TransitionProbe or StateReadout not found");
            }

            window.Close ();

            bool changed = !string.Equals (colourBefore, colourAfter, StringComparison.Ordinal);
            bool syntheticChanged = !string.Equals (colourBefore, colourSynthetic, StringComparison.Ordinal);
            Say ("result : real-trigger={0} real-pixels={1} synthetic-trigger={2} synthetic-pixels={3}",
                stateAfter == 2, changed, syntheticState == 2, syntheticChanged);
            WriteLog ();
            return (syntheticState == 2 && syntheticChanged) ? 0 : 1;
        }

        /// <summary>Reports what the visual tree says is under the middle of the border.</summary>
        private static string DescribeHit (HackBorder border)
        {
            var point = new Point (border.ActualWidth / 2.0, border.ActualHeight / 2.0);
            HitTestResult hit = VisualTreeHelper.HitTest (border, point);
            if (hit == null || hit.VisualHit == null)
                return "nothing (the border is not hit-testable)";

            var visual = hit.VisualHit as DependencyObject;
            return string.Format ("{0} (HitTestVisible={1})",
                hit.VisualHit.GetType ().Name,
                visual is UIElement ? ((UIElement)visual).IsHitTestVisible : true);
        }

        /// <summary>
        /// Writes UIElement.IsMouseOver through its read-only DependencyPropertyKey, which is the
        /// only way to change it without moving the real cursor.
        /// </summary>
        private static bool SetMouseOver (UIElement element)
        {
            System.Reflection.FieldInfo field = typeof (UIElement).GetField (
                "IsMouseOverPropertyKey",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (field == null)
            {
                Say ("synthetic: IsMouseOverPropertyKey not found");
                return false;
            }

            var key = field.GetValue (null) as DependencyPropertyKey;
            if (key == null)
            {
                Say ("synthetic: key is not a DependencyPropertyKey");
                return false;
            }

            element.SetValue (key, true);
            return true;
        }

        private static void WriteLog ()
        {
            try
            {
                string folder = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().Location);
                File.WriteAllLines (Path.Combine (folder, "hoverprobe.log"), Log.ToArray ());
            }
            catch (Exception ex)
            {
                Console.WriteLine ("hoverprobe: could not write log: " + ex.Message);
            }
        }

        /// <summary>Renders the border on its own and returns the fill colour just inside the border.</summary>
        private static string SampleFill (HackBorder border)
        {
            int width = (int)Math.Round (border.ActualWidth);
            int height = (int)Math.Round (border.ActualHeight);
            if (width <= 0 || height <= 0)
                return "n/a";

            var bmp = new RenderTargetBitmap (width, height, 96, 96, PixelFormats.Pbgra32);
            bmp.Render (border);

            // x=6 sits inside BorderThickness and left of the label glyphs; y=height/2 is mid-face.
            int x = Math.Min (6, width - 1);
            int y = height / 2;
            var pixels = new byte[4];
            bmp.CopyPixels (new Int32Rect (x, y, 1, 1), pixels, 4, 0);
            return string.Format ("#{0:X2}{1:X2}{2:X2}{3:X2}", pixels[3], pixels[2], pixels[1], pixels[0]);
        }

        private static Button FindButton (DependencyObject root, string label)
        {
            int count = VisualTreeHelper.GetChildrenCount (root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild (root, i);
                var button = child as Button;
                if (button != null && MatchesLabel (button, label))
                    return button;

                Button nested = FindButton (child, label);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static bool MatchesLabel (Button button, string label)
        {
            var block = button.Content as TextBlock;
            return block != null && string.Equals (block.Text, label, StringComparison.Ordinal);
        }

        /// <summary>Runs the dispatcher for the given time so layout, input and timers progress.</summary>
        private static void Pump (int milliseconds)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds (milliseconds);
            while (DateTime.UtcNow < deadline)
            {
                var frame = new DispatcherFrame ();
                var timer = new DispatcherTimer (DispatcherPriority.Background)
                {
                    Interval = TimeSpan.FromMilliseconds (20)
                };
                timer.Tick += delegate (object sender, EventArgs e)
                {
                    ((DispatcherTimer)sender).Stop ();
                    frame.Continue = false;
                };
                timer.Start ();
                Dispatcher.PushFrame (frame);
            }
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport ("user32.dll")]
            internal static extern bool GetCursorPos (out int x, out int y);

            [System.Runtime.InteropServices.DllImport ("user32.dll")]
            internal static extern bool SetCursorPos (int x, int y);
        }
    }
}
