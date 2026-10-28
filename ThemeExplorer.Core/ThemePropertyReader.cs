using System;
using System.Collections.Generic;
using System.Drawing;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// Symbolic names for the values of <c>TMT_ENUM</c> properties.
    /// <para xml:lang="zh-CN"><c>TMT_ENUM</c> 属性取值的符号名称。</para>
    /// </summary>
    internal static class ThemeEnumSymbols
    {
        private static readonly Dictionary<int, Dictionary<int, string>> Symbols = CreateSymbols();

        internal static string Get(int propertyId, int value)
        {
            Dictionary<int, string> map;
            string symbol;
            if (Symbols.TryGetValue(propertyId, out map) && map.TryGetValue(value, out symbol))
                return symbol;
            return null;
        }

        private static Dictionary<int, Dictionary<int, string>> CreateSymbols()
        {
            var result = new Dictionary<int, Dictionary<int, string>>
            {
                [4001] = Map("BT_IMAGEFILE", "BT_BORDERFILL", "BT_NONE"),
                [4002] = Map("BT_RECT", "BT_ROUNDRECT", "BT_ELLIPSE"),
                [4003] = Map("FT_SOLID", "FT_VERTGRADIENT", "FT_HORZGRADIENT", "FT_RADIALGRADIENT", "FT_TILEIMAGE"),
                [4004] = Map("ST_TRUESIZE", "ST_STRETCH", "ST_TILE"),
                [4005] = Map("HA_LEFT", "HA_CENTER", "HA_RIGHT"),
                [4006] = Map("CA_LEFT", "CA_CENTER", "CA_RIGHT"),
                [4007] = Map("VA_TOP", "VA_CENTER", "VA_BOTTOM"),
                [4008] = Map("OT_TOPLEFT", "OT_TOPRIGHT", "OT_TOPMIDDLE", "OT_BOTTOMLEFT", "OT_BOTTOMRIGHT",
                            "OT_BOTTOMMIDDLE", "OT_MIDDLELEFT", "OT_MIDDLERIGHT", "OT_LEFTOFCAPTION",
                            "OT_RIGHTOFCAPTION", "OT_LEFTOFLASTBUTTON", "OT_RIGHTOFLASTBUTTON",
                            "OT_ABOVELASTBUTTON", "OT_BELOWLASTBUTTON"),
                [4009] = Map("ICE_NONE", "ICE_GLOW", "ICE_SHADOW", "ICE_PULSE", "ICE_ALPHA"),
                [4010] = Map("TST_NONE", "TST_SINGLE", "TST_CONTINUOUS"),
                [4011] = Map("IL_VERTICAL", "IL_HORIZONTAL"),
                [4012] = Map("GT_NONE", "GT_IMAGEGLYPH", "GT_FONTGLYPH"),
                [4013] = Map("IST_NONE", "IST_SIZE", "IST_DPI"),
                [4014] = Map("GFST_NONE", "GFST_SIZE", "GFST_DPI"),
                [4015] = Map("TSST_NONE", "TSST_SIZE", "TSST_DPI")
            };
            return result;
        }

        private static Dictionary<int, string> Map(params string[] names)
        {
            var map = new Dictionary<int, string>();
            for (int i = 0; i < names.Length; i++)
                map[i] = names[i];
            return map;
        }
    }

    /// <summary>
    /// Reads a <see cref="ThemePropertyValue"/> for a given property definition.
    /// <para xml:lang="zh-CN">按给定属性定义读取 <see cref="ThemePropertyValue"/>。</para>
    /// </summary>
    internal static class ThemePropertyReader
    {
        internal static bool TryRead(VisualStyle style, int part, int state, ThemePropertyInfo property,
            out ThemePropertyValue value)
        {
            value = null;
            if (style == null)
                throw new ArgumentNullException(nameof(style));
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            switch (property.Type)
            {
                case ThemePropertyType.Enum:
                {
                    int number;
                    if (!style.TryGetInt(part, state, property.Id, out number))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Enum)
                    {
                        IntValue = number,
                        EnumSymbol = ThemeEnumSymbols.Get(property.Id, number)
                    };
                    return true;
                }

                case ThemePropertyType.Int:
                {
                    int number;
                    if (!style.TryGetInt(part, state, property.Id, out number))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Int) { IntValue = number };
                    return true;
                }

                case ThemePropertyType.Bool:
                {
                    bool flag;
                    if (!style.TryGetBool(part, state, property.Id, out flag))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Bool) { BoolValue = flag };
                    return true;
                }

                case ThemePropertyType.Color:
                {
                    Color color;
                    if (!style.TryGetColor(part, state, property.Id, out color))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Color) { ColorValue = color };
                    return true;
                }

                case ThemePropertyType.String:
                {
                    string text;
                    if (!style.TryGetString(part, state, property.Id, out text))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.String) { TextValue = text };
                    return true;
                }

                case ThemePropertyType.Filename:
                {
                    string text;
                    if (!style.TryGetFilename(part, state, property.Id, out text))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Filename) { TextValue = text };
                    return true;
                }

                case ThemePropertyType.Margins:
                {
                    ThemeMargins margins;
                    if (!style.TryGetMargins(part, state, property.Id, out margins))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Margins) { MarginsValue = margins };
                    return true;
                }

                case ThemePropertyType.Rect:
                {
                    Rectangle rect;
                    if (!style.TryGetRect(part, state, property.Id, out rect))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Rect) { RectValue = rect };
                    return true;
                }

                case ThemePropertyType.Position:
                {
                    Point point;
                    if (!style.TryGetPosition(part, state, property.Id, out point))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Position) { PointValue = point };
                    return true;
                }

                case ThemePropertyType.Size:
                {
                    // The TMT_SIZE rows represent the THEMESIZE pseudo properties
                    // TS_MIN/TS_TRUE/TS_DRAW (ids 0..2).  Anything else cannot be
                    // passed to GetThemePartSize.
                    if (property.Id < 0 || property.Id > 2)
                        return false;

                    Size size;
                    if (!style.TryGetPartSize(part, state, (ThemeSize)property.Id, out size))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Size) { SizeValue = size };
                    return true;
                }

                case ThemePropertyType.Font:
                {
                    ThemeFontInfo font;
                    if (!style.TryGetFont(part, state, property.Id, out font) &&
                        !style.TryGetSystemFont(property.Id, out font))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Font) { FontValue = font };
                    return true;
                }

                case ThemePropertyType.IntList:
                {
                    int[] list;
                    if (!style.TryGetIntList(part, state, property.Id, out list))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.IntList) { IntListValue = list };
                    return true;
                }

                case ThemePropertyType.Stream:
                {
                    int length;
                    if (!style.TryGetStream(part, state, property.Id, out length))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.Stream) { StreamLength = length };
                    return true;
                }

                case ThemePropertyType.DiskStream:
                {
                    int length;
                    if (!style.TryGetDiskStream(part, state, property.Id, out length))
                        return false;
                    value = new ThemePropertyValue(ThemePropertyType.DiskStream) { StreamLength = length };
                    return true;
                }

                default:
                    return false;
            }
        }
    }
}
