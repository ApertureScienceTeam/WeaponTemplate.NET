using GoldSrc.Amxmodx;
using GoldSrc.Amxmodx.Framework.Attributes;
using GoldSrc.HLSDK.Native;

namespace Module;

// 回合事件:wt_RoundStart / wt_FreezeEnd / wt_TextMsg(回合重置公告)与回合状态推进。
public static unsafe partial class RoundEvents
{
    [AmxxCallback("wt_RoundStart")]
    public static void OnRoundStartEvent(int id) => EventRoundStart();

    [AmxxCallback("wt_FreezeEnd")]
    public static void OnFreezeEnd() => WeaponStore.RoundStart = true;

    [AmxxCallback("wt_TextMsg")]
    public static int OnTextMsg(int msgId, int msgDest, int msgEntity)
    {
        AmxModX.GetMsgArgString(2, out var textArg, 31);
        string text = textArg ?? "";

        // 重置弹药
        if (text == "#Game_will_restart_in" || text == "#Game_Commencing") WeaponStore.RoundRestart = true;
        return EngineConst.PluginContinue;
    }

    // public Event_Round_Start(HLTV)
    public static void EventRoundStart()
    {
        WeaponStore.DisableBuyTime = Engine.GameTime + Engine.CvarFloat("mp_buytime") * 60.0f;
        WeaponStore.RoundStart = false;
        WeaponStore.Round++;
        if (WeaponStore.RoundRestart)
        {
            WeaponStore.RoundRestart = false;
            foreach (var player in WeaponStore.Players.Values) player.ClearAmmo();
        }
    }
}
