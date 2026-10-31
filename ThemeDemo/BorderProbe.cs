using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ThemeDemo
{
    /// <summary>
    /// Compares <c>HackBorder</c> against a structural clone of the reference project's
    /// <c>Slice9Image</c> (Sidebar Tiles / WindowsModern-Sidebar / Native / Slice9Image.xaml).
    /// <para>
    /// The reference is a UserControl over a Grid whose outer tracks are the slice thickness, with
    /// four corner Images (Stretch = Fill) and five Borders whose Background is an ImageBrush of the
    /// edge crop.  Cloning it here is the only way to state what "the same layout behaviour as
    /// Slice9Image" numerically means, without building that solution.
    /// </para>
    /// </summary>
    internal static class BorderProbe
    {
        private const string Switch = "--borderprobe";

        private static readonly List<string> Log = new List<string> ();

        private static readonly FieldInfo ArtField =
            typeof (HackBorder).GetField ("_art", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo NaturalField =
            typeof (HackBorder).GetField ("_artNatural", BindingFlags.NonPublic | BindingFlags.Instance);

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

        private static BitmapSource MakeArt (int width, int height)
        {
            var visual = new DrawingVisual ();
            using (DrawingContext dc = visual.RenderOpen ())
            {
                dc.DrawRectangle (new SolidColorBrush (Color.FromRgb (0xC0, 0xC0, 0xC0)), null,
                    new Rect (0, 0, width, height));
                dc.DrawRectangle (new SolidColorBrush (Color.FromRgb (0x20, 0x20, 0x20)), null,
                    new Rect (0, 0, width, 2));
                dc.DrawRectangle (new SolidColorBrush (Color.FromRgb (0x80, 0x40, 0x40)), null,
                    new Rect (0, 0, 2, height));
            }

            var bitmap = new RenderTargetBitmap (width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render (visual);
            bitmap.Freeze ();
            return bitmap;
        }

        private static HackImage GetArt (HackBorder border)
        {
            return (HackImage)ArtField.GetValue (border);
        }

        private static Size GetNatural (HackBorder border)
        {
            return (Size)NaturalField.GetValue (border);
        }

        private static string Format (Size size)
        {
            return string.Format ("{0} x {1}", size.Width, size.Height);
        }

        private static string Format (double value)
        {
            return double.IsInfinity (value) ? "inf" : value.ToString ("0.##");
        }

        private static Size MeasureFresh (HackBorder border, double width, double height)
        {
            border.InvalidateMeasure ();
            border.Measure (new Size (width, height));
            return border.DesiredSize;
        }

        private static HackBorder CreateBorder (ImageSource art, Thickness borderThickness, FrameworkElement child)
        {
            var border = new HackBorder ();
            border.UseLayoutRounding = true;
            border.HackThickness = new Thickness (3);
            border.BorderThickness = borderThickness;
            if (child != null)
                border.Child = child;

            if (art != null)
                GetArt (border).Source = art;

            return border;
        }

        // ---- reference clone -------------------------------------------------------------

        private sealed class Slice9Reference
        {
            public Grid Root;
            public Image TopLeft;
            public Image TopRight;
            public Image BottomLeft;
            public Image BottomRight;
            public Border TopEdge;
            public Border LeftEdge;
            public Border Center;
            public Border RightEdge;
            public Border BottomEdge;
        }

        private static CroppedBitmap Crop (BitmapSource source, int x, int y, int w, int h)
        {
            if (w <= 0 || h <= 0)
                return null;

            var crop = new CroppedBitmap (source, new Int32Rect (x, y, w, h));
            if (crop.CanFreeze)
                crop.Freeze ();
            return crop;
        }

        private static void SetBrush (Border border, BitmapSource crop)
        {
            if (border == null)
                return;

            if (crop == null)
            {
                border.Background = null;
                return;
            }

            var brush = new ImageBrush (crop);
            if (brush.CanFreeze)
                brush.Freeze ();
            border.Background = brush;
        }

        /// <summary>Structural clone of Slice9Image.xaml: same Grid tracks, same corner Images, same edge Borders.</summary>
        private static Slice9Reference CreateSlice9Reference (ImageSource source, Thickness slice, out FrameworkElement root)
        {
            BitmapSource bitmap = (BitmapSource)source;
            var reference = new Slice9Reference ();

            var grid = new Grid { SnapsToDevicePixels = true, UseLayoutRounding = true };
            grid.RowDefinitions.Add (new RowDefinition { Height = new GridLength (slice.Top) });
            grid.RowDefinitions.Add (new RowDefinition { Height = new GridLength (1.0, GridUnitType.Star) });
            grid.RowDefinitions.Add (new RowDefinition { Height = new GridLength (slice.Bottom) });
            grid.ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (slice.Left) });
            grid.ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (1.0, GridUnitType.Star) });
            grid.ColumnDefinitions.Add (new ColumnDefinition { Width = new GridLength (slice.Right) });

            Func<int, int, Image> addImage = delegate (int row, int column)
            {
                var image = new Image { Stretch = Stretch.Fill };
                Grid.SetRow (image, row);
                Grid.SetColumn (image, column);
                grid.Children.Add (image);
                return image;
            };

            Func<int, int, Border> addBorder = delegate (int row, int column)
            {
                var border = new Border ();
                Grid.SetRow (border, row);
                Grid.SetColumn (border, column);
                grid.Children.Add (border);
                return border;
            };

            reference.TopLeft = addImage (0, 0);
            reference.TopRight = addImage (0, 2);
            reference.BottomLeft = addImage (2, 0);
            reference.BottomRight = addImage (2, 2);
            reference.TopEdge = addBorder (0, 1);
            reference.LeftEdge = addBorder (1, 0);
            reference.Center = addBorder (1, 1);
            reference.RightEdge = addBorder (1, 2);
            reference.BottomEdge = addBorder (2, 1);

            // UpdateSlice(): identical crops and the same "give up" rule as the original.
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;
            int left = (int)Math.Round (slice.Left);
            int top = (int)Math.Round (slice.Top);
            int right = (int)Math.Round (slice.Right);
            int bottom = (int)Math.Round (slice.Bottom);

            if (left + right < width && top + bottom < height)
            {
                int centerWidth = width - left - right;
                int centerHeight = height - top - bottom;

                reference.TopLeft.Source = Crop (bitmap, 0, 0, left, top);
                reference.TopRight.Source = Crop (bitmap, left + centerWidth, 0, right, top);
                reference.BottomLeft.Source = Crop (bitmap, 0, top + centerHeight, left, bottom);
                reference.BottomRight.Source = Crop (bitmap, left + centerWidth, top + centerHeight, right, bottom);

                SetBrush (reference.TopEdge, Crop (bitmap, left, 0, centerWidth, top));
                SetBrush (reference.LeftEdge, Crop (bitmap, 0, top, left, centerHeight));
                SetBrush (reference.Center, Crop (bitmap, left, top, centerWidth, centerHeight));
                SetBrush (reference.RightEdge, Crop (bitmap, left + centerWidth, top, right, centerHeight));
                SetBrush (reference.BottomEdge, Crop (bitmap, left, top + centerHeight, centerWidth, bottom));
            }

            reference.Root = grid;

            // The real control is a UserControl over this Grid; the UserControl adds nothing to
            // layout beyond forwarding the constraint, so a ContentControl stands in for it.
            var host = new UserControl { Content = grid, Background = Brushes.Transparent, UseLayoutRounding = true };
            root = host;
            return reference;
        }

        private static Size MeasureReference (FrameworkElement reference, double width, double height)
        {
            reference.InvalidateMeasure ();
            reference.Measure (new Size (width, height));
            return reference.DesiredSize;
        }

        public static int Run (bool transition)
        {
            const double inf = double.PositiveInfinity;

            BitmapSource art = MakeArt (410, 42);
            Thickness slice = new Thickness (3);
            Say ("art 410 x 42, slice / HackThickness 3, no child, BorderThickness 0");
            Say ("");

            // 1. Does a source change reach layout at all?
            HackBorder lazy = CreateBorder (null, new Thickness (0), null);
            Say ("  before source   measure(inf, inf)  -> desired {0}, artNatural {1}",
                Format (MeasureFresh (lazy, inf, inf)), Format (GetNatural (lazy)));

            GetArt (lazy).Source = art;
            Say ("  after  source   no re-measure     -> desired {0}, artNatural {1}",
                Format (lazy.DesiredSize), Format (GetNatural (lazy)));

            Say ("  after  source   measure(inf, inf)  -> desired {0}, artNatural {1}",
                Format (MeasureFresh (lazy, inf, inf)), Format (GetNatural (lazy)));

            Say ("  => HackImage.UpdateSlices only calls InvalidateVisual, so a late-arriving part");
            Say ("     never refreshes the border's desired size on its own.");
            Say ("");

            // 2. Reference vs HackBorder under the same constraints.
            double[,] constraints = new double[,]
            {
                { inf, inf }, { 400, inf }, { inf, 300 }, { 200, 300 }, { 400, 300 }, { 120, 100 }, { 4, 4 }
            };
            string[] notes = new string[]
            {
                "both axes free", "width finite", "height finite", "both finite", "both finite, wide",
                "both finite, smaller than art", "smaller than the slices"
            };

            Say ("  reference = Slice9Image clone;  border = HackBorder with the same slice thickness");
            Say ("  constraint            Slice9Image        HackBorder         match");
            for (int i = 0; i < constraints.GetLength (0); i++)
            {
                double w = constraints [i, 0];
                double h = constraints [i, 1];

                FrameworkElement root;
                Slice9Reference reference = CreateSlice9Reference (art, slice, out root);
                Size referenceSize = MeasureReference (root, w, h);

                HackBorder border = CreateBorder (art, new Thickness (0), null);
                Size borderSize = MeasureFresh (border, w, h);

                bool match = Math.Abs (referenceSize.Width - borderSize.Width) < 0.01
                    && Math.Abs (referenceSize.Height - borderSize.Height) < 0.01;

                Say ("  ({0, -5} , {1, -5})  {2, -17}  {3, -17}  {4}   {5}",
                    Format (w), Format (h), Format (referenceSize), Format (borderSize), match, notes [i]);
            }

            Say ("");
            Say ("  => identical: neither control claims the bitmap (slices + border line is the floor),");
            Say ("     and neither clamps to the parent constraint.");
            Say ("");

            // 3. Arranged slice geometry: corners must stay at exactly the slice thickness.
            FrameworkElement arrangedRoot;
            Slice9Reference arrangedReference = CreateSlice9Reference (art, slice, out arrangedRoot);
            arrangedRoot.Measure (new Size (500, 120));
            arrangedRoot.Arrange (new Rect (0, 0, 500, 120));
            arrangedRoot.UpdateLayout ();

            Say ("arrange at 500 x 120");
            Say ("  Slice9Image  topLeft {0, -12} topRight {1, -12} leftEdge {2, -14} center {3, -12}",
                Format (arrangedReference.TopLeft.RenderSize), Format (arrangedReference.TopRight.RenderSize),
                Format (arrangedReference.LeftEdge.RenderSize), Format (arrangedReference.Center.RenderSize));

            HackBorder arranged = CreateBorder (art, new Thickness (0), null);
            arranged.Measure (new Size (500, 120));
            arranged.Arrange (new Rect (0, 0, 500, 120));
            HackImage inner = GetArt (arranged);
            Say ("  HackBorder   art fills {0} (its own 9-slice pass then keeps the corners at 3 px)",
                Format (inner.RenderSize));

            Say ("");
            Say ("  => corners stay at the slice thickness in both; edges and centre absorb the rest.");

            return 0;
        }
    }
}
