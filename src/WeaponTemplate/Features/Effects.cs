using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// 血液与枪口烟雾等临时效果实体/消息。
public static unsafe class Effects
{
    // stock SpawnBlood(origin, color, amount)
    public static void SpawnBlood(Span<float> vecOrigin, int color, int amount)
    {
        if (amount == 0) return;
        if (color == 0) return;

        int result = NativeFunctionRegister.Execute_WpnHookSpawnBlood(color);
        if (result == WpnConst.WPN_SUPERCEDE) return;

        amount *= 2;
        if (amount > 255) amount = 255;
        fixed (float* pOrigin = vecOrigin)
        {
            Eng.MessageBegin(EngineConst.MsgPvs, EngineConst.SvcTempEntity, pOrigin, null);
            Eng.WriteByte(EngineConst.TeBloodSprite);
            Eng.WriteCoord(vecOrigin[0]);
            Eng.WriteCoord(vecOrigin[1]);
            Eng.WriteCoord(vecOrigin[2]);
            Eng.WriteShort(WeaponStore.BloodSpray);
            Eng.WriteShort(WeaponStore.BloodDrop);
            Eng.WriteByte(color);
            Eng.WriteByte(Math.Min(Math.Max(3, amount / 10), 16));
            Eng.MessageEnd();
        }
    }

    // FakeSmoke(flEnd[3]) / public Native_FakeSmoke
    public static void FakeSmoke(Span<float> flEnd)
    {
        var entity = Eng.CreateNamedEntity("env_sprite");
        if (entity == null) return;
        var pev = &entity->v;
        pev->movetype = EngineConst.MoveTypeFly;
        pev->rendermode = EngineConst.KRenderTransAdd;
        pev->renderamt = 64.0f;
        pev->rendercolor = new vec3_t { x = 128.0f, y = 128.0f, z = 128.0f };
        pev->scale = 0.5f;
        pev->nextthink = Eng.GameTime + 0.05f;
        Eng.SetClassName(entity, "gunsmoke1");
        pev->modelindex = WeaponStore.SmokeId;
        pev->mins = new vec3_t { x = -1, y = -1, z = -1 };
        pev->maxs = new vec3_t { x = 1, y = 1, z = 1 };
        pev->origin.x = flEnd[0]; pev->origin.y = flEnd[1]; pev->origin.z = flEnd[2];
        pev->gravity = 0.01f;
        pev->solid = EngineConst.SolidTrigger;
        pev->frame = 0.0f;

        int num = Eng.RandomNum(0, 1);
        float x, y;
        if (num != 0)
        {
            x = Eng.RandomFloat(10.0f, 20.0f);
            y = Eng.RandomFloat(10.0f, 20.0f);
        }
        else
        {
            x = Eng.RandomFloat(-20.0f, -10.0f);
            y = Eng.RandomFloat(-20.0f, -10.0f);
        }
        pev->iuser1 = num;
        pev->velocity = new vec3_t { x = x, y = y, z = 50.0f };
    }
}
