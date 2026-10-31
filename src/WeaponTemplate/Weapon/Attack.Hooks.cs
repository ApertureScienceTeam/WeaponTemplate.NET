using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// 武器开火的引擎拦截:pfnTraceLine/pfnTraceHull(弹道散布与刀轨迹)、pfnPlaybackEvent(命中率修正)、
// pfnUpdateClientData(抑制客户端开枪预测)、pfnEmitSound(刀子音效替换)——与 Attack.Ham.cs 同属开火域。
public static unsafe partial class Attack
{
    // pfnPlaybackEvent 事件的 host-only 标志位
    private const int FevHostOnly = 1 << 1;

    // ---- fm_UpdateClientData_Post(阻止客户端预测开枪) ----
    // 注:本 SDK 的 DLL_FUNCTIONS.pfnUpdateClientData 声明参数顺序为 (edict_t*, int, clientdata_t*)。

    [AmxxHook("pfnUpdateClientData", Post = true)]
    public static void UpdateClientDataPost(edict_t* pEntity, int sendWeapons, clientdata_t* cd)
    {
        HookResult.Ignore();
        int id = Engine.IndexOf(pEntity);
        if (!Engine.IsAlive(id)) return;

        var pPlayer = Engine.EntOfIndex(id);
        var entity = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        if (!Engine.IsValid(entity)) return;

        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return;

        // 用于阻止客户端的开枪
        cd->m_flNextAttack = Engine.GameTime + 0.001f;
    }

    // ---- fm_PlaybackEvent(EngFunc_PlaybackEvent hook,命中率修正) ----

    [AmxxHook("pfnPlaybackEvent", Table = HookTable.Engine)]
    public static void PlaybackEvent(int flags, edict_t* pInvoker, ushort eventIndex, float delay, float* origin, float* angles,
        float fParam1, float fParam2, int iParam1, int iParam2, int bParam1, int bParam2)
    {
        HookResult.Ignore();
        int id = Engine.IndexOf(pInvoker);
        if (!Engine.IsConnected(id) || !WeaponStore.IsInPrimaryAttack) { return; }

        var pPlayer = Engine.EntOfIndex(id);
        var entity = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        int i = Engine.IsValid(entity) ? WeaponStore.GetWeaponType(entity) : 0;
        if (i == 0 || WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) { return; }

        float recoil;
        if (PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open) recoil = WeaponStore.Weapon(id, i).AimAccuracy;
        else recoil = WeaponStore.Weapon(id, i).Accuracy;
        Engine.PlaybackEvent(flags | FevHostOnly, pInvoker, eventIndex, delay, origin, angles, fParam1 * recoil, fParam2 * recoil, iParam1, iParam2, bParam1, bParam2);
        HookResult.Supercede();
    }

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
        int id = Engine.IndexOf(entity);
        if (!Engine.IsConnected(id)) return false;

        var pPlayer = Engine.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        int i = Engine.IsValid(activeItem) ? WeaponStore.GetWeaponType(activeItem) : 0;

        string sample = Marshal.PtrToStringAnsi((nint)samplePtr) ?? "";
        if (!sample.StartsWith("weapons/knife_") || i == 0 || Engine.GetPDataInt(activeItem, CsOffsets.ItemId) != EngineConst.CswKnife)
            return false;

        int attack = WeaponStore.Player(id).UserKnifeAttack;
        WeaponStore.Player(id).UserKnifeAttack = 0;
        bool isSlash = sample.Length > 16 && sample[14] == 's' && sample[15] == 'l' && sample[16] == 'a';
        bool isHit = sample.Length > 16 && sample[14] == 'h' && sample[15] == 'i' && sample[16] == 't';
        bool isStab = sample.Length > 16 && sample[14] == 's' && sample[15] == 't' && sample[16] == 'a';
        bool isDeploy = sample.Length > 16 && sample[14] == 'd' && sample[15] == 'e' && sample[16] == 'p';

        if (isSlash)
        {
            Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
            if (attack == 1) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
            else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabMiss]);
        }
        if (isHit)
        {
            bool hitWall = sample.Length > 17 && sample[17] == 'w';
            if (hitWall)
            {
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                if (attack == 1) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
                else PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab]);
            }
            else
            {
                if (Engine.RandomNum(0, 1) != 0)
                    Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound5, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[3], 0, EngineConst.PitchNorm);
                else
                    Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound6, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[4], 0, EngineConst.PitchNorm);
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
            }
        }
        if (isStab)
        {
            Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[2], 0, EngineConst.PitchNorm);
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab]);
        }
        if (isDeploy)
        {
            Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
        }
        return isSlash || isHit || isStab || isDeploy;
    }

    // ---- fm_TraceLine(散布 / 刀子轨迹) ----

    [AmxxHook("pfnTraceLine", Table = HookTable.Engine)]
    public static void TraceLine(float* vectorStart, float* vectorEnd, int ignoredMonster, edict_t* pentToSkip, TraceResult* ptr)
    {
        HookResult.Ignore();
        if (TraceLineLogic(vectorStart, vectorEnd, ignoredMonster, pentToSkip, ptr))
            HookResult.Supercede();
    }

    private static bool TraceLineLogic(float* vectorStart, float* vectorEnd, int ignoredMonster, edict_t* pentToSkip, TraceResult* ptr)
    {
        int id = Engine.IndexOf(pentToSkip);
        if (!Engine.IsConnected(id)) return false;

        var pPlayer = Engine.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        if (!Engine.IsValid(activeItem)) return false;

        int i = WeaponStore.GetWeaponType(activeItem);

        Span<float> start = stackalloc float[3] { vectorStart[0], vectorStart[1], vectorStart[2] };
        Span<float> end = stackalloc float[3] { vectorEnd[0], vectorEnd[1], vectorEnd[2] };

        if (WeaponStore.IsInPrimaryAttack)
        {
            Span<float> dir = stackalloc float[3];
            VectorMath.VecSub(end, start, dir);
            dir[0] /= 8192.0f; dir[1] /= 8192.0f; dir[2] /= 8192.0f;
            Span<float> forward = stackalloc float[3] { Engine.GP->v_forward.x, Engine.GP->v_forward.y, Engine.GP->v_forward.z };
            VectorMath.VecSub(dir, forward, dir);
            if ((Engine.GetPDataInt(activeItem, CsOffsets.ItemWeaponState) & ((1 << 2) | (1 << 0))) != 0)
                VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).SilencedAccuracy, dir);
            else if (PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open)
                VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).AimAccuracy, dir);
            else
                VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).Accuracy, dir);
            VectorMath.VecAdd(dir, forward, dir);
            VectorMath.VecMulScalar(dir, 8192.0f, dir);
            VectorMath.VecAdd(dir, start, end);
            Engine.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
            return true;
        }

        if (Engine.GetPDataInt(activeItem, CsOffsets.ItemId) != EngineConst.CswKnife || i == 0) return false;

        if ((WeaponStore.Weapon(id, i).KnifeSlashTime != 0.0f && WeaponStore.Weapon(id, i).KnifeSlashAngle != 0.0f) ||
            (WeaponStore.Weapon(id, i).KnifeStabTime != 0.0f && WeaponStore.Weapon(id, i).KnifeSlashAngle != 0.0f))
            return false;

        if ((pPlayer->v.button & EngineConst.InAttack) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            VectorMath.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).KnifeSlashRange, dir);
            VectorMath.VecAdd(start, dir, end);
            Engine.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else if ((pPlayer->v.button & EngineConst.InAttack2) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            VectorMath.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).KnifeStabRange, dir);
            VectorMath.VecAdd(start, dir, end);
            Engine.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else return false;
        return true;
    }

    // ---- fm_TraceHull(刀子轨迹) ----

    [AmxxHook("pfnTraceHull", Table = HookTable.Engine)]
    public static void TraceHull(float* vectorStart, float* vectorEnd, int ignoredMonster, int hull, edict_t* pentToSkip, TraceResult* ptr)
    {
        HookResult.Ignore();
        if (TraceHullLogic(vectorStart, vectorEnd, ignoredMonster, hull, pentToSkip, ptr))
            HookResult.Supercede();
    }

    private static bool TraceHullLogic(float* vectorStart, float* vectorEnd, int ignoredMonster, int hull, edict_t* pentToSkip, TraceResult* ptr)
    {
        int id = Engine.IndexOf(pentToSkip);
        if (!Engine.IsConnected(id)) return false;

        var pPlayer = Engine.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        if (!Engine.IsValid(activeItem)) return false;

        int i = WeaponStore.GetWeaponType(activeItem);
        if (Engine.GetPDataInt(activeItem, CsOffsets.ItemId) != EngineConst.CswKnife || i == 0) return false;

        if ((WeaponStore.Weapon(id, i).KnifeSlashTime != 0.0f && WeaponStore.Weapon(id, i).KnifeSlashAngle != 0.0f) ||
            (WeaponStore.Weapon(id, i).KnifeStabTime != 0.0f && WeaponStore.Weapon(id, i).KnifeSlashAngle != 0.0f))
            return false;

        Span<float> start = stackalloc float[3] { vectorStart[0], vectorStart[1], vectorStart[2] };
        Span<float> end = stackalloc float[3] { vectorEnd[0], vectorEnd[1], vectorEnd[2] };

        if ((pPlayer->v.button & EngineConst.InAttack) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            VectorMath.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).KnifeSlashRange, dir);
            VectorMath.VecAdd(start, dir, end);
            Engine.TraceHull(start, end, ignoredMonster, hull, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else if ((pPlayer->v.button & EngineConst.InAttack2) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            VectorMath.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).KnifeStabRange, dir);
            VectorMath.VecAdd(start, dir, end);
            Engine.TraceHull(start, end, ignoredMonster, hull, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else return false;
        return true;
    }
}
