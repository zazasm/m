using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COServer.Game.MsgMonster
{
    public class MobCollection
    {
        public const byte Multiple = 3;

        public string LocationSpawn = "";

        public object SyncRoot = new object();
        public static Extensions.Counter GenerateUid = new Extensions.Counter(400000);
        public Role.GameMap DMap = null;

        private uint DmapID = 0;
        public MobCollection(uint Map)
        {
            DmapID = Map;
            if (Database.Server.ServerMaps != null)
            {
                if (Database.Server.ServerMaps.TryGetValue(Map, out DMap))
                    DMap.MonstersColletion = this;
            }
        }
        public bool ReadMap()
        {
            if (Database.Server.ServerMaps != null)
            {
                var mapId = DmapID;
                //if (mapId == 8800)
                //    mapId = 1354;
                if (Database.Server.ServerMaps.TryGetValue(mapId, out DMap))
                    DMap.MonstersColletion = this;
            }

            return DMap != null;
        }
        public MonsterRole Add(MonsterFamily Famili, bool RemoveOnDead = false, uint dinamicid = 0, bool justone = false)
        {
            if (DMap == null)
                ReadMap();
            if (Famili.ID == 8303)
            {

            }
            MonsterRole monsterr = null;
            int count = (int)((Famili.Boss > 0) ? 1 : (int)(Math.Max(1, (int)Famili.SpawnCount) * Multiple));
            if (justone)
                count = Math.Max(1, (int)Famili.SpawnCount);


            if (DmapID == 1011 || DmapID == 3071 || DmapID == 1770 || DmapID == 1771 || DmapID == 1772 || DmapID == 1773 || DmapID == 1774
                || DmapID == 1775 || DmapID == 1777 || DmapID == 1782
                || DmapID == 1785 || DmapID == 1786 || DmapID == 1787 || DmapID == 1794)
            {
                if (count > 1 && Famili.SpawnCount > 1)
                    count = (int)(((Famili.MaxSpawnX - Famili.SpawnX) / Famili.SpawnCount) * ((Famili.MaxSpawnY - Famili.SpawnY) / Famili.SpawnCount));
            }

            for (int x = 0; x < count; x++)
            {

                MonsterRole Mob = new MonsterRole(Famili.Copy(), GenerateUid.Next, LocationSpawn, DMap);
                Mob.RemoveOnDead = RemoveOnDead;
                ushort _x = 0, _y = 0;
                TryObtainSpawnXY(Famili, out _x, out _y);
                if (!DMap.ValidLocation(_x, _y) || (DMap.MonsterOnTile(_x, _y) && Mob.Boss == 0))
                    continue;
                DMap.SetMonsterOnTile(_x, _y, true);

                Mob.X = _x;
                Mob.Y = _y;
                Mob.RespawnX = _x;
                Mob.RespawnY = _y;

                Mob.Map = DMap.ID;
                Mob.DynamicID = dinamicid;

                monsterr = Mob;

                DMap.View.EnterMap<MonsterRole>(Mob);
            }
            return monsterr;
        }

        /// <summary>
        /// Attemps to obtain a point where the monster can be re-spawned.
        /// </summary>
        /// <param name="X">The x-coordinate point.</param>
        /// <param name="Y">The y-coordinate point.</param>
        public void TryObtainSpawnXY(MonsterFamily Monster, out ushort X, out ushort Y)
        {

            X = (ushort)Program.GetRandom.Next(Monster.SpawnX, Monster.MaxSpawnX);
            Y = (ushort)Program.GetRandom.Next(Monster.SpawnY, Monster.MaxSpawnY);

            for (byte i = 0; i < 100; i++)
            {
                if (DMap == null)
                    break;
                if (DMap.ValidLocation(X, Y) && !DMap.MonsterOnTile(X, Y))
                    break;

                X = (ushort)Program.GetRandom.Next(Monster.SpawnX, Monster.MaxSpawnX);
                Y = (ushort)Program.GetRandom.Next(Monster.SpawnY, Monster.MaxSpawnY);
            }
        }
    }
}
