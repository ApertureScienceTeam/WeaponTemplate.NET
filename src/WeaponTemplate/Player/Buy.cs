using GoldSrc.Amxmodx;
using GoldSrc.HLSDK.Native;

namespace Module;

// public BotBuyWeaponEvent(id) 的翻译:BOT 进入购买区后模拟购买。
public static unsafe partial class Buy
{
    public static void BotBuyWeaponEvent(int id)
    {
        if (!Engine.IsAlive(id)) return;
        if (!Engine.IsBot(id)) return;

        if (!WeaponStore.Player(id).BotBuyWeapon) return;

        var pPlayer = Engine.EntOfIndex(id);
        if (pPlayer == null) return;

        WeaponStore.Player(id).BotMoney += Engine.GetPDataInt(pPlayer, 115);
        if (WeaponStore.Player(id).BotMoney > WeaponStore.MaxMoney) WeaponStore.Player(id).BotMoney = WeaponStore.MaxMoney;
        PlayerStocks.SetUserMoney(id, 0);

        int result = NativeFunctionRegister.Execute_WpnBotBuyWeapon(id);
        if (result == WpnConst.WPN_SUPERCEDE)
        {
            WeaponStore.Player(id).BotBuyWeapon = false;
            return;
        }

        var weaponBySlot = new int[2];
        int specialWeapon0, specialWeapon1;
        string[] grenadeName = { "weapon_hegrenade", "weapon_smokegrenade", "weapon_flashbang" };
        int[] grenadeId = { EngineConst.CswHegrenade, EngineConst.CswSmokegrenade, EngineConst.CswFlashbang };
        int[] grenadeCost = { 300, 300, 200 };

        for (int i = 0; i < 2; i++)
        {
            var entity = Engine.GetPDataCbase(pPlayer, 368 + i);
            if (Engine.IsValid(entity)) weaponBySlot[i] = WeaponStore.GetWeaponType(entity);
        }

        if (WeaponStore.Player(id).BotMoney >= 1000)
        {
            specialWeapon1 = WpnConst.SwVesthelm;
            specialWeapon0 = 0;
            int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
            if (postResult != WpnConst.WPN_SUPERCEDE && Buy.CheckBuy(id))
            {
                WeaponStore.Player(id).BotMoney -= 1000;
                pPlayer->v.armorvalue = 100.0f;
                Engine.SetPDataInt(pPlayer, 112, 2);
                Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("ArmorType"), null, pPlayer);
                Engine.WriteByte(1);
                Engine.MessageEnd();
                Engine.EmitSound(pPlayer, EngineConst.ChanItem, "items/ammopickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 1 << 0, EngineConst.PitchNorm);
            }
        }

        if (WeaponStore.Player(id).BotMoney >= 200 && Engine.GetPDataInt(pPlayer, 114) == 2)
        {
            specialWeapon1 = WpnConst.SwDefuser;
            specialWeapon0 = 0;
            int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
            bool canBuy = false;
            for (var found = Engine.FindEntityByString(null, "classname", "func_bomb_target"); found != null; found = Engine.FindEntityByString(found, "classname", "func_bomb_target"))
                canBuy = true;
            if (postResult != WpnConst.WPN_SUPERCEDE && canBuy && Buy.CheckBuy(id))
            {
                pPlayer->v.body = 1;
                int defuseKitSkill = Engine.GetPDataInt(pPlayer, 193);
                if ((defuseKitSkill & (1 << 16)) == 0)
                {
                    defuseKitSkill |= 1 << 16;
                    Engine.SetPDataInt(pPlayer, 193, defuseKitSkill);
                    Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("StatusIcon"), null, pPlayer);
                    Engine.WriteByte(1);
                    Engine.WriteString("defuser");
                    Engine.WriteByte(0);
                    Engine.WriteByte(160);
                    Engine.WriteByte(0);
                    Engine.MessageEnd();
                    WeaponStore.Player(id).BotMoney -= 200;
                    Engine.EmitSound(pPlayer, EngineConst.ChanAuto, "items/gunpickup2.wav", EngineConst.VolNorm, EngineConst.AttnNorm, 0, EngineConst.PitchNorm);
                }
            }
        }

        // 主/副/刀/雷 四个槽位的模板武器选择
        var slotCandidates = new int[4, WeaponStore.WeaponAmount + 1];
        var slotCount = new int[4];
        for (int j = 0; j < 4; j++)
        {
            for (int i = 1; i <= WeaponStore.WeaponAmount + 1; i++)
            {
                if (i > WeaponStore.WeaponAmount)
                {
                    if (slotCount[j] != 0)
                    {
                        int wpnId = slotCandidates[j, Engine.RandomNum(1, slotCount[j])];
                        specialWeapon1 = 0;
                        specialWeapon0 = wpnId;
                        if (j == 3) specialWeapon1 = WpnConst.SwHegrenade;
                        int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
                        if (postResult != WpnConst.WPN_SUPERCEDE)
                        {
                            WeaponGive.GiveWeaponByTemplate(id, wpnId);
                            WeaponStore.Player(id).BotMoney -= WeaponStore.Weapon(id, wpnId).Cost;
                            if (j < 2) PlayerStocks.SetUserBpAmmo(id, WeaponStore.Weapon(id, wpnId).Id, WeaponStore.Weapon(id, wpnId).Ammo);
                        }
                    }
                    break;
                }
                if (WeaponStore.Weapon(id, i).BuyCommand == "empty") continue;
                if (!Buy.CheckBuy(id)) continue;
                if (Engine.GetPDataInt(pPlayer, 114) != WeaponStore.Weapon(id, i).Team && WeaponStore.Weapon(id, i).Team != 0) continue;
                if (WeaponStore.Player(id).BotMoney < WeaponStore.Weapon(id, i).Cost) continue;
                if ((j == 0 && weaponBySlot[0] != 0) || (j == 1 && weaponBySlot[1] != 0)) continue;
                if (WeaponAmmo.GetWeaponSlot(id, i, j + 1))
                {
                    slotCount[j]++;
                    slotCandidates[j, slotCount[j]] = i;
                }
            }
        }

        // 手雷/烟雾/闪光
        for (int i = 0; i < 3; i++)
        {
            if (WeaponStore.Player(id).BotMoney >= grenadeCost[i])
            {
                if ((pPlayer->v.weapons & (1 << grenadeId[i])) != 0) continue;
                if (!Buy.CheckBuy(id)) continue;

                specialWeapon1 = WpnConst.SwHegrenade + i;
                specialWeapon0 = 0;
                int postResult = NativeFunctionRegister.Execute_WpnBotBuyWeaponPost(id, specialWeapon0, specialWeapon1);
                if (postResult != WpnConst.WPN_SUPERCEDE)
                {
                    WeaponGive.GiveWeapon(id, grenadeName[i], 0);
                    WeaponStore.Player(id).BotMoney -= grenadeCost[i];
                }
            }
        }

        if (slotCount[0] == 0 && slotCount[1] == 0 && weaponBySlot[0] == 0 && weaponBySlot[1] == 0)
        {
            PlayerStocks.SetUserMoney(id, WeaponStore.Player(id).BotMoney);
            WeaponStore.Player(id).BotMoney = 0;
        }
        WeaponStore.Player(id).BotBuyWeapon = false;
    }

    // public CheckBuy(id)
    public static bool CheckBuy(int id)
    {
        if (WeaponStore.Player(id).BlockBuy == WpnConst.BuyCannot) return false;
        if (WeaponStore.Player(id).BlockBuy == WpnConst.BuyEverywhere && Engine.IsAlive(id)) return true;
        if (!Engine.IsAlive(id)) return false;

        var p = Engine.EntOfIndex(id);
        if (p == null) return false;

        if ((Engine.GetPDataInt(p, 235) & (1 << 0)) == 0 && !Engine.IsBot(id)) return false;
        if (Engine.IsBot(id) && !WeaponStore.Player(id).BotBuyWeapon) return false;

        if (Engine.GameTime > WeaponStore.DisableBuyTime)
        {
            int seconds = FloatMath.Round(Engine.CvarFloat("mp_buytime") * 60.0f);
            Engine.MessageBegin(EngineConst.MsgOne, Engine.UserMsgId("TextMsg"), null, p);
            Engine.WriteByte(1 << 2);
            Engine.WriteString("#Cant_buy");
            Engine.WriteString(seconds.ToString());
            Engine.MessageEnd();
            return false;
        }
        return true;
    }
}
