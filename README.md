# WeaponTemplate.NET

参考 AMXX Pawn 项目 WeaponTemplate（【AST】加枪模板 v1.38，作者 Fly / Izayoi），
基于 GoldSrcMod.Net 的 MetaMod 模块产品线（NativeAOT + C#）完成的移植版。

框架依赖来自 nuget.org（作者 CeSun）的 1.1.0 包：`Amxmodx.Framework` 与
`Amxmodx.SourceGenerator`（分析器包自动带入 Amxmodx.Net / HLSDK.Net / MetaMod.Net），
本仓库不依赖本机存在 GoldSrcMod.Net 源码目录。

**当前状态：三个模块全部编译通过，并已通过 `dotnet publish -r win-x86`（NativeAOT）生成原生 DLL；
按需求未做实机测试。**

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
    │   ├── Const.cs             # wpn_const.inc + CSW/TE/Ham/按钮等引擎常量
    │   ├── Native.cs            # fakemeta(pev/engfunc/get_pdata)的 HLSDK 直接访问封装
    │   ├── WeaponStore.cs       # 原版静态表 + 33×MAXWEAPON 模板数据 + 每地图重置
    │   ├── ConfigLoad.cs        # plugin_precache + LoadWeapon/LoadKnife/LoadGrenade(中文 INI)
    │   ├── Forwards.cs          # WPN_* 事件([AmxxForward],WPN_LoadFiles 因 precache 时序手动注册)
    │   ├── Natives.cs           # WPN_* natives([AmxxNative])
    │   ├── Hooks.cs             # FM_* forward → [AmxxHook] metamod 函数表 hook
    │   ├── Core.cs              # GiveWeapon/换弹补给/WeaponList/金钱备弹/烟雾/血 等业务
    │   ├── Attack.cs            # CheckAttack/ExecuteAttack/KnifeAttack/机瞄开合
    │   ├── Buy.cs               # BOT 购买模拟
    │   ├── HamCallbacks.cs      # hamsandwich/消息/事件回调 + plugin_init 全量注册
    │   └── include/             # Pawn 侧 include(wpn_const.inc 原样;newweapon.inc 已按新 ABI 更新)
    ├── WeaponExtend/            # WeaponExtend.sma(抛射物/范围攻击扩展)的移植
    └── ZpNewWeapon/             # zp_newweapon.sma(ZP4.3 兼容胶水)的移植
```

## 机制映射(Pawn → C#/GoldSrcMod.Net)

| 原版 AMXX 机制 | 移植版实现 |
|---|---|
| `register_forward(FM_*)` fakemeta forward | `[AmxxHook]` metamod 函数表 hook（ClientCommand/SetModel/UpdateClientData/PlaybackEvent 为 GameDll 表，EmitSound/TraceLine/TraceHull 为 Engine 表，Think/PlayerPostThink 为 Post hook）；`FMRES_SUPERCEDE` 语义由"不调用原版函数"表达 |
| `RegisterHam`(hamsandwich) | `plugin_init` 中经 `MF.CallNative("RegisterHam", ...)` 注册，回调经 bridge public（`[AmxxCallback]`）进入 C#；返回 `HAM_IGNORED(1)/HAM_SUPERCEDE(4)` |
| `register_message` / `register_event` / `register_logevent` | AMXX 核心 native（`MF.CallNative` / `MF.RegisterEvent` / `MF.RegisterLogEvent`），回调同为 bridge public |
| fakemeta `pev/set_pev/engfunc/dllfunc` | HLSDK 结构体直接访问（`edict_t->v`、`ModuleRuntime.g_engfuncs`、`gpGamedllFuncs->dllapi_table`） |
| `get_pdata_*` / `get_pdata_cbase` | `pvPrivateData` 单元读写（Windows 偏移，linuxdiff 仅 Linux 生效故原样使用）；cbase 转换 = `[vtable][pev*]` 布局 → `pev->pContainingEntity` |
| `CreateMultiForward` 的 WPN_* 事件 | `[AmxxForward]`（SDK 在插件加载后自动注册）+ 生成的 `Execute_WpnXxx`；**例外**：`WPN_LoadFiles` 需在 precache 阶段触发，于 `plugin_precache` 中手动 `MF.RegisterForward`/`MF.ExecuteForward` |
| `register_native` 的 WPN_* | `[AmxxNative]` |
| `plugin_precache` / `plugin_init` | bridge 插件的同名 public（`[AmxxCallback]`），每张地图执行一次；C# 静态状态由 `Store.ResetAll()` 显式重置 |
| INI 解析(fopen/fgets/strtok) | `File.ReadAllLines`，优先 GBK(936) 解码，兼容 UTF-8 |

## 与原版的已知差异

1. **WPN_RegisterWeapon / WPN_SetWeaponAnim / WPN_RegisterKnife / WPN_ExecuteAttack / WPN_SpawnBlood / WPN_FakeSmoke 的数组参数 ABI**：
   源生成器要求 `Span<T>` 后紧跟 `int len`，因此这些 native 的 Pawn 签名变为 `(arr[], len)` 约定
   （原版是定长数组不带 len）。`src/WeaponTemplate/include/newweapon.inc` 已按新 ABI 更新，
   Pawn 插件把它和 `wpn_const.inc` 放进编译器 include 目录即可调用；其余 native 与全部 forward 签名不变。
2. **原生击败 Pawn 消息钩的时序**：消息类钩子（DeathMsg 等）经 AMXX 核心 `register_message`，
   与原版一致；本模块自身通过 `g_engfuncs` 直发的消息绕过 AMXX 消息钩（与原版 fakemeta engfunc 行为一致）。
3. **HAM_TakeDamage_Post 中"蹲下减击退"判定**：原版 Pawn 表达式受运算符优先级影响实际退化为
   `flags & 1`（恒假）；移植版按注释本意实现（`FL_DUCKING|FL_ONGROUND` 同时成立时击退 ×0.25）。
4. `zp_newweapon` 依赖 `zombieplague` 模块的 native，未启用 ZP 时该模块 bridge 无法加载（与原版一致，
   原版也只能在 ZP 服务器上运行）。

## 构建与部署

要求 Windows、.NET SDK 10.0.400（见 global.json）、MSVC x86 工具链（NativeAOT）。
框架依赖来自 nuget.org 的 1.1.0 包；若本地 GoldSrcMod.Net 源码新增了未发版的 API，
可改回 ProjectReference（历史版本见 git 记录）或等下一个包版本。

```cmd
dotnet build WeaponTemplate.NET.slnx -c Release
:: 或直接发布(游戏加载的原生 DLL):
dotnet publish src\WeaponTemplate\WeaponTemplate.csproj -c Release -r win-x86 -o artifacts\WeaponTemplate
dotnet publish src\WeaponExtend\WeaponExtend.csproj -c Release -r win-x86 -o artifacts\WeaponExtend
dotnet publish src\ZpNewWeapon\ZpNewWeapon.csproj -c Release -r win-x86 -o artifacts\ZpNewWeapon
```

部署到游戏目录：

1. 三个 DLL 复制到 `cstrike/addons/amxmodx/modules/`（同目录 PDB 仅调试用，可不复制）。
2. `configs/modules.ini` 启用（顺序即依赖顺序）：

   ```ini
   WeaponTemplate
   hamsandwich
   WeaponExtend
   zombieplague   ; 仅 ZpNewWeapon 需要
   ZpNewWeapon
   ```

3. `configs/plugins-WeaponTemplate_bridge.ini` 等由模块在 attach 阶段自动生成（回调模式），
   无需手改主 `plugins.ini`。
4. INI 配置放入 `amxx_configsdir`（`newweapon.ini` / `newknife.ini` / `newgrenade.ini`，见 `configs/`）。

**加载顺序注意**：`WeaponExtend` / `ZpNewWeapon` 依赖核心模块在 precache 阶段广播的
`WPN_LoadFiles`，因此它们的 bridge 插件必须在 WeaponTemplate bridge **之前**加载（AMXX 按生成
配置的加载顺序执行；若收不到配置键，调整 `plugins-WeaponTemplate_bridge.ini` 在
`plugins-WeaponExtend_bridge.ini` 之前 include 即可，与原版 Pawn 插件的 plugins.ini 顺序约束相同）。
