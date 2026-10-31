using GoldSrc.Amxmodx.Framework.Attributes;

namespace Module;

// amxmodx 核心模块的生态 native 声明式导入(调用体由源生成器补全)。
// 覆盖消息钩子取参/改参与玩家文字输出;register_event / register_logevent 走 MF.RegisterEvent。
internal static partial class AmxModX
{
    [AmxxImportNative("register_message")]
    public static partial void RegisterMessage(int messageId, string callback);

    [AmxxImportNative("get_msg_arg_int")]
    public static partial int GetMsgArgInt(int argn);

    [AmxxImportNative("get_msg_arg_string")]
    public static partial void GetMsgArgString(int argn, out string value, int len);

    [AmxxImportNative("set_msg_arg_int")]
    public static partial void SetMsgArgInt(int argn, int type, int value);

    [AmxxImportNative("set_msg_arg_string")]
    public static partial void SetMsgArgString(int argn, string value);

    [AmxxImportNative("client_print")]
    public static partial void ClientPrint(int id, int dest, string message);

    [AmxxImportNative("engclient_cmd")]
    public static partial void EngClientCmd(int id, string command);

    [AmxxImportNative("engclient_cmd")]
    public static partial void EngClientCmd(int id, string command, string argument);

    // ---- 组合封装(基于以上导入) ----

    // 玩家中心文字(client_print print_center;文本经 TextMsg 消息下发)
    public static void ClientPrintCenter(int id, string message)
        => ClientPrint(id, EngineConst.PrintCenter, message);

    // engclient_cmd 的可选参数形态:无参数时不传第三个实参
    public static void EngClientCommand(int id, string command, string? argument = null)
    {
        if (argument == null) EngClientCmd(id, command);
        else EngClientCmd(id, command, argument);
    }
}
