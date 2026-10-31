using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// 武器开火/命中的表现效果:血液(伤害命中)、弹着点贴图与枪口烟雾(墙面粉尘)。
// 包含对应的引擎/消息入口:pfnThink(烟雾精灵逐帧动画)与 wt_TempEntity(血液颜色改写)。
public static unsafe partial class WeaponEffects
{
    [AmxxCallback("wt_TraceAttackPost")]
    public static void OnTraceAttackPost(int entityIndex, int attacker, float damage, [AmxxBuffer(3)] Span<float> direction, int traceHandle, int damageType)
    {
        if (!Engine.IsAlive(attacker)) return;

        var pAttacker = Engine.EntOfIndex(attacker);
        var activeItem = pAttacker == null ? null : Engine.GetPDataCbase(pAttacker, 373);
        int i = Engine.IsValid(activeItem) ? WeaponStore.GetWeaponType(activeItem) : 0;
        if (i == 0) return;

        if (WeaponStore.Weapon(attacker, i).Id == EngineConst.CswKnife) return;

        // ham 传入的是 fakemeta trace 句柄
        var endPos = new float[3];
        FakeMeta.GetTr2(traceHandle, 5, endPos); // TR_vecEndPos = 5

        if (entityIndex != 0)
        {
            Engine.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
            Engine.WriteByte(EngineConst.TeDecal);
            Engine.WriteCoord(endPos[0]);
            Engine.WriteCoord(endPos[1]);
            Engine.WriteCoord(endPos[2]);
            Engine.WriteByte(GameTables.GunshotDecals[Engine.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
            Engine.WriteShort(entityIndex);
            Engine.MessageEnd();
        }
        else
        {
            Engine.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
            Engine.WriteByte(EngineConst.TeWorldDecal);
            Engine.WriteCoord(endPos[0]);
            Engine.WriteCoord(endPos[1]);
            Engine.WriteCoord(endPos[2]);
            Engine.WriteByte(GameTables.GunshotDecals[Engine.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
            Engine.MessageEnd();
        }
        Engine.MessageBegin(EngineConst.MsgBroadcast, EngineConst.SvcTempEntity, null, null);
        Engine.WriteByte(EngineConst.TeGunshotDecal);
        Engine.WriteCoord(endPos[0]);
        Engine.WriteCoord(endPos[1]);
        Engine.WriteCoord(endPos[2]);
        Engine.WriteShort(attacker);
        Engine.WriteByte(GameTables.GunshotDecals[Engine.RandomNum(0, GameTables.GunshotDecals.Length - 1)]);
        Engine.MessageEnd();
        endPos[2] += 5.0f;
        FakeSmoke(endPos);
    }

    [AmxxCallback("wt_TempEntity")]
    public static int OnTempEntity(int msgId, int msgDest, int msgEntity)
    {
        if (WeaponStore.BloodColor == 0) return EngineConst.PluginContinue;

        // 设置血液颜色
        int first = AmxModX.GetMsgArgInt(1);
        if (first == EngineConst.TeBloodSprite || first == EngineConst.TeBlood || first == EngineConst.TeBloodstream)
        {
            int color = AmxModX.GetMsgArgInt(7);
            int result = NativeFunctionRegister.Execute_WpnHookSpawnBlood(color);
            if (result == WpnConst.WPN_SUPERCEDE) return EngineConst.PluginHandled;
            AmxModX.SetMsgArgInt(7, EngineConst.ArgByte, WeaponStore.BloodColor);
        }
        return EngineConst.PluginContinue;
    }

    // ---- fm_Think_Post(枪口烟雾动画:FakeSmoke 创建的 gunsmoke1 精灵逐帧推进) ----

    [AmxxHook("pfnThink", Post = true)]
    public static void ThinkPost(edict_t* pEntity)
    {
        HookResult.Ignore();
        if (!Engine.IsValid(pEntity)) return;

        string className = Engine.ClassName(pEntity);
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
            velocityX -= Engine.RandomFloat(1.0f, 5.0f);
            velocityY -= Engine.RandomFloat(1.0f, 5.0f);
        }
        else
        {
            velocityX += Engine.RandomFloat(1.0f, 5.0f);
            velocityY += Engine.RandomFloat(1.0f, 5.0f);
        }
        frame = MathF.Min(10.0f, frame);
        scale = MathF.Min(1.0f, scale);
        pev->frame = frame;
        pev->scale = scale;
        pev->velocity.x = velocityX;
        pev->velocity.y = velocityY;
        pev->nextthink = Engine.GameTime + nextThink;
        if (frame == 10.0f) Engine.RemoveEntity(pEntity);
    }

    // stock SpawnBlood(origin, color, amount)
    public static void SpawnBlood(Span<float> vecOrigin, int color, int amount)
    {
        if (amount == 0) return;
        if (color == 0) return;

        int result = NativeFunctionRegister.Execute_WpnHookSpawnBlood(color);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        amount *= 2;
        if (amount > 255) amount = 255;
        Engine.MessageBegin(EngineConst.MsgPvs, EngineConst.SvcTempEntity, vecOrigin, null);
        Engine.WriteByte(EngineConst.TeBloodSprite);
        Engine.WriteCoord(vecOrigin[0]);
        Engine.WriteCoord(vecOrigin[1]);
        Engine.WriteCoord(vecOrigin[2]);
        Engine.WriteShort(WeaponStore.BloodSpray);
        Engine.WriteShort(WeaponStore.BloodDrop);
        Engine.WriteByte(color);
        Engine.WriteByte(Math.Min(Math.Max(3, amount / 10), 16));
        Engine.MessageEnd();
    }

    // FakeSmoke(flEnd[3]) / public Native_FakeSmoke
    public static void FakeSmoke(Span<float> flEnd)
    {
        var entity = Engine.CreateNamedEntity("env_sprite");
        if (entity == null) return;
        var pev = &entity->v;
        pev->movetype = EngineConst.MoveTypeFly;
        pev->rendermode = EngineConst.KRenderTransAdd;
        pev->renderamt = 64.0f;
        pev->rendercolor = new vec3_t { x = 128.0f, y = 128.0f, z = 128.0f };
        pev->scale = 0.5f;
        pev->nextthink = Engine.GameTime + 0.05f;
        Engine.SetClassName(entity, "gunsmoke1");
        pev->modelindex = WeaponStore.SmokeId;
        pev->mins = new vec3_t { x = -1, y = -1, z = -1 };
        pev->maxs = new vec3_t { x = 1, y = 1, z = 1 };
        pev->origin.x = flEnd[0]; pev->origin.y = flEnd[1]; pev->origin.z = flEnd[2];
        pev->gravity = 0.01f;
        pev->solid = EngineConst.SolidTrigger;
        pev->frame = 0.0f;

        int num = Engine.RandomNum(0, 1);
        float x, y;
        if (num != 0)
        {
            x = Engine.RandomFloat(10.0f, 20.0f);
            y = Engine.RandomFloat(10.0f, 20.0f);
        }
        else
        {
            x = Engine.RandomFloat(-20.0f, -10.0f);
            y = Engine.RandomFloat(-20.0f, -10.0f);
        }
        pev->iuser1 = num;
        pev->velocity = new vec3_t { x = x, y = y, z = 50.0f };
    }
}
