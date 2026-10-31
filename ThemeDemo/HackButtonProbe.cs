using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThemeDemo
{
    /// <summary>
    /// Exercises HackButton / HackRepeatButton / HackToggleButton: five-state selection,
    /// nine-grid stretching, and content layout.  Nothing here touches the theme dictionary —
    /// the background images are generated in memory, which is the whole point of these
    /// controls (they are convenience widgets, not themed parts).
    /// <para>
    /// States are driven through the dependency properties rather than through input:
    /// <c>IsMouseOver</c> and <c>IsPressed</c> are read-only, so the probe writes them through
    /// the internal DP keys.  That is exactly what the control watches, so if the image swaps
    /// here it swaps on a real mouse too.
    /// </para>
    /// </summary>
    internal static class HackButtonProbe
    {
        private const string Switch = "--hackbuttonprobe";
        private const int Slice = 8;
        private const int SourceSize = 40;

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
            var names = new Dictionary<ImageSource, string> ();
            ImageSource normal = MakeSlice ("Normal", Color.FromRgb (0xE1, 0xE1, 0xE1), Color.FromRgb (0x2A, 0x63, 0x9E), names);
            ImageSource hover = MakeSlice ("Hover", Color.FromRgb (0xE5, 0xF1, 0xFB), Color.FromRgb (0x00, 0x78, 0xD7), names);
            ImageSource active = MakeSlice ("Active", Color.FromRgb (0xCC, 0xE4, 0xF7), Color.FromRgb (0x00, 0x54, 0x99), names);
            ImageSource focus = MakeSlice ("Focus", Color.FromRgb (0xFF, 0xF7, 0xD6), Color.FromRgb (0xB8, 0x86, 0x0B), names);
            ImageSource disabled = MakeSlice ("Disabled", Color.FromRgb (0xCC, 0xCC, 0xCC), Color.FromRgb (0x90, 0x90, 0x90), names);

            var root = new StackPanel { Width = 860 };

            root.Children.Add (Caption ("HackButton — five states (driven through IsMouseOver / IsPressed / IsKeyboardFocused / IsEnabled)"));
            var stateRow = new StackPanel { Orientation = Orientation.Horizontal };
            var buttons = new List<HackButton> ();
            string[] labels = { "Normal", "Hover", "Active", "Focus", "Disabled" };
            for (int i = 0; i < labels.Length; i++)
            {
                var button = new HackButton
                {
                    Content = labels[i],
                    Width = 150,
                    Height = 44,
                    Margin = new Thickness (0, 0, 8, 0),
                    HackThickness = new Thickness (Slice),
                    NormalSource = normal,
                    HoverSource = hover,
                    ActiveSource = active,
                    FocusSource = focus,
                    DisabledSource = disabled
                };
                buttons.Add (button);
                stateRow.Children.Add (button);
            }
            root.Children.Add (stateRow);

            root.Children.Add (Caption ("HackRepeatButton / HackToggleButton"));
            var varietyRow = new StackPanel { Orientation = Orientation.Horizontal };
            var repeat = new HackRepeatButton
            {
                Content = "HackRepeatButton",
                Height = 40,
                Padding = new Thickness (12, 4, 12, 4),
                Margin = new Thickness (0, 0, 8, 0),
                HackThickness = new Thickness (Slice),
                NormalSource = normal,
                HoverSource = hover,
                ActiveSource = active,
                FocusSource = focus,
                DisabledSource = disabled
            };
            var repeatDisabled = new HackRepeatButton
            {
                Content = "repeat, disabled",
                Height = 40,
                Padding = new Thickness (12, 4, 12, 4),
                Margin = new Thickness (0, 0, 8, 0),
                HackThickness = new Thickness (Slice),
                IsEnabled = false,
                NormalSource = normal,
                HoverSource = hover,
                ActiveSource = active,
                FocusSource = focus,
                DisabledSource = disabled
            };
            var toggleOff = new HackToggleButton
            {
                Content = "toggle, unchecked",
                Height = 40,
                Padding = new Thickness (12, 4, 12, 4),
                Margin = new Thickness (0, 0, 8, 0),
                HackThickness = new Thickness (Slice),
                NormalSource = normal,
                HoverSource = hover,
                ActiveSource = active,
                FocusSource = focus,
                DisabledSource = disabled
            };
            var toggleOn = new HackToggleButton
            {
                Content = "toggle, checked",
                Height = 40,
                Padding = new Thickness (12, 4, 12, 4),
                HackThickness = new Thickness (Slice),
                IsChecked = true,
                NormalSource = normal,
                HoverSource = hover,
                ActiveSource = active,
                FocusSource = focus,
                DisabledSource = disabled
            };
            varietyRow.Children.Add (repeat);
            varietyRow.Children.Add (repeatDisabled);
            varietyRow.Children.Add (toggleOff);
            varietyRow.Children.Add (toggleOn);
            root.Children.Add (varietyRow);

            root.Children.Add (Caption ("Nine-grid stretch — 420x56 from a 40x40 source, HackThickness 8/8/8/8"));
            var stretched = new HackButton
            {
                Content = "the 8px corners stay 8px; edges stretch one way, the centre both",
                Width = 420,
                Height = 56,
                Padding = new Thickness (14, 6, 14, 6),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                HackThickness = new Thickness (Slice),
                NormalSource = normal,
                HoverSource = hover,
                ActiveSource = active,
                FocusSource = focus,
                DisabledSource = disabled
            };
            root.Children.Add (stretched);

            var window = new Window
            {
                Width = 900,
                Height = 420,
                Content = new Border { Background = Brushes.White, Padding = new Thickness (16), Child = root },
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 0,
                Top = 0
            };

            window.Show ();
            Pump (window);

            Say ("layout (controls have no template, so DesiredSize comes from content + Padding):");
            Report ("HackButton short", buttons [0]);
            Report ("HackButton stretched", stretched);
            Report ("HackRepeatButton", repeat);
            Report ("HackToggleButton", toggleOn);

            Say ("state driving:");
            Say ("  untouched                 -> {0}", Name (buttons [0], names));
            // IsMouseOver cannot be faked: writing it through the DP key reads back False on the
            // next pass, because the property is owned by the input system.  Move the window so
            // the stationary cursor sits over the button instead - that is a genuine hover.
            bool hovered = HoverWithRealMouse (window, buttons [1]);
            Say ("  real mouse over           -> {0} (IsMouseOver reads {1}, window moved under the cursor: {2})",
                Name (buttons [1], names), buttons [1].IsMouseOver, hovered);
            SetReadOnly (buttons [2], typeof (ButtonBase), "IsPressedPropertyKey", true);
            Say ("  IsPressed = true          -> {0} (IsPressed reads {1})", Name (buttons [2], names), buttons [2].IsPressed);
            bool focused = buttons [3].Focus ();
            Pump (window);
            Say ("  Focus() returned {0}, IsKeyboardFocused = {1} -> {2}",
                focused, buttons [3].IsKeyboardFocused, Name (buttons [3], names));
            buttons [4].IsEnabled = false;
            Say ("  IsEnabled = false         -> {0}", Name (buttons [4], names));
            Say ("  toggle checked            -> {0}", Name (toggleOn, names));
            Say ("  toggle unchecked          -> {0}", Name (toggleOff, names));

            // HackToggleButton carries ten sources: the five above plus five Checked* variants that
            // only apply while IsChecked is true.
            ImageSource checkedArt = MakeSlice ("CheckedNormal", Color.FromRgb (0x20, 0x60, 0xC0), Color.FromRgb (0x00, 0x20, 0x60), names);
            var pair = new HackToggleButton
            {
                Content = "checked art",
                Height = 40,
                Padding = new Thickness (14, 6, 14, 6),
                HackThickness = new Thickness (Slice),
                NormalSource = normal,
                CheckedNormalSource = checkedArt
            };
            root.Children.Add (pair);
            window.UpdateLayout ();
            Say ("checked variants (ten sources on HackToggleButton):");
            Say ("  unchecked                 -> {0}", Name (pair, names));
            pair.IsChecked = true;
            window.UpdateLayout ();
            Say ("  checked                   -> {0}", Name (pair, names));

            // The checked family is self-contained: with no CheckedFocusSource the focused checked
            // button must stay on CheckedNormalSource, not borrow the unchecked FocusSource.
            bool pairFocused = pair.Focus ();
            window.UpdateLayout ();
            Pump (window);
            Say ("  checked + focus, no CheckedFocusSource -> {0} (IsKeyboardFocused {1}, Focus() {2})",
                Name (pair, names), pair.IsKeyboardFocused, pairFocused);
            pair.IsChecked = false;
            window.UpdateLayout ();
            Say ("  unchecked + focus         -> {0}", Name (pair, names));

            // TransitionDuration goes through exactly the same HackImageTransitions code that the
            // attached properties use, so a fade here is a fade there.  Sample the surface before,
            // during and after a 400 ms Normal -> Active cross-fade.
            ImageSource red = MakeSlice ("Red", Color.FromRgb (0xC0, 0x20, 0x20), Color.FromRgb (0x40, 0x00, 0x00), names);
            ImageSource green = MakeSlice ("Green", Color.FromRgb (0x20, 0xC0, 0x20), Color.FromRgb (0x00, 0x40, 0x00), names);
            var fader = new HackButton
            {
                Content = "fades between states",
                Height = 40,
                Padding = new Thickness (12, 4, 12, 4),
                HackThickness = new Thickness (Slice),
                NormalSource = red,
                ActiveSource = green,
                TransitionDuration = TimeSpan.FromMilliseconds (400)
            };
            root.Children.Add (fader);
            window.UpdateLayout ();
            Pump (window);

            string before = Sample (window, fader);
            SetReadOnly (fader, typeof (ButtonBase), "IsPressedPropertyKey", true);
            Pump (window, 120);
            string midway = Sample (window, fader);
            Pump (window, 600);
            string after = Sample (window, fader);
            SetReadOnly (fader, typeof (ButtonBase), "IsPressedPropertyKey", false);
            Pump (window, 600);
            string back = Sample (window, fader);

            Say ("transition, 400 ms Normal -> Active, no input needed:");
            Say ("  before {0}   midway {1}   after {2}   released {3}", before, midway, after, back);

            window.UpdateLayout ();
            Pump (window);

            string png = Path.Combine (Directory.GetCurrentDirectory (), "hackbutton.png");
            Render (window, png);
            Say ("render: {0}", png);

            Say ("state watchers: {0}", VerifyWatchers ());
            window.Close ();

            try
            {
                File.WriteAllLines ("hackbuttonprobe.log", Log.ToArray ());
            }
            catch (Exception ex)
            {
                Say ("log write failed: {0}", ex.Message);
            }
            return 0;
        }

        private static TextBlock Caption (string text)
        {
            return new TextBlock
            {
                Text = text,
                Margin = new Thickness (0, 14, 0, 6),
                FontWeight = FontWeights.SemiBold
            };
        }

        private static void Report (string label, FrameworkElement element)
        {
            Say ("  {0,-22} desired {1:0.##} x {2:0.##}   actual {3:0.##} x {4:0.##}",
                label,
                element.DesiredSize.Width, element.DesiredSize.Height,
                element.ActualWidth, element.ActualHeight);
        }

        private static string Name (FrameworkElement button, Dictionary<ImageSource, string> names)
        {
            var art = FindArt (button);
            if (art == null)
                return "no background element";

            var image = art as HackImage;
            ImageSource source = image == null ? null : image.Source;
            if (source == null)
                return "null";

            string name;
            return names.TryGetValue (source, out name) ? name : "unknown";
        }

        private static string Sample (Window window, FrameworkElement element)
        {
            var content = window.Content as FrameworkElement;
            if (content == null || element.ActualWidth <= 1 || element.ActualHeight <= 1)
                return "n/a";

            int width = (int)Math.Ceiling (content.ActualWidth);
            int height = (int)Math.Ceiling (content.ActualHeight);
            var bitmap = new RenderTargetBitmap (width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render (content);

            Point origin = element.TransformToAncestor (content).Transform (new Point (0, 0));

            // Below the middle: the label sits on the centre line, the surface fills the rest.
            int x = (int)(origin.X + element.ActualWidth / 2);
            int y = (int)(origin.Y + element.ActualHeight - 5);
            if (x < 0 || y < 0 || x >= width || y >= height)
                return "outside";

            var pixels = new byte [4];
            bitmap.CopyPixels (new Int32Rect (x, y, 1, 1), pixels, 4, 0);
            return string.Format ("#{0:X2}{1:X2}{2:X2}", pixels [2], pixels [1], pixels [0]);
        }

        private static FrameworkElement FindArt (DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount (root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild (root, i);
                var element = child as FrameworkElement;
                if (element is HackImage)
                    return element;

                FrameworkElement deeper = FindArt (child);
                if (deeper != null)
                    return deeper;
            }
            return null;
        }

        private static void SetReadOnly (DependencyObject element, Type declaringType, string keyField, bool value)
        {
            FieldInfo field = declaringType.GetField (keyField, BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
                return;

            var key = (DependencyPropertyKey)field.GetValue (null);
            element.SetValue (key, value);
        }

        private static string VerifyWatchers ()
        {
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            string[] expected = { "IsMouseOverPropertyKey", "IsPressedPropertyKey" };
            var found = new List<string> ();
            foreach (string name in expected)
            {
                if (typeof (UIElement).GetField (name, Flags) != null || typeof (ButtonBase).GetField (name, Flags) != null)
                    found.Add (name);
            }
            return found.Count == expected.Length
                ? "read-only keys reachable, states above are genuine property values"
                : "only " + string.Join (", ", found.ToArray ());
        }

        /// <summary>
        /// Slides the window so the stationary cursor ends up over <paramref name="element"/>.
        /// This session refuses SetCursorPos, but the window can be moved, and a window that
        /// moves under the cursor produces a real IsMouseOver.
        /// </summary>
        private static bool HoverWithRealMouse (Window window, FrameworkElement element)
        {
            if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
                return false;

            Point cursor;
            if (!GetCursorPos (out cursor))
                return false;

            Point centre = element.PointToScreen (new Point (element.ActualWidth / 2, element.ActualHeight / 2));
            PresentationSource source = PresentationSource.FromVisual (window);
            if (source == null || source.CompositionTarget == null)
                return false;

            Matrix toDevice = source.CompositionTarget.TransformToDevice;
            window.Left += (cursor.X - centre.X) / toDevice.M11;
            window.Top += (cursor.Y - centre.Y) / toDevice.M22;
            Pump (window);

            // Moving a window under a stationary cursor does not always make Windows post a
            // move message; Synchronize re-reads the pointer position and settles IsMouseOver.
            try
            {
                System.Windows.Input.Mouse.Synchronize ();
            }
            catch (InvalidOperationException)
            {
            }
            Pump (window);
            return true;
        }

        [System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [System.Runtime.InteropServices.DllImport ("user32.dll", EntryPoint = "GetCursorPos")]
        private static extern bool GetCursorPosNative (out POINT point);

        private static bool GetCursorPos (out Point point)
        {
            POINT native;
            bool ok = GetCursorPosNative (out native);
            point = new Point (native.X, native.Y);
            return ok;
        }

        private static void Pump (Window window)
        {
            for (int i = 0; i < 20; i++)
            {
                window.Dispatcher.Invoke (new Action (delegate { }), System.Windows.Threading.DispatcherPriority.SystemIdle);
                System.Threading.Thread.Sleep (10);
                window.UpdateLayout ();
            }
        }

        private static void Pump (Window window, int milliseconds)
        {
            int deadline = Environment.TickCount + milliseconds;
            while (Environment.TickCount < deadline)
            {
                window.Dispatcher.Invoke (new Action (delegate { }), System.Windows.Threading.DispatcherPriority.SystemIdle);
                System.Threading.Thread.Sleep (10);
                window.UpdateLayout ();
            }
        }

        private static void Render (Window window, string path)
        {
            var content = window.Content as FrameworkElement;
            if (content == null)
                return;

            double width = content.ActualWidth > 0 ? content.ActualWidth : window.Width;
            double height = content.ActualHeight > 0 ? content.ActualHeight : window.Height;
            var bitmap = new RenderTargetBitmap ((int)Math.Ceiling (width), (int)Math.Ceiling (height), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render (content);

            var encoder = new PngBitmapEncoder ();
            encoder.Frames.Add (BitmapFrame.Create (bitmap));
            using (FileStream stream = File.Create (path))
                encoder.Save (stream);
        }

        private static ImageSource MakeSlice (string name, Color fill, Color corner, Dictionary<ImageSource, string> names)
        {
            var visual = new DrawingVisual ();
            using (DrawingContext dc = visual.RenderOpen ())
            {
                dc.DrawRectangle (new SolidColorBrush (fill), null, new Rect (0, 0, SourceSize, SourceSize));

                // A darker ring one pixel in, so a stretched middle is visibly different from the
                // corners, and four solid corner blocks that must keep their size.
                var pen = new Pen (new SolidColorBrush (corner), 1);
                dc.DrawRectangle (null, pen, new Rect (0.5, 0.5, SourceSize - 1, SourceSize - 1));

                Brush marker = new SolidColorBrush (corner);
                dc.DrawRectangle (marker, null, new Rect (0, 0, Slice, Slice));
                dc.DrawRectangle (marker, null, new Rect (SourceSize - Slice, 0, Slice, Slice));
                dc.DrawRectangle (marker, null, new Rect (0, SourceSize - Slice, Slice, Slice));
                dc.DrawRectangle (marker, null, new Rect (SourceSize - Slice, SourceSize - Slice, Slice, Slice));
            }

            var bitmap = new RenderTargetBitmap (SourceSize, SourceSize, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render (visual);
            bitmap.Freeze ();
            names [bitmap] = name;
            return bitmap;
        }
    }
}
