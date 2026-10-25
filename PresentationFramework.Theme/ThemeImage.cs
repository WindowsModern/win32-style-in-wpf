using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Attached properties that turn a <see cref="HackImage"/> or <see cref="Image"/> into a
    /// lazily loaded Win32 theme part.  Nothing is rendered until the element is loaded, and the
    /// image is released again when the element is unloaded.
    /// <para xml:lang="zh-CN">附加属性：把 <see cref="HackImage"/> 或 <see cref="Image"/> 变成按需加载的 Win32 主题部件。元素加载前不会渲染任何图像，卸载时会释放图像。</para>
    /// </summary>
    public static class ThemeImage
    {
        /// <summary>
        /// Theme class name passed to <c>OpenThemeData</c>, for example "Button" or "Edit".
        /// <para xml:lang="zh-CN">传给 <c>OpenThemeData</c> 的主题类名，例如 "Button" 或 "Edit"。</para>
        /// </summary>
        public static readonly DependencyProperty ClassProperty = DependencyProperty.RegisterAttached(
            "Class", typeof(string), typeof(ThemeImage),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDescriptorChanged));

        /// <summary>
        /// Optional window subclass passed to <c>SetWindowTheme</c> before
        /// <c>OpenThemeData</c>, for example "TaskBar" or "TrayNotifyHoriz".
        /// <para xml:lang="zh-CN">可选窗口子类，在 <c>OpenThemeData</c> 前传给 <c>SetWindowTheme</c>，例如 "TaskBar" 或 "TrayNotifyHoriz"。</para>
        /// </summary>
        public static readonly DependencyProperty SubClassProperty = DependencyProperty.RegisterAttached(
            "SubClass", typeof(string), typeof(ThemeImage),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDescriptorChanged));

        /// <summary>
        /// Rotates the loaded theme bitmap and its nine-patch margins in 90-degree
        /// steps. This is image-space rotation, not a WPF element transform, so
        /// adjacent slices cannot develop seams.
        /// <para xml:lang="zh-CN">按 90 度步进旋转已加载的主题位图及其九宫格边距；这是图像空间旋转，不是 WPF 元素变换。</para>
        /// </summary>
        public static readonly DependencyProperty RotationProperty = DependencyProperty.RegisterAttached(
            "Rotation", typeof(int), typeof(ThemeImage),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender, OnDescriptorChanged));

        /// <summary>
        /// Theme part id (the <c>xxx_P_*</c> constant).
        /// <para xml:lang="zh-CN">主题部件 id（<c>xxx_P_*</c> 常量）。</para>
        /// </summary>
        public static readonly DependencyProperty PartProperty = DependencyProperty.RegisterAttached(
            "Part", typeof(int), typeof(ThemeImage),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender, OnDescriptorChanged));

        /// <summary>
        /// Theme state id (the <c>xxx_S_*</c> constant).
        /// <para xml:lang="zh-CN">主题状态 id（<c>xxx_S_*</c> 常量）。</para>
        /// </summary>
        public static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
            "State", typeof(int), typeof(ThemeImage),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender, OnDescriptorChanged));

        /// <summary>How a theme part is rendered.</summary>
        public enum ThemeImageRenderMode
        {
            /// <summary>Draw at the element's final pixel size (UxTheme performs its own sizing).</summary>
            Exact,
            /// <summary>Draw at the part's natural size and let HackImage use its margins.</summary>
            Natural
        }

        /// <summary>
        /// The nine-patch thickness (in DIP) assigned by the host.  This mirrors
        /// HackImage.HackThickness so templates can bind a Grid margin without
        /// relying on the visual tree name scope.
        /// </summary>
        public static readonly DependencyProperty HackThicknessProperty = DependencyProperty.RegisterAttached(
            "HackThickness", typeof(Thickness), typeof(ThemeImage),
            new FrameworkPropertyMetadata(new Thickness(0)));
        /// <summary>
        /// Margin for the progress chunk container.  It is TMT_CONTENTMARGINS,
        /// falling back to TMT_SIZINGMARGINS when content margins are all zero.
        /// </summary>
        public static readonly DependencyProperty ChunkMarginProperty = DependencyProperty.RegisterAttached(
            "ChunkMargin", typeof(Thickness), typeof(ThemeImage),
            new FrameworkPropertyMetadata(new Thickness(0)));
        /// <summary>
        /// When set, the host crops the natural Progress fill bitmap by
        /// TMT_PROGRESSSPACESIZE on the right (horizontal) or top (vertical).
        /// </summary>
        public static readonly DependencyProperty CropProgressSpaceProperty = DependencyProperty.RegisterAttached(
            "CropProgressSpace", typeof(bool), typeof(ThemeImage),
            new FrameworkPropertyMetadata(false, OnDescriptorChanged));
        /// <summary>
        /// Render mode for the part.  Exact is normally correct for a complete control
        /// background; Natural is useful for a Bar that is then used as a nine-patch host.
        /// </summary>
        public static readonly DependencyProperty RenderModeProperty = DependencyProperty.RegisterAttached(
            "RenderMode", typeof(ThemeImageRenderMode), typeof(ThemeImage),
            new FrameworkPropertyMetadata(ThemeImageRenderMode.Exact, OnDescriptorChanged));
        /// <summary>
        /// Cross-fade duration for <see cref="HackImage"/> targets; zero disables the transition.
        /// <para xml:lang="zh-CN"><see cref="HackImage"/> 目标的交叉淡入时长；零表示不使用过渡。</para>
        /// </summary>
        public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.RegisterAttached(
            "TransitionDuration", typeof(TimeSpan), typeof(ThemeImage),
            new FrameworkPropertyMetadata(TimeSpan.Zero, OnDescriptorChanged));

        // Keeps the per-element host alive for exactly as long as the element itself.
        private static readonly DependencyProperty HostProperty = DependencyProperty.RegisterAttached(
            "Host", typeof(ThemeImageHost), typeof(ThemeImage), new PropertyMetadata(null));

        /// <summary>
        /// Gets the theme class name.
        /// <para xml:lang="zh-CN">获取主题类名。</para>
        /// </summary>
        public static string GetClass(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (string)element.GetValue(ClassProperty);
        }

        /// <summary>
        /// Sets the theme class name.
        /// <para xml:lang="zh-CN">设置主题类名。</para>
        /// </summary>
        public static void SetClass(DependencyObject element, string value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(ClassProperty, value);
        }

        /// <summary>
        /// Gets the optional theme window subclass.
        /// <para xml:lang="zh-CN">获取可选的主题窗口子类。</para>
        /// </summary>
        public static string GetSubClass(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (string)element.GetValue(SubClassProperty);
        }

        /// <summary>
        /// Sets the optional theme window subclass.
        /// <para xml:lang="zh-CN">设置可选的主题窗口子类。</para>
        /// </summary>
        public static void SetSubClass(DependencyObject element, string value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(SubClassProperty, value);
        }

        /// <summary>
        /// Gets the image-space rotation in degrees.
        /// <para xml:lang="zh-CN">获取图像空间旋转角度。</para>
        /// </summary>
        public static int GetRotation(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (int)element.GetValue(RotationProperty);
        }

        /// <summary>
        /// Sets the image-space rotation in degrees.
        /// <para xml:lang="zh-CN">设置图像空间旋转角度。</para>
        /// </summary>
        public static void SetRotation(DependencyObject element, int value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(RotationProperty, value);
        }

        /// <summary>
        /// Gets the theme part id.
        /// <para xml:lang="zh-CN">获取主题部件 id。</para>
        /// </summary>
        public static int GetPart(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (int)element.GetValue(PartProperty);
        }

        /// <summary>
        /// Sets the theme part id.
        /// <para xml:lang="zh-CN">设置主题部件 id。</para>
        /// </summary>
        public static void SetPart(DependencyObject element, int value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(PartProperty, value);
        }

        /// <summary>
        /// Gets the theme state id.
        /// <para xml:lang="zh-CN">获取主题状态 id。</para>
        /// </summary>
        public static int GetState(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (int)element.GetValue(StateProperty);
        }

        /// <summary>
        /// Sets the theme state id.
        /// <para xml:lang="zh-CN">设置主题状态 id。</para>
        /// </summary>
        public static void SetState(DependencyObject element, int value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(StateProperty, value);
        }

        public static Thickness GetChunkMargin(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (Thickness)element.GetValue(ChunkMarginProperty);
        }

        public static void SetChunkMargin(DependencyObject element, Thickness value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(ChunkMarginProperty, value);
        }
        public static readonly DependencyProperty PreserveAlphaProperty = DependencyProperty.RegisterAttached(
            "PreserveAlpha", typeof(bool), typeof(ThemeImage),
            new FrameworkPropertyMetadata(false, OnDescriptorChanged));
        public static bool GetPreserveAlpha(DependencyObject element)
        {
            return (bool)element.GetValue(PreserveAlphaProperty);
        }

        public static void SetPreserveAlpha(DependencyObject element, bool value)
        {
            element.SetValue(PreserveAlphaProperty, value);
        }
        public static bool GetCropProgressSpace(DependencyObject element)
        {
            return (bool)element.GetValue(CropProgressSpaceProperty);
        }

        public static void SetCropProgressSpace(DependencyObject element, bool value)
        {
            element.SetValue(CropProgressSpaceProperty, value);
        }
        public static Thickness GetHackThickness(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (Thickness)element.GetValue(HackThicknessProperty);
        }

        public static void SetHackThickness(DependencyObject element, Thickness value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(HackThicknessProperty, value);
        }
        public static ThemeImageRenderMode GetRenderMode(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (ThemeImageRenderMode)element.GetValue(RenderModeProperty);
        }

        public static void SetRenderMode(DependencyObject element, ThemeImageRenderMode value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(RenderModeProperty, value);
        }
        /// <summary>
        /// Gets the transition duration.
        /// <para xml:lang="zh-CN">获取过渡时长。</para>
        /// </summary>
        public static TimeSpan GetTransitionDuration(DependencyObject element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            return (TimeSpan)element.GetValue(TransitionDurationProperty);
        }

        /// <summary>
        /// Sets the transition duration.
        /// <para xml:lang="zh-CN">设置过渡时长。</para>
        /// </summary>
        public static void SetTransitionDuration(DependencyObject element, TimeSpan value)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            element.SetValue(TransitionDurationProperty, value);
        }

        private static void OnDescriptorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = d as FrameworkElement;
            if (element == null)
                return;

            var host = (ThemeImageHost)element.GetValue(HostProperty);
            if (host == null)
            {
                host = new ThemeImageHost(element);
                element.SetValue(HostProperty, host);
                element.Loaded += host.OnLoaded;
                element.Unloaded += host.OnUnloaded;
            }

            host.Invalidate();
        }
    }
}
