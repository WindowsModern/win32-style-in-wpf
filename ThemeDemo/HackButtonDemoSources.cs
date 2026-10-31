using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThemeDemo
{
    /// <summary>
    /// The five bitmaps the gallery hands to HackButton / HackRepeatButton / HackToggleButton.
    /// <para>
    /// They are the button art <c>WpfApplicationTest</c> uses — <c>PNGFILE1150_1.png</c> through
    /// <c>PNGFILE1154_1.png</c>, copied into <c>Images\</c> so this project builds on its own.
    /// One file per state, in the order below, each 109x24.
    /// </para>
    /// <para>
    /// A 109x24 source wants <c>HackThickness="3"</c>: the outermost 3 pixels are the border and
    /// the rounded corners, which the nine-grid must never stretch; everything inside is a flat
    /// gradient that may.  Stretch a button past 109 wide and the 3 pixel border stays 3 pixels.
    /// </para>
    /// </summary>
    public static class HackButtonDemoSources
    {
        public static readonly ImageSource Normal = Load ("PNGFILE1150_1.png");

        public static readonly ImageSource Hover = Load ("PNGFILE1151_1.png");

        public static readonly ImageSource Active = Load ("PNGFILE1152_1.png");

        public static readonly ImageSource Disabled = Load ("PNGFILE1153_1.png");

        public static readonly ImageSource Focus = Load ("PNGFILE1154_1.png");

        private static ImageSource Load (string file)
        {
            var image = new BitmapImage ();
            image.BeginInit ();
            image.UriSource = new Uri ("pack://application:,,,/Images/" + file, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit ();
            image.Freeze ();
            return image;
        }
    }
}
