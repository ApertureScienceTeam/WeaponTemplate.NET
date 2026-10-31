using GoldSrc.Amxmodx;

namespace Module;

// 元数据在 csproj 中配置;静态构造、native/hook/forward 注册和 17 个固定导出由源生成器生成。
//
// 移植自 WeaponTemplate.sma(【AST】加枪模板 v1.38, 作者 Fly):
//   fakemeta forward(FM_*)      -> [AmxxHook] metamod 函数表 hook
//   hamsandwich RegisterHam     -> plugin_init 里经生态 native 注册,回调经 bridge public 进入 C#
//   register_message/event      -> AMXX 核心 native + bridge public
//   fakemeta pev/engfunc/pdata  -> HLSDK 结构体直接访问 + pvPrivateData 单元读写
public static partial class Module
{
    static partial void Configure()
    {
        ModuleRuntime.OnAmxxAttach = () => MF.Log("加枪模板模块已加载 (WeaponTemplate v1.38, C# 移植版)");
        // 插件(含 bridge)每加载/重载一轮都会走到这里,作为注册轮次的起点
        ModuleRuntime.OnPluginsLoaded = Callbacks.ArmRegister;
        ModuleRuntime.OnPluginsUnloading = () => MF.Log("[diag] plugins unloading");
        ModuleRuntime.OnPluginsUnloaded = () => MF.Log("[diag] plugins unloaded");
    }
}
