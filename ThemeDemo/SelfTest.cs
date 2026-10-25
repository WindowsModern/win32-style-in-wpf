using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ThemeExplorer.Core;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Interop;
using Microsoft.Windows.Themes;

namespace ThemeDemo
{
    internal static class SelfTest
    {
        internal static bool IsRequested(string[] args)
        {
            if (args == null)
                return false;

            foreach (string arg in args)
            {
                if (string.Equals(arg, "--selftest", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        internal static int Run(string[] args, bool transition)
        {
            string outputDirectory = GetOption(args, "--out");
            if (string.IsNullOrEmpty(outputDirectory))
                outputDirectory = Path.Combine(Path.GetTempPath(), "ThemeDemo-SelfTest");
            Directory.CreateDirectory(outputDirectory);

            var log = new List<string>();
            int exitCode = 0;
            MainWindow window = null;

            try
            {
                log.Add("theme=" + (transition ? "withtransition" : "plain"));
                log.Add("theme-file=" + ThemeSystem.GetCurrentTheme().FileName);
                log.Add("theme-color=" + ThemeSystem.GetCurrentTheme().ColorScheme);
                log.Add("theme-size=" + ThemeSystem.GetCurrentTheme().SizeName);
                log.Add("app-themed=" + ThemeSystem.IsAppThemed);
                log.Add("theme-active=" + ThemeSystem.IsThemeActive);
                LogEnvironment(log);
                int chunkSize;
                int spaceSize;
                if (ThemeImageDiagnostics.GetProgressMetrics(out chunkSize, out spaceSize))
                    log.Add("progress-chunk=" + chunkSize + ";progress-space=" + spaceSize);
                else
                    log.Add("progress-chunk=unsupported");
                log.Add("images-created-after-merge=" + ThemeImageDiagnostics.CreatedImageCount);

                // Merely merging the ResourceDictionary must not create any image.
                if (ThemeImageDiagnostics.CreatedImageCount != 0)
                {
                    log.Add("ASSERT-FAIL: images were created before any control was loaded");
                    exitCode = 1;
                }

                window = new MainWindow(transition)
                {
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -30000,
                    Top = -30000
                };

                log.Add("images-created-before-show=" + ThemeImageDiagnostics.CreatedImageCount);
                window.Show();
                DoEvents();
                window.UpdateLayout();
                DoEvents();

                // Let the DispatcherPriority.Loaded retry and the first real layout finish;
                // otherwise the count is taken before the same images the screenshot shows.
                Pump(400);
                int createdAfterShow = ThemeImageDiagnostics.CreatedImageCount;
                log.Add("images-created-after-show=" + createdAfterShow);
                if (createdAfterShow <= 0)
                {
                    log.Add("ASSERT-FAIL: no theme image was created after showing the window");
                    exitCode = 1;
                }

                log.Add("images-cached-while-open=" + ThemeImageDiagnostics.CachedImageCount);
                LogWindowMetrics(window, log);
                foreach (string key in ThemeImageDiagnostics.GetCacheKeys())
                    log.Add("image-cache=" + key);
                foreach (string detail in ThemeImageDiagnostics.GetCacheDetails())
                    log.Add("image-detail=" + detail);

                if (HasArg(args, "--progress-live"))
                {
                    for (int second = 0; second <= 11; second++)
                    {
                        log.Add("live-progress-" + second + "=" + GetFirstProgressValue(window));
                        SaveScreenshot(window, Path.Combine(outputDirectory, "live-" + second + ".png"), log);
                        Pump(1000);
                    }
                }
                if (HasArg(args, "--progress-samples"))
                {
                    int[] values = { 0, 10, 50, 100 };
                    foreach (int value in values)
                    {
                        SetProgressValues(window, value);
                        window.UpdateLayout();
                        Pump(120);
                        SaveScreenshot(window, Path.Combine(outputDirectory, "progress-" + value + ".png"), log);
                    }
                    SetProgressValues(window, 0);
                    window.UpdateLayout();
                    Pump(120);
                }
                SaveWindowScreenshot(window, Path.Combine(outputDirectory, "window-layout.png"), log);
                SaveScreenshot(window, Path.Combine(outputDirectory,
                    transition ? "theme-withtransition.png" : "theme-plain.png"), log);

                CheckTransition(window, transition, log, ref exitCode);
                CheckPopupTemplates(window, log);

                window.Close();
                window = null;

                DoEvents();
                Collect();
                DoEvents();
                Collect();

                int cachedAfterClose = ThemeImageDiagnostics.CachedImageCount;
                log.Add("images-cached-after-close=" + cachedAfterClose);

                if (cachedAfterClose != 0)
                {
                    log.Add("ASSERT-FAIL: theme images are still cached after the window closed");
                    exitCode = 1;
                }
            }
            catch (Exception exception)
            {
                log.Add("ERROR: " + exception);
                exitCode = 1;
            }
            finally
            {
                if (window != null)
                {
                    try
                    {
                        window.Close();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            File.WriteAllLines(Path.Combine(outputDirectory, "selftest.log"), log.ToArray(),
                new UTF8Encoding(false));
            return exitCode;
        }

        private static void CheckPopupTemplates(Window window, List<string> log)
        {
            Button menuButton = FindContextMenuButton(window);
            if (menuButton != null && menuButton.ContextMenu != null)
            {
                menuButton.ContextMenu.PlacementTarget = menuButton;
                menuButton.ContextMenu.IsOpen = true;
                DoEvents();
                log.Add("contextmenu-open=" + menuButton.ContextMenu.IsOpen);
                menuButton.ContextMenu.IsOpen = false;
                DoEvents();
            }

            if (menuButton != null && menuButton.ToolTip is ToolTip)
            {
                var toolTip = (ToolTip)menuButton.ToolTip;
                toolTip.PlacementTarget = menuButton;
                toolTip.IsOpen = true;
                DoEvents();
                log.Add("tooltip-open=" + toolTip.IsOpen);
                toolTip.IsOpen = false;
                DoEvents();
            }

            ComboBox combo = FindVisual<ComboBox>(window);
            if (combo != null)
            {
                combo.IsDropDownOpen = true;
                DoEvents();
                log.Add("combobox-open=" + combo.IsDropDownOpen);
                combo.IsDropDownOpen = false;
                DoEvents();
            }
        }

        private static Button FindContextMenuButton(DependencyObject root)
        {
            var button = root as Button;
            if (button != null && button.ContextMenu != null)
                return button;

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                Button found = FindContextMenuButton(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void LogProgressMargins(List<string> log)
        {
            if (Application.Current == null || Application.Current.MainWindow == null)
                return;

            foreach (System.Windows.Controls.ProgressBar bar in FindVisualChildren<System.Windows.Controls.ProgressBar>(Application.Current.MainWindow))
            {
                FrameworkElement host = bar.Template == null ? null : bar.Template.FindName("Bar", bar) as FrameworkElement;
                if (host == null)
                    continue;

                FrameworkElement container = host.Parent as FrameworkElement;
                Thickness margin = ThemeImage.GetChunkMargin(host);
                log.Add("progress-chunk-margin=" + margin.Left.ToString("R", CultureInfo.InvariantCulture) + "," +
                    margin.Top.ToString("R", CultureInfo.InvariantCulture) + "," +
                    margin.Right.ToString("R", CultureInfo.InvariantCulture) + "," +
                    margin.Bottom.ToString("R", CultureInfo.InvariantCulture) +
                    ";container=" + (container == null ? "null" :
                        container.Margin.Left.ToString("R", CultureInfo.InvariantCulture) + "," +
                        container.Margin.Top.ToString("R", CultureInfo.InvariantCulture) + "," +
                        container.Margin.Right.ToString("R", CultureInfo.InvariantCulture) + "," +
                        container.Margin.Bottom.ToString("R", CultureInfo.InvariantCulture)));
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                T typed = child as T;
                if (typed != null)
                    yield return typed;
                foreach (T descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
        private static void LogEnvironment(List<string> log)
        {
            log.Add("os=" + Environment.OSVersion.VersionString);
            log.Add("os-64bit=" + Environment.Is64BitOperatingSystem);
            log.Add("process-64bit=" + Environment.Is64BitProcess);
            log.Add("clr=" + Environment.Version);
            log.Add("culture=" + CultureInfo.CurrentCulture.Name);
            log.Add("ui-culture=" + CultureInfo.CurrentUICulture.Name);
            log.Add("working-set=" + Environment.WorkingSet);
            log.Add("managed-bytes=" + GC.GetTotalMemory(false));

            using (Process process = Process.GetCurrentProcess())
            {
                log.Add("process-handles=" + process.HandleCount);
                log.Add("private-bytes=" + process.PrivateMemorySize64);
                log.Add("process-working-set=" + process.WorkingSet64);
            }
        }

        private static void LogWindowMetrics(Window window, List<string> log)
        {
            LogProgressMargins(log);
            log.Add("window-dip=" + window.ActualWidth.ToString("R", CultureInfo.InvariantCulture)
                + "x" + window.ActualHeight.ToString("R", CultureInfo.InvariantCulture));

            PresentationSource source = PresentationSource.FromVisual(window);
            if (source != null && source.CompositionTarget != null)
            {
                Matrix matrix = source.CompositionTarget.TransformToDevice;
                log.Add("dpi-x=" + matrix.M11.ToString("R", CultureInfo.InvariantCulture));
                log.Add("dpi-y=" + matrix.M22.ToString("R", CultureInfo.InvariantCulture));
                log.Add("window-pixel=" + (window.ActualWidth * matrix.M11).ToString("R", CultureInfo.InvariantCulture)
                    + "x" + (window.ActualHeight * matrix.M22).ToString("R", CultureInfo.InvariantCulture));
            }

            log.Add("window-offscreen-left=" + window.Left.ToString("R", CultureInfo.InvariantCulture));
            log.Add("window-offscreen-top=" + window.Top.ToString("R", CultureInfo.InvariantCulture));
        }

        private static bool HasArg(string[] args, string value)
        {
            if (args == null)
                return false;
            foreach (string arg in args)
                if (string.Equals(arg, value, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static int GetFirstProgressValue(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var bar = child as ProgressBar;
                if (bar != null && !bar.IsIndeterminate)
                    return (int)Math.Round(bar.Value);
                int value = GetFirstProgressValue(child);
                if (value >= 0)
                    return value;
            }
            return -1;
        }
        private static void SetProgressValues(DependencyObject parent, int value)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var bar = child as ProgressBar;
                if (bar != null && !bar.IsIndeterminate)
                    bar.Value = value;
                SetProgressValues(child, value);
            }
        }
        private static void CheckTransition(Window window, bool transition, List<string> log, ref int exitCode)
        {
            HackImage hack = FindHackImage(window, transition);
            if (hack == null)
            {
                log.Add("transition-check=skipped-no-hackimage");
                return;
            }

            ImageSource before = hack.Source;
            ThemeImage.SetState(hack, 2);          // hot state

            Pump(50);

            bool changedEarly = !ReferenceEquals(hack.Source, before);
            Pump(350);
            bool changedLate = !ReferenceEquals(hack.Source, before);

            log.Add("transition-check=early-changed:" + changedEarly + ";late-changed:" + changedLate);

            if (transition)
            {
                if (changedEarly || !changedLate)
                {
                    log.Add("ASSERT-FAIL: cross-fade transition did not behave as expected");
                    exitCode = 1;
                }
            }
            else if (!changedEarly)
            {
                log.Add("ASSERT-FAIL: plain theme did not switch the image immediately");
                exitCode = 1;
            }
        }

        private static HackImage FindHackImage(DependencyObject root, bool requireDuration)
        {
            var hack = root as HackImage;
            if (hack != null && hack.Source != null
                && (!requireDuration || ThemeImage.GetTransitionDuration(hack) > TimeSpan.Zero))
                return hack;

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                HackImage found = FindHackImage(VisualTreeHelper.GetChild(root, i), requireDuration);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void Pump(int milliseconds)
        {
            DateTime until = DateTime.UtcNow.AddMilliseconds(milliseconds);
            while (DateTime.UtcNow < until)
            {
                DoEvents();
                Thread.Sleep(10);
            }
        }

        private static void SaveWindowScreenshot(Window window, string path, List<string> log)
        {
            window.UpdateLayout();
            int width = Math.Max(1, (int)Math.Ceiling(window.ActualWidth));
            int height = Math.Max(1, (int)Math.Ceiling(window.ActualHeight));
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(path))
                encoder.Save(stream);
            log.Add("window-screenshot=" + path);
        }

        private static void SaveScreenshot(Window window, string path, List<string> log)
        {
            window.UpdateLayout();

            ScrollViewer scroll = FindVisual<ScrollViewer>(window);
            FrameworkElement content = scroll != null ? scroll.Content as FrameworkElement : null;
            if (content != null)
            {
                // Render the whole scroll content, not just the viewport, so the
                // screenshot shows every control in the gallery.
                content.Measure(new Size(content.ActualWidth, double.PositiveInfinity));
                content.Arrange(new Rect(0, 0, content.ActualWidth, content.DesiredSize.Height));
                content.UpdateLayout();
            }
            else
            {
                content = window;
            }

            int width = Math.Max(1, (int)Math.Ceiling(content.ActualWidth));
            int height = Math.Max(1, (int)Math.Ceiling(content.ActualHeight));

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(content);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = File.Create(path))
                encoder.Save(stream);

            log.Add("screenshot=" + path);
        }

        private static T FindVisual<T>(DependencyObject root) where T : DependencyObject
        {
            var match = root as T;
            if (match != null)
                return match;

            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                T found = FindVisual<T>(VisualTreeHelper.GetChild(root, i));
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void Collect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(delegate { frame.Continue = false; }));
            Dispatcher.PushFrame(frame);
        }

        private static string GetOption(string[] args, string name)
        {
            if (args == null)
                return null;

            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];

                string prefix = name + "=";
                if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return args[i].Substring(prefix.Length);
            }

            return null;
        }
    }
}
