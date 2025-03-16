using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using COServer.Game.MsgServer;

namespace COServer.Role.Instance
{
    public class Proficiency
    {
        public const uint MaxExperience = 1000000;

        public ConcurrentDictionary<uint, Game.MsgServer.MsgProficiency> ClientProf = new ConcurrentDictionary<uint, Game.MsgServer.MsgProficiency>();
       
        private Client.GameClient Owner;
        public static uint[] proficiencyLevelExperience = new uint[21] { 0, 1200, 68000, 250000, 640000, 1600000, 4000000, 10000000, 22000000, 40000000, 90000000, 95000000, 142500000, 213750000, 320625000, 480937500, 721406250, 1082109375, 1623164063, 2100000000, 0 };

        public static uint ProficiencyLevelExperience(byte Level)
        {
            return proficiencyLevelExperience[Math.Min(Level, (byte)20)];
        }
        public Proficiency(Client.GameClient _own)
        {
            Owner = _own;
        }
        public bool CheckProf(ushort ID, byte level)
        {
            Game.MsgServer.MsgProficiency prof;
            if (ClientProf.TryGetValue(ID, out prof))
            {
                return prof.Level >= level;
            }
            return false;
        }
        public unsafe void Add(ServerSockets.Packet stream, uint ID, uint Level = 0, uint Experience = 0, byte PreviousLevel = 0, bool ClearExp =false)
        {
            Game.MsgServer.MsgProficiency prof;
            if (ClientProf.TryGetValue(ID, out prof))
            {
                prof.UID = Owner.Player.UID;
                prof.Level = Level;
                prof.Experience = Experience;
                prof.PreviouseLevel = PreviousLevel;
                if (prof.Level == 20 || ClearExp)
                    prof.Experience = 0;
            }
            else
            {
                prof = new MsgProficiency();
                prof.ID = ID;
                prof.UID = Owner.Player.UID;
                prof.Level = Level;
                prof.Experience = Experience;
                if (prof.Level == 20 || ClearExp)
                    prof.Experience = 0;
                prof.PreviouseLevel = PreviousLevel;
                ClientProf.TryAdd(prof.ID, prof);
            }
            Owner.Send(stream.ProficiencyCreate(prof.ID, prof.Level, prof.Experience, Owner.Player.UID));
            UpdSpell(prof.ID, prof.Level, prof.Experience,stream);
        }


        private unsafe void UpdSpell(uint ID, uint level, uint Experience, ServerSockets.Packet stream)
        {
            Owner.Send(stream.UpdateProfExperienceCreate(Experience, Owner.Player.UID, ID));
        }
        public unsafe void CheckUpdate(uint ID, uint GetExperience,ServerSockets.Packet stream)
        {
            if (GetExperience == 0)
                return;
            //if (ID == 1050)
            //    return;
            if (Enum.IsDefined(typeof(Database.MagicType.WeaponsType), (Database.MagicType.WeaponsType)ID))
            {
                Game.MsgServer.MsgProficiency prof;
                if (ClientProf.TryGetValue(ID, out prof))
                {
                    if (prof.Level < 20)
                    {
                       
                        if (prof.Level < 5)
                        {
                            GetExperience *= 100;
                        }
                        else if (prof.Level > 13)
                        {
                            GetExperience /= 100;
                        }
                        GetExperience /= (uint)(prof.Level + 1);

                        prof.Experience += GetExperience;
                        prof.Experience += (uint)(GetExperience * Owner.GemValues(Role.Flags.Gem.NormalVioletGem)) / 100;
                    
                        bool leveled = false;

                        while (prof.Experience >= MaxExperience)
                        {
                            prof.Experience -= MaxExperience;
                            prof.Level++;

                            if (prof.Level == 20)
                            {
                                prof.Experience = 0;

                                Owner.Send(stream.ProficiencyCreate(prof.ID, prof.Level, prof.Experience, Owner.Player.UID));
                                Owner.SendSysMesage("You have just leveled your proficiency.", Game.MsgServer.MsgMessage.ChatMode.System);
                                UpdSpell(prof.ID, prof.Level, prof.Experience, stream);
                                return;
                            }
                            leveled = true;
                            Owner.SendSysMesage("You have just leveled your proficiency.", Game.MsgServer.MsgMessage.ChatMode.System);                          
                            if (prof.PreviouseLevel != 0)
                            {
                                if (prof.Level >= prof.PreviouseLevel / 2 && prof.Level < prof.PreviouseLevel)
                                {
                                    prof.Level = prof.PreviouseLevel;
                                    prof.Experience = 0;
                                }
                            }
                        }
                        if (leveled)
                        {
                            Owner.Send(stream.ProficiencyCreate(prof.ID, prof.Level, prof.Experience, Owner.Player.UID));
                        }
                        UpdSpell(prof.ID, prof.Level, prof.Experience, stream);

                    }
                }
                else
                {
                    Add(stream,ID);
                }
            }
        }
        public unsafe void SendAll(ServerSockets.Packet stream)
        {
            foreach (var prof in ClientProf.Values)
                Owner.Send(stream.ProficiencyCreate(prof.ID, prof.Level, prof.Experience, Owner.Player.UID));
        }
    }
}
