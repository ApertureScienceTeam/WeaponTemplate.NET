using GoldSrc.Amxmodx;

namespace Module;

// 静态构造、native/hook/forward 注册和固定导出均由源生成器生成。
public static partial class Module
{
    static partial void Configure()
    {
        ModuleRuntime.OnPluginsLoaded = WeaponExtend.ResetAll;
    }
}
