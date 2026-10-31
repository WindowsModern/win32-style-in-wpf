using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ThemeDemo
{
    /// <summary>
    /// Measures the themed Button against a stock (Aero) Button and reports the numbers.
    /// <para>
    /// The question this answers: does a themed Button size to its content the way a stock
    /// Button does, or does it claim the space it is offered?  The background layer in the
    /// theme's button templates is a <c>pt:HackImage</c>, and <c>HackImage.MeasureOverride</c>
    /// returns <c>availableSize</c> whenever that is finite, so the answer is not obvious from
    /// reading the template and has to be measured.
    /// </para>
    /// <para>
    /// The window has to be shown: the theme part is only resolved once the control is loaded,
    /// and an unresolved <c>HackImage</c> measures to zero, which would hide the effect.
    /// </para>
    /// </summary>
    internal static class LayoutProbe
    {
        private const string Switch = "--layoutprobe";

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
            // An explicit, empty Style bypasses the implicit style the theme installs, so the
            // control falls back to the framework theme style (Aero) and gives us a reference.
            var aeroStyle = new Style (typeof (Button));

            var panel = new StackPanel { Width = 400 };
            var rows = new List<KeyValuePair<string, Button>> ();

            Action<string, string, Style> add = delegate (string label, string text, Style style)
            {
                var button = new Button { Content = text, HorizontalAlignment = HorizontalAlignment.Left };
                if (style != null)
                    button.Style = style;
                panel.Children.Add (button);
                rows.Add (new KeyValuePair<string, Button> (label, button));
            };

            add ("themed / short", "OK", null);
            add ("aero   / short", "OK", aeroStyle);
            add ("themed / long", "A much longer caption", null);
            add ("aero   / long", "A much longer caption", aeroStyle);

            var window = new Window
            {
                Width = 460,
                Height = 260,
                Content = new Border { Width = 400, Child = panel },
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = 0,
                Top = 0
            };

            window.Show ();
            window.UpdateLayout ();
            Panel pump = null;
            if (pump == null)
            {
                // Let the theme part arrive and re-run layout.
                for (int i = 0; i < 40; i++)
                {
                    window.Dispatcher.Invoke (new Action (delegate { }), System.Windows.Threading.DispatcherPriority.SystemIdle);
                    System.Threading.Thread.Sleep (10);
                    window.UpdateLayout ();
                }
            }

            Say ("window shown, panel offers 400 wide");
            foreach (var row in rows)
            {
                Say ("  {0} : desired {1:0.##} x {2:0.##}   actual {3:0.##} x {4:0.##}",
                    row.Key,
                    row.Value.DesiredSize.Width, row.Value.DesiredSize.Height,
                    row.Value.ActualWidth, row.Value.ActualHeight);
            }

            Say ("background layer inside the themed button:");
            foreach (var row in rows)
            {
                FrameworkElement art = FindArt (row.Value);
                if (art == null)
                {
                    Say ("  {0} : no art element in the template", row.Key);
                    continue;
                }

                // HackBorder measures its art with an infinite constraint and keeps the result,
                // so this number is what the container will ask for on the next measure pass.
                object natural = "n/a";
                DependencyObject walk = VisualTreeHelper.GetParent (art);
                while (walk != null)
                {
                    if (walk.GetType ().Name == "HackBorder")
                    {
                        var field = walk.GetType ().GetField ("_artNatural",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (field != null)
                            natural = field.GetValue (walk);
                        break;
                    }
                    walk = VisualTreeHelper.GetParent (walk);
                }

                Say ("  {0} : art {1} desired {2:0.##} x {3:0.##}   container artNatural {4}",
                    row.Key, art.GetType ().Name, art.DesiredSize.Width, art.DesiredSize.Height, natural);
            }

            // A resize invalidates measure; do that by hand so the second pass sees whatever the
            // art settled on.
            panel.InvalidateMeasure ();
            window.UpdateLayout ();
            System.Threading.Thread.Sleep (30);
            window.Dispatcher.Invoke (new Action (delegate { }), System.Windows.Threading.DispatcherPriority.SystemIdle);
            window.UpdateLayout ();

            Say ("after a forced re-measure:");
            foreach (var row in rows)
            {
                Say ("  {0} : desired {1:0.##} x {2:0.##}   actual {3:0.##} x {4:0.##}",
                    row.Key,
                    row.Value.DesiredSize.Width, row.Value.DesiredSize.Height,
                    row.Value.ActualWidth, row.Value.ActualHeight);
            }

            window.Close ();

            try
            {
                File.WriteAllLines ("layoutprobe.log", Log.ToArray ());
            }
            catch (Exception ex)
            {
                Say ("log write failed: {0}", ex.Message);
            }
            return 0;
        }

        private static FrameworkElement FindArt (DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount (root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild (root, i);
                var element = child as FrameworkElement;
                if (element != null && element.GetType ().Name == "HackImage")
                    return element;

                FrameworkElement deeper = FindArt (child);
                if (deeper != null)
                    return deeper;
            }
            return null;
        }
    }
}
