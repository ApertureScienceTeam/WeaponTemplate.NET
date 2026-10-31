using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// Zombie Plague 4.3 Pawn 插件提供的 native 导入(调用体由源生成器补全)。
internal static partial class Zp
{
    public const int ZpTeamHuman = 1 << 1; // ZP_TEAM_HUMAN(zp_extra_item 的队伍位掩码)
    public const int PluginHandled = 97; // ZP_PLUGIN_HANDLED:取消购买并退还弹药包。

    [AmxxImportNative("zp_register_extra_item")]
    public static partial int RegisterExtraItem(string name, int cost, int team);

    [AmxxImportNative("zp_get_user_ammo_packs")]
    public static partial int GetUserAmmoPacks(int id);

    [AmxxImportNative("zp_set_user_ammo_packs")]
    public static partial void SetUserAmmoPacks(int id, int amount);
}
