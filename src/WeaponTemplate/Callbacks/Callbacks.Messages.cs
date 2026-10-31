using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// register_message 回调(wt_DeathMsg / wt_TextMsg / wt_TempEntity / wt_StatusIcon)。
public static unsafe partial class Callbacks
{
    [AmxxCallback("wt_DeathMsg")]
    public static int OnDeathMsg(int msgId, int msgDest, int msgEntity)
    {
        int id = AmxModX.GetMsgArgInt(1);
        int victim = AmxModX.GetMsgArgInt(2);
        int headshot = AmxModX.GetMsgArgInt(3);
        AmxModX.GetMsgArgString(4, out var weaponArg, 31);
        string weapon = weaponArg ?? "";

        if (id == 0)
        {
            int result = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, weapon);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            return EngineConst.PluginContinue;
        }

        // 手雷击杀图标
        if (weapon == "grenade")
        {
            if (WeaponStore.Player(id).UserGrenadeId != 0)
            {
                string spr = WeaponStore.Weapon(id, WeaponStore.Player(id).UserGrenadeId).Spr;
                AmxModX.SetMsgArgString(4, spr);
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
        if (WeaponStore.Player(id).AttackerEntity != 0)
        {
            entityIndex = WeaponStore.Player(id).AttackerEntity;
            WeaponStore.Player(id).AttackerEntity = 0;
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
        int i = WeaponGive.WeaponTypeByIndex(entityIndex);
        if (i == 0)
        {
            int result = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, weapon);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            return EngineConst.PluginContinue;
        }
        string killSpr = WeaponStore.Weapon(id, i).Spr;
        AmxModX.SetMsgArgString(4, killSpr);
        int finalResult = NativeFunctionRegister.Execute_WpnDeathMsg(id, victim, headshot, killSpr);
        if (finalResult == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
        return EngineConst.PluginContinue;
    }

    [AmxxCallback("wt_TextMsg")]
    public static int OnTextMsg(int msgId, int msgDest, int msgEntity)
    {
        AmxModX.GetMsgArgString(2, out var textArg, 31);
        string text = textArg ?? "";

        // 重置弹药
        if (text == "#Game_will_restart_in" || text == "#Game_Commencing") WeaponStore.RoundRestart = true;
        return EngineConst.PluginContinue;
    }

    [AmxxCallback("wt_TempEntity")]
    public static int OnTempEntity(int msgId, int msgDest, int msgEntity)
    {
        if (WeaponStore.BloodColor == 0) return EngineConst.PluginContinue;

        // 设置血液颜色
        int first = AmxModX.GetMsgArgInt(1);
        if (first == EngineConst.TeBloodSprite || first == EngineConst.TeBlood || first == EngineConst.TeBloodstream)
        {
            int color = AmxModX.GetMsgArgInt(7);
            int result = NativeFunctionRegister.Execute_WpnHookSpawnBlood(color);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            AmxModX.SetMsgArgInt(7, EngineConst.ArgByte, WeaponStore.BloodColor);
        }
        return EngineConst.PluginContinue;
    }

    [AmxxCallback("wt_StatusIcon")]
    public static int OnStatusIcon(int msgId, int msgDest, int msgEntity)
    {
        AmxModX.GetMsgArgString(2, out var statusArg, 32);
        string statusName = statusArg ?? "";

        // 出现buyzone图标时说明BOT在购买区
        if (statusName == "buyzone")
        {
            if (!Eng.IsConnected(msgEntity)) return EngineConst.PluginContinue;
            if (!Eng.IsBot(msgEntity)) return EngineConst.PluginContinue;
            if (!WeaponStore.Player(msgEntity).BotBuyWeapon) return EngineConst.PluginContinue;
            WeaponStore.Player(msgEntity).BotNextThink = Eng.GameTime + 0.1f;
        }
        return EngineConst.PluginContinue;
    }
}
