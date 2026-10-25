namespace ThemeExplorer.Core
{
    /// <summary>
    /// Kind of data stored in a visual style property (the <c>TMT_*</c> types defined by the Windows theming API).
    /// <para xml:lang="zh-CN">视觉样式属性中存储的数据类型（Windows 主题 API 定义的 <c>TMT_*</c> 类型）。</para>
    /// </summary>
    public enum ThemePropertyType
    {
        /// <summary>
        /// A value from one of the documented enumerations (for example <c>BT_BORDERFILL</c>).
        /// <para xml:lang="zh-CN">文档中定义的某个枚举值（例如 <c>BT_BORDERFILL</c>）。</para>
        /// </summary>
        Enum = 0,
        /// <summary>
        /// A Unicode string.
        /// <para xml:lang="zh-CN">Unicode 字符串。</para>
        /// </summary>
        String,
        /// <summary>
        /// A signed integer.
        /// <para xml:lang="zh-CN">有符号整数。</para>
        /// </summary>
        Int,
        /// <summary>
        /// A boolean value.
        /// <para xml:lang="zh-CN">布尔值。</para>
        /// </summary>
        Bool,
        /// <summary>
        /// A <c>COLORREF</c> colour.
        /// <para xml:lang="zh-CN"><c>COLORREF</c> 颜色。</para>
        /// </summary>
        Color,
        /// <summary>
        /// Four margin widths.
        /// <para xml:lang="zh-CN">四个边距宽度。</para>
        /// </summary>
        Margins,
        /// <summary>
        /// A file name, usually relative to the theme file.
        /// <para xml:lang="zh-CN">文件名，通常相对于主题文件。</para>
        /// </summary>
        Filename,
        /// <summary>
        /// A width/height pair.
        /// <para xml:lang="zh-CN">宽/高对。</para>
        /// </summary>
        Size,
        /// <summary>
        /// An x/y position.
        /// <para xml:lang="zh-CN">x/y 坐标。</para>
        /// </summary>
        Position,
        /// <summary>
        /// A rectangle.
        /// <para xml:lang="zh-CN">矩形。</para>
        /// </summary>
        Rect,
        /// <summary>
        /// A <c>LOGFONT</c> description.
        /// <para xml:lang="zh-CN"><c>LOGFONT</c> 字体描述。</para>
        /// </summary>
        Font,
        /// <summary>
        /// A list of integers.
        /// <para xml:lang="zh-CN">整数列表。</para>
        /// </summary>
        IntList,
        /// <summary>
        /// A stream stored in the theme file on disk (Windows Vista and later).
        /// <para xml:lang="zh-CN">存储在磁盘主题文件中的流（Windows Vista 及更高版本）。</para>
        /// </summary>
        DiskStream,
        /// <summary>
        /// An in-memory stream (Windows Vista and later).
        /// <para xml:lang="zh-CN">内存中的流（Windows Vista 及更高版本）。</para>
        /// </summary>
        Stream
    }

    /// <summary>
    /// Where a property value was found while resolving it (the <c>PROPERTYORIGIN</c> enumeration of the Windows theming API).
    /// <para xml:lang="zh-CN">解析属性值时该值的来源（Windows 主题 API 的 <c>PROPERTYORIGIN</c> 枚举）。</para>
    /// </summary>
    public enum ThemePropertyOrigin
    {
        /// <summary>
        /// The property is not defined for the requested part and state.
        /// <para xml:lang="zh-CN">该属性未在所请求的部件和状态上定义。</para>
        /// </summary>
        NotDefined = 0,
        /// <summary>
        /// Defined for the exact state.
        /// <para xml:lang="zh-CN">在精确匹配的状态上定义。</para>
        /// </summary>
        State = 1,
        /// <summary>
        /// Inherited from the part defaults.
        /// <para xml:lang="zh-CN">继承自部件默认值。</para>
        /// </summary>
        Part = 2,
        /// <summary>
        /// Inherited from the class defaults.
        /// <para xml:lang="zh-CN">继承自类默认值。</para>
        /// </summary>
        Class = 3,
        /// <summary>
        /// Inherited from the global (theme-wide) defaults.
        /// <para xml:lang="zh-CN">继承自全局（主题范围）默认值。</para>
        /// </summary>
        Global = 4
    }

    /// <summary>
    /// The <c>THEMESIZE</c> enumeration used by <c>GetThemePartSize</c>.
    /// <para xml:lang="zh-CN"><c>GetThemePartSize</c> 使用的 <c>THEMESIZE</c> 枚举。</para>
    /// </summary>
    public enum ThemeSize
    {
        /// <summary>
        /// Smallest size the part can be drawn at.
        /// <para xml:lang="zh-CN">部件可绘制的最小尺寸。</para>
        /// </summary>
        Min = 0,
        /// <summary>
        /// Native size of the part.
        /// <para xml:lang="zh-CN">部件的原始尺寸。</para>
        /// </summary>
        True = 1,
        /// <summary>
        /// Size used when the part is drawn.
        /// <para xml:lang="zh-CN">绘制部件时使用的尺寸。</para>
        /// </summary>
        Draw = 2
    }

    /// <summary>
    /// Selects which bitmap of a theme part is requested.
    /// <para xml:lang="zh-CN">选择要获取主题部件的哪个位图。</para>
    /// </summary>
    public enum ThemeBitmapKind
    {
        /// <summary>
        /// The part background bitmap (<c>TMT_DIBDATA</c>).
        /// <para xml:lang="zh-CN">部件背景位图（<c>TMT_DIBDATA</c>）。</para>
        /// </summary>
        Background,
        /// <summary>
        /// The glyph bitmap (<c>TMT_GLYPHDIBDATA</c>).
        /// <para xml:lang="zh-CN">字形位图（<c>TMT_GLYPHDIBDATA</c>）。</para>
        /// </summary>
        Glyph
    }

    /// <summary>
    /// Text formatting flags for <see cref="ThemeRenderer"/>; the values match the Win32 <c>DT_*</c> flags understood by <c>DrawThemeText</c>.
    /// <para xml:lang="zh-CN">用于 <see cref="ThemeRenderer"/> 的文本格式标志；取值与 <c>DrawThemeText</c> 理解的 Win32 <c>DT_*</c> 标志一致。</para>
    /// </summary>
    [System.Flags]
    public enum ThemeTextFormat
    {
        /// <summary>
        /// Left aligned text.
        /// <para xml:lang="zh-CN">左对齐文本。</para>
        /// </summary>
        Left = 0x00000000,
        /// <summary>
        /// Text is centred horizontally.
        /// <para xml:lang="zh-CN">文本水平居中。</para>
        /// </summary>
        Center = 0x00000001,
        /// <summary>
        /// Text is right aligned.
        /// <para xml:lang="zh-CN">文本右对齐。</para>
        /// </summary>
        Right = 0x00000002,
        /// <summary>
        /// Text is centred vertically.
        /// <para xml:lang="zh-CN">文本垂直居中。</para>
        /// </summary>
        VerticalCenter = 0x00000004,
        /// <summary>
        /// Break words when wrapping.
        /// <para xml:lang="zh-CN">换行时按单词断行。</para>
        /// </summary>
        WordBreak = 0x00000010,
        /// <summary>
        /// Use a single line.
        /// <para xml:lang="zh-CN">单行显示。</para>
        /// </summary>
        SingleLine = 0x00000020,
        /// <summary>
        /// Do not clip the text.
        /// <para xml:lang="zh-CN">不裁剪文本。</para>
        /// </summary>
        NoPrefix = 0x00000800
    }

    /// <summary>
    /// Flags for the optional <c>GetThemeBitmap</c> call.
    /// <para xml:lang="zh-CN">可选 <c>GetThemeBitmap</c> 调用使用的标志。</para>
    /// </summary>
    [System.Flags]
    internal enum ThemeBitmapFlags
    {
        None = 0,
        /// <summary>
        /// Return the bitmap owned by the theme (do not delete it).
        /// <para xml:lang="zh-CN">返回主题拥有的位图（不要删除它）。</para>
        /// </summary>
        Direct = 0x00000001,
        /// <summary>
        /// Return a copy of the bitmap (the caller owns it).
        /// <para xml:lang="zh-CN">返回位图的副本（由调用方负责释放）。</para>
        /// </summary>
        Copy = 0x00000002
    }
}
