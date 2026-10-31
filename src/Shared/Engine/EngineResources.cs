using GoldSrc.Amxmodx;
using GoldSrc.HLSDK;

namespace Module;

// 通过引擎文件系统查询,包含 valve 回退目录和 PAK,路径也由引擎字符串池持有。
internal static unsafe class EngineResources
{
    private static readonly Dictionary<string, int> Models = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> Sounds = new(StringComparer.Ordinal);

    public static void Reset()
    {
        Models.Clear();
        Sounds.Clear();
        EngineStrings.Reset();
    }

    public static bool Exists(string path, bool sound = false)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        var engine = ModuleRuntime.g_engfuncs;
        using var name = (sound ? $"sound/{path}" : path).GetNativeString();
        return engine.pfnGetFileSize != null && engine.pfnGetFileSize(name) > 0;
    }

    public static int PrecacheModel(string path) => Precache(path, false);
    public static int PrecacheSound(string path) => Precache(path, true);

    private static int Precache(string path, bool sound)
    {
        if (string.IsNullOrEmpty(path)) return 0;
        var cache = sound ? Sounds : Models;
        if (cache.TryGetValue(path, out int index)) return index;
        if (!Exists(path, sound))
        {
            MF.Log($"[error] resource missing from engine filesystem: {(sound ? "sound/" : "")}{path}");
            return 0;
        }
        var engine = ModuleRuntime.g_engfuncs;
        index = sound ? engine.pfnPrecacheSound(EngineStrings.Get(path))
                      : engine.pfnPrecacheModel(EngineStrings.Get(path));
        if (index > 0) cache.Add(path, index);
        return index;
    }
}
