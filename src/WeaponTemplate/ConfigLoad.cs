using System.Text;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Native;
using GoldSrc.HLSDK.Native;

namespace Module;

// plugin_precache + LoadWeapon/LoadKnife/LoadGrenade 的翻译:
// 每行 key=value,分号开头为注释;模型/声音在解析时预缓存;
// 每个键值对触发一次 WPN_LoadFiles 广播(在 preCache 阶段手动注册的 forward)。
public static unsafe class ConfigLoad
{
    // WPN_LoadFiles forward 句柄(原版 g_fwLoadFiles;因需在 precache 阶段触发,手动注册)
    public static int LoadFilesForward;

    public static void OnPrecache()
    {
        Store.BloodSpray = Eng.PrecacheModel("sprites/bloodspray.spr");
        Store.BloodDrop = Eng.PrecacheModel("sprites/blood.spr");
        Store.SmokeId = Eng.PrecacheModel("sprites/smokepuff.spr");
        Store.DisableBuyTime = Eng.GameTime + Eng.CvarFloat("mp_buytime") * 60.0f;

        LoadFilesForward = MF.RegisterForward("WPN_LoadFiles", ForwardExecType.ET_CONTINUE,
            ForwardParam.FP_CELL, ForwardParam.FP_CELL, ForwardParam.FP_STRING, ForwardParam.FP_STRING);

        var config = MF.GetLocalInfo("amxx_configsdir", "addons/amxmodx/configs") ?? "addons/amxmodx/configs";
        var file = Path.Combine(config, "newweapon.ini");
        if (File.Exists(file)) LoadWeapon(file);
        file = Path.Combine(config, "newknife.ini");
        if (File.Exists(file)) LoadKnife(file);
        file = Path.Combine(config, "newgrenade.ini");
        if (File.Exists(file)) LoadGrenade(file);
    }

    // ---- INI 读取 ----

    // CS 服务器配置常见 GBK/ANSI 编码;优先按系统默认 ANSI(936)读取,失败回退 UTF-8。
    private static Encoding DetectEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(936);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

    private static List<(string Key, string Value)> ReadIni(string path)
    {
        var result = new List<(string, string)>();
        string[] lines;
        var encoding = DetectEncoding();
        try
        {
            lines = File.ReadAllLines(path, encoding);
        }
        catch
        {
            lines = File.ReadAllLines(path, Encoding.UTF8);
        }

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith(';'))
                continue;

            int split = line.IndexOf('=');
            if (split < 0)
                continue;

            var key = line[..split].Trim();
            var value = line[(split + 1)..].Trim();
            result.Add((key, value));
        }
        return result;
    }

    private static int ToInt(string value) => int.TryParse(value, out var v) ? v : 0;
    private static float ToFloat(string value) => float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;

    // SoundVolume 的解析规则:val > 0 时取 max(1-val, 0),否则用默认值
    private static float ToAttenuation(string value, float fallback)
    {
        float val = ToFloat(value);
        if (val <= 0.0f) return fallback;
        return (1.0f - val) > 0.0f ? 1.0f - val : 0.0f;
    }

    private static void NotifyLoadFiles(int slot, int weaponIndex, string key, string value)
        => MF.ExecuteForward(LoadFilesForward, slot, weaponIndex, key, value);

    private static int StrToCsw(string name) => name switch
    {
        "CSW_AWP" => EngineConst.CswAwp,
        "CSW_SCOUT" => EngineConst.CswScout,
        "CSW_SG550" => EngineConst.CswSg550,
        "CSW_G3SG1" => EngineConst.CswG3sg1,
        "CSW_AK47" => EngineConst.CswAk47,
        "CSW_M4A1" => EngineConst.CswM4a1,
        "CSW_XM1014" => EngineConst.CswXm1014,
        "CSW_MAC10" => EngineConst.CswMac10,
        "CSW_AUG" => EngineConst.CswAug,
        "CSW_UMP45" => EngineConst.CswUmp45,
        "CSW_GALIL" => EngineConst.CswGalil,
        "CSW_FAMAS" => EngineConst.CswFamas,
        "CSW_MP5NAVY" => EngineConst.CswMp5Navy,
        "CSW_MP5" => EngineConst.CswMp5Navy,
        "CSW_M249" => EngineConst.CswM249,
        "CSW_M3" => EngineConst.CswM3,
        "CSW_TMP" => EngineConst.CswTmp,
        "CSW_SG552" => EngineConst.CswSg552,
        "CSW_P90" => EngineConst.CswP90,
        "CSW_DEAGLE" => EngineConst.CswDeagle,
        "CSW_USP" => EngineConst.CswUsp,
        "CSW_USP45" => EngineConst.CswUsp,
        "CSW_GLOCK18" => EngineConst.CswGlock18,
        "CSW_GLOCK" => EngineConst.CswGlock18,
        "CSW_ELITE" => EngineConst.CswElite,
        "CSW_FIVESEVEN" => EngineConst.CswFiveseven,
        "CSW_P228" => EngineConst.CswP228,
        _ => 0,
    };

    // ---- 主武器/副武器 ----

    private static void LoadWeapon(string path)
    {
        foreach (var (key, value) in ReadIni(path))
        {
            // 原版对"武器序号"行直接 continue,不触发 WPN_LoadFiles
            if (key == "武器序号")
            {
                Store.WeaponAmount++;
                Store.WeaponTemplateSerialNumber[WpnConst.SlotWeapon, ToInt(value)] = Store.WeaponAmount;
                continue;
            }

            if (key == "类型")
            {
                int csw = StrToCsw(value);
                for (int id = 1; id < 33; id++) Store.WeaponId[id, Store.WeaponAmount] = csw;
            }
            else if (key == "上弹模式")
            {
                int mode = ToInt(value);
                for (int id = 1; id < 33; id++) Store.ReloadMode[id, Store.WeaponAmount] = mode;
            }
            else if (key == "开启机瞄")
            {
                int sight = ToInt(value);
                for (int id = 1; id < 33; id++)
                {
                    int w = Store.WeaponId[id, Store.WeaponAmount];
                    if (w != EngineConst.CswG3sg1 && w != EngineConst.CswSg550 && w != EngineConst.CswAwp && w != EngineConst.CswScout)
                        Store.WeaponSight[id, Store.WeaponAmount] = sight;
                    else
                        Store.WeaponSight[id, Store.WeaponAmount] = 0;
                }
            }
            else if (key == "子弹数") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponClip[id, Store.WeaponAmount] = v; }
            else if (key == "备用子弹数") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAmmo[id, Store.WeaponAmount] = v; }
            else if (key == "伤害倍数") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponDamage[id, Store.WeaponAmount] = v; }
            else if (key == "武器射速") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponSpeed[id, Store.WeaponAmount] = v; }
            else if (key == "武器开镜射速") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponAimSpeed[id, Store.WeaponAmount] = v; }
            else if (key == "武器消音射速") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponSilencedSpeed[id, Store.WeaponAmount] = v; }
            else if (key == "武器后座") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponRecoil[id, Store.WeaponAmount] = v; }
            else if (key == "武器开镜后座") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponAimRecoil[id, Store.WeaponAmount] = v; }
            else if (key == "武器消音后座") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponSilencedRecoil[id, Store.WeaponAmount] = v; }
            else if (key == "武器准确度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponAccuracy[id, Store.WeaponAmount] = v; }
            else if (key == "武器开镜准确度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponAimAccuracy[id, Store.WeaponAmount] = v; }
            else if (key == "武器消音准确度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponSilencedAccuracy[id, Store.WeaponAmount] = v; }
            else if (key == "开启消音时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponSilencedTime[id, Store.WeaponAmount, 0] = v; }
            else if (key == "关闭消音时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponSilencedTime[id, Store.WeaponAmount, 1] = v; }
            else if (key == "镜头距离") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.SightDistance[id, Store.WeaponAmount] = v; }
            else if (key == "镜头变化幅度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.SightFrameRate[id, Store.WeaponAmount] = v; }
            else if (key == "开启速度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.SightOpenTime[id, Store.WeaponAmount] = v; }
            else if (key == "关闭速度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.SightCloseTime[id, Store.WeaponAmount] = v; }
            else if (key == "换弹时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponReloadTime[id, Store.WeaponAmount, 0] = v; }
            else if (key == "散弹开始换弹时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponReloadTime[id, Store.WeaponAmount, 1] = v; }
            else if (key == "散弹结束换弹时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponReloadTime[id, Store.WeaponAmount, 2] = v; }
            else if (key == "掏出时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponDrawTime[id, Store.WeaponAmount] = v; }
            else if (key == "行走速度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponMaxSpeed[id, Store.WeaponAmount] = v; }
            else if (key == "开镜行走速度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponAimMaxSpeed[id, Store.WeaponAmount] = v; }
            else if (key == "击退力度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponKnockBack[id, Store.WeaponAmount] = v; }
            else if (key == "价格") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponCost[id, Store.WeaponAmount] = v; }
            else if (key == "子弹价格") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAmmoCost[id, Store.WeaponAmount] = v; }
            else if (key == "队伍") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponTeam[id, Store.WeaponAmount] = v; }
            else if (key == "HUD菜单") { for (int id = 1; id < 33; id++) Store.WeaponList[id, Store.WeaponAmount] = value; }
            else if (key == "杀敌SPR") { for (int id = 1; id < 33; id++) Store.WeaponSpr[id, Store.WeaponAmount] = value; }
            else if (key == "W模型子模型序号") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponWModelBody[id, Store.WeaponAmount] = v; }
            else if (key == "P模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponPModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "V模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponVModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "W模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponWModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "机瞄模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponVModelS[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "开枪声音1")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound1[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "开枪声音2")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound2[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "消音开枪声音1")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound3[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "消音开枪声音2")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound4[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "开枪声音大小") { float v = ToAttenuation(value, 0.35f); for (int id = 1; id < 33; id++) Store.WeaponFireSoundVolume[id, Store.WeaponAmount] = v; }
            else if (key == "消音开枪声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); for (int id = 1; id < 33; id++) Store.WeaponSilenceFireSoundVolume[id, Store.WeaponAmount] = v; }
            else if (key == "购买命令") { for (int id = 1; id < 33; id++) Store.WeaponBuyCommand[id, Store.WeaponAmount] = value; }
            else if (key == "静止动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimIdle] = v; }
            else if (key == "换弹动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimReload] = v; }
            else if (key == "掏出动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimDraw] = v; }
            else if (key == "射击动作1") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimShoot1] = v; }
            else if (key == "射击动作2") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimShoot2] = v; }
            else if (key == "射击动作3") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimShoot3] = v; }
            else if (key == "散弹换弹开始") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimStartReload] = v; }
            else if (key == "散弹换弹装弹") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimInsert] = v; }
            else if (key == "散弹换弹结束") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimAfterReload] = v; }
            else if (key == "机瞄开启动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimSightOpen] = v; }
            else if (key == "机瞄关闭动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimSightClose] = v; }
            else if (key == "消音开启动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimAttachSilencer] = v; }
            else if (key == "消音关闭动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimDetachSilencer] = v; }
            else if (key == "消音静止动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimIdleSilenced] = v; }
            else if (key == "消音换弹动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimReloadSilenced] = v; }
            else if (key == "消音掏出动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimDrawSilenced] = v; }
            else if (key == "消音射击动作1") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimShoot1Silenced] = v; }
            else if (key == "消音射击动作2") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimShoot2Silenced] = v; }
            else if (key == "消音射击动作3") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponAnim[id, Store.WeaponAmount, WpnConst.AnimShoot3Silenced] = v; }

            NotifyLoadFiles(WpnConst.SlotWeapon, Store.WeaponAmount, key, value);
        }
    }

    // ---- 近战武器 ----

    private static void LoadKnife(string path)
    {
        foreach (var (key, value) in ReadIni(path))
        {
            if (key == "武器序号")
            {
                Store.WeaponAmount++;
                Store.WeaponTemplateSerialNumber[WpnConst.SlotKnife, ToInt(value)] = Store.WeaponAmount;
                for (int id = 1; id < 33; id++) Store.WeaponId[id, Store.WeaponAmount] = EngineConst.CswKnife;
                continue;
            }

            if (key == "轻击结束时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeSlashResetTime[id, Store.WeaponAmount] = v; }
            else if (key == "轻击延迟时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeSlashTime[id, Store.WeaponAmount] = v; }
            else if (key == "轻击距离") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeSlashRange[id, Store.WeaponAmount] = v; }
            else if (key == "轻击角度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeSlashAngle[id, Store.WeaponAmount] = v; }
            else if (key == "轻击角度判断间隔") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeSlashAngleOffset[id, Store.WeaponAmount] = v; }
            else if (key == "轻击高度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeSlashHeight[id, Store.WeaponAmount] = v; }
            else if (key == "轻击高度判断间隔") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeSlashHeightOffset[id, Store.WeaponAmount] = v; }
            else if (key == "重击结束时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeStabResetTime[id, Store.WeaponAmount] = v; }
            else if (key == "重击延迟时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeStabTime[id, Store.WeaponAmount] = v; }
            else if (key == "重击距离") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeStabRange[id, Store.WeaponAmount] = v; }
            else if (key == "重击角度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeStabAngle[id, Store.WeaponAmount] = v; }
            else if (key == "重击角度判断间隔") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeStabAngleOffset[id, Store.WeaponAmount] = v; }
            else if (key == "重击高度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeStabHeight[id, Store.WeaponAmount] = v; }
            else if (key == "重击高度判断间隔") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.KnifeStabHeightOffset[id, Store.WeaponAmount] = v; }
            else if (key == "掏出时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponDrawTime[id, Store.WeaponAmount] = v; }
            else if (key == "伤害倍数") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponDamage[id, Store.WeaponAmount] = v; }
            else if (key == "重击伤害倍数") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponDamage2[id, Store.WeaponAmount] = v; }
            else if (key == "行走速度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponMaxSpeed[id, Store.WeaponAmount] = v; }
            else if (key == "击退") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponKnockBack[id, Store.WeaponAmount] = v; }
            else if (key == "价格") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponCost[id, Store.WeaponAmount] = v; }
            else if (key == "队伍") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponTeam[id, Store.WeaponAmount] = v; }
            else if (key == "HUD菜单") { for (int id = 1; id < 33; id++) Store.WeaponList[id, Store.WeaponAmount] = value; }
            else if (key == "杀敌SPR") { for (int id = 1; id < 33; id++) Store.WeaponSpr[id, Store.WeaponAmount] = value; }
            else if (key == "P模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponPModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "V模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponVModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "掏出声音")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound1[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "击墙声音")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound2[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "击空声音")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound3[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "重击声音")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound4[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "轻击声音1")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound5[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "轻击声音2")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound6[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "击墙声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); for (int id = 1; id < 33; id++) Store.KnifeSoundVolume[id, Store.WeaponAmount, 0] = v; }
            else if (key == "击空声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); for (int id = 1; id < 33; id++) Store.KnifeSoundVolume[id, Store.WeaponAmount, 1] = v; }
            else if (key == "重击声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); for (int id = 1; id < 33; id++) Store.KnifeSoundVolume[id, Store.WeaponAmount, 2] = v; }
            else if (key == "轻击声音1大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); for (int id = 1; id < 33; id++) Store.KnifeSoundVolume[id, Store.WeaponAmount, 3] = v; }
            else if (key == "轻击声音2大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); for (int id = 1; id < 33; id++) Store.KnifeSoundVolume[id, Store.WeaponAmount, 4] = v; }
            else if (key == "购买命令") { for (int id = 1; id < 33; id++) Store.WeaponBuyCommand[id, Store.WeaponAmount] = value; }
            else if (key == "静止动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimIdle] = v; }
            else if (key == "轻击动作1") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimSlash1] = v; }
            else if (key == "轻击动作2") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimSlash2] = v; }
            else if (key == "掏出动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimDraw] = v; }
            else if (key == "重击动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimStab] = v; }
            else if (key == "重击未击中动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimStabMiss] = v; }
            else if (key == "轻击动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimMidSlash] = v; }
            else if (key == "轻击未击中动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimMidSlashMiss] = v; }
            else if (key == "轻击开始动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimSlashBegin] = v; }
            else if (key == "重击开始动作") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.KnifeAnim[id, Store.WeaponAmount, WpnConst.KnifeAnimStabBegin] = v; }

            NotifyLoadFiles(WpnConst.SlotKnife, Store.WeaponAmount, key, value);
        }
    }

    // ---- 投掷武器 ----

    private static void LoadGrenade(string path)
    {
        foreach (var (key, value) in ReadIni(path))
        {
            if (key == "武器序号")
            {
                Store.WeaponAmount++;
                Store.WeaponTemplateSerialNumber[WpnConst.SlotGrenade, ToInt(value)] = Store.WeaponAmount;
                for (int id = 1; id < 33; id++) Store.WeaponId[id, Store.WeaponAmount] = EngineConst.CswHegrenade;
                continue;
            }

            if (key == "掏出时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponDrawTime[id, Store.WeaponAmount] = v; }
            else if (key == "爆炸时间") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.GrenadeData[id, Store.WeaponAmount, 1] = v; }
            else if (key == "爆炸范围") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.GrenadeData[id, Store.WeaponAmount, 2] = v; }
            else if (key == "伤害倍数") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponDamage[id, Store.WeaponAmount] = v; }
            else if (key == "行走速度") { float v = ToFloat(value); for (int id = 1; id < 33; id++) Store.WeaponMaxSpeed[id, Store.WeaponAmount] = v; }
            else if (key == "价格") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponCost[id, Store.WeaponAmount] = v; }
            else if (key == "队伍") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponTeam[id, Store.WeaponAmount] = v; }
            else if (key == "HUD菜单") { for (int id = 1; id < 33; id++) Store.WeaponList[id, Store.WeaponAmount] = value; }
            else if (key == "杀敌SPR") { for (int id = 1; id < 33; id++) Store.WeaponSpr[id, Store.WeaponAmount] = value; }
            else if (key == "W模型子模型序号") { int v = ToInt(value); for (int id = 1; id < 33; id++) Store.WeaponWModelBody[id, Store.WeaponAmount] = v; }
            else if (key == "P模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponPModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "V模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponVModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "W模型")
            {
                for (int id = 1; id < 33; id++) Store.WeaponWModel[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "爆炸SPR")
            {
                int sprId = 0;
                if (value.Length > 0) sprId = Eng.PrecacheModel(value);
                for (int id = 1; id < 33; id++) Store.GrenadeSprId[id, Store.WeaponAmount] = sprId;
            }
            else if (key == "爆炸声音")
            {
                for (int id = 1; id < 33; id++) Store.WeaponSound1[id, Store.WeaponAmount] = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "爆炸声音大小") { float v = ToAttenuation(value, 0.25f); for (int id = 1; id < 33; id++) Store.WeaponFireSoundVolume[id, Store.WeaponAmount] = v; }
            else if (key == "购买命令") { for (int id = 1; id < 33; id++) Store.WeaponBuyCommand[id, Store.WeaponAmount] = value; }

            NotifyLoadFiles(WpnConst.SlotGrenade, Store.WeaponAmount, key, value);
        }
    }
}
