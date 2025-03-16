using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using COServer.Game.MsgServer;
using System.Diagnostics;
using COServer.Database;

namespace COServer.Role.Instance
{

    public class Inventory
    {
        private const byte File_Size = 40;

        public ConcurrentDictionary<uint, Game.MsgServer.MsgGameItem> ClientItems = new ConcurrentDictionary<uint, Game.MsgServer.MsgGameItem>();



        public int GetCountItem(uint ItemID)
        {
            int count = 0;
            foreach (var DataItem in ClientItems.Values)
            {
                if (DataItem.ITEM_ID == ItemID)
                {
                    count += DataItem.StackSize > 1 ? DataItem.StackSize : 1;
                }
            }
            return count;
        }

        public bool VerifiedUpdateItem(List<uint> ItemsUIDS, uint ID, byte count, out Queue<Game.MsgServer.MsgGameItem> Items)
        {
            Queue<Game.MsgServer.MsgGameItem> ExistItems = new Queue<Game.MsgServer.MsgGameItem>();
            foreach (var DataItem in ClientItems.Values)
            {
                if (DataItem.ITEM_ID == ID)
                {
                    if (ItemsUIDS.Contains(DataItem.UID))
                    {
                        count--;
                        ItemsUIDS.Remove(DataItem.UID);
                        ExistItems.Enqueue(DataItem);
                    }
                }
            }
            Items = ExistItems;
            return ItemsUIDS.Count == 0 && count == 0;
        }

        private Client.GameClient Owner;
        public Inventory(Client.GameClient _own)
        {
            Owner = _own;
        }

        public void AddDBItem(Game.MsgServer.MsgGameItem item)
        {
            ClientItems.TryAdd(item.UID, item);
        }
        public bool AddItemTime2(uint ID, ServerSockets.Packet stream, bool bound, int days = 0, int hours = 0, int mins = 0)
        {
            Database.ItemType.DBItem ITEMDB = null;
            if (!Database.Server.ItemsBase.TryGetValue(ID, out ITEMDB))
                return false;
            if (HaveSpace(1))
            {
                MsgGameItem ItemDat = new MsgGameItem();
                ItemDat.UID = Database.Server.ITEM_Counter.Next;
                ItemDat.ITEM_ID = ID;
                ItemDat.Durability = ItemDat.MaximDurability = ITEMDB.Durability;
                ItemDat.Bound = (byte)(bound ? 1 : 0);
                ITEMDB.Time = 100;
                //if (days == 0 && hours == 0 && mins == 0)
                //{
                //    if (ITEMDB.Time != 0 && ITEMDB.StackSize == 0)
                //    {
                //        ItemDat.Activate = 1;
                //        ItemDat.EndDate = DateTime.Now.AddMinutes(ITEMDB.Time);
                //    }
                //}
                ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                try
                {
                    Update(ItemDat, AddMode.ADD, stream);
                }
                catch (Exception e)
                {
                    Console.SaveException(e);
                }
                return true;
            }
            return false;
        }
        public bool AddItemTime(uint ID, ServerSockets.Packet stream, bool bound, int days = 0, int hours = 0, int mins = 0)
        {
            Database.ItemType.DBItem ITEMDB = null;
            if (!Database.Server.ItemsBase.TryGetValue(ID, out ITEMDB))
                return false;
            if (HaveSpace(1))
            {
                MsgGameItem ItemDat = new MsgGameItem();
                ItemDat.UID = Database.Server.ITEM_Counter.Next;
                ItemDat.ITEM_ID = ID;
                ItemDat.Durability = ItemDat.MaximDurability = ITEMDB.Durability;
                ItemDat.Bound = (byte)(bound ? 1 : 0);
                ITEMDB.Time = 100;
                //if (days == 0 && hours == 0 && mins == 0)
                //{
                //    if (ITEMDB.Time != 0 && ITEMDB.StackSize == 0)
                //    {
                //        ItemDat.Activate = 1;
                //        ItemDat.EndDate = DateTime.Now.AddMinutes(ITEMDB.Time);
                //    }
                //}
                //else if (days != 0)
                //{
                //    ItemDat.Activate = 1;
                //    ItemDat.EndDate = DateTime.Now.AddDays(days);
                //}
                //else if (hours != 0)
                //{
                //    ItemDat.Activate = 1;
                //    ItemDat.EndDate = DateTime.Now.AddHours(hours);
                //}
                //else
                //{
                //    ItemDat.Activate = 1;
                //    ItemDat.EndDate = DateTime.Now.AddMinutes(mins);
                //}
                ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                try
                {
                    Update(ItemDat, AddMode.ADD, stream);
                }
                catch (Exception e)
                {
                    Console.SaveException(e);
                }
                return true;
            }
            return false;
        }

        public void AddReturnedItem(ServerSockets.Packet stream, uint ID, byte count = 1, byte plus = 0, byte bless = 0, byte Enchant = 0
            , Role.Flags.Gem sockone = Flags.Gem.NoSocket
             , Role.Flags.Gem socktwo = Flags.Gem.NoSocket, bool bound = false, Role.Flags.ItemEffect Effect = Flags.ItemEffect.None, ushort StackSize = 0)
        {

            byte x = 0;
            for (; x < count;)
            {
                x++;
                Database.ItemType.DBItem DbItem;
                if (Database.Server.ItemsBase.TryGetValue(ID, out DbItem))
                {

                    Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                    ItemDat.UID = Database.Server.ITEM_Counter.Next;
                    ItemDat.ITEM_ID = ID;
                    ItemDat.Effect = Effect;
                    ItemDat.StackSize = StackSize;
                    ItemDat.Durability = ItemDat.MaximDurability = DbItem.Durability;
                    ItemDat.Plus = plus;
                    ItemDat.Bless = bless;
                    ItemDat.Enchant = Enchant;
                    ItemDat.SocketOne = sockone;
                    ItemDat.SocketTwo = socktwo;
                    ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                    ItemDat.Bound = (byte)(bound ? 1 : 0);
                    ItemDat.Mode = Flags.ItemMode.AddItemReturned;
                    ItemDat.WH_ID = ushort.MaxValue;
                    if (DbItem.Time != 0 && DbItem.StackSize == 1)
                    {
                        ItemDat.Activate = 1;
                        ItemDat.EndDate = DateTime.Now.AddMinutes(DbItem.Time);
                    }
                    Owner.Warehouse.AddItem(ItemDat, ushort.MaxValue);

                    ItemDat.Send(Owner, stream);
                }
            }
        }
        public bool Add(uint ID, byte Count , bool Bound, ServerSockets.Packet stream)
        {
            return Add(stream, ID, Count, 0, 0, 0, Flags.Gem.NoSocket, Flags.Gem.NoSocket, Bound);
        }

        public bool HaveSpace(byte count)
        {
            return (ClientItems.Count + count) <= File_Size;
        }

        public bool TryGetItem(uint UID, out Game.MsgServer.MsgGameItem item)
        {
            return ClientItems.TryGetValue(UID, out item);
        }
        public bool SearchItemByID(uint ID, out Game.MsgServer.MsgGameItem item)
        {
            foreach (var msg_item in ClientItems.Values)
            {
                if (msg_item.ITEM_ID == ID)
                {
                    item = msg_item;
                    return true;
                }
            }
            item = null;
            return false;
        }

        public bool SearchItemByID(uint ID, byte count, out List<Game.MsgServer.MsgGameItem> Items)
        {
            byte increase = 0;
            Items = new List<Game.MsgServer.MsgGameItem>();
            foreach (var msg_item in ClientItems.Values)
            {
                if (msg_item.ITEM_ID == ID)
                {
                    Items.Add(msg_item);
                    increase++;
                    if (increase == count)
                    {
                        return true;
                    }
                }
            }
            Items = null;
            return false;
        }
        private static List<uint> WarriorHead = new List<uint>()
        {
            112313,112314,112315,112316,112317,112318,112319,
            112413,112414,112415,112416,112417,112418,112419,
            112513,112514,112515,112516,112517,112518,112519,
            112613,112614,112615,112616,112617,112618,112619,
            112713,112714,112715,112716,112717,112718,112719,
            112813,112814,112815,112816,112817,112818,112819,
            112913,112914,112915,112916,112917,112918,112919
        };
        private static List<uint> ArcherHead = new List<uint>()
        {
            112333,112334,112335,112336,112337,112338,112339,
            112433,112434,112435,112436,112437,112438,112439,
            112533,112534,112535,112536,112537,112538,112539,
            112633,112634,112635,112636,112637,112638,112639,
            112733,112734,112735,112736,112737,112738,112739,
            112833,112834,112835,112836,112837,112838,112839,
            112933,112934,112935,112936,112937,112938,112939
        };
        private static List<uint> WaterHead = new List<uint>()
        {
            112433,112344,112445,112446,112447,112448,112449,
            112443,112444,112445,112446,112447,112448,112449,
            112543,112544,112545,112546,112547,112548,112549,
            112643,112644,112645,112646,112647,112648,112649,
            112743,112744,112745,112746,112747,112748,112749,
            112843,112844,112845,112846,112847,112848,112849,
            112943,112944,112945,112946,112947,112948,112949
        };
        private static List<uint> TrojenHead = new List<uint>()
        {
            112483,112384,112485,112486,112487,112488,112889,
            112883,112844,112485,112486,112487,112488,112889,
            112583,112584,112585,112586,112587,112588,112589,
            112683,112684,112685,112686,112687,112688,112689,
            112783,112784,112785,112786,112787,112788,112789,
            112883,112884,112885,112886,112887,112888,112889,
            112983,112984,112985,112986,112987,112988,112989
        };
        #region GoldOre Quest
        private static List<uint> GoldOre = new List<uint>()
        {
            1072050,1072051,1072052,1072053,1072054,1072055,1072056,
            1072057,1072058,1072059
        };
        public bool ContainGoldOre(uint Amount)
        {
            uint count = 0;
            foreach (var item in ClientItems.Values)
            {
                if (GoldOre.Contains(item.ITEM_ID))
                {
                    count += item.StackSize;
                    if (count >= Amount)
                        return true;
                }
            }
            return false;
        }
        public bool RemoveGoldOre(uint count, ServerSockets.Packet stream)
        {
            if (ContainGoldOre(count))
            {
                byte removed = 0;
                for (byte x = 0; x < count; x++)
                {
                    foreach (var item in ClientItems.Values)
                    {
                        if (GoldOre.Contains(item.ITEM_ID))
                        {
                            try
                            {
                                Update(item, AddMode.REMOVE, stream);
                            }
                            catch (Exception e)
                            {
                                Console.SaveException(e);
                            }
                            removed++;
                            if (removed == count)
                            {
                                break;
                            }
                        }
                    }
                    if (removed == count)
                        break;
                }
                return true;
            }
            return false;
        }
        public void RemoveGem(Client.GameClient client, ServerSockets.Packet stream)
        {
            client.Inventory.Remove(ItemType.RefinedPhoenixGem, 1, stream);
            client.Inventory.Remove(ItemType.RefinedDragonGem, 1, stream);
            client.Inventory.Remove(ItemType.RefinedFuryGem, 1, stream);
            client.Inventory.Remove(ItemType.RefinedRainbowGem, 1, stream);
            client.Inventory.Remove(ItemType.RefinedKylinGem, 1, stream);
            client.Inventory.Remove(ItemType.RefinedVioletGem, 1, stream);
            client.Inventory.Remove(ItemType.RefinedMoonGem, 1, stream);
        }
        #endregion
        #region RefinedGem [TradeTreasures Quest]
        private static List<uint> RefinedGem = new List<uint>()
        {
            700002,700012,700022,700032,700042,700052,700062,700072,
            700102,700122
        };
        public bool ContainRefinedGem(uint Amount)
        {
            uint count = 0;
            foreach (var item in ClientItems.Values)
            {
                if (RefinedGem.Contains(item.ITEM_ID))
                {
                    count += item.StackSize;
                    if (count >= Amount)
                        return true;
                }
            }
            return false;
        }
        public bool RemoveRefinedGem(uint count, ServerSockets.Packet stream)
        {
            if (ContainRefinedGem(count))
            {
                byte removed = 0;
                for (byte x = 0; x < count; x++)
                {
                    foreach (var item in ClientItems.Values)
                    {
                        if (RefinedGem.Contains(item.ITEM_ID))
                        {
                            try
                            {
                                Update(item, AddMode.REMOVE, stream);
                            }
                            catch (Exception e)
                            {
                                Console.SaveException(e);
                            }
                            removed++;
                            if (removed == count)
                            {
                                break;
                            }
                        }
                    }
                    if (removed == count)
                        break;
                }
                return true;
            }
            return false;
        }
        #endregion
        public bool ContainHead(uint Amount, byte plus = 0, string character = "")
        {
            uint count = 0;
            foreach (var item in ClientItems.Values)
            {
                if (item.Plus == plus)
                {
                    #region ConquestHelmet [ Warrior ]
                    if (character == "Warrior")
                    {
                        if (WarriorHead.Contains(item.ITEM_ID))
                        {
                            count += item.StackSize;
                            if (count >= Amount)
                                return true;
                        }
                    }
                    #endregion
                    #region PhoenixHat [ Archer ]
                    else if (character == "Archer")
                    {
                        if (ArcherHead.Contains(item.ITEM_ID))
                        {
                            count += item.StackSize;
                            if (count >= Amount)
                                return true;
                        }
                    }
                    #endregion
                    #region UltimateCap [ Water ]
                    else if (character == "Water")
                    {
                        if (WaterHead.Contains(item.ITEM_ID))
                        {
                            count += item.StackSize;
                            if (count >= Amount)
                                return true;
                        }
                    }
                    #endregion
                    #region MagicCoronet [ Trojen ]
                    else if (character == "Trojen")
                    {
                        if (TrojenHead.Contains(item.ITEM_ID))
                        {
                            count += item.StackSize;
                            if (count >= Amount)
                                return true;
                        }
                    }
                    #endregion
                }
            }
            return false;
        }
        public bool RemoveHead(ServerSockets.Packet stream, uint count, byte plus = 0, string character = "")
        {
            byte newplus = 0;
        again:
            if (ContainItemsPlus(count, plus))
            {
                byte removed = 0;
                for (byte x = 0; x < count; x++)
                {
                    foreach (var item in ClientItems.Values)
                    {
                        if (item.Plus == plus)
                        {
                            #region ConquestHelmet  [ Warrior ]
                            if (character == "Warrior")
                            {
                                if (WarriorHead.Contains(item.ITEM_ID))
                                {
                                    try
                                    {
                                        Update(item, AddMode.REMOVE, stream);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.SaveException(e);
                                    }
                                    removed++;
                                    if (removed == count)
                                    {
                                        newplus = item.Plus;
                                        newplus++;
                                        item.Plus = newplus;
                                        Update(item, AddMode.ADD, stream);
                                        goto again;
                                        break;
                                    }
                                }
                            }
                            #endregion
                            #region PhoenixHat [ Archer ]
                            if (character == "Archer")
                            {
                                if (ArcherHead.Contains(item.ITEM_ID))
                                {
                                    try
                                    {
                                        Update(item, AddMode.REMOVE, stream);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.SaveException(e);
                                    }
                                    removed++;
                                    if (removed == count)
                                    {
                                        newplus = item.Plus;
                                        newplus++;
                                        item.Plus = newplus;
                                        Update(item, AddMode.ADD, stream);
                                        goto again;
                                        break;
                                    }
                                }
                            }
                            #endregion
                            #region UltimateCap [ Water ]
                            if (character == "Water")
                            {
                                if (WaterHead.Contains(item.ITEM_ID))
                                {
                                    try
                                    {
                                        Update(item, AddMode.REMOVE, stream);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.SaveException(e);
                                    }
                                    removed++;
                                    if (removed == count)
                                    {
                                        newplus = item.Plus;
                                        newplus++;
                                        item.Plus = newplus;
                                        Update(item, AddMode.ADD, stream);
                                        goto again;
                                        break;
                                    }
                                }
                            }
                            #endregion
                            #region MagicCoronet [ Trojen ]
                            if (character == "Trojen")
                            {
                                if (TrojenHead.Contains(item.ITEM_ID))
                                {
                                    try
                                    {
                                        Update(item, AddMode.REMOVE, stream);
                                    }
                                    catch (Exception e)
                                    {
                                        Console.SaveException(e);
                                    }
                                    removed++;
                                    if (removed == count)
                                    {
                                        newplus = item.Plus;
                                        newplus++;
                                        item.Plus = newplus;
                                        Update(item, AddMode.ADD, stream);
                                        goto again;
                                        break;
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    if (removed == count)
                        break;
                }
                return true;
            }
            return false;
        }
        public bool ContainItemsPlus(uint Amount, byte plus = 0, uint position =0, byte bound = 0)
        {
            uint count = 0;
            foreach (var item in ClientItems.Values)
            {
                if (item.Plus == plus)
                {
                    if (item.ITEM_ID / 1000 == position)
                    {
                        count += item.StackSize;
                        if (count >= Amount)
                            return true;
                    }
                }
            }
            return false;
        }
        public bool RemoveItemsPlus(ServerSockets.Packet stream, uint count, byte plus, uint position = 0)
        {
            byte newplus = 0;
        again:
            if (ContainItemsPlus(count, plus, position))
            {
                byte removed = 0;
                for (byte x = 0; x < count; x++)
                {
                    foreach (var item in ClientItems.Values)
                    {
                        if (item.Plus == plus)
                        {
                            if (item.ITEM_ID / 1000 == (ushort)position)
                            {
                                try
                                {
                                    Update(item, AddMode.REMOVE, stream);
                                }
                                catch (Exception e)
                                {
                                    Console.SaveException(e);
                                }
                                removed++;
                                if (removed == count)
                                {
                                    newplus = item.Plus;
                                    newplus++;
                                    item.Plus = newplus;
                                    Update(item, AddMode.ADD, stream);
                                    goto again;
                                    break;
                                }
                            }
                        }
                    }
                    if (removed == count)
                        break;
                }
                return true;
            }
            return false;
        }
        public bool Contain(uint ID, uint Amount, byte bound = 0)
        {
            if (ID == Database.ItemType.Meteor || ID == Database.ItemType.MeteorTear)
            {
                uint count = 0;
                foreach (var item in ClientItems.Values)
                {
                    if (item.ITEM_ID == Database.ItemType.Meteor
                        || item.ITEM_ID == Database.ItemType.MeteorTear)
                    {
                        if (item.Bound == bound)
                        {
                            count += item.StackSize;
                            if (count >= Amount)
                                return true;
                        }
                    }
                }
            }
            else if (ID == Database.ItemType.MoonBox || ID == 723087)//execept for bound
            {
                uint count = 0;
                foreach (var item in ClientItems.Values)
                {
                    if (item.ITEM_ID == ID)
                    {
                        count += item.StackSize;
                        if (count >= Amount)
                            return true;
                    }
                }
            }
            else
            {
                uint count = 0;
                foreach (var item in ClientItems.Values)
                {
                    if (item.ITEM_ID == ID)
                    {
                        if (item.Bound == bound)
                        {
                            count += item.StackSize;
                            if (count >= Amount)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool Remove(uint ID, uint count, ServerSockets.Packet stream)
        {
            if (Contain(ID, count) || Contain(ID, count, 1))
            {
                if (ID == Database.ItemType.Meteor || ID == Database.ItemType.MeteorTear)
                {
                    byte removed = 0;
                    for (byte x = 0; x < count; x++)
                    {
                        foreach (var item in ClientItems.Values)
                        {
                            if (item.ITEM_ID == Database.ItemType.Meteor
                         || item.ITEM_ID == Database.ItemType.MeteorTear)
                            {
                                try
                                {
                                    Update(item, AddMode.REMOVE, stream);
                                }
                                catch (Exception e)
                                {
                                    Console.SaveException(e);
                                }
                                removed++;
                                if (removed == count)
                                    break;
                            }
                        }
                        if (removed == count)
                            break;
                    }
                }
                else
                {
                    byte removed = 0;
                    for (byte x = 0; x < count; x++)
                    {
                        foreach (var item in ClientItems.Values)
                        {
                            if (item.ITEM_ID == ID)
                            {
                                try
                                {
                                    Update(item, AddMode.REMOVE, stream);
                                }
                                catch (Exception e)
                                {
                                    Console.SaveException(e);
                                }
                                removed++;
                                if (removed == count)
                                    break;
                            }
                        }
                        if (removed == count)
                            break;
                    }
                }
                return true;
            }
            return false;
        }
        #region Desha
        public bool Add2(ServerSockets.Packet stream, uint ID, byte count = 1, byte plus = 0, byte bless = 0, byte Enchant = 0
            , Role.Flags.Gem sockone = Flags.Gem.NoSocket
             , Role.Flags.Gem socktwo = Flags.Gem.NoSocket, bool bound = false, Role.Flags.ItemEffect Effect = Flags.ItemEffect.None, bool SendMessage = false
            , string another_text = "", int days = 0, int hours = 0, int mins = 0, bool mine = false)
        {
            if (count == 0)
                count = 1;
            if (HaveSpace(count))
            {
                byte x = 0;
                for (; x < count;)
                {
                    x++;
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
                        if (SendMessage)
                        {
                            Owner.CreateBoxDialog("You~received~a~" + DbItem.Name + "" + another_text);
                        }

                        try
                        {
                            if (!Update(ItemDat, AddMode.ADD, stream))
                                return false;
                        }
                        catch (Exception e)
                        {
                            Console.SaveException(e);
                        }

                    }
                }
                if (x >= count)
                    return true;
            }

            return false;
        }
        #endregion

        public bool AddSteed(ServerSockets.Packet stream, uint ID, byte count = 1, byte plus = 0, bool bound = false, byte ProgresGreen = 0, byte ProgresBlue = 0, byte ProgresRed = 0)
        {
            if (count == 0)
                count = 1;
            if (HaveSpace(count))
            {
                for (byte x = 0; x < count; x++)
                {
                    Database.ItemType.DBItem DbItem;
                    if (Database.Server.ItemsBase.TryGetValue(ID, out DbItem))
                    {
                        Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                        ItemDat.UID = Database.Server.ITEM_Counter.Next;
                        ItemDat.ITEM_ID = ID;

                        ItemDat.ProgresGreen = ProgresGreen;
                        ItemDat.Enchant = ProgresBlue;
                        ItemDat.Bless = ProgresRed;
                        ItemDat.SocketProgress = (uint)(ProgresGreen | (ProgresBlue << 8) | (ProgresRed << 16));
                        ItemDat.Durability = ItemDat.MaximDurability = DbItem.Durability;
                        ItemDat.Plus = plus;
                        ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                        ItemDat.Bound = (byte)(bound ? 1 : 0);
                        try
                        {
                            if (!Update(ItemDat, AddMode.ADD, stream))
                                return false;
                        }
                        catch (Exception e)
                        {
                            Console.SaveException(e);
                        }
                        if (x >= count)
                            return true;
                    }
                }
            }
            return false;
        }
        public bool AddSoul(ServerSockets.Packet stream, Client.GameClient client, uint ID, uint SoulID, uint purfylevel, byte plus = 0, byte count = 1, Role.Flags.Gem sockone = 0, Role.Flags.Gem socktwo = 0, bool bound = false, Role.Flags.ItemEffect Effect = 0, bool SendMessage = false, string another_text = "")
        {
            if (count == 0)
            {
                count = 1;
            }
            if (this.HaveSpace(count))
            {
                byte num = 0;
                while (num < count)
                {
                    Database.ItemType.DBItem item;
                    num = (byte)(num + 1);
                    if (Database.Server.ItemsBase.TryGetValue(ID, out item))
                    {
                        MsgGameItem item2;
                        item2 = new MsgGameItem
                        {
                            UID = Database.Server.ITEM_Counter.Next,
                            ITEM_ID = ID,
                            Effect = Effect,
                            Durability = item.Durability,
                            Plus = plus,
                            Bless = 7,
                            Enchant = 0xff,
                            SocketOne = sockone,
                            SocketTwo = socktwo,
                            Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9),
                            Bound = bound ? ((byte)1) : ((byte)0),
                            Purification = new MsgItemExtra.Purification()
                        };
                        item2.Purification.AddedOn = DateTime.Now;
                        item2.Purification.ItemUID = item2.UID;
                        item2.Purification.PurificationLevel = purfylevel;
                        item2.Purification.PurificationDuration = 0;
                        item2.Purification.PurificationItemID = SoulID;
                        item2.Purification.Typ = MsgItemExtra.Typing.PurificationEffect;
                        MsgItemExtra extra = new MsgItemExtra
                        {
                            Purifications = { item2.Purification }
                        };
                        client.Send(extra.CreateArray(stream, false));
                        item2.Mode = Role.Flags.ItemMode.AddItem | Role.Flags.ItemMode.Trade;
                        item2.Send(client, stream);
                        if (SendMessage)
                        {
                            this.Owner.CreateBoxDialog("You~received~a~" + item.Name + another_text);
                        }
                        item2.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                        if (item.Time != 0)
                        {
                            item2.Activate = 1;
                            item2.EndDate = DateTime.Now.AddMinutes(item.Time);
                        }

                        try
                        {
                            if (!this.Update(item2, AddMode.ADD, stream, false))
                            {
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            Console.SaveException(exception);
                        }
                    }
                }
                if (num >= count)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Add(ServerSockets.Packet stream, uint ID, byte count = 1, byte plus = 0, byte bless = 0, byte Enchant = 0
            , Role.Flags.Gem sockone = Flags.Gem.NoSocket
             , Role.Flags.Gem socktwo = Flags.Gem.NoSocket, bool bound = false, Role.Flags.ItemEffect Effect = Flags.ItemEffect.None, bool SendMessage = false
            , string another_text = "", int DaysActive = 0)
        {
            if(ID == 1088000)
            {

            }
            if (count == 0)
                count = 1;
            if (HaveSpace(count))
            {
                byte x = 0;
                for (; x < count;)
                {
                    x++;
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
                        if (DbItem.Time != 0 && DbItem.StackSize == 0)
                        {
                            ItemDat.Activate = 1;
                            ItemDat.EndDate = DateTime.Now.AddMinutes(DbItem.Time);
                        }

                        // Console.WriteLine(Database.ItemType.GetItemPoints(DbItem, ItemDat));
                        if (SendMessage)
                        {
                            Owner.CreateBoxDialog("You~received~a~" + DbItem.Name + "" + another_text);
                        }

                        try
                        {
                            if (!Update(ItemDat, AddMode.ADD, stream))
                                return false;
                        }
                        catch (Exception e)
                        {
                            Console.SaveException(e);
                        }

                    }
                }
                if (x >= count)
                    return true;
            }

            return false;
        }
        public bool AddBoundItem(ServerSockets.Packet stream, uint ID, byte count = 1, bool Bound = true)
        {
            return Add(stream, ID, 1, 0, 0, 0, Flags.Gem.NoSocket, Flags.Gem.NoSocket, Bound);
        }
        public bool AddItemWitchStack(uint ID, byte Plus, ushort amount, ServerSockets.Packet stream, bool bound = false)
        {
            //return Add(stream, ID, (byte)amount, Plus, 0, 0, Flags.Gem.NoSocket, Flags.Gem.NoSocket, bound);
            Database.ItemType.DBItem DbItem;
            if (Database.Server.ItemsBase.TryGetValue(ID, out DbItem))
            {

                if (DbItem.StackSize > 0)
                {
                    byte _bound = 0;
                    if (bound)
                        _bound = 1;
                    foreach (var item in ClientItems.Values)
                    {

                        if (item.ITEM_ID == ID && item.Bound == _bound)
                        {
                            if (item.StackSize + amount <= DbItem.StackSize)
                            {
                                item.Mode = Flags.ItemMode.Update;
                                item.StackSize += amount;
                                if (bound)
                                    item.Bound = 1;
                                //if (DbItem.Time != 0)
                                //{
                                //    item.Activate = 1;
                                //    item.EndDate = DateTime.Now.AddMinutes(DbItem.Time);
                                //}
                                item.Send(Owner, stream);

                                return true;
                            }
                        }
                    }

                    if (amount > DbItem.StackSize)
                    {
                        if (HaveSpace((byte)((amount / DbItem.StackSize) + (byte)(Owner.OnInterServer ? 1 : 0))))
                        {
                            while (amount >= DbItem.StackSize)
                            {
                                Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                                ItemDat.UID = Database.Server.ITEM_Counter.Next;
                                ItemDat.ITEM_ID = ID;
                                ItemDat.Durability = ItemDat.MaximDurability = DbItem.Durability;
                                ItemDat.Plus = Plus;
                                ItemDat.StackSize += DbItem.StackSize;
                                ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                                //if (DbItem.Time != 0)
                                //{
                                //    ItemDat.Activate = 1;
                                //    ItemDat.EndDate = DateTime.Now.AddMinutes(DbItem.Time);
                                //}
                                if (bound)
                                    ItemDat.Bound = 1;
                                try
                                {
                                    Update(ItemDat, AddMode.ADD, stream);
                                }
                                catch (Exception e)
                                {
                                    Console.SaveException(e);
                                }
                                amount -= DbItem.StackSize;

                            }
                            if (amount > 0 && amount < DbItem.StackSize)
                            {
                                Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                                ItemDat.UID = Database.Server.ITEM_Counter.Next;
                                ItemDat.ITEM_ID = ID;
                                ItemDat.Durability = ItemDat.MaximDurability = DbItem.Durability;
                                ItemDat.Plus = Plus;
                                ItemDat.StackSize += amount;
                                //if (DbItem.Time != 0)
                                //{
                                //    ItemDat.Activate = 1;
                                //    ItemDat.EndDate = DateTime.Now.AddMinutes(DbItem.Time);
                                //}
                                ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                                if (bound)
                                    ItemDat.Bound = 1;
                                try
                                {
                                    Update(ItemDat, AddMode.ADD, stream);
                                }
                                catch (Exception e)
                                {
                                    Console.SaveException(e);
                                }
                            }
                            return true;
                        }
                        else
                        {
                            while (amount >= DbItem.StackSize)
                            {
                                AddReturnedItem(stream, ID, 1, Plus, 0, 0, Flags.Gem.NoSocket, Flags.Gem.NoSocket, bound, Flags.ItemEffect.None, DbItem.StackSize);
                                amount -= DbItem.StackSize;
                            }
                            if (amount > 0 && amount < DbItem.StackSize)
                            {
                                AddReturnedItem(stream, ID, 1, Plus, 0, 0, Flags.Gem.NoSocket, Flags.Gem.NoSocket, bound, Flags.ItemEffect.None, amount);
                            }
                            return true;
                        }
                    }
                    else
                    {
                        if (HaveSpace(1))
                        {
                            Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                            ItemDat.UID = Database.Server.ITEM_Counter.Next;
                            ItemDat.ITEM_ID = ID;
                            ItemDat.Durability = ItemDat.MaximDurability = DbItem.Durability;
                            ItemDat.Plus = Plus;
                            ItemDat.StackSize = amount;
                            ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                            //if (DbItem.Time != 0)
                            //{
                            //    ItemDat.Activate = 1;
                            //    ItemDat.EndDate = DateTime.Now.AddMinutes(DbItem.Time);
                            //}
                            if (bound)
                                ItemDat.Bound = 1;
                            try
                            {
                                Update(ItemDat, AddMode.ADD, stream);
                            }
                            catch (Exception e)
                            {
                                Console.SaveException(e);
                            }
                            return true;
                        }
                    }
                }
                for (int count = 0; count < amount; count++)
                    Add(ID, Plus, DbItem, stream, bound);
                return true;
            }
            return false;
        }
        public bool ContainItemWithStack(uint UID, ushort Count)
        {
            Game.MsgServer.MsgGameItem ItemDat;
            if (ClientItems.TryGetValue(UID, out ItemDat))
            {
                return ItemDat.StackSize >= Count || Count == 1 && ItemDat.StackSize == 0;
            }
            return false;
        }

        public bool RemoveStackItem(uint UID, ushort Count, ServerSockets.Packet stream)
        {
            Game.MsgServer.MsgGameItem ItemDat;
            if (ClientItems.TryGetValue(UID, out ItemDat))
            {
                if (ItemDat.StackSize > Count)
                {
                    ItemDat.StackSize -= Count;
                    ItemDat.Mode = Flags.ItemMode.Update;
                    ItemDat.Send(Owner, stream);
                }
                else
                {
                    ItemDat.StackSize = 1;
                    Update(ItemDat, AddMode.REMOVE, stream);
                    return true;
                }
            }
            else
            {

                foreach (var item in ClientItems.Values)
                {
                    if (0 == Count)
                        break;
                    if (item.ITEM_ID == UID)
                    {
                        if (item.StackSize > Count)
                        {
                            item.StackSize -= Count;
                            item.Mode = Flags.ItemMode.Update;
                            item.Send(Owner, stream);
                            Count = 0;
                        }
                        else
                        {
                            Count -= item.StackSize;
                            item.StackSize = 1;
                            Update(item, AddMode.REMOVE, stream);
                        }
                    }
                }
            }
            return false;
        }
        public bool AddSoul(uint ID, uint SoulID, byte plus, byte gem1, byte gem2, byte hp, byte daamge, byte times, ServerSockets.Packet stream, bool bound)
        {
            Database.ItemType.DBItem Soul = null;
            Database.ItemType.DBItem ITEMDB = null;
            if (SoulID != 0)
            {
                if (!Database.Server.ItemsBase.TryGetValue((uint)SoulID, out Soul))
                    return false;

            }
            if (!Database.Server.ItemsBase.TryGetValue((uint)ID, out ITEMDB))
                return false;
            if (HaveSpace(1))
            {
                Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                ItemDat.UID = Database.Server.ITEM_Counter.Next;
                ItemDat.ITEM_ID = ID;
                ItemDat.Durability = ItemDat.MaximDurability = ITEMDB.Durability;
                ItemDat.Plus = plus;
                ItemDat.SocketOne = (Flags.Gem)gem1;
                ItemDat.SocketTwo = (Flags.Gem)gem2;
                ItemDat.Bless = daamge;
                if (hp > 0)
                {
                    ItemDat.Enchant = (byte)(new System.Random().Next(200, 255));
                }
                ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                if (ITEMDB.Time != 0)
                {
                    ItemDat.Activate = 1;
                    ItemDat.EndDate = DateTime.Now.AddMinutes(ITEMDB.Time);
                }
                MsgItemExtra.Purification purify = new MsgItemExtra.Purification();
                purify.AddedOn = DateTime.Now;
                purify.ItemUID = ItemDat.UID;
                purify.PurificationLevel = 6;
                purify.PurificationItemID = SoulID;
                ItemDat.Purification.Typ = MsgItemExtra.Typing.PurificationEffect;
                ItemDat.Purification = purify;
                try
                {
                    Update(ItemDat, AddMode.ADD, stream);
                }
                catch (Exception e)
                {
                    Console.SaveException(e);
                }
                return true;
            }
            return false;

        }
        public bool Add(uint ID, byte Plus, Database.ItemType.DBItem ITEMDB, ServerSockets.Packet stream, bool bound = false)
        {
            if (ITEMDB.StackSize > 0)
            {
                byte _bound = 0;
                if (bound)
                    _bound = 1;
                foreach (var item in ClientItems.Values)
                {

                    if (item.ITEM_ID == ID && item.Bound == _bound)
                    {
                        if (item.StackSize < ITEMDB.StackSize)
                        {
                            item.Mode = Flags.ItemMode.Update;
                            item.StackSize++;
                            if (ITEMDB.Time != 0)
                            {
                                item.Activate = 1;
                                item.EndDate = DateTime.Now.AddMinutes(ITEMDB.Time);
                            }
                            if (bound)
                                item.Bound = 1;
                            item.Send(Owner, stream);

                            return true;
                        }
                    }
                }
            }
            if (HaveSpace(1))
            {
                Game.MsgServer.MsgGameItem ItemDat = new Game.MsgServer.MsgGameItem();
                ItemDat.UID = Database.Server.ITEM_Counter.Next;
                ItemDat.ITEM_ID = ID;
                ItemDat.Durability = ItemDat.MaximDurability = ITEMDB.Durability;
                ItemDat.Plus = Plus;
                ItemDat.Color = (Role.Flags.Color)Program.GetRandom.Next(3, 9);
                if (ITEMDB.Time != 0)
                {
                    ItemDat.Activate = 1;
                    ItemDat.EndDate = DateTime.Now.AddMinutes(ITEMDB.Time);
                }
                if (bound)
                    ItemDat.Bound = 1;
                try
                {
                    Update(ItemDat, AddMode.ADD, stream);
                }
                catch (Exception e)
                {
                    Console.SaveException(e);
                }
                return true;
            }
            return false;

        }
        public bool Add(Game.MsgServer.MsgGameItem ItemDat, Database.ItemType.DBItem ITEMDB, ServerSockets.Packet stream)
        {
            if (ITEMDB.StackSize > 0)
            {
                foreach (var item in ClientItems.Values)
                {
                    if (item.ITEM_ID == ItemDat.ITEM_ID)
                    {
                        if (item.StackSize < ITEMDB.StackSize)
                        {
                            item.Mode = Flags.ItemMode.Update;
                            item.StackSize++;
                            if (ITEMDB.Time != 0 && ITEMDB.StackSize == 1)
                            {
                                ItemDat.Activate = 1;
                                ItemDat.EndDate = DateTime.Now.AddMinutes(ITEMDB.Time);
                            }
                            item.Send(Owner, stream);
                            return true;
                        }
                    }
                }
            }
            if (HaveSpace(1))
            {
                if (ITEMDB.Time != 0 && ITEMDB.StackSize == 1)
                {
                    ItemDat.Activate = 1;
                    ItemDat.EndDate = DateTime.Now.AddMinutes(ITEMDB.Time);
                }
                Update(ItemDat, AddMode.ADD, stream);
                return true;
            }
            return false;

        }
        public bool AddItemWitchStack(Game.MsgServer.MsgGameItem ItemDat, byte amount, ServerSockets.Packet stream)
        {
            Database.ItemType.DBItem DbItem;
            if (Database.Server.ItemsBase.TryGetValue(ItemDat.ITEM_ID, out DbItem))
            {
                for (int count = 0; count < amount; count++)
                    Add(ItemDat, DbItem, stream);
                return true;
            }
            return false;
        }
        public unsafe bool Update(Game.MsgServer.MsgGameItem ItemDat, AddMode mode, ServerSockets.Packet stream, bool Removefull = false)
        {
            if(ItemDat.ITEM_ID == 1088000)
            {

            }
            if (HaveSpace(1) || mode == AddMode.REMOVE)
            {
                string logs = "[Item]" + Owner.Player.Name + " [" + mode + "] [" + ItemDat.UID + "]" + ItemDat.ITEM_ID + " plus [" + ItemDat.Plus + "]";
                Database.ServerDatabase.LoginQueue.Enqueue(logs);
                switch (mode)
                {
                    case AddMode.ADD:
                        {
                            CheakUp(ItemDat);
                            if (ItemDat.StackSize == 0)
                                ItemDat.StackSize = 1;
                            ItemDat.Position = 0;
                            ItemDat.Mode = Flags.ItemMode.AddItem;
                            ItemDat.Send(Owner, stream);
                            if (Owner.IsConnectedInterServer())
                            {
                                ItemDat.Send(Owner.PipeClient, stream);
                            }
                            break;
                        }
                    case AddMode.MOVE:
                        {
                            CheakUp(ItemDat);
                            ItemDat.Position = 0;
                            ItemDat.Mode = Flags.ItemMode.AddItem;
                            ItemDat.Send(Owner, stream);
                            break;
                        }
                    case AddMode.REMOVE:
                        {
                            if (ItemDat.StackSize > 1 && ItemDat.Position < 40 && !Removefull)
                            {
                                ItemDat.StackSize -= 1;
                                ItemDat.Mode = Flags.ItemMode.Update;
                                ItemDat.Send(Owner, stream);
                                break;
                            }
                            Game.MsgServer.MsgGameItem item;
                            if (ClientItems.TryRemove(ItemDat.UID, out item))
                            {
                                Owner.Send(stream.ItemUsageCreate(MsgItemUsuagePacket.ItemUsuageID.RemoveInventory, item.UID, 0, 0, 0, 0, 0));
                            }
                            break;
                        }
                }
                if (ItemDat.ITEM_ID == 750000)
                {
                    Owner.DemonExterminator.ItemUID = ItemDat.UID;
                    if (mode == AddMode.REMOVE)
                        Owner.DemonExterminator.ItemUID = 0;
                }
                /* try
                 {
                     StackTrace stackTrace = new StackTrace();           // get call stack
                     StackFrame[] stackFrames = stackTrace.GetFrames();  // get method calls (frames)

                     string data = "[CallStack]" + Owner.Player.Name + " " + ItemDat.ITEM_ID + " "+ItemDat.UID+" \n";
                     // write call stack method names
                     foreach (StackFrame stackFrame in stackFrames)
                     {
                         data += stackFrame.GetMethod().Name + " " + stackFrame.GetMethod().DeclaringType.Name + " ";   
                  
                     }

                     data += Environment.StackTrace;

                     Database.ServerDatabase.LoginQueue.Enqueue(data);
                  
                 }
                 catch (Exception e)
                 {
                     MyConsole.SaveException(e);
                 }*/

                return true;

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
                    ItemDat.UID = Database.Server.ITEM_Counter.Next;
                while
                  (ClientItems.TryAdd(ItemDat.UID, ItemDat) == false);
            }
        }

        public bool CheckMeteors(byte count, bool Removethat, ServerSockets.Packet stream)
        {

            if (Contain(1088001, count))
            {
                if (Removethat)
                    Remove(1088001, count, stream);
                return true;
            }
            else
            {
                byte Counter = 0;
                var RemoveThis = new Dictionary<uint, Game.MsgServer.MsgGameItem>();
                var MyMetscrolls = GetMyMetscrolls();
                var MyMeteors = GetMyMeteors();
                foreach (var GameItem in MyMetscrolls.Values)
                {
                    Counter += 10;
                    RemoveThis.Add(GameItem.UID, GameItem);
                    if (Counter >= count)
                        break;
                }
                if (Counter >= count)
                {
                    byte needSpace = (byte)(Counter - count);
                    if (HaveSpace(needSpace))
                    {
                        if (Removethat)
                        {
                            Add(stream, 1088001, needSpace);
                        }
                    }
                    else
                    {
                        Counter -= 10;
                        RemoveThis.Remove(RemoveThis.Values.First().UID);
                        byte needmetsss = (byte)(count - Counter);
                        if (needmetsss <= MyMeteors.Count)
                        {
                            foreach (var GameItem in MyMeteors.Values)
                            {
                                Counter += 1;
                                RemoveThis.Add(GameItem.UID, GameItem);
                                if (Counter >= count)
                                    break;
                            }
                            if (Removethat)
                            {
                                foreach (var GameItem in RemoveThis.Values)
                                    Update(GameItem, AddMode.REMOVE, stream);
                            }
                        }
                        else
                            return false;
                    }
                    if (Removethat)
                    {
                        foreach (var GameItem in RemoveThis.Values)
                            Update(GameItem, AddMode.REMOVE, stream);
                    }
                    return true;
                }
                foreach (var GameItem in MyMeteors.Values)
                {
                    Counter += 1;
                    RemoveThis.Add(GameItem.UID, GameItem);
                    if (Counter >= count)
                        break;
                }
                if (Counter >= count)
                {
                    if (Removethat)
                    {
                        foreach (var GameItem in RemoveThis.Values)
                            Update(GameItem, AddMode.REMOVE, stream);
                    }
                    return true;
                }
            }

            return false;
        }
        private Dictionary<uint, Game.MsgServer.MsgGameItem> GetMyMetscrolls()
        {
            var array = new Dictionary<uint, Game.MsgServer.MsgGameItem>();
            foreach (var GameItem in ClientItems.Values)
            {
                if (GameItem.ITEM_ID == 720027)
                {
                    if (!array.ContainsKey(GameItem.UID))
                        array.Add(GameItem.UID, GameItem);
                }
            }
            return array;
        }
        private Dictionary<uint, Game.MsgServer.MsgGameItem> GetMyMeteors()
        {
            var array = new Dictionary<uint, Game.MsgServer.MsgGameItem>();
            foreach (var GameItem in ClientItems.Values)
            {
                if (GameItem.ITEM_ID == Database.ItemType.Meteor || GameItem.ITEM_ID == Database.ItemType.MeteorTear)
                {
                    if (!array.ContainsKey(GameItem.UID))
                        array.Add(GameItem.UID, GameItem);
                }
            }
            return array;
        }


        public void ShowALL(ServerSockets.Packet stream)
        {
            foreach (var msg_item in ClientItems.Values)
            {
                msg_item.Mode = Flags.ItemMode.AddItem;
                msg_item.Send(Owner, stream);
            }
        }
        public void Clear(ServerSockets.Packet stream)
        {
            var dictionary = ClientItems.Values.ToArray();
            foreach (var msg_item in dictionary)
                Update(msg_item, AddMode.REMOVE, stream, true);
        }
    }
}
