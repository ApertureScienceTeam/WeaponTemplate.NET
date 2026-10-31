using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class Hooks
{
    // pfnPlaybackEvent 事件的 host-only 标志位
    private const int FevHostOnly = 1 << 1;

    // ---- fm_PlaybackEvent(EngFunc_PlaybackEvent hook,命中率修正) ----

    [AmxxHook("pfnPlaybackEvent", Table = HookTable.Engine)]
    public static void PlaybackEvent(int flags, edict_t* pInvoker, ushort eventIndex, float delay, float* origin, float* angles,
        float fParam1, float fParam2, int iParam1, int iParam2, int bParam1, int bParam2)
    {
        HookResult.Ignore();
        int id = Eng.IndexOf(pInvoker);
        if (!Eng.IsConnected(id) || !WeaponStore.IsInPrimaryAttack) { return; }

        var pPlayer = Eng.EntOfIndex(id);
        var entity = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        int i = Eng.IsValid(entity) ? WeaponStore.GetWeaponType(entity) : 0;
        if (i == 0 || WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) { return; }

        float recoil;
        if (PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open) recoil = WeaponStore.Weapon(id, i).AimAccuracy;
        else recoil = WeaponStore.Weapon(id, i).Accuracy;
        Eng.PlaybackEvent(flags | FevHostOnly, pInvoker, eventIndex, delay, origin, angles, fParam1 * recoil, fParam2 * recoil, iParam1, iParam2, bParam1, bParam2);
        HookResult.Supercede();
    }
}
