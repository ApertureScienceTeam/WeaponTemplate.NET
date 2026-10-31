using GoldSrc.Amxmodx;
using GoldSrc.HLSDK;

namespace Module;

// GoldSrc 的 precache 表和 entvars.model 会保留传入的字符串地址。
// 临时 NativeString 只用于 AllocString 的输入;交给引擎的地址必须来自它自己的字符串池。
internal static unsafe class EngineStrings
{
    private static readonly Dictionary<string, int> Indices = new(StringComparer.Ordinal);

    // AMXX_PluginsLoaded 在每张地图的 plugin_precache 之前调用。
    // 引擎在换图时释放字符串池,即使 pStringBase 未变化也不能复用上一张地图的索引。
    public static void Reset() => Indices.Clear();

    public static sbyte* Get(string value)
    {
        var engine = ModuleRuntime.g_engfuncs;
        if (!Indices.TryGetValue(value, out int index))
        {
            using var temporary = value.GetNativeString();
            index = engine.pfnAllocString(temporary);
            Indices.Add(value, index);
        }
        return engine.pfnSzFromIndex(index);
    }
}
