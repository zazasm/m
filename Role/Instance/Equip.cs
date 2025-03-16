using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using COServer.Game.MsgServer;
using COServer.Game.MsgTournaments;

namespace COServer.Role.Instance
{
    public class Equip
    {
        public ConcurrentDictionary<uint, Game.MsgServer.MsgGameItem> ClientItems = new ConcurrentDictionary<uint, Game.MsgServer.MsgGameItem>();
        public uint SoulsPotency = 0;

        public int WeaponsMinAttack = 0;
        public uint ArmorID;
        public bool CreateSpawn = true;
        public bool SuperArmor = false;
        public bool FullSuper
        {
            get
            {
                if (!SuperArmor)
                    return false;
                foreach (var item in CurentEquip)
                {
                    if (item.Position != (ushort)Role.Flags.ConquerItem.Steed
                        && item.Position != (ushort)Role.Flags.ConquerItem.Garment
                        && item.Position != (ushort)Role.Flags.ConquerItem.Bottle
                        && item.Position != (ushort)Role.Flags.ConquerItem.AleternanteBottle
                        && item.Position != (ushort)Role.Flags.ConquerItem.AleternanteGarment
                        && item.Position != (ushort)Role.Flags.ConquerItem.SteedMount
                        && item.Position != (ushort)Role.Flags.ConquerItem.RightWeaponAccessory
                        && item.Position != (ushort)Role.Flags.ConquerItem.LeftWeaponAccessory)
                    {
                        if (item.ITEM_ID % 10 != 9)
                            return false;
                    }
                }
                return true;
            }
        }
        public Role.Flags.ItemEffect RightWeaponEffect = Flags.ItemEffect.None;
        public Role.Flags.ItemEffect LeftWeaponEffect = Flags.ItemEffect.None;
        public Role.Flags.ItemEffect RingEffect = Flags.ItemEffect.None;
        public Role.Flags.ItemEffect NecklaceEffect = Flags.ItemEffect.None;

        public bool UseMonkEpicWeapon = false;
        public uint ShieldID = 0;
        public uint RidingCrop = 0;
        public uint HeadID;
        public uint RightWeapon = 0;
        public uint AlternanteLeftWeapon = 0;
        public uint AlternanteRightWeapon = 0;
        public uint LeftWeapon = 0;
        public byte SteedPlus { get { return (byte)Owner.Player.SteedPlus; } }
        public uint SteedPlusPorgres = 0;

        public int rangeR = 0;
        public int rangeL = 0;
        public int SizeAdd = 0;

        public int SpeedR = 0;
        public int SpeedL = 0;
        public int SpeedRing = 0;

        public bool SuperDragonGem = false;
        public bool SuperPheonixGem = false;
        public bool SuperVioletGem = false;
        public bool SuperRaibowGem = false;
        public bool SuperMoonGem = false;
        public bool SuprtTortoiseGem = false;
        public bool HaveBless = false;

        public int AttackSpeed(int MS_Delay)
        {
            MS_Delay = Math.Max(300, MS_Delay - 100);

            MS_Delay = Math.Max(300, MS_Delay - SpeedR);
            MS_Delay = Math.Max(300, MS_Delay - SpeedL);
            MS_Delay = Math.Max(300, MS_Delay - SpeedRing);
            MS_Delay = Math.Max(300, MS_Delay - Owner.Player.Agility / 2);

            if (Owner.Player.ContainFlag(Game.MsgServer.MsgUpdate.Flags.Cyclone))
                MS_Delay = Math.Max(300, MS_Delay - 150);

            return MS_Delay;
        }
        public int AttackSpeed(bool physical)
        {
            int speed = 800;
            speed = Math.Max(300, speed - SpeedR);
            speed = Math.Max(300, speed - SpeedL);
            speed = Math.Max(300, speed - SpeedRing);
            speed = Math.Max(300, speed - Owner.Player.Agility / 2);

            if (Owner.Player.ContainFlag(Game.MsgServer.MsgUpdate.Flags.Cyclone))
                speed = Math.Max(300, speed - 150);

            return speed;
        }
        public int GetAttackRange(int targetSizeAdd)
        {
            var range = 1;

            if (rangeR != 0 && rangeL != 0)
                range = (rangeR + rangeL) / 2;
            else if (rangeR != 0)
                range = rangeR;
            else if (rangeL != 0)
                range = rangeL;

            range += (SizeAdd + targetSizeAdd + 1) / 2;

            return range;
        }

        public bool Alternante = false;
        private Client.GameClient Owner;
        public Equip(Client.GameClient client)
        {
            Owner = client;
        }
        public Game.MsgServer.MsgGameItem[] CurentEquip = new Game.MsgServer.MsgGameItem[0];
        public unsafe bool Add(ServerSockets.Packet stream, uint ID, Role.Flags.ConquerItem position, byte plus = 0, byte bless = 0, byte Enchant = 0
           , Role.Flags.Gem sockone = Flags.Gem.NoSocket
            , Role.Flags.Gem socktwo = Flags.Gem.NoSocket, bool bound = false, Role.Flags.ItemEffect Effect = Flags.ItemEffect.None)
        {
            if (FreeEquip(position))
            {
                Database.ItemType.DBItem DbItem;
                if (Database.Server.ItemsBase.TryGetValue(ID, out DbItem))
                {
                    Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                    ItemDat.UID = Database.Server.ITEM_Counter.Next;
                    ItemDat.ITEM_ID = ID;
                    ItemDat.Effect = Effect;
                    ItemDat.Durability = ItemDat.MaximDurability = DbItem.Durability;
                    ItemDat.Plus = plus;
                    ItemDat.Bless = bless;
                    ItemDat.Enchant = Enchant;
                    ItemDat.SocketOne = sockone;
                    ItemDat.SocketTwo = socktwo;
                    ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                    ItemDat.Bound = (byte)(bound ? 1 : 0);
                    CheakUp(ItemDat);
                    ItemDat.Position = (ushort)position;
                    ItemDat.Mode = Flags.ItemMode.AddItem;

                    ItemDat.Send(Owner, stream);


                    Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.Equip, ItemDat.UID, ItemDat.Position, 0, 0, 0, 0));

                    return true;

                }
            }
            return false;
        }
        private void CheakUp(Game.MsgServer.MsgGameItem ItemDat)
        {
            if (ItemDat.UID == 0)
                ItemDat.UID = Database.Server.ITEM_Counter.Next;
            if (!ClientItems.TryAdd(ItemDat.UID, ItemDat))
            {
                do
                {
                    // Console.WriteLine("Modifica uidul");
                    ItemDat.UID = Database.Server.ITEM_Counter.Next;
                }
                while
                  (ClientItems.TryAdd(ItemDat.UID, ItemDat) == false);
            }
        }
        public bool Contains(uint UID)
        {
            return ClientItems.ContainsKey(UID);
        }
        public bool Delete(Role.Flags.ConquerItem position, ServerSockets.Packet stream)
        {
            if (!FreeEquip(position))
            {
                if ((byte)position > 20)
                {
                    return DeleteAlternante(position, stream);
                }
                bool Accept = Owner.Inventory.HaveSpace(1);
                if (Accept)
                {
                    Game.MsgServer.MsgGameItem itemdata;
                    if (TryGetEquip(position, out itemdata))
                    {
                        if (ClientItems.TryRemove(itemdata.UID, out itemdata))
                        {
                            if (position == Flags.ConquerItem.Garment || position == Flags.ConquerItem.SteedMount)
                            {
                                itemdata.Position = 0;
                            }
                            else
                            {
                                Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.Unequip, itemdata.UID, itemdata.Position, 0, 0, 0, 0));
                                itemdata.Position = 0;
                                itemdata.Mode = Flags.ItemMode.AddItem;
                                Owner.Inventory.Update(itemdata, AddMode.REMOVE, stream);
                            }
                        }
                    }
                }
                else
                {
#if Arabic
                     Owner.SendSysMesage("Your Inventory Is Full.");
#else
                    Owner.SendSysMesage("Your Inventory Is Full.");
#endif

                }
                return Accept;
            }
            else
                return false;
        }
        public bool DeleteAlternante(Role.Flags.ConquerItem position, ServerSockets.Packet stream)
        {
            bool Accept = Owner.Inventory.HaveSpace(1);
            if (Accept)
            {
                Game.MsgServer.MsgGameItem itemdata;
                if (TryGetEquip(position, out itemdata))
                {
                    if (ClientItems.TryRemove(itemdata.UID, out itemdata))
                    {
                        Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.Unequip, itemdata.UID, itemdata.Position, 0, 0, 0, 0));
                        itemdata.Position = 0;
                        itemdata.Mode = Flags.ItemMode.AddItem;
                        Owner.Inventory.Update(itemdata, AddMode.REMOVE, stream);
                    }
                }
            }
            else
            {
#if Arabic
                     Owner.SendSysMesage("Your Inventory Is Full.");
#else
                Owner.SendSysMesage("Your Inventory Is Full.");
#endif

            }
            return Accept;
        }

        public bool Exist(Func<Game.MsgServer.MsgGameItem, bool> predicate)
        {
            bool Exist = false;
            foreach (var item in CurentEquip)
                if (predicate(item))
                {
                    Exist = true;
                    break;
                }
            return Exist;
        }
        public void Have(Func<Game.MsgServer.MsgGameItem, bool> predicate, out int count)
        {
            count = 0;
            foreach (var item in CurentEquip)
                if (predicate(item))
                {
                    count++;
                }

        }
        public bool Exist(Func<Game.MsgServer.MsgGameItem, bool> predicate, int count)
        {
            int counter = 0;
            foreach (var item in CurentEquip)
                if (predicate(item))
                {
                    counter++;
                }
            return counter >= count;
        }
        public ICollection<Game.MsgServer.MsgGameItem> AllItems
        {
            get { return ClientItems.Values; }
        }
        public bool TryGetValue(uint UID, out Game.MsgServer.MsgGameItem itemdata)
        {
            return ClientItems.TryGetValue(UID, out itemdata);
        }
        public bool FreeEquip(Role.Flags.ConquerItem position)
        {
            var item = ClientItems.Values.Where(p => p.Position == (ushort)position)
                .FirstOrDefault();
            return item == null;
        }
        public bool TryGetEquip(Role.Flags.ConquerItem position, out Game.MsgServer.MsgGameItem itemdata)
        {

            itemdata = ClientItems.Values.Where(p => p.Position == (ushort)position).FirstOrDefault();
            return itemdata != null;
        }
        public Game.MsgServer.MsgGameItem TryGetEquip(Role.Flags.ConquerItem position)
        {
            return ClientItems.Values.Where(p => p.Position == (ushort)position).FirstOrDefault();
        }
        public bool Remove(Role.Flags.ConquerItem position, ServerSockets.Packet stream)
        {
            if (position == Flags.ConquerItem.Steed)
            {
                Owner.Player.RemoveFlag(MsgUpdate.Flags.Ride);
            }
            if (Owner.Player.ContainFlag(MsgUpdate.Flags.Fly))
                Owner.Player.RemoveFlag(MsgUpdate.Flags.Fly);
            if (!FreeEquip(position))
            {
                if ((byte)position > 20)
                {
                    return RemoveAlternante(position, stream);
                }
                bool Accept = Owner.Inventory.HaveSpace(1);
                if (Accept)
                {
                    Game.MsgServer.MsgGameItem itemdata;
                    if (TryGetEquip(position, out itemdata))
                    {
                        if (ClientItems.TryRemove(itemdata.UID, out itemdata))
                        {
                            Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.Unequip, itemdata.UID, itemdata.Position, 0, 0, 0, 0));

                            itemdata.Position = 0;
                            itemdata.Mode = Flags.ItemMode.AddItem;
                            Owner.Inventory.Update(itemdata, AddMode.MOVE, stream);

                        }
                    }
                }
                else
                {
                    Owner.SendSysMesage("Your Inventory Is Full.");
                }
                return Accept;
            }
            else
                return false;
        }
        public bool RemoveAlternante(Role.Flags.ConquerItem position, ServerSockets.Packet stream)
        {
            bool Accept = Owner.Inventory.HaveSpace(1);
            if (Accept)
            {
                Game.MsgServer.MsgGameItem itemdata;
                if (TryGetEquip(position, out itemdata))
                {
                    if (ClientItems.TryRemove(itemdata.UID, out itemdata))
                    {
                        Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.Unequip, itemdata.UID, itemdata.Position, 0, 0, 0, 0));
                        itemdata.Position = 0;
                        itemdata.Mode = Flags.ItemMode.AddItem;
                        Owner.Inventory.Update(itemdata, AddMode.MOVE, stream);
                    }
                }
            }
            else
            {
                Owner.SendSysMesage("Your Inventory Is Full.");
            }
            return Accept;
        }

        public void Add(Game.MsgServer.MsgGameItem item, ServerSockets.Packet stream)
        {
            CheakUp(item);

            if (item.Position > 20)
            {
                AddAlternante(item, stream);
                return;
            }
            ClientItems.TryAdd(item.UID, item);
            item.Mode = Flags.ItemMode.AddItem;
            item.Send(Owner, stream);


        }
        public void AddAlternante(Game.MsgServer.MsgGameItem itemdata, ServerSockets.Packet stream)
        {
            ClientItems.TryAdd(itemdata.UID, itemdata);
            itemdata.Mode = Flags.ItemMode.AddItem;

            Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.Unequip, itemdata.UID, itemdata.Position, 0, 0, 0, 0));

            itemdata.Send(Owner, stream);
        }
        public void Show(ServerSockets.Packet stream, bool eqtemp = true)
        {
            foreach (var item in ClientItems.Values)
            {
                item.Mode = Flags.ItemMode.AddItem;
                item.Send(Owner, stream);
            }
            QueryEquipment(Alternante);
        }
        public unsafe void ClearItemSpawn()
        {
            Owner.Player.ClearItemsSpawn();
        }
        public unsafe void AddSpawn(Game.MsgServer.MsgGameItem DataItem)
        {

            switch ((Role.Flags.ConquerItem)DataItem.Position)
            {
                case Role.Flags.ConquerItem.AleternanteArmor:
                case Role.Flags.ConquerItem.Armor:
                    {
                        Owner.Player.ArmorId = DataItem.ITEM_ID;
                        Owner.Player.ColorArmor = (ushort)DataItem.Color;
                        Owner.Player.ArmorSoul = DataItem.Purification.PurificationItemID;

                        break;
                    }
                case Role.Flags.ConquerItem.AleternanteHead:
                case Role.Flags.ConquerItem.Head:
                    {

                        Owner.Player.HeadId = DataItem.ITEM_ID;
                        Owner.Player.ColorHelment = (ushort)DataItem.Color;
                        Owner.Player.HeadSoul = DataItem.Purification.PurificationItemID;
                        break;
                    }
                case Role.Flags.ConquerItem.AleternanteLeftWeapon:
                case Role.Flags.ConquerItem.LeftWeapon:
                    {
                        Owner.Player.LeftWeaponId = DataItem.ITEM_ID;
                        Owner.Player.LeftWeapsonSoul = DataItem.Purification.PurificationItemID;
                        break;
                    }
                case Role.Flags.ConquerItem.LeftWeaponAccessory:
                    {
                        Owner.Player.LeftWeaponAccessoryId = DataItem.ITEM_ID;
                        break;
                    }
                case Role.Flags.ConquerItem.AleternanteRightWeapon:
                case Role.Flags.ConquerItem.RightWeapon:
                    {
                        Owner.Player.RightWeaponId = DataItem.ITEM_ID;
                        Owner.Player.ColorShield = (ushort)DataItem.Color;
                        Owner.Player.RightWeapsonSoul = DataItem.Purification.PurificationItemID;
                        break;
                    }
                case Role.Flags.ConquerItem.RightWeaponAccessory:
                    {
                        Owner.Player.RightWeaponAccessoryId = DataItem.ITEM_ID;
                        break;
                    }
                case Role.Flags.ConquerItem.Steed:
                    {
                        Owner.Player.SteedId = DataItem.ITEM_ID;
                        Owner.Player.SteedColor = DataItem.SocketProgress;
                        Owner.Player.SteedPlus = DataItem.Plus;
                        SteedPlusPorgres = DataItem.PlusProgress;
                        break;
                    }
                case Role.Flags.ConquerItem.SteedMount:
                    {
                        Owner.Player.MountArmorId = DataItem.ITEM_ID;
                        break;
                    }
                case Role.Flags.ConquerItem.AleternanteGarment:
                case Role.Flags.ConquerItem.Garment:
                    {

                        Owner.Player.GarmentId = DataItem.ITEM_ID;
                        break;
                    }
            }
            if (Owner.Player.SpecialGarment != 0)
                Owner.Player.GarmentId = Owner.Player.SpecialGarment;
        }
        public unsafe void UpdateStats(Game.MsgServer.MsgGameItem[] MyGear, ServerSockets.Packet stream)
        {

            try
            {

                SoulsPotency = 0;
                rangeR = rangeL = SizeAdd = 0;
                SpeedR = SpeedL = SpeedRing = 0;
                RightWeapon = 0;
                LeftWeapon = 0;
                UseMonkEpicWeapon = false;
                SuperArmor = false;
                HeadID = 0;
                WeaponsMinAttack = 0;
                HaveBless = false;
                RingEffect = Flags.ItemEffect.None;
                RightWeaponEffect = Flags.ItemEffect.None;
                LeftWeaponEffect = Flags.ItemEffect.None;
                SteedPlusPorgres = 0;
                Owner.Status.MaxVigor = 0;
                RidingCrop = 0;
                if (CreateSpawn)
                {
                    lock (CurentEquip)
                        CurentEquip = MyGear;
                    ClearItemSpawn();
                }
                Owner.Status = new MsgStatus();
                Owner.Status.UID = Owner.Player.UID;

                Owner.Status.MaxAttack = (ushort)(Owner.Player.Strength + 1);
                Owner.Status.MinAttack = (ushort)(Owner.Player.Strength);
                Owner.Status.MagicAttack = Owner.Player.Spirit;// (ushort)(Owner.Player.Spirit * 10);

                Owner.Gems = new ushort[13];

                foreach (var item in MyGear)
                {
                    var pos = Database.ItemType.ItemPosition(item.ITEM_ID);
                    if (pos == (ushort)Role.Flags.ConquerItem.Head || pos == (ushort)Role.Flags.ConquerItem.AleternanteHead)
                        HeadID = item.ITEM_ID;
                    //aici sa fac schimbare de position !!!!!!!!!!!
                    try
                    {

                        if (CreateSpawn)
                            AddSpawn(item);

                        if (item.Durability == 0)
                            continue;

                        ushort ItemPostion = (ushort)(item.Position % 20);
                        ushort ItemPostion2 = (ushort)(item.Position);

                        if (item.Bless >= 1)
                            HaveBless = true;
                        if (ItemPostion == (ushort)Role.Flags.ConquerItem.Armor)
                        {
                            SuperArmor = (item.ITEM_ID % 10) == 9;

                            ArmorID = item.ITEM_ID;
                        }
                        if (item.SocketOne != Role.Flags.Gem.NoSocket && item.SocketOne != Role.Flags.Gem.EmptySocket)
                        {
                            if (item.SocketOne == Role.Flags.Gem.SuperTortoiseGem)
                                SuprtTortoiseGem = true;
                            if (item.SocketOne == Role.Flags.Gem.SuperDragonGem)
                                SuperDragonGem = true;
                            if (item.SocketOne == Role.Flags.Gem.SuperPhoenixGem)
                                SuperPheonixGem = true;
                            if (item.SocketOne == Role.Flags.Gem.SuperVioletGem)
                                SuperVioletGem = true;
                            if (item.SocketOne == Role.Flags.Gem.SuperRainbowGem)
                                SuperRaibowGem = true;
                            if (item.SocketOne == Role.Flags.Gem.SuperMoonGem)
                                SuperMoonGem = true;
                        }

                        if (ItemPostion == (ushort)Role.Flags.ConquerItem.RidingCrop)
                        {
                            RidingCrop = item.ITEM_ID;
                            Owner.Status.MaxVigor += 1000;
                        }
                        if (ItemPostion == (ushort)Role.Flags.ConquerItem.LeftWeapon)
                        {
                            LeftWeapon = item.ITEM_ID;
                            LeftWeaponEffect = item.Effect;
                            if (Database.ItemType.IsShield(item.ITEM_ID))
                                ShieldID = item.ITEM_ID;
                        }
                        //if (ItemPostion2 == (ushort)Role.Flags.ConquerItem.AleternanteLeftWeapon)
                        //{
                        //    AlternanteLeftWeapon = item.ITEM_ID;
                        //}
                        if (ItemPostion == (ushort)Role.Flags.ConquerItem.RightWeapon)
                        {
                            RightWeaponEffect = item.Effect;
                            RightWeapon = item.ITEM_ID;
                        }

                        if (ItemPostion == (ushort)Role.Flags.ConquerItem.Ring)
                            RingEffect = item.Effect;

                        if (ItemPostion == (ushort)Role.Flags.ConquerItem.Necklace)
                            NecklaceEffect = item.Effect;

                        AddGem(item.SocketOne);
                        AddGem(item.SocketTwo);
                        if (!Database.Server.ItemsBase.ContainsKey(item.ITEM_ID))
                            continue;
                        var DBItem = Database.Server.ItemsBase[item.ITEM_ID];

                        // Console.WriteLine(item.Position + " " + item.ItemPoints);
                        if (ItemPostion == (byte)Role.Flags.ConquerItem.Fan)
                        {
                            Owner.Status.PhysicalDamageIncrease += DBItem.MaxAttack;
                            Owner.Status.MagicDamageIncrease += DBItem.MagicAttack;
                        }
                        else
                        {
                            if (ItemPostion == (ushort)Role.Flags.ConquerItem.Ring)
                            {
                                SpeedRing = DBItem.Frequency;
                            }
                            if (ItemPostion == (ushort)Role.Flags.ConquerItem.LeftWeapon)
                            {
                                rangeL = DBItem.AttackRange;
                                SpeedL = DBItem.Frequency;

                                WeaponsMinAttack += (int)(DBItem.MaxAttack / 2);
                                Owner.Status.MaxAttack += (uint)(DBItem.MaxAttack / 2);
                                Owner.Status.MinAttack += (uint)(DBItem.MinAttack / 2);
                                Owner.Status.MagicAttack += (uint)(DBItem.MagicAttack / 2);
                            }
                            else
                            {
                                if (ItemPostion == (ushort)Role.Flags.ConquerItem.RightWeapon)
                                {
                                    WeaponsMinAttack += DBItem.MinAttack;
                                    rangeR = DBItem.AttackRange;
                                    SpeedR = DBItem.Frequency;
                                }

                                Owner.Status.MaxAttack += DBItem.MaxAttack;
                                Owner.Status.MinAttack += DBItem.MinAttack;
                                Owner.Status.MagicAttack += DBItem.MagicAttack;
                            }
                        }
                        if (ItemPostion == (byte)Role.Flags.ConquerItem.Tower)
                        {
                            Owner.Status.MagicDamageDecrease += DBItem.MagicDefence;
                            Owner.Status.PhysicalDamageDecrease += DBItem.PhysicalDefence;
                        }
                        else
                        {
                            Owner.Status.Immunity += DBItem.Imunity;
                            Owner.Status.CriticalStrike += DBItem.Crytical;
                            Owner.Status.SkillCStrike += DBItem.SCrytical;
                            Owner.Status.Breakthrough += DBItem.BreackTrough;
                            Owner.Status.Counteraction += DBItem.ConterAction;
                            Owner.Status.MDefence += (byte)DBItem.MagicDefence;
                            Owner.Status.Defence += DBItem.PhysicalDefence;
                        }

                        if (ItemPostion != (byte)Role.Flags.ConquerItem.Steed)
                        {
                            Owner.Status.Dodge += DBItem.Dodge;
                            Owner.Status.AgilityAtack += DBItem.Frequency;//.Agility;
                            Owner.Status.ItemBless += item.Bless;
                            Owner.Status.MaxHitpoints += item.Enchant;
                        }
                        Owner.Status.MaxHitpoints += DBItem.ItemHP;
                        Owner.Status.MaxMana += DBItem.ItemMP;

                        if (item.Purification.InLife)
                        {
                            var purificare = Database.Server.ItemsBase[item.Purification.PurificationItemID];
                            Owner.Status.MaxAttack += purificare.MaxAttack;
                            Owner.Status.MinAttack += purificare.MinAttack;
                            Owner.Status.MagicAttack += purificare.MagicAttack;
                            Owner.Status.MDefence += (byte)purificare.MagicDefence;
                            Owner.Status.Defence += purificare.PhysicalDefence;

                            Owner.Status.CriticalStrike += purificare.Crytical;
                            Owner.Status.SkillCStrike += purificare.SCrytical;
                            Owner.Status.Immunity += purificare.Imunity;
                            Owner.Status.Penetration += purificare.Penetration;
                            Owner.Status.Block += purificare.Block;
                            Owner.Status.Breakthrough += purificare.BreackTrough;
                            Owner.Status.Counteraction += purificare.ConterAction;
                            Owner.Status.Detoxication += purificare.Detoxication;

                            Owner.Status.MetalResistance += purificare.MetalResistance;
                            Owner.Status.WoodResistance += purificare.WoodResistance;
                            Owner.Status.FireResistance += purificare.FireResistance;
                            Owner.Status.EarthResistance += purificare.EarthResistance;
                            Owner.Status.WaterResistance += purificare.WaterResistance;

                            Owner.Status.MaxHitpoints += purificare.ItemHP;
                            Owner.Status.MaxMana += purificare.ItemMP;
                            SoulsPotency += item.Purification.PurificationLevel;
                        }

                        if (item.Refinary.InLife)
                        {
                            IncreaseRifainaryStatus(item.Refinary.EffectID);
                        }
                        if (item.Plus > 0)
                        {
                            var extraitematributes = DBItem.Plus[item.Plus];
                            if (extraitematributes != null)
                            {
                                if (ItemPostion == (ushort)Role.Flags.ConquerItem.LeftWeapon || ItemPostion == (ushort)Role.Flags.ConquerItem.RightWeapon)
                                {
                                    Owner.Status.Accuracy += extraitematributes.Agility;
                                }

                                if (ItemPostion == (byte)Role.Flags.ConquerItem.Steed)
                                    Owner.Status.MaxVigor = extraitematributes.Agility;

                                if (ItemPostion == (byte)Role.Flags.ConquerItem.Fan)
                                {
                                    Owner.Status.PhysicalDamageIncrease += extraitematributes.MaxAttack;
                                    Owner.Status.MagicDamageIncrease += extraitematributes.MagicAttack;
                                }
                                else
                                {
                                    /*  if (ItemPostion == (ushort)Role.Flags.ConquerItem.LeftWeapon)// just for main attack
                                      {
                                          Owner.Status.MaxAttack += extraitematributes.MaxAttack / 2;
                                          Owner.Status.MinAttack += extraitematributes.MinAttack / 2;
                                          Owner.Status.MagicAttack += extraitematributes.MagicAttack;
                                      }
                                      else*/
                                    {
                                        Owner.Status.MaxAttack += extraitematributes.MaxAttack;
                                        Owner.Status.MinAttack += extraitematributes.MinAttack;
                                        Owner.Status.MagicAttack += extraitematributes.MagicAttack;
                                    }
                                }
                                if (ItemPostion == (byte)Role.Flags.ConquerItem.Tower)
                                {
                                    Owner.Status.MagicDamageDecrease += extraitematributes.MagicDefence;
                                    Owner.Status.PhysicalDamageDecrease += extraitematributes.PhysicalDefence;
                                }
                                else
                                {
                                    Owner.Status.MagicDefence += extraitematributes.MagicDefence;
                                    Owner.Status.Defence += extraitematributes.PhysicalDefence;
                                }


                                if (ItemPostion != (byte)Role.Flags.ConquerItem.Steed)
                                {
                                    Owner.Status.Dodge += extraitematributes.Dodge;
                                    //   Owner.Status.AgilityAtack += extraitematributes.Agility;
                                }
                                Owner.Status.MaxHitpoints += extraitematributes.ItemHP;
                            }
                            else
                                Console.WriteLine("Invalid Plus -> item " + item.ITEM_ID.ToString() + " ->  plus " + item.Plus.ToString() + "");
                        }
                    }
                    catch (Exception e) { Console.WriteLine(e.ToString()); }
                }

                //add gem stats
                // Owner.Status.MinAttack = (uint)(Owner.Status.MinAttack * Owner.GemValues(Role.Flags.Gem.NormalDragonGem)) / 100;
                //  Owner.Status.MaxAttack = (uint)(Owner.Status.MaxAttack * Owner.GemValues(Role.Flags.Gem.NormalDragonGem)) / 100;

                // Console.WriteLine(Owner.Status.MaxAttack);
                Owner.Status.MagicAttack += 1;
                Owner.Status.MagicDefence += Owner.GemValues(Role.Flags.Gem.NormalGloryGem);
                Owner.Status.PhysicalDamageDecrease += Owner.GemValues(Role.Flags.Gem.NormalGloryGem);

                Owner.Status.PhysicalDamageIncrease += Owner.GemValues(Role.Flags.Gem.NormalThunderGem);
                Owner.Status.MagicDamageIncrease += Owner.GemValues(Role.Flags.Gem.NormalThunderGem);

                //  Owner.Status.MagicAttack += (uint)(Owner.Status.MagicAttack * Owner.GemValues(Role.Flags.Gem.NormalPhoenixGem)) / 100;
                // Owner.Status.ItemBless += Owner.GemValues(Role.Flags.Gem.NormalTortoiseGem);

                if (!Owner.Fake)
                    AddChiAtribute(Owner.Player.MyChi);
                Owner.Status.MaxHitpoints += Owner.CalculateHitPoint();
                Owner.Status.MaxMana += Owner.CalculateMana();
                Owner.AddProf();
                Owner.Vigor = (ushort)Math.Min((int)Owner.Vigor, (int)Owner.Status.MaxVigor);
                if (CreateSpawn)
                    Owner.Send(stream.ServerInfoCreate(MsgServerInfo.Action.Vigor, Owner.Vigor));
                CalculateBattlePower();

                if (CreateSpawn)
                    Owner.Player.View.SendView(Owner.Player.GetArray(stream, false), false);

                Owner.Player.CheckAura();

                if (!Owner.Fake)
                    Owner.Player.SubClass.UpdateStatus(Owner);

                Owner.Send(stream.StatusCreate(Owner.Status));

                SendMentorShare(stream);

                Owner.Status.Damage = Owner.GemValues(Flags.Gem.SuperTortoiseGem);
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }

        public void AddChiAtribute(Role.Instance.Chi chi, uint percent = 100)
        {

            Owner.Status.CriticalStrike += chi.CriticalStrike * percent / 100;
            Owner.Status.SkillCStrike += chi.SkillCriticalStrike * percent / 100;
            Owner.Status.Immunity += chi.Immunity * percent / 100;
            Owner.Status.Counteraction += chi.Counteraction * percent / 100;
            Owner.Status.Breakthrough += chi.Breakthrough * percent / 100;
            Owner.Status.MaxHitpoints += chi.MaxLife * percent / 100;
            Owner.Status.MaxAttack += chi.AddAttack * percent / 100;
            Owner.Status.MinAttack += chi.AddAttack * percent / 100;
            Owner.Status.MagicAttack += chi.AddMagicAttack * percent / 100;
            Owner.Status.MagicDefence += chi.AddMagicDefense * percent / 100;

            Owner.Status.PhysicalDamageIncrease += chi.FinalAttack * percent / 100;
            Owner.Status.PhysicalDamageDecrease += chi.FinalDefense * percent / 100;
            Owner.Status.MagicDamageIncrease += chi.FinalMagicAttack * percent / 100;
            Owner.Status.MagicDamageDecrease += chi.FinalMagicDefense * percent / 100;

        }
        public unsafe void SendMentorShare(ServerSockets.Packet stream)
        {
            Game.MsgServer.MsgApprenticeInformation Information = Game.MsgServer.MsgApprenticeInformation.Create();
            Information.Mode = Game.MsgServer.MsgApprenticeInformation.Action.Mentor;
            if (Owner.Player.MyMentor != null)
            {
                if (Owner.Player.Associate.Associat.ContainsKey(Role.Instance.Associate.Mentor))
                {
                    if (Owner.Player.MyMentor.MyClient != null)
                    {
                        if (Owner.Player.Associate.Associat[Role.Instance.Associate.Mentor].ContainsKey(Owner.Player.MyMentor.MyUID))
                        {
                            Role.Player mentor = Owner.Player.MyMentor.MyClient.Player;
                            Owner.Player.SetMentorBattlePowers(mentor.GetShareBattlePowers((uint)Owner.Player.RealBattlePower), (uint)mentor.RealBattlePower);

                            Information.Mentor_ID = mentor.UID;
                            Information.Apprentice_ID = Owner.Player.UID;
                            Information.Enrole_date = (uint)Owner.Player.Associate.Associat[Role.Instance.Associate.Mentor][mentor.UID].Timer;
                            Information.Fill(mentor.Owner);
                            Information.Shared_Battle_Power = mentor.GetShareBattlePowers((uint)Owner.Player.RealBattlePower);
                            Information.WriteString(mentor.Name, Owner.Player.Spouse, Owner.Player.Name);
                            Owner.Send(Information.GetArray(stream));
                        }
                    }

                }
            }
            if (!Owner.Fake)
            {
                if (!Owner.Player.Associate.Associat.ContainsKey(Role.Instance.Associate.Apprentice))
                    return;

                foreach (var Apprentice in Owner.Player.Associate.OnlineApprentice.Values)
                {
                    if (!Owner.Player.Associate.Associat[Role.Instance.Associate.Apprentice].ContainsKey(Apprentice.Player.UID))
                        continue;
                    Role.Player target = Apprentice.Player;

                    target.SetMentorBattlePowers(Owner.Player.GetShareBattlePowers((uint)target.RealBattlePower), (uint)Owner.Player.RealBattlePower);

                    Information.Apprentice_ID = target.UID;
                    Information.Enrole_date = (uint)Owner.Player.Associate.Associat[Role.Instance.Associate.Apprentice][target.UID].Timer;
                    Information.Level = (byte)Owner.Player.Level;
                    Information.Class = Owner.Player.Class;
                    Information.PkPoints = Owner.Player.PKPoints;
                    Information.Mesh = Owner.Player.Mesh;
                    Information.Online = 1;
                    Information.Shared_Battle_Power = Owner.Player.GetShareBattlePowers((uint)target.RealBattlePower);
                    Information.WriteString(Owner.Player.Name, Owner.Player.Spouse, target.Name);
                    target.Owner.Send(Information.GetArray(stream));
                }
            }
        }
        public void IncreaseRifainaryStatus(uint ID)
        {
            Database.Rifinery.Item refinery;
            if (Database.Server.RifineryItems.TryGetValue(ID, out refinery))
            {
                // var refinery = Database.Server.RifineryItems[ID];
                switch (refinery.Type)
                {
                    case Database.Rifinery.RefineryType.CriticalStrike:
                        {
                            Owner.Status.CriticalStrike += refinery.Procent * 100;
                            break;
                        }
                    case Database.Rifinery.RefineryType.SkillCriticalStrike:
                        {
                            Owner.Status.SkillCStrike += refinery.Procent * 100;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Break:
                        {
                            Owner.Status.Breakthrough += refinery.Procent * 10;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Detoxication:
                        {
                            Owner.Status.Detoxication += refinery.Procent;
                            break;
                        }
                    case Database.Rifinery.RefineryType.MDefence:
                        {
                            Owner.Status.PhysicalDamageDecrease += refinery.Procent;
                            Owner.Status.MDefence += refinery.Procent;
                            break;
                        }

                    case Database.Rifinery.RefineryType.Block:
                        {
                            Owner.Status.Block += refinery.Procent * 100;
                            break;
                        }

                    case Database.Rifinery.RefineryType.Immunity:
                        {
                            Owner.Status.Immunity += refinery.Procent * 100;
                            break;
                        }

                    case Database.Rifinery.RefineryType.Penetration:
                        {
                            Owner.Status.Penetration += refinery.Procent * 100;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Intensification:
                        {
                            Owner.Status.MaxHitpoints += refinery.Procent;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Counteraction:
                        {
                            Owner.Status.Counteraction += refinery.Procent * 10;
                            break;
                        }
                    case Database.Rifinery.RefineryType.FinalMDamage:
                        {

                            Owner.Status.PhysicalDamageIncrease += refinery.Procent;

                            break;
                        }
                    case Database.Rifinery.RefineryType.FinalMAttack:
                        {
                            Owner.Status.MagicDamageIncrease += refinery.Procent;
                            break;
                        }
                }
                switch (refinery.Type2)
                {
                    case Database.Rifinery.RefineryType.CriticalStrike:
                        {
                            Owner.Status.CriticalStrike += refinery.Procent2 * 100;
                            break;
                        }
                    case Database.Rifinery.RefineryType.SkillCriticalStrike:
                        {
                            Owner.Status.SkillCStrike += refinery.Procent2 * 100;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Break:
                        {
                            Owner.Status.Breakthrough += refinery.Procent2 * 10;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Detoxication:
                        {
                            Owner.Status.Detoxication += refinery.Procent2;
                            break;
                        }
                    case Database.Rifinery.RefineryType.MDefence:
                        {
                            Owner.Status.PhysicalDamageDecrease += refinery.Procent2;
                            break;
                        }

                    case Database.Rifinery.RefineryType.Block:
                        {
                            Owner.Status.Block += refinery.Procent2;
                            break;
                        }

                    case Database.Rifinery.RefineryType.Immunity:
                        {
                            Owner.Status.Immunity += refinery.Procent2 * 100;
                            break;
                        }

                    case Database.Rifinery.RefineryType.Penetration:
                        {
                            Owner.Status.Penetration += refinery.Procent2 * 100;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Intensification:
                        {
                            Owner.Status.MaxHitpoints += refinery.Procent2;
                            break;
                        }
                    case Database.Rifinery.RefineryType.Counteraction:
                        {
                            Owner.Status.Counteraction += refinery.Procent2;
                            break;
                        }
                    case Database.Rifinery.RefineryType.FinalMDamage:
                        {

                            Owner.Status.PhysicalDamageIncrease += refinery.Procent2;

                            break;
                        }
                    case Database.Rifinery.RefineryType.FinalMAttack:
                        {
                            Owner.Status.MagicDamageIncrease += refinery.Procent2;
                            break;
                        }
                }
            }
            else
            {
                Console.WriteLine("error refinery id " + ID);
            }
        }
        public void AddGem(Role.Flags.Gem gem)
        {
            switch (gem)
            {
                case Role.Flags.Gem.SuperThunderGem:
                case Role.Flags.Gem.SuperGloryGem: Owner.AddGem(gem, 500); break;
                case Role.Flags.Gem.RefinedGloryGem:
                case Role.Flags.Gem.RefinedThunderGem: Owner.AddGem(gem, 300); break;
                case Role.Flags.Gem.NormalGloryGem:
                case Role.Flags.Gem.NormalThunderGem: Owner.AddGem(gem, 100); break;
                case Role.Flags.Gem.NormalPhoenixGem:
                case Role.Flags.Gem.NormalDragonGem: Owner.AddGem(gem, 5); break;
                case Role.Flags.Gem.RefinedPhoenixGem:
                case Role.Flags.Gem.RefinedDragonGem: Owner.AddGem(gem, 10); break;
                case Role.Flags.Gem.SuperPhoenixGem:
                case Role.Flags.Gem.SuperDragonGem: Owner.AddGem(gem, 15); break;
                case Role.Flags.Gem.NormalTortoiseGem: Owner.AddGem(gem, 2); break;//1
                case Role.Flags.Gem.RefinedTortoiseGem: Owner.AddGem(gem, 4); break;//2
                case Role.Flags.Gem.SuperTortoiseGem: Owner.AddGem(gem, 6); break;//3


                case Role.Flags.Gem.SuperRainbowGem: Owner.AddGem(gem, 25); break;
                case Flags.Gem.RefinedRainbowGem: Owner.AddGem(gem, 15); break;
                case Flags.Gem.NormalRainbowGem: Owner.AddGem(gem, 10); break;

            }
        }
        public int BattlePower = 0;
        public void CalculateBattlePower()
        {
            BattlePower = 0;
            int val = 0;
            int val_item = 0;

            foreach (var item in CurentEquip)
            {
                if (Database.ItemType.ItemPosition(item.ITEM_ID) == (ushort)Role.Flags.ConquerItem.Bottle
                    || Database.ItemType.ItemPosition(item.ITEM_ID) == (ushort)Role.Flags.ConquerItem.Garment
                    || Database.ItemType.ItemPosition(item.ITEM_ID) == (ushort)Role.Flags.ConquerItem.LeftWeaponAccessory
                    || Database.ItemType.ItemPosition(item.ITEM_ID) == (ushort)Role.Flags.ConquerItem.RightWeaponAccessory
                    || Database.ItemType.ItemPosition(item.ITEM_ID) == (ushort)Role.Flags.ConquerItem.SteedMount)
                    continue;
                val_item = 0;
                byte Quality = (byte)(item.ITEM_ID % 10);
                switch (Quality)
                {
                    case 9: val_item += 4; break;
                    case 8: val_item += 3; break;
                    case 7: val_item += 2; break;
                    case 6: val_item += 1; break;
                }
                val_item += item.Plus;

                if (item.SocketOne != Role.Flags.Gem.NoSocket)
                    val_item += 1;
                if ((byte)(((byte)item.SocketOne % 10) - 3) == 0)
                    val_item += 1;
                if (item.SocketTwo != Role.Flags.Gem.NoSocket)
                    val_item += 1;
                if ((byte)(((byte)item.SocketTwo % 10) - 3) == 0)
                    val_item += 1;

                if (Database.ItemType.IsBacksword(item.ITEM_ID))
                {
                    val_item *= 2;
                }
                else if (Database.ItemType.IsTwoHand(item.ITEM_ID) && FreeEquip(Flags.ConquerItem.LeftWeapon) && FreeEquip(Flags.ConquerItem.AleternanteLeftWeapon))
                {
                    val_item += val_item;
                }

                val += val_item;
            }
            BattlePower = val;
        }
        private Tuple<Game.MsgServer.MsgGameItem, Game.MsgServer.MsgGameItem> ComputeWeapons()
        {
            if (!Alternante)
            {
                return new Tuple<Game.MsgServer.MsgGameItem, Game.MsgServer.MsgGameItem>(
                TryGetEquip(Flags.ConquerItem.RightWeapon),
                TryGetEquip(Flags.ConquerItem.LeftWeapon));
            }
            else
            {
                if (FreeEquip(Flags.ConquerItem.AleternanteRightWeapon))
                {
                    return new Tuple<Game.MsgServer.MsgGameItem, Game.MsgServer.MsgGameItem>(
                    TryGetEquip(Flags.ConquerItem.RightWeapon),
                    TryGetEquip(Flags.ConquerItem.LeftWeapon));
                }
                else
                {
                    if (FreeEquip(Flags.ConquerItem.RightWeapon))
                    {
                        return new Tuple<Game.MsgServer.MsgGameItem, Game.MsgServer.MsgGameItem>(
                        TryGetEquip(Flags.ConquerItem.AleternanteRightWeapon),
                        TryGetEquip(Flags.ConquerItem.AleternanteLeftWeapon));
                    }
                    else
                    {
                        if (!FreeEquip(Flags.ConquerItem.AleternanteLeftWeapon))
                        {
                            return new Tuple<Game.MsgServer.MsgGameItem, Game.MsgServer.MsgGameItem>(
                            TryGetEquip(Flags.ConquerItem.AleternanteRightWeapon),
                            TryGetEquip(Flags.ConquerItem.AleternanteLeftWeapon));
                        }
                        else
                        {
                            if (FreeEquip(Flags.ConquerItem.LeftWeapon))
                            {
                                return new Tuple<Game.MsgServer.MsgGameItem, Game.MsgServer.MsgGameItem>(
                                TryGetEquip(Flags.ConquerItem.AleternanteRightWeapon),
                                    null);
                            }
                            else
                            {
                                Game.MsgServer.MsgGameItem aRight = TryGetEquip(Flags.ConquerItem.AleternanteRightWeapon),
                                             nLeft = TryGetEquip(Flags.ConquerItem.LeftWeapon);
                                if (Database.ItemType.IsTwoHand(aRight.ITEM_ID))
                                {
                                    if (Database.ItemType.IsArrow(nLeft.ITEM_ID))
                                    {
                                        if (Database.ItemType.IsBow(aRight.ITEM_ID))
                                        {
                                            return new Tuple<Game.MsgServer.MsgGameItem,
                                                Game.MsgServer.MsgGameItem>(aRight, nLeft);
                                        }
                                        else
                                        {
                                            return new Tuple<Game.MsgServer.MsgGameItem,
                                                Game.MsgServer.MsgGameItem>(aRight, null);
                                        }
                                    }
                                    else
                                    {
                                        if (Database.ItemType.IsShield(nLeft.ITEM_ID))
                                        {
                                            if (!Owner.MySpells.ClientSpells.ContainsKey(10311))
                                            {
                                                //  Owner.Send(new MsgMessage("You need to know Perseverance (Pure Warrior skill) to be able to wear 2-handed weapon and shield.", MsgMessage.MsgColor.red, MsgMessage.ChatMode.Talk).GetArray(stream));
                                                return new Tuple<Game.MsgServer.MsgGameItem,
                                                    Game.MsgServer.MsgGameItem>(aRight, null);
                                            }
                                            else
                                            {
                                                return new Tuple<Game.MsgServer.MsgGameItem,
                                                    Game.MsgServer.MsgGameItem>(aRight, nLeft);
                                            }
                                        }
                                        else
                                        {
                                            return new Tuple<Game.MsgServer.MsgGameItem,
                                                Game.MsgServer.MsgGameItem>(aRight, null);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!Database.ItemType.IsTwoHand(nLeft.ITEM_ID))
                                    {
                                        if (Database.ItemType.IsKnife(aRight.ITEM_ID))
                                        {
                                            return new Tuple<Game.MsgServer.MsgGameItem,
                                                Game.MsgServer.MsgGameItem>(aRight, null);
                                        }
                                        else
                                        {
                                            return new Tuple<Game.MsgServer.MsgGameItem,
                                                Game.MsgServer.MsgGameItem>(aRight, null);
                                        }
                                    }
                                    else
                                    {
                                        return new Tuple<Game.MsgServer.MsgGameItem,
                                            Game.MsgServer.MsgGameItem>(aRight, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnDequeue()
        {
            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();
                try
                {

                    Dictionary<uint, Game.MsgServer.MsgGameItem> statusitens = new Dictionary<uint, Game.MsgServer.MsgGameItem>();
                    foreach (var it in AllItems)
                    {
                        if (Alternante)
                            if (it.Position < 20 && !FreeEquip((Flags.ConquerItem)(it.Position + 20)))
                                continue;
                        if (!Alternante)
                            if (it.Position > 20)
                                continue;
                        Flags.ConquerItem pos = (Flags.ConquerItem)it.Position;
                        if ((int)pos > 20) pos -= 20;
                        if (pos == Flags.ConquerItem.LeftWeapon || pos == Flags.ConquerItem.RightWeapon) continue;
                        if (!statusitens.ContainsKey(it.Position))
                            statusitens.Add(it.Position, it);
                    }
                    Owner.Weapons = ComputeWeapons();
                    if (Owner.Weapons == null) Owner.Weapons = new Tuple<MsgGameItem, MsgGameItem>(null, null);
                    if (Owner.Weapons.Item1 != null)
                    {
                        if (!statusitens.ContainsKey(Owner.Weapons.Item1.Position))
                            statusitens.Add(Owner.Weapons.Item1.Position, Owner.Weapons.Item1);
                        if (Owner.Weapons.Item2 != null)
                        {
                            var client = Owner;
                            var item = Owner.Weapons.Item2;
                            if (Database.ItemType.IsPistol(item.ITEM_ID))
                            {
                                if (!Database.ItemType.IsRapier(Owner.Weapons.Item1.ITEM_ID))
                                    goto jmp;
                            }
                            bool can2hand = false;
                            bool can2wpn = false;
                            if (client.Player.Class >= 11 && client.Player.Class <= 75)
                                can2hand = true;
                            if (Database.AtributesStatus.IsTrojan(client.Player.Class) || Database.AtributesStatus.IsWarrior(client.Player.Class) || client.Player.Class >= 51 && client.Player.Class <= 145)
                                can2wpn = true;

                            if (Database.AtributesStatus.IsArcher(client.Player.Class))
                            {
                                if (Database.ItemType.IsTwoHand(item.ITEM_ID) && client.Player.IsAsasin)
                                    goto jmp;
                                can2wpn = true;
                            }
                            if (Database.ItemType.IsTwoHand(Owner.Weapons.Item1.ITEM_ID))
                            {
                                if (Database.ItemType.IsShield(item.ITEM_ID) == false)
                                    goto jmp;
                            }
                            if (Database.ItemType.IsKnife(Owner.Weapons.Item1.ITEM_ID))
                            {
                                if (Database.ItemType.IsKnife(item.ITEM_ID) == false)
                                {
                                    client.SendSysMesage("Remove the right-hand Knife.", MsgMessage.ChatMode.System);
                                    goto jmp;
                                }
                            }
                            if (Database.ItemType.IsDagger(Owner.Weapons.Item1.ITEM_ID))
                            {
                                if (Database.ItemType.IsDagger(item.ITEM_ID) == false)
                                {
                                    client.SendSysMesage("Remove the right-hand IsDagger.", MsgMessage.ChatMode.System);
                                    goto jmp;
                                }
                            }
                            if (Database.ItemType.IsDagger(item.ITEM_ID) == true)
                                goto jmp;
                            if (Database.ItemType.IsBacksword(Owner.Weapons.Item1.ITEM_ID))
                            {
                                if (Database.ItemType.IsShield(item.ITEM_ID) == false)
                                    goto jmp;
                            }

                            if (Database.AtributesStatus.IsTaoist(client.Player.Class))
                            {
                                if (Database.ItemType.IsShield(item.ITEM_ID) == true)
                                    goto jmp;
                            }

                            if (!can2hand && !can2wpn)
                                goto jmp;
                            if (Owner.Weapons.Item1.ITEM_ID / 1000 != 500 && Database.ItemType.IsArrow(item.ITEM_ID))
                                goto jmp;
                            if (!Owner.Weapons.Item1.IsTwoHander()
                                || (Owner.Weapons.Item1.IsTwoHander() && Database.ItemType.IsArrow(Owner.Weapons.Item2.ITEM_ID) || (Owner.Player.Class >= 20 && Owner.Player.Class <= 25)))
                                if (!statusitens.ContainsKey(Owner.Weapons.Item2.Position))
                                    statusitens.Add(Owner.Weapons.Item2.Position, Owner.Weapons.Item2);
                        }
                    }
                jmp:
                    AppendItems(CreateSpawn, statusitens.Values.ToArray(), stream);
                    UpdateStats(statusitens.Values.ToArray(), stream);

                    Owner.Player.HitPoints = Math.Min((int)Owner.Player.HitPoints, (int)Owner.Status.MaxHitpoints);
                    if (Owner.Player.OnTransform && Owner.Player.TransformInfo != null)
                        Owner.Player.TransformInfo.UpdateStatus();
                    else
                        Owner.Player.SendUpdateHP();
                    Owner.ClanShareBP();
                }
                catch (Exception e)
                {
                    Console.SaveException(e);
                }
            }
        }
        public void AppendItems(bool CreateSpawn, Game.MsgServer.MsgGameItem[] Items, ServerSockets.Packet stream)
        {
            Game.MsgServer.MsgShowEquipment ShowEquip = new MsgShowEquipment();
            ShowEquip.wParam = Game.MsgServer.MsgShowEquipment.Show;
            ShowEquip.Alternante = (byte)(Alternante ? 1 : 0);

            if (CreateSpawn)
            {
                foreach (var item in Items)
                {
                    if (item != null)
                    {
                        switch ((Role.Flags.ConquerItem)item.Position)
                        {
                            case Flags.ConquerItem.Ring:
                            case Flags.ConquerItem.AleternanteRing: ShowEquip.Ring = item.UID; break;
                            case Flags.ConquerItem.AleternanteHead:
                            case Flags.ConquerItem.Head: ShowEquip.Head = item.UID; break;
                            case Flags.ConquerItem.AleternanteNecklace:
                            case Flags.ConquerItem.Necklace: ShowEquip.Necklace = item.UID; break;
                            case Flags.ConquerItem.AleternanteRightWeapon:
                            case Flags.ConquerItem.RightWeapon: ShowEquip.RightWeapon = item.UID; break;
                            case Flags.ConquerItem.AleternanteLeftWeapon:
                            case Flags.ConquerItem.LeftWeapon: ShowEquip.LeftWeapon = item.UID; break;
                            case Flags.ConquerItem.AleternanteArmor:
                            case Flags.ConquerItem.Armor:
                                {
                                    ShowEquip.Armor = item.UID;
                                    break;
                                }
                            case Flags.ConquerItem.AleternanteBoots:
                            case Flags.ConquerItem.Boots: ShowEquip.Boots = item.UID; break;
                            case Flags.ConquerItem.AleternanteBottle:
                            case Flags.ConquerItem.Bottle: ShowEquip.Bottle = item.UID; break;
                            case Flags.ConquerItem.SteedMount: ShowEquip.SteedMount = item.UID; break;
                            case Flags.ConquerItem.AleternanteGarment:
                            case Flags.ConquerItem.Garment:
                                {
                                    ShowEquip.Garment = item.UID;
                                    break;
                                }
                            case Flags.ConquerItem.RidingCrop: ShowEquip.RidingCrop = item.UID; break;
                            case Flags.ConquerItem.LeftWeaponAccessory: ShowEquip.LeftWeaponAccessory = item.UID; break;
                            case Flags.ConquerItem.RightWeaponAccessory: ShowEquip.RightWeaponAccessory = item.UID; break;
                        }
                    }
                }
                if (Owner.Player.SpecialGarment != 0)
                    ShowEquip.Garment = uint.MaxValue - 1;
                if (Owner.Player.SpecialitemR != 0)
                    ShowEquip.RightWeapon = uint.MaxValue - 10;
                if (Owner.Player.SpecialitemL != 0)
                    ShowEquip.LeftWeapon = uint.MaxValue - 11;
                Owner.Send(stream.ShowEquipmentCreate(ShowEquip));


            }
        }
        public unsafe void SendAlowAlternante(ServerSockets.Packet stream)
        {
            Game.MsgServer.MsgShowEquipment ShowEquip = new MsgShowEquipment();
            ShowEquip.wParam = Game.MsgServer.MsgShowEquipment.AlternanteAllow;
            ShowEquip.Alternante = (byte)(Alternante ? 1 : 0);
            Owner.Send(stream.ShowEquipmentCreate(ShowEquip));
        }
        public unsafe void QueryEquipment(bool Alternantes, bool CallItems = true)
        {
            this.Alternante = Alternantes;
            CreateSpawn = CallItems;
            OnDequeue();
        }

        public bool DestoyArrow(Role.Flags.ConquerItem position, ServerSockets.Packet stream)
        {
            if (!FreeEquip(position))
            {
                Game.MsgServer.MsgGameItem itemdata;
                if (TryGetEquip(position, out itemdata))
                {
                    if (!(itemdata.ITEM_ID >= 1050000 && itemdata.ITEM_ID <= 1051000))
                        return false;
                    if (ClientItems.TryRemove(itemdata.UID, out itemdata))
                    {
                        Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.Unequip, itemdata.UID, itemdata.Position, 0, 0, 0, 0));
                        itemdata.Position = 0;
                        itemdata.Mode = Flags.ItemMode.AddItem;
                        Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.RemoveInventory, itemdata.UID, 0, 0, 0, 0, 0));
                    }
                }
            }
            return false;
        }
    }
}
