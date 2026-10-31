using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// CheckAttack / ExecuteAttack / KnifeAttack / 机瞄(Opening/Closing)等攻击链逻辑翻译。
public static unsafe partial class Attack
{
    // public ExecuteAttack(iAttacker, iVictim, iEntity, fDamage, iHitGroup, iSpecial, iDamageType, fOrigin[3])
    public static bool ExecuteAttack(int attacker, int victim, edict_t* entity, float damage, int hitGroup, int special, int damageType, Span<float> origin)
    {
        var pVictim = Engine.EntOfIndex(victim);
        if (pVictim == null) return false;
        if (pVictim->v.takedamage <= 0.0f) return false;

        int hit = hitGroup;
        if ((special & WpnConst.SpdmgCantHeadshot) != 0 && hit == EngineConst.HitHead) hit = EngineConst.HitStomach;
        var pAttacker = Engine.EntOfIndex(attacker);
        if (Engine.IsConnected(attacker) && pAttacker != null && (special & WpnConst.SpdmgTeamattack) == 0 &&
            Engine.GetPDataInt(pAttacker, 114) == Engine.GetPDataInt(pVictim, 114) && Engine.CvarNum("mp_friendlyfire") == 0)
            return false;

        if (damageType == WpnConst.DmgTypeSector || damageType == WpnConst.DmgTypeLine)
        {
            if (Engine.IsConnected(attacker) && WeaponStore.Player(attacker).AttackHitGroup != 0) hit = WeaponStore.Player(attacker).AttackHitGroup;
            switch (hit)
            {
                case EngineConst.HitHead: damage *= 4.0f; break;
                case EngineConst.HitStomach:
                case EngineConst.HitChest: damage *= 1.5f; break;
                case EngineConst.HitRightArm:
                case EngineConst.HitLeftArm: damage *= 1.0f; break;
                case EngineConst.HitRightLeg:
                case EngineConst.HitLeftLeg: damage *= 0.75f; break;
            }
            WeaponEffects.SpawnBlood(origin, WeaponStore.BloodColor, FloatMath.Round(damage));
        }
        Engine.SetPDataInt(pVictim, 75, hit);
        HamSandwich.TakeDamage(victim, Engine.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
        return true;
    }

    // SpawnBlood(fOrigin, g_iBloodColor, floatround(fDamage)) 的内联调用见 ExecuteAttack

    // public CheckAttack(iAttacker, iVictim, iEntity, fParam[6], fOrigin[3], iDamageType, iSpecial)
    public static int CheckAttack(int attacker, int victim, edict_t* entity, Span<float> param, Span<float> origin, int damageType, int special)
    {
        if (!Engine.IsValid(Engine.EntOfIndex(attacker))) return -1;

        var pAttacker = Engine.EntOfIndex(attacker)!;
        Span<float> targetOrigin = stackalloc float[3];
        Span<float> hitOrigin = stackalloc float[3];
        Span<float> viewOfs = stackalloc float[3];
        var attacked = stackalloc bool[33];

        float range = param[0];
        float angle = param[1];
        float angleOffset = param[2];
        float height = param[3];
        float heightOffset = param[4];
        float damage = param[5];

        viewOfs[0] = pAttacker->v.view_ofsr[0];
        viewOfs[1] = pAttacker->v.view_ofsr[1];
        viewOfs[2] = pAttacker->v.view_ofsr[2];
        VectorMath.VecAdd(origin, viewOfs, origin);

        int hitWhat = WpnConst.HitEmpty;

        switch (damageType)
        {
            case WpnConst.DmgTypeSector:
            {
                float distance = MathF.Tan(angle / 2.0f * MathF.PI / 180.0f) * range;
                distance = FloatMath.Round(distance);
                int steps = FloatMath.Round(distance * 2.0f / angleOffset);
                int heightSteps = FloatMath.Round(height / heightOffset);
                int[] number = new int[2];
                bool upSide = false;

                for (int h = 0; h <= heightSteps; h++)
                {
                    number[0] = 0;
                    bool rightSide = false;
                    upSide = !upSide;
                    if (!upSide) number[1]++;
                    for (int s = 0; s <= steps; s++)
                    {
                        rightSide = !rightSide;
                        if (!rightSide) number[0]++;
                        float fRight = (rightSide ? 1.0f : -1.0f) * number[0] * angleOffset;
                        float fUp = (upSide ? 1.0f : -1.0f) * number[1] * heightOffset;
                        VectorMath.GetPosition(attacker, range, fRight, fUp, targetOrigin);

                        TraceResult trace = default;
                        Engine.TraceLine(origin, targetOrigin, 0 /* DONT_IGNORE_MONSTERS */, pAttacker, &trace);
                        float fraction = trace.flFraction;
                        hitOrigin[0] = trace.vecEndPos.x;
                        hitOrigin[1] = trace.vecEndPos.y;
                        hitOrigin[2] = trace.vecEndPos.z;
                        int pHit = Engine.IndexOf(trace.pHit);
                        int hitGroup = trace.iHitgroup;

                        if (fraction >= 1.0f)
                        {
                            // 未击中任何东西(原版 iHitWhat == HIT_EMPTY 时保持不变)
                        }
                        else if (Engine.IsValidIndex(pHit)) // pev_valid: worldspawn(0) 也有效,与原版一致
                        {
                            if (Engine.IsConnected(pHit))
                            {
                                hitWhat = WpnConst.HitPlayer;
                                if (attacked[pHit]) continue;
                                attacked[pHit] = true;
                                ExecuteAttack(attacker, pHit, entity, damage, hitGroup, special, damageType, hitOrigin);
                            }
                            else
                            {
                                if (hitWhat != WpnConst.HitPlayer) hitWhat = WpnConst.HitEntity;
                                var pHit2 = Engine.EntOfIndex(pHit);
                                if (pHit2 == null || pHit2->v.takedamage <= 0.0f) continue;

                                if (Engine.GetPDataFloat(pHit2, 15) == Engine.GameTime) continue;

                                string className = Engine.ClassName(pHit2);
                                if (className == "hostage_entity" || className == "monster_scientist")
                                {
                                    hitWhat = WpnConst.HitPlayer;
                                    WeaponEffects.SpawnBlood(hitOrigin, WeaponStore.BloodColor, FloatMath.Round(damage));
                                }
                                Engine.SetPDataFloat(pHit2, 15, Engine.GameTime);
                                HamSandwich.TakeDamage(pHit, Engine.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
                            }
                        }
                        else
                        {
                            if (hitWhat != WpnConst.HitPlayer && hitWhat != WpnConst.HitEntity) hitWhat = WpnConst.HitWall;
                        }
                    }
                }
                break;
            }
            case WpnConst.DmgTypeLine:
            {
                VectorMath.GetAimOrigin(attacker, hitOrigin);
                VectorMath.GetPosition(attacker, range, 0.0f, 0.0f, targetOrigin);

                TraceResult trace = default;
                Engine.TraceLine(origin, targetOrigin, 0, pAttacker, &trace);
                int pHit = Engine.IndexOf(trace.pHit);
                int hitGroup = trace.iHitgroup;
                float fraction = trace.flFraction;
                hitOrigin[0] = trace.vecEndPos.x;
                hitOrigin[1] = trace.vecEndPos.y;
                hitOrigin[2] = trace.vecEndPos.z;

                if (fraction >= 1.0f) hitWhat = WpnConst.HitEmpty;
                else if (!Engine.IsValidIndex(pHit)) hitWhat = WpnConst.HitWall;

                if (Engine.IsValidIndex(pHit))
                {
                    if (Engine.IsConnected(pHit))
                    {
                        hitWhat = WpnConst.HitPlayer;
                        ExecuteAttack(attacker, pHit, entity, damage, hitGroup, special, damageType, hitOrigin);
                    }
                    else
                    {
                        hitWhat = WpnConst.HitEntity;
                        var pHit2 = Engine.EntOfIndex(pHit);
                        if (pHit2 != null && pHit2->v.takedamage != 0.0f)
                        {
                            string className = Engine.ClassName(pHit2);
                            if (className == "hostage_entity" || className == "monster_scientist")
                            {
                                hitWhat = WpnConst.HitPlayer;
                                WeaponEffects.SpawnBlood(hitOrigin, WeaponStore.BloodColor, FloatMath.Round(damage));
                            }
                            HamSandwich.TakeDamage(pHit, Engine.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
                        }
                    }
                }
                break;
            }
            case WpnConst.DmgTypeExplosion:
            {
                // 原版从实体 0(worldspawn)开始检索
                edict_t* found = Engine.EntOfIndex(0);
                fixed (float* pOrigin = origin)
                {
                    while (found != null && (found = Engine.FindEntityInSphere(found, pOrigin, range)) != null)
                    {
                        int pHit = Engine.IndexOf(found);
                        if (!Engine.IsValidIndex(pHit)) continue;
                        if (found->v.solid <= EngineConst.SolidNot) continue;

                        targetOrigin[0] = found->v.origin.x;
                        targetOrigin[1] = found->v.origin.y;
                        targetOrigin[2] = found->v.origin.z;
                        if (found->v.takedamage <= 0.0f)
                        {
                            if (hitWhat != WpnConst.HitPlayer) hitWhat = WpnConst.HitEntity;
                            continue;
                        }
                        if (Engine.IsConnected(pHit))
                        {
                            hitWhat = WpnConst.HitPlayer;
                            ExecuteAttack(attacker, pHit, entity, damage, 0, special, damageType, origin);
                        }
                        else
                        {
                            hitWhat = WpnConst.HitEntity;
                            string className = Engine.ClassName(found);
                            if (className == "hostage_entity" || className == "monster_scientist") hitWhat = WpnConst.HitPlayer;
                            HamSandwich.TakeDamage(pHit, Engine.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
                        }
                    }
                }
                break;
            }
            case WpnConst.DmgTypeTouch:
            {
                int pHit = victim;
                if (Engine.IsValidIndex(pHit))
                {
                    var pHit2 = Engine.EntOfIndex(pHit);
                    if (Engine.IsConnected(pHit))
                    {
                        hitWhat = WpnConst.HitPlayer;
                        ExecuteAttack(attacker, pHit, entity, damage, 0, special, damageType, origin);
                    }
                    else
                    {
                        hitWhat = WpnConst.HitEntity;
                        string className = Engine.ClassName(pHit2);
                        if (className == "hostage_entity" || className == "monster_scientist")
                        {
                            hitWhat = WpnConst.HitPlayer;
                            WeaponEffects.SpawnBlood(origin, WeaponStore.BloodColor, FloatMath.Round(damage));
                        }
                        HamSandwich.TakeDamage(pHit, Engine.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
                    }
                }
                else hitWhat = WpnConst.HitWall;
                break;
            }
        }
        return hitWhat;
    }

    // public KnifeAttack(id, i, style, iEntity)
    public static void KnifeAttack(int id, int i, int style, edict_t* entity)
    {
        if (!Engine.IsAlive(id)) return;

        if (style == 1)
        {
            if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] != 0)
            {
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin]);
                Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).KnifeSlashTime);
                Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).KnifeSlashTime);
                Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).KnifeSlashTime);
                Engine.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeSlashTime);
                WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeSlashTime;
            }
            else
            {
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
                Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).KnifeSlashTime + WeaponStore.Weapon(id, i).KnifeSlashResetTime);
                Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).KnifeSlashTime + WeaponStore.Weapon(id, i).KnifeSlashResetTime);
                Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).KnifeSlashTime + WeaponStore.Weapon(id, i).KnifeSlashResetTime);
                Engine.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeSlashTime + WeaponStore.Weapon(id, i).KnifeSlashResetTime);
                WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeSlashTime;
            }
        }
        else if (style == 2)
        {
            int result = NativeFunctionRegister.Execute_WpnPrimaryAttackEnd(Engine.IndexOf(entity), id, i);
            if (result == WpnConst.WPN_SUPERCEDE) return;

            float range = WeaponStore.Weapon(id, i).KnifeSlashRange;
            float damage = 15.0f * WeaponStore.Weapon(id, i).Damage;
            float angle = WeaponStore.Weapon(id, i).KnifeSlashAngle;
            Span<float> origin = stackalloc float[3];
            var pPlayer = Engine.EntOfIndex(id)!;
            origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;

            Span<float> param = stackalloc float[6];
            param[0] = range;
            param[1] = angle;
            param[2] = WeaponStore.Weapon(id, i).KnifeSlashAngleOffset;
            param[3] = WeaponStore.Weapon(id, i).KnifeSlashHeight;
            param[4] = WeaponStore.Weapon(id, i).KnifeSlashHeightOffset;
            param[5] = damage;
            int hit = CheckAttack(id, 0, entity, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
            if (hit == WpnConst.HitEmpty)
            {
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] != 0) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimMidSlashMiss]);
            }
            if (hit == WpnConst.HitPlayer)
            {
                if (WeaponStore.Player(id).KnifeSlashMode != 0) Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound5, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[3], 0, EngineConst.PitchNorm);
                else Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound6, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[4], 0, EngineConst.PitchNorm);
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] != 0) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
            }
            if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
            {
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] != 0) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WeaponStore.Player(id).KnifeSlashMode + 1]);
            }
            Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
            Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
            Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
            Engine.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeSlashResetTime);
            WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeSlashResetTime;
            NativeFunctionRegister.Execute_WpnPrimaryAttackEndPost(Engine.IndexOf(entity), id, i);
        }
        else if (style == 3)
        {
            if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin] != 0)
            {
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin]);
                Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).KnifeStabTime);
                Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).KnifeStabTime);
                Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).KnifeStabTime);
                Engine.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeStabTime);
                WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeStabTime;
            }
            else
            {
                PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab]);
                Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).KnifeStabTime + WeaponStore.Weapon(id, i).KnifeStabResetTime);
                Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).KnifeStabTime + WeaponStore.Weapon(id, i).KnifeStabResetTime);
                Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).KnifeStabTime + WeaponStore.Weapon(id, i).KnifeStabResetTime);
                Engine.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeStabTime + WeaponStore.Weapon(id, i).KnifeStabResetTime);
                WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeStabTime;
            }
        }
        else if (style == 4)
        {
            int result = NativeFunctionRegister.Execute_WpnSecondaryAttackEnd(Engine.IndexOf(entity), id, i);
            if (result == WpnConst.WPN_SUPERCEDE) return;

            float range = WeaponStore.Weapon(id, i).KnifeStabRange;
            float damage = 65.0f * WeaponStore.Weapon(id, i).Damage2;
            float angle = WeaponStore.Weapon(id, i).KnifeStabAngle;
            Span<float> origin = stackalloc float[3];
            var pPlayer = Engine.EntOfIndex(id)!;
            origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;

            Span<float> param = stackalloc float[6];
            param[0] = range;
            param[1] = angle;
            param[2] = WeaponStore.Weapon(id, i).KnifeStabAngleOffset;
            param[3] = WeaponStore.Weapon(id, i).KnifeStabHeight;
            param[4] = WeaponStore.Weapon(id, i).KnifeStabHeightOffset;
            param[5] = damage;
            int hit = CheckAttack(id, 0, entity, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
            if (hit == WpnConst.HitEmpty)
            {
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin] != 0) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabMiss]);
            }
            if (hit == WpnConst.HitPlayer)
            {
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[2], 0, EngineConst.PitchNorm);
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin] != 0) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab]);
            }
            if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
            {
                Engine.EmitSound(entity, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStabBegin] != 0) PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimStab]);
            }
            Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).KnifeStabResetTime);
            Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).KnifeStabResetTime);
            Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).KnifeStabResetTime);
            Engine.SetPDataFloat(id, 83, WeaponStore.Weapon(id, i).KnifeStabResetTime);
            WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeStabResetTime;
            NativeFunctionRegister.Execute_WpnSecondaryAttackEndPost(Engine.IndexOf(entity), id, i);
        }
    }

    // ---- 机瞄(镜头) ----

    // public BeginOpening(iEntity)
    public static void BeginOpening(edict_t* entity)
    {
        var pPlayer = Engine.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).SightOpenTime);
        Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).SightOpenTime);
        Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).SightOpenTime);
        WeaponStore.Player(id).SightMode = WpnConst.AtoB;
        PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimSightOpen]);
        Engine.SetPDataInt(pPlayer, 361, Engine.GetPDataInt(pPlayer, 361) | (1 << 6));
    }

    // public BeginClosing(iEntity)
    public static void BeginClosing(edict_t* entity)
    {
        var pPlayer = Engine.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        Engine.SetPDataFloat(entity, 46, WeaponStore.Weapon(id, i).SightCloseTime);
        Engine.SetPDataFloat(entity, 47, WeaponStore.Weapon(id, i).SightCloseTime);
        Engine.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).SightCloseTime);
        Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
        WeaponStore.Player(id).SightMode = WpnConst.BtoA;
        PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimSightClose]);
        Engine.SetPDataInt(pPlayer, 361, Engine.GetPDataInt(pPlayer, 361) & ~(1 << 6));
    }

    // public OpeningEnd(iEntity)
    public static void OpeningEnd(edict_t* entity)
    {
        var pPlayer = Engine.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        if (WeaponStore.Weapon(id, i).SightDistance != 0.0f) Engine.SetPDataInt(pPlayer, 363, FloatMath.Round(WeaponStore.Weapon(id, i).SightDistance));
        Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).SightModel);
        WeaponStore.Player(id).SightMode = WpnConst.Open;
        PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdle]);
    }

    // public ClosingEnd(iEntity)
    public static void ClosingEnd(edict_t* entity)
    {
        var pPlayer = Engine.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Engine.IndexOf(pPlayer);
        int i = WeaponStore.GetWeaponType(entity);
        Engine.SetPDataInt(pPlayer, 361, Engine.GetPDataInt(pPlayer, 361) & ~(1 << 6));
        Engine.SetPDataInt(pPlayer, 363, 90);
        WeaponStore.Player(id).SightMode = WpnConst.Closed;
        Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
        PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).Anim[WpnConst.AnimIdle]);
        if (!WeaponStore.Player(id).Reloading) return;

        WeaponStore.Player(id).Reloading = false;
        HamSandwich.ExecuteHamB(EngineConst.HamWeaponReload, Engine.IndexOf(entity));
    }
    // fm_PlayerPostThink_Post 的刀子延迟二段攻击:主/副攻击把 KnifeAttack 置 1/3 并设 PlayerThink 计时,
    // 计时到期后在这里执行真正的范围判定(原版 fw_PlayerPostThink_Post 的刀子分支)。
    public static void ProcessDelayedKnife(int id)
    {
        if (WeaponStore.Player(id).PlayerThink != 0.0f && Engine.GameTime - WeaponStore.Player(id).PlayerThink >= 0.0f)
        {
            var pPlayer = Engine.EntOfIndex(id);
            var knifeItem = pPlayer == null ? null : Engine.GetPDataCbase(pPlayer, 370);
            WeaponStore.Player(id).PlayerThink = 0.0f;
            if (!Engine.IsValid(knifeItem)) return;

            int i = WeaponStore.GetWeaponType(knifeItem);
            if (WeaponStore.Player(id).KnifeAttack == 1)
            {
                if (WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimSlashBegin] != 0)
                {
                    WeaponStore.Player(id).KnifeAttack = 2;
                    KnifeAttack(id, i, WeaponStore.Player(id).KnifeAttack, knifeItem);
                    return;
                }
                else
                {
                    int result = NativeFunctionRegister.Execute_WpnPrimaryAttackEnd(Engine.IndexOf(knifeItem), id, i);
                    if (result == WpnConst.WPN_SUPERCEDE) return;

                    WeaponStore.Player(id).KnifeAttack = 2;
                    WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeSlashResetTime;
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
                    int hit = CheckAttack(id, 0, knifeItem, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
                    if (hit == WpnConst.HitEmpty)
                        Engine.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitPlayer)
                    {
                        if (WeaponStore.Player(id).KnifeSlashMode != 0)
                            Engine.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound5, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[3], 0, EngineConst.PitchNorm);
                        else
                            Engine.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound6, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[4], 0, EngineConst.PitchNorm);
                    }
                    if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
                        Engine.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                    NativeFunctionRegister.Execute_WpnPrimaryAttackEndPost(Engine.IndexOf(knifeItem), id, i);
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
                    KnifeAttack(id, i, WeaponStore.Player(id).KnifeAttack, knifeItem);
                    return;
                }
                else
                {
                    int result = NativeFunctionRegister.Execute_WpnSecondaryAttackEnd(Engine.IndexOf(knifeItem), id, i);
                    if (result == WpnConst.WPN_SUPERCEDE) return;

                    WeaponStore.Player(id).KnifeAttack = 4;
                    WeaponStore.Player(id).PlayerThink = Engine.GameTime + WeaponStore.Weapon(id, i).KnifeStabResetTime;
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
                    int hit = CheckAttack(id, 0, knifeItem, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
                    if (hit == WpnConst.HitEmpty)
                        Engine.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound3, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[1], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitPlayer)
                        Engine.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound4, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[2], 0, EngineConst.PitchNorm);
                    if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
                        Engine.EmitSound(knifeItem, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound2, EngineConst.VolNorm, WeaponStore.Weapon(id, i).KnifeSoundVolume[0], 0, EngineConst.PitchNorm);
                    NativeFunctionRegister.Execute_WpnSecondaryAttackEndPost(Engine.IndexOf(knifeItem), id, i);
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
}
