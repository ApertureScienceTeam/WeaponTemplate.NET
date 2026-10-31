using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// register_event / register_logevent 回调(wt_CurWeapon / wt_RoundStart / wt_FreezeEnd)
// 与对应的处理逻辑(当前武器模型切换、回合状态推进)。
public static unsafe partial class Callbacks
{
    // ---- 事件回调 ----

    [AmxxCallback("wt_CurWeapon")]
    public static void OnCurWeaponEvent(int id) => Callbacks.EventCurWeapon(id);

    [AmxxCallback("wt_RoundStart")]
    public static void OnRoundStartEvent(int id) => Callbacks.EventRoundStart();

    [AmxxCallback("wt_FreezeEnd")]
    public static void OnFreezeEnd() => WeaponStore.RoundStart = true;
    // public Event_CurWeapon(id) — 同时被 CurWeapon 事件与 Ham_UseStationary 复用
    public static void EventCurWeapon(int id)
    {
        if (!Eng.IsAlive(id)) return;

        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return;
        var pEntity = Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(pEntity)) return;

        int i = WeaponStore.GetWeaponType(pEntity);
        if (i == 0) return;

        // 开镜时取消V模型
        if (PlayerStocks.GetUserZoom(id) == 2 || PlayerStocks.GetUserZoom(id) == 3) Eng.SetViewModel(pPlayer, "");
        else Eng.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
        Eng.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);

        // 无弹夹子弹的武器
        if (WeaponStore.Weapon(id, i).Clip == 0 && WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && WeaponStore.Weapon(id, i).Id != EngineConst.CswKnife)
        {
            int clip = Eng.GetPDataInt(pEntity, 51);
            PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, clip);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("CurWeapon"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.WriteByte(WeaponStore.Weapon(id, i).Id);
            Eng.WriteByte(0xFF); // Pawn write_byte(-1) 的单字节效果
            Eng.MessageEnd();
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoX"), null, pPlayer);
            Eng.WriteByte(GameTables.GameWeaponAmmoId[WeaponStore.Weapon(id, i).Id]);
            Eng.WriteByte(clip);
            Eng.MessageEnd();
        }

        // 机瞄V模型
        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode == WpnConst.Open)
            Eng.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).SightModel);
    }

    // public Event_Round_Start(HLTV)
    public static void EventRoundStart()
    {
        WeaponStore.DisableBuyTime = Eng.GameTime + Eng.CvarFloat("mp_buytime") * 60.0f;
        WeaponStore.RoundStart = false;
        WeaponStore.Round++;
        if (WeaponStore.RoundRestart)
        {
            WeaponStore.RoundRestart = false;
            foreach (var player in WeaponStore.Players.Values) player.ClearAmmo();
        }
    }
}
