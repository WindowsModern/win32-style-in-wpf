using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThemeExplorer.Core
{
    /// <summary>
    /// One state of a theme part (for example "Hot" or "Pressed").
    /// <para xml:lang="zh-CN">主题部件的一个状态（例如 "Hot" 或 "Pressed"）。</para>
    /// </summary>
    public sealed class ThemeStateInfo
    {
        internal ThemeStateInfo(int id, string name, string displayName)
        {
            Id = id;
            Name = name;
            DisplayName = displayName;
        }

        /// <summary>
        /// Numeric state id used by the native API (the <c>xxx_S_*</c> constants).
        /// <para xml:lang="zh-CN">本机 API 使用的数字状态 id（<c>xxx_S_*</c> 常量）。</para>
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Raw state constant name from the Windows SDK headers.
        /// <para xml:lang="zh-CN">Windows SDK 头文件中的原始状态常量名。</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Short human readable name.
        /// <para xml:lang="zh-CN">简短的可读名称。</para>
        /// </summary>
        public string DisplayName { get; }
    }

    /// <summary>
    /// One part of a theme class (for example the "PushButton" part of "Button").
    /// <para xml:lang="zh-CN">主题类的一个部件（例如 "Button" 的 "PushButton" 部件）。</para>
    /// </summary>
    public sealed class ThemePartInfo
    {
        private static readonly IList<ThemeStateInfo> NoStates =
            new ReadOnlyCollection<ThemeStateInfo>(new ThemeStateInfo[0]);

        internal ThemePartInfo(int id, string name, string displayName, ThemeStateInfo[] states)
        {
            Id = id;
            Name = name;
            DisplayName = displayName;
            States = states == null || states.Length == 0
                ? NoStates
                : new ReadOnlyCollection<ThemeStateInfo>(states);
        }

        /// <summary>
        /// Numeric part id used by the native API (the <c>xxx_P_*</c> constants).
        /// <para xml:lang="zh-CN">本机 API 使用的数字部件 id（<c>xxx_P_*</c> 常量）。</para>
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Raw part constant name from the Windows SDK headers.
        /// <para xml:lang="zh-CN">Windows SDK 头文件中的原始部件常量名。</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Short human readable name.
        /// <para xml:lang="zh-CN">简短的可读名称。</para>
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// States defined for this part. The collection is never <c>null</c>.
        /// <para xml:lang="zh-CN">此部件定义的状态。集合永远不会是 <c>null</c>。</para>
        /// </summary>
        public IList<ThemeStateInfo> States { get; }
    }

    /// <summary>
    /// A visual style class, optionally restricted to a window subclass (for example "Toolbar" with the "ExplorerBar" subclass).
    /// <para xml:lang="zh-CN">视觉样式类，可选地限定到某个窗口子类（例如 "Toolbar" 的 "ExplorerBar" 子类）。</para>
    /// </summary>
    public sealed class ThemeClassInfo
    {
        internal ThemeClassInfo(string name, string subClass, ThemePartInfo[] parts)
        {
            Name = name;
            SubClass = subClass;
            Parts = new ReadOnlyCollection<ThemePartInfo>(parts);
            DisplayName = string.IsNullOrEmpty(subClass) ? name : name + " [" + subClass + "]";
        }

        /// <summary>
        /// Class name passed to <c>OpenThemeData</c>.
        /// <para xml:lang="zh-CN">传给 <c>OpenThemeData</c> 的类名。</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Window subclass passed to <c>SetWindowTheme</c>, or <c>null</c>.
        /// <para xml:lang="zh-CN">传给 <c>SetWindowTheme</c> 的窗口子类，或 <c>null</c>。</para>
        /// </summary>
        public string SubClass { get; }

        /// <summary>
        /// Name shown in the class tree.
        /// <para xml:lang="zh-CN">在类树中显示的名称。</para>
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Parts known for this class. The collection is never <c>null</c>.
        /// <para xml:lang="zh-CN">此类已知的部件。集合永远不会是 <c>null</c>。</para>
        /// </summary>
        public IList<ThemePartInfo> Parts { get; }
    }

    /// <summary>
    /// A <c>TMT_*</c> property that can be queried for any part/state pair.
    /// <para xml:lang="zh-CN">可按任意部件/状态查询的 <c>TMT_*</c> 属性。</para>
    /// </summary>
    public sealed class ThemePropertyInfo
    {
        internal ThemePropertyInfo(ThemePropertyType type, int id, string name, string displayName)
        {
            Type = type;
            Id = id;
            Name = name;
            DisplayName = displayName;
        }

        /// <summary>
        /// Data type of the property.
        /// <para xml:lang="zh-CN">属性的数据类型。</para>
        /// </summary>
        public ThemePropertyType Type { get; }

        /// <summary>
        /// Numeric property id (the value of the <c>TMT_*</c> constant).
        /// <para xml:lang="zh-CN">数字属性 id（<c>TMT_*</c> 常量的值）。</para>
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The <c>TMT_*</c> constant name.
        /// <para xml:lang="zh-CN"><c>TMT_*</c> 常量名。</para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Short human readable name.
        /// <para xml:lang="zh-CN">简短的可读名称。</para>
        /// </summary>
        public string DisplayName { get; }
    }
}
