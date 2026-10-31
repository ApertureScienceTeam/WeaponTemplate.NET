using GoldSrc.HLSDK.Native;

namespace Module;

// WeaponExtend 的模块内业务辅助:模板数据、回合重置与少量专用工具(引擎/数学封装统一走 Shared 的 Eng/VectorMath)。
public static unsafe partial class WeaponExtend
{
    // ---- 模板数据(原版 [33][MAXWEAPON] 数组) ----
    // 写入只发生在注册期且每个玩家行都是同一个值,玩家维度从未分化,因此收敛为按模板序号
    // 索引的动态表:容量跟随核心注册数,不做上限;越界读取返回默认值(等价原版数组的零值位)。

    private interface ITemplateTable { void Reset(); }

    private sealed class TemplateTable<T> : ITemplateTable
    {
        private readonly List<T> _rows;
        private readonly T _zero;

        public TemplateTable(T zero)
        {
            _zero = zero;
            _rows = new List<T> { zero }; // 0 号占位
        }

        public T this[int index] => index >= 0 && index < _rows.Count ? _rows[index] : _zero;

        public void Set(int index, T value)
        {
            while (_rows.Count <= index) _rows.Add(_zero);
            _rows[index] = value;
        }

        public void Reset() => _rows.RemoveRange(1, _rows.Count - 1);
    }

    private static readonly TemplateTable<int> WeaponAttackMode = new(0);        // 攻击方式
    private static readonly TemplateTable<int> WeaponDamageType = new(0);        // 伤害类型
    private static readonly TemplateTable<int> ProjectileExplodeSprite = new(0); // 爆炸SPR
    private static readonly TemplateTable<int> ProjectileExplodeSound = new(0);  // 爆炸声音
    private static readonly TemplateTable<int> ProjectileExplodeEffect = new(0); // 爆炸特效
    private static readonly TemplateTable<int> ProjectileHasTail = new(0);       // 抛射物拖尾
    private static readonly TemplateTable<int> TailColorR = new(0);              // 抛射物拖尾颜色R
    private static readonly TemplateTable<int> TailColorG = new(0);              // 抛射物拖尾颜色G
    private static readonly TemplateTable<int> TailColorB = new(0);              // 抛射物拖尾颜色B
    private static readonly TemplateTable<int> TailColorAlpha = new(0);          // 抛射物拖尾透明度
    private static readonly TemplateTable<int> ProjectileTailSprite = new(0);    // 拖尾间隔SPR
    private static readonly TemplateTable<int> ProjectileTraceMode = new(0);     // 追踪模式
    private static readonly TemplateTable<int> ProjectileGlow = new(0);          // 抛射物发光
    private static readonly TemplateTable<int> GlowColorR = new(0);              // 抛射物发光颜色R
    private static readonly TemplateTable<int> GlowColorG = new(0);              // 抛射物发光颜色G
    private static readonly TemplateTable<int> GlowColorB = new(0);              // 抛射物发光颜色B
    private static readonly TemplateTable<int> ProjectileGlowRadius = new(0);    // 抛射物发光半径
    private static readonly TemplateTable<float> WeaponDamage = new(0f);         // 武器伤害值
    private static readonly TemplateTable<float> ProjectileFramerate = new(0f);  // 抛射物播放速率
    private static readonly TemplateTable<float> ProjectileScale = new(0f);      // 抛射物缩放
    private static readonly TemplateTable<float> ProjectileBrightness = new(0f); // 抛射物亮度
    private static readonly TemplateTable<float> ProjectileGravity = new(0f);    // 抛射物重力
    private static readonly TemplateTable<float> ProjectileFlySpeed = new(0f);   // 飞行速度
    private static readonly TemplateTable<float> ProjectileHeightOfs = new(0f);  // 飞行高度修正
    private static readonly TemplateTable<float> ProjectileExpFramerate = new(0f); // 爆炸SPR播放速率
    private static readonly TemplateTable<float> ProjectileExpScale = new(0f);   // 爆炸SPR缩放
    private static readonly TemplateTable<float> ProjectileExpHeightOfs = new(0f); // 爆炸SPR高度修正
    private static readonly TemplateTable<float> ProjectileDamageRadius = new(0f); // 伤害半径
    private static readonly TemplateTable<float> ProjectileDuration = new(0f);   // 存在时间
    private static readonly TemplateTable<float> ProjectileDelay = new(0f);      // 延迟时间
    private static readonly TemplateTable<float> ProjectileTailTime = new(0f);   // 拖尾间隔时间
    private static readonly TemplateTable<float> ProjectileTailFramerate = new(0f);
    private static readonly TemplateTable<float> ProjectileTailScale = new(0f);
    private static readonly TemplateTable<float> ProjectileTailHeightOfs = new(0f);
    private static readonly TemplateTable<float> ProjectileTraceRadius = new(0f); // 追踪检测距离
    private static readonly TemplateTable<float> ExtraSpriteFramerate = new(0f); // 抛射物附带SPR播放速率
    private static readonly TemplateTable<float> ExtraSpriteScale = new(0f);     // 抛射物附带SPR缩放
    private static readonly TemplateTable<string> ProjectileModelPath = new(""); // 抛射物模型
    private static readonly TemplateTable<string> ProjectileExplodeSoundPath = new("");
    private static readonly TemplateTable<string> ProjectileExtraSprite = new(""); // 抛射物附带SPR

    private static int InfoTarget;     // g_iInfoTarget(AllocString 句柄)
    private static int EnvSprite;      // g_iEnvSprite
    private static int SprBeamFollow;  // g_sprBeamFollow

    static WeaponExtend() => ResetAll();

    public static void ResetAll()
    {
        EngineResources.Reset();
        foreach (var field in typeof(WeaponExtend).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
            if (field.GetValue(null) is ITemplateTable table) table.Reset();
        InfoTarget = EnvSprite = SprBeamFollow = 0;
    }

    // ---- 工具函数(模块业务语义,底层走 Eng/VectorMath) ----

    private static int GetPlayerTeam(int id) => Engine.GetPDataInt(id, CsOffsets.Team); // m_iTeam

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
