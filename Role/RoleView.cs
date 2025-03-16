using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using COServer.Game.MsgServer;
using COServer.Game.MsgNpc;

namespace COServer.Role
{
    public class RoleView
    {
        public const int ViewThreshold = 18;

        public Extensions.Time32 Monster_BuffersCallbackStamp = Extensions.Time32.Now.AddMilliseconds(MapGroupThread.AI_Buffers);
        public Extensions.Time32 Monster_GuardsCallbackStamp = Extensions.Time32.Now.AddMilliseconds(MapGroupThread.AI_Guards);
        public Extensions.Time32 Monster_AliveMonstersCallback = Extensions.Time32.Now.AddMilliseconds(MapGroupThread.AI_Monsters);
        public Extensions.Time32 Monster_ReviverCallBack = Extensions.Time32.Now.AddMilliseconds(MapGroupThread.AI_Monsters);

        public Game.MsgMonster.ActionHandler MobActions = new Game.MsgMonster.ActionHandler();


        public Extensions.SafeDictionary<uint, IMapObj>[] src;
        public Client.GameClient Owner;
        public Role.Player GetPlayer()
        {
            return Owner.Player;
        }
        public RoleView(Client.GameClient _client)
        {
            Owner = _client;
            src = new Extensions.SafeDictionary<uint, IMapObj>[(byte)MapObjectType.Count];
            for (byte x = 0; x < (byte)MapObjectType.Count; x++)
                src[x] = new Extensions.SafeDictionary<uint, IMapObj>();
        }

        public void CheckUpMonsters(Extensions.Time32 clock)
        {
            if (clock > Monster_BuffersCallbackStamp)
            {
                Game.MsgMonster.PoolProcesses.BuffersCallback(Owner, clock);
                Game.AISystem.AIBotHandle.Action();
                Monster_BuffersCallbackStamp.Value = clock.Value + MapGroupThread.AI_Buffers;
            }
            else if (clock > Monster_GuardsCallbackStamp)
            {
                Game.MsgMonster.PoolProcesses.GuardsCallback(Owner, clock);
                Monster_GuardsCallbackStamp.Value = clock.Value + MapGroupThread.AI_Guards;
            }
            else if (clock > Monster_AliveMonstersCallback)
            {
                Game.MsgMonster.PoolProcesses.AliveMonstersCallback(Owner, clock);
                Monster_AliveMonstersCallback.Value = clock.Value + MapGroupThread.AI_Monsters;
            }
            else if (clock > Monster_ReviverCallBack)
            {
                Game.MsgMonster.PoolProcesses.ReviversCallback(Owner, clock);
                Monster_ReviverCallBack.Value = clock.Value + MapGroupThread.AI_Monsters;
            }
        }


        public unsafe void ReSendView(ServerSockets.Packet stream)
        {
            SendView(Owner.Player.GetArray(stream, false), false);
        }
        public bool SameLocation(MapObjectType typ, out Role.IMapObj obj)
        {
            foreach (var client in Roles(typ))
            {
                if (client.X == GetPlayer().X && client.Y == GetPlayer().Y)
                {
                    obj = client;
                    return true;
                }
            }
            obj = null;
            return false;
        }

        public unsafe void SendView(ServerSockets.Packet msg, bool me, bool effect = false, bool showPersonalEffect = false, bool isWatching = false)
        {

            if (me)
                if (!effect || (effect && showPersonalEffect))
                    Owner.Send(msg);
            foreach (IMapObj obj in Roles(MapObjectType.Player))
            {
                if (obj.ObjType == MapObjectType.Player)
                {
                    var player = (Player)obj;
                    if (!effect || (effect && player.ShowGemEffects))
                        obj.Send(msg);
                }
                else
                    obj.Send(msg);
            }
        }
        public unsafe void SendView(byte[] msg, bool me)
        {

            if (me)
                Owner.Send(msg);
            foreach (IMapObj obj in Roles(MapObjectType.Player))
            {
                (obj as Role.Player).Owner.Send(msg);
            }
        }
        public IEnumerable<IMapObj> Roles(MapObjectType typ, Predicate<bool> P = null)
        {
            if (Owner.Map != null)
            {
                if (P != null)
                    return Owner.Map.View.Roles(typ, Owner.Player.X, Owner.Player.Y, p => CanSee(p) && P(p.Alive));
                else
                    return Owner.Map.View.Roles(typ, Owner.Player.X, Owner.Player.Y, p => CanSee(p));
            }
            else
                return new IMapObj[0];
        }

        public bool TryGetValue(uint UID, out IMapObj obj, MapObjectType typ)
        {
            if (Owner.Map != null)
                return Owner.Map.View.TryGetObject<IMapObj>(UID, typ, Owner.Player.X, Owner.Player.Y, out obj);
            obj = null;
            return false;
        }
        public bool CanSee(IMapObj obj)
        {
            try
            {
                if (obj == null)
                    return false;
                if (obj.Map != Owner.Player.Map)
                    return false;
                if (!obj.AllowDynamic)
                    if (obj.DynamicID != Owner.Player.DynamicID)
                        return false;
                if (obj.UID == Owner.Player.UID)
                    return false;

                return Core.GetDistance(obj.X, obj.Y, Owner.Player.X, Owner.Player.Y) <= ViewThreshold;
            }
            catch (Exception e)
            {
                Console.WriteException(e);
                return false;
            }
        }
        public bool Contains(IMapObj obj)
        {
            if (obj.UID == Owner.Player.UID)
                return true;
            if (Owner.Map != null)
                return Owner.Map.View.Contain(obj.UID, Owner.Player.X, Owner.Player.Y);
            return false;
        }

        public bool ContainMobInScreen(string name)
        {
            bool contain = false;
            foreach (var obj in Roles(MapObjectType.Monster))
            {
                var monster = obj as Game.MsgMonster.MonsterRole;
                if (monster.Name == name)
                {
                    contain = true;
                    break;
                }
            }
            return contain;
        }
        public unsafe bool CanAdd(IMapObj obj, bool Force, ServerSockets.Packet stream)
        {
            try
            {
                if (!CanSee(obj))
                    return false;
                if (Owner.Player.InView(obj.X, obj.Y, ViewThreshold) || Force)
                {
                    try
                    {
                        if (obj.ObjType == MapObjectType.Monster)
                        {
                            if (!obj.Alive)
                            {
                                var monster = obj as Game.MsgMonster.MonsterRole;
                                if (monster.CanRespawn(Owner.Map))
                                    monster.Respawn(false);
                            }
                            else
                                Owner.Send(obj.GetArray(stream, false));
                        }
                        else if (obj.ObjType == MapObjectType.Player)
                        {
                            if (Owner.Player.Map == 700)
                            {
                                var pClient = (obj as Role.Player).Owner;
                                if (Owner.InQualifier() && pClient.IsWatching())
                                    return true;
                                if (pClient.IsWatching())
                                    return true;
                            }
                            var apClient = (obj as Role.Player).Owner;
                            //apClient.EffectStatus.Reload(Owner);
                            if (Owner.Player.Invisible == false && apClient.Player.Invisible == true)
                                return true;
                            if (apClient.Player.Invisible)
                                return true;
                            Owner.Send(obj.GetArray(stream, false));
                        }
                        else if (obj.ObjType == MapObjectType.Item)
                        {
                            if (obj.Alive == false)
                            {
                                var item = obj as Game.MsgFloorItem.MsgItem;
                                item.SendAll(stream, Game.MsgFloorItem.MsgDropID.Remove);
                                Owner.Map.View.LeaveMap<IMapObj>(obj);
                            }
                            else
                            {
                                Owner.Send(obj.GetArray(stream, false));
                            }
                        }
                        else if (obj.ObjType == MapObjectType.Npc)
                        {
                            if (obj.Map == 1015 && obj.UID == (uint)Game.MsgNpc.NpcID.LittleBen)
                            {
                                if (Owner.Player.QuestGUI.CheckQuest(6129, MsgQuestList.QuestListItem.QuestStatus.Finished))
                                    return false;
                            }
                            Owner.Send(obj.GetArray(stream, false));
                        }
                        else if (obj.ObjType == MapObjectType.SobNpc || obj.ObjType == MapObjectType.StaticRole)
                            Owner.Send(obj.GetArray(stream, false));
                    }
                    catch (Exception e) { Console.WriteException(e); }
                    return true;
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteException(e);
                return false;
            }
        }
        public unsafe void Role(bool clear = false, ServerSockets.Packet msg = null)
        {
            try
            {
                if (Owner.Player == null || Owner.Map == null)
                    return;
                if (clear)
                {
                    Owner.Player.Px = 0;
                    Owner.Player.Py = 0;
                }

                using (var rec = new ServerSockets.RecycledPacket())
                {
                    var stream = rec.GetStream();
                    try
                    {
                        if (Database.HouseTable.InHouse(Owner.Player.Map))
                        {
                            if (Owner.Player.UID == Owner.Player.DynamicID)
                            {
                                if (Owner.MyHouse != null)
                                {
                                    foreach (var npc in Owner.MyHouse.Furnitures.Values)
                                        npc.Send(stream);
                                }
                            }
                            else
                            {
                                COServer.Role.Instance.House House;
                                if (COServer.Role.Instance.House.HousePoll.TryGetValue(Owner.Player.DynamicID, out House))
                                {
                                    try
                                    {
                                        foreach (var npc in House.Furnitures.Values)
                                        {
                                            try
                                            {
                                                npc.Send(stream);
                                            }
                                            catch (Exception e)
                                            {
                                                Console.WriteException(e);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteException(e);
                                    }
                                }
                            }
                        }
                        var Map = Owner.Map;

                        foreach (var m_client in Owner.Map.View.Roles(MapObjectType.Player, Owner.Player.X, Owner.Player.Y, null))// && (p as Role.Player).View.CanAdd(Owner.Player,clear, stream)))
                        {
                            if (m_client == null)
                                continue;
                            try
                            {

                                if (CanAdd(m_client, clear, stream) && (m_client as Role.Player).View.CanAdd(Owner.Player, true, stream))
                                {

                                    var client = (m_client as Role.Player).Owner;
                                    try
                                    {
                                        if (client.Socket != null)
                                        {
                                            if (client.Socket.Alive == false)
                                            {
                                                Owner.Map.Denquer(client);
                                                ActionQuery action;

                                                action = new ActionQuery()
                                                {
                                                    ObjId = client.Player.UID,
                                                    Type = ActionType.RemoveEntity
                                                };
                                                SendView(stream.ActionCreate(&action), false);
                                                continue;
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Console.SaveException(e);
                                    }
                                    if (msg != null)
                                        client.Send(msg);

                                    client.Player.SendScrennXPSkill(Owner.Player);
                                    Owner.Player.SendScrennXPSkill(client.Player);
                                    if (client.Player.BlackSpot)
                                    {
                                        Owner.Send(stream.BlackspotCreate(true, client.Player.UID));
                                    }
                                    if (Owner.Player.BlackSpot)
                                    {
                                        client.Send(stream.BlackspotCreate(true, Owner.Player.UID));
                                    }
                                    if (client.Player.OnInteractionEffect)
                                    {
                                        client.Player.InteractionEffect.X = client.Player.X;
                                        client.Player.InteractionEffect.Y = client.Player.Y;


                                        InteractQuery action = InteractQuery.ShallowCopy(client.Player.InteractionEffect);

                                        Owner.Send(stream.InteractionCreate(&action));

                                    }
                                    if (Owner.Player.OnInteractionEffect)
                                    {
                                        Owner.Player.InteractionEffect.X = Owner.Player.X;
                                        Owner.Player.InteractionEffect.Y = Owner.Player.Y;


                                        InteractQuery action = InteractQuery.ShallowCopy(Owner.Player.InteractionEffect);
                                        client.Send(stream.InteractionCreate(&action));

                                    }
                                    if (Owner.IsVendor)
                                    {
                                        if (Owner.MyVendor.HalkMeesaje != null)
                                            client.Send(Owner.MyVendor.HalkMeesaje.GetArray(stream));
                                    }
                                    if (client.IsVendor)
                                    {
                                        if (client.MyVendor.HalkMeesaje != null)
                                            Owner.Send(client.MyVendor.HalkMeesaje.GetArray(stream));
                                    }
                                    if (Owner.Player.OnFairy != false)
                                    {
                                        client.Send(stream.TransformFairyCreate(Owner.Player.FairySpawn.Mode, Owner.Player.FairySpawn.FairyType, Owner.Player.FairySpawn.UID));
                                    }
                                    if (client.Player.OnFairy != false)
                                    {
                                        Owner.Send(stream.TransformFairyCreate(client.Player.FairySpawn.Mode, client.Player.FairySpawn.FairyType, client.Player.FairySpawn.UID));
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteException(e);
                            }
                        }
                        foreach (var npc in Owner.Map.View.Roles(MapObjectType.Npc, Owner.Player.X, Owner.Player.Y, p => CanAdd(p, clear, stream)))
                        {
                            //npc.Send(stream, Owner);
                        }
                        foreach (var npc in Owner.Map.View.Roles(MapObjectType.StaticRole, Owner.Player.X, Owner.Player.Y, p => CanAdd(p, clear, stream)))
                        {
                            //npc.Send(stream, Owner);
                        }

                        foreach (var mob in Owner.Map.View.Roles(MapObjectType.Monster, Owner.Player.X, Owner.Player.Y
                            , p => CanAdd(p, clear, stream)))
                        {
                            if (mob == null)
                                continue;
                            var Monster = mob as Game.MsgMonster.MonsterRole;
                            if (Monster.HitPoints > ushort.MaxValue || Monster.Boss == 1)
                            {
                                Game.MsgServer.MsgUpdate Upd = new Game.MsgServer.MsgUpdate(stream, Monster.UID, 2);
                                stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.MaxHitpoints, Monster.Family.MaxHealth);
                                stream = Upd.Append(stream, Game.MsgServer.MsgUpdate.DataType.Hitpoints, Monster.HitPoints);
                                stream = Upd.GetArray(stream);
                                Owner.Send(stream);
                                Monster.SendScores(stream);
                            }
                            if (Monster.BlackSpot)
                            {
                                Owner.Send(stream.BlackspotCreate(true, Monster.UID));
                            }
                        }
                        foreach (var subnpc in Owner.Map.View.Roles(MapObjectType.SobNpc, Owner.Player.X, Owner.Player.Y, p => CanAdd(p, clear, stream)))
                        {
                            if (subnpc == null)
                                continue;
                            var SobMobNpcs = subnpc as Role.SobNpc;
                            if (SobMobNpcs.BitVector.ArrayFlags.Count != 0)
                            {
                                Game.MsgServer.MsgUpdate upd = new Game.MsgServer.MsgUpdate(stream, subnpc.UID, 1);
                                stream = upd.Append(stream, MsgUpdate.DataType.StatusFlag, SobMobNpcs.BitVector.bits);
                                stream = upd.GetArray(stream);
                                Owner.Send(stream);
                            }

                        }
                        foreach (var item in Owner.Map.View.Roles(MapObjectType.Item, Owner.Player.X, Owner.Player.Y, p => CanAdd(p, clear, stream)))
                        {
                            var PItem = item as Game.MsgFloorItem.MsgItem;
                            PItem.Send(stream, Owner.Player);
                        }
                        try
                        {
                            if (Owner.Player.Map == 1858 || Owner.Player.Map == 1860)
                            {
                                foreach (var T in Poker.Database.Tables.Values)
                                {
                                    if (T.MapId == Owner.Player.Map)
                                    {
                                        if (T.OnScreen == null)
                                        {
                                            T.OnScreen = new ConcurrentDictionary<uint, uint>();
                                        }
                                        if (Owner.Player.InView(T.X, T.Y, 18) && !T.OnScreen.ContainsKey(Owner.Player.UID))
                                        {
                                            Owner.Send(T.ToArray());
                                            T.OnScreen.TryAdd(Owner.Player.UID, Owner.Player.UID);
                                        }
                                        else if (T.OnScreen.ContainsKey(Owner.Player.UID))
                                        {
                                            uint x = 0;
                                            T.OnScreen.TryRemove(Owner.Player.UID, out x);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error from Poker Spawn " + e.ToString());
                        }

                    }
                    catch (Exception e)
                    {
                        Console.WriteException(e);
                    }
                }
            }
            catch (Exception e) { Console.SaveException(e); }
        }
        public unsafe void Clear(ServerSockets.Packet stream)
        {
            Owner.Player.Px = 0;
            Owner.Player.Py = 0;

            ActionQuery action;

            action = new ActionQuery()
            {
                ObjId = Owner.Player.UID,
                Type = ActionType.RemoveEntity
            };
            SendView(stream.ActionCreate(&action), false);
        }
    }
}
