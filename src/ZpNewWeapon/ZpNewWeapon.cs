using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// zp_newweapon.sma v1.0 的 C# 移植:加枪模板 & ZP4.3 兼容插件。
// 依赖:WeaponTemplate 核心模块(在 modules.ini 中启用)与 zombieplague 模块。
// 原版说明:武器名字储存在 wpn_szUser1 里;购买命令为 empty 的武器不注册为 ZP 道具。
public static unsafe partial class ZpNewWeapon
{
    private const int AmmoCost1 = 100;
    private const int AmmoCost2 = 100;
    private const int MaxPlayers = 33;
    private const int MaxWeapon = 128;

    // 购买子弹的道具id / 购买武器的道具id
    private static readonly int[] AmmoItemId = new int[2];
    private static readonly int[] ItemId = new int[MaxWeapon];

    // ---- 引擎小工具 ----

    private static edict_t* EntOfIndex(int index)
    {
        if (index < 0) return null;
        var p = ModuleRuntime.g_engfuncs.pfnPEntityOfEntIndex(index);
        return p == null || p->free != 0 ? null : p;
    }

    private static bool IsValid(edict_t* ent) => ent != null && ent->free == 0 && ent->pvPrivateData != null;

    private static bool IsConnected(int id)
    {
        if (id < 1 || id > 32) return false;
        var p = ModuleRuntime.g_engfuncs.pfnPEntityOfEntIndex(id);
        return p != null && p->free == 0 && p->v.netname.data != 0;
    }

    private static bool IsBot(int id)
    {
        var p = ModuleRuntime.g_engfuncs.pfnPEntityOfEntIndex(id);
        return p != null && (p->v.flags & (1 << 13) /*FL_FAKECLIENT*/) != 0;
    }

    private static edict_t* GetPDataCbase(edict_t* ent, int offset)
    {
        var item = ((void**)ent->pvPrivateData)[offset];
        if (item == null) return null;
        var pev = *(entvars_t**)((byte*)item + 4);
        return pev == null ? null : pev->pContainingEntity;
    }

    private static int IndexOf(edict_t* ent) => ent == null ? 0 : ModuleRuntime.g_engfuncs.pfnIndexOfEdict(ent);

    // ---- 生命周期 ----

    [AmxxCallback("plugin_init")]
    public static void OnPluginInit()
    {
        // 取消所有玩家的购买(由 ZP 道具接管)
        for (int id = 1; id < MaxPlayers; id++)
            Wpn.SetParamInt(0, id, WpnConst.SetBuy, WpnConst.BuyCannot);

        // 注册两个购买子弹的道具
        AmmoItemId[0] = Zp.RegisterExtraItem("主武器弹药", AmmoCost1, Zp.ZpTeamHuman);
        AmmoItemId[1] = Zp.RegisterExtraItem("副武器弹药", AmmoCost2, Zp.ZpTeamHuman);

        // 将每把武器都注册为ZP道具，购买命令为empty的除外
        int amount = Wpn.GetParamInt(0, 0, WpnConst.Amount);
        for (int i = 1; i <= amount; i++)
        {
            Wpn.GetParamString(i, 1, WpnConst.Command, out var weaponCmd, 63);
            if (weaponCmd == "empty" || weaponCmd.Length == 0)
                continue;

            Wpn.GetParamString(i, 1, WpnConst.User1, out var weaponName, 63);
            int cost = Wpn.GetParamInt(i, 1, WpnConst.Cost);
            ItemId[i] = Zp.RegisterExtraItem(weaponName, cost, Zp.ZpTeamHuman);
        }
    }

    // 武器名字储存在wpn_szUser1里
    [AmxxCallback("WPN_LoadFiles")]
    public static void OnLoadFiles(int slot, int weaponIndex, string key, string value)
    {
        if (key == "武器名字")
        {
            for (int id = 1; id < MaxPlayers; id++)
                Wpn.SetParamString(weaponIndex, id, WpnConst.User1, value);
        }
    }

    // 打断BOT的购买
    [AmxxCallback("WPN_BotBuyWeapon")]
    public static int OnBotBuyWeapon(int id) => WpnConst.WpnSupercede;

    // debug处理，BOT如果使用弹药袋购买原版武器会导致游戏崩溃
    [AmxxHook("pfnPlayerPostThink", Post = true)]
    public static void PlayerPostThinkPost(edict_t* pEntity)
    {
        int id = IndexOf(pEntity);
        if (!IsConnected(id)) return;
        if (!IsBot(id)) return;

        int ammoPacks = Zp.GetUserAmmoPacks(id);
        if (ammoPacks > 0) Zp.SetUserAmmoPacks(id, 0);
    }

    [AmxxCallback("zp_extra_item_selected")]
    public static int OnExtraItemSelected(int id, int itemId)
    {
        var pPlayer = EntOfIndex(id);

        if (itemId == AmmoItemId[0])
        {
            var entity = pPlayer == null ? null : GetPDataCbase(pPlayer, 368);
            if (IsValid(entity))
            {
                int i = Core.WeaponTypeByIndex(IndexOf(entity));
                Wpn.GiveAmmo(id, IndexOf(entity), i, 1);
            }
            else return 1 /*PLUGIN_HANDLED*/;
        }
        else if (itemId == AmmoItemId[1])
        {
            var entity = pPlayer == null ? null : GetPDataCbase(pPlayer, 369);
            if (IsValid(entity))
            {
                int i = Core.WeaponTypeByIndex(IndexOf(entity));
                Wpn.GiveAmmo(id, IndexOf(entity), i, 1);
            }
            else return 1 /*PLUGIN_HANDLED*/;
        }
        else
        {
            int amount = Wpn.GetParamInt(0, 0, WpnConst.Amount);
            for (int i = 1; i <= amount; i++)
            {
                if (ItemId[i] != itemId) continue;

                int entityIndex = Wpn.GiveWeapon(id, i);
                Wpn.GiveAmmo(id, entityIndex, i, 1);
            }
        }
        return 0 /*PLUGIN_CONTINUE*/;
    }

    // 原版 GetWeaponType 的本地实现(读武器实体 11 号单元的模板序号)
    private static class Core
    {
        public static int WeaponTypeByIndex(int index)
        {
            if (index <= 0) return 0;
            var entity = EntOfIndex(index);
            if (entity == null) return 0;
            int serial = ((int*)entity->pvPrivateData)[11];
            return serial >= 1 ? serial : 0;
        }
    }

    // 常量子集(wpn_const.inc)
    internal static class WpnConst
    {
        public const int WpnSupercede = 1;
        public const int SetBuy = 100;
        public const int Amount = 96;
        public const int Cost = 51;
        public const int User1 = 92;
        public const int Command = 50;
        public const int BuyCannot = 1;
    }

    // ---- native 导入 ----
}

// WeaponTemplate 核心模块的 WPN_* native 导入
internal static partial class Wpn
{
    [AmxxImportNative("WPN_SetParamInt")]
    public static partial void SetParamInt(int weaponIndex, int id, int type, int value);

    [AmxxImportNative("WPN_GetParamInt")]
    public static partial int GetParamInt(int weaponIndex, int id, int type);

    [AmxxImportNative("WPN_GetParamString")]
    public static partial void GetParamString(int weaponIndex, int id, int type, out string value, int len);

    [AmxxImportNative("WPN_SetParamString")]
    public static partial void SetParamString(int weaponIndex, int id, int type, string value);

    [AmxxImportNative("WPN_GiveWeapon")]
    public static partial int GiveWeapon(int id, int weaponIndex);

    [AmxxImportNative("WPN_GiveAmmo")]
    public static partial void GiveAmmo(int id, int entity, int weaponIndex, int full);
}

// Zombie Plague 4.3 模块的 native 导入(要求 modules.ini 启用 zombieplague)
internal static partial class Zp
{
    public const int ZpTeamHuman = 0; // ZP_TEAM_HUMAN(zp_extra_item 的队伍位掩码)

    [AmxxImportNative("zp_register_extra_item")]
    public static partial int RegisterExtraItem(string name, int cost, int team);

    [AmxxImportNative("zp_get_user_ammo_packs")]
    public static partial int GetUserAmmoPacks(int id);

    [AmxxImportNative("zp_set_user_ammo_packs")]
    public static partial void SetUserAmmoPacks(int id, int amount);
}
