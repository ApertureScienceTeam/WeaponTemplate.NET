using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// hamsandwich / register_message / register_event 回调的 C# 入口。
// Ham 回调返回 HAM_IGNORED(1)/HAM_SUPERCEDE(4);消息回调返回 PLUGIN_CONTINUE(0)/PLUGIN_HANDLED(1)。
// 全部 hook 在 plugin_init 中经生态 native 注册(与原版 plugin_init 相同的注册集合)。
public static unsafe class Callbacks
{
    // 原版 KNIFE_COMMAND 等购买命令常量
    private const string KnifeCommand = "normal_knife";

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

    // ---- 生命周期 ----

    [AmxxCallback("plugin_precache")]
    public static void OnPrecache()
    {
        MF.Log($"[diag] plugin_precache #{++PrecacheCount} t={Eng.GameTime:F2}");
        // Pawn 版每张地图重载插件即重置全局量;C# 模块常驻,这里显式重置
        Store.ResetAll();
        ConfigLoad.OnPrecache();
        MF.Log($"[diag] plugin_precache done templates={Store.WeaponAmount}");
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
        MF.CallNative("register_message", Eng.UserMsgId("DeathMsg"), "wt_DeathMsg");
        MF.CallNative("register_message", Eng.UserMsgId("TextMsg"), "wt_TextMsg");
        MF.CallNative("register_message", EngineConst.SvcTempEntity, "wt_TempEntity");
        MF.CallNative("register_message", Eng.UserMsgId("StatusIcon"), "wt_StatusIcon");
        MF.RegisterEvent("CurWeapon", "wt_CurWeapon", "be", "1=1");
        MF.RegisterEvent("HLTV", "wt_RoundStart", "a", "1=0", "2=0");
        MF.RegisterLogEvent("wt_FreezeEnd", 2, "1=Round_Start");

        // ---- 原版武器后备弹药表 / 血色 ----
        // 原版循环上界是 sizeof g_szWeaponMaxBpammo(31 个元素),不能写成 32
        for (int i = 0; i < GameTables.GameWeaponMaxBpammoBase.Length; i++) Store.GameWeaponMaxBpammo[i] = GameTables.GameWeaponMaxBpammoBase[i];
        // 玩家倍率默认 1.0 由 PlayerData 构造时给出,不再给 32 个 id 建空数据
        Store.BloodColor = 247;
        Core.ApplyDefaultSoundVolumes();
        MF.Log($"[diag] state tables ready t={Eng.GameTime:F2}");

        // ---- Ham 注册(逐武器类名) ----
        MF.Log($"[diag] msg/event registered t={Eng.GameTime:F2}");
        for (int i = 1; i < GameTables.GameWeaponClassName.Length; i++)
        {
            string className = GameTables.GameWeaponClassName[i];
            if (className.Length == 0) continue;

            MF.CallNative("RegisterHam", EngineConst.HamItemAddToPlayer, className, "wt_ItemAddToPlayerPost", 1);
            MF.CallNative("RegisterHam", EngineConst.HamItemDeploy, className, "wt_ItemDeploy", 0);
            MF.CallNative("RegisterHam", EngineConst.HamItemDeploy, className, "wt_ItemDeployPost", 1);
            MF.CallNative("RegisterHam", EngineConst.HamItemHolster, className, "wt_ItemHolsterPost", 1);
            MF.CallNative("RegisterHam", EngineConst.HamWeaponPrimaryAttack, className, "wt_WeaponPrimaryAttack", 0);
            MF.CallNative("RegisterHam", EngineConst.HamWeaponPrimaryAttack, className, "wt_WeaponPrimaryAttackPost", 1);
            MF.CallNative("RegisterHam", EngineConst.HamWeaponSecondaryAttack, className, "wt_WeaponSecondaryAttack", 0);
            MF.CallNative("RegisterHam", EngineConst.HamWeaponSecondaryAttack, className, "wt_WeaponSecondaryAttackPost", 1);
            MF.CallNative("RegisterHam", EngineConst.HamItemPostFrame, className, "wt_ItemPostFrame", 0);
            MF.CallNative("RegisterHam", EngineConst.HamWeaponReload, className, "wt_WeaponReload", 0);
            MF.CallNative("RegisterHam", EngineConst.HamWeaponReload, className, "wt_WeaponReloadPost", 1);
            MF.CallNative("RegisterHam", EngineConst.HamWeaponWeaponIdle, className, "wt_WeaponWeaponIdle", 0);
        }
        MF.Log($"[diag] per-weapon ham registered t={Eng.GameTime:F2}");
        MF.CallNative("RegisterHam", EngineConst.HamUse, "func_tank", "wt_UseStationaryPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamUse, "func_tankmortar", "wt_UseStationaryPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamUse, "func_tankrocket", "wt_UseStationaryPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamUse, "func_tanklaser", "wt_UseStationaryPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTakeDamage, "player", "wt_TakeDamage", 0);
        MF.CallNative("RegisterHam", EngineConst.HamTakeDamage, "player", "wt_TakeDamagePost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamKilled, "player", "wt_KilledPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamThink, "grenade", "wt_ThinkGrenade", 0);
        MF.CallNative("RegisterHam", EngineConst.HamGiveAmmo, "player", "wt_GiveAmmo", 0);
        MF.CallNative("RegisterHam", EngineConst.HamSpawn, "player", "wt_PlayerSpawnPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTraceAttack, "worldspawn", "wt_TraceAttackPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTraceAttack, "func_breakable", "wt_TraceAttackPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTraceAttack, "func_wall", "wt_TraceAttackPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTraceAttack, "func_door", "wt_TraceAttackPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTraceAttack, "func_door_rotating", "wt_TraceAttackPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTraceAttack, "func_plat", "wt_TraceAttackPost", 1);
        MF.CallNative("RegisterHam", EngineConst.HamTraceAttack, "func_rotating", "wt_TraceAttackPost", 1);
        MF.Log($"[diag] plugin_init done t={Eng.GameTime:F2}");
    }

    // ---- 事件回调 ----

    [AmxxCallback("wt_CurWeapon")]
    public static void OnCurWeaponEvent(int id) => Core.EventCurWeapon(id);

    [AmxxCallback("wt_RoundStart")]
    public static void OnRoundStartEvent(int id) => Core.EventRoundStart();

    [AmxxCallback("wt_FreezeEnd")]
    public static void OnFreezeEnd() => Store.RoundStart = true;

    // ---- 消息回调 ----

    [AmxxCallback("wt_DeathMsg")]
    public static int OnDeathMsg(int msgId, int msgDest, int msgEntity)
    {
        int id = MF.CallNative("get_msg_arg_int", 1);
        int victim = MF.CallNative("get_msg_arg_int", 2);
        int headshot = MF.CallNative("get_msg_arg_int", 3);
        var weaponArg = MF.OutString(32);
        MF.CallNative("get_msg_arg_string", 4, weaponArg, 31);
        string weapon = weaponArg.Value ?? "";

        if (id == 0)
        {
            int result = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, weapon);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            return EngineConst.PluginContinue;
        }

        // 手雷击杀图标
        if (weapon == "grenade")
        {
            if (Store.Player(id).UserGrenadeId != 0)
            {
                string spr = Store.Weapon(id, Store.Player(id).UserGrenadeId).Spr;
                MF.CallNative("set_msg_arg_string", 4, spr);
                int result = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, spr);
                if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
                return EngineConst.PluginContinue;
            }
            int result2 = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, weapon);
            if (result2 == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            return EngineConst.PluginContinue;
        }

        // 取一下刀子攻击时的实体
        int entityIndex = 0;
        if (Store.Player(id).AttackerEntity != 0)
        {
            entityIndex = Store.Player(id).AttackerEntity;
            Store.Player(id).AttackerEntity = 0;
        }
        else
        {
            // debug处理，防止特殊击杀时出现武器击杀图标
            var pPlayer = Eng.EntOfIndex(id);
            for (int j = 0; j < GameTables.GameWeaponKillSpr.Length; j++)
            {
                if (GameTables.GameWeaponKillSpr[j].Length == 0) continue;
                if (GameTables.GameWeaponKillSpr[j] != weapon) continue;
                entityIndex = pPlayer == null ? 0 : Eng.IndexOf(Eng.GetPDataCbase(pPlayer, 373));
            }
        }
        if (entityIndex <= 0)
        {
            int result = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, weapon);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            return EngineConst.PluginContinue;
        }
        int i = Core.WeaponTypeByIndex(entityIndex);
        if (i == 0)
        {
            int result = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, weapon);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            return EngineConst.PluginContinue;
        }
        string killSpr = Store.Weapon(id, i).Spr;
        MF.CallNative("set_msg_arg_string", 4, killSpr);
        int finalResult = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, killSpr);
        if (finalResult == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
        return EngineConst.PluginContinue;
    }

    [AmxxCallback("wt_TextMsg")]
    public static int OnTextMsg(int msgId, int msgDest, int msgEntity)
    {
        var textArg = MF.OutString(32);
        MF.CallNative("get_msg_arg_string", 2, textArg, 31);
        string text = textArg.Value ?? "";

        // 重置弹药
        if (text == "#Game_will_restart_in" || text == "#Game_Commencing") Store.RoundRestart = true;
        return EngineConst.PluginContinue;
    }

    [AmxxCallback("wt_TempEntity")]
    public static int OnTempEntity(int msgId, int msgDest, int msgEntity)
    {
        if (Store.BloodColor == 0) return EngineConst.PluginContinue;

        // 设置血液颜色
        int first = MF.CallNative("get_msg_arg_int", 1);
        if (first == EngineConst.TeBloodSprite || first == EngineConst.TeBlood || first == EngineConst.TeBloodstream)
        {
            int color = MF.CallNative("get_msg_arg_int", 7);
            int result = NativeFunctionRegister.Execute_WpnHookSpawnBlood(color);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            MF.CallNative("set_msg_arg_int", 7, EngineConst.ArgByte, Store.BloodColor);
        }
        return EngineConst.PluginContinue;
    }

    [AmxxCallback("wt_StatusIcon")]
    public static int OnStatusIcon(int msgId, int msgDest, int msgEntity)
    {
        var statusArg = MF.OutString(33);
        MF.CallNative("get_msg_arg_string", 2, statusArg, 32);
        string statusName = statusArg.Value ?? "";

        // 出现buyzone图标时说明BOT在购买区
        if (statusName == "buyzone")
        {
            if (!Eng.IsConnected(msgEntity)) return EngineConst.PluginContinue;
            if (!Eng.IsBot(msgEntity)) return EngineConst.PluginContinue;
            if (!Store.Player(msgEntity).BotBuyWeapon) return EngineConst.PluginContinue;
            Store.Player(msgEntity).BotNextThink = Eng.GameTime + 0.1f;
        }
        return EngineConst.PluginContinue;
    }

    // ---- Ham 回调 ----

    [AmxxCallback("wt_ItemAddToPlayerPost")]
    public static void OnItemAddToPlayerPost(int entityIndex, int id)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        if (Eng.GetPDataInt(entity, 43) == EngineConst.CswC4) return;
        Core.ChangeWeaponList(id, entity);
    }

    [AmxxCallback("wt_ItemDeploy")]
    public static int OnItemDeploy(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        // AWP无法设置切枪时间的bug
        Store.Player(id).UserDeploy[0] = Eng.GetPDataFloat(entity, 46);
        Store.Player(id).UserDeploy[1] = Eng.GetPDataFloat(entity, 47);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_ItemDeployPost")]
    public static void OnItemDeployPost(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);

        if (i == 0)
        {
            // 记录原版武器备弹
            int weapon = Eng.GetPDataInt(entity, 43);
            const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            if (((1 << weapon) & specialBitSum) == 0) Core.SetUserBpAmmo(id, weapon, Store.Player(id).GameBpAmmo[weapon]);
            return;
        }

        // 记录新武器备弹
        if (Store.Weapon(id, i).Id != EngineConst.CswHegrenade && Store.Weapon(id, i).Id != EngineConst.CswKnife)
            Core.SetUserBpAmmo(id, Store.Weapon(id, i).Id, Store.Player(id).BPAmmo[i]);
        Core.ChangeWeaponList(id, entity);
        Eng.SetViewModel(pPlayer, Store.Weapon(id, i).VModel);
        Eng.SetWeaponModel(pPlayer, Store.Weapon(id, i).PModel);
        if (Store.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            Core.PlayAnim(id, Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw]);
            Eng.EmitSound(pPlayer, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound1, EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
        }
        else if (Store.Weapon(id, i).Id != EngineConst.CswHegrenade)
        {
            if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
                Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimDrawSilenced]);
            else
                Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimDraw]);
        }
        Eng.SetPDataFloat(pPlayer, 83, Store.Weapon(id, i).DrawTime);
        Eng.SetPDataFloat(entity, 46, Store.Player(id).UserDeploy[0]);
        Eng.SetPDataFloat(entity, 47, Store.Player(id).UserDeploy[1]);
        if (Store.Weapon(id, i).Clip == 0 && Store.Weapon(id, i).Id != EngineConst.CswHegrenade && Store.Weapon(id, i).Id != EngineConst.CswKnife)
        {
            int clip = Eng.GetPDataInt(entity, 51);
            Core.SetUserBpAmmo(id, Store.Weapon(id, i).Id, clip);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("CurWeapon"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.WriteByte(Store.Weapon(id, i).Id);
            Eng.WriteByte(0xFF); // -1
            Eng.MessageEnd();
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoX"), null, pPlayer);
            Eng.WriteByte(GameTables.GameWeaponAmmoId[Store.Weapon(id, i).Id]);
            Eng.WriteByte(clip);
            Eng.MessageEnd();
        }
        else
        {
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("CurWeapon"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.WriteByte(Store.Weapon(id, i).Id);
            Eng.WriteByte(Eng.GetPDataInt(entity, 51));
            Eng.MessageEnd();
        }
        if (Store.Weapon(id, i).Sight != 0)
        {
            Eng.SetPDataInt(pPlayer, 363, 90);
            Store.Player(id).SightMode = WpnConst.Closed;
            Eng.SetPDataInt(pPlayer, 361, Eng.GetPDataInt(pPlayer, 361) & ~(1 << 6));
            Store.Player(id).Reloading = false;
        }
        NativeFunctionRegister.Execute_WpnWeaponDraw(entityIndex, id, i);
    }

    [AmxxCallback("wt_ItemHolsterPost")]
    public static void OnItemHolsterPost(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (i != 0 && Store.Weapon(id, i).Sight != 0)
        {
            Eng.SetPDataInt(pPlayer, 363, 90);
            Store.Player(id).SightMode = WpnConst.Closed;
            Eng.SetPDataInt(pPlayer, 361, Eng.GetPDataInt(pPlayer, 361) & ~(1 << 6));
            Store.Player(id).Reloading = false;
        }

        // 收起武器时记录备用子弹
        int weapon = Eng.GetPDataInt(entity, 43);
        const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
        if (((1 << weapon) & specialBitSum) == 0)
        {
            if (i == 0) Store.Player(id).GameBpAmmo[weapon] = Core.GetUserBpAmmo(id, weapon);
            else Store.Player(id).BPAmmo[i] = Core.GetUserBpAmmo(id, Store.Weapon(id, i).Id);
        }
    }

    [AmxxCallback("wt_WeaponPrimaryAttack")]
    public static int OnWeaponPrimaryAttack(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        if (Store.Weapon(id, i).Id == EngineConst.CswHegrenade) return EngineConst.HamIgnored;

        if (Store.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            int preResult = NativeFunctionRegister.Execute_WpnPrimaryPreAttack(entityIndex, id, i);
            if (preResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

            if (Store.Weapon(id, i).KnifeSlashTime == 0.0f)
            {
                Store.Player(id).KnifeSlashMode = Store.Player(id).KnifeSlashMode != 0 ? 0 : 1;
                Store.Player(id).KnifeTempDamage = Store.Weapon(id, i).Damage;
                if (Store.Player(id).UserKnifeAttack == 0) Store.Player(id).UserKnifeAttack = 1;
                NativeFunctionRegister.Execute_WpnPrimaryPreAttackPost(entityIndex, id, i);
                return EngineConst.HamIgnored;
            }
            else
            {
                if (Store.Player(id).KnifeAttack != 0) return EngineConst.HamSupercede;

                Store.Player(id).KnifeSlashMode = Store.Player(id).KnifeSlashMode != 0 ? 0 : 1;
                Store.Player(id).KnifeAttack = 1;
                Attack.KnifeAttack(id, i, Store.Player(id).KnifeAttack, entity);
            }
            NativeFunctionRegister.Execute_WpnPrimaryPreAttackPost(entityIndex, id, i);
            return EngineConst.HamSupercede;
        }

        int result = NativeFunctionRegister.Execute_WpnPrimaryPreAttack(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (Eng.GetPDataInt(entity, 55) != 0) Eng.SetPDataInt(entity, 55, 0);
        if (result == WpnConst.WPN_HANDLED)
        {
            // WPN_HANDLED:打断原版攻击并执行一次假攻击
            if (Eng.GetPDataInt(entity, 51) <= 0) return EngineConst.HamIgnored;

            bool silenced = (Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0;

            var push = new float[] { 0.0f, 0.0f, 0.0f };
            if (silenced)
            {
                if (Eng.RandomNum(0, 1) != 0)
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound3, EngineConst.VolNorm, Store.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
                else
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound4, EngineConst.VolNorm, Store.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
            }
            else
            {
                if (Eng.RandomNum(0, 1) != 0)
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound1, EngineConst.VolNorm, Store.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
                else
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound2, EngineConst.VolNorm, Store.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
            }
            if (Core.GetUserZoom(id) != 1 || Store.Player(id).SightMode == WpnConst.Open)
            {
                push[0] = -1.0f * Store.Weapon(id, i).AimRecoil;
                Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).AimSpeed);
                Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).AimSpeed);
            }
            else if (Core.GetUserZoom(id) == 1 && Store.Player(id).SightMode != WpnConst.Open)
            {
                push[0] = -1.0f * Store.Weapon(id, i).Recoil;
                Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).Speed);
                Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).Speed);
            }
            else if (silenced)
            {
                push[0] = -1.0f * Store.Weapon(id, i).SilencedRecoil;
                Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).SilencedSpeed);
                Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).SilencedSpeed);
            }
            pPlayer->v.punchangle = new vec3_t { x = push[0], y = push[1], z = push[2] };
            if (silenced) Core.PlayAnim(id, Store.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1Silenced, WpnConst.AnimShoot3Silenced)]);
            else if (Store.Weapon(id, i).Id == EngineConst.CswElite)
            {
                if (Store.Player(id).EliteShotNumber == 0)
                {
                    Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimShoot1]);
                    Store.Player(id).EliteShotNumber = 1;
                }
                else
                {
                    Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimShoot2]);
                    Store.Player(id).EliteShotNumber = 0;
                }
            }
            else Core.PlayAnim(id, Store.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1, WpnConst.AnimShoot3)]);
            if (Eng.GetPDataInt(entity, 51) > 0)
            {
                int postResult = NativeFunctionRegister.Execute_WpnPrimaryPostAttack(entityIndex, id, i);
                if (postResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;
            }
            Eng.SetPDataInt(entity, 51, Eng.GetPDataInt(entity, 51) - 1);
            NativeFunctionRegister.Execute_WpnPrimaryPostAttackPost(entityIndex, id, i);
            return EngineConst.HamSupercede;
        }
        if (Store.Weapon(id, i).Id == EngineConst.CswM3 || Store.Weapon(id, i).Id == EngineConst.CswXm1014)
        {
            if (Store.Player(id).SightMode == WpnConst.AtoB || Store.Player(id).SightMode == WpnConst.BtoA)
                return EngineConst.HamSupercede;
        }
        Store.IsInPrimaryAttack = true;
        Store.Player(id).PushAngle[0] = pPlayer->v.punchangle.x;
        Store.Player(id).PushAngle[1] = pPlayer->v.punchangle.y;
        Store.Player(id).PushAngle[2] = pPlayer->v.punchangle.z;
        Store.Player(id).ClipAmmo = Eng.GetPDataInt(entity, 51);
        NativeFunctionRegister.Execute_WpnPrimaryPreAttackPost(entityIndex, id, i);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponPrimaryAttackPost")]
    public static void OnWeaponPrimaryAttackPost(int entityIndex)
    {
        bool wasInPrimaryAttack = Store.IsInPrimaryAttack;
        Store.IsInPrimaryAttack = false;
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (i == 0) return;

        if (Store.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (Store.Weapon(id, i).KnifeSlashTime == 0.0f)
            {
                Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).KnifeSlashResetTime);
                Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).KnifeSlashResetTime);
            }
            return;
        }
        if (!wasInPrimaryAttack) return;

        if (Eng.GetPDataInt(entity, 51) > 0)
        {
            int preResult = NativeFunctionRegister.Execute_WpnPrimaryPostAttack(entityIndex, id, i);
            if (preResult == WpnConst.WPN_SUPERCEDE) return;
        }
        if (Store.Player(id).ClipAmmo == 0 || Store.Player(id).ClipAmmo == Eng.GetPDataInt(entity, 51)) return;

        bool silenced = (Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0;

        var push = new float[] { pPlayer->v.punchangle.x, pPlayer->v.punchangle.y, pPlayer->v.punchangle.z };
        push[0] -= Store.Player(id).PushAngle[0];
        push[1] -= Store.Player(id).PushAngle[1];
        push[2] -= Store.Player(id).PushAngle[2];
        if (silenced)
        {
            if (Eng.RandomNum(0, 1) != 0)
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound3, EngineConst.VolNorm, Store.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
            else
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound4, EngineConst.VolNorm, Store.Weapon(id, i).SilenceFireSoundVolume, 0, EngineConst.PitchNorm);
        }
        else
        {
            if (Eng.RandomNum(0, 1) != 0)
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound1, EngineConst.VolNorm, Store.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
            else
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.Weapon(id, i).Sound2, EngineConst.VolNorm, Store.Weapon(id, i).FireSoundVolume, 0, EngineConst.PitchNorm);
        }
        if ((Core.GetUserZoom(id) != 1 || Store.Player(id).SightMode == WpnConst.Open) && !silenced)
        {
            push[0] *= Store.Weapon(id, i).AimRecoil;
            push[1] *= Store.Weapon(id, i).AimRecoil;
            push[2] *= Store.Weapon(id, i).AimRecoil;
            Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).AimSpeed);
            Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).AimSpeed);
        }
        else if (Core.GetUserZoom(id) == 1 && Store.Player(id).SightMode != WpnConst.Open && !silenced)
        {
            push[0] *= Store.Weapon(id, i).Recoil;
            push[1] *= Store.Weapon(id, i).Recoil;
            push[2] *= Store.Weapon(id, i).Recoil;
            Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).Speed);
            Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).Speed);
        }
        else if (silenced)
        {
            push[0] *= Store.Weapon(id, i).SilencedRecoil;
            push[1] *= Store.Weapon(id, i).SilencedRecoil;
            push[2] *= Store.Weapon(id, i).SilencedRecoil;
            Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).SilencedSpeed);
            Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).SilencedSpeed);
        }
        push[0] += Store.Player(id).PushAngle[0];
        push[1] += Store.Player(id).PushAngle[1];
        push[2] += Store.Player(id).PushAngle[2];
        pPlayer->v.punchangle = new vec3_t { x = push[0], y = push[1], z = push[2] };
        if (silenced) Core.PlayAnim(id, Store.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1Silenced, WpnConst.AnimShoot3Silenced)]);
        else if (Store.Weapon(id, i).Id == EngineConst.CswElite)
        {
            if (Store.Player(id).EliteShotNumber == 0)
            {
                Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimShoot1]);
                Store.Player(id).EliteShotNumber = 1;
            }
            else
            {
                Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimShoot2]);
                Store.Player(id).EliteShotNumber = 0;
            }
        }
        else Core.PlayAnim(id, Store.Weapon(id, i).Anim[Eng.RandomNum(WpnConst.AnimShoot1, WpnConst.AnimShoot3)]);
        NativeFunctionRegister.Execute_WpnPrimaryPostAttackPost(entityIndex, id, i);
    }

    [AmxxCallback("wt_WeaponSecondaryAttack")]
    public static int OnWeaponSecondaryAttack(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        if (Store.Weapon(id, i).Id == EngineConst.CswHegrenade) return EngineConst.HamIgnored;

        int result = NativeFunctionRegister.Execute_WpnSecondaryPreAttack(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (Store.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (Store.Weapon(id, i).KnifeStabTime == 0.0f)
            {
                Store.Player(id).KnifeTempDamage = Store.Weapon(id, i).Damage2;
                if (Store.Player(id).UserKnifeAttack == 0) Store.Player(id).UserKnifeAttack = 2;
                NativeFunctionRegister.Execute_WpnSecondaryPreAttackPost(entityIndex, id, i);
                return EngineConst.HamIgnored;
            }
            else
            {
                if (Store.Player(id).KnifeAttack != 0) return EngineConst.HamSupercede;

                Store.Player(id).KnifeAttack = 3;
                Attack.KnifeAttack(id, i, Store.Player(id).KnifeAttack, entity);
            }
            NativeFunctionRegister.Execute_WpnSecondaryPreAttackPost(entityIndex, id, i);
            return EngineConst.HamSupercede;
        }
        if (Store.Weapon(id, i).Sight != 0) return EngineConst.HamSupercede;

        if (Store.Weapon(id, i).Id is EngineConst.CswM4a1 or EngineConst.CswUsp or EngineConst.CswFamas or EngineConst.CswGlock18)
            return EngineConst.HamSupercede;

        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponSecondaryAttackPost")]
    public static void OnWeaponSecondaryAttackPost(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (i == 0) return;

        int result = NativeFunctionRegister.Execute_WpnSecondaryPostAttack(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (Store.Weapon(id, i).Id == EngineConst.CswKnife)
        {
            if (Store.Weapon(id, i).KnifeStabTime == 0.0f)
            {
                Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).KnifeStabResetTime);
                Eng.SetPDataFloat(entity, 47, Store.Weapon(id, i).KnifeStabResetTime);
            }
            return;
        }
        if (Store.Weapon(id, i).Id == EngineConst.CswHegrenade) return;

        if (Store.Weapon(id, i).Sight != 0) return;

        if (Core.GetUserZoom(id) == 2 || Core.GetUserZoom(id) == 3) Eng.SetViewModel(pPlayer, "");
        else
        {
            Eng.SetViewModel(pPlayer, Store.Weapon(id, i).VModel);
            Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimIdle]);
        }
        if (Store.Weapon(id, i).Id == EngineConst.CswM4a1 || Store.Weapon(id, i).Id == EngineConst.CswUsp)
        {
            if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
            {
                if (Store.Weapon(id, i).Id == EngineConst.CswM4a1) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) & ~(1 << 2));
                else if (Store.Weapon(id, i).Id == EngineConst.CswUsp) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) & ~(1 << 0));
                Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimDetachSilencer]);
                Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).SilencedTime[1]);
                Eng.SetPDataFloat(entity, 47, Store.Weapon(id, i).SilencedTime[1]);
                Eng.SetPDataFloat(entity, 48, Store.Weapon(id, i).SilencedTime[1]);
                Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).SilencedTime[1]);
            }
            else
            {
                if (Store.Weapon(id, i).Id == EngineConst.CswM4a1) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) | (1 << 2));
                else if (Store.Weapon(id, i).Id == EngineConst.CswUsp) Eng.SetPDataInt(entity, 74, Eng.GetPDataInt(entity, 74) | (1 << 0));
                Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimAttachSilencer]);
                Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).SilencedTime[0]);
                Eng.SetPDataFloat(entity, 47, Store.Weapon(id, i).SilencedTime[0]);
                Eng.SetPDataFloat(entity, 48, Store.Weapon(id, i).SilencedTime[0]);
                Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).SilencedTime[0]);
            }
        }
        NativeFunctionRegister.Execute_WpnSecondaryPostAttackPost(entityIndex, id, i);
    }

    [AmxxCallback("wt_ItemPostFrame")]
    public static int OnItemPostFrame(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return EngineConst.HamIgnored;

        if (i == 0)
        {
            // 记录原版武器的换弹
            int weapon = Eng.GetPDataInt(entity, 43);
            const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            if (((1 << weapon) & specialBitSum) == 0)
            {
                if (weapon != EngineConst.CswM3 && weapon != EngineConst.CswXm1014)
                {
                    if (Eng.GetPDataInt(entity, 54) != 0 && Eng.GetPDataFloat(id, 83) <= 0.0f)
                        Store.Player(id).GameBpAmmo[weapon] = Core.GetUserBpAmmo(id, weapon);
                }
                else if (Eng.GetPDataInt(entity, 55) == 1 && Eng.GetPDataFloat(entity, 48) <= 0.0f)
                    Store.Player(id).GameBpAmmo[weapon] = Core.GetUserBpAmmo(id, weapon);
            }
            return EngineConst.HamIgnored;
        }

        int forwardResult = NativeFunctionRegister.Execute_WpnWeaponPostFrame(entityIndex, id, i);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (Store.Weapon(id, i).Id == EngineConst.CswKnife) return EngineConst.HamIgnored;
        if (Store.Weapon(id, i).Clip == 0) return EngineConst.HamIgnored;

        if (!Store.Player(id).Reloading)
        {
            int clipExtra = Store.Weapon(id, i).Clip;
            float flNextAttack = Eng.GetPDataFloat(id, 83);
            int bpAmmo = Core.GetUserBpAmmo(id, Store.Weapon(id, i).Id);
            int clip = Eng.GetPDataInt(entity, 51);
            int inReload = Eng.GetPDataInt(entity, 54);
            if (inReload != 0 && flNextAttack <= 0.0f && Store.Weapon(id, i).ReloadMode == 0)
            {
                int j = Math.Min(clipExtra - clip, bpAmmo);
                Eng.SetPDataInt(entity, 51, clip + j);
                Core.SetUserBpAmmo(id, Store.Weapon(id, i).Id, bpAmmo - j);
                Store.Player(id).BPAmmo[i] = bpAmmo - j;
                Eng.SetPDataInt(entity, 54, 0);
                inReload = 0;
            }
            if (Store.Weapon(id, i).ReloadMode != 0)
            {
                int button = pPlayer->v.button;
                if ((button & EngineConst.InAttack) != 0 && Eng.GetPDataFloat(entity, 46) <= 0.0f && Eng.GetPDataInt(entity, 51) != 0)
                    return EngineConst.HamIgnored;

                if (Eng.GetPDataInt(entity, 51) == 0 && Core.GetUserBpAmmo(id, Store.Weapon(id, i).Id) != 0)
                {
                    pPlayer->v.button = button & ~EngineConst.InAttack;
                    if (Eng.GetPDataInt(entity, 55) == 0 && Eng.GetPDataInt(entity, 54) == 0)
                    {
                        if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).SightMode != WpnConst.Closed)
                        {
                            if (Store.Player(id).SightMode != WpnConst.Open) return EngineConst.HamIgnored;

                            Store.Player(id).Reloading = true;
                            Attack.BeginClosing(entity);
                            return EngineConst.HamIgnored;
                        }
                    }
                }
                int tempClip = 0;
                if (Store.Weapon(id, i).Id == EngineConst.CswM3) tempClip = 8;
                else if (Store.Weapon(id, i).Id == EngineConst.CswXm1014) tempClip = 7;
                if ((button & EngineConst.InReload) != 0 && inReload == 0 && tempClip != 0)
                {
                    if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).SightMode != WpnConst.Closed)
                    {
                        if (Store.Player(id).SightMode != WpnConst.Open) return EngineConst.HamIgnored;

                        Store.Player(id).Reloading = true;
                        Attack.BeginClosing(entity);
                        return EngineConst.HamIgnored;
                    }
                    if (clip >= clipExtra) pPlayer->v.button = button & ~EngineConst.InReload;
                    else if (clip == tempClip)
                    {
                        if (bpAmmo == 0) return EngineConst.HamIgnored;

                        if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).SightMode != WpnConst.Closed) return EngineConst.HamIgnored;

                        // 子弹数和原版一样时，按R键装一发子弹，然后触发idle的装弹
                        Core.ShotgunReload(entity, clipExtra, clip, bpAmmo, id, Store.Weapon(id, i).Id);
                    }
                }
            }
        }
        if (Store.Weapon(id, i).Sight != 0)
        {
            if (Eng.GetPDataInt(entity, 54) != 0 || Eng.GetPDataInt(entity, 55) != 0) return EngineConst.HamIgnored;

            if (Eng.GetPDataFloat(entity, 47) > 0.0f)
            {
                float curTime = Eng.GP->time;
                if (curTime <= Store.Player(id).NextThink) return EngineConst.HamIgnored;

                if (Store.Weapon(id, i).SightFrameRate <= 0.0f || Store.Weapon(id, i).SightDistance <= 0.0f) return EngineConst.HamIgnored;

                float valve = Store.Weapon(id, i).SightFrameRate;
                if (valve < 1.0f) valve = 1.0f;
                if (Store.Player(id).SightMode == WpnConst.AtoB)
                {
                    Eng.SetPDataInt(pPlayer, 363, Math.Max(Eng.GetPDataInt(pPlayer, 363) - PawnMath.Round(valve), PawnMath.Round(Store.Weapon(id, i).SightDistance)));
                    Store.Player(id).NextThink = curTime + Store.Weapon(id, i).SightOpenTime / ((90.0f - Store.Weapon(id, i).SightDistance) / Store.Weapon(id, i).SightFrameRate);
                }
                else if (Store.Player(id).SightMode == WpnConst.BtoA)
                {
                    Eng.SetPDataInt(pPlayer, 363, Math.Min(Eng.GetPDataInt(pPlayer, 363) + PawnMath.Round(valve), 90));
                    Store.Player(id).NextThink = curTime + Store.Weapon(id, i).SightCloseTime / ((90.0f - Store.Weapon(id, i).SightDistance) / Store.Weapon(id, i).SightFrameRate);
                }
                return EngineConst.HamIgnored;
            }
            if (Store.Player(id).SightMode == WpnConst.AtoB)
            {
                Attack.OpeningEnd(entity);
                return EngineConst.HamIgnored;
            }
            if (Store.Player(id).SightMode == WpnConst.BtoA)
            {
                Attack.ClosingEnd(entity);
                return EngineConst.HamIgnored;
            }
            int button = pPlayer->v.button;
            if ((button & EngineConst.InAttack2) == 0)
            {
                Store.Player(id).OldButton = false;
                return EngineConst.HamIgnored;
            }
            if (Store.Player(id).OldButton) return EngineConst.HamSupercede;

            Store.Player(id).OldButton = true;
            if ((button & EngineConst.InAttack) != 0) return EngineConst.HamSupercede;

            if (Eng.GetPDataFloat(entity, 46) > 0.0f) return EngineConst.HamSupercede;

            if (Store.Player(id).SightMode == WpnConst.Closed)
            {
                Attack.BeginOpening(entity);
                return EngineConst.HamSupercede;
            }
            Attack.BeginClosing(entity);
            return EngineConst.HamSupercede;
        }
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponReload")]
    public static int OnWeaponReload(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return EngineConst.HamIgnored;
        if (i == 0) return EngineConst.HamIgnored;
        if (Store.Weapon(id, i).Id == EngineConst.CswKnife) return EngineConst.HamIgnored;
        if (Store.Weapon(id, i).Clip == 0) return EngineConst.HamSupercede;

        int result = NativeFunctionRegister.Execute_WpnWeaponReload(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).SightMode != WpnConst.Closed)
        {
            if (Store.Player(id).SightMode != WpnConst.Open) return EngineConst.HamSupercede;

            Store.Player(id).Reloading = true;
            Attack.BeginClosing(entity);
            return EngineConst.HamSupercede;
        }
        int bpAmmo = Core.GetUserBpAmmo(id, Store.Weapon(id, i).Id);
        int clip = Eng.GetPDataInt(entity, 51);
        int inSpecialReload = Eng.GetPDataInt(entity, 55);

        // 在ShotgunReload里判断并设置换弹状态，fInSpecReload为2时忽略，使用原版的装填子弹
        if (Store.Weapon(id, i).ReloadMode != 0)
        {
            if (inSpecialReload < 2 && (Store.Weapon(id, i).Id == EngineConst.CswM3 || Store.Weapon(id, i).Id == EngineConst.CswXm1014))
                Core.ShotgunReload(entity, Store.Weapon(id, i).Clip, clip, bpAmmo, id, Store.Weapon(id, i).Id);

            if (Store.Weapon(id, i).Id != EngineConst.CswM3 && Store.Weapon(id, i).Id != EngineConst.CswXm1014)
            {
                Core.ShotgunReload(entity, Store.Weapon(id, i).Clip, clip, bpAmmo, id, Store.Weapon(id, i).Id);
                return EngineConst.HamSupercede;
            }
        }
        int clipExtra = Store.Weapon(id, i).Clip;
        Store.Player(id).TmpClip[i] = -1;
        if (bpAmmo <= 0) return EngineConst.HamSupercede;

        if (clip >= clipExtra) return EngineConst.HamSupercede;

        Store.Player(id).TmpClip[i] = clip;
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponReloadPost")]
    public static void OnWeaponReloadPost(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return;
        if (i == 0) return;
        if (Store.Weapon(id, i).Id == EngineConst.CswKnife) return;

        int result = NativeFunctionRegister.Execute_WpnWeaponReloadPost(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).Reloading) return;
        if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).SightMode != WpnConst.Closed) return;

        int inSpecialReload = Eng.GetPDataInt(entity, 55);
        if (Store.Weapon(id, i).ReloadMode != 0)
        {
            if (inSpecialReload == 1) Store.Player(id).BPAmmo[i] = Core.GetUserBpAmmo(id, Store.Weapon(id, i).Id);
            return;
        }
        if (Store.Weapon(id, i).Clip == 0) return;

        int tmpClip = Store.Player(id).TmpClip[i];
        if (tmpClip == -1) return;

        Eng.SetPDataInt(entity, 51, tmpClip);
        Eng.SetPDataFloat(entity, 48, Store.Weapon(id, i).ReloadTime[0]);
        Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).ReloadTime[0]);
        Eng.SetPDataInt(entity, 54, 1);
        if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
            Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimReloadSilenced]);
        else
            Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimReload]);
    }

    [AmxxCallback("wt_WeaponWeaponIdle")]
    public static int OnWeaponWeaponIdle(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (!Eng.IsAlive(id)) return EngineConst.HamIgnored;
        if (i == 0) return EngineConst.HamIgnored;

        int result = NativeFunctionRegister.Execute_WpnWeaponIdle(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (Store.Weapon(id, i).ReloadMode != 0)
        {
            if (Eng.GetPDataFloat(entity, 48) > 0.0f) return EngineConst.HamIgnored;

            if (Store.Player(id).Reloading) return EngineConst.HamIgnored;

            int bpAmmo = Core.GetUserBpAmmo(id, Store.Weapon(id, i).Id);
            int clip = Eng.GetPDataInt(entity, 51);
            int inSpecialReload = Eng.GetPDataInt(entity, 55);
            if (clip == 0 && inSpecialReload == 0)
            {
                if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).SightMode != WpnConst.Open && Store.Player(id).SightMode != WpnConst.Closed)
                    return EngineConst.HamSupercede;

                return EngineConst.HamIgnored;
            }
            if (Store.Weapon(id, i).Sight != 0 && Store.Player(id).SightMode != WpnConst.Open && Store.Player(id).SightMode != WpnConst.Closed)
                return EngineConst.HamSupercede;

            int tempClip = 0;
            if (Store.Weapon(id, i).Id == EngineConst.CswM3) tempClip = 8;
            else if (Store.Weapon(id, i).Id == EngineConst.CswXm1014) tempClip = 7;
            if (inSpecialReload != 0 && tempClip != 0)
            {
                // 弹夹数和原版一样时继续换弹
                if (clip < Store.Weapon(id, i).Clip && clip == tempClip && bpAmmo != 0)
                    Core.ShotgunReload(entity, Store.Weapon(id, i).Clip, clip, bpAmmo, id, Store.Weapon(id, i).Id);

                // 换弹结束
                else if (clip == Store.Weapon(id, i).Clip)
                {
                    Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimAfterReload]);
                    Eng.SetPDataInt(entity, 55, 0);
                    Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 47, Store.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 48, Store.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).ReloadTime[2]);
                }
            }
            else if (inSpecialReload != 0 && tempClip == 0)
            {
                if (clip < Store.Weapon(id, i).Clip && bpAmmo != 0)
                    MF.CallNative("ExecuteHamB", EngineConst.HamWeaponReload, entityIndex);

                else if (clip == Store.Weapon(id, i).Clip || bpAmmo == 0)
                {
                    Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimAfterReload]);
                    Eng.SetPDataInt(entity, 55, 0);
                    Eng.SetPDataFloat(entity, 46, Store.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 47, Store.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(entity, 48, Store.Weapon(id, i).ReloadTime[2]);
                    Eng.SetPDataFloat(id, 83, Store.Weapon(id, i).ReloadTime[2]);
                }
                return EngineConst.HamSupercede;
            }
        }
        if (Eng.GetPDataFloat(entity, 48) <= 0.0f && Store.Weapon(id, i).Id != EngineConst.CswHegrenade && Eng.GetPDataInt(entity, 55) == 0 && Eng.GetPDataInt(entity, 54) == 0)
        {
            if (Store.Weapon(id, i).Id == EngineConst.CswKnife) Core.PlayAnim(id, Store.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimIdle]);
            else
            {
                if ((Eng.GetPDataInt(entity, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(entity, 74) & (1 << 0)) != 0)
                    Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimIdleSilenced]);
                else
                    Core.PlayAnim(id, Store.Weapon(id, i).Anim[WpnConst.AnimIdle]);
            }
            Eng.SetPDataFloat(entity, 48, 20.0f);
            return EngineConst.HamSupercede;
        }
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_UseStationaryPost")]
    public static void OnUseStationaryPost(int entityIndex, int id, int activator, int useType)
    {
        if (useType == 0 && Eng.IsConnected(id)) Core.EventCurWeapon(id);
    }

    [AmxxCallback("wt_TakeDamage")]
    public static int OnTakeDamage(int victim, int inflictor, int attacker, float damage, int damageType)
    {
        if (!Eng.IsConnected(attacker) || !Eng.IsConnected(victim)) return EngineConst.HamIgnored;

        int forwardResult = NativeFunctionRegister.Execute_WpnTakeDamage(victim, inflictor, attacker, damage, damageType);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        var pVictim = Eng.EntOfIndex(victim);
        var pAttacker = Eng.EntOfIndex(attacker);
        if (pVictim == null || pAttacker == null) return EngineConst.HamIgnored;

        damage *= Store.Player(attacker).DamagePoint * Store.Player(victim).DamageVictimPoint;
        int i = Core.WeaponTypeByIndex(inflictor);
        if ((damageType & (1 << 24)) != 0)
        {
            if (i == 0)
            {
                MF.CallNative("SetHamParamFloat", 4, damage);
                return EngineConst.HamIgnored;
            }
            damage *= Store.Weapon(attacker, i).Damage;
            var pInflictor = Eng.EntOfIndex(inflictor);
            var victimOrigin = new float[] { pVictim->v.origin.x, pVictim->v.origin.y, pVictim->v.origin.z };
            var inflictorOrigin = pInflictor != null
                ? new float[] { pInflictor->v.origin.x, pInflictor->v.origin.y, pInflictor->v.origin.z }
                : new float[] { 0f, 0f, 0f };
            float distance = Core.Distance(victimOrigin, inflictorOrigin);
            damage -= damage * distance / Store.Weapon(attacker, i).GrenadeData[2];
            if (damage <= 0.0f) damage = 0.0f;
            MF.CallNative("SetHamParamFloat", 4, damage);
            return EngineConst.HamIgnored;
        }
        if (attacker != inflictor)
        {
            if (i != 0) Store.Player(attacker).AttackerEntity = inflictor;
            else
            {
                MF.CallNative("SetHamParamFloat", 4, damage);
                return EngineConst.HamIgnored;
            }
        }
        else
        {
            var activeItem = Eng.GetPDataCbase(pAttacker, 373);
            if (activeItem == null) return EngineConst.HamIgnored;

            i = Store.GetWeaponType(activeItem);
            if (i == 0)
            {
                MF.CallNative("SetHamParamFloat", 4, damage);
                return EngineConst.HamIgnored;
            }
            Store.Player(attacker).AttackerEntity = Eng.IndexOf(activeItem);
        }
        if (Store.Weapon(attacker, i).Id == EngineConst.CswKnife)
        {
            if (Store.Player(attacker).KnifeTempDamage != 0.0f)
            {
                MF.CallNative("SetHamParamFloat", 4, damage * Store.Player(attacker).KnifeTempDamage);
                Store.Player(attacker).KnifeTempDamage = 0.0f;
            }
            return EngineConst.HamIgnored;
        }
        MF.CallNative("SetHamParamFloat", 4, damage * Store.Weapon(attacker, i).Damage);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_TakeDamagePost")]
    public static void OnTakeDamagePost(int victim, int inflictor, int attacker, float damage, int damageType)
    {
        if (!Eng.IsConnected(attacker) || !Eng.IsConnected(victim)) return;

        var pAttacker = Eng.EntOfIndex(attacker);
        var pVictim = Eng.EntOfIndex(victim);
        if (pAttacker == null || pVictim == null) return;

        if (Eng.GetPDataInt(pAttacker, 114) == Eng.GetPDataInt(pVictim, 114)) return;

        if (Store.Player(attacker).AttackerEntity != 0)
        {
            float health = pVictim->v.health;
            if (health - damage > 0.0f) Store.Player(attacker).AttackerEntity = 0;
        }
        var activeItem = Eng.GetPDataCbase(pAttacker, 373);
        if (activeItem == null) return;

        int i = Store.GetWeaponType(activeItem);
        int forwardResult = NativeFunctionRegister.Execute_WpnWeaponKnockBack(attacker, victim, inflictor, i, damage);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return;

        if (i == 0) return;

        // 原版 pev_flags & (FL_DUCKING|FL_ONGROUND) == (FL_DUCKING|FL_ONGROUND) 受 Pawn 运算符优先级影响
        // 实际退化为 flags & 1;这里按注释的本意(蹲下且在地面)实现。
        bool ducking = ((pVictim->v.flags & (EngineConst.FlDucking | EngineConst.FlOnground)) == (EngineConst.FlDucking | EngineConst.FlOnground));

        var victimOrigin = new float[] { pVictim->v.origin.x, pVictim->v.origin.y, pVictim->v.origin.z };
        var attackerOrigin = new float[] { pAttacker->v.origin.x, pAttacker->v.origin.y, pAttacker->v.origin.z };
        var velocity = new float[3];
        float victimVelocityZ = pVictim->v.velocity.z;
        float knockBack = ducking
            ? damage * Store.Weapon(attacker, i).KnockBack * 0.25f * Store.Player(victim).KnockBackPoint
            : damage * Store.Weapon(attacker, i).KnockBack * Store.Player(victim).KnockBackPoint;
        Core.GetVelocityFromOrigin(victimOrigin, attackerOrigin, knockBack, velocity);
        velocity[2] = victimVelocityZ;
        pVictim->v.velocity = new vec3_t { x = velocity[0], y = velocity[1], z = velocity[2] };
    }

    [AmxxCallback("wt_KilledPost")]
    public static void OnKilledPost(int id, int attacker)
    {
        if (!Eng.IsConnected(id)) return;

        // 死亡时剥夺所有子弹
        const int primaryBitSum =
            (1 << EngineConst.CswScout) | (1 << EngineConst.CswXm1014) | (1 << EngineConst.CswMac10) | (1 << EngineConst.CswAug) |
            (1 << EngineConst.CswUmp45) | (1 << EngineConst.CswSg550) | (1 << EngineConst.CswGalil) | (1 << EngineConst.CswFamas) |
            (1 << EngineConst.CswAwp) | (1 << EngineConst.CswMp5Navy) | (1 << EngineConst.CswM249) | (1 << EngineConst.CswM3) |
            (1 << EngineConst.CswM4a1) | (1 << EngineConst.CswTmp) | (1 << EngineConst.CswG3sg1) | (1 << EngineConst.CswSg552) |
            (1 << EngineConst.CswAk47) | (1 << EngineConst.CswP90);
        const int secondaryBitSum =
            (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) |
            (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);
        Array.Clear(Store.Player(id).BPAmmo);
        for (int i = 0; i < 32; i++) Store.Player(id).GameBpAmmo[i] = 0;
        for (int i = 1; i < 31; i++)
            if (((1 << i) & (primaryBitSum | secondaryBitSum)) != 0) Core.SetUserBpAmmo(id, i, 0);
    }

    [AmxxCallback("wt_ThinkGrenade")]
    public static int OnThinkGrenade(int entityIndex)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;

        float dmgTime = entity->v.dmgtime;
        float curTime = Eng.GameTime;
        if (dmgTime > curTime) return EngineConst.HamIgnored;

        int i = Store.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        int ownerId = Eng.IndexOf(entity->v.owner);
        int forwardResult = NativeFunctionRegister.Execute_WpnHookGrenadeExplode(entityIndex, ownerId, i);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        var origin = stackalloc float[3];
        origin[0] = entity->v.origin.x;
        origin[1] = entity->v.origin.y;
        origin[2] = entity->v.origin.z;

        Eng.MessageBegin(EngineConst.MsgAll, EngineConst.SvcTempEntity, origin, null);
        Eng.WriteByte(EngineConst.TeExplosion);
        Eng.WriteCoord(origin[0]);
        Eng.WriteCoord(origin[1]);
        Eng.WriteCoord(origin[2] + 45.0f);
        Eng.WriteShort(Store.Weapon(ownerId, i).GrenadeSprId);
        Eng.WriteByte(25);
        Eng.WriteByte(30);
        Eng.WriteByte(EngineConst.TeExplFlagNoSound);
        Eng.MessageEnd();

        Eng.MessageBegin(EngineConst.MsgPas, EngineConst.SvcTempEntity, origin, null);
        Eng.WriteByte(EngineConst.TeWorldDecal);
        Eng.WriteCoord(origin[0]);
        Eng.WriteCoord(origin[1]);
        Eng.WriteCoord(origin[2]);
        Eng.WriteByte(Eng.DecalIndex("{scorch3"));
        Eng.MessageEnd();

        Eng.EmitSound(entity, EngineConst.ChanStatic, Store.Weapon(ownerId, i).Sound1, EngineConst.VolNorm, Store.Weapon(ownerId, i).FireSoundVolume, 0, EngineConst.PitchNorm);

        // 记录手雷的序号，给deathmsg用
        Store.Player(ownerId).UserGrenadeId = i;

        // 原版从实体 0(worldspawn)开始检索
        var found = Eng.EntOfIndex(0);
        while (found != null && (found = Eng.FindEntityInSphere(found, origin, Store.Weapon(ownerId, i).GrenadeData[2])) != null)
        {
            int victim = Eng.IndexOf(found);
            if (!Eng.IsValidIndex(victim)) continue;
            if (found->v.solid <= EngineConst.SolidNot) continue;
            if (found->v.takedamage <= 0.0f) continue;
            var pOwner = Eng.EntOfIndex(ownerId);
            if (victim != ownerId && Eng.IsConnected(victim) && Eng.IsValid(pOwner) && Eng.GetPDataInt(pOwner, 114) == Eng.GetPDataInt(found, 114) && Eng.CvarNum("mp_friendlyfire") == 0)
                continue;

            Eng.TakeDamage(victim, entityIndex, ownerId, 100.0f, 1 << 24);
            NativeFunctionRegister.Execute_WpnHookGrenadeExplodePost(ownerId, entityIndex, victim, i);
        }
        Store.Player(ownerId).UserGrenadeId = 0;
        Eng.RemoveEntity(entity);
        return EngineConst.HamSupercede;
    }

    // 其他方式获取的子弹
    [AmxxCallback("wt_GiveAmmo")]
    public static int OnGiveAmmo(int id, int amount, string ammoName, int max)
    {
        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return EngineConst.HamIgnored;

        if (Store.Player(id).BuyingAmmo) return EngineConst.HamIgnored;

        ammoName = ammoName.ToLowerInvariant();
        if (ammoName is "hegrenade" or "flashbang" or "smokegrenade" or "c4") return EngineConst.HamIgnored;

        int i = Store.GetWeaponType(activeItem);
        int weapon = Eng.GetPDataInt(activeItem, 43);
        const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
        const int secWeaponBitSum = (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) | (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);

        if (GameTables.GameWeaponAmmoType[weapon] == ammoName)
        {
            if (((1 << weapon) & secWeaponBitSum) == 0 && ((1 << weapon) & specialBitSum) == 0)
                Core.GivePriAmmo(id, activeItem, i, i != 0 ? (Store.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
            else if (((1 << weapon) & secWeaponBitSum) != 0)
                Core.GiveSecAmmo(id, activeItem, i, i != 0 ? (Store.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
        }
        else
        {
            if (Eng.GetPDataCbase(pPlayer, 368) == activeItem) activeItem = Eng.GetPDataCbase(pPlayer, 369);
            else if (Eng.GetPDataCbase(pPlayer, 369) == activeItem) activeItem = Eng.GetPDataCbase(pPlayer, 368);
            else if (Eng.GetPDataCbase(pPlayer, 368) != activeItem && Eng.GetPDataCbase(pPlayer, 369) != activeItem)
            {
                var item1 = Eng.GetPDataCbase(pPlayer, 369);
                var item2 = Eng.GetPDataCbase(pPlayer, 368);
                if (Eng.IsValid(item1) && GameTables.GameWeaponAmmoType[Eng.GetPDataInt(item1, 43)] == ammoName) activeItem = item1;
                if (Eng.IsValid(item2) && GameTables.GameWeaponAmmoType[Eng.GetPDataInt(item2, 43)] == ammoName) activeItem = item2;
            }
            if (!Eng.IsValid(activeItem)) return EngineConst.HamSupercede;

            i = Store.GetWeaponType(activeItem);
            weapon = Eng.GetPDataInt(activeItem, 43);
            if (GameTables.GameWeaponAmmoType[weapon] == ammoName)
            {
                if (((1 << weapon) & secWeaponBitSum) == 0)
                    Core.GivePriAmmo(id, activeItem, i, i != 0 ? (Store.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
                else
                    Core.GiveSecAmmo(id, activeItem, i, i != 0 ? (Store.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
            }
        }
        return EngineConst.HamSupercede;
    }

    [AmxxCallback("wt_PlayerSpawnPost")]
    public static void OnPlayerSpawnPost(int id)
    {
        if (!Eng.IsAlive(id)) return;

        if (!Eng.IsBot(id))
        {
            var pPlayer = Eng.EntOfIndex(id);
            var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
            if (activeItem == null) return;

            int i = Store.GetWeaponType(activeItem);
            if (i == 0) return;

            if (Store.Weapon(id, i).Sight == 0) return;

            if (Store.Player(id).SightMode == WpnConst.Closed) return;

            Attack.ClosingEnd(activeItem);
            return;
        }
        if (Store.Round == 0) return;

        Store.Player(id).BotBuyWeapon = true;
        if (Store.Player(id).BlockBuy == WpnConst.BuyNormally) Store.Player(id).BotNextThink = 0.0f;
        else if (Store.Player(id).BlockBuy == WpnConst.BuyEverywhere) Store.Player(id).BotNextThink = Eng.GameTime + 0.1f;
        else Store.Player(id).BotBuyWeapon = false;
    }

    [AmxxCallback("wt_TraceAttackPost")]
    public static void OnTraceAttackPost(int entityIndex, int attacker, float damage, [AmxxBuffer(3)] Span<float> direction, int traceHandle, int damageType)
    {
        if (!Eng.IsAlive(attacker)) return;

        var pAttacker = Eng.EntOfIndex(attacker);
        var activeItem = pAttacker == null ? null : Eng.GetPDataCbase(pAttacker, 373);
        int i = Eng.IsValid(activeItem) ? Store.GetWeaponType(activeItem) : 0;
        if (i == 0) return;

        if (Store.Weapon(attacker, i).Id == EngineConst.CswKnife) return;

        // ham 传入的是 fakemeta trace 句柄
        var endPos = new float[3];
        MF.CallNative("get_tr2", traceHandle, 5, endPos); // TR_vecEndPos = 5

        if (entityIndex != 0)
        {
            Eng.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
            Eng.WriteByte(EngineConst.TeDecal);
            Eng.WriteCoord(endPos[0]);
            Eng.WriteCoord(endPos[1]);
            Eng.WriteCoord(endPos[2]);
            Eng.WriteByte(GameTables.GunshotDecals[Eng.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
            Eng.WriteShort(entityIndex);
            Eng.MessageEnd();
        }
        else
        {
            Eng.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
            Eng.WriteByte(EngineConst.TeWorldDecal);
            Eng.WriteCoord(endPos[0]);
            Eng.WriteCoord(endPos[1]);
            Eng.WriteCoord(endPos[2]);
            Eng.WriteByte(GameTables.GunshotDecals[Eng.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
            Eng.MessageEnd();
        }
        Eng.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
        Eng.WriteByte(EngineConst.TeGunshotDecal);
        Eng.WriteCoord(endPos[0]);
        Eng.WriteCoord(endPos[1]);
        Eng.WriteCoord(endPos[2]);
        Eng.WriteShort(attacker);
        Eng.WriteByte(GameTables.GunshotDecals[Eng.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
        Eng.MessageEnd();
        endPos[2] += 5.0f;
        Core.FakeSmoke(endPos);
    }
}
