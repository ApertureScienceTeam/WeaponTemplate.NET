using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// CheckAttack / ExecuteAttack / KnifeAttack / 机瞄(Opening/Closing)等攻击链逻辑翻译。
public static unsafe class Attack
{
    // public ExecuteAttack(iAttacker, iVictim, iEntity, fDamage, iHitGroup, iSpecial, iDamageType, fOrigin[3])
    public static bool ExecuteAttack(int attacker, int victim, edict_t* entity, float damage, int hitGroup, int special, int damageType, Span<float> origin)
    {
        var pVictim = Eng.EntOfIndex(victim);
        if (pVictim == null) return false;
        if (pVictim->v.takedamage <= 0.0f) return false;

        int hit = hitGroup;
        if ((special & WpnConst.SpdmgCantHeadshot) != 0 && hit == EngineConst.HitHead) hit = EngineConst.HitStomach;
        var pAttacker = Eng.EntOfIndex(attacker);
        if (Eng.IsConnected(attacker) && pAttacker != null && (special & WpnConst.SpdmgTeamattack) == 0 &&
            Eng.GetPDataInt(pAttacker, 114) == Eng.GetPDataInt(pVictim, 114) && Eng.CvarNum("mp_friendlyfire") == 0)
            return false;

        if (damageType == WpnConst.DmgTypeSector || damageType == WpnConst.DmgTypeLine)
        {
            if (Eng.IsConnected(attacker) && Store.AttackHitGroup[attacker] != 0) hit = Store.AttackHitGroup[attacker];
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
            Core.SpawnBlood(origin, Store.BloodColor, PawnMath.Round(damage));
        }
        Eng.SetPDataInt(pVictim, 75, hit);
        Eng.TakeDamage(victim, Eng.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
        return true;
    }

    // SpawnBlood(fOrigin, g_iBloodColor, floatround(fDamage)) 的内联调用见 ExecuteAttack

    // public CheckAttack(iAttacker, iVictim, iEntity, fParam[6], fOrigin[3], iDamageType, iSpecial)
    public static int CheckAttack(int attacker, int victim, edict_t* entity, Span<float> param, Span<float> origin, int damageType, int special)
    {
        if (!Eng.IsValid(Eng.EntOfIndex(attacker))) return -1;

        var pAttacker = Eng.EntOfIndex(attacker)!;
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
        Core.VecAdd(origin, viewOfs, origin);

        int hitWhat = WpnConst.HitEmpty;

        switch (damageType)
        {
            case WpnConst.DmgTypeSector:
            {
                float distance = MathF.Tan(angle / 2.0f * MathF.PI / 180.0f) * range;
                distance = PawnMath.Round(distance);
                int steps = PawnMath.Round(distance * 2.0f / angleOffset);
                int heightSteps = PawnMath.Round(height / heightOffset);
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
                        Core.GetPosition(attacker, range, fRight, fUp, targetOrigin);

                        TraceResult trace = default;
                        Eng.TraceLine(origin, targetOrigin, 0 /* DONT_IGNORE_MONSTERS */, pAttacker, &trace);
                        float fraction = trace.flFraction;
                        hitOrigin[0] = trace.vecEndPos.x;
                        hitOrigin[1] = trace.vecEndPos.y;
                        hitOrigin[2] = trace.vecEndPos.z;
                        int pHit = Eng.IndexOf(trace.pHit);
                        int hitGroup = trace.iHitgroup;

                        if (fraction >= 1.0f)
                        {
                            // 未击中任何东西(原版 iHitWhat == HIT_EMPTY 时保持不变)
                        }
                        else if (Eng.IsValidIndex(pHit)) // pev_valid: worldspawn(0) 也有效,与原版一致
                        {
                            if (Eng.IsConnected(pHit))
                            {
                                hitWhat = WpnConst.HitPlayer;
                                if (attacked[pHit]) continue;
                                attacked[pHit] = true;
                                ExecuteAttack(attacker, pHit, entity, damage, hitGroup, special, damageType, hitOrigin);
                            }
                            else
                            {
                                if (hitWhat != WpnConst.HitPlayer) hitWhat = WpnConst.HitEntity;
                                var pHit2 = Eng.EntOfIndex(pHit);
                                if (pHit2 == null || pHit2->v.takedamage <= 0.0f) continue;

                                if (Eng.GetPDataFloat(pHit2, 15) == Eng.GameTime) continue;

                                string className = Eng.ClassName(pHit2);
                                if (className == "hostage_entity" || className == "monster_scientist")
                                {
                                    hitWhat = WpnConst.HitPlayer;
                                    Core.SpawnBlood(hitOrigin, Store.BloodColor, PawnMath.Round(damage));
                                }
                                Eng.SetPDataFloat(pHit2, 15, Eng.GameTime);
                                Eng.TakeDamage(pHit, Eng.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
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
                Core.GetAimOrigin(attacker, hitOrigin);
                Core.GetPosition(attacker, range, 0.0f, 0.0f, targetOrigin);

                TraceResult trace = default;
                Eng.TraceLine(origin, targetOrigin, 0, pAttacker, &trace);
                int pHit = Eng.IndexOf(trace.pHit);
                int hitGroup = trace.iHitgroup;
                float fraction = trace.flFraction;
                hitOrigin[0] = trace.vecEndPos.x;
                hitOrigin[1] = trace.vecEndPos.y;
                hitOrigin[2] = trace.vecEndPos.z;

                if (fraction >= 1.0f) hitWhat = WpnConst.HitEmpty;
                else if (!Eng.IsValidIndex(pHit)) hitWhat = WpnConst.HitWall;

                if (Eng.IsValidIndex(pHit))
                {
                    if (Eng.IsConnected(pHit))
                    {
                        hitWhat = WpnConst.HitPlayer;
                        ExecuteAttack(attacker, pHit, entity, damage, hitGroup, special, damageType, hitOrigin);
                    }
                    else
                    {
                        hitWhat = WpnConst.HitEntity;
                        var pHit2 = Eng.EntOfIndex(pHit);
                        if (pHit2 != null && pHit2->v.takedamage != 0.0f)
                        {
                            string className = Eng.ClassName(pHit2);
                            if (className == "hostage_entity" || className == "monster_scientist")
                            {
                                hitWhat = WpnConst.HitPlayer;
                                Core.SpawnBlood(hitOrigin, Store.BloodColor, PawnMath.Round(damage));
                            }
                            Eng.TakeDamage(pHit, Eng.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
                        }
                    }
                }
                break;
            }
            case WpnConst.DmgTypeExplosion:
            {
                // 原版从实体 0(worldspawn)开始检索
                edict_t* found = Eng.EntOfIndex(0);
                fixed (float* pOrigin = origin)
                {
                    while (found != null && (found = Eng.FindEntityInSphere(found, pOrigin, range)) != null)
                    {
                        int pHit = Eng.IndexOf(found);
                        if (!Eng.IsValidIndex(pHit)) continue;
                        if (found->v.solid <= EngineConst.SolidNot) continue;

                        targetOrigin[0] = found->v.origin.x;
                        targetOrigin[1] = found->v.origin.y;
                        targetOrigin[2] = found->v.origin.z;
                        if (found->v.takedamage <= 0.0f)
                        {
                            if (hitWhat != WpnConst.HitPlayer) hitWhat = WpnConst.HitEntity;
                            continue;
                        }
                        if (Eng.IsConnected(pHit))
                        {
                            hitWhat = WpnConst.HitPlayer;
                            ExecuteAttack(attacker, pHit, entity, damage, 0, special, damageType, origin);
                        }
                        else
                        {
                            hitWhat = WpnConst.HitEntity;
                            string className = Eng.ClassName(found);
                            if (className == "hostage_entity" || className == "monster_scientist") hitWhat = WpnConst.HitPlayer;
                            Eng.TakeDamage(pHit, Eng.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
                        }
                    }
                }
                break;
            }
            case WpnConst.DmgTypeTouch:
            {
                int pHit = victim;
                if (Eng.IsValidIndex(pHit))
                {
                    var pHit2 = Eng.EntOfIndex(pHit);
                    if (Eng.IsConnected(pHit))
                    {
                        hitWhat = WpnConst.HitPlayer;
                        ExecuteAttack(attacker, pHit, entity, damage, 0, special, damageType, origin);
                    }
                    else
                    {
                        hitWhat = WpnConst.HitEntity;
                        string className = Eng.ClassName(pHit2);
                        if (className == "hostage_entity" || className == "monster_scientist")
                        {
                            hitWhat = WpnConst.HitPlayer;
                            Core.SpawnBlood(origin, Store.BloodColor, PawnMath.Round(damage));
                        }
                        Eng.TakeDamage(pHit, Eng.IndexOf(entity), attacker, damage, EngineConst.DmgNeverGib | EngineConst.DmgBullet);
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
        if (!Eng.IsAlive(id)) return;

        if (style == 1)
        {
            if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin] != 0)
            {
                Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin]);
                Eng.SetPDataFloat(entity, 46, Store.KnifeSlashTime[id, i]);
                Eng.SetPDataFloat(entity, 47, Store.KnifeSlashTime[id, i]);
                Eng.SetPDataFloat(entity, 48, Store.KnifeSlashTime[id, i]);
                Eng.SetPDataFloat(id, 83, Store.KnifeSlashTime[id, i]);
                Store.PlayerThink[id] = Eng.GameTime + Store.KnifeSlashTime[id, i];
            }
            else
            {
                Core.PlayAnim(id, Store.KnifeAnim[id, i, Store.KnifeSlashMode[id] + 1]);
                Eng.SetPDataFloat(entity, 46, Store.KnifeSlashTime[id, i] + Store.KnifeSlashResetTime[id, i]);
                Eng.SetPDataFloat(entity, 47, Store.KnifeSlashTime[id, i] + Store.KnifeSlashResetTime[id, i]);
                Eng.SetPDataFloat(entity, 48, Store.KnifeSlashTime[id, i] + Store.KnifeSlashResetTime[id, i]);
                Eng.SetPDataFloat(id, 83, Store.KnifeSlashTime[id, i] + Store.KnifeSlashResetTime[id, i]);
                Store.PlayerThink[id] = Eng.GameTime + Store.KnifeSlashTime[id, i];
            }
        }
        else if (style == 2)
        {
            int result = NativeFunctionRegister.Execute_WpnPrimaryAttackEnd(Eng.IndexOf(entity), id, i);
            if (result == WpnConst.WPN_SUPERCEDE) return;

            float range = Store.KnifeSlashRange[id, i];
            float damage = 15.0f * Store.WeaponDamage[id, i];
            float angle = Store.KnifeSlashAngle[id, i];
            Span<float> origin = stackalloc float[3];
            var pPlayer = Eng.EntOfIndex(id)!;
            origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;

            Span<float> param = stackalloc float[6];
            param[0] = range;
            param[1] = angle;
            param[2] = Store.KnifeSlashAngleOffset[id, i];
            param[3] = Store.KnifeSlashHeight[id, i];
            param[4] = Store.KnifeSlashHeightOffset[id, i];
            param[5] = damage;
            int hit = CheckAttack(id, 0, entity, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
            if (hit == WpnConst.HitEmpty)
            {
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound3[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 1], 0, EngineConst.PitchNorm);
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin] != 0) Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimMidSlashMiss]);
            }
            if (hit == WpnConst.HitPlayer)
            {
                if (Store.KnifeSlashMode[id] != 0) Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound5[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 3], 0, EngineConst.PitchNorm);
                else Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound6[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 4], 0, EngineConst.PitchNorm);
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin] != 0) Core.PlayAnim(id, Store.KnifeAnim[id, i, Store.KnifeSlashMode[id] + 1]);
            }
            if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
            {
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound2[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 0], 0, EngineConst.PitchNorm);
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimSlashBegin] != 0) Core.PlayAnim(id, Store.KnifeAnim[id, i, Store.KnifeSlashMode[id] + 1]);
            }
            Eng.SetPDataFloat(entity, 46, Store.KnifeSlashResetTime[id, i]);
            Eng.SetPDataFloat(entity, 47, Store.KnifeSlashResetTime[id, i]);
            Eng.SetPDataFloat(entity, 48, Store.KnifeSlashResetTime[id, i]);
            Eng.SetPDataFloat(id, 83, Store.KnifeSlashResetTime[id, i]);
            Store.PlayerThink[id] = Eng.GameTime + Store.KnifeSlashResetTime[id, i];
            NativeFunctionRegister.Execute_WpnPrimaryAttackEndPost(Eng.IndexOf(entity), id, i);
        }
        else if (style == 3)
        {
            if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin] != 0)
            {
                Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin]);
                Eng.SetPDataFloat(entity, 46, Store.KnifeStabTime[id, i]);
                Eng.SetPDataFloat(entity, 47, Store.KnifeStabTime[id, i]);
                Eng.SetPDataFloat(entity, 48, Store.KnifeStabTime[id, i]);
                Eng.SetPDataFloat(id, 83, Store.KnifeStabTime[id, i]);
                Store.PlayerThink[id] = Eng.GameTime + Store.KnifeStabTime[id, i];
            }
            else
            {
                Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab]);
                Eng.SetPDataFloat(entity, 46, Store.KnifeStabTime[id, i] + Store.KnifeStabResetTime[id, i]);
                Eng.SetPDataFloat(entity, 47, Store.KnifeStabTime[id, i] + Store.KnifeStabResetTime[id, i]);
                Eng.SetPDataFloat(entity, 48, Store.KnifeStabTime[id, i] + Store.KnifeStabResetTime[id, i]);
                Eng.SetPDataFloat(id, 83, Store.KnifeStabTime[id, i] + Store.KnifeStabResetTime[id, i]);
                Store.PlayerThink[id] = Eng.GameTime + Store.KnifeStabTime[id, i];
            }
        }
        else if (style == 4)
        {
            int result = NativeFunctionRegister.Execute_WpnSecondaryAttackEnd(Eng.IndexOf(entity), id, i);
            if (result == WpnConst.WPN_SUPERCEDE) return;

            float range = Store.KnifeStabRange[id, i];
            float damage = 65.0f * Store.WeaponDamage2[id, i];
            float angle = Store.KnifeStabAngle[id, i];
            Span<float> origin = stackalloc float[3];
            var pPlayer = Eng.EntOfIndex(id)!;
            origin[0] = pPlayer->v.origin.x; origin[1] = pPlayer->v.origin.y; origin[2] = pPlayer->v.origin.z;

            Span<float> param = stackalloc float[6];
            param[0] = range;
            param[1] = angle;
            param[2] = Store.KnifeStabAngleOffset[id, i];
            param[3] = Store.KnifeStabHeight[id, i];
            param[4] = Store.KnifeStabHeightOffset[id, i];
            param[5] = damage;
            int hit = CheckAttack(id, 0, entity, param, origin, angle != 0.0f ? WpnConst.DmgTypeSector : WpnConst.DmgTypeLine, WpnConst.SpdmgNormal);
            if (hit == WpnConst.HitEmpty)
            {
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound3[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 1], 0, EngineConst.PitchNorm);
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin] != 0) Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabMiss]);
            }
            if (hit == WpnConst.HitPlayer)
            {
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound4[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 2], 0, EngineConst.PitchNorm);
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin] != 0) Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab]);
            }
            if (hit == WpnConst.HitEntity || hit == WpnConst.HitWall)
            {
                Eng.EmitSound(entity, EngineConst.ChanWeapon, Store.WeaponSound2[id, i], EngineConst.VolNorm, Store.KnifeSoundVolume[id, i, 0], 0, EngineConst.PitchNorm);
                if (Store.KnifeAnim[id, i, WpnConst.KnifeAnimStabBegin] != 0) Core.PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimStab]);
            }
            Eng.SetPDataFloat(entity, 46, Store.KnifeStabResetTime[id, i]);
            Eng.SetPDataFloat(entity, 47, Store.KnifeStabResetTime[id, i]);
            Eng.SetPDataFloat(entity, 48, Store.KnifeStabResetTime[id, i]);
            Eng.SetPDataFloat(id, 83, Store.KnifeStabResetTime[id, i]);
            Store.PlayerThink[id] = Eng.GameTime + Store.KnifeStabResetTime[id, i];
            NativeFunctionRegister.Execute_WpnSecondaryAttackEndPost(Eng.IndexOf(entity), id, i);
        }
    }

    // ---- 机瞄(镜头) ----

    // public BeginOpening(iEntity)
    public static void BeginOpening(edict_t* entity)
    {
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        Eng.SetPDataFloat(entity, 46, Store.SightOpenTime[id, i]);
        Eng.SetPDataFloat(entity, 47, Store.SightOpenTime[id, i]);
        Eng.SetPDataFloat(entity, 48, Store.SightOpenTime[id, i]);
        Store.SightMode[id] = WpnConst.AtoB;
        Core.PlayAnim(id, Store.WeaponAnim[id, i, WpnConst.AnimSightOpen]);
        Eng.SetPDataInt(pPlayer, 361, Eng.GetPDataInt(pPlayer, 361) | (1 << 6));
    }

    // public BeginClosing(iEntity)
    public static void BeginClosing(edict_t* entity)
    {
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        Eng.SetPDataFloat(entity, 46, Store.SightCloseTime[id, i]);
        Eng.SetPDataFloat(entity, 47, Store.SightCloseTime[id, i]);
        Eng.SetPDataFloat(entity, 48, Store.SightCloseTime[id, i]);
        Eng.SetViewModel(pPlayer, Store.WeaponVModel[id, i]);
        Store.SightMode[id] = WpnConst.BtoA;
        Core.PlayAnim(id, Store.WeaponAnim[id, i, WpnConst.AnimSightClose]);
        Eng.SetPDataInt(pPlayer, 361, Eng.GetPDataInt(pPlayer, 361) & ~(1 << 6));
    }

    // public OpeningEnd(iEntity)
    public static void OpeningEnd(edict_t* entity)
    {
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        if (Store.SightDistance[id, i] != 0.0f) Eng.SetPDataInt(pPlayer, 363, PawnMath.Round(Store.SightDistance[id, i]));
        Eng.SetViewModel(pPlayer, Store.WeaponVModelS[id, i]);
        Store.SightMode[id] = WpnConst.Open;
        Core.PlayAnim(id, Store.WeaponAnim[id, i, WpnConst.AnimIdle]);
    }

    // public ClosingEnd(iEntity)
    public static void ClosingEnd(edict_t* entity)
    {
        var pPlayer = Eng.GetPDataCbase(entity, 41);
        if (pPlayer == null) return;
        int id = Eng.IndexOf(pPlayer);
        int i = Store.GetWeaponType(entity);
        Eng.SetPDataInt(pPlayer, 361, Eng.GetPDataInt(pPlayer, 361) & ~(1 << 6));
        Eng.SetPDataInt(pPlayer, 363, 90);
        Store.SightMode[id] = WpnConst.Closed;
        Eng.SetViewModel(pPlayer, Store.WeaponVModel[id, i]);
        Core.PlayAnim(id, Store.WeaponAnim[id, i, WpnConst.AnimIdle]);
        if (!Store.Reloading[id]) return;

        Store.Reloading[id] = false;
        MF.CallNative("ExecuteHamB", EngineConst.HamWeaponReload, Eng.IndexOf(entity));
    }
}
