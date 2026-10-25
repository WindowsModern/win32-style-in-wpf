using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// One lazily rendered Win32 theme part: the frozen bitmap plus the nine-patch margins
    /// (in image pixels) that <see cref="HackImage"/> should use.
    /// </summary>
    internal sealed class ThemeImageData
    {
        internal ThemeImageData(BitmapSource image, Thickness margins, Thickness sizingMargins,
            Thickness contentMargins, bool hasContentMargins)
        {
            Image = image;
            Margins = margins;
            SizingMargins = sizingMargins;
            ContentMargins = contentMargins;
            HasContentMargins = hasContentMargins;
        }

        internal BitmapSource Image { get; }

        internal Thickness Margins { get; }

        internal Thickness SizingMargins { get; }

        internal Thickness ContentMargins { get; }

        internal bool HasContentMargins { get; }
    }
}
