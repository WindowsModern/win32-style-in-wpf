using System;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using DrawingColor = System.Drawing.Color;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Creates a ToolTip Close button image resource from the current Win32 visual style.
    /// When <see cref="PreserveAlpha"/> is enabled, every state uses alpha reconstruction;
    /// the normal, hot and pressed resources all enable it.
    /// <para xml:lang="zh-CN">
    /// 从当前 Win32 视觉样式创建 ToolTip 关闭按钮图片资源。启用 <see cref="PreserveAlpha"/> 时，
    /// 所有状态都使用像素透明重建；普通、热和按下资源都启用了它。
    /// </para>
    /// </summary>
    public sealed class ThemeToolTipCloseImage : MarkupExtension
    {
        /// <summary>
        /// Close-button state: 1 normal, 2 hot, 3 pressed.
        /// <para xml:lang="zh-CN">关闭按钮状态：1 普通，2 热，3 按下。</para>
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// Reconstruct pixel alpha for this state. Enabled by all three Close resources.
        /// <para xml:lang="zh-CN">是否为该状态重建像素透明。三个 Close 资源都启用了它。</para>
        /// </summary>
        public bool PreserveAlpha { get; set; }

        /// <summary>
        /// Returns the themed close image, or null when visual styles are unavailable.
        /// <para xml:lang="zh-CN">返回主题关闭按钮图片；视觉样式不可用时返回 null。</para>
        /// </summary>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return CreateImage(State <= 0 ? 1 : State, PreserveAlpha);
        }

        internal static BitmapSource CreateImage(int state, bool preserveAlpha)
        {
            ThemeImageData data;

            if (preserveAlpha)
            {
                data = ThemeImageProvider.GetAlphaNatural("Tooltip", 5, state, IntPtr.Zero);
            }
            else
            {
                System.Windows.Media.Color fill = ThemeColors.ToolTipEdgeFill;
                DrawingColor background = DrawingColor.FromArgb(fill.A, fill.R, fill.G, fill.B);
                data = ThemeImageProvider.GetNatural("Tooltip", 5, state, IntPtr.Zero, null, background);
            }

            return data == null ? null : data.Image;
        }
    }
}
