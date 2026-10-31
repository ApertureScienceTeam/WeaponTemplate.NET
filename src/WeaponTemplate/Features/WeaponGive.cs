using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// 武器发放:实体创建/复用、武器列表消息、模板序号判定。
public static unsafe class WeaponGive
{
    // m_bIsVIP：Windows 字节偏移 837，落在 pdata 单元 209 的第 2 个字节
    private const int VipCell = 209;
    private const int VipBit = 0x100;

    // stock ChangeWeaponList(id, iEntity)
    public static void ChangeWeaponList(int id, edict_t* entity)
    {
        if (!Eng.IsValid(entity)) return;

        int i = WeaponStore.GetWeaponType(entity);
        int wpnId;
        string listName;
        if (i != 0)
        {
            wpnId = WeaponStore.Weapon(id, i).Id;
            listName = $"weapon_{WeaponStore.Weapon(id, i).List}";
        }
        else
        {
            wpnId = Eng.GetPDataInt(entity, 43);
            listName = wpnId >= 0 && wpnId < GameTables.GameWeaponClassName.Length ? GameTables.GameWeaponClassName[wpnId] : "";
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
            Eng.WriteByte(GameTables.GameWeaponAmmoId[wpnId]);
            Eng.WriteByte(WeaponStore.GameWeaponMaxBpammo[wpnId]);
        }
        else
        {
            Eng.WriteByte(GameTables.GameWeaponAmmoId[wpnId]);
            Eng.WriteByte(i != 0 ? WeaponStore.Weapon(id, i).Ammo : WeaponStore.GameWeaponMaxBpammo[wpnId]);
        }
        Eng.WriteByte(0xFF); // -1
        Eng.WriteByte(0xFF); // -1
        Eng.WriteByte(GameTables.GameWeaponPosition[wpnId]);
        Eng.WriteByte(GameTables.GameWeaponSlot[wpnId]);
        Eng.WriteByte(wpnId);
        Eng.MessageEnd();
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
                int weaponType = WeaponStore.GetWeaponType(entity);
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
        int slot = HamSandwich.ExecuteHam(EngineConst.HamItemItemSlot, Eng.IndexOf(entity));
        var existing = Eng.GetPDataCbase(pPlayer, 365 + slot + 2);
        if (existing != null)
        {
            int weaponType = WeaponStore.GetWeaponType(existing);
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
        if (!WeaponStore.IsTemplate(i) || !Eng.IsAlive(id))
        {
            MF.Log($"[diag] give abort: template={i} amount={WeaponStore.WeaponAmount} alive={Eng.IsAlive(id)}");
            return 0;
        }

        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return 0;
        var curEntity = Eng.GetPDataCbase(pPlayer, 373);
        var entity = GiveWeapon(id, GameTables.GameWeaponClassName[WeaponStore.Weapon(id, i).Id], i);
        if (!Eng.IsValid(entity))
        {
            MF.Log($"[diag] give abort: GiveWeapon returned invalid (tpl={i} cls={GameTables.GameWeaponClassName[WeaponStore.Weapon(id, i).Id]})");
            return 0;
        }

        int csw = WeaponStore.Weapon(id, i).Id;

        // 是武器
        if (csw != EngineConst.CswKnife && csw != EngineConst.CswHegrenade)
        {
            // 两种设置clip，CurWeapon那里会发送消息隐藏弹夹
            if (WeaponStore.Weapon(id, i).Clip != 0) Eng.SetPDataInt(entity, 51, WeaponStore.Weapon(id, i).Clip);
            else
            {
                Eng.SetPDataInt(entity, 51, WeaponStore.Weapon(id, i).Ammo);
                PlayerStocks.SetUserBpAmmo(id, csw, Eng.GetPDataInt(entity, 51));
            }
        }
        ChangeWeaponList(id, entity);

        // 如果给的是刀子，且当前武器就是刀子
        if (Eng.GetPDataInt(entity, 43) == EngineConst.CswKnife && Eng.IsValid(curEntity) && Eng.GetPDataInt(curEntity, 43) == EngineConst.CswKnife)
        {
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw]);
            Eng.EmitSound(pPlayer, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
            Eng.SetPDataFloat(pPlayer, 83, WeaponStore.Weapon(id, i).DrawTime);
            Eng.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
            Eng.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);
        }

        // 给的是手雷，第二个判断没看懂(原注释)
        if (Eng.GetPDataInt(entity, 43) == EngineConst.CswHegrenade && curEntity != Eng.GetPDataCbase(pPlayer, 373))
        {
            Eng.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
            Eng.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);
            Eng.SetPDataFloat(entity, 48, WeaponStore.Weapon(id, i).DrawTime);
        }

        // 给的同种类型（CSW）的不同序号的武器的时候，debug处理
        else if (WeaponStore.GetWeaponType(entity) != WeaponStore.GetWeaponType(curEntity) && Eng.IsValid(curEntity) && Eng.GetPDataInt(curEntity, 43) == Eng.GetPDataInt(entity, 43))
        {
            const int weaponBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            const int secWeaponBitSum = (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) | (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);

            int entityCsw = Eng.GetPDataInt(entity, 43);
            int curCsw = Eng.GetPDataInt(curEntity, 43);
            if (((1 << entityCsw) & secWeaponBitSum) != 0 && !Eng.IsValid(Eng.GetPDataCbase(pPlayer, 368)))
                HamSandwich.ExecuteHamB(EngineConst.HamItemDeploy, Eng.IndexOf(entity));
            if (((1 << entityCsw) & secWeaponBitSum) == 0 && ((1 << curCsw) & weaponBitSum) == 0)
                HamSandwich.ExecuteHamB(EngineConst.HamItemDeploy, Eng.IndexOf(entity));
        }
        return Eng.IndexOf(entity);
    }
    // WeaponTypeByIndex: 原版 GetWeaponType 接受实体下标时的形态
    public static int WeaponTypeByIndex(int index)
    {
        if (index < 0) return 0;
        var entity = Eng.EntOfIndex(index);
        if (entity == null) return 0;
        int serial = Eng.GetPDataInt(entity, 11);
        return serial >= 1 && serial <= WeaponStore.WeaponAmount ? serial : 0;
    }
}
