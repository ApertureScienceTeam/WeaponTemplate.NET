using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// 插件生命周期入口,全部集中在本文件:
//   AMXX_PluginsLoaded -> ArmRegister(开启一轮注册窗口)
//   plugin_precache    -> OnPrecache(重置全局状态 + INI 配置加载)
//   plugin_init        -> OnPluginInit(消息/事件/Ham 注册)
// 各类游戏回调(Ham/消息/事件)在 Callbacks/ 目录,引擎函数表 hook 在 Hooks/ 目录。
public static partial class PluginLifecycle
{
    // Pawn 插件每加载一次只跑一遍 plugin_init,register_message/RegisterHam 的注册项也随插件卸载一起释放;
    // C# 模块常驻,plugin_init 可能被反复调用,逐次注册会把几百个 Ham/消息钩子叠加进去。
    // 因此以 AMXX_PluginsLoaded 为轮次起点,每轮只允许注册一次。
    // (AMXX 每局的顺序是 C_Spawn: 装插件 → 注册核心 forward → AMXX_PluginsLoaded → plugin_precache →
    //  ServerActivate_Post 里才 plugin_init;PluginsLoaded 是这一轮里最早、且必定先于两者到达的钩子,
    //  源生成器也正好在这里注册 [AmxxForward]。)
    private static int InitCount;
    private static int PrecacheCount;

    // AMXX_PluginsLoaded:插件(含 bridge)加载/重载完成,开一轮新的注册窗口。
    public static void ArmRegister()
    {
        EngineResources.Reset();
        MF.Log($"[diag] plugins loaded t={Eng.GameTime:F2} init={InitCount}");
        InitCount = 0;
    }

    [AmxxCallback("plugin_precache")]
    public static void OnPrecache()
    {
        MF.Log($"[diag] plugin_precache #{++PrecacheCount} t={Eng.GameTime:F2}");
        // Pawn 版每张地图重载插件即重置全局量;C# 模块常驻,这里显式重置
        WeaponStore.ResetAll();
        ConfigLoad.OnPrecache();
        MF.Log($"[diag] plugin_precache done templates={WeaponStore.WeaponAmount}");
    }

    [AmxxCallback("plugin_init")]
    public static void OnPluginInit()
    {
        int round = ++InitCount;
        // 第 1、2 次各记一条,之后每 50 次一条:既能看出重复频率,又不会把日志刷爆。
        if (round <= 2 || round % 50 == 0) MF.Log($"[diag] plugin_init #{round} t={Eng.GameTime:F2}");
        if (round == 2)
        {
            // 谁在重复调用 plugin_init:托管栈里若出现本模块的 Execute_Wpn*/NotifyLoadFiles,
            // 就是我们自己拿着 forward 句柄(0 = AMXX 的 plugin_init 广播)重播初始化;
            // 只到 Invoke_0 为止则是 AMXX/其它插件直接驱动的。
            MF.Log($"[diag] fwd loadFiles={ConfigLoad.LoadFilesForward} postFrame={NativeFunctionRegister.Fwd_WpnWeaponPostFrame} " +
                $"idle={NativeFunctionRegister.Fwd_WpnWeaponIdle} damage={NativeFunctionRegister.Fwd_WpnTakeDamage}\n{Environment.StackTrace}");
        }
        if (round > 1) return;

        // ---- 消息 / 事件注册(原版 register_message / register_event / register_logevent) ----
        AmxModX.RegisterMessage(Eng.UserMsgId("DeathMsg"), "wt_DeathMsg");
        AmxModX.RegisterMessage(Eng.UserMsgId("TextMsg"), "wt_TextMsg");
        AmxModX.RegisterMessage(EngineConst.SvcTempEntity, "wt_TempEntity");
        AmxModX.RegisterMessage(Eng.UserMsgId("StatusIcon"), "wt_StatusIcon");
        MF.RegisterEvent("CurWeapon", "wt_CurWeapon", "be", "1=1");
        MF.RegisterEvent("HLTV", "wt_RoundStart", "a", "1=0", "2=0");
        MF.RegisterLogEvent("wt_FreezeEnd", 2, "1=Round_Start");

        // ---- 原版武器后备弹药表 / 血色 ----
        // 原版循环上界是 sizeof g_szWeaponMaxBpammo(31 个元素),不能写成 32
        for (int i = 0; i < GameTables.GameWeaponMaxBpammoBase.Length; i++) WeaponStore.GameWeaponMaxBpammo[i] = GameTables.GameWeaponMaxBpammoBase[i];
        // 玩家倍率默认 1.0 由 PlayerData 构造时给出,不再给 32 个 id 建空数据
        WeaponStore.BloodColor = 247;
        WeaponStore.ApplyDefaultSoundVolumes();
        MF.Log($"[diag] state tables ready t={Eng.GameTime:F2}");

        // ---- Ham 注册(逐武器类名) ----
        MF.Log($"[diag] msg/event registered t={Eng.GameTime:F2}");
        for (int i = 1; i < GameTables.GameWeaponClassName.Length; i++)
        {
            string className = GameTables.GameWeaponClassName[i];
            if (className.Length == 0) continue;

            HamSandwich.RegisterHam(EngineConst.HamItemAddToPlayer, className, "wt_ItemAddToPlayerPost", 1);
            HamSandwich.RegisterHam(EngineConst.HamItemDeploy, className, "wt_ItemDeploy", 0);
            HamSandwich.RegisterHam(EngineConst.HamItemDeploy, className, "wt_ItemDeployPost", 1);
            HamSandwich.RegisterHam(EngineConst.HamItemHolster, className, "wt_ItemHolsterPost", 1);
            HamSandwich.RegisterHam(EngineConst.HamWeaponPrimaryAttack, className, "wt_WeaponPrimaryAttack", 0);
            HamSandwich.RegisterHam(EngineConst.HamWeaponPrimaryAttack, className, "wt_WeaponPrimaryAttackPost", 1);
            HamSandwich.RegisterHam(EngineConst.HamWeaponSecondaryAttack, className, "wt_WeaponSecondaryAttack", 0);
            HamSandwich.RegisterHam(EngineConst.HamWeaponSecondaryAttack, className, "wt_WeaponSecondaryAttackPost", 1);
            HamSandwich.RegisterHam(EngineConst.HamItemPostFrame, className, "wt_ItemPostFrame", 0);
            HamSandwich.RegisterHam(EngineConst.HamWeaponReload, className, "wt_WeaponReload", 0);
            HamSandwich.RegisterHam(EngineConst.HamWeaponReload, className, "wt_WeaponReloadPost", 1);
            HamSandwich.RegisterHam(EngineConst.HamWeaponWeaponIdle, className, "wt_WeaponWeaponIdle", 0);
        }
        MF.Log($"[diag] per-weapon ham registered t={Eng.GameTime:F2}");
        HamSandwich.RegisterHam(EngineConst.HamUse, "func_tank", "wt_UseStationaryPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamUse, "func_tankmortar", "wt_UseStationaryPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamUse, "func_tankrocket", "wt_UseStationaryPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamUse, "func_tanklaser", "wt_UseStationaryPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTakeDamage, "player", "wt_TakeDamage", 0);
        HamSandwich.RegisterHam(EngineConst.HamTakeDamage, "player", "wt_TakeDamagePost", 1);
        HamSandwich.RegisterHam(EngineConst.HamKilled, "player", "wt_KilledPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamThink, "grenade", "wt_ThinkGrenade", 0);
        HamSandwich.RegisterHam(EngineConst.HamGiveAmmo, "player", "wt_GiveAmmo", 0);
        HamSandwich.RegisterHam(EngineConst.HamSpawn, "player", "wt_PlayerSpawnPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTraceAttack, "worldspawn", "wt_TraceAttackPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTraceAttack, "func_breakable", "wt_TraceAttackPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTraceAttack, "func_wall", "wt_TraceAttackPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTraceAttack, "func_door", "wt_TraceAttackPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTraceAttack, "func_door_rotating", "wt_TraceAttackPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTraceAttack, "func_plat", "wt_TraceAttackPost", 1);
        HamSandwich.RegisterHam(EngineConst.HamTraceAttack, "func_rotating", "wt_TraceAttackPost", 1);
        MF.Log($"[diag] plugin_init done t={Eng.GameTime:F2}");
    }
}
