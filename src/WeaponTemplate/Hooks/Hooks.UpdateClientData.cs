using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class Hooks
{
    // ---- fm_UpdateClientData_Post(阻止客户端预测开枪) ----
    // 注:本 SDK 的 DLL_FUNCTIONS.pfnUpdateClientData 声明参数顺序为 (edict_t*, int, clientdata_t*)。

    [AmxxHook("pfnUpdateClientData", Post = true)]
    public static void UpdateClientDataPost(edict_t* pEntity, int sendWeapons, clientdata_t* cd)
    {
        HookResult.Ignore();
        int id = Eng.IndexOf(pEntity);
        if (!Eng.IsAlive(id)) return;

        var pPlayer = Eng.EntOfIndex(id);
        var entity = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(entity)) return;

        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return;

        // 用于阻止客户端的开枪
        cd->m_flNextAttack = Eng.GameTime + 0.001f;
    }
}
