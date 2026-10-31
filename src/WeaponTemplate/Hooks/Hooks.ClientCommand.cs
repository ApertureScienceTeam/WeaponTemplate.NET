using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// 原版 fakemeta forward(FM_*) 到 metamod 函数表 hook 的映射:
//   FM_ClientCommand            -> GameDll pfnClientCommand(pre, 通过 mres 阻止原版)
//   FM_SetModel                 -> Engine  pfnSetModel(pre)
//   FM_UpdateClientData_Post    -> GameDll pfnUpdateClientData(post)
//   FM_PlaybackEvent            -> Engine  pfnPlaybackEvent(pre)
//   FM_EmitSound                -> Engine  pfnEmitSound(pre)
//   FM_TraceLine / FM_TraceHull -> Engine  pfnTraceLine / pfnTraceHull(pre)
//   FM_PlayerPreThink           -> GameDll pfnPlayerPreThink(pre)
//   FM_PlayerPostThink_Post     -> GameDll pfnPlayerPostThink(post)
//   FM_Think_Post               -> GameDll pfnThink(post)
// 未处理时由 Metamod 调原版;替换时才直接调用引擎并设置 SUPERCEDE。
public static unsafe partial class Hooks
{
    // ---- 购买命令(fm_ClientCommand) ----

    [AmxxHook("pfnClientCommand")]
    public static void ClientCommand(edict_t* pEntity)
    {
        HookResult.Ignore();
        int id = Eng.IndexOf(pEntity);
        string command = Marshal.PtrToStringAnsi((nint)Eng.EF.pfnCmd_Argv(0)) ?? "";
        bool handled = ClientCommandLogic(id, command);
        if (handled) HookResult.Supercede();
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

            if (!Buy.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 368);
            if (!Eng.IsValid(entity)) return false;

            int type = WeaponStore.GetWeaponType(entity);
            if (WeaponStore.Weapon(id, type).Clip == 0 && type != 0)
            {
                WeaponAmmo.GivePriAmmo(id, entity, type, 1, 0, 0);
                return true;
            }
            WeaponAmmo.GivePriAmmo(id, entity, type, 0, 0, 0);
            return true;
        }
        if (command == "buyammo2")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 369);
            if (!Eng.IsValid(entity)) return false;

            int type = WeaponStore.GetWeaponType(entity);
            if (WeaponStore.Weapon(id, type).Clip == 0 && type != 0)
            {
                WeaponAmmo.GiveSecAmmo(id, entity, type, 1, 0, 0);
                return true;
            }
            WeaponAmmo.GiveSecAmmo(id, entity, type, 0, 0, 0);
            return true;
        }
        if (command == "primammo")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 368);
            if (!Eng.IsValid(entity)) return false;

            int type = WeaponStore.GetWeaponType(entity);
            if (WeaponStore.Weapon(id, type).Clip == 0 && type != 0)
            {
                while (WeaponAmmo.GivePriAmmo(id, entity, type, 1, 0, 0)) { }
                return true;
            }
            while (WeaponAmmo.GivePriAmmo(id, entity, type, 0, 0, 0)) { }
            return true;
        }
        if (command == "secammo")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 369);
            if (!Eng.IsValid(entity)) return false;

            int type = WeaponStore.GetWeaponType(entity);
            if (WeaponStore.Weapon(id, type).Clip == 0 && type != 0)
            {
                while (WeaponAmmo.GiveSecAmmo(id, entity, type, 1, 0, 0)) { }
                return true;
            }
            while (WeaponAmmo.GiveSecAmmo(id, entity, type, 0, 0, 0)) { }
            return true;
        }
        if (command == "shield") return true;

        if (command == "normal_knife")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

            var entity = Eng.GetPDataCbase(pPlayer, 370);
            var newEntity = WeaponGive.GiveWeapon(id, "weapon_knife", 0);
            if (!Eng.IsValid(entity)) entity = newEntity;
            WeaponGive.ChangeWeaponList(id, newEntity);
            if (Eng.IsValid(entity) && Eng.GetPDataInt(entity, 43) == EngineConst.CswKnife)
            {
                HamSandwich.ExecuteHamB(EngineConst.HamItemDeploy, Eng.IndexOf(entity));
                PlayerStocks.PlayAnim(id, WpnConst.KnifeAnimDraw);
                Eng.SetPDataFloat(id, 83, 0.7f);
            }
            return true;
        }
        if (command == "vest")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

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
            PlayerStocks.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 650);
            Eng.EmitSound(pPlayer, EngineConst.ChanItem, "items/ammopickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
            return true;
        }
        if (command == "vesthelm")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

            if (pPlayer->v.armorvalue >= 100.0f && Eng.GetPDataInt(pPlayer, 112) == 2) return true;

            if ((Eng.GetPDataInt(pPlayer, 115) < 1000 && pPlayer->v.armorvalue < 100.0f) ||
                (Eng.GetPDataInt(pPlayer, 115) < 350 && pPlayer->v.armorvalue >= 100.0f))
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            if (Eng.GetPDataInt(pPlayer, 112) == 1) PlayerStocks.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 350);
            else PlayerStocks.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 1000);
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

            if (!Buy.CheckBuy(id)) return true;

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
            PlayerStocks.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 200);
            Eng.EmitSound(pPlayer, EngineConst.ChanAuto, "items/gunpickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
            return true;
        }
        if (command == "nvgs")
        {
            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

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
            PlayerStocks.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - 1250);
            return true;
        }

        // 原版武器购买命令
        for (int i = 0; i < GameTables.GameWeaponBuyCommand.Length; i++)
        {
            if (command != GameTables.GameWeaponBuyCommand[i] || GameTables.GameWeaponBuyCommand[i].Length == 0)
                continue;

            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (!Buy.CheckBuy(id)) return true;

            if (Eng.GetPDataInt(pPlayer, 114) != GameTables.GameWeaponDefaultTeam[i] && GameTables.GameWeaponDefaultTeam[i] != 0 && WeaponStore.AllTeamBuy == 0)
            {
                Eng.ClientPrintCenter(id, $"对于你的队伍来说还不能购买\"{GameTables.GameWeaponBuyCommand[i]}\"。");
                return true;
            }
            if (Eng.GetPDataInt(pPlayer, 115) < GameTables.GameWeaponDefaultCost[i])
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
            var entity = WeaponGive.GiveWeapon(id, GameTables.GameWeaponClassName[i], 0);
            if (Eng.IsValid(entity))
            {
                PlayerStocks.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - GameTables.GameWeaponDefaultCost[i]);
                if (PlayerStocks.GetUserBpAmmo(id, i) > WeaponStore.GameWeaponMaxBpammo[i]) PlayerStocks.SetUserBpAmmo(id, i, WeaponStore.GameWeaponMaxBpammo[i]);
                NativeFunctionRegister.Execute_WpnBuyWeaponPost(Eng.IndexOf(entity), id, 0);
            }
            return true;
        }

        // 模板武器购买命令
        for (int i = 1; i <= WeaponStore.WeaponAmount; i++)
        {
            string weaponListCommand = $"weapon_{WeaponStore.Weapon(id, i).List}";
            if (command == weaponListCommand) Eng.EngClientCommand(id, GameTables.GameWeaponClassName[WeaponStore.Weapon(id, i).Id]);
            if (string.IsNullOrEmpty(WeaponStore.Weapon(id, i).BuyCommand) || command != WeaponStore.Weapon(id, i).BuyCommand)
                continue;

            int result = NativeFunctionRegister.Execute_WpnBuyWeapon(id, command);
            if (result == WpnConst.WPN_SUPERCEDE) return true;

            if (command == "empty") return true;

            if (!Buy.CheckBuy(id)) return true;

            if (Eng.GetPDataInt(pPlayer, 114) != WeaponStore.Weapon(id, i).Team && WeaponStore.Weapon(id, i).Team != 0)
            {
                Eng.ClientPrintCenter(id, $"对于你的队伍来说还不能购买\"{WeaponStore.Weapon(id, i).List}\"。");
                return true;
            }
            if (Eng.GetPDataInt(pPlayer, 115) < WeaponStore.Weapon(id, i).Cost)
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Not_Enough_Money");
                return true;
            }
            MF.Log($"[diag] buy '{command}' tpl={i} csw={WeaponStore.Weapon(id, i).Id} cls={GameTables.GameWeaponClassName[WeaponStore.Weapon(id, i).Id]} money={Eng.GetPDataInt(pPlayer, 115)} cost={WeaponStore.Weapon(id, i).Cost}");
            int entityIndex = WeaponGive.GiveWeaponByTemplate(id, i);
            bool given = WeaponGive.PlayerHasItem(id, entityIndex);
            MF.Log($"[diag] buy give idx={entityIndex} valid={Eng.IsValidIndex(entityIndex)} owned={given}");
            if (given)
            {
                PlayerStocks.SetUserMoney(id, Eng.GetPDataInt(pPlayer, 115) - WeaponStore.Weapon(id, i).Cost);

                // 防止备弹超过上限
                if (PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id) > WeaponStore.Weapon(id, i).Ammo &&
                    WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && WeaponStore.Weapon(id, i).Id != EngineConst.CswKnife)
                    PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, WeaponStore.Weapon(id, i).Ammo);
                NativeFunctionRegister.Execute_WpnBuyWeaponPost(entityIndex, id, i);
            }
            else MF.Log($"[diag] buy skipped charge: weapon not in player inventory (idx={entityIndex})");
            return true;
        }
        return false;
    }
}
