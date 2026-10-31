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
    // 必须是 -1 而不是默认的 0:AMXX 里 registerForward 返回 m_Forwards.length() << 1,
    // 而 plugin_init 是 clear() 之后第一个注册的 forward,它的 id 恰好就是 0。
    // 也就是说 0 不是"还没注册"的空值,而是一个真句柄 —— 拿 0 去 ExecuteForward
    // 会向服务器上每一个插件广播 plugin_init(见 amxmodx/CForward.cpp:404、meta_api.cpp:407/524)。
    public static int LoadFilesForward = -1;

    public static void OnPrecache()
    {
        Store.BloodSpray = PrecacheAndProbe("sprites/bloodspray.spr");
        Store.BloodDrop = PrecacheAndProbe("sprites/blood.spr");
        Store.SmokeId = PrecacheAndProbe("sprites/smokepuff.spr");
        Store.DisableBuyTime = Eng.GameTime + Eng.CvarFloat("mp_buytime") * 60.0f;

        LoadFilesForward = MF.RegisterForward("WPN_LoadFiles", ForwardExecType.ET_CONTINUE,
            ForwardParam.FP_CELL, ForwardParam.FP_CELL, ForwardParam.FP_STRING, ForwardParam.FP_STRING);
        MF.Log($"[diag] WPN_LoadFiles forward={LoadFilesForward}");

        var config = MF.GetLocalInfo("amxx_configsdir", "addons/amxmodx/configs") ?? "addons/amxmodx/configs";
        var weaponIni = ResolveConfigFile(config, "newweapon.ini");
        if (weaponIni != null) LoadWeapon(weaponIni);
        var knifeIni = ResolveConfigFile(config, "newknife.ini");
        if (knifeIni != null) LoadKnife(knifeIni);
        var grenadeIni = ResolveConfigFile(config, "newgrenade.ini");
        if (grenadeIni != null) LoadGrenade(grenadeIni);
        MF.Log($"[diag] ini weapon={weaponIni ?? "missing"} knife={knifeIni ?? "missing"} grenade={grenadeIni ?? "missing"}");
    }

    // amxx_configsdir 相对 gamedir,而进程 CWD 是启动目录(如 Half-Life 根),
    // File API 必须先经 BuildPathname 解析,否则永远找不到配置。
    private static string? ResolveConfigFile(string configDir, string name)
    {
        var resolved = MF.BuildPathname($"{configDir}/{name}");
        return resolved != null && File.Exists(resolved) ? resolved : null;
    }

    // 预缓存同时记录引擎返回的 model index;Eng.PrecacheModel 内部已探测文件存在性,
    // 缺失时跳过并告警(避免把不存在的资源放进下载列表卡死客户端)。
    private static int PrecacheAndProbe(string path)
    {
        int index = Eng.PrecacheModel(path);
        MF.Log($"[diag] precache {path} index={index}");
        return index;
    }

    // ---- INI 读取 ----

    // 配置编码:BOM 优先;无 BOM 时先做严格 UTF-8 校验(现代编辑器默认 UTF-8),
    // 校验失败才按老 Pawn 生态的 ANSI/GBK 读;纯 ASCII 两种编码结果一致。
    private static Encoding DetectEncoding(string path)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch
        {
            return GetAnsiEncoding();
        }
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode;
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode;
        try
        {
            new UTF8Encoding(false, true).GetString(bytes);
            return Array.Exists(bytes, b => b > 0x7F) ? Encoding.UTF8 : GetAnsiEncoding();
        }
        catch (DecoderFallbackException)
        {
            return GetAnsiEncoding();
        }
    }

    private static Encoding GetAnsiEncoding()
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
        string[] lines = File.ReadAllLines(path, DetectEncoding(path));

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

    private static int ToInt(string value) => PawnMath.ParseInt(value);
    private static float ToFloat(string value) => PawnMath.ParseFloat(value);

    // SoundVolume 的解析规则:val > 0 时取 max(1-val, 0),否则用默认值
    private static float ToAttenuation(string value, float fallback)
    {
        float val = ToFloat(value);
        if (val <= 0.0f) return fallback;
        return (1.0f - val) > 0.0f ? 1.0f - val : 0.0f;
    }

    private static void NotifyLoadFiles(int slot, int weaponIndex, string key, string value)
    {
        // id 0 会被 ExecuteForward 当成 plugin_init 广播,宁可丢掉这次通知也不能重播插件初始化。
        if (LoadFilesForward <= 0)
        {
            MF.Log($"[diag] WPN_LoadFiles skipped forward={LoadFilesForward} key={key}");
            return;
        }
        MF.ExecuteForward(LoadFilesForward, slot, weaponIndex, key, value);
    }

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

    // 整个区块校验完成后才分配模板和广播,无效区块不会留下半成品或占用序号。
    private static IEnumerable<(int Index, List<(string Key, string Value)> Entries)> ReadTemplates(string path, int slot)
    {
        var blocks = new List<(string Serial, List<(string Key, string Value)> Entries)>();
        foreach (var entry in ReadIni(path))
        {
            if (entry.Key == "武器序号") blocks.Add((entry.Value, new()));
            else if (blocks.Count > 0) blocks[^1].Entries.Add(entry);
        }
        foreach (var (serialText, entries) in blocks)
        {
            int serial = ToInt(serialText);
            if (serial <= 0 || Store.Config.HasSerial(slot, serial) || !Store.CanRegister)
            {
                MF.Log($"[error] invalid/duplicate template serial or capacity exceeded: {path} serial={serialText}");
                continue;
            }
            int csw = slot == WpnConst.SlotKnife ? EngineConst.CswKnife :
                slot == WpnConst.SlotGrenade ? EngineConst.CswHegrenade :
                StrToCsw(entries.LastOrDefault(e => e.Key == "类型").Value ?? "");
            if (slot == WpnConst.SlotWeapon && !Store.IsGun(csw))
            {
                MF.Log($"[error] invalid weapon type: {path} serial={serial}");
                continue;
            }
            bool resourcesValid = true;
            foreach (var (key, value) in entries)
            {
                if (value.Length == 0) continue;
                bool model = key is "P模型" or "V模型" or "W模型" or "机瞄模型" or "爆炸SPR" or
                    "抛射物模型" or "拖尾间隔SPR" or "抛射物附带SPR";
                bool sound = key is "开枪声音1" or "开枪声音2" or "消音开枪声音1" or "消音开枪声音2" or
                    "掏出声音" or "击墙声音" or "击空声音" or "重击声音" or "轻击声音1" or "轻击声音2" or "爆炸声音路径" ||
                    (slot == WpnConst.SlotGrenade && key == "爆炸声音");
                if ((model || sound) && (sound ? Eng.PrecacheSound(value) : Eng.PrecacheModel(value)) <= 0)
                    resourcesValid = false;
            }
            if (!resourcesValid)
            {
                MF.Log($"[error] template rejected because a resource is missing: {path} serial={serial}");
                continue;
            }
            if (!Store.TryCreateTemplate(out int index)) continue;
            Store.Config.SetSerial(slot, serial, index);
            Store.Config[index].Id = csw;
            MF.Log($"[diag] template slot={slot} serial={serial} index={index} csw={csw} " +
                $"buy='{entries.LastOrDefault(e => e.Key == "购买命令").Value}' " +
                $"cost='{entries.LastOrDefault(e => e.Key == "价格").Value}' " +
                $"hud='{entries.LastOrDefault(e => e.Key == "HUD菜单").Value}'");
            yield return (index, entries);
        }
    }

    // ---- 主武器/副武器 ----

    private static void LoadWeapon(string path)
    {
        foreach (var template in ReadTemplates(path, WpnConst.SlotWeapon))
        foreach (var (key, value) in template.Entries)
        {
            // 原版对"武器序号"行直接 continue,不触发 WPN_LoadFiles

            if (key == "类型")
            {
                int csw = StrToCsw(value);
                Store.Config[template.Index].Id = csw;
            }
            else if (key == "上弹模式")
            {
                int mode = ToInt(value);
                Store.Config[template.Index].ReloadMode = mode;
            }
            else if (key == "开启机瞄")
            {
                var weapon = Store.Config[template.Index];
                weapon.Sight = weapon.Id is EngineConst.CswG3sg1 or EngineConst.CswSg550 or EngineConst.CswAwp or EngineConst.CswScout
                    ? 0 : ToInt(value);
            }
            else if (key == "子弹数") { int v = ToInt(value); Store.Config[template.Index].Clip = v; }
            else if (key == "备用子弹数") { int v = ToInt(value); Store.Config[template.Index].Ammo = v; }
            else if (key == "伤害倍数") { float v = ToFloat(value); Store.Config[template.Index].Damage = v; }
            else if (key == "武器射速") { float v = ToFloat(value); Store.Config[template.Index].Speed = v; }
            else if (key == "武器开镜射速") { float v = ToFloat(value); Store.Config[template.Index].AimSpeed = v; }
            else if (key == "武器消音射速") { float v = ToFloat(value); Store.Config[template.Index].SilencedSpeed = v; }
            else if (key == "武器后座") { float v = ToFloat(value); Store.Config[template.Index].Recoil = v; }
            else if (key == "武器开镜后座") { float v = ToFloat(value); Store.Config[template.Index].AimRecoil = v; }
            else if (key == "武器消音后座") { float v = ToFloat(value); Store.Config[template.Index].SilencedRecoil = v; }
            else if (key == "武器准确度") { float v = ToFloat(value); Store.Config[template.Index].Accuracy = v; }
            else if (key == "武器开镜准确度") { float v = ToFloat(value); Store.Config[template.Index].AimAccuracy = v; }
            else if (key == "武器消音准确度") { float v = ToFloat(value); Store.Config[template.Index].SilencedAccuracy = v; }
            else if (key == "开启消音时间") { float v = ToFloat(value); Store.Config[template.Index].SilencedTime[0] = v; }
            else if (key == "关闭消音时间") { float v = ToFloat(value); Store.Config[template.Index].SilencedTime[1] = v; }
            else if (key == "镜头距离") { float v = ToFloat(value); Store.Config[template.Index].SightDistance = v; }
            else if (key == "镜头变化幅度") { float v = ToFloat(value); Store.Config[template.Index].SightFrameRate = v; }
            else if (key == "开启速度") { float v = ToFloat(value); Store.Config[template.Index].SightOpenTime = v; }
            else if (key == "关闭速度") { float v = ToFloat(value); Store.Config[template.Index].SightCloseTime = v; }
            else if (key == "换弹时间") { float v = ToFloat(value); Store.Config[template.Index].ReloadTime[0] = v; }
            else if (key == "散弹开始换弹时间") { float v = ToFloat(value); Store.Config[template.Index].ReloadTime[1] = v; }
            else if (key == "散弹结束换弹时间") { float v = ToFloat(value); Store.Config[template.Index].ReloadTime[2] = v; }
            else if (key == "掏出时间") { float v = ToFloat(value); Store.Config[template.Index].DrawTime = v; }
            else if (key == "行走速度") { float v = ToFloat(value); Store.Config[template.Index].MaxSpeed = v; }
            else if (key == "开镜行走速度") { float v = ToFloat(value); Store.Config[template.Index].AimMaxSpeed = v; }
            else if (key == "击退力度") { float v = ToFloat(value); Store.Config[template.Index].KnockBack = v; }
            else if (key == "价格") { int v = ToInt(value); Store.Config[template.Index].Cost = v; }
            else if (key == "子弹价格") { int v = ToInt(value); Store.Config[template.Index].AmmoCost = v; }
            else if (key == "队伍") { int v = ToInt(value); Store.Config[template.Index].Team = v; }
            else if (key == "HUD菜单") { Store.Config[template.Index].List = value; }
            else if (key == "杀敌SPR") { Store.Config[template.Index].Spr = value; }
            else if (key == "W模型子模型序号") { int v = ToInt(value); Store.Config[template.Index].WModelBody = v; }
            else if (key == "P模型")
            {
                Store.Config[template.Index].PModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "V模型")
            {
                Store.Config[template.Index].VModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "W模型")
            {
                Store.Config[template.Index].WModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "机瞄模型")
            {
                Store.Config[template.Index].SightModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "开枪声音1")
            {
                Store.Config[template.Index].Sound1 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "开枪声音2")
            {
                Store.Config[template.Index].Sound2 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "消音开枪声音1")
            {
                Store.Config[template.Index].Sound3 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "消音开枪声音2")
            {
                Store.Config[template.Index].Sound4 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "开枪声音大小") { float v = ToAttenuation(value, 0.35f); Store.Config[template.Index].FireSoundVolume = v; }
            else if (key == "消音开枪声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); Store.Config[template.Index].SilenceFireSoundVolume = v; }
            else if (key == "购买命令") { Store.Config[template.Index].BuyCommand = value; }
            else if (key == "静止动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimIdle] = v; }
            else if (key == "换弹动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimReload] = v; }
            else if (key == "掏出动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimDraw] = v; }
            else if (key == "射击动作1") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimShoot1] = v; }
            else if (key == "射击动作2") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimShoot2] = v; }
            else if (key == "射击动作3") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimShoot3] = v; }
            else if (key == "散弹换弹开始") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimStartReload] = v; }
            else if (key == "散弹换弹装弹") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimInsert] = v; }
            else if (key == "散弹换弹结束") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimAfterReload] = v; }
            else if (key == "机瞄开启动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimSightOpen] = v; }
            else if (key == "机瞄关闭动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimSightClose] = v; }
            else if (key == "消音开启动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimAttachSilencer] = v; }
            else if (key == "消音关闭动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimDetachSilencer] = v; }
            else if (key == "消音静止动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimIdleSilenced] = v; }
            else if (key == "消音换弹动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimReloadSilenced] = v; }
            else if (key == "消音掏出动作") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimDrawSilenced] = v; }
            else if (key == "消音射击动作1") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimShoot1Silenced] = v; }
            else if (key == "消音射击动作2") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimShoot2Silenced] = v; }
            else if (key == "消音射击动作3") { int v = ToInt(value); Store.Config[template.Index].Anim[WpnConst.AnimShoot3Silenced] = v; }

            NotifyLoadFiles(WpnConst.SlotWeapon, template.Index, key, value);
        }
    }

    // ---- 近战武器 ----

    private static void LoadKnife(string path)
    {
        foreach (var template in ReadTemplates(path, WpnConst.SlotKnife))
        foreach (var (key, value) in template.Entries)
        {

            if (key == "轻击结束时间") { float v = ToFloat(value); Store.Config[template.Index].KnifeSlashResetTime = v; }
            else if (key == "轻击延迟时间") { float v = ToFloat(value); Store.Config[template.Index].KnifeSlashTime = v; }
            else if (key == "轻击距离") { float v = ToFloat(value); Store.Config[template.Index].KnifeSlashRange = v; }
            else if (key == "轻击角度") { float v = ToFloat(value); Store.Config[template.Index].KnifeSlashAngle = v; }
            else if (key == "轻击角度判断间隔") { float v = ToFloat(value); Store.Config[template.Index].KnifeSlashAngleOffset = v; }
            else if (key == "轻击高度") { float v = ToFloat(value); Store.Config[template.Index].KnifeSlashHeight = v; }
            else if (key == "轻击高度判断间隔") { float v = ToFloat(value); Store.Config[template.Index].KnifeSlashHeightOffset = v; }
            else if (key == "重击结束时间") { float v = ToFloat(value); Store.Config[template.Index].KnifeStabResetTime = v; }
            else if (key == "重击延迟时间") { float v = ToFloat(value); Store.Config[template.Index].KnifeStabTime = v; }
            else if (key == "重击距离") { float v = ToFloat(value); Store.Config[template.Index].KnifeStabRange = v; }
            else if (key == "重击角度") { float v = ToFloat(value); Store.Config[template.Index].KnifeStabAngle = v; }
            else if (key == "重击角度判断间隔") { float v = ToFloat(value); Store.Config[template.Index].KnifeStabAngleOffset = v; }
            else if (key == "重击高度") { float v = ToFloat(value); Store.Config[template.Index].KnifeStabHeight = v; }
            else if (key == "重击高度判断间隔") { float v = ToFloat(value); Store.Config[template.Index].KnifeStabHeightOffset = v; }
            else if (key == "掏出时间") { float v = ToFloat(value); Store.Config[template.Index].DrawTime = v; }
            else if (key == "伤害倍数") { float v = ToFloat(value); Store.Config[template.Index].Damage = v; }
            else if (key == "重击伤害倍数") { float v = ToFloat(value); Store.Config[template.Index].Damage2 = v; }
            else if (key == "行走速度") { float v = ToFloat(value); Store.Config[template.Index].MaxSpeed = v; }
            else if (key == "击退") { float v = ToFloat(value); Store.Config[template.Index].KnockBack = v; }
            else if (key == "价格") { int v = ToInt(value); Store.Config[template.Index].Cost = v; }
            else if (key == "队伍") { int v = ToInt(value); Store.Config[template.Index].Team = v; }
            else if (key == "HUD菜单") { Store.Config[template.Index].List = value; }
            else if (key == "杀敌SPR") { Store.Config[template.Index].Spr = value; }
            else if (key == "P模型")
            {
                Store.Config[template.Index].PModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "V模型")
            {
                Store.Config[template.Index].VModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "掏出声音")
            {
                Store.Config[template.Index].Sound1 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "击墙声音")
            {
                Store.Config[template.Index].Sound2 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "击空声音")
            {
                Store.Config[template.Index].Sound3 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "重击声音")
            {
                Store.Config[template.Index].Sound4 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "轻击声音1")
            {
                Store.Config[template.Index].Sound5 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "轻击声音2")
            {
                Store.Config[template.Index].Sound6 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "击墙声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); Store.Config[template.Index].KnifeSoundVolume[0] = v; }
            else if (key == "击空声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); Store.Config[template.Index].KnifeSoundVolume[1] = v; }
            else if (key == "重击声音大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); Store.Config[template.Index].KnifeSoundVolume[2] = v; }
            else if (key == "轻击声音1大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); Store.Config[template.Index].KnifeSoundVolume[3] = v; }
            else if (key == "轻击声音2大小") { float v = ToAttenuation(value, EngineConst.AttnNorm); Store.Config[template.Index].KnifeSoundVolume[4] = v; }
            else if (key == "购买命令") { Store.Config[template.Index].BuyCommand = value; }
            else if (key == "静止动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimIdle] = v; }
            else if (key == "轻击动作1") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimSlash1] = v; }
            else if (key == "轻击动作2") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimSlash2] = v; }
            else if (key == "掏出动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimDraw] = v; }
            else if (key == "重击动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimStab] = v; }
            else if (key == "重击未击中动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimStabMiss] = v; }
            else if (key == "轻击动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimMidSlash] = v; }
            else if (key == "轻击未击中动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimMidSlashMiss] = v; }
            else if (key == "轻击开始动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimSlashBegin] = v; }
            else if (key == "重击开始动作") { int v = ToInt(value); Store.Config[template.Index].KnifeAnim[WpnConst.KnifeAnimStabBegin] = v; }

            NotifyLoadFiles(WpnConst.SlotKnife, template.Index, key, value);
        }
    }

    // ---- 投掷武器 ----

    private static void LoadGrenade(string path)
    {
        foreach (var template in ReadTemplates(path, WpnConst.SlotGrenade))
        foreach (var (key, value) in template.Entries)
        {

            if (key == "掏出时间") { float v = ToFloat(value); Store.Config[template.Index].DrawTime = v; }
            else if (key == "爆炸时间") { float v = ToFloat(value); Store.Config[template.Index].GrenadeData[1] = v; }
            else if (key == "爆炸范围") { float v = ToFloat(value); Store.Config[template.Index].GrenadeData[2] = v; }
            else if (key == "伤害倍数") { float v = ToFloat(value); Store.Config[template.Index].Damage = v; }
            else if (key == "行走速度") { float v = ToFloat(value); Store.Config[template.Index].MaxSpeed = v; }
            else if (key == "价格") { int v = ToInt(value); Store.Config[template.Index].Cost = v; }
            else if (key == "队伍") { int v = ToInt(value); Store.Config[template.Index].Team = v; }
            else if (key == "HUD菜单") { Store.Config[template.Index].List = value; }
            else if (key == "杀敌SPR") { Store.Config[template.Index].Spr = value; }
            else if (key == "W模型子模型序号") { int v = ToInt(value); Store.Config[template.Index].WModelBody = v; }
            else if (key == "P模型")
            {
                Store.Config[template.Index].PModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "V模型")
            {
                Store.Config[template.Index].VModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "W模型")
            {
                Store.Config[template.Index].WModel = value;
                if (value.Length > 0) Eng.PrecacheModel(value);
            }
            else if (key == "爆炸SPR")
            {
                int sprId = 0;
                if (value.Length > 0) sprId = Eng.PrecacheModel(value);
                Store.Config[template.Index].GrenadeSprId = sprId;
            }
            else if (key == "爆炸声音")
            {
                Store.Config[template.Index].Sound1 = value;
                if (value.Length > 0) Eng.PrecacheSound(value);
            }
            else if (key == "爆炸声音大小") { float v = ToAttenuation(value, 0.25f); Store.Config[template.Index].FireSoundVolume = v; }
            else if (key == "购买命令") { Store.Config[template.Index].BuyCommand = value; }

            NotifyLoadFiles(WpnConst.SlotGrenade, template.Index, key, value);
        }
    }
}
