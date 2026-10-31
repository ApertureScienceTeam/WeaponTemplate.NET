namespace Module;

// 单个玩家的运行时数据。只有真正进过服的玩家才会被创建(Store.Player),
// 按模板序号索引的数组只开到当前模板数,模板注册后再扩容。
public sealed class PlayerData
{
    private readonly WeaponConfig _config;
    private WeaponData?[]? _overrides; // 第一次 WPN_SetParam* 覆写模板时才分配

    public int Id { get; }

    public PlayerData(int id, WeaponConfig config)
    {
        Id = id;
        _config = config;
        DamagePoint = 1.0f;
        DamageVictimPoint = 1.0f;
        KnockBackPoint = 1.0f;
        EnsureTemplateCapacity(config.WeaponAmount);
    }

    // 模板注册后同步扩容,保证所有按模板序号索引的数组都覆盖到最新序号
    public void EnsureTemplateCapacity(int amount)
    {
        int size = amount + 1;
        if (BPAmmo.Length >= size) return;

        Array.Resize(ref BPAmmo, size);
        Array.Resize(ref TmpClip, size);
        Array.Resize(ref User1, size);
        Array.Resize(ref User2, size);
        Array.Resize(ref User3, size);
        Array.Resize(ref User4, size);
        Array.Resize(ref FUser1, size);
        Array.Resize(ref FUser2, size);
        Array.Resize(ref FUser3, size);
        Array.Resize(ref FUser4, size);

        int oldSize = SzUser1.Length;
        Array.Resize(ref SzUser1, size);
        Array.Resize(ref SzUser2, size);
        Array.Resize(ref SzUser3, size);
        Array.Resize(ref SzUser4, size);
        Array.Fill(SzUser1, "", oldSize, size - oldSize);
        Array.Fill(SzUser2, "", oldSize, size - oldSize);
        Array.Fill(SzUser3, "", oldSize, size - oldSize);
        Array.Fill(SzUser4, "", oldSize, size - oldSize);

        if (_overrides != null) Array.Resize(ref _overrides, size);
    }

    // 未被玩家覆写的模板直接读共享配置
    public WeaponData Weapon(int index)
    {
        var overrides = _overrides;
        if (overrides != null && index >= 0 && index < overrides.Length && overrides[index] != null)
            return overrides[index]!;
        return _config[index];
    }

    // WPN_SetParam* 写入时复制一份模板,之后只有该玩家看到改动
    public WeaponData WeaponForWrite(int index)
    {
        var overrides = _overrides;
        if (overrides == null || index >= overrides.Length)
        {
            int size = Math.Max(_config.WeaponAmount + 1, index + 1);
            var grown = new WeaponData?[size];
            if (overrides != null) Array.Copy(overrides, grown, overrides.Length);
            _overrides = overrides = grown;
        }
        var data = overrides[index];
        if (data != null) return data;
        data = new WeaponData();
        data.CopyFrom(_config[index]);
        overrides[index] = data;
        return data;
    }

    public IEnumerable<WeaponData> Overrides
    {
        get
        {
            if (_overrides == null) yield break;
            foreach (var data in _overrides)
                if (data != null) yield return data;
        }
    }

    // ---- 每个玩家的弹药/自定义槽位(按模板序号索引) ----

    public int[] BPAmmo = Array.Empty<int>();      // g_iCurWeaponBPAmmo
    public int[] TmpClip = Array.Empty<int>();     // g_iTmpClip
    public int[] User1 = Array.Empty<int>();
    public int[] User2 = Array.Empty<int>();
    public int[] User3 = Array.Empty<int>();
    public int[] User4 = Array.Empty<int>();
    public float[] FUser1 = Array.Empty<float>();
    public float[] FUser2 = Array.Empty<float>();
    public float[] FUser3 = Array.Empty<float>();
    public float[] FUser4 = Array.Empty<float>();
    public string[] SzUser1 = Array.Empty<string>();
    public string[] SzUser2 = Array.Empty<string>();
    public string[] SzUser3 = Array.Empty<string>();
    public string[] SzUser4 = Array.Empty<string>();

    // ---- 按 CS 武器 id 索引 ----

    public readonly int[] GameBpAmmo = new int[32]; // g_iGameCurWeaponBPAmmo

    // ---- 玩家一维状态 ----

    public readonly float[] PushAngle = new float[3];
    public readonly float[] UserDeploy = new float[2];
    public int ClipAmmo;
    public int KnifeAttack;
    public int KnifeSlashMode;
    public float PlayerThink;
    public int UserGrenadeId;
    public int BlockBuy;
    public int SightMode;
    public bool Reloading;
    public float NextThink;
    public bool OldButton;
    public int BotMoney;
    public bool BotBuyWeapon;
    public float BotNextThink;
    public int AttackerEntity;
    public float KnifeTempDamage;
    public float DamagePoint;
    public float DamageVictimPoint;
    public float KnockBackPoint;
    public int AttackHitGroup;
    public int EliteShotNumber;
    public int UserKnifeAttack;
    public bool BuyingAmmo;

    public void ClearAmmo()
    {
        Array.Clear(BPAmmo);
        Array.Clear(GameBpAmmo);
    }
}
