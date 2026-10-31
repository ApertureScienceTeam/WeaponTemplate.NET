using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class Hooks
{
    // ---- fm_SetModel(EngFunc_SetModel hook) ----

    [AmxxHook("pfnSetModel", Table = HookTable.Engine)]
    public static void SetModel(edict_t* pEntity, sbyte* model)
    {
        HookResult.Ignore();
        if (SetModelLogic(pEntity)) HookResult.Supercede();
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

            int i = WeaponStore.GetWeaponType(activeItem);
            if (i == 0) return false;

            Eng.SetPDataInt(entity, 11, i);
            entity->v.dmgtime = Eng.GameTime + WeaponStore.Weapon(id, i).GrenadeData[1];
            Eng.SetModel(entity, WeaponStore.Weapon(id, i).WModel);
            if (WeaponStore.Weapon(id, i).WModelBody != 0) entity->v.body = WeaponStore.Weapon(id, i).WModelBody;
            return !string.IsNullOrEmpty(WeaponStore.Weapon(id, i).WModel);
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

        int type = WeaponStore.GetWeaponType(weapon);
        if (type == 0) return false;

        int ownerId = Eng.IndexOf(entity->v.owner);
        if (WeaponStore.Weapon(ownerId, type).Id == EngineConst.CswHegrenade)
        {
            entity->v.nextthink = Eng.GameTime + 0.1f;
            return true;
        }
        Eng.SetModel(entity, WeaponStore.Weapon(ownerId, type).WModel);
        if (WeaponStore.Weapon(ownerId, type).WModelBody != 0) entity->v.body = WeaponStore.Weapon(ownerId, type).WModelBody;
        return !string.IsNullOrEmpty(WeaponStore.Weapon(ownerId, type).WModel);
    }
}
