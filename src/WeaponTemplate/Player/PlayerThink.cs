using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class PlayerThink
{
    // ---- fm_PlayerPreThink(行走速度) ----

    [AmxxHook("pfnPlayerPreThink")]
    public static void PlayerPreThink(edict_t* pEntity)
    {
        HookResult.Ignore();
        PlayerPreThinkLogic(pEntity);
    }

    private static void PlayerPreThinkLogic(edict_t* pEntity)
    {
        int id = Engine.IndexOf(pEntity);
        if (!Engine.IsAlive(id)) return;

        if (!WeaponStore.RoundStart) return;

        var pPlayer = Engine.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, 373);
        if (!Engine.IsValid(activeItem)) return;

        int i = WeaponStore.GetWeaponType(activeItem);
        int result = NativeFunctionRegister.Execute_WpnWeaponMaxSpeed(Engine.IndexOf(activeItem), id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (i == 0) return;

        if (PlayerStocks.GetUserZoom(id) == 1 && WeaponStore.Player(id).SightMode != WpnConst.Open)
        {
            if (WeaponStore.Weapon(id, i).MaxSpeed != 0.0f)
            {
                pPlayer->v.maxspeed = WeaponStore.Weapon(id, i).MaxSpeed;
                return;
            }
        }
        else if (PlayerStocks.GetUserZoom(id) != 1 || WeaponStore.Player(id).SightMode == WpnConst.Open)
        {
            if (WeaponStore.Weapon(id, i).AimMaxSpeed != 0.0f)
            {
                pPlayer->v.maxspeed = WeaponStore.Weapon(id, i).AimMaxSpeed;
                return;
            }
        }
        HamSandwich.ExecuteHamGetMaxSpeed(EngineConst.HamCsItemGetMaxSpeed, Engine.IndexOf(activeItem), out float maxSpeed);
        pPlayer->v.maxspeed = maxSpeed;
    }

    // ---- fm_PlayerPostThink_Post(BOT Ham 注册/购买 + 刀子延迟攻击入口) ----

    [AmxxHook("pfnPlayerPostThink", Post = true)]
    public static void PlayerPostThinkPost(edict_t* pEntity)
    {
        HookResult.Ignore();
        PlayerPostThinkLogic(pEntity);
    }

    private static void PlayerPostThinkLogic(edict_t* pEntity)
    {
        HookResult.Ignore();
        int id = Engine.IndexOf(pEntity);
        if (!Engine.IsConnected(id)) return;

        if (Engine.IsBot(id) && WeaponStore.BotForwardRegister)
        {
            WeaponStore.BotForwardRegister = false;
            HamSandwich.RegisterHamFromEntity(EngineConst.HamTakeDamage, id, "wt_TakeDamage", 0);
            HamSandwich.RegisterHamFromEntity(EngineConst.HamTakeDamage, id, "wt_TakeDamagePost", 1);
            HamSandwich.RegisterHamFromEntity(EngineConst.HamKilled, id, "wt_KilledPost", 1);
            HamSandwich.RegisterHamFromEntity(EngineConst.HamSpawn, id, "wt_PlayerSpawnPost", 1);
        }

        // debug
        if (WeaponStore.Player(id).BuyingAmmo) WeaponStore.Player(id).BuyingAmmo = false;

        if (!Engine.IsAlive(id)) return;

        if (Engine.GameTime >= WeaponStore.Player(id).BotNextThink && WeaponStore.Player(id).BotNextThink != 0.0f)
        {
            WeaponStore.Player(id).BotNextThink = 0.0f;
            Buy.BotBuyWeaponEvent(id);
        }

        // 刀子延迟二段攻击:状态机与判定在 Weapon/Attack.ProcessDelayedKnife
        Attack.ProcessDelayedKnife(id);
    }

}
