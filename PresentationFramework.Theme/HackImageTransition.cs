using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
namespace System.Windows.Controls
{
	/// <summary>在两张图片之间通过 9 片逐像素混合实现过渡动画；from/to 可为 null（视为透明）；完成后通过 SetCurrentValue 更新目标 Source（保留绑定）。需在项目中启用 AllowUnsafeBlocks。</summary>
	public class HackImageTransition: IDisposable
	{
		// 目标控件；Start 期间其 Source 保持不变，混合结果通过 SetSliceImages 注入，Stop 时 SetCurrentValue 切到 _to
		private readonly HackImage _target;
		// 起始图 / 目标图（可为 null，null 视为透明图，Preprocess 用全 0 缓冲代替）
		private readonly BitmapSource _from;
		private readonly BitmapSource _to;
		// HackThickness 快照（以图像像素为单位），构造时从目标读取，from/to 必须共用
		private readonly Thickness _hack;
		// 动画总时长
		private readonly TimeSpan _duration;
		// 缓动函数，null 表示线性
		private IEasingFunction _easing;
		// 最大帧率，默认 24
		private int _maxFps = 24;
		// 每片 from/to 拉伸到共同尺寸后的 Bgra32 像素（Preprocess 填充，逐帧只读；null 侧为全 0）
		private byte [] [] _fromPixels;
		private byte [] [] _toPixels;
		// 每片输出的 WriteableBitmap（Preprocess 创建，逐帧 Lock 写 BackBuffer，Dispose 释放）
		private WriteableBitmap [] _mixed;
		// 每片的共同尺寸（两侧片宽高取 max；仅一侧为 null 时取非 null 侧尺寸）
		private int [] _sliceW;
		private int [] _sliceH;
		// 动画计时
		private DateTime _startTime;
		private DateTime _lastFrame;
		private bool _running;
		private bool _disposed;
		/// <summary>动画完成时触发（在 UI 线程，此时目标 Source 已切到 _to，可能为 null）。</summary>
		public event EventHandler Completed;
		/// <summary>目标控件（只读），供附加属性在 Completed 里读取以便清理状态。</summary>
		public HackImage Target { get { return _target; } }
		/// <summary>创建过渡动画；A、B 可为 null（null 视为透明），但不能同时为 null；非 null 的图必须已加载完成（PixelWidth &gt; 0）。需在项目中启用 AllowUnsafeBlocks。</summary>
		public HackImageTransition (HackImage target, ImageSource from, ImageSource to, TimeSpan duration)
		{
			if (target == null) throw new ArgumentNullException (nameof (target));
			var a = from as BitmapSource;
			var b = to as BitmapSource;
			if (from != null && a == null) throw new ArgumentException ("From must be BitmapSource or null.", nameof (from));
			if (to != null && b == null) throw new ArgumentException ("To must be BitmapSource or null.", nameof (to));
			if (a == null && b == null) throw new ArgumentException ("From and To cannot both be null.");
			_target = target; _from = a; _to = b; _duration = duration; _hack = target.HackThickness;
		}
		/// <summary>缓动函数（null 表示线性）。</summary>
		public IEasingFunction Easing { get { return _easing; } set { _easing = value; } }
		/// <summary>最大帧率，默认 24；设置 ≤0 时忽略。</summary>
		public int MaxFrameRate { get { return _maxFps; } set { if (value > 0) _maxFps = value; } }
		/// <summary>开始动画：先预处理（拉伸两侧的 9 片并缓存像素，null 侧为全 0），再按帧率驱动；重复调用安全。</summary>
		public void Start ()
		{
			if (_disposed || _running) return;
			if (_from != null && (_from.PixelWidth == 0 || _from.PixelHeight == 0)) throw new InvalidOperationException ("From source not fully loaded.");
			if (_to != null && (_to.PixelWidth == 0 || _to.PixelHeight == 0)) throw new InvalidOperationException ("To source not fully loaded.");
			Preprocess ();
			_startTime = DateTime.UtcNow; _lastFrame = DateTime.MinValue; _running = true;
			RenderFrame (0);
			CompositionTarget.Rendering += OnRendering;
		}
		/// <summary>结束动画：通过 SetCurrentValue 切到 _to（保留 Source 绑定）并触发 <see cref="Completed"/>；重复调用安全。</summary>
		public void Stop ()
		{
			if (!_running) return;
			_running = false;
			CompositionTarget.Rendering -= OnRendering;
			_target.SetCurrentValue (HackImage.SourceProperty, _to);
			var h = Completed; if (h != null) h (this, EventArgs.Empty);
		}
		/// <summary>释放资源（from/to 像素缓冲、9 个 WriteableBitmap）；可重复调用。</summary>
		public void Dispose ()
		{
			if (_disposed) return;
			Stop ();
			_disposed = true;
			_fromPixels = null; _toPixels = null;
			if (_mixed != null) { for (int i = 0; i < 9; i++) _mixed [i] = null; }
			_mixed = null;
		}
		/// <summary>把当前混合结果拼成整图（用于中途打断时作为新过渡的起点）；未 Start 或已 Dispose 返回 null。</summary>
		public BitmapSource GetCurrentFrame ()
		{
			if (_mixed == null || _sliceW == null) return null;
			int l = (int)Math.Round (_hack.Left), t = (int)Math.Round (_hack.Top);
			int r = (int)Math.Round (_hack.Right), b = (int)Math.Round (_hack.Bottom);
			int cw = _sliceW [4], ch = _sliceH [4];
			int w = l + cw + r, h = t + ch + b;
			if (w <= 0 || h <= 0) return null;
			var visual = new DrawingVisual ();
			using (var dc = visual.RenderOpen ())
			{
				DrawSlice (dc, _mixed [0], 0, 0, l, t);
				DrawSlice (dc, _mixed [1], l, 0, cw, t);
				DrawSlice (dc, _mixed [2], l + cw, 0, r, t);
				DrawSlice (dc, _mixed [3], 0, t, l, ch);
				DrawSlice (dc, _mixed [4], l, t, cw, ch);
				DrawSlice (dc, _mixed [5], l + cw, t, r, ch);
				DrawSlice (dc, _mixed [6], 0, t + ch, l, b);
				DrawSlice (dc, _mixed [7], l, t + ch, cw, b);
				DrawSlice (dc, _mixed [8], l + cw, t + ch, r, b);
			}
			var rtb = new RenderTargetBitmap (w, h, 96, 96, PixelFormats.Pbgra32);
			rtb.Render (visual);
			rtb.Freeze ();
			return rtb;
		}
		// 每帧回调：按 MaxFrameRate 限帧、算缓动进度；raw>=1 直接 Stop 跳过末帧混合与上传
		private void OnRendering (object sender, EventArgs e)
		{
			if (!_running) return;
			var now = DateTime.UtcNow;
			if (_lastFrame != DateTime.MinValue && (now - _lastFrame).TotalMilliseconds < 1000.0 / _maxFps) return;
			_lastFrame = now;
			double raw = Math.Min (1.0, (now - _startTime).TotalMilliseconds / _duration.TotalMilliseconds);
			if (raw >= 1.0) { Stop (); return; }
			double t = _easing != null ? _easing.Ease (raw) : raw;
			RenderFrame (t);
		}
		// 预处理：按 _hack 求两侧 9 片矩形；null 侧用全 0 缓冲，非 null 侧拉伸到共同尺寸（max）；创建 9 个 WriteableBitmap 作为输出
		private void Preprocess ()
		{
			int l = (int)Math.Round (_hack.Left), t = (int)Math.Round (_hack.Top);
			int r = (int)Math.Round (_hack.Right), b = (int)Math.Round (_hack.Bottom);
			Int32Rect [] aRects = _from != null ? GetSlices (_from.PixelWidth, _from.PixelHeight, l, t, r, b) : null;
			Int32Rect [] bRects = _to != null ? GetSlices (_to.PixelWidth, _to.PixelHeight, l, t, r, b) : null;
			_sliceW = new int [9]; _sliceH = new int [9];
			_fromPixels = new byte [9] []; _toPixels = new byte [9] [];
			_mixed = new WriteableBitmap [9];
			for (int i = 0; i < 9; i++)
			{
				int aw = aRects != null ? aRects [i].Width : 0, ah = aRects != null ? aRects [i].Height : 0;
				int bw = bRects != null ? bRects [i].Width : 0, bh = bRects != null ? bRects [i].Height : 0;
				int mw = Math.Max (aw, bw), mh = Math.Max (ah, bh);
				_sliceW [i] = mw; _sliceH [i] = mh;
				if (mw <= 0 || mh <= 0) continue;
				_fromPixels [i] = (aw > 0 && ah > 0) ? StretchToBgra32 (_from, aRects [i], mw, mh) : new byte [mw * mh * 4];
				_toPixels [i] = (bw > 0 && bh > 0) ? StretchToBgra32 (_to, bRects [i], mw, mh) : new byte [mw * mh * 4];
				_mixed [i] = new WriteableBitmap (mw, mh, 96, 96, PixelFormats.Bgra32, null);
			}
		}
		// 计算 9 片在 (w, h) 图中的矩形；顺序 TL, T, TR, L, C, R, BL, B, BR；仅由 Preprocess 调用
		private static Int32Rect [] GetSlices (int w, int h, int l, int t, int r, int b)
		{
			int cw = w - l - r, ch = h - t - b;
			return new []
			{
				new Int32Rect(0, 0, l, t),
				new Int32Rect(l, 0, cw, t),
				new Int32Rect(l + cw, 0, r, t),
				new Int32Rect(0, t, l, ch),
				new Int32Rect(l, t, cw, ch),
				new Int32Rect(l + cw, t, r, ch),
				new Int32Rect(0, t + ch, l, b),
				new Int32Rect(l, t + ch, cw, b),
				new Int32Rect(l + cw, t + ch, r, b),
			};
		}
		// 从 src 的 rect 裁剪，拉伸到 targetW x targetH（TransformedBitmap 双线性），统一转 Bgra32 后读成字节数组；仅由 Preprocess 调用
		private byte [] StretchToBgra32 (BitmapSource src, Int32Rect rect, int targetW, int targetH)
		{
			var pixels = new byte [targetW * targetH * 4];
			if (rect.Width <= 0 || rect.Height <= 0) return pixels;
			var cropped = new CroppedBitmap (src, rect);
			BitmapSource stretched = (cropped.PixelWidth == targetW && cropped.PixelHeight == targetH)
				? (BitmapSource)cropped
				: new TransformedBitmap (cropped, new ScaleTransform ((double)targetW / cropped.PixelWidth, (double)targetH / cropped.PixelHeight));
			var formatted = stretched.Format == PixelFormats.Bgra32 ? stretched : new FormatConvertedBitmap (stretched, PixelFormats.Bgra32, null, 0);
			formatted.CopyPixels (pixels, targetW * 4, 0);
			return pixels;
		}
		// 混合 9 片并交给目标渲染；每帧调用
		private void RenderFrame (double t)
		{
			for (int i = 0; i < 9; i++) if (_mixed [i] != null) BlendAndUpload (i, t);
			_target.SetSliceImages (_mixed);
		}
		// 混合第 index 片；t<=0 / t>=1 走 WritePixels 快路径，中间帧 Lock BackBuffer + 指针逐字节 lerp，AddDirtyRect 后 Unlock
		private unsafe void BlendAndUpload (int index, double t)
		{
			int w = _sliceW [index], h = _sliceH [index];
			if (w <= 0 || h <= 0) return;
			var a = _fromPixels [index];
			var b = _toPixels [index];
			var wb = _mixed [index];
			var rect = new Int32Rect (0, 0, w, h);
			if (t <= 0) { wb.WritePixels (rect, a, w * 4, 0); return; }
			if (t >= 1) { wb.WritePixels (rect, b, w * 4, 0); return; }
			int rowBytes = w * 4;
			int t256 = (int)(t * 256), it256 = 256 - t256;
			wb.Lock ();
			try
			{
				byte* pDst = (byte*)wb.BackBuffer;
				int stride = wb.BackBufferStride;
				fixed (byte* pA = a, pB = b)
				{
					for (int y = 0; y < h; y++)
					{
						byte* pa = pA + y * rowBytes;
						byte* pb = pB + y * rowBytes;
						byte* pd = pDst + y * stride;
						for (int x = 0; x < rowBytes; x++) pd [x] = (byte)((pa [x] * it256 + pb [x] * t256) >> 8);
					}
				}
				wb.AddDirtyRect (rect);
			}
			finally { wb.Unlock (); }
		}
		// 绘制单片的辅助：null 或 0 尺寸直接跳过，避免 DrawImage 抛异常；仅由 GetCurrentFrame 调用
		private static void DrawSlice (DrawingContext dc, BitmapSource bmp, double x, double y, double w, double h)
		{
			if (bmp == null || w <= 0 || h <= 0) return;
			dc.DrawImage (bmp, new Rect (x, y, w, h));
		}
	}
}