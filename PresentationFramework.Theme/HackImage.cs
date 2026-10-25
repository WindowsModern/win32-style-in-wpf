using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace System.Windows.Controls
{
	/// <summary>九宫格图片（Nine-Patch）控件：四角固定，边单向拉伸，中心双向拉伸。</summary>
	public class HackImage: FrameworkElement
	{
		// 九宫格切片在 _slices 中的下标：row * 3 + col
		private const int TopLeft = 0, Top = 1, TopRight = 2, Left = 3, Center = 4, Right = 5, BottomLeft = 6, Bottom = 7, BottomRight = 8;
		// 当前源图的 BitmapSource 快照（已尝试 Freeze），供 CreateSlice 裁剪；每次 UpdateSlices 开头刷新
		private BitmapSource _bitmapSource;
		// 九块切片，OnRender 直接 DrawImage；Source/HackThickness 变化时由 UpdateSlices 重建，动画期间由 SetSliceImages 注入
		private readonly BitmapSource [] _slices = new BitmapSource [9];
		// 仍在下载中的 BitmapImage；SubscribePendingBitmap 记录，Download* 回调后清空并触发重切片
		private BitmapImage _pendingBitmapImage;
		// 初始化像素对齐、布局舍入，并把默认 HighQuality 写入 RenderOptions
		public HackImage () { SnapsToDevicePixels = true; UseLayoutRounding = true; ApplyScalingMode (); }
		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register (nameof (Source), typeof (ImageSource), typeof (HackImage), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnSourceChanged));
		/// <summary>图像源。仅支持 <see cref="BitmapSource"/>（含 <see cref="BitmapImage"/>、<see cref="BitmapFrame"/>）。</summary>
		public ImageSource Source { get { return (ImageSource)GetValue (SourceProperty); } set { SetValue (SourceProperty, value); } }
		// Source DP 变更回调：解除旧 BitmapImage 的下载订阅、订阅新的，并重建切片
		private static void OnSourceChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { var c = (HackImage)d; c.UnsubscribePendingBitmap (); c.SubscribePendingBitmap (e.NewValue as BitmapImage); c.UpdateSlices (); }
		public static readonly DependencyProperty HackThicknessProperty = DependencyProperty.Register (nameof (HackThickness), typeof (Thickness), typeof (HackImage), new FrameworkPropertyMetadata (new Thickness (0), FrameworkPropertyMetadataOptions.AffectsRender, OnHackThicknessChanged));
		/// <summary>九宫格切片（Left, Top, Right, Bottom），单位为图像像素。</summary>
		public Thickness HackThickness { get { return (Thickness)GetValue (HackThicknessProperty); } set { SetValue (HackThicknessProperty, value); } }
		// HackThickness DP 变更回调：仅重建切片，AffectsRender 已负责请求重绘
		private static void OnHackThicknessChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { ((HackImage)d).UpdateSlices (); }
		public static readonly DependencyProperty CanSourceShrinkProperty = DependencyProperty.Register (nameof (CanSourceShrink), typeof (bool), typeof (HackImage), new FrameworkPropertyMetadata (true, FrameworkPropertyMetadataOptions.AffectsRender));
		/// <summary>当 HackThickness 无法放入目标尺寸时，是否把整张源图缩放绘制，而不是切割/清空。默认 true。</summary>
		public bool CanSourceShrink { get { return (bool)GetValue (CanSourceShrinkProperty); } set { SetValue (CanSourceShrinkProperty, value); } }
		public static readonly DependencyProperty ScalingModeProperty = DependencyProperty.Register (nameof (ScalingMode), typeof (BitmapScalingMode), typeof (HackImage), new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality, OnScalingModeChanged));
		/// <summary>位图缩放质量。</summary>
		public BitmapScalingMode ScalingMode { get { return (BitmapScalingMode)GetValue (ScalingModeProperty); } set { SetValue (ScalingModeProperty, value); } }
		// ScalingMode DP 变更回调：把新值重新写入 RenderOptions
		private static void OnScalingModeChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { ((HackImage)d).ApplyScalingMode (); }
		// 把 ScalingMode 写到控件自身：RenderOptions 是继承属性，OnRender 的 DrawImage 会自动采用；构造与 DP 变更时调用
		private void ApplyScalingMode () { RenderOptions.SetBitmapScalingMode (this, ScalingMode); }
		/// <summary>直接设置 9 片（TL, T, TR, L, C, R, BL, B, BR），用于动画等外部切片场景；随后调用 <see cref="UIElement.InvalidateVisual"/>。</summary>
		public void SetSliceImages (BitmapSource [] slices)
		{
			if (slices == null || slices.Length != 9) return;
			for (int i = 0; i < 9; i++) _slices [i] = slices [i];
			InvalidateVisual ();
		}
		// 测量：无界时用图像自然尺寸，有界时填满可用空间（与 Image 控件默认 Stretch=Fill 一致）；布局系统自动调用
		protected override Size MeasureOverride (Size availableSize)
		{
			if (_bitmapSource == null) return new Size (0, 0);
			double w = double.IsInfinity (availableSize.Width) ? _bitmapSource.Width : availableSize.Width;
			double h = double.IsInfinity (availableSize.Height) ? _bitmapSource.Height : availableSize.Height;
			return new Size (w, h);
		}
		// 渲染：先画透明矩形保留命中测试，再按 DPI 换算切片厚度并 DrawImage 九块；目标尺寸小于切片之和时按比例收缩以避免负尺寸
		protected override void OnRender (DrawingContext dc)
		{
			base.OnRender (dc);
			double targetW = RenderSize.Width, targetH = RenderSize.Height;
			if (targetW <= 0 || targetH <= 0) return;
			dc.DrawRectangle (Brushes.Transparent, null, new Rect (0, 0, targetW, targetH));
			if (_bitmapSource == null) return;
			if (IsAllSlicesNull ())
			{
				if (CanSourceShrink)
					dc.DrawImage (_bitmapSource, new Rect (0, 0, targetW, targetH));
				return;
			}
			double dpiScaleX = _bitmapSource.Width / _bitmapSource.PixelWidth;
			double dpiScaleY = _bitmapSource.Height / _bitmapSource.PixelHeight;
			double l = HackThickness.Left * dpiScaleX, t = HackThickness.Top * dpiScaleY;
			double r = HackThickness.Right * dpiScaleX, b = HackThickness.Bottom * dpiScaleY;
			// UxTheme tells us whether this part is a nine-patch via TMT_SIZINGMARGINS.
			// No margins means a plain bitmap: draw it as one image instead of
			// manufacturing nine identical slices.  The choice follows theme data,
			// not a hard-coded control list.
			if (l <= 0 && t <= 0 && r <= 0 && b <= 0)
			{
				var whole = _slices [Center] ?? _bitmapSource;
				if (whole != null)
					dc.DrawImage (whole, new Rect (0, 0, targetW, targetH));
				return;
			}
			if (l + r > targetW && l + r > 0) { double s = targetW / (l + r); l *= s; r *= s; }
			if (t + b > targetH && t + b > 0) { double s = targetH / (t + b); t *= s; b *= s; }
			double cw = targetW - l - r, ch = targetH - t - b;
			double [] xs = { 0, l, l + cw }, ws = { l, cw, r }, ys = { 0, t, t + ch }, hs = { t, ch, b };
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
		// OnRender 的早期退出判断：9 块全为 null 时无需绘制
		private bool IsAllSlicesNull () { for (int i = 0; i < 9; i++) if (_slices [i] != null) return false; return true; }
		// 根据 Source 与 HackThickness 重建 9 块 CroppedBitmap 并请求重绘；Source / HackThickness 变更、BitmapImage 下载完成时调用
		private void UpdateSlices ()
		{
			_bitmapSource = GetBitmapSource (Source);
			if (_bitmapSource == null || _bitmapSource.PixelWidth == 0 || _bitmapSource.PixelHeight == 0) { ClearSlices (); return; }
			int w = _bitmapSource.PixelWidth, h = _bitmapSource.PixelHeight;
			int l = ToNonNegativeInt (HackThickness.Left), t = ToNonNegativeInt (HackThickness.Top);
			int r = ToNonNegativeInt (HackThickness.Right), b = ToNonNegativeInt (HackThickness.Bottom);
			if ((long)l + r > w || (long)t + b > h) { ClearSlices (); return; }
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
		// 从 _bitmapSource 裁剪指定矩形并 Freeze；w/h <= 0 返回 null（该块在 OnRender 中跳过）；仅由 UpdateSlices 调用
		private CroppedBitmap CreateSlice (int x, int y, int w, int h)
		{
			if (_bitmapSource == null || w <= 0 || h <= 0) return null;
			var cropped = new CroppedBitmap (_bitmapSource, new Int32Rect (x, y, w, h));
			if (cropped.CanFreeze) cropped.Freeze ();
			return cropped;
		}
		// 清空 9 块并请求重绘；源为空、像素为 0 或切片越界时由 UpdateSlices 调用
		private void ClearSlices () { for (int i = 0; i < 9; i++) _slices [i] = null; InvalidateVisual (); }
		// Thickness 分量 → 非负 int 像素：过滤 NaN/Infinity/负值，并限制到 int.MaxValue 防溢出；切片换算时调用
		private static int ToNonNegativeInt (double v)
		{
			if (double.IsNaN (v) || double.IsInfinity (v) || v <= 0) return 0;
			var rounded = Math.Round (v);
			return rounded > int.MaxValue ? int.MaxValue : (int)rounded;
		}
		// ImageSource → BitmapSource，并尝试 Freeze 以便渲染时跨线程复用；非 BitmapSource 返回 null；UpdateSlices 开头调用
		private BitmapSource GetBitmapSource (ImageSource source)
		{
			var bs = source as BitmapSource;
			if (bs == null) return null;
			if (!bs.IsFrozen && bs.CanFreeze) { try { bs.Freeze (); } catch (InvalidOperationException) { } }
			return bs;
		}
		// 若传入的 BitmapImage 仍在异步下载，记录并订阅 DownloadCompleted/Failed；Source DP 变更时调用
		private void SubscribePendingBitmap (BitmapImage bitmap)
		{
			if (bitmap == null || !bitmap.IsDownloading) return;
			_pendingBitmapImage = bitmap;
			bitmap.DownloadCompleted += OnPendingBitmapDownloadCompleted;
			bitmap.DownloadFailed += OnPendingBitmapDownloadFailed;
		}
		// 解除 _pendingBitmapImage 的事件订阅并清空字段；Source DP 变更或下载结束（成功/失败）时调用
		private void UnsubscribePendingBitmap ()
		{
			if (_pendingBitmapImage == null) return;
			_pendingBitmapImage.DownloadCompleted -= OnPendingBitmapDownloadCompleted;
			_pendingBitmapImage.DownloadFailed -= OnPendingBitmapDownloadFailed;
			_pendingBitmapImage = null;
		}
		// BitmapImage 下载完成回调：先解除订阅，再切回 UI 线程重建切片（下载事件可能运行在后台线程）
		private void OnPendingBitmapDownloadCompleted (object sender, EventArgs e) { UnsubscribePendingBitmap (); if (Dispatcher.CheckAccess ()) UpdateSlices (); else Dispatcher.BeginInvoke (new Action (UpdateSlices)); }
		// BitmapImage 下载失败回调：处理方式同完成回调，让控件退化为空白/部分显示
		private void OnPendingBitmapDownloadFailed (object sender, ExceptionEventArgs e) { UnsubscribePendingBitmap (); if (Dispatcher.CheckAccess ()) UpdateSlices (); else Dispatcher.BeginInvoke (new Action (UpdateSlices)); }
	}
}
