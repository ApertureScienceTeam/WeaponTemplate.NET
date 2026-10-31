using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// 玩家事件:wt_StatusIcon(BOT 进入购买区)与 wt_PlayerSpawnPost(玩家生成)。
public static unsafe partial class PlayerEvents
{
    [AmxxCallback("wt_StatusIcon")]
    public static int OnStatusIcon(int msgId, int msgDest, int msgEntity)
    {
        AmxModX.GetMsgArgString(2, out var statusArg, 32);
        string statusName = statusArg ?? "";

        // 出现buyzone图标时说明BOT在购买区
        if (statusName == "buyzone")
        {
            if (!Engine.IsConnected(msgEntity)) return EngineConst.PluginContinue;
            if (!Engine.IsBot(msgEntity)) return EngineConst.PluginContinue;
            if (!WeaponStore.Player(msgEntity).BotBuyWeapon) return EngineConst.PluginContinue;
            WeaponStore.Player(msgEntity).BotNextThink = Engine.GameTime + 0.1f;
        }
        return EngineConst.PluginContinue;
    }

    // public HAM_PlayerSpawn_Post(id):真人出生时若手持开启机瞄的模板武器则收起机瞄;
    // BOT 出生后进入购买流程(由 Buy 模块接管)。
    [AmxxCallback("wt_PlayerSpawnPost")]
    public static void OnPlayerSpawnPost(int id)
    {
        if (!Engine.IsAlive(id)) return;

        if (!Engine.IsBot(id))
        {
            var pPlayer = Engine.EntOfIndex(id);
            var activeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, 373);
            if (!Engine.IsValid(activeItem)) return;

            int i = WeaponStore.GetWeaponType(activeItem);
            if (i == 0) return;

            if (WeaponStore.Weapon(id, i).Sight == 0) return;

            if (WeaponStore.Player(id).SightMode == WpnConst.Closed) return;

            Attack.ClosingEnd(activeItem);
            return;
        }
        if (WeaponStore.Round == 0) return;

        WeaponStore.Player(id).BotBuyWeapon = true;
        if (WeaponStore.Player(id).BlockBuy == WpnConst.BuyNormally) WeaponStore.Player(id).BotNextThink = 0.0f;
        else if (WeaponStore.Player(id).BlockBuy == WpnConst.BuyEverywhere) WeaponStore.Player(id).BotNextThink = Engine.GameTime + 0.1f;
        else WeaponStore.Player(id).BotBuyWeapon = false;
    }
}
