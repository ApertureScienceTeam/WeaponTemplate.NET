using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// 弹药补给与霰弹枪逐发装填。
public static unsafe class WeaponAmmo
{
    // public GivePriAmmo(id, iEntity, WpnId, iMode, iNative, iGiveAmount)
    public static bool GivePriAmmo(int id, edict_t* entity, int wpnId, int mode, int nativeBuy, int giveAmount)
    {
        WeaponStore.Player(id).BuyingAmmo = true;
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return false;

        int money = Eng.GetPDataInt(pPlayer, 115);
        int weapon = Eng.GetPDataInt(entity, 43);
        int ammo = wpnId != 0 ? WeaponStore.Player(id).BPAmmo[wpnId] : WeaponStore.Player(id).GameBpAmmo[weapon];
        int maxAmmo = wpnId != 0 ? WeaponStore.Weapon(id, wpnId).Ammo : WeaponStore.GameWeaponMaxBpammo[weapon];
        int amount, cost;

        for (int i = 0; i < WpnConst.MaxPrim; i++)
        {
            if (mode == 0)
            {
                amount = wpnId != 0 ? Math.Min(WeaponStore.Weapon(id, wpnId).Clip, 30) : GameTables.PrimAmount[i];
            }
            else amount = 1;
            if (giveAmount != 0) amount = giveAmount;
            cost = wpnId != 0 ? WeaponStore.Weapon(id, wpnId).AmmoCost : GameTables.PrimCost[i];
            if (nativeBuy != 0) cost = 0;

            if (weapon == GameTables.PrimId[i] && money >= cost)
            {
                if (mode != 0 && Eng.GetPDataInt(entity, 51) >= maxAmmo && wpnId != 0) return false;
                if (mode == 0 && ammo >= maxAmmo && wpnId != 0) return false;
                if (wpnId == 0 && ammo >= maxAmmo) return false;

                if (ammo + amount > maxAmmo) amount = maxAmmo - ammo;
                Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoPickup"), null, pPlayer);
                Eng.WriteByte(GameTables.GameWeaponAmmoId[weapon]);
                Eng.WriteByte(amount);
                Eng.MessageEnd();
                Eng.EmitSound(entity, EngineConst.ChanWeapon, "items/9mmclip1.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
                ammo += amount;
                money -= cost;
                if (mode != 0) Eng.SetPDataInt(entity, 51, ammo);
                if (Eng.GetPDataCbase(pPlayer, 373) == entity) PlayerStocks.SetUserBpAmmo(id, weapon, ammo);
                if (wpnId == 0) WeaponStore.Player(id).GameBpAmmo[weapon] = ammo;
                else WeaponStore.Player(id).BPAmmo[wpnId] = ammo;
                PlayerStocks.SetUserMoney(id, money);
                return true;
            }
        }
        return false;
    }

    // public GiveSecAmmo(id, iEntity, WpnId, iMode, iNative, iGiveAmount)
    public static bool GiveSecAmmo(int id, edict_t* entity, int wpnId, int mode, int nativeBuy, int giveAmount)
    {
        WeaponStore.Player(id).BuyingAmmo = true;
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return false;

        int money = Eng.GetPDataInt(pPlayer, 115);
        int weapon = Eng.GetPDataInt(entity, 43);
        int ammo = wpnId != 0 ? WeaponStore.Player(id).BPAmmo[wpnId] : WeaponStore.Player(id).GameBpAmmo[weapon];
        int maxAmmo = wpnId != 0 ? WeaponStore.Weapon(id, wpnId).Ammo : WeaponStore.GameWeaponMaxBpammo[weapon];
        int amount, cost;

        for (int i = 0; i < WpnConst.MaxSec; i++)
        {
            if (mode == 0)
            {
                amount = wpnId != 0 ? Math.Min(WeaponStore.Weapon(id, wpnId).Clip, 30) : GameTables.SecAmount[i];
            }
            else amount = 1;
            if (giveAmount != 0) amount = giveAmount;
            cost = wpnId != 0 ? WeaponStore.Weapon(id, wpnId).AmmoCost : GameTables.SecondCost[i];
            if (nativeBuy != 0) cost = 0;

            if (weapon == GameTables.SecondId[i] && money >= cost)
            {
                if (mode != 0 && Eng.GetPDataInt(entity, 51) >= maxAmmo && wpnId != 0) return false;
                if (mode == 0 && ammo >= maxAmmo && wpnId != 0) return false;
                if (wpnId == 0 && ammo >= maxAmmo) return false;

                if (ammo + amount > maxAmmo) amount = maxAmmo - ammo;
                Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoPickup"), null, pPlayer);
                Eng.WriteByte(GameTables.GameWeaponAmmoId[weapon]);
                Eng.WriteByte(amount);
                Eng.MessageEnd();
                Eng.EmitSound(entity, EngineConst.ChanWeapon, "items/9mmclip1.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
                ammo += amount;
                money -= cost;
                if (mode != 0) Eng.SetPDataInt(entity, 51, ammo);
                if (Eng.GetPDataCbase(pPlayer, 373) == entity) PlayerStocks.SetUserBpAmmo(id, weapon, ammo);
                if (wpnId == 0) WeaponStore.Player(id).GameBpAmmo[weapon] = ammo;
                else WeaponStore.Player(id).BPAmmo[wpnId] = ammo;
                PlayerStocks.SetUserMoney(id, money);
                return true;
            }
        }
        return false;
    }

    // public Native_GiveAmmo(id, iEntity, i, iFull)
    public static void GiveAmmoForWeapon(int id, int entityIndex, int wpnId, int full)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;

        for (int i = 0; i < WpnConst.MaxPrim; i++)
        {
            if (Eng.GetPDataInt(entity, 43) == GameTables.PrimId[i])
            {
                if (WeaponStore.Weapon(id, wpnId).Clip == 0)
                {
                    if (wpnId != 0)
                    {
                        if (full != 0) while (GivePriAmmo(id, entity, wpnId, 1, 1, 0)) { }
                        else GivePriAmmo(id, entity, wpnId, 1, 1, 0);
                    }
                    else
                    {
                        if (full != 0) while (GivePriAmmo(id, entity, wpnId, 0, 1, 0)) { }
                        else GivePriAmmo(id, entity, wpnId, 0, 1, 0);
                    }
                }
                else
                {
                    if (full != 0) while (GivePriAmmo(id, entity, wpnId, 0, 1, 0)) { }
                    else GivePriAmmo(id, entity, wpnId, 0, 1, 0);
                }
            }
        }
        for (int i = 0; i < WpnConst.MaxSec; i++)
        {
            if (Eng.GetPDataInt(entity, 43) == GameTables.SecondId[i])
            {
                if (WeaponStore.Weapon(id, wpnId).Clip == 0)
                {
                    if (wpnId != 0)
                    {
                        if (full != 0) while (GiveSecAmmo(id, entity, wpnId, 1, 1, 0)) { }
                        else GiveSecAmmo(id, entity, wpnId, 1, 1, 0);
                    }
                    else
                    {
                        if (full != 0) while (GiveSecAmmo(id, entity, wpnId, 0, 1, 0)) { }
                        else GiveSecAmmo(id, entity, wpnId, 0, 1, 0);
                    }
                }
                else
                {
                    if (full != 0) while (GiveSecAmmo(id, entity, wpnId, 0, 1, 0)) { }
                    else GiveSecAmmo(id, entity, wpnId, 0, 1, 0);
                }
            }
        }
    }

    // public ShotgunReload(iEntity, iMaxClip, iClip, iBpAmmo, id, WeaponId)
    public static void ShotgunReload(edict_t* entity, int maxClip, int clip, int bpAmmo, int id, int weaponId)
    {
        int i = WeaponStore.GetWeaponType(entity);
        if (bpAmmo <= 0 || maxClip == clip) return;

        if (Eng.GetPDataFloat(entity, 46) > 0.0f) return;

        switch (Eng.GetPDataInt(entity, 55))
        {
            case 0:
            {
                // 开始换弹
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimStartReload]);
                Eng.SetPDataInt(entity, 55, 1);
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).ReloadTime[1]);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).ReloadTime[1]);
                Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).ReloadTime[1]);
                return;
            }
            case 1:
            {
                if (Eng.GetPDataFloat(entity, 48) > 0.0f) return;

                // 装弹动作
                Eng.SetPDataInt(entity, 55, 2);
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimInsert]);
                Eng.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).ReloadTime[0]);
                Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).ReloadTime[0]);
                Eng.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).ReloadTime[0]);
                break;
            }
            default:
            {
                // 子弹+1
                Eng.SetPDataInt(entity, 51, clip + 1);
                PlayerStocks.SetUserBpAmmo(id, weaponId, bpAmmo - 1);
                WeaponStore.Player(id).BPAmmo[i] = bpAmmo - 1;
                Eng.SetPDataInt(entity, 55, 1);
                break;
            }
        }
    }

    // stock GetWeaponSlot(id, WpnID, slot)
    public static bool GetWeaponSlot(int id, int wpnId, int slot)
    {
        if (slot == 1) for (int i = 0; i < WpnConst.MaxPrim; i++) if (WeaponStore.Weapon(id, wpnId).Id == GameTables.PrimId[i]) return true;
        if (slot == 2) for (int i = 0; i < WpnConst.MaxSec; i++) if (WeaponStore.Weapon(id, wpnId).Id == GameTables.SecondId[i]) return true;
        if (slot == 3) if (WeaponStore.Weapon(id, wpnId).Id == EngineConst.CswKnife) return true;
        if (slot == 4) if (WeaponStore.Weapon(id, wpnId).Id == EngineConst.CswHegrenade) return true;
        return false;
    }
}
