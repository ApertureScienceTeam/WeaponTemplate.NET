using System.Runtime.InteropServices;
using GoldSrc.Amxmodx;
using GoldSrc.HLSDK;
using GoldSrc.HLSDK.Native;

namespace Module;

// fakemeta native(pev/engfunc/dllfunc/get_pdata_*) 的 C# 对应物:
// 引擎经 ModuleRuntime.g_engfuncs,实体数据经 edict->v(entvars_t)与 pvPrivateData 单元读写。
// pdata 偏移一律使用 CsOffsets 的命名常量(Windows/Linux 双列,运行时按平台取值),禁止写裸数字。
public static unsafe partial class Engine
{
    public static enginefuncs_t EF => ModuleRuntime.g_engfuncs;
    public static globalvars_t* GP => ModuleRuntime.gpGlobals;

    // ---- 基础查询 ----

    public static float GameTime => GP->time;

    public static edict_t* EntOfIndex(int index)
    {
        if (index < 0 || index >= GP->maxEntities)
            return null;
        var p = EF.pfnPEntityOfEntIndex(index);
        return p == null || p->free != 0 ? null : p;
    }

    public static int IndexOf(edict_t* ent) => ent == null ? 0 : EF.pfnIndexOfEdict(ent);

    public static bool IsValid(edict_t* ent) => ent != null && ent->free == 0 && ent->pvPrivateData != null;

    public static bool IsValidIndex(int index)
    {
        return IsValid(EntOfIndex(index));
    }

    public static bool IsConnected(int id)
    {
        if (id < 1 || id > 32) return false;
        var p = EF.pfnPEntityOfEntIndex(id);
        return p != null && p->free == 0 && p->v.netname.data != 0;
    }

    public static bool IsAlive(int id)
    {
        if (id < 1 || id > 32) return false;
        var p = EF.pfnPEntityOfEntIndex(id);
        return p != null && p->free == 0 && p->v.deadflag == 0 && p->v.health > 0.0f;
    }

    public static bool IsBot(int id)
    {
        var p = EF.pfnPEntityOfEntIndex(id);
        return p != null && (p->v.flags & EngineConst.FlFakeclient) != 0;
    }

    // ---- entvars 字符串 ----

    public static string Str(string_t s)
    {
        if (s.data == 0 || GP->pStringBase == null) return "";
        return Marshal.PtrToStringAnsi((nint)(GP->pStringBase + s.data)) ?? "";
    }

    public static string_t AllocStr(string value)
    {
        using var native = value.GetNativeString();
        return new string_t { data = unchecked((uint)EF.pfnAllocString(native)) };
    }

    public static string ClassName(edict_t* ent) => Str(ent->v.classname);
    public static string GlobalName(edict_t* ent) => Str(ent->v.globalname);

    public static void SetClassName(edict_t* ent, string name) => ent->v.classname = AllocStr(name);
    public static void SetGlobalName(edict_t* ent, string name) => ent->v.globalname = AllocStr(name);

    // pev_viewmodel2 / pev_weaponmodel2 语义:重新分配引擎字符串
    public static void SetViewModel(edict_t* ent, string model) => ent->v.viewmodel = (int)AllocStr(model).data;
    public static void SetWeaponModel(edict_t* ent, string model) => ent->v.weaponmodel = (int)AllocStr(model).data;

    // ---- pvPrivateData 单元读写(Windows 偏移) ----

    public static int GetPDataInt(edict_t* ent, int offset) => ((int*)ent->pvPrivateData)[offset];
    public static void SetPDataInt(edict_t* ent, int offset, int value) => ((int*)ent->pvPrivateData)[offset] = value;
    public static float GetPDataFloat(edict_t* ent, int offset) => ((float*)ent->pvPrivateData)[offset];
    public static void SetPDataFloat(edict_t* ent, int offset, float value) => ((float*)ent->pvPrivateData)[offset] = value;

    // set_pdata_*(id, ...) 的玩家下标重载(对应原版 get_pdata_int(id, ofs, linuxdiff))
    public static int GetPDataInt(int id, int offset)
    {
        var p = EntOfIndex(id);
        return p == null ? 0 : ((int*)p->pvPrivateData)[offset];
    }

    public static void SetPDataInt(int id, int offset, int value)
    {
        var p = EntOfIndex(id);
        if (p != null) ((int*)p->pvPrivateData)[offset] = value;
    }

    public static float GetPDataFloat(int id, int offset)
    {
        var p = EntOfIndex(id);
        return p == null ? 0f : ((float*)p->pvPrivateData)[offset];
    }

    public static void SetPDataFloat(int id, int offset, float value)
    {
        var p = EntOfIndex(id);
        if (p != null) ((float*)p->pvPrivateData)[offset] = value;
    }

    // get_pdata_cbase:CBasePlayerItem* -> 其 pev -> pContainingEntity
    // (GoldSrc CBaseEntity 布局:[vtable][entvars_t* pev][...],x86 下 pev 在 +4)
    public static edict_t* GetPDataCbase(edict_t* ent, int offset)
    {
        if (!IsValid(ent)) return null;
        var item = ((void**)ent->pvPrivateData)[offset];
        if (item == null) return null;
        var pev = *(entvars_t**)((byte*)item + 4);
        if (pev == null) return null;
        return pev->pContainingEntity;
    }

    public static void SetPDataCbase(edict_t* ent, int offset, edict_t* value) => ((void**)ent->pvPrivateData)[offset] = value;

    // ---- cvar ----

    public static float CvarFloat(string name)
    {
        using var n = name.GetNativeString();
        return EF.pfnCVarGetFloat(n);
    }

    public static int CvarNum(string name) => (int)CvarFloat(name);

    // ---- 随机数 ----

    public static int RandomNum(int lo, int hi) => EF.pfnRandomLong(lo, hi);
    public static float RandomFloat(float lo, float hi) => EF.pfnRandomFloat(lo, hi);

    // ---- 精确 / 模型 / 索引 ----

    public static int PrecacheModel(string path) => EngineResources.PrecacheModel(path);
    public static int PrecacheSound(string path) => EngineResources.PrecacheSound(path);

    public static void SetModel(edict_t* ent, string model)
    {
        if (IsValid(ent) && !string.IsNullOrEmpty(model))
            EF.pfnSetModel(ent, EngineStrings.Get(model));
    }

    public static int ModelIndex(string model)
    {
        using var m = model.GetNativeString();
        return EF.pfnModelIndex(m);
    }

    public static int ModelFrames(int modelIndex) => EF.pfnModelFrames(modelIndex);
    public static int DecalIndex(string decal)
    {
        using var d = decal.GetNativeString();
        return EF.pfnDecalIndex(d);
    }

    public static edict_t* CreateNamedEntity(string className)
    {
        var alloc = AllocStr(className);
        return EF.pfnCreateNamedEntity((int)alloc.data);
    }

    public static void RemoveEntity(edict_t* ent) => EF.pfnRemoveEntity(ent);

    public static edict_t* FindEntityByString(edict_t* start, string field, string value)
    {
        using var f = field.GetNativeString();
        using var v = value.GetNativeString();
        var found = EF.pfnFindEntityByString(start, f, v);
        return found == null || IndexOf(found) == 0 ? null : found;
    }

    public static edict_t* FindEntityInSphere(edict_t* start, float* origin, float radius)
    {
        var found = EF.pfnFindEntityInSphere(start, origin, radius);
        return found == null || IndexOf(found) == 0 ? null : found;
    }

    // Span 友好重载(内部 fixed 转指针)
    public static edict_t* FindEntityInSphere(edict_t* start, Span<float> origin, float radius)
    {
        fixed (float* p = origin) return FindEntityInSphere(start, p, radius);
    }

    // ---- 声音 / 事件 ----

    public static void EmitSound(edict_t* ent, int channel, string sample, float volume, float attn, int flags, int pitch)
    {
        if (string.IsNullOrEmpty(sample)) return;
        using var s = sample.GetNativeString();
        EF.pfnEmitSound(ent, channel, s, volume, attn, flags, pitch);
    }

    public static void PlaybackEvent(int flags, edict_t* invoker, ushort eventIndex, float delay,
        float* origin, float* angles, float fParam1, float fParam2, int iParam1, int iParam2, int bParam1, int bParam2)
        => EF.pfnPlaybackEvent(flags, invoker, eventIndex, delay, origin, angles, fParam1, fParam2, iParam1, iParam2, bParam1, bParam2);

    public static void MakeVectors(float* angles) => EF.pfnMakeVectors(angles);

    // Span 友好重载(内部 fixed 转指针)
    public static void MakeVectors(Span<float> angles)
    {
        fixed (float* p = angles) EF.pfnMakeVectors(p);
    }

    // engfunc VecToAngles:方向向量 -> 欧拉角
    public static void VecToAngles(Span<float> vecIn, Span<float> anglesOut)
    {
        fixed (float* pIn = vecIn, pOut = anglesOut) EF.pfnVecToAngles(pIn, pOut);
    }

    public static void TraceLine(Span<float> start, Span<float> end, int flags, edict_t* skip, TraceResult* ptr)
    {
        fixed (float* pStart = start, pEnd = end) EF.pfnTraceLine(pStart, pEnd, flags, skip, ptr);
    }

    public static void TraceHull(Span<float> start, Span<float> end, int flags, int hull, edict_t* skip, TraceResult* ptr)
    {
        fixed (float* pStart = start, pEnd = end) EF.pfnTraceHull(pStart, pEnd, flags, hull, skip, ptr);
    }

    // ---- 用户消息(直接走引擎;SVC 消息直接用常量) ----

    // get_user_msgid 等价:RegUserMsg 对已注册消息返回原 id
    public static int UserMsgId(string name)
    {
        using var n = name.GetNativeString();
        return EF.pfnRegUserMsg(n, -1);
    }

    // Span 友好重载(内部 fixed 转指针);origin 传 null 表示无原点(全局广播)
    public static void MessageBegin(int dest, int type, Span<float> origin, edict_t* entity)
    {
        fixed (float* p = origin) EF.pfnMessageBegin(dest, type, p, entity);
    }

    public static void MessageEnd() => EF.pfnMessageEnd();
    public static void WriteByte(int v) => EF.pfnWriteByte(v);
    public static void WriteChar(int v) => EF.pfnWriteChar(v);
    public static void WriteShort(int v) => EF.pfnWriteShort(v);
    public static void WriteLong(int v) => EF.pfnWriteLong(v);
    public static void WriteAngle(float v) => EF.pfnWriteAngle(v);
    public static void WriteCoord(float v) => EF.pfnWriteCoord(v);

    public static void WriteString(string value)
    {
        using var s = value.GetNativeString();
        EF.pfnWriteString(s);
    }

    public static void WriteEntity(int v) => EF.pfnWriteEntity(v);
}
