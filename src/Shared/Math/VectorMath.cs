using GoldSrc.HLSDK.Native;

namespace Module;

// 向量/角度/弹道起点的纯计算工具(替代 Pawn xs 库)。
public static unsafe class VectorMath
{
    // ---- 向量小工具(替代 xs 库) ----

    public static void VecAdd(Span<float> a, Span<float> b, Span<float> dst)
    {
        dst[0] = a[0] + b[0]; dst[1] = a[1] + b[1]; dst[2] = a[2] + b[2];
    }

    public static void VecSub(Span<float> a, Span<float> b, Span<float> dst)
    {
        dst[0] = a[0] - b[0]; dst[1] = a[1] - b[1]; dst[2] = a[2] - b[2];
    }

    public static void VecMulScalar(Span<float> a, float scalar, Span<float> dst)
    {
        dst[0] = a[0] * scalar; dst[1] = a[1] * scalar; dst[2] = a[2] * scalar;
    }

    public static float Distance(Span<float> a, Span<float> b)
    {
        float dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // HLSDK AngleVectors(对应 fakemeta angle_vector,mode: 1=forward 2=right 3=up)
    public static void AngleVector(Span<float> angles, int mode, Span<float> dest)
    {
        const float Deg2Rad = MathF.PI / 180.0f;
        float sp = MathF.Sin(angles[0] * Deg2Rad), cp = MathF.Cos(angles[0] * Deg2Rad);
        float sy = MathF.Sin(angles[1] * Deg2Rad), cy = MathF.Cos(angles[1] * Deg2Rad);
        float sr = MathF.Sin(angles[2] * Deg2Rad), cr = MathF.Cos(angles[2] * Deg2Rad);
        switch (mode)
        {
            case EngineConst.AngleVectorForward:
                dest[0] = cp * cy; dest[1] = cp * sy; dest[2] = -sp;
                break;
            case EngineConst.AngleVectorRight:
                dest[0] = -sr * sp * cy + cr * sy;
                dest[1] = -sr * sp * sy - cr * cy;
                dest[2] = -sr * cp;
                break;
            case EngineConst.AngleVectorUp:
                dest[0] = cr * sp * cy + sy * sr;
                dest[1] = cr * sp * sy - cy * sr;
                dest[2] = cr * cp;
                break;
        }
    }

    // stock get_position(id, forw, right, up, vStart[])
    public static void GetPosition(int id, float forward, float right, float up, Span<float> start)
    {
        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return;
        var pev = pPlayer->v;

        Span<float> origin = stackalloc float[3] { pev.origin.x, pev.origin.y, pev.origin.z };
        Span<float> viewOfs = stackalloc float[3] { pev.view_ofsr[0], pev.view_ofsr[1], pev.view_ofsr[2] };
        VecAdd(origin, viewOfs, origin);

        Span<float> angles = stackalloc float[3] { pev.v_angle.x, pev.v_angle.y, pev.v_angle.z };
        Span<float> vForward = stackalloc float[3];
        Span<float> vRight = stackalloc float[3];
        Span<float> vUp = stackalloc float[3];
        AngleVector(angles, EngineConst.AngleVectorForward, vForward);
        AngleVector(angles, EngineConst.AngleVectorRight, vRight);
        AngleVector(angles, EngineConst.AngleVectorUp, vUp);

        start[0] = origin[0] + vForward[0] * forward + vRight[0] * right + vUp[0] * up;
        start[1] = origin[1] + vForward[1] * forward + vRight[1] * right + vUp[1] * up;
        start[2] = origin[2] + vForward[2] * forward + vRight[2] * right + vUp[2] * up;
    }

    // stock fm_get_aim_origin(id, origin[])
    public static void GetAimOrigin(int id, Span<float> origin)
    {
        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return;
        var pev = pPlayer->v;

        Span<float> start = stackalloc float[3] { pev.origin.x, pev.origin.y, pev.origin.z };
        Span<float> viewOfs = stackalloc float[3] { pev.view_ofsr[0], pev.view_ofsr[1], pev.view_ofsr[2] };
        VecAdd(start, viewOfs, start);

        Span<float> angles = stackalloc float[3] { pev.v_angle.x, pev.v_angle.y, pev.v_angle.z };
        Engine.MakeVectors(angles);
        Span<float> dest = stackalloc float[3] { Engine.GP->v_forward.x, Engine.GP->v_forward.y, Engine.GP->v_forward.z };
        VecMulScalar(dest, 9999.0f, dest);
        VecAdd(start, dest, dest);

        TraceResult trace = default;
        Engine.TraceLine(start, dest, 0, pPlayer, &trace);
        origin[0] = trace.vecEndPos.x;
        origin[1] = trace.vecEndPos.y;
        origin[2] = trace.vecEndPos.z;
    }

    // stock GetVelocityFromOrigin(origin1, origin2, speed, velocity)
    public static void GetVelocityFromOrigin(Span<float> origin1, Span<float> origin2, float speed, Span<float> velocity)
    {
        VecSub(origin1, origin2, velocity);
        float dx = origin1[0] - origin2[0], dy = origin1[1] - origin2[1], dz = origin1[2] - origin2[2];
        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        float valve = distance / speed;
        VecMulScalar(velocity, 1.0f / valve, velocity);
    }

    // stock get_speed_vector(WeaponExtend.sma 内同名的私有实现)
    public static void GetSpeedVector(Span<float> origin1, Span<float> origin2, float speed, Span<float> newVelocity)
    {
        newVelocity[0] = origin2[0] - origin1[0];
        newVelocity[1] = origin2[1] - origin1[1];
        newVelocity[2] = origin2[2] - origin1[2];
        float len = MathF.Sqrt(newVelocity[0] * newVelocity[0] + newVelocity[1] * newVelocity[1] + newVelocity[2] * newVelocity[2]);
        float num = MathF.Sqrt(speed * speed / (len * len));
        newVelocity[0] *= num;
        newVelocity[1] *= num;
        newVelocity[2] *= num;
    }
}
