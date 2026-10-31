namespace Module;

// 一个武器模板的全部字段。注册/INI 只维护 Store.Config 这一份;
// 玩家用 WPN_SetParam* 改模板时才复制出自己的副本(见 PlayerData)。
public sealed class WeaponData
{
    public int Id;
    public string List = "", Spr = "", PModel = "", VModel = "", WModel = "", SightModel = "", BuyCommand = "";
    public string Sound1 = "", Sound2 = "", Sound3 = "", Sound4 = "", Sound5 = "", Sound6 = "";

    public int ReloadMode, Sight, Clip, Ammo, Cost, AmmoCost, Team, WModelBody, GrenadeSprId;

    public float Damage, Damage2, Speed, AimSpeed, SilencedSpeed, Recoil, AimRecoil, SilencedRecoil,
        Accuracy, AimAccuracy, SilencedAccuracy, DrawTime, MaxSpeed, AimMaxSpeed, KnockBack,
        SightDistance, SightFrameRate, SightOpenTime, SightCloseTime,
        FireSoundVolume, SilenceFireSoundVolume;

    public readonly float[] ReloadTime = new float[3];
    public readonly float[] SilencedTime = new float[2];
    public readonly int[] Anim = new int[WpnConst.MaxAnimation];
    public readonly float[] GrenadeData = new float[3];
    public readonly int[] KnifeAnim = new int[WpnConst.KnifeMaxAnimation];
    public readonly float[] KnifeSoundVolume = new float[5];

    public float KnifeSlashResetTime, KnifeSlashTime, KnifeSlashRange, KnifeSlashAngle,
        KnifeSlashAngleOffset, KnifeSlashHeight, KnifeSlashHeightOffset;
    public float KnifeStabResetTime, KnifeStabTime, KnifeStabRange, KnifeStabAngle,
        KnifeStabAngleOffset, KnifeStabHeight, KnifeStabHeightOffset;

    public void CopyFrom(WeaponData src)
    {
        Id = src.Id;
        List = src.List; Spr = src.Spr; PModel = src.PModel; VModel = src.VModel; WModel = src.WModel;
        SightModel = src.SightModel; BuyCommand = src.BuyCommand;
        Sound1 = src.Sound1; Sound2 = src.Sound2; Sound3 = src.Sound3;
        Sound4 = src.Sound4; Sound5 = src.Sound5; Sound6 = src.Sound6;
        ReloadMode = src.ReloadMode; Sight = src.Sight; Clip = src.Clip; Ammo = src.Ammo;
        Cost = src.Cost; AmmoCost = src.AmmoCost; Team = src.Team;
        WModelBody = src.WModelBody; GrenadeSprId = src.GrenadeSprId;
        Damage = src.Damage; Damage2 = src.Damage2; Speed = src.Speed; AimSpeed = src.AimSpeed;
        SilencedSpeed = src.SilencedSpeed; Recoil = src.Recoil; AimRecoil = src.AimRecoil;
        SilencedRecoil = src.SilencedRecoil; Accuracy = src.Accuracy; AimAccuracy = src.AimAccuracy;
        SilencedAccuracy = src.SilencedAccuracy; DrawTime = src.DrawTime; MaxSpeed = src.MaxSpeed;
        AimMaxSpeed = src.AimMaxSpeed; KnockBack = src.KnockBack;
        SightDistance = src.SightDistance; SightFrameRate = src.SightFrameRate;
        SightOpenTime = src.SightOpenTime; SightCloseTime = src.SightCloseTime;
        FireSoundVolume = src.FireSoundVolume; SilenceFireSoundVolume = src.SilenceFireSoundVolume;
        Array.Copy(src.ReloadTime, ReloadTime, ReloadTime.Length);
        Array.Copy(src.SilencedTime, SilencedTime, SilencedTime.Length);
        Array.Copy(src.Anim, Anim, Anim.Length);
        Array.Copy(src.GrenadeData, GrenadeData, GrenadeData.Length);
        Array.Copy(src.KnifeAnim, KnifeAnim, KnifeAnim.Length);
        Array.Copy(src.KnifeSoundVolume, KnifeSoundVolume, KnifeSoundVolume.Length);
        KnifeSlashResetTime = src.KnifeSlashResetTime; KnifeSlashTime = src.KnifeSlashTime;
        KnifeSlashRange = src.KnifeSlashRange; KnifeSlashAngle = src.KnifeSlashAngle;
        KnifeSlashAngleOffset = src.KnifeSlashAngleOffset; KnifeSlashHeight = src.KnifeSlashHeight;
        KnifeSlashHeightOffset = src.KnifeSlashHeightOffset;
        KnifeStabResetTime = src.KnifeStabResetTime; KnifeStabTime = src.KnifeStabTime;
        KnifeStabRange = src.KnifeStabRange; KnifeStabAngle = src.KnifeStabAngle;
        KnifeStabAngleOffset = src.KnifeStabAngleOffset; KnifeStabHeight = src.KnifeStabHeight;
        KnifeStabHeightOffset = src.KnifeStabHeightOffset;
    }

    // plugin_init 的默认音量:0 视为未配置
    public void ApplyDefaultSoundVolumes()
    {
        if (FireSoundVolume == 0.0f) FireSoundVolume = Id == EngineConst.CswHegrenade ? 0.25f : 0.35f;
        if (SilenceFireSoundVolume == 0.0f) SilenceFireSoundVolume = EngineConst.AttnNorm;
        for (int i = 0; i < KnifeSoundVolume.Length; i++)
            if (KnifeSoundVolume[i] == 0.0f) KnifeSoundVolume[i] = EngineConst.AttnNorm;
    }
}

// 全部模板的配置(每张地图重新注册)。模板按实际注册数量存储,不预留固定上限。
public sealed class WeaponConfig
{
    // 未注册序号的读取统一落到这个空模板,避免为越界访问分配对象
    private static readonly WeaponData Empty = new();

    private readonly List<WeaponData> _templates = new() { new WeaponData() }; // 0 号占位,模板序号从 1 开始
    private readonly Dictionary<int, int>[] _serialNumbers = { new(), new(), new() }; // [slot] INI 序号 -> 模板序号

    public int WeaponAmount => _templates.Count - 1;

    public bool CanRegister => WeaponAmount < WpnConst.MaxTemplateCount;

    public bool IsTemplate(int index) => index > 0 && index < _templates.Count;

    public WeaponData this[int index] => IsTemplate(index) ? _templates[index] : Empty;

    public bool TryCreate(out int index)
    {
        index = 0;
        if (!CanRegister) return false;
        index = _templates.Count;
        _templates.Add(new WeaponData());
        return true;
    }

    public bool HasSerial(int slot, int serial) => _serialNumbers[slot].ContainsKey(serial);

    public void SetSerial(int slot, int serial, int index) => _serialNumbers[slot][serial] = index;

    public int GetTemplateBySerial(int slot, int serial) =>
        _serialNumbers[slot].TryGetValue(serial, out int index) ? index : 0;

    public void Reset()
    {
        _templates.Clear();
        _templates.Add(new WeaponData());
        foreach (var serialNumbers in _serialNumbers) serialNumbers.Clear();
    }
}
