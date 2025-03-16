﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using COServer.Game.MsgServer;

namespace COServer.Role.Instance
{
    public class Nobility
    {
        public class NobilityRanking
        {
            public ulong KnightDonation = 30000000, BaronDonation = 100000000, EarlDonation = 200000000;
            public ulong DukeDonation;
            public ulong PrinceDonation;
            public ulong KingDonation;

            private Dictionary<uint, Nobility> ClientPoll;

            public NobilityRanking()
            {
                ClientPoll = new Dictionary<uint, Nobility>();
            }
            public bool TryGetValue(uint UID, out Nobility user)
            {
                return ClientPoll.TryGetValue(UID, out user);
            }
            public Nobility[] GetArray()
            {
                return ClientPoll.Values.ToArray();
            }
            public void UpdateRank(Nobility user)
            {
                if (ClientPoll.Count < 50)
                    CreateRank(user);
                else
                {
                    var array = ClientPoll.Values.ToArray();
                    if (array[49].Donation < user.Donation)
                        CreateRank(user);
                    else if (ClientPoll.ContainsKey(user.UID))
                        CreateRank(user);
                }
            }
            internal void CreateRank(Nobility user)
            {
                lock (ClientPoll)
                {
                    using (var rec = new ServerSockets.RecycledPacket())
                    {
                        var stream = rec.GetStream();
                        if (!ClientPoll.ContainsKey(user.UID))
                            ClientPoll.Add(user.UID, user);
                        var Array = ClientPoll.Values.Where(p => p.Donation > 600).ToArray();
                        var Order = Array.OrderByDescending(p => p.Donation).ToArray();
                        ClientPoll.Clear();

                        for (int x = 0; x < Order.Length; x++)
                        {
                            var nobilityclient = Order[x];
                            int OldPosition = nobilityclient.Position;
                            var OldRank = nobilityclient.Rank;
                            if (x < 50)
                            {
                                nobilityclient.Position = x;
                                ClientPoll.Add(nobilityclient.UID, nobilityclient);
                                if (x == 3)
                                    KingDonation = nobilityclient.Donation;
                                else if (x == 14)
                                    PrinceDonation = nobilityclient.Donation;
                                else if (x == 49)
                                    DukeDonation = nobilityclient.Donation;
                            }
                            else
                                nobilityclient.Position = -1;

                            if (OldPosition != nobilityclient.Position)
                            {

                                UpdateClientIcon(stream, nobilityclient);
                                if (nobilityclient.Rank > OldRank)
                                {
                                    if (Program.SendGlobalPackets != null)
                                    {
                                        Program.SendGlobalPackets.Enqueue(new Game.MsgServer.MsgMessage(nobilityclient.Name + " has been promoted to " + nobilityclient.Rank.ToString() + ".", "ALL", Game.MsgServer.MsgMessage.MsgColor.red, Game.MsgServer.MsgMessage.ChatMode.Center).GetArray(stream));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            internal unsafe void UpdateClientIcon(ServerSockets.Packet stream, Nobility user)
            {
                Client.GameClient client;
                if (Database.Server.GamePoll.TryGetValue(user.UID, out client))
                {
                    client.Player.NobilityRank = user.Rank;
                    client.Send(stream.NobilityIconCreate(user));
                }
            }
        }
        public enum NobilityRank : byte
        {
            Serf = 0,
            Knight = 1,
            Baron = 3,
            Earl = 5,
            Duke = 7,
            Prince = 9,
            King = 12
        }


        public uint UID;
        public string Name;
        public int Position = -1;
        public ulong Donation;
        public uint Mesh;
        public byte Gender;
        public NobilityRank Rank
        {
            get
            {
                if (Position >= 0 && Position < 3)//0,1,2,3,4
                    return NobilityRank.King;
                else if (Position >= 3 && Position < 15)//5,....,15
                    return NobilityRank.Prince;
                else if (Position >= 15 && Position < 49)//16,....,80
                    return NobilityRank.Duke;
                if (Donation >= 200000000)
                    return NobilityRank.Earl;
                else if (Donation >= 100000000)
                    return NobilityRank.Baron;
                else if (Donation >= 30000000)
                    return NobilityRank.Knight;

                return NobilityRank.Serf;
            }
        }
        public Nobility(uint _uid, string _name, ulong _donation, uint _mesh, byte _gender)
        {
            UID = _uid;
            Name = _name;
            Donation = _donation;
            Mesh = _mesh;
            Gender = _gender;
        }
        public Nobility(Client.GameClient user)
        {
            UID = user.Player.UID;
            Name = user.Player.Name;
            Mesh = user.Player.Mesh;
            Gender = user.Player.GetGender;
        }
    }
}
