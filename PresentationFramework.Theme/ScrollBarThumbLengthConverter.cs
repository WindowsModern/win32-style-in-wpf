using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Computes the proportional ScrollBar thumb length from the track length,
    /// viewport size and range.  WPF's Track normally does this, but explicit
    /// thumb sizing in the template can defeat it.
    /// </summary>
    public sealed class ScrollBarThumbLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4)
                return 8.0;

            double track, viewport, maximum, minimum;
            if (!TryNumber(values[0], out track) ||
                !TryNumber(values[1], out viewport) ||
                !TryNumber(values[2], out maximum) ||
                !TryNumber(values[3], out minimum))
                return 8.0;

            if (track <= 0 || viewport <= 0)
                return 8.0;

            double extent = maximum - minimum;
            if (extent <= 0)
                return Math.Max(8.0, track);

            double length = track * viewport / (extent + viewport);
            return Math.Max(8.0, Math.Min(track, length));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static bool TryNumber(object value, out double number)
        {
            if (value is double)
            {
                number = (double)value;
                return !double.IsNaN(number) && !double.IsInfinity(number);
            }
            return double.TryParse(value == null ? null : value.ToString(),
                NumberStyles.Float, CultureInfo.InvariantCulture, out number);
        }
    }
}