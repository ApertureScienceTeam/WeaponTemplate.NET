using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// 原版 fakemeta forward(FM_*) 到 metamod 函数表 hook 的映射:
//   FM_ClientCommand            -> GameDll pfnClientCommand(pre, SUPERCEDE=不调原版)
//   FM_SetModel                 -> GameDll pfnSetModel(pre)
//   FM_UpdateClientData_Post    -> GameDll pfnUpdateClientData(post)
//   FM_PlaybackEvent            -> GameDll pfnPlaybackEvent(pre)
//   FM_EmitSound                -> Engine  pfnEmitSound(pre)
//   FM_TraceLine / FM_TraceHull -> Engine  pfnTraceLine / pfnTraceHull(pre)
//   FM_PlayerPreThink           -> GameDll pfnPlayerPreThink(pre)
//   FM_PlayerPostThink_Post     -> GameDll pfnPlayerPostThink(post)
//   FM_Think_Post               -> GameDll pfnThink(post)
// 调原版:gamedll 经 gpGamedllFuncs->dllapi_table,引擎经 g_engfuncs(绕过 hook,不递归)。
public static unsafe class Hooks
{
    private const int FevHostOnly = 1 << 1;

    // ---- 购买命令(fm_ClientCommand) ----

    [AmxxHook("pfnClientCommand")]
    public static void ClientCommand(edict_t* pEntity)
    {
        int id = Eng.IndexOf(pEntity);
        string command = Marshal.PtrToStringAnsi((nint)Eng.EF.pfnCmd_Argv(0)) ?? "";
        bool handled = ClientCommandLogic(id, command);
        if (!handled)
            ModuleRuntime.gpGamedllFuncs->dllapi_table->pfnClientCommand(pEntity);
    }

    // 原版 fw_ClientCommand;返回 true = FMRES_SUPERCEDE(不调用原版 gamedll)
    private static bool ClientCommandLogic(int id, string command)
    {
        if (!Eng.IsAlive(id)) return false;

        var pPlayer = Eng.EntOfIndex(id)!;

        if (command == "buyammo1")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 368);
            if (!Eng.IsValid(entity)) return false;

            int type = Store.GetWeaponType(entity);
            if (Store.WeaponClip[id, type] == 0 && type != 0)
            {
                Core.GivePriAmmo(id, entity, type, 1, 0, 0);
                return true;
            }
            Core.GivePriAmmo(id, entity, type, 0, 0, 0);
            return true;
        }
        if (command == "buyammo2")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 369);
            if (!Eng.IsValid(entity)) return false;

            int type = Store.GetWeaponType(entity);
            if (Store.WeaponClip[id, type] == 0 && type != 0)
            {
                Core.GiveSecAmmo(id, entity, type, 1, 0, 0);
                return true;
            }
            Core.GiveSecAmmo(id, entity, type, 0, 0, 0);
            return true;
        }
        if (command == "primammo")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 368);
            if (!Eng.IsValid(entity)) return false;

            int type = Store.GetWeaponType(entity);
            if (Store.WeaponClip[id, type] == 0 && type != 0)
            {
                while (Core.GivePriAmmo(id, entity, type, 1, 0, 0)) { }
                return true;
            }
            while (Core.GivePriAmmo(id, entity, type, 0, 0, 0)) { }
            return true;
        }
        if (command == "secammo")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 369);
            if (!Eng.IsValid(entity)) return false;

            int type = Store.GetWeaponType(entity);
            if (Store.WeaponClip[id, type] == 0 && type != 0)
            {
                while (Core.GiveSecAmmo(id, entity, type, 1, 0, 0)) { }
                return true;
            }
            while (Core.GiveSecAmmo(id, entity, type, 0, 0, 0)) { }
            return true;
        }
        if (command == "shield") return true;

        if (command == "normal_knife")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 370);
            var newEntity = Core.GiveWeapon(id, "weapon_knife", 0);
            if (!Eng.IsValid(entity)) entity = newEntity;
            Core.ChangeWeaponList(id, newEntity);
            if (Eng.IsValid(entity) && Eng.GetPDataInt(entity, 43) == EngineConst.CswKnife)
            {
                MF.CallNative("ExecuteHamB", EngineConst.HamItemDeploy, Eng.IndexOf(entity));
                Core.PlayAnim(id, WpnConst.KnifeAnimDraw);
                Eng.SetPDataFloat(id, 83, 0.7f);
            }
            return true;
        }
        if (command == "vest")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            if (pPlayer->v.armorvalue >= 100.0f) return true;

            if (Eng.GetPDataInt(pPlayer, 115) < 650)
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            pPlayer->v.armorvalue = 100.0f;
            Eng.SetPDataInt(pPlayer, 112, 1);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("ArmorType"), null, pPlayer);
            Eng.WriteByte(0);
            Eng.MessageEnd();
            Core.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 650);
            Eng.EmitSound(pPlayer, EngineConst.ChanItem, "items/ammopickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
            return true;
        }
        if (command == "vesthelm")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            if (pPlayer->v.armorvalue >= 100.0f && Eng.GetPDataInt(pPlayer, 112) == 2) return true;

            if ((Eng.GetPDataInt(pPlayer, 115) < 1000 && pPlayer->v.armorvalue < 100.0f) ||
                (Eng.GetPDataInt(pPlayer, 115) < 350 && pPlayer->v.armorvalue >= 100.0f))
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            if (Eng.GetPDataInt(pPlayer, 112) == 1) Core.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 350);
            else Core.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 1000);
            pPlayer->v.armorvalue = 100.0f;
            Eng.SetPDataInt(pPlayer, 112, 2);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("ArmorType"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.MessageEnd();
            Eng.EmitSound(pPlayer, EngineConst.ChanItem, "items/ammopickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
            return true;
        }
        if (command == "defuser")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            bool canBuy = false;
            for (var found = Eng.FindEntityByString(null, "classname", "func_bomb_target"); found != null; found = Eng.FindEntityByString(found, "classname", "func_bomb_target"))
                canBuy = true;
            if (!canBuy) return true;

            if (Eng.GetPDataInt(pPlayer, 115) < 200)
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            if (Eng.GetPDataInt(pPlayer, 114) != 2)
            {
                Eng.ClientPrintCenter(id, "对于你的队伍来说还不能购买\"炸弹拆除装备\"。");
                return true;
            }
            pPlayer->v.body = 1;
            int defuseKitSkill = Eng.GetPDataInt(pPlayer, 193);
            if ((defuseKitSkill & (1 << 16)) != 0)
            {
                Eng.ClientPrintCenter(id, "你已经有一个了。");
                return true;
            }
            defuseKitSkill |= 1 << 16;
            Eng.SetPDataInt(pPlayer, 193, defuseKitSkill);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("StatusIcon"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.WriteString("defuser");
            Eng.WriteByte(0);
            Eng.WriteByte(160);
            Eng.WriteByte(0);
            Eng.MessageEnd();
            Core.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 200);
            Eng.EmitSound(pPlayer, EngineConst.ChanAuto, "items/gunpickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
            return true;
        }
        if (command == "nvgs")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            if (Eng.GetPDataInt(pPlayer, 115) < 1250)
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            int nvgKit = Eng.GetPDataInt(pPlayer, 129);
            if ((nvgKit & (1 << 0)) != 0)
            {
                Eng.ClientPrintCenter(id, "你已经有一个了。");
                return true;
            }
            nvgKit |= 1 << 0;
            Eng.SetPDataInt(pPlayer, 129, nvgKit);
            Core.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 1250);
            return true;
        }

        // 原版武器购买命令
        for (int i = 0; i < Store.GameWeaponBuyCommand.Length; i++)
        {
            if (command != Store.GameWeaponBuyCommand[i] || Store.GameWeaponBuyCommand[i].Length == 0)
                continue;

            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Core.CheckBuy(id)) return true;

            if (Eng.GetPDataInt(pPlayer, 114) != Store.GameWeaponDefaultTeam[i] && Store.GameWeaponDefaultTeam[i] != 0 && Store.AllTeamBuy == 0)
            {
                Eng.ClientPrintCenter(id, $"对于你的队伍来说还不能购买\"{Store.GameWeaponBuyCommand[i]}\"。");
                return true;
            }
            if (Eng.GetPDataInt(pPlayer, 115) < Store.GameWeaponDefaultCost[i])
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            if (command == "hegren" || command == "sgren")
            {
                if ((pPlayer->v.weapons & (1 << i)) != 0)
                {
                    Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Cannot_Carry_Anymore");
                    return true;
                }
            }
            else if (command == "flash" && Eng.GetPDataInt(pPlayer, 387) >= 2)
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Cannot_Carry_Anymore");
                return true;
            }
            var entity = Core.GiveWeapon(id, Store.GameWeaponClassName[i], 0);
            if (Eng.IsValid(entity))
            {
                Core.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - Store.GameWeaponDefaultCost[i]);
                if (Core.GetUserBpAmmo(id, i) > Store.GameWeaponMaxBpammo[i]) Core.SetUserBpAmmo(id, i, Store.GameWeaponMaxBpammo[i]);
                NativeFunctionRegister.Execute_WpnBuyWeaponPost(Eng.IndexOf(entity), id, 0);
            }
            return true;
        }

        // 模板武器购买命令
        for (int i = 1; i <= Store.WeaponAmount; i++)
        {
            string weaponListCommand = $"weapon_{Store.WeaponList[id, i]}";
            if (command == weaponListCommand) Eng.EngClientCommand(id, Store.GameWeaponClassName[Store.WeaponId[id, i]]);
            if (command != Store.WeaponBuyCommand[id, i] && Store.WeaponBuyCommand[id, i].Length != 0)
                continue;

            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (command == "empty") return true;

            if (!Core.CheckBuy(id)) return true;

            if (Eng.GetPDataInt(pPlayer, 114) != Store.WeaponTeam[id, i] && Store.WeaponTeam[id, i] != 0)
            {
                Eng.ClientPrintCenter(id, $"对于你的队伍来说还不能购买\"{Store.WeaponList[id, i]}\"。");
                return true;
            }
            if (Eng.GetPDataInt(pPlayer, 115) < Store.WeaponCost[id, i])
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            int entityIndex = Core.GiveWeaponByTemplate(id, i);
            if (Eng.IsValidIndex(entityIndex))
            {
                Core.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - Store.WeaponCost[id, i]);

                // 防止备弹超过上限
                if (Core.GetUserBpAmmo(id, Store.WeaponId[id, i]) > Store.WeaponAmmo[id, i] &&
                    Store.WeaponId[id, i] != EngineConst.CswHegrenade && Store.WeaponId[id, i] != EngineConst.CswKnife)
                    Core.SetUserBpAmmo(id, Store.WeaponId[id, i], Store.WeaponAmmo[id, i]);
                NativeFunctionRegister.Execute_WpnBuyWeaponPost(entityIndex, id, i);
            }
            return true;
        }
        return false;
    }

    // ---- fm_SetModel(EngFunc_SetModel hook) ----

    [AmxxHook("pfnSetModel", Table = HookTable.Engine)]
    public static void SetModel(edict_t* pEntity, sbyte* model)
    {
        if (SetModelLogic(pEntity)) return; // SUPERCEDE:已设置自定义模型
        Eng.EF.pfnSetModel(pEntity, model);
    }

    private static bool SetModelLogic(edict_t* entity)
    {
        if (!Eng.IsValid(entity)) return false;

        string className = Eng.ClassName(entity);
        if (className == "grenade")
        {
            int id = Eng.IndexOf(entity->v.owner);
            if (id == 0) return false;

            var pPlayer = Eng.EntOfIndex(id);
            var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
            if (activeItem == null) return false;

            int i = Store.GetWeaponType(activeItem);
            if (i == 0) return false;

            Eng.SetPDataInt(entity, 11, i);
            entity->v.dmgtime = Eng.GameTime + Store.GrenadeData[id, i, 1];
            Eng.SetModel(entity, Store.WeaponWModel[id, i]);
            if (Store.WeaponWModelBody[id, i] != 0) entity->v.body = Store.WeaponWModelBody[id, i];
            return true;
        }
        if (className != "weaponbox") return false;

        // 通过weaponbox实体获取武器实体
        edict_t* weapon = null;
        for (int i = 0; i < 6; i++)
        {
            weapon = Eng.GetPDataCbase(entity, 34 + i);
            if (weapon != null) break;
        }
        if (weapon == null) return false;

        int type = Store.GetWeaponType(weapon);
        if (type == 0) return false;

        int ownerId = Eng.IndexOf(entity->v.owner);
        if (Store.WeaponId[ownerId, type] == EngineConst.CswHegrenade)
        {
            entity->v.nextthink = Eng.GameTime + 0.1f;
            return true;
        }
        Eng.SetModel(entity, Store.WeaponWModel[ownerId, type]);
        if (Store.WeaponWModelBody[ownerId, type] != 0) entity->v.body = Store.WeaponWModelBody[ownerId, type];
        return true;
    }

    // ---- fm_UpdateClientData_Post(阻止客户端预测开枪) ----
    // 注:本 SDK 的 DLL_FUNCTIONS.pfnUpdateClientData 声明参数顺序为 (edict_t*, int, clientdata_t*)。

    [AmxxHook("pfnUpdateClientData", Post = true)]
    public static void UpdateClientDataPost(edict_t* pEntity, int sendWeapons, clientdata_t* cd)
    {
        int id = Eng.IndexOf(pEntity);
        if (!Eng.IsAlive(id)) return;

        var pPlayer = Eng.EntOfIndex(id);
        var entity = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(entity)) return;

        int i = Store.GetWeaponType(entity);
        if (i == 0) return;

        if (Store.WeaponId[id, i] == EngineConst.CswKnife) return;

        // 用于阻止客户端的开枪
        cd->m_flNextAttack = Eng.GameTime + 0.001f;
    }

    // ---- fm_PlaybackEvent(EngFunc_PlaybackEvent hook,命中率修正) ----

    [AmxxHook("pfnPlaybackEvent", Table = HookTable.Engine)]
    public static void PlaybackEvent(int flags, edict_t* pInvoker, ushort eventIndex, float delay, float* origin, float* angles,
        float fParam1, float fParam2, int iParam1, int iParam2, int bParam1, int bParam2)
    {
        int id = Eng.IndexOf(pInvoker);
        if (!Eng.IsConnected(id) || !Store.IsInPrimaryAttack) { CallOriginalPlayback(flags, pInvoker, eventIndex, delay, origin, angles, fParam1, fParam2, iParam1, iParam2, bParam1, bParam2); return; }

        var pPlayer = Eng.EntOfIndex(id);
        var entity = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        int i = Eng.IsValid(entity) ? Store.GetWeaponType(entity) : 0;
        if (i == 0 || Store.WeaponId[id, i] == EngineConst.CswKnife) { CallOriginalPlayback(flags, pInvoker, eventIndex, delay, origin, angles, fParam1, fParam2, iParam1, iParam2, bParam1, bParam2); return; }

        float recoil;
        if (Core.GetUserZoom(id) != 1 || Store.SightMode[id] == WpnConst.Open) recoil = Store.WeaponAimAccuracy[id, i];
        else recoil = Store.WeaponAccuracy[id, i];
        Eng.PlaybackEvent(flags | FevHostOnly, pInvoker, eventIndex, delay, origin, angles, fParam1 * recoil, fParam2 * recoil, iParam1, iParam2, bParam1, bParam2);
    }

    private static void CallOriginalPlayback(int flags, edict_t* pInvoker, ushort eventIndex, float delay, float* origin, float* angles,
        float fParam1, float fParam2, int iParam1, int iParam2, int bParam1, int bParam2)
        => Eng.EF.pfnPlaybackEvent(flags, pInvoker, eventIndex, delay, origin, angles, fParam1, fParam2, iParam1, iParam2, bParam1, bParam2);

    // ---- fm_EmitSound(刀子声音替换) ----

    [AmxxHook("pfnEmitSound", Table = HookTable.Engine)]
    public static void EmitSound(edict_t* pEntity, int channel, sbyte* sample, float volume, float attenuation, int flags, int pitch)
    {
        if (!EmitSoundLogic(pEntity, channel, sample, volume, attenuation, flags, pitch))
        {
            Eng.EF.pfnEmitSound(pEntity, channel, sample, volume, attenuation, flags, pitch);
        }
    }

    private static bool EmitSoundLogic(edict_t* entity, int channel, sbyte* samplePtr, float volume, float attenuation, int flags, int pitch)
    {
        int id = Eng.IndexOf(entity);
        if (!Eng.IsConnected(id)) return false;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        int i = Eng.IsValid(activeItem) ? Store.GetWeaponType(activeItem) : 0;

        string sample = Marshal.PtrToStringAnsi((nint)samplePtr) ?? "";
        if (!sample.StartsWith("weapons/knife_") || i == 0 || Eng.GetPDataInt(activeItem, 43) != EngineConst.CswKnife)
            return false;

        int attack = Store.UserKnifeAttack[id];
        Store.UserKnifeAttack[id] = 0;
        bool isSlash = sample.Length > 16 && sample[14] == 's' && sample[15] == 'l' && sample[16] == 'a';
        bool isHit = sample.Length > 16 && sample[14] == 'h' && sample[15] == 'i' && sample[16] == 't';
        bool isStab = sample.Length > 16 && sample[14] == 's' && sample[15] == 't' && sample[16] == 'a';
        bool isDeploy = sample.Length > 16 && sample[14] == 'd' && sample[15] == 'e' && sample[16] == 'p';

        if (isSlash)
        {
            Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound3[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 1], 0, EngineConst.PitchNorm);
            if (attack == 1) Core.PlayAnim(id, Store.KnifeAnim[id, i, Store.KnifeSlashMode[id] + 1]);
            else Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabMiss]);
        }
        if (isHit)
        {
            bool hitWall = sample.Length > 17 && sample[17] == 'w';
            if (hitWall)
            {
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound2[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 0], 0, EngineConst.PitchNorm);
                if (attack == 1) Core.PlayAnim(id, Store.KnifeAnim[id, i, Store.KnifeSlashMode[id] + 1]);
                else Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab]);
            }
            else
            {
                if (Eng.RandomNum(0, 1) != 0)
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound5[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 3], 0, EngineConst.PitchNorm);
                else
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound6[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 4], 0, EngineConst.PitchNorm);
                Core.PlayAnim(id, Store.KnifeAnim[id, i, Store.KnifeSlashMode[id] + 1]);
            }
        }
        if (isStab)
        {
            Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound4[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 2], 0, EngineConst.PitchNorm);
            Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab]);
        }
        if (isDeploy)
        {
            Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound1[id, i], EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
        }
        return true;
    }

    // ---- fm_TraceLine(散布 / 刀子轨迹) ----

    [AmxxHook("pfnTraceLine", Table = HookTable.Engine)]
    public static void TraceLine(float* vectorStart, float* vectorEnd, int ignoredMonster, edict_t* pentToSkip, TraceResult* ptr)
    {
        if (!TraceLineLogic(vectorStart, vectorEnd, ignoredMonster, pentToSkip, ptr))
        {
            Eng.EF.pfnTraceLine(vectorStart, vectorEnd, ignoredMonster, pentToSkip, ptr);
        }
    }

    private static bool TraceLineLogic(float* vectorStart, float* vectorEnd, int ignoredMonster, edict_t* pentToSkip, TraceResult* ptr)
    {
        int id = Eng.IndexOf(pentToSkip);
        if (!Eng.IsConnected(id)) return false;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return false;

        int i = Store.GetWeaponType(activeItem);

        Span<float> start = stackalloc float[3] { vectorStart[0], vectorStart[1], vectorStart[2] };
        Span<float> end = stackalloc float[3] { vectorEnd[0], vectorEnd[1], vectorEnd[2] };

        if (Store.IsInPrimaryAttack)
        {
            Span<float> dir = stackalloc float[3];
            Core.VecSub(end, start, dir);
            dir[0] /= 8192.0f; dir[1] /= 8192.0f; dir[2] /= 8192.0f;
            Span<float> forward = stackalloc float[3] { Eng.GP->v_forward.x, Eng.GP->v_forward.y, Eng.GP->v_forward.z };
            Core.VecSub(dir, forward, dir);
            if (Core.GetUserZoom(id) != 1 || Store.SightMode[id] == WpnConst.Open)
                Core.VecMulScalar(dir, Store.WeaponAimAccuracy[id, i], dir);
            else if (Core.GetUserZoom(id) == 1 && Store.SightMode[id] != WpnConst.Open)
                Core.VecMulScalar(dir, Store.WeaponAccuracy[id, i], dir);
            else if ((Eng.GetPDataInt(activeItem, 74) & (1 << 2)) != 0 || (Eng.GetPDataInt(activeItem, 74) & (1 << 0)) != 0)
                Core.VecMulScalar(dir, Store.WeaponSilencedAccuracy[id, i], dir);
            else
                Core.VecMulScalar(dir, Store.WeaponAccuracy[id, i], dir);
            Core.VecAdd(dir, forward, dir);
            Core.VecMulScalar(dir, 8192.0f, dir);
            Core.VecAdd(dir, start, end);
            Eng.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
            return true;
        }

        if (Eng.GetPDataInt(activeItem, 43) != EngineConst.CswKnife || i == 0) return false;

        if ((Store.KnifeSlashTime[id, i] != 0.0f && Store.KnifeSlashAngle[id, i] != 0.0f) ||
            (Store.KnifeStabTime[id, i] != 0.0f && Store.KnifeSlashAngle[id, i] != 0.0f))
            return false;

        if ((pPlayer->v.button & EngineConst.InAttack) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            Core.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            Core.VecMulScalar(dir, Store.KnifeSlashRange[id, i], dir);
            Core.VecAdd(start, dir, end);
            Eng.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else if ((pPlayer->v.button & EngineConst.InAttack2) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            Core.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            Core.VecMulScalar(dir, Store.KnifeStabRange[id, i], dir);
            Core.VecAdd(start, dir, end);
            Eng.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        return true;
    }

    // ---- fm_TraceHull(刀子轨迹) ----

    [AmxxHook("pfnTraceHull", Table = HookTable.Engine)]
    public static void TraceHull(float* vectorStart, float* vectorEnd, int ignoredMonster, int hull, edict_t* pentToSkip, TraceResult* ptr)
    {
        if (!TraceHullLogic(vectorStart, vectorEnd, ignoredMonster, hull, pentToSkip, ptr))
        {
            Eng.EF.pfnTraceHull(vectorStart, vectorEnd, ignoredMonster, hull, pentToSkip, ptr);
        }
    }

    private static bool TraceHullLogic(float* vectorStart, float* vectorEnd, int ignoredMonster, int hull, edict_t* pentToSkip, TraceResult* ptr)
    {
        int id = Eng.IndexOf(pentToSkip);
        if (!Eng.IsConnected(id)) return false;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return false;

        int i = Store.GetWeaponType(activeItem);
        if (Eng.GetPDataInt(activeItem, 43) != EngineConst.CswKnife || i == 0) return false;

        if ((Store.KnifeSlashTime[id, i] != 0.0f && Store.KnifeSlashAngle[id, i] != 0.0f) ||
            (Store.KnifeStabTime[id, i] != 0.0f && Store.KnifeSlashAngle[id, i] != 0.0f))
            return false;

        Span<float> start = stackalloc float[3] { vectorStart[0], vectorStart[1], vectorStart[2] };
        Span<float> end = stackalloc float[3] { vectorEnd[0], vectorEnd[1], vectorEnd[2] };

        if ((pPlayer->v.button & EngineConst.InAttack) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            Core.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            Core.VecMulScalar(dir, Store.KnifeSlashRange[id, i], dir);
            Core.VecAdd(start, dir, end);
            Eng.TraceHull(start, end, ignoredMonster, hull, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else if ((pPlayer->v.button & EngineConst.InAttack2) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            Core.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            Core.VecMulScalar(dir, Store.KnifeStabRange[id, i], dir);
            Core.VecAdd(start, dir, end);
            Eng.TraceHull(start, end, ignoredMonster, hull, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        return true;
    }

    // ---- fm_PlayerPreThink(行走速度) ----

    [AmxxHook("pfnPlayerPreThink")]
    public static void PlayerPreThink(edict_t* pEntity)
    {
        PlayerPreThinkLogic(pEntity);
        ModuleRuntime.gpGamedllFuncs->dllapi_table->pfnPlayerPreThink(pEntity);
    }

    private static void PlayerPreThinkLogic(edict_t* pEntity)
    {
        int id = Eng.IndexOf(pEntity);
        if (!Eng.IsAlive(id)) return;

        if (!Store.RoundStart) return;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return;

        int i = Store.GetWeaponType(activeItem);
        int result = NativeFunctionRegister.Execute_WpnWeaponMaxSpeed(Eng.IndexOf(activeItem), id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (i == 0) return;

        if (Core.GetUserZoom(id) == 1 && Store.SightMode[id] != WpnConst.Open)
        {
            if (Store.WeaponMaxSpeed[id, i] != 0.0f)
            {
                pPlayer->v.maxspeed = Store.WeaponMaxSpeed[id, i];
                return;
            }
        }
        else if (Core.GetUserZoom(id) != 1 || Store.SightMode[id] == WpnConst.Open)
        {
            if (Store.WeaponAimMaxSpeed[id, i] != 0.0f)
            {
                pPlayer->v.maxspeed = Store.WeaponAimMaxSpeed[id, i];
                return;
            }
        }
        var maxSpeed = MF.Out<float>();
        MF.CallNative("ExecuteHam", EngineConst.HamCsItemGetMaxSpeed, Eng.IndexOf(activeItem), maxSpeed);
        pPlayer->v.maxspeed = maxSpeed.Value;
    }

    // ---- fm_PlayerPostThink_Post(刀子延迟攻击 + BOT 注册/购买) ----

    [AmxxHook("pfnPlayerPostThink", Post = true)]
    public static void PlayerPostThinkPost(edict_t* pEntity) => PlayerPostThinkLogic(pEntity);

    private static void PlayerPostThinkLogic(edict_t* pEntity)
    {
        int id = Eng.IndexOf(pEntity);
        if (!Eng.IsConnected(id)) return;

        if (Eng.IsBot(id) && Store.BotForwardRegister)
        {
            Store.BotForwardRegister = false;
            MF.CallNative("RegisterHamFromEntity", EngineConst.HamTakeDamage, id, "wt_TakeDamage", 0);
            MF.CallNative("RegisterHamFromEntity", EngineConst.HamTakeDamage, id, "wt_TakeDamagePost", 1);
            MF.CallNative("RegisterHamFromEntity", EngineConst.HamKilled, id, "wt_KilledPost", 1);
            MF.CallNative("RegisterHamFromEntity", EngineConst.HamSpawn, id, "wt_PlayerSpawnPost", 1);
        }

        // debug
        if (Store.BuyingAmmo[id]) Store.BuyingAmmo[id] = false;

        if (!Eng.IsAlive(id)) return;

        if (Eng.GameTime >= Store.BotNextThink[id] && Store.BotNextThink[id] != 0.0f)
        {
            Store.BotNextThink[id] = 0.0f;
            Buy.BotBuyWeaponEvent(id);
        }

        if (Store.PlayerThink[id] != 0.0f && Eng.GameTime - Store.PlayerThink[id] >= 0.0f)
        {
            var pPlayer = Eng.EntOfIndex(id);
            var knifeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 370);
            Store.PlayerThink[id] = 0.0f;
            if (!Eng.IsValid(knifeItem)) return;

            int i = Store.GetWeaponType(knifeItem);
            if (Store.KnifeAttack[id] == 1)
            {
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin] != 0)
                {
                    Store.KnifeAttack[id] = 2;
                    Attack.KnifeAttack(id, i, Store.KnifeAttack[id], knifeItem);
                    return;
                }
                else
                {
                    int result = NativeFunctionRegister.Execute_WpnPrimaryAttackEnd(Eng.IndexOf(knifeItem), id, i);
                    if (result == WpnConst.WPN_SUPERCEDE) return;

                    Store.KnifeAttack[id] = 2;
                    Store.PlayerThink[id] = Eng.GameTime + Store.KnifeSlashResetTime[id, i];
                    float range = Store.KnifeSlashRange[id, i];
                    float damage = 15.0f * Store.WeaponDamage[id, i];
                    float angle = Store.KnifeSlashAngle[id, i];
                    Span<float> origin = stackalloc float[3];
                    origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;
                    Span<float> param = stackalloc float[6];
                    param[0] = range;
                    param[1] = angle;
                    param[2] = Store.KnifeSlashAngleOffset[id, i];
                    param[3] = Store.KnifeSlashHeight[id, i];
                    param[4] = Store.KnifeSlashHeightOffset[id, i];
                    param[5] = damage;
                    int hit = Attack.CheckAttack(id, 0, knifeItem, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
                    if (hit == WpnConst.HitEmpty)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, Store.WeaponSound3[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 1], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitPlayer)
                    {
                        if (Store.KnifeSlashMode[id] != 0)
                            Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, Store.WeaponSound5[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 3], 0, EngineConst.PitchNorm);
                        else
                            Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, Store.WeaponSound6[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 4], 0, EngineConst.PitchNorm);
                    }
                    if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, Store.WeaponSound2[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 0], 0, EngineConst.PitchNorm);
                    NativeFunctionRegister.Execute_WpnPrimaryAttackEndPost(Eng.IndexOf(knifeItem), id, i);
                    return;
                }
            }
            else if (Store.KnifeAttack[id] == 2)
            {
                Store.KnifeAttack[id] = 0;
                return;
            }
            else if (Store.KnifeAttack[id] == 3)
            {
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin] != 0)
                {
                    Store.KnifeAttack[id] = 4;
                    Attack.KnifeAttack(id, i, Store.KnifeAttack[id], knifeItem);
                    return;
                }
                else
                {
                    int result = NativeFunctionRegister.Execute_WpnSecondaryAttackEnd(Eng.IndexOf(knifeItem), id, i);
                    if (result == WpnConst.WPN_SUPERCEDE) return;

                    Store.KnifeAttack[id] = 4;
                    Store.PlayerThink[id] = Eng.GameTime + Store.KnifeStabResetTime[id, i];
                    float range = Store.KnifeStabRange[id, i];
                    float damage = 65.0f * Store.WeaponDamage2[id, i];
                    float angle = Store.KnifeStabAngle[id, i];
                    Span<float> origin = stackalloc float[3];
                    origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;
                    Span<float> param = stackalloc float[6];
                    param[0] = range;
                    param[1] = angle;
                    param[2] = Store.KnifeStabAngleOffset[id, i];
                    param[3] = Store.KnifeStabHeight[id, i];
                    param[4] = Store.KnifeStabHeightOffset[id, i];
                    param[5] = damage;
                    int hit = Attack.CheckAttack(id, 0, knifeItem, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
                    if (hit == WpnConst.HitEmpty)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, Store.WeaponSound3[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 1], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitPlayer)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, Store.WeaponSound4[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 2], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, Store.WeaponSound2[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 0], 0, EngineConst.PitchNorm);
                    NativeFunctionRegister.Execute_WpnSecondaryAttackEndPost(Eng.IndexOf(knifeItem), id, i);
                    return;
                }
            }
            else if (Store.KnifeAttack[id] == 4)
            {
                Store.KnifeAttack[id] = 0;
                return;
            }
        }
    }

    // ---- fm_Think_Post(枪口烟雾动画) ----

    [AmxxHook("pfnThink", Post = true)]
    public static void ThinkPost(edict_t* pEntity)
    {
        if (!Eng.IsValid(pEntity)) return;

        string className = Eng.ClassName(pEntity);
        if (className != "gunsmoke1") return;

        var pev = &pEntity->v;
        float frame = pev->frame;
        float scale = pev->scale;
        int num = pev->iuser1;
        float velocityX = pev->velocity.x;
        float velocityY = pev->velocity.y;
        float nextThink = 0.005f;
        frame += 0.2f;
        scale += 0.01f;
        if (num != 0)
        {
            velocityX -= Eng.RandomFloat(1.0f, 5.0f);
            velocityY -= Eng.RandomFloat(1.0f, 5.0f);
        }
        else
        {
            velocityX += Eng.RandomFloat(1.0f, 5.0f);
            velocityY += Eng.RandomFloat(1.0f, 5.0f);
        }
        frame = MathF.Min(10.0f, frame);
        scale = MathF.Min(1.0f, scale);
        pev->frame = frame;
        pev->scale = scale;
        pev->velocity.x = velocityX;
        pev->velocity.y = velocityY;
        pev->nextthink = Eng.GameTime + nextThink;
        if (frame == 10.0f) Eng.RemoveEntity(pEntity);
    }
}
