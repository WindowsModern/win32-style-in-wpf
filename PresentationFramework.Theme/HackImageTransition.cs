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
		private readonly HackImage _target;
		private readonly BitmapSource _from;
		private readonly BitmapSource _to;
		private readonly Thickness _hack;
		private readonly bool _isSourceShrink;
		private readonly TimeSpan _duration;
		private IEasingFunction _easing;
		private int _maxFps = 24;
		private byte [] [] _fromPixels;
		private byte [] [] _toPixels;
		private WriteableBitmap [] _mixed;
		private int [] _sliceW;
		private int [] _sliceH;
		private DateTime _startTime;
		private DateTime _lastFrame;
		private bool _running;
		private bool _disposed;
		private bool _isFallback;
		/// <summary>动画完成时触发（在 UI 线程，此时目标 Source 已切到 _to，可能为 null）。</summary>
		public event EventHandler Completed;
		/// <summary>目标控件（只读），供附加属性在 Completed 里读取以便清理状态。</summary>
		public HackImage Target { get { return _target; } }
		/// <summary>创建过渡动画；A、B 可为 null（null 视为透明），但不能同时为 null；非 null 的图必须已加载完成。</summary>
		public HackImageTransition (HackImage target, ImageSource from, ImageSource to, TimeSpan duration)
		{
			if (target == null) throw new ArgumentNullException (nameof (target));
			var a = from as BitmapSource;
			var b = to as BitmapSource;
			if (from != null && a == null) throw new ArgumentException ("From must be BitmapSource or null.", nameof (from));
			if (to != null && b == null) throw new ArgumentException ("To must be BitmapSource or null.", nameof (to));
			if (a == null && b == null) throw new ArgumentException ("From and To cannot both be null.");
			_target = target; _from = a; _to = b; _duration = duration;
			_hack = target.HackThickness; _isSourceShrink = target.IsSourceShrink;
		}
		/// <summary>缓动函数（null 表示线性）。</summary>
		public IEasingFunction Easing { get { return _easing; } set { _easing = value; } }
		/// <summary>最大帧率，默认 24；设置 ≤0 时忽略。</summary>
		public int MaxFrameRate { get { return _maxFps; } set { if (value > 0) _maxFps = value; } }
		/// <summary>开始动画：先预处理再按帧率驱动；重复调用安全。</summary>
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
		/// <summary>结束动画：切到 _to 并触发 <see cref="Completed"/>；重复调用安全。</summary>
		public void Stop ()
		{
			if (!_running) return;
			_running = false;
			CompositionTarget.Rendering -= OnRendering;
			_target.SetCurrentValue (HackImage.SourceProperty, _to);
			var h = Completed; if (h != null) h (this, EventArgs.Empty);
		}
		/// <summary>释放资源；可重复调用。</summary>
		public void Dispose ()
		{
			if (_disposed) return;
			Stop ();
			_disposed = true;
			_fromPixels = null; _toPixels = null;
			if (_mixed != null) { for (int i = 0; i < 9; i++) _mixed [i] = null; }
			_mixed = null;
		}
		/// <summary>获取当前混合帧用于打断过渡。</summary>
		public BitmapSource GetCurrentFrame ()
		{
			if (_mixed == null) return null;
			if (_isFallback)
			{
				return _mixed [4];
			}
			if (_sliceW == null) return null;
			int lw = _sliceW [0], tw = _sliceW [1], rw = _sliceW [2];
			int th = _sliceH [0], ch = _sliceH [4], bh = _sliceH [6];
			int w = lw + tw + rw, h = th + ch + bh;
			if (w <= 0 || h <= 0) return null;
			var visual = new DrawingVisual ();
			using (var dc = visual.RenderOpen ())
			{
				DrawSlice (dc, _mixed [0], 0, 0, lw, th);
				DrawSlice (dc, _mixed [1], lw, 0, tw, th);
				DrawSlice (dc, _mixed [2], lw + tw, 0, rw, th);
				DrawSlice (dc, _mixed [3], 0, th, lw, ch);
				DrawSlice (dc, _mixed [4], lw, th, tw, ch);
				DrawSlice (dc, _mixed [5], lw + tw, th, rw, ch);
				DrawSlice (dc, _mixed [6], 0, th + ch, lw, bh);
				DrawSlice (dc, _mixed [7], lw, th + ch, tw, bh);
				DrawSlice (dc, _mixed [8], lw + tw, th + ch, rw, bh);
			}
			var rtb = new RenderTargetBitmap (w, h, 96, 96, PixelFormats.Pbgra32);
			rtb.Render (visual);
			rtb.Freeze ();
			return rtb;
		}
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
		private void Preprocess ()
		{
			int l = HackImage.ToNonNegativeInt (_hack.Left), t = HackImage.ToNonNegativeInt (_hack.Top);
			int r = HackImage.ToNonNegativeInt (_hack.Right), b = HackImage.ToNonNegativeInt (_hack.Bottom);
			bool fromInvalid = _from != null && ((long)l + r > _from.PixelWidth || (long)t + b > _from.PixelHeight);
			bool toInvalid = _to != null && ((long)l + r > _to.PixelWidth || (long)t + b > _to.PixelHeight);
			if (fromInvalid || toInvalid)
			{
				if (!_isSourceShrink)
				{
					_target.SetCurrentValue (HackImage.SourceProperty, _to);
					_running = false;
					return;
				}
				_isFallback = true;
				int maxW = Math.Max (_from?.PixelWidth ?? 0, _to?.PixelWidth ?? 0);
				int maxH = Math.Max (_from?.PixelHeight ?? 0, _to?.PixelHeight ?? 0);
				if (maxW > 0 && maxH > 0)
				{
					_sliceW = new int [9]; _sliceH = new int [9];
					_fromPixels = new byte [9] []; _toPixels = new byte [9] [];
					_mixed = new WriteableBitmap [9];
					_sliceW [4] = maxW; _sliceH [4] = maxH;
					_fromPixels [4] = _from != null ? StretchToBgra32 (_from, new Int32Rect (0, 0, _from.PixelWidth, _from.PixelHeight), maxW, maxH) : new byte [maxW * maxH * 4];
					_toPixels [4] = _to != null ? StretchToBgra32 (_to, new Int32Rect (0, 0, _to.PixelWidth, _to.PixelHeight), maxW, maxH) : new byte [maxW * maxH * 4];
					_mixed [4] = new WriteableBitmap (maxW, maxH, 96, 96, PixelFormats.Bgra32, null);
				}
				return;
			}
			_isFallback = false;
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
		private static Int32Rect [] GetSlices (int w, int h, int l, int t, int r, int b)
		{
			int cw = w - l - r, ch = h - t - b;
			return new []
			{
				new Int32Rect(0, 0, l, t), new Int32Rect(l, 0, cw, t), new Int32Rect(l + cw, 0, r, t),
				new Int32Rect(0, t, l, ch), new Int32Rect(l, t, cw, ch), new Int32Rect(l + cw, t, r, ch),
				new Int32Rect(0, t + ch, l, b), new Int32Rect(l, t + ch, cw, b), new Int32Rect(l + cw, t + ch, r, b),
			};
		}
		private byte [] StretchToBgra32 (BitmapSource src, Int32Rect rect, int targetW, int targetH)
		{
			var pixels = new byte [targetW * targetH * 4];
			if (rect.Width <= 0 || rect.Height <= 0) return pixels;
			if (rect.X < 0 || rect.Y < 0 || (long)rect.X + rect.Width > src.PixelWidth || (long)rect.Y + rect.Height > src.PixelHeight) return pixels;
			var cropped = new CroppedBitmap (src, rect);
			BitmapSource stretched = (cropped.PixelWidth == targetW && cropped.PixelHeight == targetH)
				? (BitmapSource)cropped
				: new TransformedBitmap (cropped, new ScaleTransform ((double)targetW / cropped.PixelWidth, (double)targetH / cropped.PixelHeight));
			var formatted = stretched.Format == PixelFormats.Bgra32 ? stretched : new FormatConvertedBitmap (stretched, PixelFormats.Bgra32, null, 0);
			formatted.CopyPixels (pixels, targetW * 4, 0);
			return pixels;
		}
		private void RenderFrame (double t)
		{
			if (_isFallback)
			{
				if (_mixed != null && _mixed [4] != null) BlendAndUpload (4, t);
				_target.SetWholeImage (_mixed != null ? _mixed [4] : null);
			}
			else
			{
				for (int i = 0; i < 9; i++) if (_mixed [i] != null) BlendAndUpload (i, t);
				_target.SetSliceImages (_mixed);
			}
		}
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
		private static void DrawSlice (DrawingContext dc, BitmapSource bmp, double x, double y, double w, double h)
		{
			if (bmp == null || w <= 0 || h <= 0) return;
			dc.DrawImage (bmp, new Rect (x, y, w, h));
		}
	}
}