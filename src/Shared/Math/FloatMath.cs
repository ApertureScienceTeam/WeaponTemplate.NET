namespace Module;

// 标量浮点舍入,复刻 AMXX floatround_round 的确切行为。
// 不能用 MathF.Round 替代:它是银行家舍入(midpoint to even),与 Pawn 生态的取整结果不同。
// 向量类的几何计算见 VectorMath;本类只负责标量。
internal static class FloatMath
{
    // AMXX floatround_round: floor((double)value + .5), including negative ties.
    // https://github.com/alliedmodders/amxmodx/blob/master/amxmodx/float.cpp#L198
    public static int Round(float value) => (int)Math.Floor((double)value + 0.5);
}
