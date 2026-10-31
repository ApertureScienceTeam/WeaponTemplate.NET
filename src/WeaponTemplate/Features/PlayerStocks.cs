using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// 玩家一维状态的 pdata 读写 stock:金钱、后备弹药、焦距、持枪动画。
public static unsafe class PlayerStocks
{
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
        money = Math.Min(money, WeaponStore.MaxMoney);
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
}
