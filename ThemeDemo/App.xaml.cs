using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Windows.Themes;

namespace ThemeDemo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool selfTest = SelfTest.IsRequested(e.Args);
            bool hoverProbe = HoverProbe.IsRequested(e.Args);
            bool layoutProbe = LayoutProbe.IsRequested(e.Args);
            bool hackButtonProbe = HackButtonProbe.IsRequested(e.Args);
            bool borderProbe = BorderProbe.IsRequested(e.Args);
            ShutdownMode = (selfTest || hoverProbe || layoutProbe || hackButtonProbe || borderProbe) ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnLastWindowClose;

            bool transition = HasSwitch(e.Args, "--transition");

            // Load the theme the same way for both modes.  Loading the plain dictionary
            // statically in App.xaml made XP use a different first-use timing and could
            // pin UxTheme to the previous style for the whole process.
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(transition
                    ? "pack://application:,,,/PresentationFramework.Theme;component/theme.withtransition.xaml"
                    : "pack://application:,,,/PresentationFramework.Theme;component/theme.xaml")
            });

            if (selfTest)
            {
                Environment.Exit(SelfTest.Run(e.Args, transition));
                return;
            }

            if (hoverProbe)
            {
                Environment.Exit(HoverProbe.Run(transition));
                return;
            }

            if (layoutProbe)
            {
                Environment.Exit(LayoutProbe.Run(transition));
                return;
            }
            if (hackButtonProbe)
            {
                Environment.Exit(HackButtonProbe.Run(transition));
                return;
            }

            if (borderProbe)
            {
                Environment.Exit(BorderProbe.Run(transition));
                return;
            }

            var window = new MainWindow(transition);
            MainWindow = window;
            window.Show();

            // XP may serve the old visual style during the first render pass.
            // Re-render once after the window is fully up.
            var refresh = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            refresh.Tick += delegate
            {
                refresh.Stop();
                ThemeImageDiagnostics.RefreshAll();
            };
            refresh.Start();
        }

        private static bool HasSwitch(string[] args, string name)
        {
            if (args == null)
                return false;

            foreach (string arg in args)
            {
                if (string.Equals(arg, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
