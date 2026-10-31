using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// 武器发放:实体创建/复用、武器列表消息、模板序号判定。
public static unsafe class WeaponGive
{
    // m_bIsVIP：Windows 字节偏移 837，落在 pdata 单元 209 的第 2 个字节

    // stock ChangeWeaponList(id, iEntity)
    public static void ChangeWeaponList(int id, edict_t* entity)
    {
        if (!Engine.IsValid(entity)) return;

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
            wpnId = Engine.GetPDataInt(entity, CsOffsets.ItemId);
            listName = wpnId >= 0 && wpnId < GameTables.GameWeaponClassName.Length ? GameTables.GameWeaponClassName[wpnId] : "";
        }

        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return;
        Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("WeaponList"), null, pPlayer);
        Engine.WriteString(listName);
        if (wpnId == EngineConst.CswKnife)
        {
            Engine.WriteByte(0xFF); // -1
            Engine.WriteByte(0xFF);
        }
        else if (wpnId == EngineConst.CswHegrenade)
        {
            Engine.WriteByte(GameTables.GameWeaponAmmoId[wpnId]);
            Engine.WriteByte(WeaponStore.GameWeaponMaxBpammo[wpnId]);
        }
        else
        {
            Engine.WriteByte(GameTables.GameWeaponAmmoId[wpnId]);
            Engine.WriteByte(i != 0 ? WeaponStore.Weapon(id, i).Ammo : WeaponStore.GameWeaponMaxBpammo[wpnId]);
        }
        Engine.WriteByte(0xFF); // -1
        Engine.WriteByte(0xFF); // -1
        Engine.WriteByte(GameTables.GameWeaponPosition[wpnId]);
        Engine.WriteByte(GameTables.GameWeaponSlot[wpnId]);
        Engine.WriteByte(wpnId);
        Engine.MessageEnd();
    }
    // 实体是否真的进了玩家的武器槽(m_rgpPlayerItems[0..5],Windows 单元 367..372)
    public static bool PlayerHasItem(int id, int entityIndex)
    {
        if (entityIndex <= 0) return false;
        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return false;
        for (int slot = 0; slot < 6; slot++)
            if (Engine.IndexOf(Engine.GetPDataCbase(pPlayer, CsOffsets.PlayerSlot(slot))) == entityIndex) return true;
        return false;
    }

    // public GiveWeapon(id, szClassName[], i) — 创建/复用实体发放
    public static edict_t* GiveWeapon(int id, string className, int i)
    {
        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return null;

        edict_t* entity;
        if (className == "weapon_knife")
        {
            entity = Engine.GetPDataCbase(pPlayer, CsOffsets.KnifeSlot);
            if (Engine.IsValid(entity))
            {
                int weaponType = WeaponStore.GetWeaponType(entity);
                if (i == weaponType)
                {
                    AmxModX.ClientPrintCenter(id, "#Cstrike_Already_Own_Weapon");
                    return null;
                }
                Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("WeapPickup"), null, pPlayer);
                Engine.WriteByte(EngineConst.CswKnife);
                Engine.MessageEnd();
                Engine.EmitSound(pPlayer, EngineConst.ChanAuto, "items/gunpickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
                Engine.SetPDataInt(entity, CsOffsets.ItemTemplateSerial, i);
                return entity;
            }
        }
        else if (className == "weapon_hegrenade")
        {
            if ((pPlayer->v.weapons & (1 << EngineConst.CswHegrenade)) != 0)
            {
                AmxModX.ClientPrintCenter(id, "#Cstrike_TitlesTXT_Cannot_Carry_Anymore");
                return null;
            }
        }

        entity = Engine.CreateNamedEntity(className);
        MF.Log($"[diag] give create '{className}' tpl={i} idx={(entity == null ? -1 : Engine.IndexOf(entity))} pdata={(entity == null || entity->pvPrivateData == null ? 0 : 1)}");
        if (!Engine.IsValid(entity))
        {
            MF.Log("[diag] give abort: entity invalid right after CreateNamedEntity");
            return null;
        }
        Engine.SetPDataInt(entity, CsOffsets.ItemTemplateSerial, i);
        int slot = HamSandwich.ExecuteHam(EngineConst.HamItemItemSlot, Engine.IndexOf(entity));
        var existing = Engine.GetPDataCbase(pPlayer, CsOffsets.PlayerSlot(slot));
        if (existing != null)
        {
            int weaponType = WeaponStore.GetWeaponType(existing);
            string existingClass = Engine.ClassName(existing);
            MF.Log($"[diag] give slot={slot} existing={Engine.IndexOf(existing)} existingTpl={weaponType} existingCls={existingClass}");
            if ((weaponType == i && i != 0 && weaponType != 0 && slot != 4) || (i == 0 && weaponType == 0 && className == existingClass && slot != 4))
            {
                Engine.RemoveEntity(entity);
                MF.Log("[diag] give abort: already own same weapon");
                AmxModX.ClientPrintCenter(id, "#Cstrike_Already_Own_Weapon");
                return null;
            }
            if (slot < 3)
            {
                MF.Log($"[diag] give drop '{existingClass}'");
                AmxModX.EngClientCommand(id, "drop", existingClass);
            }
        }
        else MF.Log($"[diag] give slot={slot} existing=none");

        var origin = pPlayer->v.origin;
        entity->v.origin = origin;
        entity->v.spawnflags = EngineConst.SfNoRespawn;

        var gamedll = ModuleRuntime.gpGamedllFuncs->dllapi_table;
        int spawnResult = gamedll->pfnSpawn(entity);
        int entityIndex = Engine.IndexOf(entity);
        MF.Log($"[diag] give spawn idx={entityIndex} ret={spawnResult} csw={Engine.GetPDataInt(entity, CsOffsets.ItemId)} serial={Engine.GetPDataInt(entity, CsOffsets.ItemTemplateSerial)}");

        // CS 的 CBasePlayerItem::DefaultTouch 会拒绝 VIP 拾取非手枪/刀(as_ 刺杀图)，这是游戏规则，不绕过
        bool isVip = (Engine.GetPDataInt(pPlayer, CsOffsets.Vip) & CsOffsets.VipBit) != 0;
        gamedll->pfnTouch(entity, pPlayer);

        MF.Log($"[diag] give touch idx={entityIndex} vip={isVip} free={entity->free} csw={Engine.GetPDataInt(entity, CsOffsets.ItemId)} playerPrimary={Engine.IndexOf(Engine.GetPDataCbase(pPlayer, CsOffsets.PrimaryWeapon))} active={Engine.IndexOf(Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem))} carried=0x{pPlayer->v.weapons:X8}");
        return entity;
    }

    // public Native_GiveWeapon(id, i) — 模板发放入口
    public static int GiveWeaponByTemplate(int id, int i)
    {
        if (!WeaponStore.IsTemplate(i) || !Engine.IsAlive(id))
        {
            MF.Log($"[diag] give abort: template={i} amount={WeaponStore.WeaponAmount} alive={Engine.IsAlive(id)}");
            return 0;
        }

        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return 0;
        var curEntity = Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem);
        var entity = GiveWeapon(id, GameTables.GameWeaponClassName[WeaponStore.Weapon(id, i).Id], i);
        if (!Engine.IsValid(entity))
        {
            MF.Log($"[diag] give abort: GiveWeapon returned invalid (tpl={i} cls={GameTables.GameWeaponClassName[WeaponStore.Weapon(id, i).Id]})");
            return 0;
        }

        int csw = WeaponStore.Weapon(id, i).Id;

        // 是武器
        if (csw != EngineConst.CswKnife && csw != EngineConst.CswHegrenade)
        {
            // 两种设置clip，CurWeapon那里会发送消息隐藏弹夹
            if (WeaponStore.Weapon(id, i).Clip != 0) Engine.SetPDataInt(entity, CsOffsets.ItemClip, WeaponStore.Weapon(id, i).Clip);
            else
            {
                Engine.SetPDataInt(entity, CsOffsets.ItemClip, WeaponStore.Weapon(id, i).Ammo);
                PlayerStocks.SetUserBpAmmo(id, csw, Engine.GetPDataInt(entity, CsOffsets.ItemClip));
            }
        }
        ChangeWeaponList(id, entity);

        // 如果给的是刀子，且当前武器就是刀子
        if (Engine.GetPDataInt(entity, CsOffsets.ItemId) == EngineConst.CswKnife && Engine.IsValid(curEntity) && Engine.GetPDataInt(curEntity, CsOffsets.ItemId) == EngineConst.CswKnife)
        {
            PlayerStocks.PlayAnim(id, WeaponStore.Weapon(id, i).KnifeAnim[WpnConst.KnifeAnimDraw]);
            Engine.EmitSound(pPlayer, EngineConst.ChanWeapon, WeaponStore.Weapon(id, i).Sound1, EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
            Engine.SetPDataFloat(pPlayer, CsOffsets.NextAttack, WeaponStore.Weapon(id, i).DrawTime);
            Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
            Engine.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);
        }

        // 给的是手雷，第二个判断没看懂(原注释)
        if (Engine.GetPDataInt(entity, CsOffsets.ItemId) == EngineConst.CswHegrenade && curEntity != Engine.GetPDataCbase(pPlayer, CsOffsets.ActiveItem))
        {
            Engine.SetViewModel(pPlayer, WeaponStore.Weapon(id, i).VModel);
            Engine.SetWeaponModel(pPlayer, WeaponStore.Weapon(id, i).PModel);
            Engine.SetPDataFloat(entity, CsOffsets.ItemTimeWeaponIdle, WeaponStore.Weapon(id, i).DrawTime);
        }

        // 给的同种类型（CSW）的不同序号的武器的时候，debug处理
        else if (WeaponStore.GetWeaponType(entity) != WeaponStore.GetWeaponType(curEntity) && Engine.IsValid(curEntity) && Engine.GetPDataInt(curEntity, CsOffsets.ItemId) == Engine.GetPDataInt(entity, CsOffsets.ItemId))
        {
            const int weaponBitSum = (1 << EngineConst.CswKnife) | (1 << EngineConst.CswHegrenade) | (1 << EngineConst.CswSmokegrenade) | (1 << EngineConst.CswFlashbang) | (1 << EngineConst.CswC4);
            const int secWeaponBitSum = (1 << EngineConst.CswP228) | (1 << EngineConst.CswElite) | (1 << EngineConst.CswFiveseven) | (1 << EngineConst.CswUsp) | (1 << EngineConst.CswGlock18) | (1 << EngineConst.CswDeagle);

            int entityCsw = Engine.GetPDataInt(entity, CsOffsets.ItemId);
            int curCsw = Engine.GetPDataInt(curEntity, CsOffsets.ItemId);
            if (((1 << entityCsw) & secWeaponBitSum) != 0 && !Engine.IsValid(Engine.GetPDataCbase(pPlayer, CsOffsets.PrimaryWeapon)))
                HamSandwich.ExecuteHamB(EngineConst.HamItemDeploy, Engine.IndexOf(entity));
            if (((1 << entityCsw) & secWeaponBitSum) == 0 && ((1 << curCsw) & weaponBitSum) == 0)
                HamSandwich.ExecuteHamB(EngineConst.HamItemDeploy, Engine.IndexOf(entity));
        }
        return Engine.IndexOf(entity);
    }
    // WeaponTypeByIndex: 原版 GetWeaponType 接受实体下标时的形态
    public static int WeaponTypeByIndex(int index)
    {
        if (index < 0) return 0;
        var entity = Engine.EntOfIndex(index);
        if (entity == null) return 0;
        int serial = Engine.GetPDataInt(entity, CsOffsets.ItemTemplateSerial);
        return serial >= 1 && serial <= WeaponStore.WeaponAmount ? serial : 0;
    }
}
