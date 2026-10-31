using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// RegisterHam 武器状态循环回调:每帧处理、装填、待机与弹药补给。
public static unsafe partial class WeaponAmmo
{
    [AmxxCallback("wt_ItemPostFrame")]
    public static int OnItemPostFrame(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Engine.IsAlive(id)) return EngineConst.HamIgnored;

        if (i == 0)
        {
            // 记录原版武器的换弹
            int weapon = Engine.GetPDataInt(entity, CsOffsets.ItemId);
            const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            if (((1 << weapon) & specialBitSum) == 0)
            {
                if (weapon != EngineConst.CswM3 && weapon != EngineConst.CswXm1014)
                {
                    if (Engine.GetPDataInt(entity, CsOffsets.ItemInReload) != 0 && Engine.GetPDataFloat(id, CsOffsets.NextAttack) <= 0.0f)
                        WeaponStore.Player(id).GameBpAmmo[weapon] = PlayerStocks.GetUserBpAmmo(id, weapon);
                }
                else if (Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload) == 1 && Engine.GetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle) <= 0.0f)
                    WeaponStore.Player(id).GameBpAmmo[weapon] = PlayerStocks.GetUserBpAmmo(id, weapon);
            }
            return EngineConst.HamIgnored;
        }

        int forwardResult = NativeFunctionRegister.Execute_WpnWeaponPostFrame(entityIndex, id, i);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return EngineConst.HamIgnored;
        if (WeaponStore.Weapon(id, i).Clip == 0) return EngineConst.HamIgnored;

        if (!WeaponStore.Player(id).Reloading)
        {
            int clipExtra = WeaponStore.Weapon(id, i).Clip;
            float flNextAttack = Engine.GetPDataFloat(id, CsOffsets.NextAttack);
            int bpAmmo = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
            int clip = Engine.GetPDataInt(entity, CsOffsets.ItemClip);
            int inReload = Engine.GetPDataInt(entity, CsOffsets.ItemInReload);
            if (inReload != 0 && flNextAttack <= 0.0f && WeaponStore.Weapon(id, i).ReloadMode == 0)
            {
                int j = Math.Min(clipExtra - clip, bpAmmo);
                Engine.SetPDataInt(entity, CsOffsets.ItemClip, clip + j);
                PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id, bpAmmo - j);
                WeaponStore.Player(id).BPAmmo[i] = bpAmmo - j;
                Engine.SetPDataInt(entity, CsOffsets.ItemInReload, 0);
                inReload = 0;
            }
            if (WeaponStore.Weapon(id, i).ReloadMode != 0)
            {
                int button = pPlayer->v.button;
                if ((button & EngineConst.InAttack) != 0 && Engine.GetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack) <= 0.0f && Engine.GetPDataInt(entity, CsOffsets.ItemClip) != 0)
                    return EngineConst.HamIgnored;

                if (Engine.GetPDataInt(entity, CsOffsets.ItemClip) == 0 && PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id) != 0)
                {
                    pPlayer->v.button = button & ~EngineConst.InAttack;
                    if (Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload) == 0 && Engine.GetPDataInt(entity, CsOffsets.ItemInReload) == 0)
                    {
                        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                        {
                            if (WeaponStore.Player(id).SightMode != WpnConst.Open) return EngineConst.HamIgnored;

                            WeaponStore.Player(id).Reloading = true;
                            Attack.BeginClosing(entity);
                            return EngineConst.HamIgnored;
                        }
                    }
                }
                int tempClip = 0;
                if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM3) tempClip = 8;
                else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswXm1014) tempClip = 7;
                if ((button & EngineConst.InReload) != 0 && inReload == 0 && tempClip != 0)
                {
                    if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                    {
                        if (WeaponStore.Player(id).SightMode != WpnConst.Open) return EngineConst.HamIgnored;

                        WeaponStore.Player(id).Reloading = true;
                        Attack.BeginClosing(entity);
                        return EngineConst.HamIgnored;
                    }
                    if (clip >= clipExtra) pPlayer->v.button = button & ~EngineConst.InReload;
                    else if (clip == tempClip)
                    {
                        if (bpAmmo == 0) return EngineConst.HamIgnored;

                        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed) return EngineConst.HamIgnored;

                        // 子弹数和原版一样时，按R键装一发子弹，然后触发idle的装弹
                        WeaponAmmo.ShotgunReload(entity, clipExtra, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);
                    }
                }
            }
        }
        if (WeaponStore.Weapon(id, i).Sight != 0)
        {
            if (Engine.GetPDataInt(entity, CsOffsets.ItemInReload) != 0 || Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload) != 0) return EngineConst.HamIgnored;

            if (Engine.GetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack) > 0.0f)
            {
                float curTime = Engine.GP->time;
                if (curTime <= WeaponStore.Player(id).NextThink) return EngineConst.HamIgnored;

                if (WeaponStore.Weapon(id, i).SightFrameRate <= 0.0f || WeaponStore.Weapon(id, i).SightDistance <= 0.0f) return EngineConst.HamIgnored;

                float valve = WeaponStore.Weapon(id, i).SightFrameRate;
                if (valve < 1.0f) valve = 1.0f;
                if (WeaponStore.Player(id).SightMode == WpnConst.AtoB)
                {
                    Engine.SetPDataInt(pPlayer, CsOffsets.Zoom, Math.Max(Engine.GetPDataInt(pPlayer, CsOffsets.Zoom) - FloatMath.Round(valve), FloatMath.Round(WeaponStore.Weapon(id, i).SightDistance)));
                    WeaponStore.Player(id).NextThink = curTime + WeaponStore.Weapon(id, i).SightOpenTime / ((90.0f - WeaponStore.Weapon(id, i).SightDistance) / WeaponStore.Weapon(id, i).SightFrameRate);
                }
                else if (WeaponStore.Player(id).SightMode == WpnConst.BtoA)
                {
                    Engine.SetPDataInt(pPlayer, CsOffsets.Zoom, Math.Min(Engine.GetPDataInt(pPlayer, CsOffsets.Zoom) + FloatMath.Round(valve), 90));
                    WeaponStore.Player(id).NextThink = curTime + WeaponStore.Weapon(id, i).SightCloseTime / ((90.0f - WeaponStore.Weapon(id, i).SightDistance) / WeaponStore.Weapon(id, i).SightFrameRate);
                }
                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Player(id).SightMode == WpnConst.AtoB)
            {
                Attack.OpeningEnd(entity);
                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Player(id).SightMode == WpnConst.BtoA)
            {
                Attack.ClosingEnd(entity);
                return EngineConst.HamIgnored;
            }
            int button = pPlayer->v.button;
            if ((button & EngineConst.InAttack2) == 0)
            {
                WeaponStore.Player(id).OldButton = false;
                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Player(id).OldButton) return EngineConst.HamSupercede;

            WeaponStore.Player(id).OldButton = true;
            if ((button & EngineConst.InAttack) != 0) return EngineConst.HamSupercede;

            if (Engine.GetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack) > 0.0f) return EngineConst.HamSupercede;

            if (WeaponStore.Player(id).SightMode == WpnConst.Closed)
            {
                Attack.BeginOpening(entity);
                return EngineConst.HamSupercede;
            }
            Attack.BeginClosing(entity);
            return EngineConst.HamSupercede;
        }
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponReload")]
    public static int OnWeaponReload(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Engine.IsAlive(id)) return EngineConst.HamIgnored;
        if (i == 0) return EngineConst.HamIgnored;
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return EngineConst.HamIgnored;
        if (WeaponStore.Weapon(id, i).Clip == 0) return EngineConst.HamSupercede;

        int result = NativeFunctionRegister.Execute_WpnWeaponReload(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed)
        {
            if (WeaponStore.Player(id).SightMode != WpnConst.Open) return EngineConst.HamSupercede;

            WeaponStore.Player(id).Reloading = true;
            Attack.BeginClosing(entity);
            return EngineConst.HamSupercede;
        }
        int bpAmmo = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
        int clip = Engine.GetPDataInt(entity, CsOffsets.ItemClip);
        int inSpecialReload = Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload);

        // 在ShotgunReload里判断并设置换弹状态，fInSpecReload为2时忽略，使用原版的装填子弹
        if (WeaponStore.Weapon(id, i).ReloadMode != 0)
        {
            if (inSpecialReload < 2 && (WeaponStore.Weapon(id, i).Id == EngineConst.CswM3 || WeaponStore.Weapon(id, i).Id == EngineConst.CswXm1014))
                WeaponAmmo.ShotgunReload(entity, WeaponStore.Weapon(id, i).Clip, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);

            if (WeaponStore.Weapon(id, i).Id != EngineConst.CswM3 && WeaponStore.Weapon(id, i).Id != EngineConst.CswXm1014)
            {
                WeaponAmmo.ShotgunReload(entity, WeaponStore.Weapon(id, i).Clip, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);
                return EngineConst.HamSupercede;
            }
        }
        int clipExtra = WeaponStore.Weapon(id, i).Clip;
        WeaponStore.Player(id).TmpClip[i] = -1;
        if (bpAmmo <= 0) return EngineConst.HamSupercede;

        if (clip >= clipExtra) return EngineConst.HamSupercede;

        WeaponStore.Player(id).TmpClip[i] = clip;
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_WeaponReloadPost")]
    public static void OnWeaponReloadPost(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Engine.IsAlive(id)) return;
        if (i == 0) return;
        if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) return;

        int result = NativeFunctionRegister.Execute_WpnWeaponReloadPost(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).Reloading) return;
        if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Closed) return;

        int inSpecialReload = Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload);
        if (WeaponStore.Weapon(id, i).ReloadMode != 0)
        {
            if (inSpecialReload == 1) WeaponStore.Player(id).BPAmmo[i] = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
            return;
        }
        if (WeaponStore.Weapon(id, i).Clip == 0) return;

        int tmpClip = WeaponStore.Player(id).TmpClip[i];
        if (tmpClip == -1) return;

        Engine.SetPDataInt(entity, CsOffsets.ItemClip, tmpClip);
        Engine.SetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle, WeaponStore.Weapon(id, i).ReloadTime[0]);
        Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).ReloadTime[0]);
        Engine.SetPDataInt(entity, CsOffsets.ItemInReload, 1);
        if ((Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 2)) != 0 || (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 0)) != 0)
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimReloadSilenced]);
        else
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimReload]);
    }

    [AmxxCallback("wt_WeaponWeaponIdle")]
    public static int OnWeaponWeaponIdle(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;
        var pPlayer = Engine.GetPDataCbase(entity, CsOffsets.ItemOwner);
        if (pPlayer == null) return EngineConst.HamIgnored;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (!Engine.IsAlive(id)) return EngineConst.HamIgnored;
        if (i == 0) return EngineConst.HamIgnored;

        int result = NativeFunctionRegister.Execute_WpnWeaponIdle(entityIndex, id, i);
        if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        if (WeaponStore.Weapon(id, i).ReloadMode != 0)
        {
            if (Engine.GetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle) > 0.0f) return EngineConst.HamIgnored;

            if (WeaponStore.Player(id).Reloading) return EngineConst.HamIgnored;

            int bpAmmo = PlayerStocks.GetUserBpAmmo(id, WeaponStore.Weapon(id, i).Id);
            int clip = Engine.GetPDataInt(entity, CsOffsets.ItemClip);
            int inSpecialReload = Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload);
            if (clip == 0 && inSpecialReload == 0)
            {
                if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Open && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                    return EngineConst.HamSupercede;

                return EngineConst.HamIgnored;
            }
            if (WeaponStore.Weapon(id, i).Sight != 0 && WeaponStore.Player(id).SightMode != WpnConst.Open && WeaponStore.Player(id).SightMode != WpnConst.Closed)
                return EngineConst.HamSupercede;

            int tempClip = 0;
            if (WeaponStore.Weapon(id, i).Id == EngineConst.CswM3) tempClip = 8;
            else if (WeaponStore.Weapon(id, i).Id == EngineConst.CswXm1014) tempClip = 7;
            if (inSpecialReload != 0 && tempClip != 0)
            {
                // 弹夹数和原版一样时继续换弹
                if (clip < WeaponStore.Weapon(id, i).Clip && clip == tempClip && bpAmmo != 0)
                    WeaponAmmo.ShotgunReload(entity, WeaponStore.Weapon(id, i).Clip, clip, bpAmmo, id, WeaponStore.Weapon(id, i).Id);

                // 换弹结束
                else if (clip == WeaponStore.Weapon(id, i).Clip)
                {
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAfterReload]);
                    Engine.SetPDataInt(entity, CsOffsets.ItemInSpecialReload, 0);
                    Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Engine.SetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Engine.SetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).ReloadTime[2]);
                }
            }
            else if (inSpecialReload != 0 && tempClip == 0)
            {
                if (clip < WeaponStore.Weapon(id, i).Clip && bpAmmo != 0)
                    HamSandwich.ExecuteHamB(EngineConst.HamWeaponReload, entityIndex);

                else if (clip == WeaponStore.Weapon(id, i).Clip || bpAmmo == 0)
                {
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimAfterReload]);
                    Engine.SetPDataInt(entity, CsOffsets.ItemInSpecialReload, 0);
                    Engine.SetPDataFloat(entity, CsOffsets.ItemNextPrimaryAttack, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Engine.SetPDataFloat(entity, CsOffsets.ItemNextSecondaryAttack, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Engine.SetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle, WeaponStore.Weapon(id, i).ReloadTime[2]);
                    Engine.SetPDataFloat(id, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).ReloadTime[2]);
                }
                return EngineConst.HamSupercede;
            }
        }
        if (Engine.GetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle) <= 0.0f && WeaponStore.Weapon(id, i).Id != EngineConst.CswHegrenade && Engine.GetPDataInt(entity, CsOffsets.ItemInSpecialReload) == 0 && Engine.GetPDataInt(entity, CsOffsets.ItemInReload) == 0)
        {
            if (WeaponStore.Weapon(id, i).Id == EngineConst.CswKnife) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimIdle]);
            else
            {
                if ((Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 2)) != 0 || (Engine.GetPDataInt(entity, CsOffsets.ItemWeaponState) & (1 << 0)) != 0)
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdleSilenced]);
                else
                    PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdle]);
            }
            Engine.SetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle, 20.0f);
            return EngineConst.HamSupercede;
        }
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_GiveAmmo")]
    public static int OnGiveAmmo(int id, int amount, string ammoName, int max)
    {
        var pPlayer = Engine.EntOfIndex(id);
        var activeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        if (!Engine.IsValid(activeItem)) return EngineConst.HamIgnored;

        if (WeaponStore.Player(id).BuyingAmmo) return EngineConst.HamIgnored;

        ammoName = ammoName.ToLowerInvariant();
        if (ammoName is "hegrenade" or "flashbang" or "smokegrenade" or "c4") return EngineConst.HamIgnored;

        int i = WeaponStore.GetWeaponType(activeItem);
        int weapon = Engine.GetPDataInt(activeItem, CsOffsets.ItemId);
        const int specialBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
        const int secWeaponBitSum = (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) | (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);

        if (GameTables.GameWeaponAmmoType[weapon] == ammoName)
        {
            if (((1 << weapon) & secWeaponBitSum) == 0 && ((1 << weapon) & specialBitSum) == 0)
                WeaponAmmo.GivePriAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
            else if (((1 << weapon) & secWeaponBitSum) != 0)
                WeaponAmmo.GiveSecAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
        }
        else
        {
            if (Engine.GetPDataCbase(pPlayer, CsOffsets.PrimaryWeapon) == activeItem) activeItem = Engine.GetPDataCbase(pPlayer, CsOffsets.SecondaryWeapon);
            else if (Engine.GetPDataCbase(pPlayer, CsOffsets.SecondaryWeapon) == activeItem) activeItem = Engine.GetPDataCbase(pPlayer, CsOffsets.PrimaryWeapon);
            else if (Engine.GetPDataCbase(pPlayer, CsOffsets.PrimaryWeapon) != activeItem && Engine.GetPDataCbase(pPlayer, CsOffsets.SecondaryWeapon) != activeItem)
            {
                var item1 = Engine.GetPDataCbase(pPlayer, CsOffsets.SecondaryWeapon);
                var item2 = Engine.GetPDataCbase(pPlayer, CsOffsets.PrimaryWeapon);
                if (Engine.IsValid(item1) && GameTables.GameWeaponAmmoType[Engine.GetPDataInt(item1, CsOffsets.ItemId)] == ammoName) activeItem = item1;
                if (Engine.IsValid(item2) && GameTables.GameWeaponAmmoType[Engine.GetPDataInt(item2, CsOffsets.ItemId)] == ammoName) activeItem = item2;
            }
            if (!Engine.IsValid(activeItem)) return EngineConst.HamSupercede;

            i = WeaponStore.GetWeaponType(activeItem);
            weapon = Engine.GetPDataInt(activeItem, CsOffsets.ItemId);
            if (GameTables.GameWeaponAmmoType[weapon] == ammoName)
            {
                if (((1 << weapon) & secWeaponBitSum) == 0)
                    WeaponAmmo.GivePriAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
                else
                    WeaponAmmo.GiveSecAmmo(id, activeItem, i, i != 0 ? (WeaponStore.Weapon(id, i).Clip != 0 ? 0 : 1) : 0, 1, amount);
            }
        }
        return EngineConst.HamSupercede;
    }

}
