using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace System.Windows.Controls
{
	/// <summary>
	/// <see cref="HackButton"/> 的切换按钮版本：勾选状态沿用 <see cref="ToggleButton"/> 的 IsChecked，
	/// 只有背景改成五张九宫格图。勾选后（且未悬停/按下/禁用）用 <see cref="ActiveSource"/> 作为静止态，
	/// 未勾选用 <see cref="NormalSource"/>。不依赖主题字典。
	/// </summary>
	public class HackToggleButton: ToggleButton
	{
		private readonly HackButtonChrome _chrome;
		static HackToggleButton () { DefaultStyleKeyProperty.OverrideMetadata (typeof (HackToggleButton), new FrameworkPropertyMetadata (typeof (HackToggleButton))); }
		public HackToggleButton ()
		{
			// 阻断隐式 {x:Type ToggleButton} 样式，避免与主题模板叠加。
			Style = new Style (typeof (HackToggleButton));
			_chrome = new HackButtonChrome (this, HackThickness);
			AddVisualChild (_chrome.Art);
			AddVisualChild (_chrome.Content);
			AddLogicalChild (_chrome.Art);
			AddLogicalChild (_chrome.Content);
			HackButtonChrome.WatchState (this, delegate { UpdateArt (); });
			IsEnabledChanged += delegate { UpdateArt (); };
			Checked += delegate { UpdateArt (); };
			Unchecked += delegate { UpdateArt (); };
			Indeterminate += delegate { UpdateArt (); };
			UpdateArt ();
		}
		protected override int VisualChildrenCount { get { return _chrome.VisualChildrenCount; } }
		protected override Visual GetVisualChild (int index) { return _chrome.GetVisualChild (index); }
		protected override Size MeasureOverride (Size availableSize) { return _chrome.Measure (availableSize, Padding); }
		protected override Size ArrangeOverride (Size finalSize) { return _chrome.Arrange (finalSize, Padding); }
		protected override void OnIsPressedChanged (DependencyPropertyChangedEventArgs e) { base.OnIsPressedChanged (e); UpdateArt (); }
		internal void UpdateArt () { if (_chrome != null) _chrome.SetSource (PickSource (), TransitionDuration, TransitionEasing); }

		/// <summary>
		/// 按当前交互状态挑选背景图：禁用 &gt; 按下 &gt; 悬停 &gt; 焦点 &gt; 常态。
		/// <para>
		/// 勾选态自成一套：只要给了任意一张 Checked*，勾选时缺的那一张就退回
		/// <see cref="CheckedNormalSource"/>（再退回 <see cref="ActiveSource"/>、<see cref="NormalSource"/>），
		/// <b>不会</b>掉进未勾选那套图里 —— 否则勾选态会显示未勾选的悬停/焦点外观。
		/// 一张 Checked* 都不给时，沿用"勾选看起来像按下"的旧行为。
		/// </para>
		/// </summary>
		protected virtual ImageSource PickSource ()
		{
			bool on = IsChecked == true;

			if (on && HasCheckedArt)
			{
				ImageSource baseline = CheckedNormalSource ?? ActiveSource ?? NormalSource;
				if (!IsEnabled) return CheckedDisabledSource ?? baseline;
				if (IsPressed) return CheckedActiveSource ?? baseline;
				if (IsMouseOver) return CheckedHoverSource ?? baseline;
				if (IsKeyboardFocused) return CheckedFocusSource ?? baseline;
				return baseline;
			}

			ImageSource resting = on ? (ActiveSource ?? NormalSource) : NormalSource;
			if (!IsEnabled) return DisabledSource ?? resting;
			if (IsPressed) return ActiveSource ?? HoverSource ?? resting;
			if (IsMouseOver) return HoverSource ?? resting;
			if (IsKeyboardFocused) return FocusSource ?? HoverSource ?? resting;
			return resting;
		}

		/// <summary>是否至少提供了一张 Checked* 图；决定勾选态用独立的一套还是沿用未勾选那套。</summary>
		private bool HasCheckedArt
		{
			get
			{
				return CheckedNormalSource != null
					|| CheckedHoverSource != null
					|| CheckedActiveSource != null
					|| CheckedFocusSource != null
					|| CheckedDisabledSource != null;
			}
		}

		private static void OnArtChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { ((HackToggleButton)d).UpdateArt (); }
		public static readonly DependencyProperty NormalSourceProperty = DependencyProperty.Register (nameof (NormalSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>未勾选、无交互时的背景图。</summary>
		public ImageSource NormalSource { get { return (ImageSource)GetValue (NormalSourceProperty); } set { SetValue (NormalSourceProperty, value); } }
		public static readonly DependencyProperty HoverSourceProperty = DependencyProperty.Register (nameof (HoverSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>鼠标悬停时的背景图；为 null 时退回当前静止态。</summary>
		public ImageSource HoverSource { get { return (ImageSource)GetValue (HoverSourceProperty); } set { SetValue (HoverSourceProperty, value); } }
		public static readonly DependencyProperty ActiveSourceProperty = DependencyProperty.Register (nameof (ActiveSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>按下时的背景图，同时用作勾选后的静止态；为 null 时退回 <see cref="HoverSource"/>，再退回静止态。</summary>
		public ImageSource ActiveSource { get { return (ImageSource)GetValue (ActiveSourceProperty); } set { SetValue (ActiveSourceProperty, value); } }
		public static readonly DependencyProperty FocusSourceProperty = DependencyProperty.Register (nameof (FocusSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>键盘焦点时的背景图；为 null 时退回 <see cref="HoverSource"/>，再退回静止态。</summary>
		public ImageSource FocusSource { get { return (ImageSource)GetValue (FocusSourceProperty); } set { SetValue (FocusSourceProperty, value); } }
		public static readonly DependencyProperty DisabledSourceProperty = DependencyProperty.Register (nameof (DisabledSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>禁用时的背景图；为 null 时退回当前静止态。</summary>
		public ImageSource DisabledSource { get { return (ImageSource)GetValue (DisabledSourceProperty); } set { SetValue (DisabledSourceProperty, value); } }
		public static readonly DependencyProperty HackThicknessProperty = DependencyProperty.Register (nameof (HackThickness), typeof (Thickness), typeof (HackToggleButton), new FrameworkPropertyMetadata (new Thickness (0), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, OnHackThicknessChanged));
		/// <summary>九宫格切片（Left, Top, Right, Bottom），单位为图像像素。与 <see cref="HackImage.HackThickness"/> 含义一致。</summary>
		public Thickness HackThickness { get { return (Thickness)GetValue (HackThicknessProperty); } set { SetValue (HackThicknessProperty, value); } }
		private static void OnHackThicknessChanged (DependencyObject d, DependencyPropertyChangedEventArgs e) { var c = (HackToggleButton)d; if (c._chrome != null) c._chrome.SetHackThickness ((Thickness)e.NewValue); }

		/// <summary>状态切换时背景图的交叉淡入时长；默认 TimeSpan.Zero，即立即切换。</summary>
		public Duration TransitionDuration { get { return (Duration)GetValue (TransitionDurationProperty); } set { SetValue (TransitionDurationProperty, value); } }
		public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register (nameof (TransitionDuration), typeof (Duration), typeof (HackToggleButton), new PropertyMetadata (new Duration (TimeSpan.Zero)));
		/// <summary>交叉淡入使用的缓动函数；为 null 时线性。</summary>
		public IEasingFunction TransitionEasing { get { return (IEasingFunction)GetValue (TransitionEasingProperty); } set { SetValue (TransitionEasingProperty, value); } }
		public static readonly DependencyProperty TransitionEasingProperty = DependencyProperty.Register (nameof (TransitionEasing), typeof (IEasingFunction), typeof (HackToggleButton), new PropertyMetadata (null));

		/// <summary>勾选且无交互时的背景图；为 null 时退回 <see cref="ActiveSource"/>，再退回 <see cref="NormalSource"/>。</summary>
		public ImageSource CheckedNormalSource { get { return (ImageSource)GetValue (CheckedNormalSourceProperty); } set { SetValue (CheckedNormalSourceProperty, value); } }
		public static readonly DependencyProperty CheckedNormalSourceProperty = DependencyProperty.Register (nameof (CheckedNormalSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>勾选且悬停时的背景图；为 null 时退回 <see cref="HoverSource"/>。</summary>
		public ImageSource CheckedHoverSource { get { return (ImageSource)GetValue (CheckedHoverSourceProperty); } set { SetValue (CheckedHoverSourceProperty, value); } }
		public static readonly DependencyProperty CheckedHoverSourceProperty = DependencyProperty.Register (nameof (CheckedHoverSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>勾选且按下时的背景图；为 null 时退回 <see cref="ActiveSource"/>。</summary>
		public ImageSource CheckedActiveSource { get { return (ImageSource)GetValue (CheckedActiveSourceProperty); } set { SetValue (CheckedActiveSourceProperty, value); } }
		public static readonly DependencyProperty CheckedActiveSourceProperty = DependencyProperty.Register (nameof (CheckedActiveSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>勾选且有键盘焦点时的背景图；为 null 时退回 <see cref="FocusSource"/>。</summary>
		public ImageSource CheckedFocusSource { get { return (ImageSource)GetValue (CheckedFocusSourceProperty); } set { SetValue (CheckedFocusSourceProperty, value); } }
		public static readonly DependencyProperty CheckedFocusSourceProperty = DependencyProperty.Register (nameof (CheckedFocusSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
		/// <summary>勾选且禁用时的背景图；为 null 时退回 <see cref="DisabledSource"/>。</summary>
		public ImageSource CheckedDisabledSource { get { return (ImageSource)GetValue (CheckedDisabledSourceProperty); } set { SetValue (CheckedDisabledSourceProperty, value); } }
		public static readonly DependencyProperty CheckedDisabledSourceProperty = DependencyProperty.Register (nameof (CheckedDisabledSource), typeof (ImageSource), typeof (HackToggleButton), new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));
	}
}
