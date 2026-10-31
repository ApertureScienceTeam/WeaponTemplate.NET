using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// hamsandwich 模块的生态 native 声明式导入(调用体由源生成器补全)。
// RegisterHam* 在 plugin_init 注册,ExecuteHam(B) 触发虚函数,SetHamParamFloat 改写 byref 参数。
internal static partial class HamSandwich
{
    [AmxxImportNative("RegisterHam")]
    public static partial void RegisterHam(int function, string className, string callback, int post);

    [AmxxImportNative("RegisterHamFromEntity")]
    public static partial void RegisterHamFromEntity(int function, int entity, string callback, int post);

    [AmxxImportNative("ExecuteHam")]
    public static partial int ExecuteHam(int function, int entity);

    // Ham_CsItemGetMaxSpeed 的 maxspeed 是 byref 标量,由 gamedll 写回
    [AmxxImportNative("ExecuteHam")]
    public static partial void ExecuteHamGetMaxSpeed(int function, int entity, out float maxSpeed);

    [AmxxImportNative("ExecuteHamB")]
    public static partial void ExecuteHamB(int function, int entity);

    // Ham_TakeDamage 的标量参数按 Pawn 地址传递,回调内可修改(SetHamParamFloat 依赖这一点)
    [AmxxImportNative("ExecuteHamB")]
    public static partial void ExecuteHamB(int function, int victim, ref int inflictor, ref int attacker, ref float damage, ref int damageType);

    [AmxxImportNative("SetHamParamFloat")]
    public static partial void SetHamParamFloat(int paramIndex, float value);

    // ---- 组合封装(基于以上导入) ----

    // ExecuteHamB(Ham_TakeDamage):标量参数按 Pawn 地址传递,回调内可修改(SetHamParamFloat 依赖这一点)
    public static void TakeDamage(int victim, int inflictor, int attacker, float damage, int damageType)
        => ExecuteHamB(EngineConst.HamTakeDamage, victim,
            ref inflictor, ref attacker, ref damage, ref damageType);
}
