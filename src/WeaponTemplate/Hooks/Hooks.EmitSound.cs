using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class Hooks
{
    // ---- fm_EmitSound(刀子声音替换) ----

    [AmxxHook("pfnEmitSound", Table = HookTable.Engine)]
    public static void EmitSound(edict_t* pEntity, int channel, sbyte* sample, float volume, float attenuation, int flags, int pitch)
    {
        HookResult.Ignore();
        if (EmitSoundLogic(pEntity, channel, sample, volume, attenuation, flags, pitch))
            HookResult.Supercede();
    }

    private static bool EmitSoundLogic(edict_t* entity, int channel, sbyte* samplePtr, float volume, float attenuation, int flags, int pitch)
    {
        int id = Eng.IndexOf(entity);
        if (!Eng.IsConnected(id)) return false;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        int i = Eng.IsValid(activeItem) ? WeaponStore.GetWeaponType(activeItem) : 0;

        string sample = Marshal.PtrToStringAnsi((nint)samplePtr) ?? "";
        if (!sample.StartsWith("weapons/knife_") || i == 0 || Eng.GetPDataInt(activeItem, 43) != EngineConst.CswKnife)
            return false;

        int attack = WeaponStore.Player(id).UserKnifeAttack;
        WeaponStore.Player(id).UserKnifeAttack = 0;
        bool isSlash = sample.Length > 16 && sample[14] == 's' && sample[15] == 'l' && sample[16] == 'a';
        bool isHit = sample.Length > 16 && sample[14] == 'h' && sample[15] == 'i' && sample[16] == 't';
        bool isStab = sample.Length > 16 && sample[14] == 's' && sample[15] == 't' && sample[16] == 'a';
        bool isDeploy = sample.Length > 16 && sample[14] == 'd' && sample[15] == 'e' && sample[16] == 'p';

        if (isSlash)
        {
            Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
            if (attack == 1) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
            else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabMiss]);
        }
        if (isHit)
        {
            bool hitWall = sample.Length > 17 && sample[17] == 'w';
            if (hitWall)
            {
                Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                if (attack == 1) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
                else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab]);
            }
            else
            {
                if (Eng.RandomNum(0, 1) != 0)
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound5, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[3], 0, EngineConst.PitchNorm);
                else
                    Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound6, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[4], 0, EngineConst.PitchNorm);
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
            }
        }
        if (isStab)
        {
            Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[2], 0, EngineConst.PitchNorm);
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab]);
        }
        if (isDeploy)
        {
            Eng.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
        }
        return isSlash || isHit || isStab || isDeploy;
    }
}
