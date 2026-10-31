using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class Hooks
{
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
        int id = Eng.IndexOf(pentToSkip);
        if (!Eng.IsConnected(id)) return false;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return false;

        int i = WeaponStore.GetWeaponType(activeItem);

        Span<float> start = stackalloc float[3] { vectorStart[0], vectorStart[1], vectorStart[2] };
        Span<float> end = stackalloc float[3] { vectorEnd[0], vectorEnd[1], vectorEnd[2] };

        if (WeaponStore.IsInPrimaryAttack)
        {
            Span<float> dir = stackalloc float[3];
            VectorMath.VecSub(end, start, dir);
            dir[0] /= 8192.0f; dir[1] /= 8192.0f; dir[2] /= 8192.0f;
            Span<float> forward = stackalloc float[3] { Eng.GP->v_forward.x, Eng.GP->v_forward.y, Eng.GP->v_forward.z };
            VectorMath.VecSub(dir, forward, dir);
            if ((Eng.GetPDataInt(activeItem, 74) & ((1 << 2) | (1 << 0))) != 0)
                VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).SilencedAccuracy, dir);
            else if (PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open)
                VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).AimAccuracy, dir);
            else
                VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).Accuracy, dir);
            VectorMath.VecAdd(dir, forward, dir);
            VectorMath.VecMulScalar(dir, 8192.0f, dir);
            VectorMath.VecAdd(dir, start, end);
            Eng.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
            return true;
        }

        if (Eng.GetPDataInt(activeItem, 43) != EngineConst.CswKnife || i == 0) return false;

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
            Eng.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else if ((pPlayer->v.button & EngineConst.InAttack2) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            VectorMath.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).KnifeStabRange, dir);
            VectorMath.VecAdd(start, dir, end);
            Eng.TraceLine(start, end, ignoredMonster, pentToSkip, ptr);
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
        int id = Eng.IndexOf(pentToSkip);
        if (!Eng.IsConnected(id)) return false;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return false;

        int i = WeaponStore.GetWeaponType(activeItem);
        if (Eng.GetPDataInt(activeItem, 43) != EngineConst.CswKnife || i == 0) return false;

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
            Eng.TraceHull(start, end, ignoredMonster, hull, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else if ((pPlayer->v.button & EngineConst.InAttack2) != 0)
        {
            Span<float> angles = stackalloc float[3] { pPlayer->v.v_angle.x, pPlayer->v.v_angle.y, pPlayer->v.v_angle.z };
            Span<float> dir = stackalloc float[3];
            VectorMath.AngleVector(angles, EngineConst.AngleVectorForward, dir);
            VectorMath.VecMulScalar(dir, WeaponStore.Weapon(id, i).KnifeStabRange, dir);
            VectorMath.VecAdd(start, dir, end);
            Eng.TraceHull(start, end, ignoredMonster, hull, pentToSkip, ptr);
            vectorEnd[0] = end[0]; vectorEnd[1] = end[1]; vectorEnd[2] = end[2];
        }
        else return false;
        return true;
    }
}
