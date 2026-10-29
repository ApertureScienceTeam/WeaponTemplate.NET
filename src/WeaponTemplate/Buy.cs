using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// public BotBuyWeaponEvent(id) 的翻译:BOT 进入购买区后模拟购买。
public static unsafe class Buy
{
    public static void BotBuyWeaponEvent(int id)
    {
        if (!Eng.IsAlive(id)) return;
        if (!Eng.IsBot(id)) return;

        if (!Store.BotBuyWeapon[id]) return;

        var pPlayer = Eng.EntOfIndex(id);
        if (pPlayer == null) return;

        Store.BotMoney[id] += Eng.GetPDataInt(pPlayer, 115);
        if (Store.BotMoney[id] > Store.MaxMoney) Store.BotMoney[id] = Store.MaxMoney;
        Core.SetUserMoney(id, 0);

        int result = NativeFunctionRegister.Execute_WpnBotBuyWeapon(id);
        if (result == WpnConst.WPN_SUPERCEDE)
        {
            Store.BotBuyWeapon[id] = false;
            return;
        }

        var weaponBySlot = new edict_t*[2];
        int specialWeapon0, specialWeapon1;
        string[] grenadeName = { "weapon_hegrenade", "weapon_smokegrenade", "weapon_flashbang" };
        int[] grenadeId = { EngineConst.CswHegrenade, EngineConst.CswSmokegrenade, EngineConst.CswFlashbang };
        int[] grenadeCost = { 300, 300, 200 };

        for (int i = 0; i < 2; i++)
        {
            var entity = Eng.GetPDataCbase(pPlayer, 368 + i);
            if (Eng.IsValid(entity)) weaponBySlot[i] = entity;
        }

        if (Store.BotMoney[id] >= 1000)
        {
            specialWeapon1 = WpnConst.SwVesthelm;
            specialWeapon0 = 0;
            int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
            if (postResult != WpnConst.WPN_SUPERCEDE && Core.CheckBuy(id))
            {
                Store.BotMoney[id] -= 1000;
                pPlayer->v.armorvalue = 100.0f;
                Eng.SetPDataInt(pPlayer, 112, 2);
                Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("ArmorType"), null, pPlayer);
                Eng.WriteByte(1);
                Eng.MessageEnd();
                Eng.EmitSound(pPlayer, EngineConst.ChanItem, "items/ammopickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
            }
        }

        if (Store.BotMoney[id] >= 200 && Eng.GetPDataInt(pPlayer, 114) == 2)
        {
            specialWeapon1 = WpnConst.SwDefuser;
            specialWeapon0 = 0;
            int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
            bool canBuy = false;
            for (var found = Eng.FindEntityByString(null, "classname", "func_bomb_target"); found != null; found = Eng.FindEntityByString(found, "classname", "func_bomb_target"))
                canBuy = true;
            if (postResult != WpnConst.WPN_SUPERCEDE && canBuy && Core.CheckBuy(id))
            {
                pPlayer->v.body = 1;
                int defuseKitSkill = Eng.GetPDataInt(pPlayer, 193);
                if ((defuseKitSkill & (1 << 16)) == 0)
                {
                    defuseKitSkill |= 1 << 16;
                    Eng.SetPDataInt(pPlayer, 193, defuseKitSkill);
                    Eng.MessageBegin(EngineConst.MsgOne, Eng.UserMsgId("StatusIcon"), null, pPlayer);
                    Eng.WriteByte(1);
                    Eng.WriteString("defuser");
                    Eng.WriteByte(0);
                    Eng.WriteByte(160);
                    Eng.WriteByte(0);
                    Eng.MessageEnd();
                    Store.BotMoney[id] -= 200;
                    Eng.EmitSound(pPlayer, EngineConst.ChanAuto, "items/gunpickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
                }
            }
        }

        // 主/副/刀/雷 四个槽位的模板武器选择
        var slotCandidates = new int[4, WpnConst.MaxWeapon];
        var slotCount = new int[4];
        for (int j = 0; j < 4; j++)
        {
            for (int i = 1; i < WpnConst.MaxWeapon; i++)
            {
                if (i > Store.WeaponAmount)
                {
                    if (slotCount[j] != 0)
                    {
                        int wpnId = slotCandidates[j, Eng.RandomNum(1, slotCount[j])];
                        specialWeapon1 = 0;
                        specialWeapon0 = wpnId;
                        if (j == 3) specialWeapon1 = WpnConst.SwHegrenade;
                        int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
                        if (postResult != WpnConst.WPN_SUPERCEDE)
                        {
                            Core.GiveWeaponByTemplate(id, wpnId);
                            Store.BotMoney[id] -= Store.WeaponCost[id, wpnId];
                            if (j < 2) Core.SetUserBpAmmo(id, Store.WeaponId[id, wpnId], Store.WeaponAmmo[id, wpnId]);
                        }
                    }
                    break;
                }
                if (Store.WeaponBuyCommand[id, i] == "empty") continue;
                if (!Core.CheckBuy(id)) continue;
                if (Eng.GetPDataInt(pPlayer, 114) != Store.WeaponTeam[id, i] && Store.WeaponTeam[id, i] != 0) continue;
                if (Store.BotMoney[id] < Store.WeaponCost[id, i]) continue;
                if ((j == 0 && weaponBySlot[0] != null) || (j == 1 && weaponBySlot[1] != null)) continue;
                if (Core.GetWeaponSlot(id, i, j + 1))
                {
                    slotCount[j]++;
                    slotCandidates[j, slotCount[j]] = i;
                }
            }
        }

        // 手雷/烟雾/闪光
        for (int i = 0; i < 3; i++)
        {
            if (Store.BotMoney[id] >= grenadeCost[i])
            {
                if ((pPlayer->v.weapons & (1 << grenadeId[i])) != 0) continue;
                if (!Core.CheckBuy(id)) continue;

                specialWeapon1 = WpnConst.SwHegrenade + i;
                specialWeapon0 = 0;
                int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
                if (postResult != WpnConst.WPN_SUPERCEDE)
                {
                    Core.GiveWeapon(id, grenadeName[i], 0);
                    Store.BotMoney[id] -= grenadeCost[i];
                }
            }
        }

        if (slotCount[0] == 0 && slotCount[1] == 0 && weaponBySlot[0] == null && weaponBySlot[1] == null)
        {
            Core.SetUserMoney(id, Store.BotMoney[id]);
            Store.BotMoney[id] = 0;
        }
        Store.BotBuyWeapon[id] = false;
    }
}
