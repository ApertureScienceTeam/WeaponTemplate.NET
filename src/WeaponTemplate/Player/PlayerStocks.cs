using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// 玩家一维状态的 pdata 读写 stock:金钱、后备弹药、焦距、持枪动画。
public static unsafe class PlayerStocks
{
    // ---- 金钱 / 备弹 / 焦距(pdata stock) ----

    public static int GetUserMoney(int id)
    {
        var p = Engine.EntOfIndex(id);
        return p == null ? 0 : Engine.GetPDataInt(p, CsOffsets.Money);
    }

    // stock fm_set_user_money(id, iMoney, iFlash = 1)
    public static void SetUserMoney(int id, int money, int flash = 1)
    {
        var p = Engine.EntOfIndex(id);
        if (p == null) return;
        money = Math.Min(money, WeaponStore.MaxMoney);
        Engine.SetPDataInt(p, CsOffsets.Money, money);
        int msg = Engine.UserMsgId("Money");
        Engine.MessageBegin(EngineConst.MsgOne, msg, null, p);
        Engine.WriteLong(money);
        Engine.WriteByte(flash);
        Engine.MessageEnd();
    }

    public static int GetUserBpAmmo(int id, int weaponId)
    {
        int offset = BpAmmoOffset(weaponId);
        if (offset < 0) return 0;
        var p = Engine.EntOfIndex(id);
        return p == null ? 0 : Engine.GetPDataInt(p, offset);
    }

    public static void SetUserBpAmmo(int id, int weaponId, int amount)
    {
        int offset = BpAmmoOffset(weaponId);
        if (offset < 0)
        {
            MF.Log($"fm_cs_set_user_bpammo: Invalid weapon id {weaponId}");
            return;
        }
        var p = Engine.EntOfIndex(id);
        if (p == null) return;
        Engine.SetPDataInt(p, offset, amount);
    }

    // 原版 fm_cs_(get|set)_user_bpammo 的偏移表(含 Linux 列),见 CsOffsets.BpAmmoOffset
    private static int BpAmmoOffset(int weaponId) => CsOffsets.BpAmmoOffset(weaponId);

    public static int GetUserZoom(int id)
    {
        var p = Engine.EntOfIndex(id);
        if (p == null) return 0;
        int value = Engine.GetPDataInt(p, CsOffsets.Zoom);
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
        var p = Engine.EntOfIndex(id);
        if (p == null) return;
        p->v.weaponanim = anim;
        Engine.MessageBegin(EngineConst.MsgOne, EngineConst.SvcWeaponAnim, null, p);
        Engine.WriteByte(anim);
        Engine.WriteByte(p->v.body);
        Engine.MessageEnd();
    }
}
