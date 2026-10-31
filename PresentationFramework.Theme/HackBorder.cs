using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Windows.Themes;

namespace System.Windows.Controls
{
    /// <summary>
    /// A <see cref="Border"/> that paints a Win32 theme part (nine-patch) as its surface, behind its
    /// single child, and behaves like a Border in every other respect.
    /// <para xml:lang="zh-CN">
    /// 一个 <see cref="Border"/>：把 Win32 主题部件（九宫格位图）画成自己的表面、位于单个子节点之下，
    /// 其余行为与 Border 完全一致。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para xml:lang="zh-CN">
    /// <b>用法与 Border 相同。</b>
    /// <c>Background</c>、<c>BorderBrush</c>、<c>BorderThickness</c>、<c>CornerRadius</c>、
    /// <c>Padding</c> 以及"XAML 里直接写一个子节点"全部是 <see cref="Border"/> 自己的属性，
    /// 不是转发、不是重实现。
    /// </para>
    /// <para xml:lang="zh-CN">
    /// <b>叠放顺序：</b><c>Background</c>（Border 自己画）→ 主题位图 → 子节点内容 → 边框线。
    /// 位图被裁剪在 <c>BorderThickness</c> 之内并遵守 <c>CornerRadius</c>，所以它既不会盖住
    /// 边框线，也不会溢出圆角。<c>Padding</c> 只影响子节点，与 Border 语义一致。
    /// </para>
    /// <para xml:lang="zh-CN">
    /// <b>为什么内部要有一个真的 <see cref="HackImage"/>。</b>
    /// <c>HackImageTransition</c> 的构造函数只接受 <see cref="HackImage"/>，
    /// 主题位图的加载器 <c>ThemeImageHost</c> 也是硬转型到 <see cref="HackImage"/> 才把位图交出来。
    /// 继承 <see cref="Border"/> 就不再是 <see cref="HackImage"/>，所以这里组合一个真实的
    /// <see cref="HackImage"/> 作为额外的可视子级，并把 <c>ThemeImage</c> 附加属性转发给它。
    /// 这样过渡与按需加载全部照旧，而且 <see cref="HackImage"/>、<see cref="HackImageTransition"/>、
    /// <c>ThemeImageHost</c> 一个都不用改。
    /// </para>
    /// <para xml:lang="zh-CN">
    /// <b>有界测量不变。</b>
    /// 内部位图用"无穷可用尺寸"测量，所以它只贡献自然尺寸，不会把可用空间当成自己的需求；
    /// 本控件的期望尺寸因此由 Border 的内容推导，放进 <c>SizeToContent</c> 窗口不会把窗口撑大。
    /// </para>
    /// </remarks>
    public class HackBorder : Border, IThemeImageHostRelay
    {
        private readonly HackImage _art;


        /// <summary>The inner element that loads and transitions the theme part.</summary>
        HackImage IThemeImageHostRelay.ThemeImageTarget
        {
            get { return _art; }
        }

        /// <summary>Creates the control and its internal art element.</summary>
        public HackBorder ()
        {
            _art = new HackImage ();
            _art.HorizontalAlignment = HorizontalAlignment.Stretch;
            _art.VerticalAlignment = VerticalAlignment.Stretch;

            // The art is a real visual child so that it is Loaded, resolved and able to transition;
            // it is deliberately NOT the logical child, because Border.Child is the user's content.
            AddVisualChild (_art);
            AddLogicalChild (_art);

            // Border paints Background and BorderBrush itself; the art is composited above both and
            // is clipped to the inner rect, so the border line stays visible on top.
            ClipToBounds = false;

            // Descriptors (ThemeImage.Class/SubClass/Part/State/...) are set on THIS border by the
            // theme, but only a real HackImage can load and transition a part. Pointing the art at
            // this object makes every ThemeImage getter read through the indirection, so nothing
            // has to be copied and no descriptor can be forgotten.
            _art.ThemeDescriptorSource = this;
        }

        #region Art surface properties (mirrors HackImage)

        /// <summary>Identifies the <see cref="Source"/> property.</summary>
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register (
            "Source", typeof (ImageSource), typeof (HackBorder),
            new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));

        /// <summary>
        /// The nine-patch image. Normally left unset: <c>ThemeImage</c> attached properties fill it
        /// from the current visual style.
        /// <para xml:lang="zh-CN">九宫格图片。通常不直接设置，由 <c>ThemeImage</c> 附加属性从当前视觉样式填入。</para>
        /// </summary>
        public ImageSource Source
        {
            get { return (ImageSource)GetValue (SourceProperty); }
            set { SetValue (SourceProperty, value); }
        }

        /// <summary>Identifies the <see cref="HackThickness"/> property.</summary>
        public static readonly DependencyProperty HackThicknessProperty = DependencyProperty.Register (
            "HackThickness", typeof (Thickness), typeof (HackBorder),
            new FrameworkPropertyMetadata (new Thickness (0), FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));

        /// <summary>
        /// Nine-patch slice widths (Left, Top, Right, Bottom), in image pixels.
        /// <para xml:lang="zh-CN">九宫格切片宽度（左、上、右、下），单位为图像像素。</para>
        /// </summary>
        public Thickness HackThickness
        {
            get { return (Thickness)GetValue (HackThicknessProperty); }
            set { SetValue (HackThicknessProperty, value); }
        }

        /// <summary>Identifies the <see cref="IsSourceShrink"/> property.</summary>
        public static readonly DependencyProperty IsSourceShrinkProperty = DependencyProperty.Register (
            "IsSourceShrink", typeof (bool), typeof (HackBorder),
            new FrameworkPropertyMetadata (false, FrameworkPropertyMetadataOptions.AffectsRender, OnArtChanged));

        /// <summary>
        /// When the image is smaller than the slice widths, fall back to stretching the whole image
        /// instead of drawing nothing.
        /// <para xml:lang="zh-CN">当图像小于切片宽度之和时，退化为一整图拉伸，而不是什么都不画。</para>
        /// </summary>
        public bool IsSourceShrink
        {
            get { return (bool)GetValue (IsSourceShrinkProperty); }
            set { SetValue (IsSourceShrinkProperty, value); }
        }

        /// <summary>Identifies the <see cref="ScalingMode"/> property.</summary>
        public static readonly DependencyProperty ScalingModeProperty = DependencyProperty.Register (
            "ScalingMode", typeof (BitmapScalingMode), typeof (HackBorder),
            new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality, OnArtChanged));

        /// <summary>Bitmap scaling quality for the theme part.</summary>
        /// <para xml:lang="zh-CN">主题位图的缩放质量。</para>
        public BitmapScalingMode ScalingMode
        {
            get { return (BitmapScalingMode)GetValue (ScalingModeProperty); }
            set { SetValue (ScalingModeProperty, value); }
        }

        /// <summary>Identifies the <see cref="Foreground"/> property.</summary>
        public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register (
            "Foreground", typeof (Brush), typeof (HackBorder),
            new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Text foreground. <see cref="Border"/> has no <c>Foreground</c>, so this stores the brush
        /// and mirrors it onto the inherited <c>TextElement.Foreground</c> so the child inherits it.
        /// <para xml:lang="zh-CN">文本前景色。<see cref="Border"/> 没有该属性，这里保存画刷并同步到继承属性
        /// <c>TextElement.Foreground</c>，使子节点能够继承。</para>
        /// </summary>
        public Brush Foreground
        {
            get { return (Brush)GetValue (ForegroundProperty); }
            set { SetValue (ForegroundProperty, value); }
        }

        private static void OnArtChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            HackBorder border = (HackBorder)d;
            border.SyncArt ();
        }

        /// <summary>Pushes this control's art properties onto the internal element.</summary>
        internal void SyncArt ()
        {
            _art.HackThickness = HackThickness;
            _art.IsSourceShrink = IsSourceShrink;
            _art.ScalingMode = ScalingMode;

            // Only take over Source when the caller set one explicitly; leaving it alone otherwise
            // is what lets ThemeImage (and its transitions) own the image.
            if (Source != null && !ReferenceEquals (_art.Source, Source))
            {
                _art.Source = Source;
            }
        }

        /// <summary>Pushes <see cref="Foreground"/> onto the inherited text property.</summary>
        internal void SyncForeground ()
        {
            TextElement.SetForeground (this, Foreground);
        }

        #endregion

        #region Layout

        private Size _artNatural;

        /// <inheritdoc/>
        protected override Size MeasureOverride (Size constraint)
        {
            // Measure the art with an INFINITE constraint so it reports its natural size instead of
            // claiming the offered space. HackImage hands `availableSize` back when it is finite, and
            // letting that reach the layout would push a SizeToContent window to the work area.
            _art.Measure (new Size (double.PositiveInfinity, double.PositiveInfinity));
            _artNatural = _art.DesiredSize;

            Size content = base.MeasureOverride (constraint);

            // The art only widens the control when there is no child to size to; with a child the
            // Border behaviour wins, which is what "used like a Border" means.
            double width = Math.Max (content.Width, _artNatural.Width + BorderThickness.Left + BorderThickness.Right);
            double height = Math.Max (content.Height, _artNatural.Height + BorderThickness.Top + BorderThickness.Bottom);

            // Never exceed what the parent actually offers.
            if (!double.IsInfinity (constraint.Width)) width = Math.Min (width, constraint.Width);
            if (!double.IsInfinity (constraint.Height)) height = Math.Min (height, constraint.Height);

            return new Size (width, height);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride (Size finalSize)
        {
            Size result = base.ArrangeOverride (finalSize);

            // The art fills the area inside the border line but ignores Padding: Padding belongs to
            // the child, exactly as in Border.
            double left = BorderThickness.Left;
            double top = BorderThickness.Top;
            double width = Math.Max (0.0, finalSize.Width - BorderThickness.Left - BorderThickness.Right);
            double height = Math.Max (0.0, finalSize.Height - BorderThickness.Top - BorderThickness.Bottom);

            _art.Arrange (new Rect (left, top, width, height));

            // Clip to the rounded inner rect so the part cannot spill past CornerRadius.
            double radius = Math.Max (0.0, Math.Max (CornerRadius.TopLeft, Math.Max (CornerRadius.TopRight, Math.Max (CornerRadius.BottomLeft, CornerRadius.BottomRight))));
            if (radius > 0.0)
            {
                RectangleGeometry clip = new RectangleGeometry (new Rect (left, top, width, height), radius, radius);
                clip.Freeze ();
                _art.Clip = clip;
            }
            else
            {
                _art.Clip = null;
            }

            return result;
        }

        /// <inheritdoc/>
        protected override int VisualChildrenCount
        {
            get
            {
                // Art first, then whatever Border contributes (its Child), so the child is on top.
                return (_art != null ? 1 : 0) + base.VisualChildrenCount;
            }
        }

        /// <inheritdoc/>
        protected override Visual GetVisualChild (int index)
        {
            if (_art != null)
            {
                if (index == 0)
                {
                    return _art;
                }
                index--;
            }
            return base.GetVisualChild (index);
        }

        #endregion

        #region Property plumbing

        /// <inheritdoc/>
        protected override void OnPropertyChanged (DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged (e);

            if (e.Property == ForegroundProperty || e.Property == TextElement.ForegroundProperty)
            {
                SyncForeground ();
            }
            else if (e.Property == CornerRadiusProperty)
            {
                InvalidateArrange ();
            }
        }

        #endregion
    }
}
