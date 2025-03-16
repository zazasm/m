﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COServer.Role
{
    public class StatusFlagsBigVector32 : Extensions.BitVector32
    {
        public const int PermanentFlag = 60 * 60 * 24 * 30;

        public class Flag
        {
            public int Seconds = 0;
            public int InvokerSecouds = 0;
            public int Key;
            public Extensions.Time32 TimerInvoker = new Extensions.Time32();
            public Extensions.Time32 Timer = new Extensions.Time32();
            public bool RemoveOnDead = false;
            public Flag(int _flag, int _Seconds, bool _removeondead, int _InvokerSecouds)
            {
                Seconds = _Seconds;
                Key = _flag;
                Timer = Extensions.Time32.Now;
                RemoveOnDead = _removeondead;
                InvokerSecouds = _InvokerSecouds;
                if (InvokerSecouds != 0)
                    TimerInvoker = Extensions.Time32.Now;
            }

            public bool Expire(Extensions.Time32 Now)
            {
                if (Now >= Timer.AddSeconds(Seconds))
                    return true;
                return false;
            }
            public bool CheckInvoke(Extensions.Time32 Now)
            {
                if (InvokerSecouds == 0)
                    return true;
                else
                {
                    if (Now >= TimerInvoker.AddSeconds(InvokerSecouds))
                    {
                        TimerInvoker = Extensions.Time32.Now;
                        return true;
                    }
                    return false;
                }
            }
        }

        public System.Collections.Concurrent.ConcurrentDictionary<int, Flag> ArrayFlags;
        private Flag[] Array = new Flag[0];
        private bool Update = false;

        public StatusFlagsBigVector32(int Size)
            : base(Size)
        {
            ArrayFlags = new System.Collections.Concurrent.ConcurrentDictionary<int, Flag>();
        }

        public bool TryAdd(int flag, int Seconds, bool RemoveOnDead, int InvokerSecouds)
        {
            if (!ArrayFlags.ContainsKey(flag))
            {
                ArrayFlags.TryAdd(flag, new Flag(flag, Seconds, RemoveOnDead, InvokerSecouds));
                Add(flag);
                
                Update = true;

                return true;
            }
            return false;
        }
        public bool UpdateFlag(int flag, int Seconds, bool SetNewTimer, int MaxSeconds)
        {
            Flag FlagClass;
            if (ArrayFlags.TryGetValue(flag, out FlagClass))
            {
                if (SetNewTimer)
                {
                    FlagClass.Timer = Extensions.Time32.Now;
                    FlagClass.Seconds = Seconds;
                }
                else
                {
                    if (FlagClass.Timer.AddSeconds(FlagClass.Seconds) > Extensions.Time32.Now.AddSeconds(MaxSeconds))
                    {
                        FlagClass.Timer = Extensions.Time32.Now;
                        FlagClass.Seconds = MaxSeconds;
                    }
                    else
                        FlagClass.Seconds += Seconds;

                }
                return true;
            }
            return false;
        }
        public bool TryRemove(int flag)
        {
            Flag FlagClass;
            if (ArrayFlags.TryRemove(flag, out FlagClass))
            {
                Remove(flag);
                
                Update = true;

                return true;
            }
            return false;
        }
        public bool InLife(int flag, Extensions.Time32 Now64)
        {
            Flag FlagClass;
            if (ArrayFlags.TryGetValue(flag, out FlagClass))
            {
                return FlagClass.Expire(Now64);
            }
            return false;
        }
        public bool CheckInvoke(int flag, Extensions.Time32 Now64)
        {
            Flag FlagClass;
            if (ArrayFlags.TryGetValue(flag, out FlagClass))
            {
                return FlagClass.CheckInvoke(Now64);
            }
            return false;
        }
        public bool ContainFlag(int flag)
        {
            return ArrayFlags.ContainsKey(flag);
        }
        public void GetClear()
        {
            List<int> remove = new List<int>();

            foreach (var item in GetFlags())
            {
                if (item.RemoveOnDead)
                    remove.Add(item.Key);
            }
            foreach (var item in remove)
                TryRemove(item);
        }
        public Flag[] GetFlags()
        {
            try
            {
                if (Update)
                {
                    Array = ArrayFlags.Values.ToArray();
                    Update = false;
                }
                return Array;
            }
            catch (Exception e)
            {
                Console.WriteException(e);
                return new Flag[0];
            }
          
        }
    }
}
