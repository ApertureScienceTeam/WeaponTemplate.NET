using GoldSrc.HLSDK.Native;

namespace Module;

// WeaponExtend 的模块内业务辅助:模板数据、回合重置与少量专用工具(引擎/数学封装统一走 Shared 的 Eng/VectorMath)。
public static unsafe partial class WeaponExtend
{
    // ---- 模板数据(原版 [33][MAXWEAPON] 数组) ----

    private static readonly int[,] WeaponAttackMode = new int[MaxPlayers, MaxWeapon];      // 攻击方式
    private static readonly int[,] WeaponDamageType = new int[MaxPlayers, MaxWeapon];      // 伤害类型
    private static readonly int[,] ProjectileExplodeSprite = new int[MaxPlayers, MaxWeapon];   // 爆炸SPR
    private static readonly int[,] ProjectileExplodeSound = new int[MaxPlayers, MaxWeapon];    // 爆炸声音
    private static readonly int[,] ProjectileExplodeEffect = new int[MaxPlayers, MaxWeapon];   // 爆炸特效
    private static readonly int[,] ProjectileHasTail = new int[MaxPlayers, MaxWeapon];         // 抛射物拖尾
    private static readonly int[,,] ProjectileTailColor = new int[MaxPlayers, MaxWeapon, 4];    // 抛射物拖尾颜色
    private static readonly int[,] ProjectileTailSprite = new int[MaxPlayers, MaxWeapon];      // 拖尾间隔SPR
    private static readonly int[,] ProjectileTraceMode = new int[MaxPlayers, MaxWeapon];       // 追踪模式
    private static readonly int[,] ProjectileGlow = new int[MaxPlayers, MaxWeapon];            // 抛射物发光
    private static readonly int[,,] ProjectileGlowColor = new int[MaxPlayers, MaxWeapon, 3];    // 抛射物发光颜色
    private static readonly int[,] ProjectileGlowRadius = new int[MaxPlayers, MaxWeapon];      // 抛射物发光半径
    private static readonly float[,] WeaponDamage = new float[MaxPlayers, MaxWeapon];          // 武器伤害值
    private static readonly float[,] ProjectileFramerate = new float[MaxPlayers, MaxWeapon];   // 抛射物播放速率
    private static readonly float[,] ProjectileScale = new float[MaxPlayers, MaxWeapon];       // 抛射物缩放
    private static readonly float[,] ProjectileBrightness = new float[MaxPlayers, MaxWeapon];  // 抛射物亮度
    private static readonly float[,] ProjectileGravity = new float[MaxPlayers, MaxWeapon];     // 抛射物重力
    private static readonly float[,] ProjectileFlySpeed = new float[MaxPlayers, MaxWeapon];    // 飞行速度
    private static readonly float[,] ProjectileHeightOfs = new float[MaxPlayers, MaxWeapon];   // 飞行高度修正
    private static readonly float[,] ProjectileExpFramerate = new float[MaxPlayers, MaxWeapon];// 爆炸SPR播放速率
    private static readonly float[,] ProjectileExpScale = new float[MaxPlayers, MaxWeapon];    // 爆炸SPR缩放
    private static readonly float[,] ProjectileExpHeightOfs = new float[MaxPlayers, MaxWeapon];// 爆炸SPR高度修正
    private static readonly float[,] ProjectileDamageRadius = new float[MaxPlayers, MaxWeapon];// 伤害半径
    private static readonly float[,] ProjectileDuration = new float[MaxPlayers, MaxWeapon];    // 存在时间
    private static readonly float[,] ProjectileDelay = new float[MaxPlayers, MaxWeapon];       // 延迟时间
    private static readonly float[,] ProjectileTailTime = new float[MaxPlayers, MaxWeapon];    // 拖尾间隔时间
    private static readonly float[,] ProjectileTailFramerate = new float[MaxPlayers, MaxWeapon];
    private static readonly float[,] ProjectileTailScale = new float[MaxPlayers, MaxWeapon];
    private static readonly float[,] ProjectileTailHeightOfs = new float[MaxPlayers, MaxWeapon];
    private static readonly float[,] ProjectileTraceRadius = new float[MaxPlayers, MaxWeapon]; // 追踪检测距离
    private static readonly float[,] ExtraSpriteFramerate = new float[MaxPlayers, MaxWeapon];  // 抛射物附带SPR播放速率
    private static readonly float[,] ExtraSpriteScale = new float[MaxPlayers, MaxWeapon];      // 抛射物附带SPR缩放
    private static readonly string[,] ProjectileModelPath = new string[MaxPlayers, MaxWeapon]; // 抛射物模型
    private static readonly string[,] ProjectileExplodeSoundPath = new string[MaxPlayers, MaxWeapon];
    private static readonly string[,] ProjectileExtraSprite = new string[MaxPlayers, MaxWeapon];// 抛射物附带SPR

    private static int InfoTarget;     // g_iInfoTarget(AllocString 句柄)
    private static int EnvSprite;      // g_iEnvSprite
    private static int SprBeamFollow;  // g_sprBeamFollow

    static WeaponExtend() => ResetAll();

    public static void ResetAll()
    {
        EngineResources.Reset();
        foreach (var field in typeof(WeaponExtend).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
        {
            if (field.GetValue(null) is string[,] strings)
            {
                for (int row = 0; row < strings.GetLength(0); row++)
                    for (int col = 0; col < strings.GetLength(1); col++) strings[row, col] = "";
            }
            else if (field.GetValue(null) is Array array) Array.Clear(array);
        }
        InfoTarget = EnvSprite = SprBeamFollow = 0;
    }

    // ---- 工具函数(模块业务语义,底层走 Eng/VectorMath) ----

    private static int GetPlayerTeam(int id) => Engine.GetPDataInt(id, 114); // m_iTeam

    // stock CheckSuffix
    private static bool CheckSuffix(string fileName, string suffix)
    {
        int dot = fileName.LastIndexOf('.');
        if (dot < 0) return false;
        return string.Equals(fileName[(dot + 1)..], suffix, StringComparison.Ordinal);
    }

    // stock EntityTurn:实体朝向目标点
    private static void EntityTurn(edict_t* entity, Span<float> target)
    {
        var pev = &entity->v;
        float dx = target[0] - pev->origin.x;
        float dy = target[1] - pev->origin.y;
        float dz = target[2] - pev->origin.z;
        float length = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        Span<float> direction = stackalloc float[3] { dx / length, dy / length, dz / length };
        Span<float> angles = stackalloc float[3];
        Engine.VecToAngles(direction, angles);
        if (angles[1] > 180.0f) angles[1] -= 360.0f;
        if (angles[1] < -180.0f) angles[1] += 360.0f;
        if (angles[1] == 180.0f || angles[1] == -180.0f) angles[1] = -179.999999f;
        pev->angles.x = angles[0];
        pev->angles.y = angles[1];
        pev->angles.z = angles[2];
    }

    // stock is_wall_between_points
    private static bool IsWallBetweenPoints(Span<float> start, Span<float> end, edict_t* ignoreEntity)
    {
        TraceResult trace = default;
        Engine.TraceLine(start, end, 1 /* IGNORE_MONSTERS */, ignoreEntity, &trace);
        float dx = end[0] - trace.vecEndPos.x;
        float dy = end[1] - trace.vecEndPos.y;
        float dz = end[2] - trace.vecEndPos.z;
        return (int)MathF.Sqrt(dx * dx + dy * dy + dz * dz) != 0;
    }
}
