using COServer.Game.MsgServer.AttackHandler.Algoritms;
using COServer.Role;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COServer.Game.MsgMonster
{
    public class MobRateWatcher
    {
        private int tick;
        private int count;
        public static implicit operator bool(MobRateWatcher q)
        {
            bool result = false;
            q.count++;
            if (q.count == q.tick)
            {
                q.count = 0;
                result = true;
            }
            return result;
        }
        public MobRateWatcher(int Tick)
        {
            tick = Tick;
            count = 0;
        }
    }

    public struct SpecialItemWatcher
    {
        public uint ID;
        public MobRateWatcher Rate;
        public SpecialItemWatcher(uint ID, int Tick)
        {
            this.ID = ID;
            Rate = new MobRateWatcher(Tick);
        }
    }

    public class MobItemGenerator
    {
        public static ushort[] NecklaceType = new ushort[] { 120, 121 };
        public static ushort[] RingType = new ushort[] { 150, 151 };
        public static ushort[] ArmetType = new ushort[] { 111, 112, 113, 114, 117, 118 };
        public static ushort[] ArmorType = new ushort[] { 130, 131, 132, 133, 134 };
        public static ushort[] OneHanderType = new ushort[] { 410, 420, 421, 430, 440, 450, 460, 480, 481, 490, 500, 601 };
        public static ushort[] TwoHanderType = new ushort[] { 510, 530, 560, 561, 580, 900, };
        public static uint[] SeaPotions = new uint[] { 3004230, 3004231, 3004232, 3004233, 3004234, 3004235, 3004236, 3004237, 3004238 };
        private MonsterFamily Family;

        private MobRateWatcher Refined;
        private MobRateWatcher Unique;
        private MobRateWatcher Elite;
        private MobRateWatcher Super;
        private MobRateWatcher PlusOne;
        private MobRateWatcher PlusTwo;

        private MobRateWatcher DropHp;
        private MobRateWatcher DropMp;
        //private MobRateWatcher Chi100;
        //private MobRateWatcher Study20;
        //private MobRateWatcher Chi300;
        //private MobRateWatcher Bomb;
        //private MobRateWatcher LuckyAmulet;
        //private MobRateWatcher MoonBox;

        //720665 CuteCPPack
        private MobRateWatcher DropSpecialPotions;
        //private MobRateWatcher CuteCPPack;
        private MobRateWatcher DragonBalls;
        private MobRateWatcher Meteor;


        public MobItemGenerator(MonsterFamily family)
        {
            Family = family;
            Refined = new MobRateWatcher(10);//500 / Family.Level);//1000 / Family.Level);
            Unique = new MobRateWatcher(20);//2000 / Family.Level);//4000 / Family.Level);
            Elite = new MobRateWatcher(25);//4000 / Family.Level);//8000 / Family.Level);
            Super = new MobRateWatcher(30);//5000 / Family.Level);//10000 / Family.Level);

            DropSpecialPotions = new MobRateWatcher(50);
            PlusTwo = new MobRateWatcher(3000000);//4000 / Family.Level);//6000 / Family.Level);

            DropHp = new MobRateWatcher(50);
            DropMp = new MobRateWatcher(50);


            // Drop rates of DB - Mets - +1 //
            PlusOne = new MobRateWatcher(250);// Da <<<< rate el drop
           // DragonBalls = new MobRateWatcher(4500);// Da <<<< rate el drop
           // Meteor = new MobRateWatcher(100);//Da <<<< rate el drop
            // end of drop rate //


        }

        public uint GeneratePotionExtra(bool Special = false)
        {
            if (Special)
            {
                return SeaPotions[Program.GetRandom.Next(0, SeaPotions.Length)];
            }

            if (DropSpecialPotions)
            {
                return SeaPotions[Program.GetRandom.Next(0, SeaPotions.Length)];
            }
            return 0;
        }
        public List<uint> GenerateStones()
        {
            
            List<uint> items = new List<uint>();
            items.Add(730004);


            return items;
        }
        public List<uint> GenerateSoulsItems(ushort level)
        {
            if (level == 345)
                level = (ushort)Role.Core.Random.Next(3, 5);
            if (level == 0)
                level = (ushort)Role.Core.Random.Next(1, 7);
            List<uint> items = new List<uint>();
            byte count = 1;
            if (Database.ItemType.PurificationItems.ContainsKey(level))
            {
                var array = Database.ItemType.PurificationItems[level].Values.ToArray();
                for (int x = 0; x < (int)(count == 0 ? 1 : count); x++)
                {
                    int position = Program.GetRandom.Next(0, array.Length);
                    items.Add(array[position].ID);
                }
            }

            //genereate accessory
            //var Accessorys = Database.ItemType.Accessorys.Values.ToArray();
            //var rand = (ushort)(Program.GetRandom.Next() % 1000);
            //count = (byte)(rand % 3);
            //for (int x = 0; x < count; x++)
            //{
            //    int position = Program.GetRandom.Next(0, Accessorys.Length);
            //    items.Add(Accessorys[position].ID);
            //}

            //--------------------------

            if (level <= 3)
                items.Add(723341);//20 study points
            else if (level > 3)
                items.Add(723342);//500 study points

            return items;
        }
        public List<uint> GenerateSoulsItemsp6(ushort level)//Desha
        {
            if (level == 0)
                level = 6;
            List<uint> items = new List<uint>();
            byte count = 1;
            if (Database.ItemType.PurificationItems.ContainsKey(level))
            {
                var array = Database.ItemType.PurificationItems[level].Values.ToArray();
                for (int x = 0; x < (int)(count == 0 ? 1 : count); x++)
                {
                    int position = Program.GetRandom.Next(0, array.Length);
                    items.Add(array[position].ID);
                }
            }
            //if (level <= 3)
            //    items.Add(723341);//20 study points
            //else 
            if (level > 3)
                items.Add(723342);//500 study points

            return items;
        }
        public List<uint> GenerateRefineryItems(ushort level)
        {
            if (level == 0)
                level = (ushort)Role.Core.Random.Next(2, 4);
            if (Role.MyMath.Success(0.001))
                level = 5;
            List<uint> items = new List<uint>();
            byte count = 1;
            if (Database.ItemType.Refinary.ContainsKey(level))
            {
                var array = Database.ItemType.Refinary[level].Values.ToArray();
                for (int x = 0; x < (int)(count == 0 ? 1 : count); x++)
                {
                    int position = Program.GetRandom.Next(0, array.Length);
                    items.Add(array[position].ItemID);
                }
            }

            //genereate accessory
            var Accessorys = Database.ItemType.Accessorys.Values.ToArray();
            var rand = (ushort)(Program.GetRandom.Next() % 1000);
            count = (byte)(rand % 3);
            for (int x = 0; x < count; x++)
            {
                int position = Program.GetRandom.Next(0, Accessorys.Length);
                items.Add(Accessorys[position].ID);
            }

            //--------------------------

            if (level <= 3)
                items.Add(723341);//20 study points
            else if (level > 3)
                items.Add(723342);//500 study points

            return items;
        }
        public List<uint> GenerateBossFamily()
        {
            List<uint> Items = new List<uint>();
            byte rand = (byte)Program.GetRandom.Next(1, 7);
            for (int x = 0; x < 4; x++)
            {
                byte dwItemQuality = GenerateQuality();
                uint dwItemSort = 0;
                uint dwItemLev = 0;
                switch (rand)
                {
                    case 1:
                        {
                            dwItemSort = NecklaceType[Program.GetRandom.Next(0, NecklaceType.Length)];
                            dwItemLev = Family.DropNecklace;
                            break;
                        }
                    case 2:
                        {
                            dwItemSort = RingType[Program.GetRandom.Next(0, RingType.Length)];
                            dwItemLev = Family.DropRing;
                            break;
                        }
                    case 3:
                        {
                            dwItemSort = ArmorType[Program.GetRandom.Next(0, ArmorType.Length)];
                            dwItemLev = Family.DropArmor;
                            break;
                        }
                    case 4:
                        {
                            dwItemSort = TwoHanderType[Program.GetRandom.Next(0, TwoHanderType.Length)];
                            dwItemLev = ((dwItemSort == 900) ? Family.DropShield : Family.DropWeapon);
                            break;
                        }
                    default:
                        {
                            dwItemSort = OneHanderType[Program.GetRandom.Next(0, OneHanderType.Length)];
                            dwItemLev = Family.DropWeapon;
                            break;
                        }
                }
                dwItemLev = AlterItemLevel(dwItemLev, dwItemSort);
                uint idItemType = (dwItemSort * 1000) + (dwItemLev * 10) + dwItemQuality;
                if (Database.Server.ItemsBase.ContainsKey(idItemType))
                    Items.Add(idItemType);
            }
            return Items;
        }
        public uint GenerateItemId(uint map, out byte dwItemQuality, out bool Special, out Database.ItemType.DBItem DbItem)
        {
            Special = false;

            foreach (SpecialItemWatcher sp in Family.DropSpecials)
            {
                if (sp.Rate)
                {
                    Special = true;
                    dwItemQuality = (byte)(sp.ID % 10);
                    if (Database.Server.ItemsBase.TryGetValue(sp.ID, out DbItem))
                        return sp.ID;
                }
            }
            if (DropHp)
            {
                dwItemQuality = 0;
                Special = true;
                if (Database.Server.ItemsBase.TryGetValue(Family.DropHPItem, out DbItem))
                    return Family.DropHPItem;
            }
            if (DropMp)
            {
                dwItemQuality = 0;
                Special = true; if (Database.Server.ItemsBase.TryGetValue(Family.DropMPItem, out DbItem))
                    return Family.DropMPItem;
            }
            
            //if (DragonBalls)
            //{
            //    dwItemQuality = 0;
            //    Special = true;
            //    if (Database.Server.ItemsBase.TryGetValue(1088000, out DbItem))
            //        return 1088000;
            //}
            //if (Meteor)
            //{
            //    dwItemQuality = 0;
            //    Special = true;
            //    if (Database.Server.ItemsBase.TryGetValue(1088001, out DbItem))
            //        return 1088001;
            //}
            //if (Chi100)
            //{
            //    dwItemQuality = 0;
            //    Special = true;
            //    if (Database.Server.ItemsBase.TryGetValue(729476, out DbItem))
            //        return 729476;
            //}
            //if (Study20)
            //{
            //    dwItemQuality = 0;
            //    Special = true;
            //    if (Database.Server.ItemsBase.TryGetValue(723341, out DbItem))
            //        return 723341;
            //}
            //if (MoonBox)
            //{
            //    dwItemQuality = 0;
            //    Special = true;
            //    if (Database.Server.ItemsBase.TryGetValue(Database.ItemType.MoonBox, out DbItem))
            //        return Database.ItemType.MoonBox;
            //}
            //if (map == 1001 || Role.GameMap.IsFrozengrotoMaps(map))
            //{
            //    if (Chi300)
            //    {
            //        dwItemQuality = 0;
            //        Special = true;
            //        if (Database.Server.ItemsBase.TryGetValue(729478, out DbItem))
            //            return 729478;
            //    }
            //    if (Bomb)
            //    {
            //        dwItemQuality = 0;
            //        Special = true;
            //        if (Database.Server.ItemsBase.TryGetValue(721261, out DbItem))
            //            return 721261;
            //    }
            //    if (LuckyAmulet)
            //    {
            //        dwItemQuality = 0;
            //        Special = true;
            //        if (Database.Server.ItemsBase.TryGetValue(723087, out DbItem))
            //            return 723087;
            //    }

            //}
            dwItemQuality = GenerateQuality();
            DbItem = null;
            return 0;
        }
        public byte GeneratePurity()
        {
            if (PlusOne)
                return 1;
            if (PlusTwo)
                return 2;
            return 0;
        }
        public byte GenerateBless()
        {
            if (Program.GetRandom.Next(0, 1000) < 250) // 25%
            {
                int selector = Program.GetRandom.Next(0, 100);
                if (selector < 1)
                    return 5;
                else if (selector < 6)
                    return 3;
            }
            return 0;
        }
        public byte GenerateSocketCount(uint ItemID)
        {
            if (ItemID >= 410000 && ItemID <= 601999)
            {
                int nRate = Program.GetRandom.Next(0, 1000) % 100;
                if (nRate < 5) // 5%
                    return 2;
                else if (nRate < 20) // 15%
                    return 1;
            }
            return 0;
        }
        public byte GenerateQuality()
        {
            if (Refined)
                return 6;
            else if (Unique)
                return 7;
            else if (Elite)
                return 8;
            return 3;
        }
        public uint GenerateGold(uint Map, out uint ItemID, bool normal = false, bool twin = false)
        {
            uint amount = 0;
            if (twin)
            {
                amount = (uint)Program.GetRandom.Next(0, (int)(((100 * Family.Level / 2) / 2) + 1));
            }
            else
            {
                if (normal)
                    amount = (uint)Program.GetRandom.Next(Family.DropMoney, Family.DropMoney * 10);
                else
                {
                    amount = (uint)Program.GetRandom.Next(0, (int)(((1000 * Family.Level / 2) / 2) + 1));

                    amount /= 2;
                }
            }
            if(Map == 1002)
            {
                amount = 50;
            }
            if (Map == 1000)
            {
                amount = 500;
            }
            if (Map == 1015)
            {
                amount = 850;
            }
            if (Map == 1020)
            {
                amount = 350;
            }
            if (Map == 1011)
            {
                amount = 200;
            }
            ItemID = Database.ItemType.MoneyItemID((uint)amount);

            return amount;
        }
        public uint AlterItemLevel(uint dwItemLev, uint dwItemSort)
        {
            int nRand = Extensions.BaseFunc.RandGet(100, true);

            if (nRand < 50) // 50% down one level
            {
                uint dwLev = dwItemLev;
                dwItemLev = (uint)(Extensions.BaseFunc.RandGet((int)(dwLev / 2 + dwLev / 3), false));

                if (dwItemLev > 1)
                    dwItemLev--;
            }
            else if (nRand > 80) // 20% up one level
            {
                if ((dwItemSort >= 110 && dwItemSort <= 114) ||
                    (dwItemSort >= 130 && dwItemSort <= 134) ||
                    (dwItemSort >= 900 && dwItemSort <= 999))
                {
                    dwItemLev = Math.Min(dwItemLev + 1, 9);
                }
                else
                {
                    dwItemLev = Math.Min(dwItemLev + 1, 23);
                }
            }

            return dwItemLev;
        }
    }
}
