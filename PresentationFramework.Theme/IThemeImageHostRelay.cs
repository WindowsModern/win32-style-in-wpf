using System.Windows.Controls;

namespace Microsoft.Windows.Themes
{
    /// <summary>
    /// Implemented by a container that carries <see cref="ThemeImage"/> descriptors on behalf of a
    /// child <see cref="HackImage"/> which does the actual loading.
    /// <para xml:lang="zh-CN">
    /// 由"替子 <see cref="HackImage"/> 承载 <see cref="ThemeImage"/> 描述符"的容器实现。
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para xml:lang="zh-CN">
    /// 有这个接口的原因：描述符写在容器上（主题 XAML 里出现的是容器），
    /// 但加载与过渡只能发生在真 <see cref="HackImage"/> 上
    /// （<c>ThemeImageHost</c> 硬转型、<c>HackImageTransition</c> 只接受它）。
    /// 于是描述符变更的通知会到达容器，而宿主必须被刷新在子元素上。
    /// 这个接口就是"请刷新哪一个"的答案。
    /// </para>
    /// <para xml:lang="zh-CN">
    /// 内部接口：不构成公开 API，也不会被外部程序集实现。
    /// </para>
    /// </remarks>
    internal interface IThemeImageHostRelay
    {
        /// <summary>
        /// The element that should own the <c>ThemeImageHost</c>, or <c>null</c> when the
        /// container has no art element yet.
        /// </summary>
        HackImage ThemeImageTarget { get; }
    }
}
