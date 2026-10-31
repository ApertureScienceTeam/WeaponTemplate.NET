using GoldSrc.Amxmodx;
using GoldSrc.MetaMod.Native;

namespace Module;

internal static unsafe class HookResult
{
    public static void Ignore() => Set(META_RES.MRES_IGNORED);
    public static void Supercede() => Set(META_RES.MRES_SUPERCEDE);

    private static void Set(META_RES result)
    {
        if (ModuleRuntime.gpMetaGlobals != null)
            ModuleRuntime.gpMetaGlobals->mres = result;
    }
}
