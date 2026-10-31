using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// 武器表现事件:wt_CurWeapon(视/枪模型切换)与 wt_DeathMsg(武器击杀图标)。
public static unsafe partial class WeaponView
{
    [AmxxCallback("wt_CurWeapon")]
    public static void OnCurWeaponEvent(int id) => EventCurWeapon(id);

    // public Event_CurWeapon(id) — 同时被 CurWeapon 事件与 Ham_UseStationary 复用
    public static void EventCurWeapon(int id)
    {
        if (!Engine.IsAlive(id)) return;

        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return;
        var pEntity = Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        if (!Engine.IsValid(pEntity)) return;

        int i = WeaponStore.GetWeaponType(pEntity);
        if (i == 0) return;

        // 开镜时取消V模型
        if (PlayerStocks.GetUserZoom(id) == 2 || PlayerStocks.GetUserZoom(id) == 3) Engine.SetViewModel(pPlayer, "");
        else Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
        Engine.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);

        // 无弹夹子弹的武器
        if (WeaponStore.Weapon(id, i).Clip == 0 && WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && WeaponStore.Weapon(id, i).Id != EngineConst.CswKnife)
        {
            int clip = Engine.GetPDataInt(pEntity, CsOffsets.ItemClip);
            PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, clip);
            Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("CurWeapon"), null, pPlayer);
            Engine.WriteByte(1);
            Engine.WriteByte(WeaponStore.Weapon(id, i).Id);
            Engine.WriteByte(0xFF); // Pawn write_byte(-1) 的单字节效果
            Engine.MessageEnd();
            Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("AmmoX"), null, pPlayer);
            Engine.WriteByte(GameTables.GameWeaponAmmoId[WeaponStore.Weapon(id, i).Id]);
            Engine.WriteByte(clip);
            Engine.MessageEnd();
        }

        // 机瞄V模型
        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode == WpnConst.Open)
            Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).SightModel);
    }

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
            var pPlayer = Engine.EntOfIndex(id);
            for (int j = 0; j < GameTables.GameWeaponKillSpr.Length; j++)
            {
                if (GameTables.GameWeaponKillSpr[j].Length == 0) continue;
                if (GameTables.GameWeaponKillSpr[j] != weapon) continue;
                entityIndex = pPlayer == null ? 0 : Engine.IndexOf(Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem));
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
}
