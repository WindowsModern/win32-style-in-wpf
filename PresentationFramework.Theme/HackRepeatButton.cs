using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace System.Windows.Controls
{
	/// <summary>
	/// <see cref="HackButton"/> 的重复按钮版本：连按行为、延迟等全部沿用 <see cref="RepeatButton"/>，
	/// 只有背景改成五张九宫格图。不依赖主题字典。
	/// </summary>
	public class HackRepeatButton: RepeatButton
	{
		private readonly HackButtonChrome _chrome;
		static HackRepeatButton () { DefaultStyleKeyProperty.OverrideMetadata (typeof (HackRepeatButton), new FrameworkPropertyMetadata (typeof (HackRepeatButton))); }
		public HackRepeatButton ()
		{
			// 阻断隐式 {x:Type RepeatButton} 样式，避免与主题模板叠加。
			Style = new Style (typeof (HackRepeatButton));
			_chrome = new HackButtonChrome (this, HackThickness);
			AddVisualChild (_chrome.Art);
			AddVisualChild (_chrome.Content);
			AddLogicalChild (_chrome.Art);
			AddLogicalChild (_chrome.Content);
			HackButtonChrome.WatchState (this, delegate { UpdateArt (); });
			IsEnabledChanged += delegate { UpdateArt (); };
			UpdateArt ();
		}
		protected override int VisualChildrenCount { get { return _chrome.VisualChildrenCount; } }
		protected override Visual GetVisualChild (int index) { return _chrome.GetVisualChild (index); }
		protected override Size MeasureOverride (Size availableSize) { return _chrome.Measure (availableSize, Padding); }
		protected override Size ArrangeOverride (Size finalSize) { return _chrome.Arrange (finalSize, Padding); }
		protected override void OnIsPressedChanged (DependencyPropertyChangedEventArgs e) { base.OnIsPressedChanged (e); UpdateArt (); }
		internal void UpdateArt () { if (_chrome != null) _chrome.SetSource (PickSource (), TransitionDuration, TransitionEasing); }

		/// <summary>按当前交互状态挑选背景图。</summary>
		protected virtual ImageSource PickSource ()
		{
			if (!IsEnabled) return DisabledSource ?? NormalSource;
			if (IsPressed) return ActiveSource ?? HoverSource ?? NormalSource;
			if (IsMouseOver) return HoverSource ?? NormalSource;
			if (IsKeyboardFocused) return FocusSource ?? HoverSource ?? NormalSource;
			return NormalSource;
		}

		private static void OnArtChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { ((HackRepeatButton)d).UpdateArt (); }
		public static readonly DependencyProperty NormalSourceProperty = DependencyProperty.Register (nameof (NormalSource), typeof (ImageSource), typeof (HackRepeatButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>常态背景图。</summary>
		public ImageSource NormalSource { get { return (ImageSource)GetValue (NormalSourceProperty); } set { SetValue (NormalSourceProperty, value); } }
		public static readonly DependencyProperty HoverSourceProperty = DependencyProperty.Register (nameof (HoverSource), typeof (ImageSource), typeof (HackRepeatButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>鼠标悬停时的背景图；为 null 时退回 <see cref="NormalSource"/>。</summary>
		public ImageSource HoverSource { get { return (ImageSource)GetValue (HoverSourceProperty); } set { SetValue (HoverSourceProperty, value); } }
		public static readonly DependencyProperty ActiveSourceProperty = DependencyProperty.Register (nameof (ActiveSource), typeof (ImageSource), typeof (HackRepeatButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>按下时的背景图；为 null 时退回 <see cref="HoverSource"/>，再退回 <see cref="NormalSource"/>。</summary>
		public ImageSource ActiveSource { get { return (ImageSource)GetValue (ActiveSourceProperty); } set { SetValue (ActiveSourceProperty, value); } }
		public static readonly DependencyProperty FocusSourceProperty = DependencyProperty.Register (nameof (FocusSource), typeof (ImageSource), typeof (HackRepeatButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>键盘焦点时的背景图；为 null 时退回 <see cref="HoverSource"/>，再退回 <see cref="NormalSource"/>。</summary>
		public ImageSource FocusSource { get { return (ImageSource)GetValue (FocusSourceProperty); } set { SetValue (FocusSourceProperty, value); } }
		public static readonly DependencyProperty DisabledSourceProperty = DependencyProperty.Register (nameof (DisabledSource), typeof (ImageSource), typeof (HackRepeatButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>禁用时的背景图；为 null 时退回 <see cref="NormalSource"/>。</summary>
		public ImageSource DisabledSource { get { return (ImageSource)GetValue (DisabledSourceProperty); } set { SetValue (DisabledSourceProperty, value); } }
		public static readonly DependencyProperty HackThicknessProperty = DependencyProperty.Register (nameof (HackThickness), typeof (Thickness), typeof (HackRepeatButton), new FrameworkPropertyMetadata (new Thickness (0), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnHackThicknessChanged));
		/// <summary>九宫格切片（Left, Top, Right, Bottom），单位为图像像素。与 <see cref="HackImage.HackThickness"/> 含义一致。</summary>
		public Thickness HackThickness { get { return (Thickness)GetValue (HackThicknessProperty); } set { SetValue (HackThicknessProperty, value); } }
		private static void OnHackThicknessChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { var c = (HackRepeatButton)d; if (c._chrome != null) c._chrome.SetHackThickness ((Thickness)e.NewValue); }

		/// <summary>状态切换时背景图的交叉淡入时长；默认 TimeSpan.Zero，即立即切换。</summary>
		public Duration TransitionDuration { get { return (Duration)GetValue (TransitionDurationProperty); } set { SetValue (TransitionDurationProperty, value); } }
		public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register (nameof (TransitionDuration), typeof (Duration), typeof (HackRepeatButton), new PropertyMetadata (new Duration (TimeSpan.Zero)));
		/// <summary>交叉淡入使用的缓动函数；为 null 时线性。</summary>
		public IEasingFunction TransitionEasing { get { return (IEasingFunction)GetValue (TransitionEasingProperty); } set { SetValue (TransitionEasingProperty, value); } }
		public static readonly DependencyProperty TransitionEasingProperty = DependencyProperty.Register (nameof (TransitionEasing), typeof (IEasingFunction), typeof (HackRepeatButton), new PropertyMetadata (null));
	}
}
