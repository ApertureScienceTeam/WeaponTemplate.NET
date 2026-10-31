using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// fakemeta 模块的生态 native 声明式导入(调用体由源生成器补全)。
internal static partial class FakeMeta
{
    // trace 结果读取:TR_vecEndPos = 5,输出 3 个浮点
    [AmxxImportNative("get_tr2")]
    public static partial void GetTr2(int traceHandle, int member, float[] output);
}
