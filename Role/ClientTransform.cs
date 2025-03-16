﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using COServer.Game.MsgServer;

namespace COServer.Role
{
    public class ClientTransform
    {
        public uint HitPoints = 0;
        public ushort ID = 0;
        public Extensions.Time32 Stamp = new Extensions.Time32();
        public ushort SpellID = 0;
        public ushort Level = 0;
        private Role.Player Owner;

        public ClientTransform(Role.Player  _Owner)
        {
            Owner = _Owner;
        }

        public bool CheckUp(Extensions.Time32 CurentTime)
        {
            if (CurentTime > Stamp)
            {
                FinishTransform();
                return true;
            }
            return false;
        }
        public unsafe void FinishTransform()
        {
            Owner.TransformationID = 0;
            Owner.Owner.Equipment.QueryEquipment(Owner.Owner.Equipment.Alternante);
           


            using (var rec = new ServerSockets.RecycledPacket())
            {
                var stream = rec.GetStream();
                Owner.View.SendView(Owner.GetArray(stream, false), false);
                 ActionQuery action = new ActionQuery()
            {
                Type = ActionType.EndTransformation,
                ObjId = Owner.UID
            };
                 Owner.Send(stream.ActionCreate(&action));
                /*if (SpellID != 0)
                {
                    Game.MsgServer.MsgSpellAnimation Spell = new Game.MsgServer.MsgSpellAnimation
                   (Owner.UID, 0, Owner.X, Owner.Y, SpellID, Level, 0);
                    Spell.Targets.Enqueue(new Game.MsgServer.MsgSpellAnimation.SpellObj()
                    {
                        Damage = (uint)0,
                        Hit = 1,
                        UID = Owner.UID
                    });
                    Spell.SetStream(stream);
                    Spell.Send(Owner.Owner);
                }*/
            }
        }
        public void UpdateStatus()
        {

            Owner.HitPoints = (int)HitPoints;
            Owner.Owner.Status.MaxHitpoints = HitPoints;
            Owner.SendUpdateHP();
        }
        public void CreateTransform(ServerSockets.Packet stream, uint hp, ushort _ID, int Seconds, ushort _SpellID = 0, byte _level = 0)
        {
            HitPoints = hp;
            ID = _ID;
            Stamp = Extensions.Time32.Now.AddSeconds(Seconds);
            Owner.TransformationID = ID;
            SpellID = _SpellID;
            Level = _level;
            if (SpellID != 0)
            {
                Game.MsgServer.MsgSpellAnimation Spell = new Game.MsgServer.MsgSpellAnimation
                (Owner.UID, 0, Owner.X, Owner.Y, SpellID, Level, 0);
                Spell.Targets.Enqueue(new Game.MsgServer.MsgSpellAnimation.SpellObj()
                {
                    Damage = (uint)Seconds,
                    Hit = 0,
                    UID = Owner.UID
                });
                Spell.SetStream(stream);
                Spell.Send(Owner.Owner);

            }

            Owner.View.SendView(Owner.GetArray(stream, false), false);

            UpdateStatus();
        }
    }
}
