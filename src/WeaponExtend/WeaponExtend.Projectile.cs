using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// 抛射物域:延迟思考、碰撞伤害与抛射物创建。
public static unsafe partial class WeaponExtend
{
    // ---- 抛射物思考 ----

    private static void ThinkLogic(edict_t* entity)
    {
        if (!Engine.IsValid(entity)) return;

        string globalName = Engine.GlobalName(entity);
        if (globalName != WpnProjectileName) return;

        var pev = &entity->v;
        Span<float> origin = stackalloc float[3] { pev->origin.x, pev->origin.y, pev->origin.z };
        Span<float> targetOrigin = stackalloc float[3];
        Span<float> velocity = stackalloc float[3];
        int owner = Engine.IndexOf(pev->owner);
        int i = Engine.GetPDataInt(entity, WeaponIdPData);
        pev->nextthink = Engine.GameTime + 0.01f;

        int sprEnt = pev->iuser4;
        if (Engine.IsValid(Engine.EntOfIndex(sprEnt)))
        {
            var sprPev = &Engine.EntOfIndex(sprEnt)->v;
            sprPev->frame += 0.2f * sprPev->framerate;
            if ((int)sprPev->frame >= sprPev->iuser1) sprPev->frame = 0.0f;
        }

        // SPR播放
        if (pev->iuser2 == ProjectileSprite)
        {
            pev->frame += 0.2f * pev->framerate;
            if ((int)pev->frame >= pev->iuser3) pev->frame = 0.0f;
        }

        // 延迟时间
        float dmgTime = pev->dmgtime;
        if (WeaponDamageType[owner, i] == DamageTypeDelayExp && Engine.GameTime > dmgTime)
        {
            int explodeFlag = 0;
            if (ProjectileExplodeEffect[owner, i] == 0)
                explodeFlag = EngineConst.TeExplFlagNoDlights | EngineConst.TeExplFlagNoParticles;
            else
            {
                Engine.MessageBegin(0, 23, null, null);
                Engine.WriteByte(EngineConst.TeWorldDecal);
                Engine.WriteCoord(origin[0]);
                Engine.WriteCoord(origin[1]);
                Engine.WriteCoord(origin[2]);
                Engine.WriteByte(Engine.RandomNum(46, 48));
                Engine.MessageEnd();
            }
            if (ProjectileExplodeSound[owner, i] != DmgSoundDefault)
            {
                explodeFlag |= EngineConst.TeExplFlagNoSound;
                if (ProjectileExplodeSound[owner, i] == DmgSoundCustom)
                    Engine.EmitSound(entity, 1, ProjectileExplodeSoundPath[owner, i], 1.0f, 0.8f, 0, 100);
            }
            Engine.MessageBegin(0, 23, null, null);
            Engine.WriteByte(EngineConst.TeExplosion);
            Engine.WriteCoord(origin[0]);
            Engine.WriteCoord(origin[1]);
            Engine.WriteCoord(origin[2] + ProjectileExpHeightOfs[owner, i]);
            Engine.WriteShort(ProjectileExplodeSprite[owner, i]);
            Engine.WriteByte(FloatMath.Round(ProjectileExpScale[owner, i] * 10.0f));
            Engine.WriteByte(FloatMath.Round(ProjectileExpFramerate[owner, i] * 10.0f));
            Engine.WriteByte(explodeFlag);
            Engine.MessageEnd();

            Span<float> param = stackalloc float[6];
            param[0] = ProjectileDamageRadius[owner, i];
            param[5] = WeaponDamage[owner, i];
            Wpn.ExecuteAttack(owner, 0, Engine.IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeExplosion, WpnConst.SpdmgCantHeadshot);
            if (Engine.IsValid(Engine.EntOfIndex(sprEnt))) Engine.RemoveEntity(Engine.EntOfIndex(sprEnt));
            Engine.RemoveEntity(entity);
            return;
        }

        // 范围碰撞伤害
        if (WeaponDamageType[owner, i] == DamageTypeAoeTouch)
        {
            Span<float> param = stackalloc float[6];
            for (int id = 1; id < MaxPlayers; id++)
            {
                if (!Engine.IsAlive(id)) continue;

                var pTarget = Engine.EntOfIndex(id);
                targetOrigin[0] = pTarget->v.origin.x;
                targetOrigin[1] = pTarget->v.origin.y;
                targetOrigin[2] = pTarget->v.origin.z;
                if (VectorMath.Distance(origin, targetOrigin) > ProjectileDamageRadius[owner, i]) continue;

                if ((pev->iuser1 & (1 << id)) != 0) continue;

                if (GetPlayerTeam(id) == GetPlayerTeam(owner)) continue;

                pev->iuser1 |= 1 << id;
                param.Clear();
                param[5] = WeaponDamage[owner, i];
                Wpn.ExecuteAttack(owner, id, Engine.IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeTouch, WpnConst.SpdmgCantHeadshot);
            }
        }

        // 存在时间
        float user4Time = pev->fuser4;
        if (Engine.GameTime > user4Time && user4Time > 0.0f)
        {
            if (Engine.IsValid(Engine.EntOfIndex(sprEnt))) Engine.RemoveEntity(Engine.EntOfIndex(sprEnt));
            Engine.RemoveEntity(entity);
            return;
        }

        // 拖尾间隔SPR
        float user1Time = pev->fuser1;
        if (Engine.GameTime > user1Time && user1Time != 0.0f)
        {
            Engine.MessageBegin(0, 23, null, null);
            Engine.WriteByte(EngineConst.TeExplosion);
            Engine.WriteCoord(origin[0]);
            Engine.WriteCoord(origin[1]);
            Engine.WriteCoord(origin[2] + ProjectileTailHeightOfs[owner, i]);
            Engine.WriteShort(ProjectileTailSprite[owner, i]);
            Engine.WriteByte(FloatMath.Round(ProjectileTailScale[owner, i] * 10.0f));
            Engine.WriteByte(FloatMath.Round(ProjectileTailFramerate[owner, i] * 10.0f));
            Engine.WriteByte(EngineConst.TeExplFlagNoDlights | EngineConst.TeExplFlagNoSound | EngineConst.TeExplFlagNoParticles);
            Engine.MessageEnd();
            pev->fuser1 = Engine.GameTime + ProjectileTailTime[owner, i];
        }

        // 抛射物发光
        float user2Time = pev->fuser2;
        if (Engine.GameTime > user2Time && user2Time != 0.0f)
        {
            Engine.MessageBegin(3, 23, origin, null);
            Engine.WriteByte(EngineConst.TeDlight);
            Engine.WriteCoord(origin[0]);
            Engine.WriteCoord(origin[1]);
            Engine.WriteCoord(origin[2]);
            Engine.WriteByte(ProjectileGlowRadius[owner, i] / 10);
            Engine.WriteByte(ProjectileGlowColor[owner, i, 0]);
            Engine.WriteByte(ProjectileGlowColor[owner, i, 1]);
            Engine.WriteByte(ProjectileGlowColor[owner, i, 2]);
            Engine.WriteByte(1);
            Engine.WriteByte(0);
            Engine.MessageEnd();

            Engine.MessageBegin(3, 23, origin, null);
            Engine.WriteByte(EngineConst.TeElight);
            Engine.WriteShort(Engine.IndexOf(entity));
            Engine.WriteCoord(origin[0]);
            Engine.WriteCoord(origin[1]);
            Engine.WriteCoord(origin[2]);
            Engine.WriteCoord(10.0f);
            Engine.WriteByte(ProjectileGlowColor[owner, i, 0]);
            Engine.WriteByte(ProjectileGlowColor[owner, i, 1]);
            Engine.WriteByte(ProjectileGlowColor[owner, i, 2]);
            Engine.WriteByte(2);
            Engine.WriteCoord(0.0f);
            Engine.MessageEnd();

            pev->fuser2 = Engine.GameTime + 0.01f;
        }

        int target = pev->euser1 != null ? Engine.IndexOf(pev->euser1) : 0;
        if (ProjectileTraceMode[owner, i] == TraceModeAuto)
        {
            float maxDis = 99999.0f;
            int tempTarget = 0;
            if (!Engine.IsAlive(target))
            {
                for (int player = 1; player < MaxPlayers; player++)
                {
                    if (!Engine.IsAlive(player)) continue;

                    if (GetPlayerTeam(owner) == GetPlayerTeam(player)) continue;

                    var pPlayer = Engine.EntOfIndex(player);
                    targetOrigin[0] = pPlayer->v.origin.x;
                    targetOrigin[1] = pPlayer->v.origin.y;
                    targetOrigin[2] = pPlayer->v.origin.z;
                    if (IsWallBetweenPoints(origin, targetOrigin, entity)) continue;

                    float distance = VectorMath.Distance(origin, targetOrigin);
                    if (distance < maxDis)
                    {
                        maxDis = distance;
                        tempTarget = player;
                    }
                }
                if (maxDis <= ProjectileTraceRadius[owner, i])
                {
                    var pTemp = Engine.EntOfIndex(tempTarget);
                    pev->euser1 = pTemp;
                }
            }
            else
            {
                var pTarget = Engine.EntOfIndex(target);
                targetOrigin[0] = pTarget->v.origin.x;
                targetOrigin[1] = pTarget->v.origin.y;
                targetOrigin[2] = pTarget->v.origin.z;
                VectorMath.GetSpeedVector(origin, targetOrigin, ProjectileFlySpeed[owner, i], velocity);
                EntityTurn(entity, targetOrigin);
                pev->velocity.x = velocity[0];
                pev->velocity.y = velocity[1];
                pev->velocity.z = velocity[2];
            }
        }
        else if (ProjectileTraceMode[owner, i] == TraceModeAim)
        {
            VectorMath.GetAimOrigin(owner, targetOrigin);
            VectorMath.GetSpeedVector(origin, targetOrigin, ProjectileFlySpeed[owner, i], velocity);
            EntityTurn(entity, targetOrigin);
            pev->velocity.x = velocity[0];
            pev->velocity.y = velocity[1];
            pev->velocity.z = velocity[2];
        }
    }

    // ---- 碰撞 ----

    private static void TouchLogic(edict_t* entity, edict_t* other)
    {
        if (!Engine.IsValid(entity)) return;

        string globalName = Engine.GlobalName(entity);
        if (globalName != WpnProjectileName) return;

        if (Engine.IsValid(other))
        {
            string otherName = Engine.GlobalName(other);
            if (otherName == globalName) return;
        }

        var pev = &entity->v;
        Span<float> origin = stackalloc float[3] { pev->origin.x, pev->origin.y, pev->origin.z };
        int owner = Engine.IndexOf(pev->owner);
        int i = Engine.GetPDataInt(entity, WeaponIdPData);
        int sprEnt = pev->iuser4;

        // 碰撞爆炸
        if (WeaponDamageType[owner, i] == DamageTypeTouchExp)
        {
            int explodeFlag = 0;
            if (ProjectileExplodeEffect[owner, i] == 0)
                explodeFlag = EngineConst.TeExplFlagNoDlights | EngineConst.TeExplFlagNoParticles;
            else
            {
                Engine.MessageBegin(0, 23, null, null);
                Engine.WriteByte(EngineConst.TeWorldDecal);
                Engine.WriteCoord(origin[0]);
                Engine.WriteCoord(origin[1]);
                Engine.WriteCoord(origin[2]);
                Engine.WriteByte(Engine.RandomNum(46, 48));
                Engine.MessageEnd();
            }
            if (ProjectileExplodeSound[owner, i] != DmgSoundDefault)
            {
                explodeFlag |= EngineConst.TeExplFlagNoSound;
                if (ProjectileExplodeSound[owner, i] == DmgSoundCustom)
                    Engine.EmitSound(entity, 1, ProjectileExplodeSoundPath[owner, i], 1.0f, 0.8f, 0, 100);
            }
            Engine.MessageBegin(0, 23, null, null);
            Engine.WriteByte(EngineConst.TeExplosion);
            Engine.WriteCoord(origin[0]);
            Engine.WriteCoord(origin[1]);
            Engine.WriteCoord(origin[2] + ProjectileExpHeightOfs[owner, i]);
            Engine.WriteShort(ProjectileExplodeSprite[owner, i]);
            Engine.WriteByte(FloatMath.Round(ProjectileExpScale[owner, i] * 10.0f));
            Engine.WriteByte(FloatMath.Round(ProjectileExpFramerate[owner, i] * 10.0f));
            Engine.WriteByte(explodeFlag);
            Engine.MessageEnd();

            Span<float> param = stackalloc float[6];
            param[0] = ProjectileDamageRadius[owner, i];
            param[5] = WeaponDamage[owner, i];
            Wpn.ExecuteAttack(owner, 0, Engine.IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeExplosion, WpnConst.SpdmgCantHeadshot);
            if (Engine.IsValid(Engine.EntOfIndex(sprEnt))) Engine.RemoveEntity(Engine.EntOfIndex(sprEnt));
        }
        else if (WeaponDamageType[owner, i] == DamageTypeTouch)
        {
            Span<float> param = stackalloc float[6];
            param[5] = WeaponDamage[owner, i];
            Wpn.ExecuteAttack(owner, Engine.IndexOf(other), Engine.IndexOf(entity), param.ToArray(), origin.ToArray(), WpnConst.DmgTypeTouch, WpnConst.SpdmgCantHeadshot);
        }
        if (Engine.IsValid(Engine.EntOfIndex(sprEnt))) Engine.RemoveEntity(Engine.EntOfIndex(sprEnt));
        Engine.RemoveEntity(entity);
    }

    // ---- 创建抛射物 ----

    private static edict_t* CreateProjectile(int id, int i, string className, Span<float> origin, Span<float> targetOrigin)
    {
        int entityType = 0;
        edict_t* entity;

        // 抛射物模型
        if (CheckSuffix(ProjectileModelPath[id, i], "mdl") || CheckSuffix(ProjectileModelPath[id, i], "MDL"))
        {
            entityType = ProjectileModel;
            entity = CreateNamedEntityByIndex(InfoTarget);
        }
        else if (CheckSuffix(ProjectileModelPath[id, i], "spr") || CheckSuffix(ProjectileModelPath[id, i], "SPR"))
        {
            entityType = ProjectileSprite;
            entity = CreateNamedEntityByIndex(EnvSprite);
        }
        else return null;

        if (!Engine.IsValid(entity)) return null;

        Engine.SetModel(entity, ProjectileModelPath[id, i]);
        var pev = &entity->v;

        // 抛射物重力
        if (ProjectileGravity[id, i] == 0.0f) pev->movetype = 5;  // MOVETYPE_FLY
        else pev->movetype = 6;                                    // MOVETYPE_TOSS
        pev->gravity = ProjectileGravity[id, i];

        // 飞行速度 & 飞行高度修正
        Span<float> velocity = stackalloc float[3];
        VectorMath.GetSpeedVector(origin, targetOrigin, ProjectileFlySpeed[id, i], velocity);
        if (ProjectileGravity[id, i] > 0.0f) velocity[2] += ProjectileHeightOfs[id, i];
        pev->velocity.x = velocity[0];
        pev->velocity.y = velocity[1];
        pev->velocity.z = velocity[2];

        // 延迟时间
        if (WeaponDamageType[id, i] == DamageTypeDelayExp)
            pev->dmgtime = Engine.GameTime + ProjectileDelay[id, i];

        // 存在时间
        if (ProjectileDuration[id, i] > 0.0f)
            pev->fuser4 = Engine.GameTime + ProjectileDuration[id, i];

        // 其他设置
        var pPlayer = Engine.EntOfIndex(id);
        float angleX = pPlayer != null ? -pPlayer->v.v_angle.x : 0.0f;
        float angleY = pPlayer != null ? pPlayer->v.v_angle.y : 0.0f;
        float angleZ = pPlayer != null ? pPlayer->v.v_angle.z : 0.0f;
        Engine.SetPDataInt(entity, WeaponIdPData, i);
        pev->solid = 1; // SOLID_TRIGGER
        entity->v.classname = Engine.AllocStr(className);
        entity->v.globalname = Engine.AllocStr(WpnProjectileName);
        pev->origin.x = origin[0];
        pev->origin.y = origin[1];
        pev->origin.z = origin[2];
        pev->angles.x = angleX;
        pev->angles.y = angleY;
        pev->angles.z = angleZ;
        pev->owner = pPlayer;
        pev->mins = new vec3_t { x = -1, y = -1, z = -1 };
        pev->maxs = new vec3_t { x = 1, y = 1, z = 1 };
        pev->nextthink = Engine.GameTime + 0.01f;
        pev->iuser2 = entityType;
        pev->framerate = ProjectileFramerate[id, i];
        if (entityType == ProjectileSprite)
        {
            pev->rendermode = 5; // kRenderTransAdd
            pev->renderamt = ProjectileBrightness[id, i];
            pev->rendercolor = new vec3_t { x = 255, y = 255, z = 255 };
            pev->scale = ProjectileScale[id, i];
            int modelIndex = Engine.ModelIndex(ProjectileModelPath[id, i]);
            int frames = Engine.ModelFrames(modelIndex);
            pev->iuser3 = frames;
        }
        else pev->animtime = Engine.GameTime;

        // 抛射物拖尾
        if (ProjectileHasTail[id, i] != 0)
        {
            Engine.MessageBegin(0, 23, null, null);
            Engine.WriteByte(EngineConst.TeBeamFollow);
            Engine.WriteShort(Engine.IndexOf(entity));
            Engine.WriteShort(SprBeamFollow);
            Engine.WriteByte(10);
            Engine.WriteByte(3);
            Engine.WriteByte(ProjectileTailColor[id, i, 0]);
            Engine.WriteByte(ProjectileTailColor[id, i, 1]);
            Engine.WriteByte(ProjectileTailColor[id, i, 2]);
            Engine.WriteByte(ProjectileTailColor[id, i, 3]);
            Engine.MessageEnd();
        }

        // 拖尾间隔SPR
        if (ProjectileTailSprite[id, i] > 0)
            pev->fuser1 = Engine.GameTime + ProjectileTailTime[id, i];

        // 抛射物附带SPR
        if (ProjectileExtraSprite[id, i] is { Length: > 0 })
        {
            var sprEnt = CreateNamedEntityByIndex(EnvSprite);
            if (Engine.IsValid(sprEnt))
            {
                var sprPev = &sprEnt->v;
                sprPev->globalname = Engine.AllocStr(WpnProjectileExtra);
                sprPev->movetype = 12; // MOVETYPE_FOLLOW
                sprPev->rendermode = 5; // kRenderTransAdd
                sprPev->renderamt = 255.0f;
                sprPev->rendercolor = new vec3_t { x = 255, y = 255, z = 255 };
                sprPev->aiment = entity;
                sprPev->framerate = ExtraSpriteFramerate[id, i];
                sprPev->scale = ExtraSpriteScale[id, i];
                Engine.SetModel(sprEnt, ProjectileExtraSprite[id, i]);
                int modelIndex = Engine.ModelIndex(ProjectileExtraSprite[id, i]);
                int frames = Engine.ModelFrames(modelIndex);
                sprPev->iuser1 = frames;
                pev->iuser4 = Engine.IndexOf(sprEnt);
            }
        }

        // 抛射物发光
        if (ProjectileGlow[id, i] != 0)
        {
            Engine.MessageBegin(3, 23, origin, null);
            Engine.WriteByte(EngineConst.TeDlight);
            Engine.WriteCoord(origin[0]);
            Engine.WriteCoord(origin[1]);
            Engine.WriteCoord(origin[2]);
            Engine.WriteByte(ProjectileGlowRadius[id, i] / 10);
            Engine.WriteByte(ProjectileGlowColor[id, i, 0]);
            Engine.WriteByte(ProjectileGlowColor[id, i, 1]);
            Engine.WriteByte(ProjectileGlowColor[id, i, 2]);
            Engine.WriteByte(1);
            Engine.WriteByte(0);
            Engine.MessageEnd();

            Engine.MessageBegin(3, 23, origin, null);
            Engine.WriteByte(EngineConst.TeElight);
            Engine.WriteShort(Engine.IndexOf(entity));
            Engine.WriteCoord(origin[0]);
            Engine.WriteCoord(origin[1]);
            Engine.WriteCoord(origin[2]);
            Engine.WriteCoord(10.0f);
            Engine.WriteByte(ProjectileGlowColor[id, i, 0]);
            Engine.WriteByte(ProjectileGlowColor[id, i, 1]);
            Engine.WriteByte(ProjectileGlowColor[id, i, 2]);
            Engine.WriteByte(2);
            Engine.WriteCoord(0.0f);
            Engine.MessageEnd();

            pev->fuser2 = Engine.GameTime + 0.01f;
        }

        return entity;
    }

    private static edict_t* CreateNamedEntityByIndex(int stringIndex) => Engine.EF.pfnCreateNamedEntity(stringIndex);

}
