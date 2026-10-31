using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace System.Windows.Controls
{
	/// <summary>
	/// HackButton / HackToggleButton / HackRepeatButton 共用的视觉组合。
	/// 一张九宫格背景图铺满整个控件，内容按 Padding 内缩后叠在上面。
	/// 三个控件都不设 ControlTemplate —— 宿主提供 AddVisualChild，这里只负责组合与测量。
	/// </summary>
	internal sealed class HackButtonChrome
	{
		private readonly Control _owner;
		private readonly HackImage _art;
		private readonly ContentPresenter _content;

		internal HackButtonChrome (Control owner, Thickness hackThickness)
		{
			_owner = owner;
			_art = new HackImage
			{
				SnapsToDevicePixels = true,
				UseLayoutRounding = true,
				HackThickness = hackThickness
			};
			RenderOptions.SetBitmapScalingMode (_art, BitmapScalingMode.Fant);
			_content = new ContentPresenter { RecognizesAccessKey = true };
			Bind (ContentPresenter.ContentProperty, "Content");
			Bind (ContentPresenter.ContentTemplateProperty, "ContentTemplate");
			Bind (ContentPresenter.ContentStringFormatProperty, "ContentStringFormat");
			Bind (FrameworkElement.HorizontalAlignmentProperty, "HorizontalContentAlignment");
			Bind (FrameworkElement.VerticalAlignmentProperty, "VerticalContentAlignment");

			// 这些属性本来是"可继承"的，但控件没有 ControlTemplate，ContentPresenter 是直接
			// 挂上去的视觉子级，继承链不保证把值送到它内部隐式创建的 TextBlock。显式绑定既
			// 可靠又与模板里的 TemplateBinding 等价。
			Bind (TextElement.ForegroundProperty, "Foreground");
			Bind (TextElement.FontFamilyProperty, "FontFamily");
			Bind (TextElement.FontSizeProperty, "FontSize");
			Bind (TextElement.FontStyleProperty, "FontStyle");
			Bind (TextElement.FontWeightProperty, "FontWeight");
			Bind (TextElement.FontStretchProperty, "FontStretch");
		}

		private void Bind (DependencyProperty target, string path)
		{
			BindingOperations.SetBinding (_content, target, new Binding (path) { Source = _owner, Mode = BindingMode.OneWay });
		}

		internal HackImage Art { get { return _art; } }
		internal ContentPresenter Content { get { return _content; } }
		internal int VisualChildrenCount { get { return 2; } }

		/// <summary>
		/// 监视 IsMouseOver / IsKeyboardFocused / IsPressed 这三个只读依赖属性。
		/// 用 DependencyPropertyDescriptor 而不是 MouseEnter 之类的输入事件，是因为属性值本身
		/// 才是权威来源（合成输入、键盘切换焦点、CaptureMouse 失败等场景下事件和属性并不一致）。
		/// </summary>
		internal static void WatchState (DependencyObject element, EventHandler handler)
		{
			DependencyPropertyDescriptor.FromProperty (UIElement.IsMouseOverProperty, typeof (UIElement)).AddValueChanged (element, handler);
			DependencyPropertyDescriptor.FromProperty (UIElement.IsKeyboardFocusedProperty, typeof (UIElement)).AddValueChanged (element, handler);
			DependencyPropertyDescriptor.FromProperty (ButtonBase.IsPressedProperty, typeof (ButtonBase)).AddValueChanged (element, handler);
		}

		internal Visual GetVisualChild (int index)
		{
			if (index == 0) return _art;
			if (index == 1) return _content;
			throw new ArgumentOutOfRangeException ("index");
		}

		/// <summary>
		/// 换背景图。尺寸可能随图变化（没有内容时），所以顺带作废测量。
		/// <para>
		/// duration 有时间且大于零时走 <see cref="HackImageTransitions"/> 的交叉淡入（与
		/// HackImage 上的附加属性是同一套实现），否则立即换图。第一张图（当前还没有任何
		/// 背景）总是直接赋值，避免控件一出现就先淡进来一次。
		/// </para>
		/// </summary>
		internal void SetSource (ImageSource source, Duration duration, IEasingFunction easing)
		{
			bool animate = duration.HasTimeSpan && duration.TimeSpan > TimeSpan.Zero && _art.Source != null;
			if (!animate)
			{
				if (ReferenceEquals (_art.Source, source)) return;
				_art.Source = source;
				_owner.InvalidateMeasure ();
				return;
			}

			HackImageTransitions.SetDuration (_art, duration);
			HackImageTransitions.SetEasing (_art, easing);
			HackImageTransitions.SetTo (_art, source);
			_owner.InvalidateMeasure ();
		}

		internal void SetHackThickness (Thickness value)
		{
			if (_art.HackThickness == value) return;
			_art.HackThickness = value;
			_owner.InvalidateMeasure ();
		}

		/// <summary>
		/// 背景按自然尺寸参与测量（它自己会铺满最终尺寸），内容按 Padding 内缩后参与测量；
		/// 期望尺寸取两者较大者，这与 HackBorder 的规则一致。
		/// </summary>
		internal Size Measure (Size constraint, Thickness padding)
		{
			_art.Measure (new Size (double.PositiveInfinity, double.PositiveInfinity));
			Size artNatural = _art.DesiredSize;

			double horizontal = padding.Left + padding.Right;
			double vertical = padding.Top + padding.Bottom;
			Size inner = new Size (Math.Max (0, constraint.Width - horizontal), Math.Max (0, constraint.Height - vertical));
			_content.Measure (inner);
			Size contentSize = _content.DesiredSize;

			double width = Math.Max (contentSize.Width + horizontal, artNatural.Width);
			double height = Math.Max (contentSize.Height + vertical, artNatural.Height);
			if (!double.IsInfinity (constraint.Width)) width = Math.Min (width, Math.Max (0, constraint.Width));
			if (!double.IsInfinity (constraint.Height)) height = Math.Min (height, Math.Max (0, constraint.Height));
			return new Size (Math.Max (0, width), Math.Max (0, height));
		}

		internal Size Arrange (Size finalSize, Thickness padding)
		{
			_art.Arrange (new Rect (0, 0, finalSize.Width, finalSize.Height));

			double horizontal = padding.Left + padding.Right;
			double vertical = padding.Top + padding.Bottom;
			_content.Arrange (new Rect (
				padding.Left,
				padding.Top,
				Math.Max (0, finalSize.Width - horizontal),
				Math.Max (0, finalSize.Height - vertical)));
			return finalSize;
		}
	}
}
