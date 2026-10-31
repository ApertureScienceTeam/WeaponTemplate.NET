# WeaponTemplate.NET

参考 AMXX Pawn 项目 WeaponTemplate（【AST】加枪模板 v1.38，作者 Fly / Izayoi），
基于 GoldSrcMod.Net 的 MetaMod 模块产品线（NativeAOT + C#）完成的移植版。

三个模块通过 NuGet 引用 `Amxmodx.Framework` 和 `Amxmodx.SourceGenerator`，
版本统一为 `1.1.1-alpha.0.4`。源生成器包自动导入 bridge 构建目标，
构建时会在模块 DLL 同目录生成 `.amxx`。

三个模块支持发布为 Windows x86 NativeAOT DLL。
GoldSrcMod.Net 生成器已支持固定数组；16 个 native 及原版 include 的参数数量和顺序现已保持一致。

## 工程结构

```
WeaponTemplate.NET/
├── global.json                  # .NET SDK 10.0.400(与 GoldSrcMod.Net 一致)
├── WeaponTemplate.NET.slnx          # 解决方案(.NET 10 slnx 格式)
├── configs/                     # 原版 INI 配置(newweapon/newknife/newgrenade.ini)
├── Example/                     # 原版示例配置(主武器副武器/投掷/近战示例.ini)
├── artifacts/                   # publish 产物(三个原生 DLL)
└── src/
    ├── WeaponTemplate/          # 核心模块(WeaponTemplate.sma 的移植)
    │   ├── Module.cs            # 入口 partial class(源生成器补全 17 个固定导出)
    │   ├── Lifecycle/           # 插件生命周期:PluginLifecycle.cs(precache/init/PluginsLoaded + 全量注册)
    │   ├── Consts/              # WpnConst(wpn_const.inc) + EngineConst(CSW/TE/Ham/按钮等)
    │   ├── Data/                # WeaponData/WeaponConfig(模板) + PlayerData + WeaponStore(状态枢纽) + GameTables(静态表)
    │   ├── Engine/              # Eng.cs:fakemeta(pev/engfunc/get_pdata)的 HLSDK 直接访问封装
    │   ├── Api/                 # 对 Pawn 的接口:Natives(注册/发放/参数) + Forwards(WPN_* 事件)
    │   ├── Interop/             # 生态 native 声明式导入([AmxxImportNative],按来源模块分文件:HamSandwich/AmxModX/FakeMeta)
    │   ├── Hooks/               # [AmxxHook] metamod 函数表 hook(按 ClientCommand/SetModel/Trace/PlayerThink 等分文件)
    │   ├── Callbacks/           # [AmxxCallback] Ham/消息/事件回调(按 Events/Messages/Ham 分文件)
    │   ├── Features/            # 业务逻辑:ConfigLoad(INI)/WeaponGive(发放)/WeaponAmmo(弹药)/PlayerStocks(金钱备弹焦距)/VectorMath(向量)/Effects(血烟雾)/Attack/Buy
    │   └── include/             # Pawn 侧 include(与原版逐字节一致)
    ├── WeaponExtend/            # WeaponExtend.sma(抛射物/范围攻击扩展)的移植
    └── ZpNewWeapon/             # zp_newweapon.sma(ZP4.3 兼容胶水)的移植
```

## 机制映射(Pawn → C#/GoldSrcMod.Net)

| 原版 AMXX 机制 | 移植版实现 |
|---|---|
| `register_forward(FM_*)` fakemeta forward | `[AmxxHook]` metamod 函数表 hook（ClientCommand/UpdateClientData 为 GameDll 表，SetModel/PlaybackEvent/EmitSound/TraceLine/TraceHull 为 Engine 表，Think/PlayerPostThink 为 Post hook）；`FMRES_SUPERCEDE` 通过设置 `gpMetaGlobals->mres` 表达；未处理时由宿主调用原版 |
| `RegisterHam`(hamsandwich) | `[AmxxImportNative("RegisterHam")]` 声明式导入（`Interop/HamSandwich.cs`，`RegisterHamFromEntity`/`ExecuteHam(B)`/`SetHamParamFloat` 同类），回调经 bridge public（`[AmxxCallback]`）进入 C#；返回 `HAM_IGNORED(1)/HAM_SUPERCEDE(4)` |
| `register_message` / `register_event` / `register_logevent` | `register_message` 及 `get/set_msg_arg_*` 经 `[AmxxImportNative]` 导入（`Interop/AmxModX.cs`）；`register_event` / `register_logevent` 经 `MF.RegisterEvent` / `MF.RegisterLogEvent`，回调同为 bridge public |
| fakemeta `pev/set_pev/engfunc/dllfunc` | HLSDK 结构体直接访问（`edict_t->v`、`ModuleRuntime.g_engfuncs`、`gpGamedllFuncs->dllapi_table`） |
| `get_pdata_*` / `get_pdata_cbase` | `pvPrivateData` 单元读写（Windows 偏移，linuxdiff 仅 Linux 生效故原样使用）；cbase 转换 = `[vtable][pev*]` 布局 → `pev->pContainingEntity` |
| `CreateMultiForward` 的 WPN_* 事件 | `[AmxxForward]` + `ET_CONTINUE`（SDK 在插件加载后自动注册，保留插件返回值）+ 生成的 `Execute_WpnXxx`；**例外**：`WPN_LoadFiles` 需在 precache 阶段触发，于 `plugin_precache` 中手动 `MF.RegisterForward`/`MF.ExecuteForward` |
| `register_native` 的 WPN_* | 显式 `[AmxxNative("WPN_...")]`，与 include 保持相同名称 |
| `plugin_precache` / `plugin_init` | bridge 插件的同名 public（`[AmxxCallback]`），每张地图执行一次；C# 静态状态由 `Store.ResetAll()` 显式重置 |
| INI 解析(fopen/fgets/strtok) | `File.ReadAllLines`，BOM 优先；无 BOM 时严格验证 UTF-8，否则按 GBK(936) 解码 |

## 与原版的已知差异

1. **固定数组 ABI 已恢复原版**：六个数组 native 用 `[AmxxBuffer(固定长度)] Span<T>` 声明。
   Pawn 调用继续使用原版签名，无需额外长度参数，`newweapon.inc` 与 `wpn_const.inc` 均与原版相同。
   原先试验移植版添加的长度参数已取消；针对那个版本修改过的 `.sma` 需恢复原版写法并重新编译。
2. **原生击败 Pawn 消息钩的时序**：消息类钩子（DeathMsg 等）经 AMXX 核心 `register_message`，
   与原版一致；本模块自身通过 `g_engfuncs` 直发的消息绕过 AMXX 消息钩（与原版 fakemeta engfunc 行为一致）。
3. **HAM_TakeDamage_Post 中"蹲下减击退"判定**：原版 Pawn 表达式受运算符优先级影响实际退化为
   `flags & 1`；移植版按注释本意实现（`FL_DUCKING|FL_ONGROUND` 同时成立时击退 ×0.25）。
4. `ZpNewWeapon` 依赖 Zombie Plague 4.3 **Pawn 插件**提供的 `zp_*` native；只有安装了相应 ZP 插件才启用该模块和 bridge。

## 构建与发布

需要 Windows、.NET SDK 10.0.400（见 global.json）、MSVC x86 工具链。

```cmd
dotnet build WeaponTemplate.NET.slnx -c Release
dotnet publish src\WeaponTemplate\WeaponTemplate.csproj -c Release -r win-x86 -o artifacts\WeaponTemplate
dotnet publish src\WeaponExtend\WeaponExtend.csproj -c Release -r win-x86 -o artifacts\WeaponExtend
dotnet publish src\ZpNewWeapon\ZpNewWeapon.csproj -c Release -r win-x86 -o artifacts\ZpNewWeapon
```

## 部署

1. 每个 `artifacts/<模块名>/<模块名>_amxx.dll` 复制到游戏的 `cstrike/addons/amxmodx/modules/`。
   PDB 可随 DLL 部署以便排错。
2. 每个 `artifacts/<模块名>/<模块名>_bridge.amxx`（与模块 DLL 同目录）复制到游戏的
   `cstrike/addons/amxmodx/plugins/`。DLL 与 bridge 应使用同一次构建的产物。
3. 在 `cstrike/addons/amxmodx/configs/modules.ini` 中启用需要的模块：

   ```ini
   hamsandwich
   WeaponTemplate
   WeaponExtend   ; 可选：抛射物扩展
   ;ZpNewWeapon   ; 仅安装 Zombie Plague 4.3 时启用
   ```

4. **手动**在 `cstrike/addons/amxmodx/configs/plugins.ini` 中列出已部署的 bridge。
   构建期生成 `.amxx`；运行时不会生成或修改 `plugins-*.ini`。例如：

   ```ini
   ;zombie_plague40.amxx        ; 仅 ZP 服务器：先加载 zp_* native 的提供者
   WeaponExtend_bridge.amxx    ; 仅启用 WeaponExtend 时添加
   ;ZpNewWeapon_bridge.amxx     ; 仅启用 ZpNewWeapon 时添加
   WeaponTemplate_bridge.amxx
   ```

   所有启用模块的 bridge 必须能成功加载，才会绑定对应回调。保留现有其他插件条目。
5. 把 `configs/newweapon.ini`、`newknife.ini`、`newgrenade.ini` 放入游戏的 `amxx_configsdir`
   （默认 `addons/amxmodx/configs`）。重启游戏/服务器进程，使原生 DLL 生效。

## 配置和资源约定

- 模板总量最多 **127**，主/副武器、刀、手雷共用容量；每类配置的“武器序号”范围为 1–127，不能重复。
- 先校验整个配置区块及资源，再分配模板并发送 `WPN_LoadFiles`。无效区块会记录错误并跳过，后续有效区块继续加载。
- 未填写的字符串为 `""`；未填写或为空的购买命令不会匹配任何命令。`empty` 保留为禁止菜单购买的约定。
- 模型与精灵路径相对游戏文件系统；声音路径相对 `sound/`。文件可来自 cstrike、valve 回退目录或 PAK。
  只要填写了资源路径，该文件就必须有效；资源缺失时不启用该模板。
- GoldSrc 会继续持有预缓存和模型名称的地址，模块统一使用引擎字符串池，并在地图切换时重建缓存。
  该要求可见 [ReHLDS 的原版兼容实现](https://github.com/rehlds/ReHLDS/blob/master/rehlds/engine/pr_cmds.cpp#L1397)。
- 核心、抛射物扩展及 ZP 道具映射都按地图重置。`WPN_ExecuteAttack` 与原版相同，
  使用七个参数，参数数组和坐标数组的固定长度 6、3 由模块内部声明。
