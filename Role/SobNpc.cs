using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using COServer.Game.MsgServer;
using COServer.Game.MsgTournaments;

namespace COServer.Role
{

    public unsafe class SobNpc : IMapObj
    {
        public enum StaticMesh : ushort
        {
            Vendor = 406,
            LeftGate = 241,
            OpenLeftGate = 251,
            RightGate = 277,
            OpenRightGate = 287,
            Pole = 1137,
            PoleDomination = 3868,
            SuperGuildWarPole = 31220
        }

        public Role.Statue statue = null;
        public bool AllowDynamic { get; set; }
        public Role.StatusFlagsBigVector32 BitVector;
        public uint IndexInScreen { get; set; }
        public bool IsStatue
        {
            get { return statue != null; }
        }
        public SobNpc(Role.Statue _statue)
        {
            statue = _statue;
            BitVector = new StatusFlagsBigVector32(32 * 5);
        }


        public SobNpc()
        {
            AllowDynamic = false;
            BitVector = new StatusFlagsBigVector32(32 * 5);
        }
        public const byte SeedDistrance = 19;//17
        public bool IsTrap() { return false; }
        public uint UID { get; set; }
        public int MaxHitPoints { get; set; }
        int Hit;
        public int HitPoints
        {
            get { return Hit; }
            set
            {
                Hit = value;
            }

        }

        public ushort X { get; set; }
        public ushort Y { get; set; }
        public StaticMesh Mesh;
        public Flags.NpcType Type;
        public ushort Sort;
        public string Name;

        public uint Map { get; set; }
        public uint DynamicID { get; set; }

        public bool Alive { get { return HitPoints > 0; } }
        public MapObjectType ObjType { get; set; }

        public Client.GameClient OwnerVendor;

        public void RemoveRole(IMapObj obj)
        {

        }
        public unsafe void Send(byte[] packet)
        {

        }
        public unsafe void Send(ServerSockets.Packet msg)
        {

        }
        public bool AddFlag(Game.MsgServer.MsgUpdate.Flags Flag, int Seconds, bool RemoveOnDead, int StampSeconds = 0, uint showamount = 0, uint amount = 0)
        {
            if (!BitVector.ContainFlag((int)Flag))
            {
                BitVector.TryAdd((int)Flag, Seconds, RemoveOnDead, StampSeconds);
                UpdateFlagScreen();
                return true;
            }
            return false;
        }
        public bool RemoveFlag(Game.MsgServer.MsgUpdate.Flags Flag)
        {
            if (BitVector.ContainFlag((int)Flag))
            {
                BitVector.TryRemove((int)Flag);
                UpdateFlagScreen();

                return true;
            }
            return false;
        }
        public bool ContainFlag(Game.MsgServer.MsgUpdate.Flags Flag)
        {
            return BitVector.ContainFlag((int)Flag);
        }
        public void UpdateFlagScreen()
        {
            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();

                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, MsgUpdate.DataType.StatusFlag, BitVector.bits);
                stream = upd.GetArray(stream);

                foreach (var user in Database.Server.GamePoll.Values)
                {
                    if (user.Player.Map == Map)
                        user.Send(stream);
                }
            }
        }

        public unsafe void Die(ServerSockets.Packet stream, Client.GameClient killer)
        {
            if (UID >= 7000 && UID <= 7008)
            {
                if (HitPoints == 0)
                {
                }
            }
            else
            {
                if (HitPoints == 0)
                    return;
            }
            //if (killer.OnAutoAttack)
            //    killer.OnAutoAttack = false;
            if (Map == 1039)
            {
                HitPoints = MaxHitPoints;
                InteractQuery action = new InteractQuery()
                {
                    UID = killer.Player.UID,
                    X = X,
                    Y = Y,
                    AtkType = MsgAttackPacket.AttackID.Death,
                    KillCounter = killer.Player.KillCounter,
                    SpellID = (ushort)(Database.ItemType.IsBow(killer.Equipment.RightWeapon) ? 5 : 1),
                    OpponentUID = UID,
                };
                killer.Player.View.SendView(stream.InteractionCreate(&action), true);


                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 2);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Hitpoints, (long)HitPoints);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.MaxHitpoints, (long)MaxHitPoints);
                stream = upd.GetArray(stream);
                killer.Player.View.SendView(stream, true);
                return;
            }
            if (IsStatue)
            {
                HitPoints = 0;
                Role.Statue.RemoveStatue(stream, killer, UID, this);
                return;
            }
            else if (UID >= 7000 && UID <= 7008)
            {
                HitPoints = MaxHitPoints;
                InteractQuery action = new InteractQuery()
                {
                    UID = killer.Player.UID,
                    X = X,
                    Y = Y,
                    AtkType = MsgAttackPacket.AttackID.Death,
                    KillCounter = killer.Player.KillCounter,
                    SpellID = (ushort)(Database.ItemType.IsBow(killer.Equipment.RightWeapon) ? 5 : 1),
                    OpponentUID = UID,
                };
                killer.Player.View.SendView(stream.InteractionCreate(&action), true);


                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 2);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Hitpoints, (long)HitPoints);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.MaxHitpoints, (long)MaxHitPoints);
                stream = upd.GetArray(stream);
                killer.Player.View.SendView(stream, true);
                killer.Player.Away = 0;
                return;

            }

            #region CityWar
            #region _Twin_War
            #region RightGate
            else if (UID == MsgSchedules._Twin_War.Furnitures[StaticMesh.RightGate].UID)
            {
                if (MsgSchedules._Twin_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Twin_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The right gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenRightGate;
                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region LeftGate
            else if (UID == MsgSchedules._Twin_War.Furnitures[StaticMesh.LeftGate].UID)
            {
                if (MsgSchedules._Twin_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Twin_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The left gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenLeftGate;

                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region Pole
            else if (UID == MsgSchedules._Twin_War.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules._Twin_War.UpdateScore(killer.Player, Damage);
            }
            #endregion
            #endregion
            #region _Phonix_War
            #region RightGate
            else if (UID == MsgSchedules._Phonix_War.Furnitures[StaticMesh.RightGate].UID)
            {
                if (MsgSchedules._Phonix_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Phonix_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The right gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenRightGate;
                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region LeftGate
            else if (UID == MsgSchedules._Phonix_War.Furnitures[StaticMesh.LeftGate].UID)
            {
                if (MsgSchedules._Phonix_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Phonix_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The left gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenLeftGate;

                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region Pole
            else if (UID == MsgSchedules._Phonix_War.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules._Phonix_War.UpdateScore(killer.Player, Damage);
            }
            #endregion
            #endregion
            #region _Ape_War
            #region RightGate
            else if (UID == MsgSchedules._Ape_War.Furnitures[StaticMesh.RightGate].UID)
            {
                if (MsgSchedules._Ape_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Ape_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The right gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenRightGate;
                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region LeftGate
            else if (UID == MsgSchedules._Ape_War.Furnitures[StaticMesh.LeftGate].UID)
            {
                if (MsgSchedules._Ape_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Ape_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The left gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenLeftGate;

                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region Pole
            else if (UID == MsgSchedules._Ape_War.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules._Ape_War.UpdateScore(killer.Player, Damage);
            }
            #endregion
            #endregion
            #region _Bird_War
            #region RightGate
            else if (UID == MsgSchedules._Bird_War.Furnitures[StaticMesh.RightGate].UID)
            {
                if (MsgSchedules._Bird_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Bird_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The right gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenRightGate;
                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region LeftGate
            else if (UID == MsgSchedules._Bird_War.Furnitures[StaticMesh.LeftGate].UID)
            {
                if (MsgSchedules._Bird_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Bird_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The left gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenLeftGate;

                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region Pole
            else if (UID == MsgSchedules._Bird_War.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules._Bird_War.UpdateScore(killer.Player, Damage);
            }
            #endregion
            #endregion
            #region _Desert_War
            #region RightGate
            else if (UID == MsgSchedules._Desert_War.Furnitures[StaticMesh.RightGate].UID)
            {
                if (MsgSchedules._Desert_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Desert_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The right gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenRightGate;
                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region LeftGate
            else if (UID == MsgSchedules._Desert_War.Furnitures[StaticMesh.LeftGate].UID)
            {
                if (MsgSchedules._Desert_War.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules._Desert_War.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[Twin_War] The left gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenLeftGate;

                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            #endregion
            #region Pole
            else if (UID == MsgSchedules._Desert_War.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules._Desert_War.UpdateScore(killer.Player, Damage);
            }
            #endregion
            #endregion
            #endregion
            else if (UID == MsgSchedules.GuildWar.Furnitures[StaticMesh.RightGate].UID)
            {
                if (MsgSchedules.GuildWar.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules.GuildWar.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[GuildWar] The right gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenRightGate;
                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            else if (UID == MsgSchedules.GuildWar.Furnitures[StaticMesh.LeftGate].UID)
            {
                if (MsgSchedules.GuildWar.Winner != null)
                {
                    Instance.Guild guild;
                    if (Instance.Guild.GuildPoll.TryGetValue(MsgSchedules.GuildWar.Winner.GuildID, out guild))
                    {
                        guild.SendMessajGuild("[GuildWar] The left gate has been breached!");
                    }
                }
                Mesh = StaticMesh.OpenLeftGate;

                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, UID, 1);
                stream = upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Mesh, (long)Mesh);
                stream = upd.GetArray(stream);
                foreach (var client in Database.Server.GamePoll.Values)
                {
                    if (client.Player.Map == Map)
                    {
                        if (Role.Core.GetDistance(client.Player.X, client.Player.Y, X, Y) <= Role.SobNpc.SeedDistrance)
                        {
                            client.Send(stream);
                        }

                    }
                }
            }
            else if (UID == 890)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.ClassicClanWar.UpdateScore(killer.Player, Damage);
            }

            else if (UID == MsgSchedules.GuildWar.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.GuildWar.UpdateScore(killer.Player, Damage);
            }
            else if (UID == MsgSchedules.EliteGuildWar.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.EliteGuildWar.UpdateScore(killer.Player, Damage);
            }
            else if (UID == MsgSchedules.PoleDomination.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.PoleDomination.UpdateScore(killer.Player, Damage);
            }
            else if (UID == MsgSchedules.PoleDominationBI.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.PoleDominationBI.UpdateScore(killer.Player, Damage);
            }
            else if (UID == MsgSchedules.PoleDominationDC.Furnitures[StaticMesh.Pole].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.PoleDominationDC.UpdateScore(killer.Player, Damage);
            }
            else if (UID == MsgSchedules.PoleDominationPC.Furnitures[StaticMesh.PoleDomination].UID)
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.PoleDominationPC.UpdateScore(killer.Player, Damage);
            }
            else if (MsgSchedules.CaptureTheFlag.Bases.ContainsKey(UID))
            {
                uint Damage = (uint)HitPoints;
                if (HitPoints > 0)
                {
                    HitPoints = 0;
                }
                MsgSchedules.CaptureTheFlag.UpdateFlagScore(killer.Player, this, 0, stream);

            }
            else if (HitPoints > 0)
            {
                HitPoints = 0;
            }
        }
        public unsafe void SendString(ServerSockets.Packet stream, Game.MsgServer.MsgStringPacket.StringID id, params string[] args)
        {
            Game.MsgServer.MsgStringPacket packet = new Game.MsgServer.MsgStringPacket();
            packet.ID = id;
            packet.UID = UID;
            packet.Strings = args;

            SendScrennPacket(stream.StringPacketCreate(packet));
        }

        public unsafe void SendScrennPacket(ServerSockets.Packet packet)
        {
            foreach (var client in Database.Server.GamePoll.Values)
            {
                if (client.Player.Map == Map)
                {
                    if (client.Player.GetMyDistance(X, Y) < SeedDistrance)
                    {
                        client.Send(packet);
                    }
                }
            }
        }
        public unsafe ServerSockets.Packet GetArray(ServerSockets.Packet stream, bool view)
        {
            if (statue != null)
            {
                if (statue.StatuePacket != null && statue.Static)
                {
                    stream.Seek(0);
                    fixed (byte* ptr = statue.StatuePacket)
                    {
                        stream.memcpy(stream.Memory, ptr, statue.StatuePacket.Length);
                    }
                    stream.Size = statue.StatuePacket.Length;
                    return stream;
                }
                stream.InitWriter();

                //  stream.Write(Extensions.Time32.Now.Value);
                stream.Write((uint)(statue.user.Player.TransformationID * 10000000 + statue.user.Player.Face * 10000 + statue.user.Player.Body));
                stream.Write(UID);
                stream.Write(statue.user.Player.GuildID);
                stream.Write((ushort)statue.user.Player.GuildRank);
                stream.Write((uint)0);//unknow

                for (int x = 0; x < statue.user.Player.BitVector.bits.Length; x++)
                    stream.Write(0);//out flags

                stream.Write((ushort)0);//apparence type
                stream.Write(statue.user.Player.HeadId);
                stream.Write(statue.user.Player.GarmentId);
                stream.Write(statue.user.Player.ArmorId);
                stream.Write(statue.user.Player.LeftWeaponId);
                stream.Write(statue.user.Player.RightWeaponId);
                stream.Write(statue.user.Player.LeftWeaponAccessoryId);
                stream.Write(statue.user.Player.RightWeaponAccessoryId);
                stream.Write(statue.user.Player.SteedId);
                stream.Write(statue.user.Player.MountArmorId);

                stream.ZeroFill(6);//unknow

                stream.Write(HitPoints / 500);
                stream.Write((ushort)0);//unknow
                stream.Write((ushort)0);//monster level

                stream.Write(X);
                stream.Write(Y);
                stream.Write(statue.user.Player.Hair);
                if (statue.Static)
                    stream.Write((byte)0);
                else
                    stream.Write((byte)statue.user.Player.Angle);
                if (statue.Static)
                    stream.Write((uint)Role.Flags.ConquerAction.Sit);
                else
                    stream.Write((uint)statue.Action);
                stream.Write((ushort)statue.Action2);//unknow
                stream.Write((byte)0);//padding?
                stream.Write(statue.user.Player.Reborn);
                stream.Write(statue.user.Player.Level);
                stream.Write((byte)0);
                stream.Write((byte)0);//away
                stream.Write(statue.user.Player.ExtraBattlePower);
                stream.Write((uint)0);//unknow position = 125
                stream.Write((uint)0);//unknow position = 129
                stream.Write((uint)0);//unknow p = 133;
                stream.Write((uint)(statue.user.Player.FlowerRank + 10000));
                stream.Write((uint)statue.user.Player.NobilityRank);

                stream.Write(statue.user.Player.ColorArmor);
                stream.Write(statue.user.Player.ColorShield);
                stream.Write(statue.user.Player.ColorHelment);
                stream.Write((uint)0);//quiz points
                stream.Write(statue.user.Player.SteedPlus);
                stream.Write((ushort)0);//unknow
                stream.Write(statue.user.Player.SteedColor);
                stream.Write((ushort)statue.user.Player.Enilghten);
                stream.Write((ushort)0);//merit points
                stream.Write((uint)0);//unknow
                stream.Write((uint)0);//unknow
                stream.Write(statue.user.Player.ClanUID);
                stream.Write(statue.user.Player.ClanRank);
                stream.Write(0);//p = 187
                stream.Write((ushort)0);//unknow
                stream.Write(statue.user.Player.MyTitle);

                stream.ZeroFill(14);
                stream.Write(statue.user.Player.HeadSoul);
                stream.Write(statue.user.Player.ArmorSoul);
                stream.Write(statue.user.Player.LeftWeapsonSoul);
                stream.Write(statue.user.Player.RightWeapsonSoul);
                stream.Write((byte)statue.user.Player.ActiveSublass);
                stream.Write(statue.user.Player.SubClassHasPoints);
                stream.Write((uint)0);//unknow
                stream.Write((ushort)statue.user.Player.FirstClass);
                stream.Write((ushort)statue.user.Player.SecondClass);
                stream.Write((ushort)statue.user.Player.Class);
                stream.Write((ushort)statue.user.Player.CountryID);//unknow
                stream.Write((uint)0);
                stream.Write(statue.user.Player.BattlePower);
                stream.Write((byte)0);
                stream.Write((byte)0);
                /*  stream.ZeroFill(10);
                  stream.Write(0);
                  stream.Write(0);
                  stream.Write(0);*/
                stream.Write((ushort)0);

                stream.Write((byte)0);
                stream.Write((uint)0);
                stream.Write((byte)2);//clone count 
                stream.Write((ushort)0); // clone ID
                stream.Write(0); //clone owner
                stream.Write((ushort)0);//3

                stream.Write(10);//union id
                stream.Write((ushort)0);//1
                stream.Write((uint)0);//union type 
                stream.Write(ushort.MaxValue);//1 = union leader;

                stream.Write((uint)statue.user.Player.MainFlag);
                stream.Write(0);
                stream.Write(Name, string.Empty, statue.user.Player.ClanName);

                stream.Finalize(Game.GamePackets.SpawnPlayer);
                if (statue.StatuePacket == null && statue.Static)
                {
                    statue.StatuePacket = new byte[stream.Size];
                    int size = stream.Size;
                    fixed (byte* ptr = statue.StatuePacket)
                    {
                        stream.memcpy(ptr, stream.Memory, size);
                    }
                }
                return stream;
            }
            stream.InitWriter();//4

            stream.Write(UID);//8
            stream.Write(0);//8
            stream.Write(MaxHitPoints);//12
            stream.Write(HitPoints);//14
            stream.Write(X);//16
            stream.Write(Y);//18
            stream.Write((ushort)Mesh);//20
            stream.Write((ushort)Type);//22
            stream.Write((ushort)Sort);//24
            stream.ZeroFill(5);//27
            if (Name != "")
            {
                stream.Write((byte)1);//28
                if (Name != null)
                {
                    if (Name.Length > 16)
                        Name = Name.Substring(0, 16);

                    stream.Write(Name);
                }
            }
            stream.Finalize(Game.GamePackets.SobNpcs);

            return stream;


        }
    }
}
