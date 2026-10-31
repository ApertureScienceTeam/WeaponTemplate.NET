using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// 武器伤害与击杀:Ham 回调(TakeDamage/TakeDamagePost/KilledPost)与手雷引爆(wt_ThinkGrenade)。
public static unsafe partial class WeaponDamage
{
    [AmxxCallback("wt_TakeDamage")]
    public static int OnTakeDamage(int victim, int inflictor, int attacker, float damage, int damageType)
    {
        if (!Engine.IsConnected(attacker) || !Engine.IsConnected(victim)) return EngineConst.HamIgnored;

        int forwardResult = NativeFunctionRegister.Execute_WpnTakeDamage(victim, inflictor, attacker, damage, damageType);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        var pVictim = Engine.EntOfIndex(victim);
        var pAttacker = Engine.EntOfIndex(attacker);
        if (pVictim == null || pAttacker == null) return EngineConst.HamIgnored;

        damage *= WeaponStore.Player(attacker).DamagePoint * WeaponStore.Player(victim).DamageVictimPoint;
        int i = WeaponGive.WeaponTypeByIndex(inflictor);
        if ((damageType & (1 << 24)) != 0)
        {
            if (i == 0)
            {
                HamSandwich.SetHamParamFloat(4, damage);
                return EngineConst.HamIgnored;
            }
            damage *= WeaponStore.Weapon(attacker, i).Damage;
            var pInflictor = Engine.EntOfIndex(inflictor);
            var victimOrigin = new float[] { pVictim->v.origin.x, pVictim->v.origin.y, pVictim->v.origin.z };
            var inflictorOrigin = pInflictor != null
                ? new float[] { pInflictor->v.origin.x, pInflictor->v.origin.y, pInflictor->v.origin.z }
                : new float[] { 0f, 0f, 0f };
            float distance = VectorMath.Distance(victimOrigin, inflictorOrigin);
            damage -= damage * distance / WeaponStore.Weapon(attacker, i).GrenadeData[2];
            if (damage <= 0.0f) damage = 0.0f;
            HamSandwich.SetHamParamFloat(4, damage);
            return EngineConst.HamIgnored;
        }
        if (attacker != inflictor)
        {
            if (i != 0) WeaponStore.Player(attacker).AttackerEntity = inflictor;
            else
            {
                HamSandwich.SetHamParamFloat(4, damage);
                return EngineConst.HamIgnored;
            }
        }
        else
        {
            var activeItem = Engine.GetPDataCbase(pAttacker, CsOffsets.ActiveItem);
            if (activeItem == null) return EngineConst.HamIgnored;

            i = WeaponStore.GetWeaponType(activeItem);
            if (i == 0)
            {
                HamSandwich.SetHamParamFloat(4, damage);
                return EngineConst.HamIgnored;
            }
            WeaponStore.Player(attacker).AttackerEntity = Engine.IndexOf(activeItem);
        }
        if (WeaponStore.Weapon(attacker, i).Id == EngineConst.CswKnife)
        {
            if (WeaponStore.Player(attacker).KnifeTempDamage != 0.0f)
            {
                HamSandwich.SetHamParamFloat(4, damage * WeaponStore.Player(attacker).KnifeTempDamage);
                WeaponStore.Player(attacker).KnifeTempDamage = 0.0f;
            }
            return EngineConst.HamIgnored;
        }
        HamSandwich.SetHamParamFloat(4, damage * WeaponStore.Weapon(attacker, i).Damage);
        return EngineConst.HamIgnored;
    }

    [AmxxCallback("wt_TakeDamagePost")]
    public static void OnTakeDamagePost(int victim, int inflictor, int attacker, float damage, int damageType)
    {
        if (!Engine.IsConnected(attacker) || !Engine.IsConnected(victim)) return;

        var pAttacker = Engine.EntOfIndex(attacker);
        var pVictim = Engine.EntOfIndex(victim);
        if (pAttacker == null || pVictim == null) return;

        if (Engine.GetPDataInt(pAttacker, CsOffsets.Team) == Engine.GetPDataInt(pVictim, CsOffsets.Team)) return;

        if (WeaponStore.Player(attacker).AttackerEntity != 0)
        {
            float health = pVictim->v.health;
            if (health - damage > 0.0f) WeaponStore.Player(attacker).AttackerEntity = 0;
        }
        var activeItem = Engine.GetPDataCbase(pAttacker, CsOffsets.ActiveItem);
        if (activeItem == null) return;

        int i = WeaponStore.GetWeaponType(activeItem);
        int forwardResult = NativeFunctionRegister.Execute_WpnWeaponKnockBack(attacker, victim, inflictor, i, damage);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return;

        if (i == 0) return;

        // 原版 pev_flags & (FL_DUCKING|FL_ONGROUND) == (FL_DUCKING|FL_ONGROUND) 受 Pawn 运算符优先级影响
        // 实际退化为 flags & 1;这里按注释的本意(蹲下且在地面)实现。
        bool ducking = ((pVictim->v.flags & (EngineConst.FlDucking | EngineConst.FlOnground)) == (EngineConst.FlDucking | EngineConst.FlOnground));

        var victimOrigin = new float[] { pVictim->v.origin.x, pVictim->v.origin.y, pVictim->v.origin.z };
        var attackerOrigin = new float[] { pAttacker->v.origin.x, pAttacker->v.origin.y, pAttacker->v.origin.z };
        var velocity = new float[3];
        float victimVelocityZ = pVictim->v.velocity.z;
        float knockBack = ducking
            ? damage * WeaponStore.Weapon(attacker, i).KnockBack * 0.25f * WeaponStore.Player(victim).KnockBackPoint
            : damage * WeaponStore.Weapon(attacker, i).KnockBack * WeaponStore.Player(victim).KnockBackPoint;
        VectorMath.GetVelocityFromOrigin(victimOrigin, attackerOrigin, knockBack, velocity);
        velocity[2] = victimVelocityZ;
        pVictim->v.velocity = new vec3_t { x = velocity[0], y = velocity[1], z = velocity[2] };
    }

    [AmxxCallback("wt_KilledPost")]
    public static void OnKilledPost(int id, int attacker)
    {
        if (!Engine.IsConnected(id)) return;

        // 死亡时剥夺所有子弹
        const int primaryBitSum =
            (1 << EngineConst.CswScout) | (1 << EngineConst.CswXm1014) | (1 << EngineConst.CswMac10) | (1 << EngineConst.CswAug) |
            (1 << EngineConst.CswUmp45) | (1 << EngineConst.CswSg550) | (1 << EngineConst.CswGalil) | (1 << EngineConst.CswFamas) |
            (1 << EngineConst.CswAwp) | (1 << EngineConst.CswMp5Navy) | (1 << EngineConst.CswM249) | (1 << EngineConst.CswM3) |
            (1 << EngineConst.CswM4a1) | (1 << EngineConst.CswTmp) | (1 << EngineConst.CswG3sg1) | (1 << EngineConst.CswSg552) |
            (1 << EngineConst.CswAk47) | (1 << EngineConst.CswP90);
        const int secondaryBitSum =
            (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) |
            (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);
        Array.Clear(WeaponStore.Player(id).BPAmmo);
        for (int i = 0; i < 32; i++) WeaponStore.Player(id).GameBpAmmo[i] = 0;
        for (int i = 1; i < 31; i++)
            if (((1 << i) & (primaryBitSum | secondaryBitSum)) != 0) PlayerStocks.SetUserBpAmmo(id, i, 0);
    }

    [AmxxCallback("wt_ThinkGrenade")]
    public static int OnThinkGrenade(int entityIndex)
    {
        var entity = Engine.EntOfIndex(entityIndex);
        if (entity == null) return EngineConst.HamIgnored;

        float dmgTime = entity->v.dmgtime;
        float curTime = Engine.GameTime;
        if (dmgTime > curTime) return EngineConst.HamIgnored;

        int i = WeaponStore.GetWeaponType(entity);
        if (i == 0) return EngineConst.HamIgnored;

        int ownerId = Engine.IndexOf(entity->v.owner);
        int forwardResult = NativeFunctionRegister.Execute_WpnHookGrenadeExplode(entityIndex, ownerId, i);
        if (forwardResult == WpnConst.WPN_SUPERCEDE) return EngineConst.HamSupercede;

        Span<float> origin = stackalloc float[3];
        origin[0] = entity->v.origin.x;
        origin[1] = entity->v.origin.y;
        origin[2] = entity->v.origin.z;

        Engine.MessageBegin(EngineConst.MsgAll, EngineConst.SvcTempEntity, origin, null);
        Engine.WriteByte(EngineConst.TeExplosion);
        Engine.WriteCoord(origin[0]);
        Engine.WriteCoord(origin[1]);
        Engine.WriteCoord(origin[2] + 45.0f);
        Engine.WriteShort(WeaponStore.Weapon(ownerId, i).GrenadeSprId);
        Engine.WriteByte(25);
        Engine.WriteByte(30);
        Engine.WriteByte(EngineConst.TeExplFlagNoSound);
        Engine.MessageEnd();

        Engine.MessageBegin(EngineConst.MsgPas, EngineConst.SvcTempEntity, origin, null);
        Engine.WriteByte(EngineConst.TeWorldDecal);
        Engine.WriteCoord(origin[0]);
        Engine.WriteCoord(origin[1]);
        Engine.WriteCoord(origin[2]);
        Engine.WriteByte(Engine.DecalIndex("{scorch3"));
        Engine.MessageEnd();

        Engine.EmitSound(entity, EngineConst.ChanStatic, WeaponStore.Weapon(ownerId, i).Sound1, EngineConst.VolNorm, WeaponStore.Weapon(ownerId, i).FireSoundVolume, 0, EngineConst.PitchNorm);

        // 记录手雷的序号，给deathmsg用
        WeaponStore.Player(ownerId).UserGrenadeId = i;

        // 原版从实体 0(worldspawn)开始检索
        var found = Engine.EntOfIndex(0);
        while (found != null && (found = Engine.FindEntityInSphere(found, origin, WeaponStore.Weapon(ownerId, i).GrenadeData[2])) != null)
        {
            int victim = Engine.IndexOf(found);
            if (!Engine.IsValidIndex(victim)) continue;
            if (found->v.solid <= EngineConst.SolidNot) continue;
            if (found->v.takedamage <= 0.0f) continue;
            var pOwner = Engine.EntOfIndex(ownerId);
            if (victim != ownerId && Engine.IsConnected(victim) && Engine.IsValid(pOwner) && Engine.GetPDataInt(pOwner, CsOffsets.Team) == Engine.GetPDataInt(found, CsOffsets.Team) && Engine.CvarNum("mp_friendlyfire") == 0)
                continue;

            HamSandwich.TakeDamage(victim, entityIndex, ownerId, 100.0f, 1 << 24);
            NativeFunctionRegister.Execute_WpnHookGrenadeExplodePost(ownerId, entityIndex, victim, i);
        }
        WeaponStore.Player(ownerId).UserGrenadeId = 0;
        Engine.RemoveEntity(entity);
        return EngineConst.HamSupercede;
    }

}
