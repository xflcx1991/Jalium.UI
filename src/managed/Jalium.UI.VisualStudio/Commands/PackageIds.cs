using System;

namespace Jalium.UI.VisualStudio.Commands
{
    /// <summary>
    /// 定义命令和菜单的 GUID 和 ID
    /// </summary>
    internal static class PackageIds
    {
        // 命令组 GUID
        public const string guidJaliumCmdSetString = "a1b2c3d4-5678-90ab-cdef-123456789abc";
        public static readonly Guid guidJaliumCmdSet = new Guid(guidJaliumCmdSetString);

        // 命令组 ID
        public const int JaliumMenuGroup = 0x1020;
        public const int JaliumContextMenuGroup = 0x1021;

        // 命令 ID
        public const int CompileJalxamlCommandId = 0x0100;
        public const int CompileAllJalxamlCommandId = 0x0101;
    }
}
