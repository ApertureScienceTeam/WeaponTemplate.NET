using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

public static unsafe partial class Hooks
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
        int id = Eng.IndexOf(pEntity);
        if (!Eng.IsAlive(id)) return;

        if (!WeaponStore.RoundStart) return;

        var pPlayer = Eng.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(activeItem)) return;

        int i = WeaponStore.GetWeaponType(activeItem);
        int result = NativeFunctionRegister.Execute_WpnWeaponMaxSpeed(Eng.IndexOf(activeItem), id, i);
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
        HamSandwich.ExecuteHamGetMaxSpeed(EngineConst.HamCsItemGetMaxSpeed, Eng.IndexOf(activeItem), out float maxSpeed);
        pPlayer->v.maxspeed = maxSpeed;
    }

    // ---- fm_PlayerPostThink_Post(刀子延迟攻击 + BOT 注册/购买) ----

    [AmxxHook("pfnPlayerPostThink", Post = true)]
    public static void PlayerPostThinkPost(edict_t* pEntity)
    {
        HookResult.Ignore();
        PlayerPostThinkLogic(pEntity);
    }

    private static void PlayerPostThinkLogic(edict_t* pEntity)
    {
        HookResult.Ignore();
        int id = Eng.IndexOf(pEntity);
        if (!Eng.IsConnected(id)) return;

        if (Eng.IsBot(id) && WeaponStore.BotForwardRegister)
        {
            WeaponStore.BotForwardRegister = false;
            HamSandwich.RegisterHamFromEntity(EngineConst.HamTakeDamage, id, "wt_TakeDamage", 0);
            HamSandwich.RegisterHamFromEntity(EngineConst.HamTakeDamage, id, "wt_TakeDamagePost", 1);
            HamSandwich.RegisterHamFromEntity(EngineConst.HamKilled, id, "wt_KilledPost", 1);
            HamSandwich.RegisterHamFromEntity(EngineConst.HamSpawn, id, "wt_PlayerSpawnPost", 1);
        }

        // debug
        if (WeaponStore.Player(id).BuyingAmmo) WeaponStore.Player(id).BuyingAmmo = false;

        if (!Eng.IsAlive(id)) return;

        if (Eng.GameTime >= WeaponStore.Player(id).BotNextThink && WeaponStore.Player(id).BotNextThink != 0.0f)
        {
            WeaponStore.Player(id).BotNextThink = 0.0f;
            Buy.BotBuyWeaponEvent(id);
        }

        if (WeaponStore.Player(id).PlayerThink != 0.0f && Eng.GameTime - WeaponStore.Player(id).PlayerThink >= 0.0f)
        {
            var pPlayer = Eng.EntOfIndex(id);
            var knifeItem = pPlayer == null ? null : Eng.GetPDataCbase(pPlayer, 370);
            WeaponStore.Player(id).PlayerThink = 0.0f;
            if (!Eng.IsValid(knifeItem)) return;

            int i = WeaponStore.GetWeaponType(knifeItem);
            if (WeaponStore.Player(id).KnifeAttack == 1)
            {
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] != 0)
                {
                    WeaponStore.Player(id).KnifeAttack = 2;
                    Attack.KnifeAttack(id, i, WeaponStore.Player(id).KnifeAttack, knifeItem);
                    return;
                }
                else
                {
                    int result = NativeFunctionRegister.Execute_WpnPrimaryAttackEnd(Eng.IndexOf(knifeItem), id, i);
                    if (result == WpnConst.WPN_SUPERCEDE) return;

                    WeaponStore.Player(id).KnifeAttack = 2;
                    WeaponStore.Player(id).PlayerThink = Eng.GameTime + WeaponStore.Weapon(id, i).KnifeSlashResetTime;
                    float range = WeaponStore.Weapon(id, i).KnifeSlashRange;
                    float damage = 15.0f * WeaponStore.Weapon(id, i).Damage;
                    float angle = WeaponStore.Weapon(id, i).KnifeSlashAngle;
                    Span<float> origin = stackalloc float[3];
                    origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;
                    Span<float> param = stackalloc float[6];
                    param[0] = range;
                    param[1] = angle;
                    param[2] = WeaponStore.Weapon(id, i).KnifeSlashAngleOffset;
                    param[3] = WeaponStore.Weapon(id, i).KnifeSlashHeight;
                    param[4] = WeaponStore.Weapon(id, i).KnifeSlashHeightOffset;
                    param[5] = damage;
                    int hit = Attack.CheckAttack(id, 0, knifeItem, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
                    if (hit == WpnConst.HitEmpty)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitPlayer)
                    {
                        if (WeaponStore.Player(id).KnifeSlashMode != 0)
                            Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound5, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[3], 0, EngineConst.PitchNorm);
                        else
                            Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound6, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[4], 0, EngineConst.PitchNorm);
                    }
                    if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                    NativeFunctionRegister.Execute_WpnPrimaryAttackEndPost(Eng.IndexOf(knifeItem), id, i);
                    return;
                }
            }
            else if (WeaponStore.Player(id).KnifeAttack == 2)
            {
                WeaponStore.Player(id).KnifeAttack = 0;
                return;
            }
            else if (WeaponStore.Player(id).KnifeAttack == 3)
            {
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin] != 0)
                {
                    WeaponStore.Player(id).KnifeAttack = 4;
                    Attack.KnifeAttack(id, i, WeaponStore.Player(id).KnifeAttack, knifeItem);
                    return;
                }
                else
                {
                    int result = NativeFunctionRegister.Execute_WpnSecondaryAttackEnd(Eng.IndexOf(knifeItem), id, i);
                    if (result == WpnConst.WPN_SUPERCEDE) return;

                    WeaponStore.Player(id).KnifeAttack = 4;
                    WeaponStore.Player(id).PlayerThink = Eng.GameTime + WeaponStore.Weapon(id, i).KnifeStabResetTime;
                    float range = WeaponStore.Weapon(id, i).KnifeStabRange;
                    float damage = 65.0f * WeaponStore.Weapon(id, i).Damage2;
                    float angle = WeaponStore.Weapon(id, i).KnifeStabAngle;
                    Span<float> origin = stackalloc float[3];
                    origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;
                    Span<float> param = stackalloc float[6];
                    param[0] = range;
                    param[1] = angle;
                    param[2] = WeaponStore.Weapon(id, i).KnifeStabAngleOffset;
                    param[3] = WeaponStore.Weapon(id, i).KnifeStabHeight;
                    param[4] = WeaponStore.Weapon(id, i).KnifeStabHeightOffset;
                    param[5] = damage;
                    int hit = Attack.CheckAttack(id, 0, knifeItem, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
                    if (hit == WpnConst.HitEmpty)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitPlayer)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[2], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
                        Eng.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                    NativeFunctionRegister.Execute_WpnSecondaryAttackEndPost(Eng.IndexOf(knifeItem), id, i);
                    return;
                }
            }
            else if (WeaponStore.Player(id).KnifeAttack == 4)
            {
                WeaponStore.Player(id).KnifeAttack = 0;
                return;
            }
        }
    }

    // ---- fm_Think_Post(枪口烟雾动画) ----

    [AmxxHook("pfnThink", Post = true)]
    public static void ThinkPost(edict_t* pEntity)
    {
        HookResult.Ignore();
        if (!Eng.IsValid(pEntity)) return;

        string className = Eng.ClassName(pEntity);
        if (className != "gunsmoke1") return;

        var pev = &pEntity->v;
        float frame = pev->frame;
        float scale = pev->scale;
        int num = pev->iuser1;
        float velocityX = pev->velocity.x;
        float velocityY = pev->velocity.y;
        float nextThink = 0.005f;
        frame += 0.2f;
        scale += 0.01f;
        if (num != 0)
        {
            velocityX -= Eng.RandomFloat(1.0f, 5.0f);
            velocityY -= Eng.RandomFloat(1.0f, 5.0f);
        }
        else
        {
            velocityX += Eng.RandomFloat(1.0f, 5.0f);
            velocityY += Eng.RandomFloat(1.0f, 5.0f);
        }
        frame = MathF.Min(10.0f, frame);
        scale = MathF.Min(1.0f, scale);
        pev->frame = frame;
        pev->scale = scale;
        pev->velocity.x = velocityX;
        pev->velocity.y = velocityY;
        pev->nextthink = Eng.GameTime + nextThink;
        if (frame == 10.0f) Eng.RemoveEntity(pEntity);
    }
}
