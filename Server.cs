using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using COServer.Game.MsgServer;
using COServer.Game.MsgFloorItem;
using System.IO;

namespace COServer.Database
{
    public class Server
    {
        public static Extensions.Time32 ResetStamp = Extensions.Time32.Now.AddMilliseconds(KernelThread.ResetDayStamp);

        public static ushort[] PriceUpdatePorf = new ushort[] { 600, 600, 600, 600, 600, 600, 1200, 1800, 3000, 3600, 6000, 7200, 7200, 7200, 7200, 8318, 12170, 17735, 25639, 31537 };
        public static Dictionary<DBLevExp.Sort, Dictionary<byte, DBLevExp>> LevelInfo = new Dictionary<DBLevExp.Sort, Dictionary<byte, DBLevExp>>();

        public static ConcurrentDictionary<uint, TheCrimeTable> TheCrimePoll = new ConcurrentDictionary<uint, TheCrimeTable>();
        //public static Dictionary<ushort, ushort> WeaponSpells = new Dictionary<ushort, ushort>();
        public static Dictionary<ushort, List<ushort>> WeaponSpells = new Dictionary<ushort, List<ushort>>();
        public static MagicType Magic = new MagicType();
      //  public static LotteryEffect Lottery = new LotteryEffect();
        public static LotteryTable Lottery = new LotteryTable();
        public static SubProfessionInfo SubClassInfo = new SubProfessionInfo();
        public static Dictionary<uint, Game.MsgMonster.MonsterFamily> MonsterFamilies = new Dictionary<uint, Game.MsgMonster.MonsterFamily>();
        public static Extensions.Counter ITEM_Counter = new Extensions.Counter(1);
        public static Rifinery RifineryItems;
        public static RefinaryBoxes DBRerinaryBoxes;
        public static ItemType ItemsBase;
      //  public static Dictionary<uint, Role.GameMap> ServerMaps;
        public static Dictionary<uint, Role.GameMap> ServerMaps;
        public static Dictionary<ushort, Role.GameMap> Maps = new Dictionary<ushort, Role.GameMap>(1000);

        public static ConcurrentDictionary<uint, Client.GameClient> GamePoll;
        public static List<int> NameUsed;
        public static RebornInfomations RebornInfo;

        public static ArenaTable Arena = new ArenaTable();
        public static TeamArenaTable TeamArena = new TeamArenaTable();

        public static Extensions.Counter ClientCounter = new Extensions.Counter(1000000);
        public static ConfiscatorTable QueueContainer = new ConfiscatorTable();

        public static bool FullLoading = false;
        public static Dictionary<uint, string> MapName = new Dictionary<uint, string>() { { 1015, "BirdIsland" }, { 1011, "PhoenixCastle" }, { 1000, "DesertCity" }, { 1020, "ApeMountain" }, { 1001, "MysticCastle" } };
        public static uint ResetServerDay = 0;
        public static bool ResetedAlready = false;
        public static unsafe void Reset(Extensions.Time32 Clock)
        {
            if (Clock > ResetStamp)
            {
                if (DateTime.Now.Hour % 12 != 0)
                    ResetedAlready = false;
                if (!ResetedAlready && DateTime.Now.Hour % 12 == 0)
                {
                    try
                    {
                        if (DateTime.Now.Day == 1)
                        {
                            Program.ConsoleCMD("resetnobility");
                        }
                        Console.WriteLine("Reseting Arena.");
                        ResetedAlready = true;

                        Arena.ResetArena();
                        TeamArena.ResetArena();

                        foreach (var flowerclient in Role.Instance.Flowers.ClientPoll.Values)
                        {
                            foreach (var flower in flowerclient)
                                flower.Amount2day = 0;
                        }
                        using (var rec = new ServerSockets.RecycledPacket())
                        {
                            var stream = rec.GetStream();

                            foreach (var client in GamePoll.Values)
                            {
                                client.Player.youDidBruh = false;
                                client.Player.TodayChampionPoints = 0;
                                client.Player.UseChiToken = 0;
                                client.Player.LetterQuest = 0;
                                client.Player.QuestGUI.RemoveQuest(6126);
                                client.Player.SpookeFinish = 0;
                                client.Player.SpookeLvl = 0;
                                client.Player.OpenHousePack = 0;
                                if (client.Player.DailyMonth == 0)
                                    client.Player.DailyMonth = (byte)DateTime.Now.Month;
                                if (client.Player.DailyMonth != DateTime.Now.Month)
                                {
                                    client.Player.DailySignUpRewards = 0;
                                    client.Player.DailySignUpDays = 0;
                                    client.Player.DailyMonth = (byte)DateTime.Now.Month;
                                }
                                client.Player.ArenaKills = client.Player.ArenaDeads = 0;
                                client.Player.HitShoot = client.Player.MisShoot = 0;
                                client.Player.DbTry = false;
                                client.Player.LotteryEntries = 0;
                                client.Player.Day = DateTime.Now.DayOfYear;
                                client.Player.BDExp = 0;
                                client.Player.TCCaptainTimes = 0;
                                client.Player.ExpBallUsed = 0;
                                client.Player.MysteryFruit = 0;
                                client.DemonExterminator.FinishToday = 0;
                                if (client.Player.MyChi != null)
                                {
                                    client.Player.MyChi.ChiPoints = client.Player.MyChi.ChiPoints + 300;
                                    Game.MsgServer.MsgChiInfo.MsgHandleChi.SendInfo(client, Game.MsgServer.MsgChiInfo.Action.Upgrade);
                                }
                                if (client.Player.VipLevel == 6)
                                {
                                    client.Player.Flowers.FreeFlowers = 30;
                                }
                                else
                                {
                                    client.Player.Flowers.FreeFlowers = 1;
                                }
                                foreach (var flower in client.Player.Flowers)
                                    flower.Amount2day = 0;
                                if (client.Player.Flowers.FreeFlowers > 0)
                                {
                                    client.Send(stream.FlowerCreate(Role.Core.IsBoy(client.Player.Body)
                                        ? Game.MsgServer.MsgFlower.FlowerAction.FlowerSender
                                        : Game.MsgServer.MsgFlower.FlowerAction.Flower
                                        , 0, 0, client.Player.Flowers.FreeFlowers));
                                }

                                if (client.Player.Level >= 90)
                                {
                                    Console.WriteLine("Reseting Enilghten.");
                                    client.Player.Enilghten = ServerDatabase.CalculateEnlighten(client.Player);
                                    client.Player.SendUpdate(stream, client.Player.Enilghten, Game.MsgServer.MsgUpdate.DataType.EnlightPoints);
                                }
                                client.Player.QuestGUI.RemoveQuest(35024);
                                client.Player.QuestGUI.RemoveQuest(35007);
                                client.Player.QuestGUI.RemoveQuest(35025);
                                client.Player.QuestGUI.RemoveQuest(35028);
                                client.Player.QuestGUI.RemoveQuest(35034);

                                //---- reset Quests
                                client.Player.QuestGUI.RemoveQuest(6390);
                                client.Player.QuestGUI.RemoveQuest(6329);
                                client.Player.QuestGUI.RemoveQuest(6245);
                                client.Player.QuestGUI.RemoveQuest(6049);
                                client.Player.QuestGUI.RemoveQuest(6366);
                                client.Player.QuestGUI.RemoveQuest(6014);
                                client.Player.QuestGUI.RemoveQuest(2375);
                                client.Player.QuestGUI.RemoveQuest(6126);
                                client.Player.SpookeFinish = 0;
                                client.Player.SpookeLvl = 0;
                                client.Player.DailyHeavenChance = client.Player.DailyMagnoliaChance
                                    = client.Player.DailyMagnoliaItemId
                                    = client.Player.DailyHeavenChance = client.Player.DailySpiritBeadCount = client.Player.DailyRareChance = 0;
                                //
                            }
                        }
                        ResetServerDay = (uint)DateTime.Now.DayOfYear;
                    }
                    catch (Exception e) { Console.WriteLine(e.ToString()); }
                }
                ResetStamp.Value = Clock.Value + KernelThread.ResetDayStamp;
            }
        }
        public static void Initialize()
        {
            ServerMaps = new Dictionary<uint, Role.GameMap>();
            GamePoll = new ConcurrentDictionary<uint, Client.GameClient>();
            NameUsed = new List<int>();

            WindowsAPI.IniFile IniFile = new WindowsAPI.IniFile(System.IO.Directory.GetCurrentDirectory() + "\\shell.ini", true);
            Program.ServerConfig.IPAddres = IniFile.ReadString("ServerInfo", "AddresIP", "");
            Program.ServerConfig.GamePort = IniFile.ReadUInt16("ServerInfo", "Game_Port", 5816);
            Program.ServerConfig.ServerName = IniFile.ReadString("ServerInfo", "ServerName", "Insomania");
            Program.ServerConfig.OfficialWebSite = IniFile.ReadString("ServerInfo", "WebSite", "");

            Program.ServerConfig.Port_BackLog = IniFile.ReadUInt16("InternetPort", "BackLog", 100);
            Program.ServerConfig.Port_ReceiveSize = IniFile.ReadUInt16("InternetPort", "ReceiveSize", 8194);
            Program.ServerConfig.Port_SendSize = IniFile.ReadUInt16("InternetPort", "SendSize", 1024);

            Program.ServerConfig.DbLocation = IniFile.ReadString("Database", "Location", "");
            Program.ServerConfig.CO2Folder = IniFile.ReadString("Database", "CO2FOLDER", "");

            Program.ServerConfig.InterServerAddress = IniFile.ReadString("InterServer", "AddresIP", "");
            Program.ServerConfig.InterServerPort = IniFile.ReadUInt16("InterServer", "Port", 9915);
            Program.ServerConfig.IsInterServer = IniFile.ReadUInt32("InterServer", "IsInterServer", 0) > 0 ? true : false;

            Program.ServerConfig.GuildWarStart = IniFile.ReadUInt16("ServerControl", "GuildWarStart", 0);
            Program.ServerConfig.GuildWarEnd = IniFile.ReadUInt16("ServerControl", "GuildWarEnd", 0);
            Program.ServerConfig.GuildWarPrize = IniFile.ReadUInt32("ServerControl", "GuildWarPrize", 0);
            Program.ServerConfig.GuildWarFlamesQuest = IniFile.ReadUInt16("ServerControl", "GuildWarFlamesQuest", 0);
            Program.ServerConfig.GuildWarInvitation = IniFile.ReadUInt16("ServerControl", "GuildWarInvitation", 0);

            Program.DeleteItems = IniFile.ReadUInt32("ServerInfo", "Reset", 0) == 24313197 ? true : false;
            RebornInfo = new RebornInfomations();
            Console.WriteLine("RebornInfo");
            
            RebornInfo.Load();
            
            ITEM_Counter.Set(IniFile.ReadUInt32("Database", "ItemUID", 0));
            uint nextitem = ITEM_Counter.Next;
            ClientCounter.Set(IniFile.ReadUInt32("Database", "ClientUID", 1000000));
            uint nextclient = ClientCounter.Next;
            ResetServerDay = IniFile.ReadUInt32("Database", "Day", 0);
            Game.MsgTournaments.MsgSchedules.PkWar.WinnerUID = IniFile.ReadUInt32("Tournaments", "PkWarWinner", 0);

            ItemsBase = new ItemType();
            RifineryItems = new Rifinery();
            DBRerinaryBoxes = new RefinaryBoxes();
            Console.Write("Loading ItemsBase ...  ");
            ItemsBase.Loading();
            Console.DoneLoading();
            Console.Write("Loading EShopFile ...  ");
            Shops.EShopFile.Load();
            Console.DoneLoading();
            Console.Write("Loading EShopV2File ...  ");
            Shops.EShopV2File.Load();
            Console.DoneLoading();
            Console.Write("Loading HonorShop ...  ");
            Shops.HonorShop.Load();
            Console.DoneLoading();
            Console.Write("Loading RacePointShop ...  ");
            Shops.RacePointShop.Load();
            Console.DoneLoading();
            Console.Write("Loading ShopFile ...  ");
            Shops.ShopFile.Load();
            Console.DoneLoading();
            Console.Write("Loading  ServerControl ...  ");
            ProjectControl.ServerControl();
            Console.DoneLoading();
            Console.Write("Loading  LotteryControl ...  ");
            ProjectControl.LotteryControl();
            Console.DoneLoading();
            Console.Write("Loading  EventPrize ...  ");
            ProjectControl.EventPrize();
            Console.DoneLoading();
            Console.Write("Loading Flags ...  ");
            ProjectControl.LoadFlags();
            Console.DoneLoading();
            Console.Write("Loading MiningTable ...  ");
            MiningTable.Load();
            Console.DoneLoading();
            Console.Write("Loading SystemBanned ...  ");
            SystemBanned.Load();
            Console.DoneLoading();
            Console.Write("Loading SystemBannedAccount ...  ");
            SystemBannedAccount.Load();
            Console.DoneLoading();
            Console.Write("Loading ShareVIP ...  ");
            Database.ShareVIP.Load();
            Console.DoneLoading();
            Console.Write("Loading ExpInfo ...  ");
            LoadExpInfo();
            Console.DoneLoading();
            Console.Write("Loading DataCore ...  ");
            DataCore.AtributeStatus.Load();
            Console.DoneLoading();
            Console.Write("Loading GameMap ...  ");
            Role.GameMap.LoadMaps();
            Console.DoneLoading();
            Console.Write("Loading NpcServer ...  ");
            NpcServer.LoadNpcs();
            Console.DoneLoading();
            Console.Write("Loading LoadServerTraps ...  ");
            NpcServer.LoadServerTraps();
            Console.DoneLoading();
            Console.Write("Loading Magic ...  ");
            Magic.Load();
            Console.DoneLoading();
            Console.Write("Loading Monsters ...  ");
            LoadMonsters();
            Console.DoneLoading();
            Console.Write("Loading Tranformation ...  ");
            Tranformation.Int();
            Console.DoneLoading();
            Console.Write("Loading QuestInfo ...  ");
            QuestInfo.Init();
            Console.DoneLoading();
            Console.Write("Loading LoadPortals ...  ");
            LoadPortals();
            Console.DoneLoading();
            Console.Write("Loading SubClassInfo ...  ");
            SubClassInfo.Load();
            Console.DoneLoading();
            Console.Write("Loading ChiTable ...  ");
            ChiTable.Load();
            Console.DoneLoading();
            Console.Write("Loading FlowersTable ...  ");
            FlowersTable.Load();
            Console.DoneLoading();
            Console.Write("Loading NobilityTable ...  ");
            NobilityTable.Load();
            Console.DoneLoading();
            Console.Write("Loading Associate ...  ");
            Role.Instance.Associate.Load();
            Console.DoneLoading();
            Console.Write("Loading NpcServer ...  ");
            NpcServer.LoadSobNpcs();
            Console.DoneLoading();
            Console.Write("Loading GuildTable ...  ");
            GuildTable.Load();
            Console.DoneLoading();
            Console.Write("Loading ClanTable ...  ");
            Database.ClanTable.Load();
            Console.DoneLoading();
            Console.Write("Loading QuizShow ...  ");
            QuizShow.Load();
            Console.DoneLoading();
            Game.MsgTournaments.MsgSchedules.ClassPkWar.Load();
            Game.MsgTournaments.MsgSchedules.CouplesPKWar.Load();
            Game.MsgTournaments.MsgSchedules.ElitePkTournament.Load();
            Game.MsgTournaments.MsgSchedules.TeamPkTournament.Load();
            Game.MsgTournaments.MsgSchedules.SkillTeamPkTournament.Load();
            TheCrimeTable.Load();
            Role.Statue.Load();
            Role.KOBoard.KOBoardRanking.Load();
            Database.Disdain.Load();
            Arena.Load();
            TeamArena.Load();
            //Rankings.Load();
            Database.TutorInfo.Load();
            InfoDemonExterminators.Create();
            Lottery.LoadLotteryItems();
            QueueContainer.Load();
            LoadMapName();
            GroupServerList.Load();
            VoteSystem.Load();
            FullLoading = true;
        }
        public static byte NameChangeCount(byte vipLevel)
        {
            byte chance = 1;
            switch (vipLevel)
            {
                case 1:
                    chance = 2;
                    break;
                case 2:
                    chance = 3;
                    break;
                case 3:
                    chance = 4;
                    break;
                case 4:
                    chance = 5;
                    break;
                case 5:
                    chance = 10;
                    break;

                case 6:
                    chance = 30;
                    break;
            }
            return chance;
        }
        public static void LoadMapName()
        {

            if (System.IO.File.Exists(Program.ServerConfig.DbLocation + "GameMapEx.ini"))
            {
                foreach (var map in ServerMaps.Values)
                {
                    WindowsAPI.IniFile ini = new WindowsAPI.IniFile("GameMapEx.ini");
                    map.Name = ini.ReadString(map.ID.ToString(), "Name", Program.ServerConfig.ServerName);
                }
            }
        }
        public static void LoadExpInfo()
        {
            if (System.IO.File.Exists(Program.ServerConfig.DbLocation + "levexp.txt"))
            {
                using (System.IO.StreamReader read = System.IO.File.OpenText(Program.ServerConfig.DbLocation + "levexp.txt"))
                {
                    while (true)
                    {
                        string GetLine = read.ReadLine();
                        if (GetLine == null) return;
                        string[] line = GetLine.Split(' ');
                        DBLevExp exp = new DBLevExp();
                        exp.Action = (DBLevExp.Sort)byte.Parse(line[0]);
                        exp.Level = byte.Parse(line[1]);
                        exp.Experience = ulong.Parse(line[2]);
                        exp.UpLevTime = int.Parse(line[3]);
                        exp.MentorUpLevTime = int.Parse(line[4]);

                        if (!LevelInfo.ContainsKey(exp.Action))
                            LevelInfo.Add(exp.Action, new Dictionary<byte, DBLevExp>());

                        LevelInfo[exp.Action].Add(exp.Level, exp);

                    }
                }
            }
            GC.Collect();
        }
        public static void LoadMonsters()
        {
            try
            {
                //var ini2 = new WindowsAPI.IniFile("monster.txt");
                WindowsAPI.IniFile ini = new WindowsAPI.IniFile("");
                foreach (string fname in System.IO.Directory.GetFiles(Program.ServerConfig.DbLocation + "\\Monsters\\"))
                {
                    ini.FileName = fname;
                    Game.MsgMonster.MonsterFamily Family = new Game.MsgMonster.MonsterFamily();
                    Family.ID = ini.ReadUInt32("cq_monstertype", "id", 0);
                    Family.Name = ini.ReadString("cq_monstertype", "name", "INVALID_MOB");

                    Family.Level = ini.ReadUInt16("cq_monstertype", "level", 0);
                    Family.MaxAttack = ini.ReadInt32("cq_monstertype", "attack_max", 0);
                    Family.MinAttack = ini.ReadInt32("cq_monstertype", "attack_min", 0);
                    if (Family.Name == "INVALID_MOB" || Family.Level == 0 || Family.ID == 0 || Family.MinAttack > Family.MaxAttack)
                    {
                        Console.WriteLine("MONSTER FILE CORRUPT: \r\n" + fname + "\r\n");
                        continue;
                    }
                    Family.Defense = ini.ReadUInt16("cq_monstertype", "defence", 0);
                    Family.Mesh = ini.ReadUInt16("cq_monstertype", "lookface", 0);
                    Family.MaxHealth = ini.ReadInt32("cq_monstertype", "life", 0);
                    //int HP = ini2.ReadInt32(Family.Name, "MaxLife", 0);
                    //if (HP > Family.MaxHealth)
                    //{
                    //    ini.WriteString("cq_monstertype", "life", HP.ToString());
                    //    Console.WriteLine("Name: " + Family.Name + " Real: " + HP + " Current: " + Family.MaxHealth);
                    //}

                    Family.ViewRange = 16;
                    Family.AttackRange = ini.ReadSByte("cq_monstertype", "attack_range", 0);
                    Family.Dodge = ini.ReadByte("cq_monstertype", "dodge", 0);
                    Family.DropBoots = ini.ReadByte("cq_monstertype", "drop_shoes", 0);
                    Family.DropNecklace = ini.ReadByte("cq_monstertype", "drop_necklace", 0);
                    Family.DropRing = ini.ReadByte("cq_monstertype", "drop_ring", 0);
                    Family.DropArmet = ini.ReadByte("cq_monstertype", "drop_armet", 0);
                    Family.DropArmor = ini.ReadByte("cq_monstertype", "drop_armor", 0);
                    Family.DropShield = ini.ReadByte("cq_monstertype", "drop_shield", 0);
                    Family.DropWeapon = ini.ReadByte("cq_monstertype", "drop_weapon", 0);
                    Family.DropMoney = ini.ReadUInt16("cq_monstertype", "drop_money", 0);
                    Family.DropHPItem = ini.ReadUInt32("cq_monstertype", "drop_hp", 0);
                    Family.DropMPItem = ini.ReadUInt32("cq_monstertype", "drop_mp", 0);
                    Family.Boss = ini.ReadByte("cq_monstertype", "Boss", 0);
                    Family.Defense2 = ini.ReadInt32("cq_monstertype", "defence2", 0);
                    if (Family.Boss != 0)
                        Family.AttackRange = 3;
                    //defence2

                    //if (Family.Boss != 0)
                    //    MyConsole.WriteLine(Family.Name);
                    //if (Family.Dodge > 50)
                    //    MyConsole.WriteLine(Family.Name);
                    Family.MoveSpeed = ini.ReadInt32("cq_monstertype", "move_speed", 0);
                    Family.AttackSpeed = ini.ReadInt32("cq_monstertype", "attack_speed", 0);
                    Family.SpellId = ini.ReadUInt32("cq_monstertype", "magic_type", 0);

                    Family.ExtraCritical = ini.ReadUInt32("cq_monstertype", "critical", 0);
                    Family.ExtraBreack = ini.ReadUInt32("cq_monstertype", "break", 0);

                    Family.extra_battlelev = ini.ReadInt32("cq_monstertype", "extra_battlelev", 0);
                    Family.extra_exp = ini.ReadInt32("cq_monstertype", "extra_exp", 0);
                    Family.extra_damage = ini.ReadInt32("cq_monstertype", "extra_damage", 0);


                    if (Family.Boss == 0 && Family.MaxAttack > 3000)
                    {
                        Family.MaxAttack = Family.MaxAttack / 2;
                        Family.MinAttack = Family.MinAttack / 2;
                    }
                    Family.DropSpecials = new Game.MsgMonster.SpecialItemWatcher[ini.ReadInt32("SpecialDrop", "Count", 0)];
                    for (int i = 0; i < Family.DropSpecials.Length; i++)
                    {
                        string[] Data = ini.ReadString("SpecialDrop", i.ToString(), "", 32).Split(',');

                        Family.DropSpecials[i] = new Game.MsgMonster.SpecialItemWatcher(uint.Parse(Data[0]), int.Parse(Data[1]));
                    }

                    Family.CreateItemGenerator();
                    Family.CreateMonsterSettings();

                    MonsterFamilies.Add(Family.ID, Family);
                }
                using (var reader = new StreamReader(Program.ServerConfig.DbLocation + "\\Spawns.txt"))
                {
                    var values = reader.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in values)
                    {
                        var data = line.Split(',');
                        uint ID = uint.Parse(data[0]);
                        uint MapId = uint.Parse(data[1]);
                        Game.MsgMonster.MobCollection colletion = new Game.MsgMonster.MobCollection(MapId);
                        if (colletion.ReadMap())
                        {

                            colletion.LocationSpawn = "";
                            Game.MsgMonster.MonsterFamily famil;
                            if (!MonsterFamilies.TryGetValue(ID, out famil))
                            {
                                continue;
                            }
                            if (Game.MsgMonster.MonsterRole.SpecialMonsters.Contains(famil.ID))
                                continue;
                            Game.MsgMonster.MonsterFamily Monster = famil.Copy();
                            Monster.SpawnX = ushort.Parse(data[2]);
                            Monster.SpawnY = ushort.Parse(data[3]);
                            Monster.MaxSpawnX = (ushort)(Monster.SpawnX + ushort.Parse(data[4]));
                            Monster.MaxSpawnY = (ushort)(Monster.SpawnY + ushort.Parse(data[5]));
                            Monster.MapID = MapId;
                            Monster.SpawnCount = int.Parse(data[6]);//"maxnpc", 0);//max_per_gen", 0);
                            //if (Monster.ID == 18)
                            //    Monster.SpawnCount *= 2;
                            Monster.rest_secs = int.Parse(data[7]);


                            if (Monster.MapID == 3071 || Monster.MapID == 1770 || Monster.MapID == 1771 || Monster.MapID == 1772
                                || Monster.MapID == 1773 || Monster.MapID == 1774 || Monster.MapID == 1775 || Monster.MapID == 1777
                                || Monster.MapID == 1782 || Monster.MapID == 1785 || Monster.MapID == 1786 || Monster.MapID == 1787
                                || Monster.MapID == 1794)
                                Monster.SpawnCount = int.Parse(data[8]) / 5;
                            if (Monster.MapID == 1020 || Monster.MapID == 1000)
                            {
                                if (Monster.SpawnCount > 5)
                                {
                                    if (Monster.MapID == 1020 )
                                    {

                                        Monster.MaxSpawnX += 20;
                                        Monster.MaxSpawnY += 20;
                                    }

                                }
                            }
                            colletion.Add(Monster);
                        }
                    }
                }
                foreach (string fmap in System.IO.Directory.GetDirectories(Program.ServerConfig.DbLocation + "\\MonsterSpawns\\"))
                {
                    uint tMapID;
                    if (!uint.TryParse(fmap.Remove(0, (Program.ServerConfig.DbLocation + "\\MonsterSpawns\\").Length), out tMapID))
                        continue;
                    Game.MsgMonster.MobCollection colletion = new Game.MsgMonster.MobCollection(tMapID);
                    if (colletion.ReadMap())
                    {
                        foreach (string fmobtype in System.IO.Directory.GetDirectories(fmap))
                        {
                            foreach (string ffile in System.IO.Directory.GetFiles(fmobtype))
                            {
                                ini.FileName = ffile;
                                colletion.LocationSpawn = ffile;

                                uint ID = ini.ReadUInt32("cq_generator", "npctype", 0);

                                Game.MsgMonster.MonsterFamily famil;
                                if (!MonsterFamilies.TryGetValue(ID, out famil))
                                {
                                    continue;
                                }
                                if (Game.MsgMonster.MonsterRole.SpecialMonsters.Contains(famil.ID))
                                    continue;
                                if (famil.ID == 6) continue;
                                Game.MsgMonster.MonsterFamily Monster = famil.Copy();

                                Monster.SpawnX = ini.ReadUInt16("cq_generator", "bound_x", 0);
                                Monster.SpawnY = ini.ReadUInt16("cq_generator", "bound_y", 0);
                                Monster.MaxSpawnX = (ushort)(Monster.SpawnX + ini.ReadUInt16("cq_generator", "bound_cx", 0));
                                Monster.MaxSpawnY = (ushort)(Monster.SpawnY + ini.ReadUInt16("cq_generator", "bound_cy", 0));
                                Monster.MapID = ini.ReadUInt32("cq_generator", "mapid", 0);
                                Monster.SpawnCount = ini.ReadByte("cq_generator", "max_per_gen", 0);//"maxnpc", 0);//max_per_gen", 0);
                                //if (Monster.ID == 18)
                                //    Monster.SpawnCount *= 2;
                                Monster.rest_secs = ini.ReadInt32("cq_generator", "rest_secs", 0);


                                if (Monster.MapID == 3071)
                                    Monster.SpawnCount = ini.ReadByte("cq_generator", "maxnpc", 0);
                                colletion.Add(Monster);
                            }
                        }
                    }
                }
                LoadMapMonsters("Monsters1779.txt");
                LoadMapMonsters("Monsters1780.txt");
                LoadMapMonsters("Monsters1020.txt");
                GC.Collect();
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
        }
        public static void LoadMapMonsters(string file)
        {
            if (System.IO.File.Exists(Program.ServerConfig.DbLocation + "MonsterSpawns\\" + file + ""))
            {
                using (System.IO.StreamReader read = System.IO.File.OpenText(Program.ServerConfig.DbLocation + "MonsterSpawns\\" + file + ""))
                {
                    while (true)
                    {

                        string aline = read.ReadLine();
                        if (aline != null && aline != "")
                        {
                            try
                            {
                                string[] line = aline.Split(',');
                                uint body = uint.Parse(line[1]);
                                string name = line[2];
                                if (name.Contains("Titan"))
                                    continue;
                                if (name == "WhiteTiger")
                                {

                                }
                                ushort X = ushort.Parse(line[3]);
                                ushort Y = ushort.Parse(line[4]);
                                uint Map = uint.Parse(line[5]);
                                var GMap = Database.Server.ServerMaps[Map];
                                if (Map == 1002)
                                {
                                    X += 128;
                                    Y += 100;
                                }
                                if (GMap.MonstersColletion == null)
                                {
                                    GMap.MonstersColletion = new Game.MsgMonster.MobCollection(GMap.ID);
                                }
                                else if (GMap.MonstersColletion.DMap == null)
                                    GMap.MonstersColletion.DMap = GMap;
                                foreach (var _monster in MonsterFamilies.Values)
                                {
                                    if (_monster.Name == name)
                                    {

                                        Game.MsgMonster.MonsterFamily Monster = _monster.Copy();

                                        Monster.SpawnX = X;
                                        Monster.SpawnY = Y;
                                        Monster.MaxSpawnX = (ushort)(X + 1);
                                        Monster.MaxSpawnY = (ushort)(Y + 1);
                                        Monster.MapID = GMap.ID;
                                        Monster.SpawnCount = 1;

                                        Game.MsgMonster.MonsterRole rolemonster = GMap.MonstersColletion.Add(Monster, false, 0, true);
                                        break;
                                    }


                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.ToString());
                                break;
                            }
                        }
                        else
                            break;

                    }
                }
            }
        }
        public unsafe static void AddMapMonster(ServerSockets.Packet stream, Role.GameMap map, uint ID, ushort x, ushort y, ushort max_x, ushort max_y, byte count, uint DinamicID = 0, bool RemoveOnDead = true
            , Game.MsgFloorItem.MsgItemPacket.EffectMonsters m_effect = Game.MsgFloorItem.MsgItemPacket.EffectMonsters.None, string streffect = "")
        {
            if (map.MonstersColletion == null)
            {
                map.MonstersColletion = new Game.MsgMonster.MobCollection(map.ID);
            }
            if (map.MonstersColletion.ReadMap())
            {

                Game.MsgMonster.MonsterFamily famil;
                if (MonsterFamilies.TryGetValue(ID, out famil))
                {
                    Game.MsgMonster.MonsterFamily Monster = famil.Copy();

                    Monster.SpawnX = x;
                    Monster.SpawnY = y;
                    Monster.MaxSpawnX = (ushort)(x + max_x);
                    Monster.MaxSpawnY = (ushort)(y + max_y);
                    Monster.MapID = map.ID;
                    Monster.SpawnCount = count;
                    Game.MsgMonster.MonsterRole rolemonster = map.MonstersColletion.Add(Monster, RemoveOnDead, DinamicID, true);

                    if (rolemonster == null)
                    {
                        Console.WriteLine("Eror monster spawn. Server. (" + Monster.Name + ")" + "MapID : " + Monster.MapID + "ID : " + Monster.ID);
                        return;
                    }
                    //   map.View.EnterMap<Role.IMapObj>(rolemonster);

                    Game.MsgServer.ActionQuery action = new Game.MsgServer.ActionQuery()
                    {
                        ObjId = rolemonster.UID,
                        Type = Game.MsgServer.ActionType.RemoveEntity
                    };
                    rolemonster.Send(stream.ActionCreate(&action));
                    rolemonster.Send(rolemonster.GetArray(stream, false));

                    if (streffect != null)
                    {
                        rolemonster.SendString(stream, MsgStringPacket.StringID.Effect, streffect);
                    }



                    if (m_effect != Game.MsgFloorItem.MsgItemPacket.EffectMonsters.None && rolemonster != null)
                    {
                        Game.MsgFloorItem.MsgItemPacket effect = Game.MsgFloorItem.MsgItemPacket.Create();
                        effect.m_UID = (uint)m_effect;
                        effect.m_X = rolemonster.X;
                        effect.m_Y = rolemonster.Y;
                        effect.DropType = MsgDropID.Earth;
                        rolemonster.Send(stream.ItemPacketCreate(effect));
                        rolemonster.SendString(stream, Game.MsgServer.MsgStringPacket.StringID.Effect, "glebesword");
                    }
                    if (rolemonster.HitPoints > 65535)
                    {
                        Game.MsgServer.MsgUpdate Upd = new Game.MsgServer.MsgUpdate(stream, rolemonster.UID, 2);
                        stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.MaxHitpoints, rolemonster.Family.MaxHealth);
                        stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Hitpoints, rolemonster.HitPoints);
                        stream = Upd.GetArray(stream);
                        rolemonster.Send(stream);
                    }
                }
            }
        }
        public unsafe static bool AddFloor(ServerSockets.Packet stream, Role.GameMap map, uint ID, ushort x, ushort y, ushort spelllevel, Database.MagicType.Magic dbspell, Client.GameClient Owner, uint GuildID, uint OwnerUID, uint DinamicID = 0, string Name = "", bool RemoveOnDead = true)
        {
            try
            {
                if (map.MonstersColletion == null)
                {
                    map.MonstersColletion = new Game.MsgMonster.MobCollection(map.ID);
                }
                if (map.MonstersColletion.ReadMap())
                {

                    Game.MsgMonster.MonsterFamily famil;
                    if (MonsterFamilies.TryGetValue(1, out famil))
                    {
                        Game.MsgMonster.MonsterFamily Monster = famil.Copy();

                        Monster.SpawnX = x;
                        Monster.SpawnY = y;
                        Monster.MaxSpawnX = (ushort)(x + 1);
                        Monster.MaxSpawnY = (ushort)(y + 1);
                        Monster.MapID = map.ID;
                        Monster.SpawnCount = 1;
                        Game.MsgMonster.MonsterRole rolemonster = map.MonstersColletion.Add(Monster, RemoveOnDead, DinamicID, true);
                        if (rolemonster == null)
                        {
                            //invalid x ,y
                            return false;
                        }
                        rolemonster.Family.ID = ID;
                        rolemonster.IsFloor = true;
                        rolemonster.FloorStampTimer = DateTime.Now.AddSeconds(7);
                        rolemonster.Family.Settings = Game.MsgMonster.MonsterSettings.Lottus;

                        rolemonster.FloorPacket = new MsgItemPacket();
                        rolemonster.FloorPacket.m_UID = rolemonster.UID;
                        rolemonster.FloorPacket.m_ID = ID;
                        rolemonster.FloorPacket.m_X = x;
                        rolemonster.FloorPacket.m_Y = y;
                        rolemonster.FloorPacket.MaxLife = 25;
                        rolemonster.FloorPacket.Life = 25;
                        rolemonster.FloorPacket.DropType = MsgDropID.Effect;
                        rolemonster.FloorPacket.m_Color = 13;
                        rolemonster.FloorPacket.ItemOwnerUID = OwnerUID;
                        rolemonster.FloorPacket.GuildID = GuildID;
                        rolemonster.FloorPacket.FlowerType = 2;//2;
                        rolemonster.FloorPacket.Timer = Role.Core.TqTimer(rolemonster.FloorStampTimer);
                        rolemonster.FloorPacket.Name = Name;

                        rolemonster.DBSpell = dbspell;
                        rolemonster.Family.MaxHealth = 25;
                        rolemonster.HitPoints = 25;
                        rolemonster.OwnerFloor = Owner;
                        rolemonster.SpellLevel = spelllevel;


                        if (rolemonster == null)
                        {
                            Console.WriteLine("Eror monster spawn. Server.");
                            return false;
                        }
                        map.View.EnterMap<Role.IMapObj>(rolemonster);
                        rolemonster.Send(rolemonster.GetArray(stream, false));
                        return true;
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
            return false;

        }
        public unsafe static void LoadDatabase()
        {
            try
            {
                foreach (string fname in System.IO.Directory.GetFiles(Program.ServerConfig.DbLocation + "\\Users\\"))
                {
                    WindowsAPI.IniFile IniFile = new WindowsAPI.IniFile(fname);
                    IniFile.FileName = fname;
                    string name = IniFile.ReadString("Character", "Name", "");
                    NameUsed.Add(name.GetHashCode());
                }
            }
            catch (Exception e)
            {
                Console.WriteException(e);
            }
        }
        public unsafe static void SaveDatabase()
        {
            if (!FullLoading)
                return;
            try
            {
                try
                {
                    Save(new Action(Role.Instance.Associate.Save));
                }
                catch (Exception e) { Console.SaveException(e); }
                try
                {
                    Save(new Action(Database.GuildTable.Save));
                }
                catch (Exception e) { Console.SaveException(e); }
                WindowsAPI.IniFile IniFile = new WindowsAPI.IniFile("");
                IniFile.FileName = System.IO.Directory.GetCurrentDirectory() + "\\shell.ini";
                IniFile.Write<uint>("Database", "ItemUID", ITEM_Counter.Count);
                IniFile.Write<uint>("Database", "ClientUID", ClientCounter.Count);
                IniFile.Write<uint>("Database", "Day", ResetServerDay);
                IniFile.Write<uint>("Tournaments", "PkWarWinner", Game.MsgTournaments.MsgSchedules.PkWar.WinnerUID);
                Save(new Action(Database.ClanTable.Save));
                Save(new Action(QueueContainer.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules.GuildWar.Save));
                #region CityWar
                Save(new Action(Game.MsgTournaments.MsgSchedules._Twin_War.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules._Phonix_War.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules._Ape_War.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules._Bird_War.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules._Desert_War.Save));
                #endregion
                Save(new Action(Game.MsgTournaments.MsgSchedules.PoleDominationPC.Save));
                //Save(new Action(Game.MsgTournaments.MsgSchedules.PlayerTop.SaveTop));
                //Save(new Action(Game.MsgTournaments.MsgSchedules.SuperGuildWar.Save));
                Save(new Action(TheCrimeTable.Save));
                Save(new Action(Arena.Save));
                Save(new Action(TeamArena.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules.ClassPkWar.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules.ElitePkTournament.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules.TeamPkTournament.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules.SkillTeamPkTournament.Save));
                //Save(new Action((Game.MsgTournaments.MsgSchedules.Tournaments[Game.MsgTournaments.TournamentType.BattleField]
                //    as Game.MsgTournaments.MsgBattleField).Save));
                Save(new Action(SystemBanned.Save));
                //Save(new Action(SystemBannedPC.Save));
                Save(new Action(SystemBannedAccount.Save));
                Save(new Action(ShareVIP.Save));
                Save(new Action(VoteSystem.Save));
                Save(new Action(Game.MsgTournaments.MsgSchedules.ClanWar.Save));
                IniFile = new WindowsAPI.IniFile("");
                IniFile.FileName = System.IO.Directory.GetCurrentDirectory() + "\\shell.ini";
                IniFile.Write<uint>("Database", "ItemUID", ITEM_Counter.Count);
                IniFile.Write<uint>("Database", "ClientUID", ClientCounter.Count);
                IniFile.Write<uint>("Database", "Day", ResetServerDay);
                IniFile.Write<uint>("Tournaments", "PkWarWinner", Game.MsgTournaments.MsgSchedules.PkWar.WinnerUID);
                Save(new Action(Role.Statue.Save));
                Save(new Action(Role.KOBoard.KOBoardRanking.Save));
            }
            catch (Exception e) { Console.WriteException(e); }
        }
        public static void Save(Action obj)
        {
            try
            {
                obj.Invoke();
            }
            catch (Exception e) { Console.SaveException(e); }
        }
        public static void LoadPortals()
        {

            if (System.IO.File.Exists(Program.ServerConfig.DbLocation + "portals.ini"))
            {
                using (System.IO.StreamReader read = System.IO.File.OpenText(Program.ServerConfig.DbLocation + "portals.ini"))
                {
                    ushort count = 0;
                    while (true)
                    {
                        string lines = read.ReadLine();
                        if (lines == null)
                            break;
                        ushort Map = ushort.Parse(lines.Split('[')[1].ToString().Split(']')[0]);
                        ushort Count = ushort.Parse(read.ReadLine().Split('=')[1]);
                        for (ushort x = 0; x < Count; x++)
                        {
                            Role.Portal portal = new Role.Portal();
                            string[] line = read.ReadLine().Split('=')[1].Split(' ');
                            portal.MapID = ushort.Parse(line[0]);
                            portal.X = ushort.Parse(line[1]);
                            portal.Y = ushort.Parse(line[2]);

                            string[] dline = read.ReadLine().Split('=')[1].Split(' ');
                            portal.Destiantion_MapID = ushort.Parse(dline[0]);
                            portal.Destiantion_X = ushort.Parse(dline[1]);
                            portal.Destiantion_Y = ushort.Parse(dline[2]);
                            if (ServerMaps.ContainsKey(portal.MapID))
                                ServerMaps[portal.MapID].Portals.Add(portal);
                            count++;
                        }
                    }
                    //Console.WriteLine("Loading " + count + " portals");
                }
            }
            GC.Collect();
        }
    }
}
