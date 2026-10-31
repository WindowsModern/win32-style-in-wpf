using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Resource keys for the theme's background templates, and helpers to consume them from
    /// outside the theme assembly.
    /// <para xml:lang="zh-CN">
    /// 主题背景模板的资源键，以及供外部程序集取用的辅助方法。
    /// 这些键指向 <c>ControlTemplate</c>，模板内容是一个铺满的 <see cref="HackImage"/>，
    /// 该 <see cref="HackImage"/> 通过 <see cref="ThemeImage"/> 附加属性取得 Win32 视觉样式的部件图。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// These keys exist so that a shell-shaped application can paint Win32 taskbar and tray-Notify
    /// backgrounds with WPF layout, without re-deriving the uxTheme class/part/state triples. The
    /// triples are not guesswork: they are the documented part constants of the <c>TaskBar</c> and
    /// <c>TrayNotify</c> theme classes, and each one below is named after its constant.
    /// </para>
    /// <para>
    /// The reason the taskbar needs two of them is that its background part is split by edge, not
    /// by state. <c>TaskBar</c> has a distinct part for each edge the bar can be docked against,
    /// and the two this project ships are the ones a shell actually reaches: the bottom edge for a
    /// horizontal bar and the right edge for a vertical one. There is no single "taskbar
    /// background" part to fall back to, which is exactly why the caller has to choose.
    /// </para>
    /// <para>
    /// Consumption, from XAML — the template is looked up at run time, so the theme can be swapped:
    /// <code language="xml">
    /// &lt;ContentControl Template="{DynamicResource ThemeTaskbarBackgroundBottomTemplate}" /&gt;
    /// </code>
    /// or from code:
    /// <code language="csharp">
    /// ContentControl bar = ThemeResourceKeys.CreateBackground(ThemeResourceKeys.TaskbarBackgroundBottom);
    /// </code>
    /// </para>
    /// </remarks>
    public static class ThemeResourceKeys
    {
        /// <summary>
        /// Taskbar background, bottom edge. uxTheme <c>TaskBar</c> part 1,
        /// <c>TBP_BACKGROUNDBOTTOM</c>.
        /// <para xml:lang="zh-CN">任务栏背景（底边）。uxTheme 类 <c>TaskBar</c> 部件 1，<c>TBP_BACKGROUNDBOTTOM</c>。</para>
        /// </summary>
        public const string TaskbarBackgroundBottom = "ThemeTaskbarBackgroundBottomTemplate";

        /// <summary>
        /// Taskbar background, right edge. uxTheme <c>TaskBar</c> part 2,
        /// <c>TBP_BACKGROUNDRIGHT</c>.
        /// <para xml:lang="zh-CN">任务栏背景（右边）。uxTheme 类 <c>TaskBar</c> 部件 2，<c>TBP_BACKGROUNDRIGHT</c>。</para>
        /// </summary>
        public const string TaskbarBackgroundRight = "ThemeTaskbarBackgroundRightTemplate";

        /// <summary>
        /// Taskbar sizing bar, bottom edge. uxTheme <c>TaskBar</c> part 5,
        /// <c>TBP_SIZINGBARBOTTOM</c>.
        /// <para xml:lang="zh-CN">任务栏尺寸条（底边）。uxTheme 类 <c>TaskBar</c> 部件 5，<c>TBP_SIZINGBARBOTTOM</c>。</para>
        /// </summary>
        public const string TaskbarSizingBarBottom = "ThemeTaskbarSizingBarBottomTemplate";

        /// <summary>
        /// Taskbar bottom background rotated 90 degrees counter-clockwise.
        /// <para xml:lang="zh-CN">任务栏底边背景逆时针旋转 90 度后的结果。</para>
        /// </summary>
        public const string TaskbarBackgroundBottomRotated = "ThemeTaskbarBackgroundBottomRotatedTemplate";

        /// <summary>
        /// Notification-area (tray) background, bottom edge. uxTheme <c>TrayNotify</c> part 1,
        /// <c>TNP_BACKGROUND</c>.
        /// <para xml:lang="zh-CN">通知区域（托盘）背景（底边）。uxTheme 类 <c>TrayNotify</c> 部件 1，<c>TNP_BACKGROUND</c>。</para>
        /// </summary>
        public const string TrayNotifyBackground = "ThemeTrayNotifyBackgroundTemplate";

        // ---------------------------------------------------------------------------------
        // The same three backgrounds again, rendered by HackBorder instead of HackImage.
        //
        // Identical pixels; the difference is measurement. HackImage returns the offered size as
        // its desired size, so under SizeToContent it claims the whole work area and the window
        // grows to fill the screen. HackBorder reports the image's natural size clamped to what is
        // offered, which is how Sidebar's Slice9Image behaves.
        //
        // Both families are kept because which one is correct depends on the host: a bar filling a
        // fixed slot wants HackImage, a window that sizes to its content wants HackBorder. Leaving
        // the choice at the resource-key level keeps it out of every call site.
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Taskbar background, bottom edge, measured by <see cref="HackBorder"/> so it does not
        /// expand a <c>SizeToContent</c> window.
        /// <para xml:lang="zh-CN">任务栏背景（底边），由 <see cref="HackBorder"/> 测量，不会撑大 <c>SizeToContent</c> 窗口。</para>
        /// </summary>
        public const string TaskbarBackgroundBottomBounded = "ThemeTaskbarBackgroundBottomBorderTemplate";

        /// <summary>
        /// Taskbar background, right edge, measured by <see cref="HackBorder"/>.
        /// <para xml:lang="zh-CN">任务栏背景（右边），由 <see cref="HackBorder"/> 测量。</para>
        /// </summary>
        public const string TaskbarBackgroundRightBounded = "ThemeTaskbarBackgroundRightBorderTemplate";

        /// <summary>
        /// Notification-area (tray) background, bottom edge, measured by <see cref="HackBorder"/>.
        /// <para xml:lang="zh-CN">通知区域（托盘）背景（底边），由 <see cref="HackBorder"/> 测量。</para>
        /// </summary>
        public const string TrayNotifyBackgroundBounded = "ThemeTrayNotifyBackgroundBorderTemplate";

        /// <summary>
        /// Taskbar foreground brush, derived from the current theme.
        /// <para xml:lang="zh-CN">任务栏前景色画刷，来自当前主题。</para>
        /// </summary>
        public const string TaskbarForeground = "ThemeTaskbarForegroundBrush";

        /// <summary>
        /// General shell foreground brush, using the taskbar foreground color.
        /// <para xml:lang="zh-CN">通用外壳前景色画刷，使用任务栏前景色。</para>
        /// </summary>
        public const string GlobalForeground = "GlobalForegroundBrush";

        /// <summary>
        /// Notification-area foreground brush, from <c>TrayNotify [TrayNotifyHoriz]</c>.
        /// <para xml:lang="zh-CN">通知区域前景色画刷，来自 <c>TrayNotify [TrayNotifyHoriz]</c>。</para>
        /// </summary>
        public const string TrayNotifyForeground = "ThemeTrayNotifyForegroundBrush";

        /// <summary>
        /// Active normal-window border template.
        /// <para xml:lang="zh-CN">有焦点的普通窗口边框模板。</para>
        /// </summary>
        public const string NormalWindowBorder = "ThemeNormalWindowBorderTemplate";

        /// <summary>
        /// Active tool-window border template.
        /// <para xml:lang="zh-CN">有焦点的工具窗口边框模板。</para>
        /// </summary>
        public const string ToolWindowBorder = "ThemeToolWindowBorderTemplate";

        /// <summary>
        /// Active normal-window caption foreground brush.
        /// <para xml:lang="zh-CN">有焦点的普通窗口标题前景色画刷。</para>
        /// </summary>
        public const string NormalWindowForeground = "ThemeNormalWindowForegroundBrush";

        /// <summary>
        /// Active tool-window caption foreground brush.
        /// <para xml:lang="zh-CN">有焦点的工具窗口标题前景色画刷。</para>
        /// </summary>
        public const string ToolWindowForeground = "ThemeToolWindowForegroundBrush";

        /// <summary>
        /// Window client-area background template. Defaults to white.
        /// <para xml:lang="zh-CN">窗口内容区背景模板，默认白色。</para>
        /// </summary>
        public const string WindowContentBackground = "ThemeWindowContentBackgroundTemplate";

        /// <summary>
        /// Window client-area text foreground brush.
        /// <para xml:lang="zh-CN">窗口内容区文本前景色画刷。</para>
        /// </summary>
        public const string WindowContentForeground = "ThemeWindowContentForegroundBrush";

        /// <summary>
        /// Content margin for an active normal window.
        /// <para xml:lang="zh-CN">活动普通窗口的内容区边距。</para>
        /// </summary>
        public const string NormalWindowContentMargin = "ThemeNormalWindowContentMargin";

        /// <summary>
        /// Content margin for an active tool window.
        /// <para xml:lang="zh-CN">活动工具窗口的内容区边距。</para>
        /// </summary>
        public const string ToolWindowContentMargin = "ThemeToolWindowContentMargin";

        /// <summary>Left/right margin for a normal-window title.</summary>
        public const string NormalWindowTitleMargin = "ThemeNormalWindowTitleMargin";

        /// <summary>Left/right margin for a tool-window title.</summary>
        public const string ToolWindowTitleMargin = "ThemeToolWindowTitleMargin";

        /// <summary>
        /// Title height for an active normal window.
        /// <para xml:lang="zh-CN">活动普通窗口标题区高度。</para>
        /// </summary>
        public const string NormalWindowTitleHeight = "ThemeNormalWindowTitleHeight";

        /// <summary>
        /// Title height for an active tool window.
        /// <para xml:lang="zh-CN">活动工具窗口标题区高度。</para>
        /// </summary>
        public const string ToolWindowTitleHeight = "ThemeToolWindowTitleHeight";

        /// <summary>
        /// Width of the native left/right resize frame.
        /// <para xml:lang="zh-CN">原生窗口左右缩放边框宽度。</para>
        /// </summary>
        public const string WindowBorderWidth = "ThemeWindowBorderWidth";

        /// <summary>
        /// Height of the native bottom resize frame.
        /// <para xml:lang="zh-CN">原生窗口底边缩放边框高度。</para>
        /// </summary>
        public const string WindowBorderHeight = "ThemeWindowBorderHeight";

        /// <summary>
        /// Caption corner radius for a normal window.
        /// <para xml:lang="zh-CN">普通窗口标题栏圆角半径。</para>
        /// </summary>
        public const string NormalWindowCornerRadius = "ThemeNormalWindowCornerRadius";

        /// <summary>
        /// Caption corner radius for a tool window.
        /// <para xml:lang="zh-CN">工具窗口标题栏圆角半径。</para>
        /// </summary>
        public const string ToolWindowCornerRadius = "ThemeToolWindowCornerRadius";

        public const string NormalWindowTopLeftCornerRadius = "ThemeNormalWindowTopLeftCornerRadius";
        public const string NormalWindowTopRightCornerRadius = "ThemeNormalWindowTopRightCornerRadius";
        public const string NormalWindowBottomLeftCornerRadius = "ThemeNormalWindowBottomLeftCornerRadius";
        public const string NormalWindowBottomRightCornerRadius = "ThemeNormalWindowBottomRightCornerRadius";
        public const string ToolWindowTopLeftCornerRadius = "ThemeToolWindowTopLeftCornerRadius";
        public const string ToolWindowTopRightCornerRadius = "ThemeToolWindowTopRightCornerRadius";
        public const string ToolWindowBottomLeftCornerRadius = "ThemeToolWindowBottomLeftCornerRadius";
        public const string ToolWindowBottomRightCornerRadius = "ThemeToolWindowBottomRightCornerRadius";

        /// <summary>
        /// Caption font size for an active normal window.
        /// <para xml:lang="zh-CN">活动普通窗口标题栏字号。</para>
        /// </summary>
        public const string NormalWindowTitleFontSize = "ThemeNormalWindowTitleFontSize";

        /// <summary>
        /// Caption font size for an active tool window.
        /// <para xml:lang="zh-CN">活动工具窗口标题栏字号。</para>
        /// </summary>
        public const string ToolWindowTitleFontSize = "ThemeToolWindowTitleFontSize";

        // Array, not a read-only collection: IReadOnlyList<T> arrived in .NET 4.5 and this
        // assembly targets 4.0.
        private static readonly string[] AllKeys = new string[]
        {
            TaskbarBackgroundBottom,
            TaskbarBackgroundRight,
            TaskbarSizingBarBottom,
            TaskbarBackgroundBottomRotated,
            TrayNotifyBackground,
            TaskbarBackgroundBottomBounded,
            TaskbarBackgroundRightBounded,
            TrayNotifyBackgroundBounded,
            NormalWindowBorder,
            ToolWindowBorder
        };

        /// <summary>
        /// Every background key this class defines, in the order they appear above.
        /// <para xml:lang="zh-CN">本类定义的全部背景键，按上面的顺序。</para>
        /// </summary>
        /// <remarks>
        /// A copy is returned every time. Handing out the backing array would let a caller
        /// reorder or overwrite the theme's own list, and a diagnostic loop is not worth that.
        /// </remarks>
        public static IList<string> All
        {
            get { return new List<string>(AllKeys); }
        }

        /// <summary>
        /// Looks up one of these templates.
        /// <para xml:lang="zh-CN">查找其中一个模板。</para>
        /// </summary>
        /// <param name="key">One of the constants on this class.</param>
        /// <param name="scope">
        /// An element to search first, typically the caller's window. Its resource chain is walked
        /// before the application's, which is what lets a host override a single background without
        /// replacing the whole theme.
        /// </param>
        /// <returns>The template, or <c>null</c> when the theme is not loaded.</returns>
        public static ControlTemplate FindTemplate(string key, DependencyObject scope)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Walk the caller's own resource chain first, then the application's.
            FrameworkElement element = scope as FrameworkElement;
            if (element != null)
            {
                ControlTemplate local = element.TryFindResource(key) as ControlTemplate;
                if (local != null)
                {
                    return local;
                }
            }

            Application application = Application.Current;
            if (application != null)
            {
                return application.TryFindResource(key) as ControlTemplate;
            }

            return null;
        }

        /// <summary>
        /// Creates an element that renders one of these backgrounds.
        /// <para xml:lang="zh-CN">创建一个渲染这些背景之一的元素。</para>
        /// </summary>
        /// <param name="key">One of the constants on this class.</param>
        /// <param name="scope">
        /// See <see cref="FindTemplate"/>. May be <c>null</c>, in which case only the application's
        /// resources are searched.
        /// </param>
        /// <returns>
        /// A <see cref="ContentControl"/> whose <see cref="Control.Template"/> is the requested
        /// template, or <c>null</c> when the theme is not loaded.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The template is assigned, not referenced by a <c>DynamicResource</c> expression. A
        /// caller that wants live theme swapping should set the expression itself:
        /// </para>
        /// <code language="csharp">
        /// bar.SetResourceReference(Control.TemplateProperty, ThemeResourceKeys.TaskbarBackgroundBottom);
        /// </code>
        /// <para>
        /// This method is for the case where the caller wants one concrete element and does not
        /// care about re-theming.
        /// </para>
        /// </remarks>
        public static ContentControl CreateBackground(string key, DependencyObject scope)
        {
            ControlTemplate template = FindTemplate(key, scope);
            if (template == null)
            {
                return null;
            }

            ContentControl host = new ContentControl();
            host.Template = template;
            return host;
        }
    }
}
