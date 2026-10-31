using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// WeaponTemplate.sma 中的业务函数(GiveWeapon/CheckAttack/GivePriAmmo/...)翻译。
// Pawn 的实体下标语义在 C# 中用 edict_t* + Eng.IndexOf 表达。
public static unsafe class Core
{
    // m_bIsVIP：Windows 字节偏移 837，落在 pdata 单元 209 的第 2 个字节
    private const int VipCell = 209;
    private const int VipBit = 0x100;

    // ---- 向量小工具(替代 xs 库) ----

    public static void VecAdd(Span<float> a, Span<float> b, Span<float> dst)
    {
        dst[0] = a[0] + b[0]; dst[1] = a[1] + b[1]; dst[2] = a[2] + b[2];
    }

    public static void VecSub(Span<float> a, Span<float> b, Span<float> dst)
    {
        dst[0] = a[0] - b[0]; dst[1] = a[1] - b[1]; dst[2] = a[2] - b[2];
    }

    public static void VecMulScalar(Span<float> a, float scalar, Span<float> dst)
    {
        dst[0] = a[0] * scalar; dst[1] = a[1] * scalar; dst[2] = a[2] * scalar;
    }

    public static float Distance(Span<float> a, Span<float> b)
    {
        float dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    // HLSDK AngleVectors(对应 fakemeta angle_vector,mode: 1=forward 2=right 3=up)
    public static void AngleVector(Span<float> angles, int mode, Span<float> dest)
    {
        const float Deg2Rad = MathF.PI / 180.0f;
        float sp = MathF.Sin(angles[0] * Deg2Rad), cp = MathF.Cos(angles[0] * Deg2Rad);
        float sy = MathF.Sin(angles[1] * Deg2Rad), cy = MathF.Cos(angles[1] * Deg2Rad);
        float sr = MathF.Sin(angles[2] * Deg2Rad), cr = MathF.Cos(angles[2] * Deg2Rad);
        switch (mode)
        {
            case EngineConst.AngleVectorForward:
                dest[0] = cp * cy; dest[1] = cp * sy; dest[2] = -sp;
                break;
            case EngineConst.AngleVectorRight:
                dest[0] = -sr * sp * cy + cr * sy;
                dest[1] = -sr * sp * sy - cr * cy;
                dest[2] = -sr * cp;
                break;
            case EngineConst.AngleVectorUp:
                dest[0] = cr * sp * cy + sy * sr;
                dest[1] = cr * sp * sy - cy * sr;
                dest[2] = cr * cp;
                break;
        }
    }

    // stock get_position(id, forw, right, up, vStart[])
    public static void GetPosition(int id, float forward, float right, float up, Span<float> start)
    {
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return;
        var pev = pPlayer->v;

        Span<float> origin = stackalloc float[3] { pev.origin.x, pev.origin.y, pev.origin.z };
        Span<float> viewOfs = stackalloc float[3] { pev.view_ofsr[0], pev.view_ofsr[1], pev.view_ofsr[2] };
        VecAdd(origin, viewOfs, origin);

        Span<float> angles = stackalloc float[3] { pev.v_angle.x, pev.v_angle.y, pev.v_angle.z };
        Span<float> vForward = stackalloc float[3];
        Span<float> vRight = stackalloc float[3];
        Span<float> vUp = stackalloc float[3];
        AngleVector(angles, EngineConst.AngleVectorForward, vForward);
        AngleVector(angles, EngineConst.AngleVectorRight, vRight);
        AngleVector(angles, EngineConst.AngleVectorUp, vUp);

        start[0] = origin[0] + vForward[0] * forward + vRight[0] * right + vUp[0] * up;
        start[1] = origin[1] + vForward[1] * forward + vRight[1] * right + vUp[1] * up;
        start[2] = origin[2] + vForward[2] * forward + vRight[2] * right + vUp[2] * up;
    }

    // stock fm_get_aim_origin(id, origin[])
    public static void GetAimOrigin(int id, Span<float> origin)
    {
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return;
        var pev = pPlayer->v;

        Span<float> start = stackalloc float[3] { pev.origin.x, pev.origin.y, pev.origin.z };
        Span<float> viewOfs = stackalloc float[3] { pev.view_ofsr[0], pev.view_ofsr[1], pev.view_ofsr[2] };
        VecAdd(start, viewOfs, start);

        Span<float> angles = stackalloc float[3] { pev.v_angle.x, pev.v_angle.y, pev.v_angle.z };
        Eng.MakeVectors(angles);
        Span<float> dest = stackalloc float[3] { Eng.GP->v_forward.x, Eng.GP->v_forward.y, Eng.GP->v_forward.z };
        VecMulScalar(dest, 9999.0f, dest);
        VecAdd(start, dest, dest);

        TraceResult trace = default;
        Eng.TraceLine(start, dest, 0, pPlayer, &trace);
        origin[0] = trace.vecEndPos.x;
        origin[1] = trace.vecEndPos.y;
        origin[2] = trace.vecEndPos.z;
    }

    // stock GetVelocityFromOrigin(origin1, origin2, speed, velocity)
    public static void GetVelocityFromOrigin(Span<float> origin1, Span<float> origin2, float speed, Span<float> velocity)
    {
        VecSub(origin1, origin2, velocity);
        float dx = origin1[0] - origin2[0], dy = origin1[1] - origin2[1], dz = origin1[2] - origin2[2];
        float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        float valve = distance / speed;
        VecMulScalar(velocity, 1.0f / valve, velocity);
    }

    // stock get_speed_vector(WeaponExtend.sma 内同名的私有实现)
    public static void GetSpeedVector(Span<float> origin1, Span<float> origin2, float speed, Span<float> newVelocity)
    {
        newVelocity[0] = origin2[0] - origin1[0];
        newVelocity[1] = origin2[1] - origin1[1];
        newVelocity[2] = origin2[2] - origin1[2];
        float len = MathF.Sqrt(newVelocity[0] * newVelocity[0] + newVelocity[1] * newVelocity[1] + newVelocity[2] * newVelocity[2]);
        float num = MathF.Sqrt(speed * speed / (len * len));
        newVelocity[0] *= num;
        newVelocity[1] *= num;
        newVelocity[2] *= num;
    }

    // ---- 金钱 / 备弹 / 焦距(pdata stock) ----

    public static int GetUserMoney(int id)
    {
        var p = Eng.EntOfIndex(id);
        return p == null ? 0 : Eng.GetPDataInt(p, 115);
    }

    // stock fm_set_user_money(id, iMoney, iFlash = 1)
    public static void SetUserMoney(int id, int money, int flash = 1)
    {
        var p = Eng.EntOfIndex(id);
        if (p == null) return;
        money = Math.Min(money, Store.MaxMoney);
        Eng.SetPDataInt(p, 115, money);
        int msg = Eng.UserMsgId("Money");
        Eng.MessageBegin(EngineConst.MsgOne, msg, null, p);
        Eng.WriteLong(money);
        Eng.WriteByte(flash);
        Eng.MessageEnd();
    }

    public static int GetUserBpAmmo(int id, int weaponId)
    {
        int offset = BpAmmoOffset(weaponId);
        if (offset < 0) return 0;
        var p = Eng.EntOfIndex(id);
        return p == null ? 0 : Eng.GetPDataInt(p, offset);
    }

    public static void SetUserBpAmmo(int id, int weaponId, int amount)
    {
        int offset = BpAmmoOffset(weaponId);
        if (offset < 0)
        {
            MF.Log($"fm_cs_set_user_bpammo: Invalid weapon id {weaponId}");
            return;
        }
        var p = Eng.EntOfIndex(id);
        if (p == null) return;
        Eng.SetPDataInt(p, offset, amount);
    }

    // 原版 fm_cs_(get|set)_user_bpammo 的偏移表
    private static int BpAmmoOffset(int weaponId) => weaponId switch
    {
        EngineConst.CswAwp => 377,
        EngineConst.CswScout or EngineConst.CswAk47 or EngineConst.CswG3sg1 => 378,
        EngineConst.CswM249 => 379,
        EngineConst.CswM4a1 or EngineConst.CswFamas or EngineConst.CswAug or EngineConst.CswSg550 or EngineConst.CswGali or EngineConst.CswSg552 => 380,
        EngineConst.CswM3 or EngineConst.CswXm1014 => 381,
        EngineConst.CswUsp or EngineConst.CswUmp45 or EngineConst.CswMac10 => 382,
        EngineConst.CswFiveseven or EngineConst.CswP90 => 383,
        EngineConst.CswDeagle => 384,
        EngineConst.CswP228 => 385,
        EngineConst.CswGlock18 or EngineConst.CswMp5Navy or EngineConst.CswTmp or EngineConst.CswElite => 386,
        EngineConst.CswFlashbang => 387,
        EngineConst.CswHegrenade => 388,
        EngineConst.CswSmokegrenade => 389,
        EngineConst.CswC4 => 390,
        _ => -1,
    };

    public static int GetUserZoom(int id)
    {
        var p = Eng.EntOfIndex(id);
        if (p == null) return 0;
        int value = Eng.GetPDataInt(p, 363);
        return value switch
        {
            0x5A => 1,
            0x28 => 2,
            0xF => 3,
            0xA => 3,
            0x37 => 4,
            _ => 0,
        };
    }

    // stock PlayAnim(id, anim)
    public static void PlayAnim(int id, int anim)
    {
        var p = Eng.EntOfIndex(id);
        if (p == null) return;
        p->v.weaponanim = anim;
        Eng.MessageBegin(EngineConst.MsgOne, EngineConst.SvcWeaponAnim, null, p);
        Eng.WriteByte(anim);
        Eng.WriteByte(p->v.body);
        Eng.MessageEnd();
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
        fixed (float* pOrigin = vecOrigin)
        {
            Eng.MessageBegin(EngineConst.MsgPvs, EngineConst.SvcTempEntity, pOrigin, null);
            Eng.WriteByte(EngineConst.TeBloodSprite);
            Eng.WriteCoord(vecOrigin[0]);
            Eng.WriteCoord(vecOrigin[1]);
            Eng.WriteCoord(vecOrigin[2]);
            Eng.WriteShort(Store.BloodSpray);
            Eng.WriteShort(Store.BloodDrop);
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
        pev->modelindex = Store.SmokeId;
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

    // public Event_CurWeapon(id) — 同时被 CurWeapon 事件与 Ham_UseStationary 复用
    public static void EventCurWeapon(int id)
    {
        if (!Eng.IsAlive(id)) return;

        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return;
        var pEntity = Eng.GetPDataCbase(pPlayer, 373);
        if (!Eng.IsValid(pEntity)) return;

        int i = Store.GetWeaponType(pEntity);
        if (i == 0) return;

        // 开镜时取消V模型
        if (GetUserZoom(id) == 2 || GetUserZoom(id) == 3) Eng.SetViewModel(pPlayer, "");
        else Eng.SetViewModel(pPlayer, Store.WeaponVModel[id, i]);
        Eng.SetWeaponModel(pPlayer, Store.WeaponPModel[id, i]);

        // 无弹夹子弹的武器
        if (Store.WeaponClip[id, i] == 0 && Store.WeaponId[id, i] != EngineConst.CswHegrenade && Store.WeaponId[id, i] != EngineConst.CswKnife)
        {
            int clip = Eng.GetPDataInt(pEntity, 51);
            SetUserBpAmmo(id, Store.WeaponId[id, i], clip);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("CurWeapon"), null, pPlayer);
            Eng.WriteByte(1);
            Eng.WriteByte(Store.WeaponId[id, i]);
            Eng.WriteByte(0xFF); // Pawn write_byte(-1) 的单字节效果
            Eng.MessageEnd();
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoX"), null, pPlayer);
            Eng.WriteByte(Store.GameWeaponAmmoId[Store.WeaponId[id, i]]);
            Eng.WriteByte(clip);
            Eng.MessageEnd();
        }

        // 机瞄V模型
        if (Store.WeaponSight[id, i] != 0 && Store.SightMode[id] == WpnConst.Open)
            Eng.SetViewModel(pPlayer, Store.WeaponVModelS[id, i]);
    }

    // public Event_Round_Start(HLTV)
    public static void EventRoundStart()
    {
        Store.DisableBuyTime = Eng.GameTime + Eng.CvarFloat("mp_buytime") * 60.0f;
        Store.RoundStart = false;
        Store.Round++;
        if (Store.RoundRestart)
        {
            Store.RoundRestart = false;
            for (int id = 1; id < 33; id++)
            {
                for (int i = 1; i < WpnConst.MaxWeapon; i++) Store.CurWeaponBPAmmo[id, i] = 0;
                for (int i = 0; i < 32; i++) Store.GameCurWeaponBPAmmo[id, i] = 0;
            }
        }
    }

    // stock ChangeWeaponList(id, iEntity)
    public static void ChangeWeaponList(int id, edict_t* entity)
    {
        if (!Eng.IsValid(entity)) return;

        int i = Store.GetWeaponType(entity);
        int wpnId;
        string listName;
        if (i != 0)
        {
            wpnId = Store.WeaponId[id, i];
            listName = $"weapon_{Store.WeaponList[id, i]}";
        }
        else
        {
            wpnId = Eng.GetPDataInt(entity, 43);
            listName = wpnId >= 0 && wpnId < Store.GameWeaponClassName.Length ? Store.GameWeaponClassName[wpnId] : "";
        }

        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return;
        Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("WeaponList"), null, pPlayer);
        Eng.WriteString(listName);
        if (wpnId == EngineConst.CswKnife)
        {
            Eng.WriteByte(0xFF); // -1
            Eng.WriteByte(0xFF);
        }
        else if (wpnId == EngineConst.CswHegrenade)
        {
            Eng.WriteByte(Store.GameWeaponAmmoId[wpnId]);
            Eng.WriteByte(Store.GameWeaponMaxBpammo[wpnId]);
        }
        else
        {
            Eng.WriteByte(Store.GameWeaponAmmoId[wpnId]);
            Eng.WriteByte(i != 0 ? Store.WeaponAmmo[id, i] : Store.GameWeaponMaxBpammo[wpnId]);
        }
        Eng.WriteByte(0xFF); // -1
        Eng.WriteByte(0xFF); // -1
        Eng.WriteByte(Store.GameWeaponPosition[wpnId]);
        Eng.WriteByte(Store.GameWeaponSlot[wpnId]);
        Eng.WriteByte(wpnId);
        Eng.MessageEnd();
    }

    // public CheckBuy(id)
    public static bool CheckBuy(int id)
    {
        if (Store.BlockBuy[id] == WpnConst.BuyCannot) return false;
        if (Store.BlockBuy[id] == WpnConst.BuyEverywhere && Eng.IsAlive(id)) return true;
        if (!Eng.IsAlive(id)) return false;

        var p = Eng.EntOfIndex(id);
        if (p == null) return false;

        if ((Eng.GetPDataInt(p, 235) & (1 << 0)) == 0 && !Eng.IsBot(id)) return false;
        if (Eng.IsBot(id) && !Store.BotBuyWeapon[id]) return false;

        if (Eng.GameTime > Store.DisableBuyTime)
        {
            int seconds = PawnMath.Round(Eng.CvarFloat("mp_buytime") * 60.0f);
            Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("TextMsg"), null, p);
            Eng.WriteByte(1 << 2);
            Eng.WriteString("#Cant_buy");
            Eng.WriteString(seconds.ToString());
            Eng.MessageEnd();
            return false;
        }
        return true;
    }

    // 实体是否真的进了玩家的武器槽(m_rgpPlayerItems[0..5],Windows 单元 367..372)
    public static bool PlayerHasItem(int id, int entityIndex)
    {
        if (entityIndex <= 0) return false;
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return false;
        for (int slot = 367; slot <= 372; slot++)
            if (Eng.IndexOf(Eng.GetPDataCbase(pPlayer, slot)) == entityIndex) return true;
        return false;
    }

    // public GiveWeapon(id, szClassName[], i) — 创建/复用实体发放
    public static edict_t* GiveWeapon(int id, string className, int i)
    {
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return null;

        edict_t* entity;
        if (className == "weapon_knife")
        {
            entity = Eng.GetPDataCbase(pPlayer, 370);
            if (Eng.IsValid(entity))
            {
                int weaponType = Store.GetWeaponType(entity);
                if (i == weaponType)
                {
                    Eng.ClientPrintCenter(id, "#Cstrike_Already_Own_Weapon");
                    return null;
                }
                Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("WeapPickup"), null, pPlayer);
                Eng.WriteByte(EngineConst.CswKnife);
                Eng.MessageEnd();
                Eng.EmitSound(pPlayer, EngineConst.ChanAuto, "items/gunpickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
                Eng.SetPDataInt(entity, 11, i);
                return entity;
            }
        }
        else if (className == "weapon_hegrenade")
        {
            if ((pPlayer->v.weapons & (1 << EngineConst.CswHegrenade)) != 0)
            {
                Eng.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Cannot_Carry_Anymore");
                return null;
            }
        }

        entity = Eng.CreateNamedEntity(className);
        MF.Log($"[diag] give create '{className}' tpl={i} idx={(entity == null ? -1 : Eng.IndexOf(entity))} pdata={(entity == null || entity->pvPrivateData == null ? 0 : 1)}");
        if (!Eng.IsValid(entity))
        {
            MF.Log("[diag] give abort: entity invalid right after CreateNamedEntity");
            return null;
        }
        Eng.SetPDataInt(entity, 11, i);
        int slot = MF.CallNative("ExecuteHam", EngineConst.HamItemItemSlot, Eng.IndexOf(entity));
        var existing = Eng.GetPDataCbase(pPlayer, 365 + slot + 2);
        if (existing != null)
        {
            int weaponType = Store.GetWeaponType(existing);
            string existingClass = Eng.ClassName(existing);
            MF.Log($"[diag] give slot={slot} existing={Eng.IndexOf(existing)} existingTpl={weaponType} existingCls={existingClass}");
            if ((weaponType == i && i != 0 && weaponType != 0 && slot != 4) || (i == 0 && weaponType == 0 && className == existingClass && slot != 4))
            {
                Eng.RemoveEntity(entity);
                MF.Log("[diag] give abort: already own same weapon");
                Eng.ClientPrintCenter(id, "#Cstrike_Already_Own_Weapon");
                return null;
            }
            if (slot < 3)
            {
                MF.Log($"[diag] give drop '{existingClass}'");
                Eng.EngClientCommand(id, "drop", existingClass);
            }
        }
        else MF.Log($"[diag] give slot={slot} existing=none");

        var origin = pPlayer->v.origin;
        entity->v.origin = origin;
        entity->v.spawnflags = EngineConst.SfNoRespawn;

        var gamedll = ModuleRuntime.gpGamedllFuncs->dllapi_table;
        int spawnResult = gamedll->pfnSpawn(entity);
        int entityIndex = Eng.IndexOf(entity);
        MF.Log($"[diag] give spawn idx={entityIndex} ret={spawnResult} csw={Eng.GetPDataInt(entity, 43)} serial={Eng.GetPDataInt(entity, 11)}");

        // CS 的 CBasePlayerItem::DefaultTouch 会拒绝 VIP 拾取非手枪/刀(as_ 刺杀图)，这是游戏规则，不绕过
        bool isVip = (Eng.GetPDataInt(pPlayer, VipCell) & VipBit) != 0;
        gamedll->pfnTouch(entity, pPlayer);

        MF.Log($"[diag] give touch idx={entityIndex} vip={isVip} free={entity->free} csw={Eng.GetPDataInt(entity, 43)} playerPrimary={Eng.IndexOf(Eng.GetPDataCbase(pPlayer, 368))} active={Eng.IndexOf(Eng.GetPDataCbase(pPlayer, 373))} carried=0x{pPlayer->v.weapons:X8}");
        return entity;
    }

    // public Native_GiveWeapon(id, i) — 模板发放入口
    public static int GiveWeaponByTemplate(int id, int i)
    {
        if (!Store.IsTemplate(i) || !Eng.IsAlive(id))
        {
            MF.Log($"[diag] give abort: template={i} amount={Store.WeaponAmount} alive={Eng.IsAlive(id)}");
            return 0;
        }

        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return 0;
        var curEntity = Eng.GetPDataCbase(pPlayer, 373);
        var entity = GiveWeapon(id, Store.GameWeaponClassName[Store.WeaponId[id, i]], i);
        if (!Eng.IsValid(entity))
        {
            MF.Log($"[diag] give abort: GiveWeapon returned invalid (tpl={i} cls={Store.GameWeaponClassName[Store.WeaponId[id, i]]})");
            return 0;
        }

        int csw = Store.WeaponId[id, i];

        // 是武器
        if (csw != EngineConst.CswKnife && csw != EngineConst.CswHegrenade)
        {
            // 两种设置clip，CurWeapon那里会发送消息隐藏弹夹
            if (Store.WeaponClip[id, i] != 0) Eng.SetPDataInt(entity, 51, Store.WeaponClip[id, i]);
            else
            {
                Eng.SetPDataInt(entity, 51, Store.WeaponAmmo[id, i]);
                SetUserBpAmmo(id, csw, Eng.GetPDataInt(entity, 51));
            }
        }
        ChangeWeaponList(id, entity);

        // 如果给的是刀子，且当前武器就是刀子
        if (Eng.GetPDataInt(entity, 43) == EngineConst.CswKnife && Eng.IsValid(curEntity) && Eng.GetPDataInt(curEntity, 43) == EngineConst.CswKnife)
        {
            PlayAnim(id, Store.KnifeAnim[id, i, WpnConst.KnifeAnimDraw]);
            Eng.EmitSound(pPlayer, EngineConst.ChanWeapon, Store.WeaponSound1[id, i], EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
            Eng.SetPDataFloat(pPlayer, 83, Store.WeaponDrawTime[id, i]);
            Eng.SetViewModel(pPlayer, Store.WeaponVModel[id, i]);
            Eng.SetWeaponModel(pPlayer, Store.WeaponPModel[id, i]);
        }

        // 给的是手雷，第二个判断没看懂(原注释)
        if (Eng.GetPDataInt(entity, 43) == EngineConst.CswHegrenade && curEntity != Eng.GetPDataCbase(pPlayer, 373))
        {
            Eng.SetViewModel(pPlayer, Store.WeaponVModel[id, i]);
            Eng.SetWeaponModel(pPlayer, Store.WeaponPModel[id, i]);
            Eng.SetPDataFloat(entity, 48, Store.WeaponDrawTime[id, i]);
        }

        // 给的同种类型（CSW）的不同序号的武器的时候，debug处理
        else if (Store.GetWeaponType(entity) != Store.GetWeaponType(curEntity) && Eng.IsValid(curEntity) && Eng.GetPDataInt(curEntity, 43) == Eng.GetPDataInt(entity, 43))
        {
            const int weaponBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            const int secWeaponBitSum = (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) | (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);

            int entityCsw = Eng.GetPDataInt(entity, 43);
            int curCsw = Eng.GetPDataInt(curEntity, 43);
            if (((1 << entityCsw) & secWeaponBitSum) != 0 && !Eng.IsValid(Eng.GetPDataCbase(pPlayer, 368)))
                MF.CallNative("ExecuteHamB", EngineConst.HamItemDeploy, Eng.IndexOf(entity));
            if (((1 << entityCsw) & secWeaponBitSum) == 0 && ((1 << curCsw) & weaponBitSum) == 0)
                MF.CallNative("ExecuteHamB", EngineConst.HamItemDeploy, Eng.IndexOf(entity));
        }
        return Eng.IndexOf(entity);
    }

    // public GivePriAmmo(id, iEntity, WpnId, iMode, iNative, iGiveAmount)
    public static bool GivePriAmmo(int id, edict_t* entity, int wpnId, int mode, int nativeBuy, int giveAmount)
    {
        Store.BuyingAmmo[id] = true;
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return false;

        int money = Eng.GetPDataInt(pPlayer, 115);
        int weapon = Eng.GetPDataInt(entity, 43);
        int ammo = wpnId != 0 ? Store.CurWeaponBPAmmo[id, wpnId] : Store.GameCurWeaponBPAmmo[id, weapon];
        int maxAmmo = wpnId != 0 ? Store.WeaponAmmo[id, wpnId] : Store.GameWeaponMaxBpammo[weapon];
        int amount, cost;

        for (int i = 0; i < WpnConst.MaxPrim; i++)
        {
            if (mode == 0)
            {
                amount = wpnId != 0 ? Math.Min(Store.WeaponClip[id, wpnId], 30) : Store.PrimAmount[i];
            }
            else amount = 1;
            if (giveAmount != 0) amount = giveAmount;
            cost = wpnId != 0 ? Store.WeaponAmmoCost[id, wpnId] : Store.PrimCost[i];
            if (nativeBuy != 0) cost = 0;

            if (weapon == Store.PrimId[i] && money >= cost)
            {
                if (mode != 0 && Eng.GetPDataInt(entity, 51) >= maxAmmo && wpnId != 0) return false;
                if (mode == 0 && ammo >= maxAmmo && wpnId != 0) return false;
                if (wpnId == 0 && ammo >= maxAmmo) return false;

                if (ammo + amount > maxAmmo) amount = maxAmmo - ammo;
                Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoPickup"), null, pPlayer);
                Eng.WriteByte(Store.GameWeaponAmmoId[weapon]);
                Eng.WriteByte(amount);
                Eng.MessageEnd();
                Eng.EmitSound(entity, EngineConst.ChanWeapon, "items/9mmclip1.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
                ammo += amount;
                money -= cost;
                if (mode != 0) Eng.SetPDataInt(entity, 51, ammo);
                if (Eng.GetPDataCbase(pPlayer, 373) == entity) SetUserBpAmmo(id, weapon, ammo);
                if (wpnId == 0) Store.GameCurWeaponBPAmmo[id, weapon] = ammo;
                else Store.CurWeaponBPAmmo[id, wpnId] = ammo;
                SetUserMoney(id, money);
                return true;
            }
        }
        return false;
    }

    // public GiveSecAmmo(id, iEntity, WpnId, iMode, iNative, iGiveAmount)
    public static bool GiveSecAmmo(int id, edict_t* entity, int wpnId, int mode, int nativeBuy, int giveAmount)
    {
        Store.BuyingAmmo[id] = true;
        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return false;

        int money = Eng.GetPDataInt(pPlayer, 115);
        int weapon = Eng.GetPDataInt(entity, 43);
        int ammo = wpnId != 0 ? Store.CurWeaponBPAmmo[id, wpnId] : Store.GameCurWeaponBPAmmo[id, weapon];
        int maxAmmo = wpnId != 0 ? Store.WeaponAmmo[id, wpnId] : Store.GameWeaponMaxBpammo[weapon];
        int amount, cost;

        for (int i = 0; i < WpnConst.MaxSec; i++)
        {
            if (mode == 0)
            {
                amount = wpnId != 0 ? Math.Min(Store.WeaponClip[id, wpnId], 30) : Store.SecAmount[i];
            }
            else amount = 1;
            if (giveAmount != 0) amount = giveAmount;
            cost = wpnId != 0 ? Store.WeaponAmmoCost[id, wpnId] : Store.SecondCost[i];
            if (nativeBuy != 0) cost = 0;

            if (weapon == Store.SecondId[i] && money >= cost)
            {
                if (mode != 0 && Eng.GetPDataInt(entity, 51) >= maxAmmo && wpnId != 0) return false;
                if (mode == 0 && ammo >= maxAmmo && wpnId != 0) return false;
                if (wpnId == 0 && ammo >= maxAmmo) return false;

                if (ammo + amount > maxAmmo) amount = maxAmmo - ammo;
                Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("AmmoPickup"), null, pPlayer);
                Eng.WriteByte(Store.GameWeaponAmmoId[weapon]);
                Eng.WriteByte(amount);
                Eng.MessageEnd();
                Eng.EmitSound(entity, EngineConst.ChanWeapon, "items/9mmclip1.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
                ammo += amount;
                money -= cost;
                if (mode != 0) Eng.SetPDataInt(entity, 51, ammo);
                if (Eng.GetPDataCbase(pPlayer, 373) == entity) SetUserBpAmmo(id, weapon, ammo);
                if (wpnId == 0) Store.GameCurWeaponBPAmmo[id, weapon] = ammo;
                else Store.CurWeaponBPAmmo[id, wpnId] = ammo;
                SetUserMoney(id, money);
                return true;
            }
        }
        return false;
    }

    // public Native_GiveAmmo(id, iEntity, i, iFull)
    public static void GiveAmmoForWeapon(int id, int entityIndex, int wpnId, int full)
    {
        var entity = Eng.EntOfIndex(entityIndex);
        if (entity == null) return;

        for (int i = 0; i < WpnConst.MaxPrim; i++)
        {
            if (Eng.GetPDataInt(entity, 43) == Store.PrimId[i])
            {
                if (Store.WeaponClip[id, wpnId] == 0)
                {
                    if (wpnId != 0)
                    {
                        if (full != 0) while (GivePriAmmo(id, entity, wpnId, 1, 1, 0)) { }
                        else GivePriAmmo(id, entity, wpnId, 1, 1, 0);
                    }
                    else
                    {
                        if (full != 0) while (GivePriAmmo(id, entity, wpnId, 0, 1, 0)) { }
                        else GivePriAmmo(id, entity, wpnId, 0, 1, 0);
                    }
                }
                else
                {
                    if (full != 0) while (GivePriAmmo(id, entity, wpnId, 0, 1, 0)) { }
                    else GivePriAmmo(id, entity, wpnId, 0, 1, 0);
                }
            }
        }
        for (int i = 0; i < WpnConst.MaxSec; i++)
        {
            if (Eng.GetPDataInt(entity, 43) == Store.SecondId[i])
            {
                if (Store.WeaponClip[id, wpnId] == 0)
                {
                    if (wpnId != 0)
                    {
                        if (full != 0) while (GiveSecAmmo(id, entity, wpnId, 1, 1, 0)) { }
                        else GiveSecAmmo(id, entity, wpnId, 1, 1, 0);
                    }
                    else
                    {
                        if (full != 0) while (GiveSecAmmo(id, entity, wpnId, 0, 1, 0)) { }
                        else GiveSecAmmo(id, entity, wpnId, 0, 1, 0);
                    }
                }
                else
                {
                    if (full != 0) while (GiveSecAmmo(id, entity, wpnId, 0, 1, 0)) { }
                    else GiveSecAmmo(id, entity, wpnId, 0, 1, 0);
                }
            }
        }
    }

    // public ShotgunReload(iEntity, iMaxClip, iClip, iBpAmmo, id, WeaponId)
    public static void ShotgunReload(edict_t* entity, int maxClip, int clip, int bpAmmo, int id, int weaponId)
    {
        int i = Store.GetWeaponType(entity);
        if (bpAmmo <= 0 || maxClip == clip) return;

        if (Eng.GetPDataFloat(entity, 46) > 0.0f) return;

        switch (Eng.GetPDataInt(entity, 55))
        {
            case 0:
            {
                // 开始换弹
                PlayAnim(id, Store.WeaponAnim[id, i, WpnConst.AnimStartReload]);
                Eng.SetPDataInt(entity, 55, 1);
                Eng.SetPDataFloat(id, 83, Store.WeaponReloadTime[id, i, 1]);
                Eng.SetPDataFloat(entity, 46, Store.WeaponReloadTime[id, i, 1]);
                Eng.SetPDataFloat(entity, 48, Store.WeaponReloadTime[id, i, 1]);
                return;
            }
            case 1:
            {
                if (Eng.GetPDataFloat(entity, 48) > 0.0f) return;

                // 装弹动作
                Eng.SetPDataInt(entity, 55, 2);
                PlayAnim(id, Store.WeaponAnim[id, i, WpnConst.AnimInsert]);
                Eng.SetPDataFloat(entity, 46, Store.WeaponReloadTime[id, i, 0]);
                Eng.SetPDataFloat(entity, 48, Store.WeaponReloadTime[id, i, 0]);
                Eng.SetPDataFloat(id, 83, Store.WeaponReloadTime[id, i, 0]);
                break;
            }
            default:
            {
                // 子弹+1
                Eng.SetPDataInt(entity, 51, clip + 1);
                SetUserBpAmmo(id, weaponId, bpAmmo - 1);
                Store.CurWeaponBPAmmo[id, i] = bpAmmo - 1;
                Eng.SetPDataInt(entity, 55, 1);
                break;
            }
        }
    }

    // stock GetWeaponSlot(id, WpnID, slot)
    public static bool GetWeaponSlot(int id, int wpnId, int slot)
    {
        if (slot == 1) for (int i = 0; i < WpnConst.MaxPrim; i++) if (Store.WeaponId[id, wpnId] == Store.PrimId[i]) return true;
        if (slot == 2) for (int i = 0; i < WpnConst.MaxSec; i++) if (Store.WeaponId[id, wpnId] == Store.SecondId[i]) return true;
        if (slot == 3) if (Store.WeaponId[id, wpnId] == EngineConst.CswKnife) return true;
        if (slot == 4) if (Store.WeaponId[id, wpnId] == EngineConst.CswHegrenade) return true;
        return false;
    }

    // WeaponTypeByIndex: 原版 GetWeaponType 接受实体下标时的形态
    public static int WeaponTypeByIndex(int index)
    {
        if (index < 0) return 0;
        var entity = Eng.EntOfIndex(index);
        if (entity == null) return 0;
        int serial = Eng.GetPDataInt(entity, 11);
        return serial >= 1 && serial <= Store.WeaponAmount ? serial : 0;
    }

    // plugin_init 里的默认音量初始化
    public static void ApplyDefaultSoundVolumes()
    {
        for (int i = 1; i <= Store.WeaponAmount; i++)
        {
            for (int id = 1; id < 33; id++)
            {
                if (Store.WeaponFireSoundVolume[id, i] == 0.0f)
                {
                    Store.WeaponFireSoundVolume[id, i] = Store.WeaponId[id, i] == EngineConst.CswHegrenade ? 0.25f : 0.35f;
                }
                if (Store.WeaponSilenceFireSoundVolume[id, i] == 0.0f) Store.WeaponSilenceFireSoundVolume[id, i] = EngineConst.AttnNorm;
                for (int j = 0; j < 5; j++)
                    if (Store.KnifeSoundVolume[id, i, j] == 0.0f) Store.KnifeSoundVolume[id, i, j] = EngineConst.AttnNorm;
            }
        }
    }
}
