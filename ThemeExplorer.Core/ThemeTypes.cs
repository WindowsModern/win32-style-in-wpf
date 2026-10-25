using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// The four margin widths of a theme part.
    /// <para xml:lang="zh-CN">主题部件的四个边距宽度。</para>
    /// </summary>
    public struct ThemeMargins : System.IEquatable<ThemeMargins>
    {
        /// <summary>
        /// Creates a margin value.
        /// <para xml:lang="zh-CN">创建边距值。</para>
        /// </summary>
        public ThemeMargins(int left, int right, int top, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        /// <summary>
        /// Left margin, in pixels.
        /// <para xml:lang="zh-CN">左边距（像素）。</para>
        /// </summary>
        public int Left { get; }

        /// <summary>
        /// Right margin, in pixels.
        /// <para xml:lang="zh-CN">右边距（像素）。</para>
        /// </summary>
        public int Right { get; }

        /// <summary>
        /// Top margin, in pixels.
        /// <para xml:lang="zh-CN">上边距（像素）。</para>
        /// </summary>
        public int Top { get; }

        /// <summary>
        /// Bottom margin, in pixels.
        /// <para xml:lang="zh-CN">下边距（像素）。</para>
        /// </summary>
        public int Bottom { get; }

        /// <summary>
        /// Returns whether two margin values are equal.
        /// <para xml:lang="zh-CN">返回两个边距值是否相等。</para>
        /// </summary>
        public bool Equals(ThemeMargins other)
        {
            return Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom;
        }

        /// <summary>
        /// Returns whether this value equals another object.
        /// <para xml:lang="zh-CN">返回此值是否等于另一个对象。</para>
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is ThemeMargins && Equals((ThemeMargins)obj);
        }

        /// <summary>
        /// Returns a hash code for the margin value.
        /// <para xml:lang="zh-CN">返回边距值的哈希代码。</para>
        /// </summary>
        public override int GetHashCode()
        {
            return Left ^ (Right << 8) ^ (Top << 16) ^ (Bottom << 24);
        }

        /// <summary>
        /// Returns a textual representation of the margins.
        /// <para xml:lang="zh-CN">返回边距的文本表示。</para>
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0},{1} (horz), {2},{3} (vert)", Left, Right, Top, Bottom);
        }
    }

    /// <summary>
    /// A font as reported by <c>GetThemeFont</c>.
    /// <para xml:lang="zh-CN">由 <c>GetThemeFont</c> 返回的字体。</para>
    /// </summary>
    public sealed class ThemeFontInfo
    {
        internal ThemeFontInfo(string faceName, int height, int weight, bool italic, bool underline)
        {
            FaceName = faceName;
            Height = height;
            Weight = weight;
            Italic = italic;
            Underline = underline;
        }

        /// <summary>
        /// Typeface name, for example "Segoe UI".
        /// <para xml:lang="zh-CN">字体名称，例如 "Segoe UI"。</para>
        /// </summary>
        public string FaceName { get; }

        /// <summary>
        /// Character height, in logical units.
        /// <para xml:lang="zh-CN">字符高度（逻辑单位）。</para>
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Weight (400 is normal, 700 is bold).
        /// <para xml:lang="zh-CN">字重（400 为常规，700 为粗体）。</para>
        /// </summary>
        public int Weight { get; }

        /// <summary>
        /// Whether the font is italic.
        /// <para xml:lang="zh-CN">字体是否斜体。</para>
        /// </summary>
        public bool Italic { get; }

        /// <summary>
        /// Whether the font is underlined.
        /// <para xml:lang="zh-CN">字体是否带下划线。</para>
        /// </summary>
        public bool Underline { get; }

        /// <summary>
        /// Creates a <see cref="Font"/> that approximates this theme font.
        /// <para xml:lang="zh-CN">创建接近此主题字体的 <see cref="Font"/>。</para>
        /// </summary>
        public Font ToFont(float dpiScale)
        {
            float size = Height * dpiScale * 72.0f / 96.0f;
            if (size <= 0)
                size = 9.0f;

            FontStyle style = FontStyle.Regular;
            if (Italic)
                style |= FontStyle.Italic;
            if (Underline)
                style |= FontStyle.Underline;
            if (Weight >= 700)
                style |= FontStyle.Bold;

            try
            {
                return new Font(FaceName, size, style, GraphicsUnit.Point);
            }
            catch (System.ArgumentException)
            {
                return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
            }
        }

        /// <summary>
        /// Returns a textual representation of the font.
        /// <para xml:lang="zh-CN">返回字体的文本表示。</para>
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0},{1} weight {2}{3}{4}", FaceName, Height, Weight,
                Italic ? ", italic" : string.Empty,
                Underline ? ", underlined" : string.Empty);
        }
    }

    /// <summary>
    /// Information about the visual style file currently in use.
    /// <para xml:lang="zh-CN">当前使用的视觉样式文件信息。</para>
    /// </summary>
    public sealed class ThemeInfo
    {
        internal ThemeInfo(string fileName, string colorScheme, string sizeName)
        {
            FileName = fileName;
            ColorScheme = colorScheme;
            SizeName = sizeName;
        }

        /// <summary>
        /// Full path of the <c>.msstyles</c> file, or an empty string.
        /// <para xml:lang="zh-CN"><c>.msstyles</c> 文件的完整路径，或空字符串。</para>
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Colour scheme name (for example "NormalColor").
        /// <para xml:lang="zh-CN">配色方案名称（例如 "NormalColor"）。</para>
        /// </summary>
        public string ColorScheme { get; }

        /// <summary>
        /// Size name (for example "NormalSize").
        /// <para xml:lang="zh-CN">尺寸名称（例如 "NormalSize"）。</para>
        /// </summary>
        public string SizeName { get; }

        /// <summary>
        /// File name without the directory, for display purposes.
        /// <para xml:lang="zh-CN">不含目录的文件名，用于显示。</para>
        /// </summary>
        public string ShortFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                    return string.Empty;
                int index = FileName.LastIndexOfAny(new[] { '\\', '/' });
                return index >= 0 ? FileName.Substring(index + 1) : FileName;
            }
        }
    }

    /// <summary>
    /// Read-only access to the visual style database (classes, parts, properties) that ships with the library.
    /// <para xml:lang="zh-CN">对随库发布的视觉样式数据库（类、部件、属性）的只读访问。</para>
    /// </summary>
    public static class ThemeDatabase
    {
        private static readonly ReadOnlyCollection<ThemeClassInfo> ClassList =
            new ReadOnlyCollection<ThemeClassInfo>(ThemeData.Classes);

        private static readonly ReadOnlyCollection<ThemePropertyInfo> PropertyList =
            new ReadOnlyCollection<ThemePropertyInfo>(ThemeData.Properties);

        /// <summary>
        /// All known visual style classes, ordered by name. Window subclasses are listed as separate entries; see <see cref="ThemeClassInfo.SubClass"/>.
        /// <para xml:lang="zh-CN">所有已知的视觉样式类，按名称排序。窗口子类会作为单独条目列出；参见 <see cref="ThemeClassInfo.SubClass"/>。</para>
        /// </summary>
        public static IList<ThemeClassInfo> Classes
        {
            get { return ClassList; }
        }

        /// <summary>
        /// All <c>TMT_*</c> properties that <see cref="VisualStyle"/> can read via its TryGetProperty overloads understands.
        /// <para xml:lang="zh-CN">所有可通过 <see cref="VisualStyle"/> 的 TryGetProperty 重载读取的 <c>TMT_*</c> 属性。</para>
        /// </summary>
        public static IList<ThemePropertyInfo> Properties
        {
            get { return PropertyList; }
        }

        /// <summary>
        /// Tries to resolve a numeric property id to its definition.
        /// <para xml:lang="zh-CN">尝试把数字属性 id 解析为属性定义。</para>
        /// </summary>
        public static bool TryGetProperty(int propertyId, out ThemePropertyInfo property)
        {
            for (int i = 0; i < PropertyList.Count; i++)
            {
                if (PropertyList[i].Id == propertyId)
                {
                    property = PropertyList[i];
                    return true;
                }
            }

            property = null;
            return false;
        }
    }
}
