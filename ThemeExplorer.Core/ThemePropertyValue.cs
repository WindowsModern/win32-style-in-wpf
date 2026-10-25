using System;
using System.Drawing;
using System.Globalization;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// A value read from a theme property. <see cref="Type"/> indicates which of the typed values is meaningful.
    /// <para xml:lang="zh-CN">从主题属性读取的值。<see cref="Type"/> 指示哪个强类型字段有效。</para>
    /// </summary>
    public sealed class ThemePropertyValue
    {
        internal ThemePropertyValue(ThemePropertyType type)
        {
            Type = type;
        }

        /// <summary>
        /// Data type of the value.
        /// <para xml:lang="zh-CN">值的数据类型。</para>
        /// </summary>
        public ThemePropertyType Type { get; }

        /// <summary>
        /// Integer value (also used for enumeration values).
        /// <para xml:lang="zh-CN">整数值（也用于枚举值）。</para>
        /// </summary>
        public int IntValue { get; internal set; }

        /// <summary>
        /// Symbolic name of an enumeration value, when known.
        /// <para xml:lang="zh-CN">枚举值的符号名称（如果已知）。</para>
        /// </summary>
        public string EnumSymbol { get; internal set; }

        /// <summary>
        /// String value (also used for file names).
        /// <para xml:lang="zh-CN">字符串值（也用于文件名）。</para>
        /// </summary>
        public string TextValue { get; internal set; }

        /// <summary>
        /// Boolean value.
        /// <para xml:lang="zh-CN">布尔值。</para>
        /// </summary>
        public bool BoolValue { get; internal set; }

        /// <summary>
        /// Colour value.
        /// <para xml:lang="zh-CN">颜色值。</para>
        /// </summary>
        public Color ColorValue { get; internal set; }

        /// <summary>
        /// Margin value.
        /// <para xml:lang="zh-CN">边距值。</para>
        /// </summary>
        public ThemeMargins MarginsValue { get; internal set; }

        /// <summary>
        /// Rectangle value.
        /// <para xml:lang="zh-CN">矩形值。</para>
        /// </summary>
        public Rectangle RectValue { get; internal set; }

        /// <summary>
        /// Position value.
        /// <para xml:lang="zh-CN">位置值。</para>
        /// </summary>
        public Point PointValue { get; internal set; }

        /// <summary>
        /// Size value.
        /// <para xml:lang="zh-CN">尺寸值。</para>
        /// </summary>
        public Size SizeValue { get; internal set; }

        /// <summary>
        /// Font value.
        /// <para xml:lang="zh-CN">字体值。</para>
        /// </summary>
        public ThemeFontInfo FontValue { get; internal set; }

        /// <summary>
        /// Integer list value.
        /// <para xml:lang="zh-CN">整数列表值。</para>
        /// </summary>
        public int[] IntListValue { get; internal set; }

        /// <summary>
        /// Length, in bytes, of a stream value.
        /// <para xml:lang="zh-CN">流值的长度（字节）。</para>
        /// </summary>
        public int StreamLength { get; internal set; }

        /// <summary>
        /// Returns a culture-independent textual representation, mainly useful for debugging. The GUI uses its own localised formatter.
        /// <para xml:lang="zh-CN">返回与区域文化无关的文本表示，主要用于调试。GUI 使用自己的本地化格式化器。</para>
        /// </summary>
        public override string ToString()
        {
            switch (Type)
            {
                case ThemePropertyType.Enum:
                    return EnumSymbol == null
                        ? IntValue.ToString(CultureInfo.InvariantCulture)
                        : string.Format(CultureInfo.InvariantCulture, "{0} ({1})", IntValue, EnumSymbol);

                case ThemePropertyType.String:
                case ThemePropertyType.Filename:
                    return TextValue ?? string.Empty;

                case ThemePropertyType.Int:
                    return string.Format(CultureInfo.InvariantCulture, "{0} (0x{1:x})", IntValue, IntValue);

                case ThemePropertyType.Bool:
                    return BoolValue ? "True" : "False";

                case ThemePropertyType.Color:
                    return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2} (#{3:x6})",
                        ColorValue.R, ColorValue.G, ColorValue.B, ColorValue.ToArgb() & 0xFFFFFF);

                case ThemePropertyType.Margins:
                    return string.Format(CultureInfo.InvariantCulture, "{0},{1} (horz), {2},{3} (vert)",
                        MarginsValue.Left, MarginsValue.Right, MarginsValue.Top, MarginsValue.Bottom);

                case ThemePropertyType.Size:
                    return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", SizeValue.Width, SizeValue.Height);

                case ThemePropertyType.Position:
                    return string.Format(CultureInfo.InvariantCulture, "{0},{1}", PointValue.X, PointValue.Y);

                case ThemePropertyType.Rect:
                    return string.Format(CultureInfo.InvariantCulture, "{0},{1} - {2},{3} ({4}x{5})",
                        RectValue.Left, RectValue.Top, RectValue.Right, RectValue.Bottom,
                        RectValue.Width, RectValue.Height);

                case ThemePropertyType.Font:
                    return FontValue == null ? string.Empty : FontValue.ToString();

                case ThemePropertyType.IntList:
                    return IntListValue == null ? string.Empty : string.Join(",", IntListValue);

                case ThemePropertyType.DiskStream:
                case ThemePropertyType.Stream:
                    return string.Format(CultureInfo.InvariantCulture, "<stream {0} bytes>", StreamLength);

                default:
                    return string.Empty;
            }
        }
    }
}
