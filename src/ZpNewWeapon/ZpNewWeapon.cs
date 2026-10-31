using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// zp_newweapon.sma v1.0 的 C# 移植:加枪模板 & ZP4.3 兼容插件。
// 依赖:WeaponTemplate 核心模块与提供 zp_* native 的 Zombie Plague 4.3 Pawn 插件。
// 原版说明:武器名字储存在 wpn_szUser1 里;购买命令为 empty 的武器不注册为 ZP 道具。
public static unsafe partial class ZpNewWeapon
{
    private const int AmmoCost1 = 100;
    private const int AmmoCost2 = 100;
    private const int MaxPlayers = 33;

    // 购买子弹的道具id / 购买武器的道具id
    private static readonly int[] AmmoItemId = new int[2];
    // 模板序号 -> ZP 道具id;购买命令为 empty 等未注册为 ZP 道具的模板没有条目
    private static readonly Dictionary<int, int> ItemId = new();

    static ZpNewWeapon() => ResetAll();

    public static void ResetAll()
    {
        Array.Fill(AmmoItemId, -1);
        ItemId.Clear();
    }

    // ---- 生命周期 ----

    [AmxxCallback("plugin_init")]
    public static void OnPluginInit()
    {
        ResetAll();
        // 取消所有玩家的购买(由 ZP 道具接管)
        for (int id = 1; id < MaxPlayers; id++)
            Wpn.SetParamInt(0, id, WpnConst.WpnSetBuy, WpnConst.BuyCannot);

        // 注册两个购买子弹的道具
        AmmoItemId[0] = Zp.RegisterExtraItem("主武器弹药", AmmoCost1, Zp.ZpTeamHuman);
        AmmoItemId[1] = Zp.RegisterExtraItem("副武器弹药", AmmoCost2, Zp.ZpTeamHuman);

        // 将每把武器都注册为ZP道具，购买命令为empty的除外
        int amount = Wpn.GetParamInt(0, 0, WpnConst.WpnIAmount);
        for (int i = 1; i <= amount; i++)
        {
            Wpn.GetParamString(i, 1, WpnConst.WpnSzCommand, out var weaponCmd, 63);
            if (weaponCmd == "empty" || weaponCmd.Length == 0)
                continue;

            Wpn.GetParamString(i, 1, WpnConst.WpnSzUser1, out var weaponName, 63);
            int cost = Wpn.GetParamInt(i, 1, WpnConst.WpnICost);
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
                Wpn.SetParamString(weaponIndex, id, WpnConst.WpnSzUser1, value);
        }
    }

    // 打断BOT的购买
    [AmxxCallback("WPN_BotBuyWeapon")]
    public static int OnBotBuyWeapon(int id) => WpnConst.WPN_SUPERCEDE;

    // debug处理，BOT如果使用弹药袋购买原版武器会导致游戏崩溃
    [AmxxHook("pfnPlayerPostThink", Post = true)]
    public static void PlayerPostThinkPost(edict_t* pEntity)
    {
        HookResult.Ignore();
        int id = Engine.IndexOf(pEntity);
        if (!Engine.IsConnected(id)) return;
        if (!Engine.IsBot(id)) return;

        int ammoPacks = Zp.GetUserAmmoPacks(id);
        if (ammoPacks > 0) Zp.SetUserAmmoPacks(id, 0);
    }

    [AmxxCallback("zp_extra_item_selected")]
    public static int OnExtraItemSelected(int id, int itemId)
    {
        if (itemId < 0) return 0;
        var pPlayer = Engine.EntOfIndex(id);

        if (itemId == AmmoItemId[0])
        {
            var entity = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.PrimaryWeapon);
            if (Engine.IsValid(entity))
            {
                int i = WeaponTypeByIndex(Engine.IndexOf(entity));
                Wpn.GiveAmmo(id, Engine.IndexOf(entity), i, 1);
            }
            else return Zp.PluginHandled;
        }
        else if (itemId == AmmoItemId[1])
        {
            var entity = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.SecondaryWeapon);
            if (Engine.IsValid(entity))
            {
                int i = WeaponTypeByIndex(Engine.IndexOf(entity));
                Wpn.GiveAmmo(id, Engine.IndexOf(entity), i, 1);
            }
            else return Zp.PluginHandled;
        }
        else
        {
            int amount = Wpn.GetParamInt(0, 0, WpnConst.WpnIAmount);
            for (int i = 1; i <= amount; i++)
            {
                if (ItemId.GetValueOrDefault(i, -1) != itemId) continue;

                int entityIndex = Wpn.GiveWeapon(id, i);
                if (entityIndex <= 0) return Zp.PluginHandled;
                Wpn.GiveAmmo(id, entityIndex, i, 1);
            }
        }
        return 0 /*PLUGIN_CONTINUE*/;
    }

    // 原版 GetWeaponType 的本地实现(读武器实体 11 号单元的模板序号)
    private static int WeaponTypeByIndex(int index)
    {
        if (index <= 0) return 0;
        var entity = Engine.EntOfIndex(index);
        if (!Engine.IsValid(entity)) return 0;
        int serial = Engine.GetPDataInt(entity, CsOffsets.ItemTemplateSerial);
        return serial >= 1 && serial <= Wpn.GetParamInt(0, 0, WpnConst.WpnIAmount) ? serial : 0;
    }
}
