using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace System.Windows.Controls
{
	/// <summary>九宫格图片（Nine-Patch）控件：四角固定，边单向拉伸，中心双向拉伸。</summary>
	public class HackImage: FrameworkElement
	{
		private const int TopLeft = 0, Top = 1, TopRight = 2, Left = 3, Center = 4, Right = 5, BottomLeft = 6, Bottom = 7, BottomRight = 8;
		private BitmapSource _bitmapSource;
		private readonly BitmapSource [] _slices = new BitmapSource [9];
		private BitmapSource _wholeImage;
		private BitmapImage _pendingBitmapImage;
		public HackImage () { SnapsToDevicePixels = true; UseLayoutRounding = true; ApplyScalingMode (); }
		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register (nameof (Source), typeof (ImageSource), typeof (HackImage), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnSourceChanged));
		/// <summary>图像源。仅支持 <see cref="BitmapSource"/>（含 <see cref="BitmapImage"/>、<see cref="BitmapFrame"/>）。</summary>
		public ImageSource Source { get { return (ImageSource)GetValue (SourceProperty); } set { SetValue (SourceProperty, value); } }
		private static void OnSourceChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { var c = (HackImage)d; c.UnsubscribePendingBitmap (); c.SubscribePendingBitmap (e.NewValue as BitmapImage); c.UpdateSlices (); }
		public static readonly DependencyProperty HackThicknessProperty = DependencyProperty.Register (nameof (HackThickness), typeof (Thickness), typeof (HackImage), new FrameworkPropertyMetadata (new Thickness (0), FrameworkPropertyMetadataOptions.AffectsRender, OnHackThicknessChanged));
		/// <summary>九宫格切片（Left, Top, Right, Bottom），单位为图像像素。</summary>
		public Thickness HackThickness { get { return (Thickness)GetValue (HackThicknessProperty); } set { SetValue (HackThicknessProperty, value); } }
		private static void OnHackThicknessChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { ((HackImage)d).UpdateSlices (); }
		public static readonly DependencyProperty IsSourceShrinkProperty = DependencyProperty.Register (nameof (IsSourceShrink), typeof (bool), typeof (HackImage), new FrameworkPropertyMetadata (false, FrameworkPropertyMetadataOptions.AffectsRender, OnIsSourceShrinkChanged));
		/// <summary>源图像素宽/高小于 HackThickness 左右/上下之和时，是否放弃九宫格并退化为整图缩放；false（默认）则整图清空。</summary>
		public bool IsSourceShrink { get { return (bool)GetValue (IsSourceShrinkProperty); } set { SetValue (IsSourceShrinkProperty, value); } }
		private static void OnIsSourceShrinkChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { ((HackImage)d).UpdateSlices (); }
		public static readonly DependencyProperty ScalingModeProperty = DependencyProperty.Register (nameof (ScalingMode), typeof (BitmapScalingMode), typeof (HackImage), new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality, OnScalingModeChanged));
		/// <summary>位图缩放质量。</summary>
		public BitmapScalingMode ScalingMode { get { return (BitmapScalingMode)GetValue (ScalingModeProperty); } set { SetValue (ScalingModeProperty, value); } }
		private static void OnScalingModeChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { ((HackImage)d).ApplyScalingMode (); }
		private void ApplyScalingMode () { RenderOptions.SetBitmapScalingMode (this, ScalingMode); }
		public static readonly DependencyProperty ThemeDescriptorSourceProperty = DependencyProperty.Register (nameof (ThemeDescriptorSource), typeof (DependencyObject), typeof (HackImage), new FrameworkPropertyMetadata (null));
		/// <summary>
		/// 描述符来源：不为 null 时，ThemeImage 的 Class/SubClass/Part/State 等附加属性从该对象读取，
		/// 而不是从这个元素读取。用于"容器承载描述符、本元素负责加载"的组合场景（如 HackBorder）。
		/// 为 null 时行为与之前完全一致。
		/// </summary>
		public DependencyObject ThemeDescriptorSource { get { return (DependencyObject)GetValue (ThemeDescriptorSourceProperty); } set { SetValue (ThemeDescriptorSourceProperty, value); } }
		/// <summary>直接设置 9 片（TL, T, TR, L, C, R, BL, B, BR），用于动画等外部切片场景。</summary>
		public void SetSliceImages (BitmapSource [] slices)
		{
			_wholeImage = null;
			if (slices == null || slices.Length != 9) return;
			for (int i = 0; i < 9; i++) _slices [i] = slices [i];
			InvalidateVisual ();
		}
		/// <summary>直接设置整图拉伸渲染（用于尺寸不足时的退化动画）。</summary>
		internal void SetWholeImage (BitmapSource image)
		{
			_wholeImage = image;
			for (int i = 0; i < 9; i++) _slices [i] = null;
			InvalidateVisual ();
		}
		protected override Size MeasureOverride (Size availableSize)
		{
			if (_bitmapSource == null) return new Size (0, 0);
			double w = double.IsInfinity (availableSize.Width) ? _bitmapSource.Width : availableSize.Width;
			double h = double.IsInfinity (availableSize.Height) ? _bitmapSource.Height : availableSize.Height;
			return new Size (w, h);
		}
		protected override void OnRender (DrawingContext dc)
		{
			base.OnRender (dc);
			double targetW = RenderSize.Width, targetH = RenderSize.Height;
			if (targetW <= 0 || targetH <= 0) return;
			dc.DrawRectangle (Brushes.Transparent, null, new Rect (0, 0, targetW, targetH));
			if (_wholeImage != null)
			{
				dc.DrawImage (_wholeImage, new Rect (0, 0, targetW, targetH));
				return;
			}
			if (_bitmapSource == null || IsAllSlicesNull ()) return;

			double dpiScaleX = _bitmapSource.Width / _bitmapSource.PixelWidth;
			double dpiScaleY = _bitmapSource.Height / _bitmapSource.PixelHeight;
			double l = HackThickness.Left * dpiScaleX, t = HackThickness.Top * dpiScaleY;
			double r = HackThickness.Right * dpiScaleX, b = HackThickness.Bottom * dpiScaleY;

			// 控件尺寸小于设定厚度时等比例压缩
			if (l + r > targetW && l + r > 0) { double s = targetW / (l + r); l *= s; r *= s; }
			if (t + b > targetH && t + b > 0) { double s = targetH / (t + b); t *= s; b *= s; }
			double cw = targetW - l - r, ch = targetH - t - b;

			// 获取当前 DPI 缩放比例 (兼容 .NET 4.0)
			double dpiX = 1.0, dpiY = 1.0;
			var source = PresentationSource.FromVisual (this);
			if (source != null && source.CompositionTarget != null)
			{
				dpiX = source.CompositionTarget.TransformToDevice.M11;
				dpiY = source.CompositionTarget.TransformToDevice.M22;
			}

			// 将坐标吸附到物理设备像素，防止抗锯齿导致切片间漏白
			Func<double, double> snapX = x => Math.Round (x * dpiX) / dpiX;
			Func<double, double> snapY = y => Math.Round (y * dpiY) / dpiY;

			double x1 = snapX (l);
			double x2 = snapX (l + cw);
			double y1 = snapY (t);
			double y2 = snapY (t + ch);

			// 计算精确的 Rect 边界，保证 xs[i] + ws[i] == xs[i+1]
			double [] xs = { 0, x1, x2, targetW };
			double [] ws = { xs [1] - xs [0], xs [2] - xs [1], xs [3] - xs [2] };
			double [] ys = { 0, y1, y2, targetH };
			double [] hs = { ys [1] - ys [0], ys [2] - ys [1], ys [3] - ys [2] };

			for (int row = 0; row < 3; row++)
			{
				for (int col = 0; col < 3; col++)
				{
					var bmp = _slices [row * 3 + col];
					if (bmp == null) continue;
					double w = ws [col], h = hs [row];
					if (w <= 0 || h <= 0) continue;
					dc.DrawImage (bmp, new Rect (xs [col], ys [row], w, h));
				}
			}
		}
		private bool IsAllSlicesNull () { for (int i = 0; i < 9; i++) if (_slices [i] != null) return false; return true; }
		private void UpdateSlices ()
		{
			_bitmapSource = GetBitmapSource (Source);
			if (_bitmapSource == null || _bitmapSource.PixelWidth == 0 || _bitmapSource.PixelHeight == 0) { ClearSlices (); return; }
			int w = _bitmapSource.PixelWidth, h = _bitmapSource.PixelHeight;
			int l = ToNonNegativeInt (HackThickness.Left), t = ToNonNegativeInt (HackThickness.Top);
			int r = ToNonNegativeInt (HackThickness.Right), b = ToNonNegativeInt (HackThickness.Bottom);
			if ((long)l + r > w || (long)t + b > h)
			{
				if (!IsSourceShrink) { ClearSlices (); return; }
				_wholeImage = _bitmapSource;
				for (int i = 0; i < 9; i++) _slices [i] = null;
				InvalidateVisual ();
				return;
			}
			_wholeImage = null;
			int cw = w - l - r, ch = h - t - b;
			_slices [TopLeft] = CreateSlice (0, 0, l, t);
			_slices [Top] = CreateSlice (l, 0, cw, t);
			_slices [TopRight] = CreateSlice (l + cw, 0, r, t);
			_slices [Left] = CreateSlice (0, t, l, ch);
			_slices [Center] = CreateSlice (l, t, cw, ch);
			_slices [Right] = CreateSlice (l + cw, t, r, ch);
			_slices [BottomLeft] = CreateSlice (0, t + ch, l, b);
			_slices [Bottom] = CreateSlice (l, t + ch, cw, b);
			_slices [BottomRight] = CreateSlice (l + cw, t + ch, r, b);
			InvalidateVisual ();
		}
		private CroppedBitmap CreateSlice (int x, int y, int w, int h)
		{
			if (_bitmapSource == null || w <= 0 || h <= 0) return null;
			var cropped = new CroppedBitmap (_bitmapSource, new Int32Rect (x, y, w, h));
			if (cropped.CanFreeze) cropped.Freeze ();
			return cropped;
		}
		private void ClearSlices () { _wholeImage = null; for (int i = 0; i < 9; i++) _slices [i] = null; InvalidateVisual (); }
		internal static int ToNonNegativeInt (double v)
		{
			if (double.IsNaN (v) || double.IsInfinity (v) || v <= 0) return 0;
			var rounded = Math.Round (v);
			return rounded > int.MaxValue ? int.MaxValue : (int)rounded;
		}
		private BitmapSource GetBitmapSource (ImageSource source)
		{
			var bs = source as BitmapSource;
			if (bs == null) return null;
			if (!bs.IsFrozen && bs.CanFreeze) { try { bs.Freeze (); } catch (InvalidOperationException) { } }
			return bs;
		}
		private void SubscribePendingBitmap (BitmapImage bitmap)
		{
			if (bitmap == null || !bitmap.IsDownloading) return;
			_pendingBitmapImage = bitmap;
			bitmap.DownloadCompleted += OnPendingBitmapDownloadCompleted;
			bitmap.DownloadFailed += OnPendingBitmapDownloadFailed;
		}
		private void UnsubscribePendingBitmap ()
		{
			if (_pendingBitmapImage == null) return;
			_pendingBitmapImage.DownloadCompleted -= OnPendingBitmapDownloadCompleted;
			_pendingBitmapImage.DownloadFailed -= OnPendingBitmapDownloadFailed;
			_pendingBitmapImage = null;
		}
		private void OnPendingBitmapDownloadCompleted (object sender, EventArgs e) { UnsubscribePendingBitmap (); if (Dispatcher.CheckAccess ()) UpdateSlices (); else Dispatcher.BeginInvoke (new Action (UpdateSlices)); }
		private void OnPendingBitmapDownloadFailed (object sender, ExceptionEventArgs e) { UnsubscribePendingBitmap (); if (Dispatcher.CheckAccess ()) UpdateSlices (); else Dispatcher.BeginInvoke (new Action (UpdateSlices)); }
	}
}