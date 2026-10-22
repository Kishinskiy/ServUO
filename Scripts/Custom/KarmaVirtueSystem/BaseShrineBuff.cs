using System;
using Server;

namespace Server.Custom.KarmaVirtueSystem
{
    public abstract class BaseShrineBuff
    {
        public abstract string BuffName { get; }
        public abstract int IconGumpID { get; }
        public abstract TimeSpan Duration { get; }

        public virtual void OnApply(Mobile from) { }
        public virtual void OnRemove(Mobile from) { }
    }
}