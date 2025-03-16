using System;
using COServer.Game.MsgServer;
using COServer.Game.MsgFloorItem;

namespace COServer.Game.MsgMonster
{
    public class PoolProcesses
    {
        public unsafe static void BuffersCallback(Client.GameClient client, Extensions.Time32 clock)
        {
            try
            {
                if (client.Fake)
                    return;
                Extensions.Time32 timer = Extensions.Time32.Now;
                var Array = client.Player.View.Roles(Role.MapObjectType.Monster);
                foreach (var map_mob in Array)
                {

                    var Mob = (map_mob as MonsterRole);
                    if (Mob.BlackSpot)
                    {
                        if (timer > Mob.Stamp_BlackSpot)
                        {
                            Mob.BlackSpot = false;

                            using (var rec = new ServerSockets.RecycledPacket())
                            {
                                var stream = rec.GetStream();

                                Mob.Send(stream.BlackspotCreate(false, Mob.UID));
                            }

                        }
                    }
                    foreach (var flag in Mob.BitVector.GetFlags())
                    {

                        if (flag.Expire(timer))
                        {
                            Mob.RemoveFlag((MsgServer.MsgUpdate.Flags)flag.Key, client.Map);
                        }
                        else if (flag.Key == (int)Game.MsgServer.MsgUpdate.Flags.Poisoned)
                        {
                            if (flag.CheckInvoke(timer))
                            {
                                uint damage = Game.MsgServer.AttackHandler.Calculate.Base.CalculatePoisonDamage(Mob.HitPoints, Mob.PoisonLevel);
                                if (Mob.Boss != 1)
                                {
                                    if (Mob.HitPoints == 1)
                                    {
                                        damage = 0;
                                        goto jump;
                                    }

                                    Mob.HitPoints = (uint)Math.Max(1, (int)(Mob.HitPoints - damage));

                                jump:

                                    using (var rec = new ServerSockets.RecycledPacket())
                                    {
                                        var stream = rec.GetStream();

                                        InteractQuery action = new InteractQuery()
                                        {
                                            Damage = (int)damage,
                                            AtkType = MsgAttackPacket.AttackID.Physical,
                                            X = Mob.X,
                                            Y = Mob.Y,
                                            OpponentUID = Mob.UID
                                        };

                                        Mob.Send(stream.InteractionCreate(&action));

                                    }
                                }
                                else
                                    Mob.RemoveFlag((MsgServer.MsgUpdate.Flags)flag.Key, client.Map);

                            }
                        }
                    }
                }



            }
            catch (Exception e)
            {
                Console.WriteException(e);
            }

        }
        public static void GuardsCallback(Client.GameClient client, Extensions.Time32 clock)
        {
            try
            {
                if (client.Fake)
                    return;

                if (client.Map == null)
                    return;
                Extensions.Time32 timer = Extensions.Time32.Now;

                var Array = client.Player.View.Roles(Role.MapObjectType.Monster);
                foreach (var map_mob in Array)
                {

                    var Guard = (map_mob as MonsterRole);
                    if ((Guard.Family.Settings & MonsterSettings.Guard) == MonsterSettings.Guard)
                    {
                        if (timer > Guard.AttackSpeed.AddMilliseconds(Guard.Family.AttackSpeed + 1000))
                        {
                            client.Player.View.MobActions.CheckGuardPosition(client.Player.View.GetPlayer(), Guard);
                            if (client.Player.View.MobActions.GuardAttackPlayer(client.Player.View.GetPlayer(), Guard))
                                Guard.AttackSpeed = timer;

                            if (!Guard.Alive)
                            {
                                Guard.AddFadeAway(timer.AllMilliseconds, client.Map);
                                Guard.RemoveView(timer.AllMilliseconds, client.Map);

                            }
                            foreach (var mob in Array)
                            {
                                var monseter = (mob as MonsterRole);
                                if ((monseter.Family.Settings & MonsterSettings.Guard) != MonsterSettings.Guard
                                    && (monseter.Family.Settings & MonsterSettings.Reviver) != MonsterSettings.Reviver
                                    && !monseter.IsFloor)
                                {
                                    if (client.Player.View.MobActions.GuardAttackMonster(client.Map, monseter, Guard))
                                        break;
                                }
                            }
                        }
                    }
                }



            }
            catch (Exception e)
            {
                Console.WriteException(e);
            }

        }
        public static void AliveMonstersCallback(Client.GameClient client, Extensions.Time32 clock)
        {
            try
            {

                if (client.Fake)
                    return;
                if (client.Map == null)
                    return;
                if (client == null)
                    return;

                Extensions.Time32 timer = Extensions.Time32.Now;
                var Array = client.Player.View.Roles(Role.MapObjectType.Monster);

                foreach (var map_mob in Array)
                {

                    var monster = (map_mob as MonsterRole);
                    if (!map_mob.Alive)
                    {
                        if (monster.State == Game.MsgMonster.MobStatus.Respawning)
                        {
                            if (MonsterRole.SpecialMonsters.Contains(monster.Family.ID))
                                continue;
                            if (timer > monster.RespawnStamp)
                            {
                                if (!client.Map.MonsterOnTile(monster.RespawnX, monster.RespawnY))
                                {
                                    monster.Respawn();
                                    client.Map.SetMonsterOnTile(monster.X, monster.Y, true);
                                }
                            }
                        }
                    }
                    if ((monster.Family.Settings & MonsterSettings.Guard) != MonsterSettings.Guard
                        && (monster.Family.Settings & MonsterSettings.Reviver) != MonsterSettings.Reviver
                        && (monster.Family.Settings & MonsterSettings.Lottus) != MonsterSettings.Lottus)
                    {
                        var Mob = map_mob as MonsterRole;
                        if (Mob.Family.ID == 20211)
                            continue;
                        client.Player.View.MobActions.ExecuteAction(client.Player, Mob);
                        if (!Mob.Alive)
                        {
                            var now = Extensions.Time32.Now;
                            Mob.AddFadeAway(now.AllMilliseconds, client.Map);
                            Mob.RemoveView(now.AllMilliseconds, client.Map);

                        }
                    }
                }

            }

            catch (Exception e)
            {
                Console.WriteException(e);
            }

        }
        public static void ReviversCallback(Client.GameClient client, Extensions.Time32 clock)
        {
            try
            {

                if (client.Fake)
                    return;
                if (client.Map == null)
                    return;

                var Array = client.Player.View.Roles(Role.MapObjectType.Monster);
                foreach (var map_mob in Array)
                {
                    var monseter = (map_mob as MonsterRole);
                    if ((monseter.Family.Settings & MonsterSettings.Reviver) == MonsterSettings.Reviver)
                    {
                        if (!monseter.Alive)
                        {
                            var now = Extensions.Time32.Now.AllMilliseconds;
                            monseter.AddFadeAway(now, client.Map);
                            monseter.RemoveView(now, client.Map);
                        }
                        if (Role.Core.GetDistance(map_mob.X, map_mob.Y, client.Player.View.GetPlayer().X, client.Player.View.GetPlayer().Y) < 13)
                        {
                            if (!client.Player.View.GetPlayer().Alive)
                            {
                                using (var rec = new ServerSockets.RecycledPacket())
                                {
                                    var stream = rec.GetStream();
                                    client.Player.View.GetPlayer().Revive(stream);
                                }
                                using (var rec = new ServerSockets.RecycledPacket())
                                {
                                    var stream = rec.GetStream();

                                    MsgServer.MsgSpellAnimation SpellPacket = new MsgServer.MsgSpellAnimation(map_mob.UID
                                    , 0, map_mob.X, map_mob.Y, (ushort)Role.Flags.SpellID.Pray, 0, 0);
                                    SpellPacket.Targets.Enqueue(new MsgServer.MsgSpellAnimation.SpellObj(client.Player.View.GetPlayer().UID, 0, MsgServer.MsgAttackPacket.AttackEffect.None));
                                    SpellPacket.SetStream(stream);
                                    SpellPacket.Send(map_mob as Game.MsgMonster.MonsterRole);
                                }
                            }
                        }
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
