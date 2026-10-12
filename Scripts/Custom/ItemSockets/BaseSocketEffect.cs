using System;
using Server;
using Server.Items;

namespace Server.Custom.ItemSockets
{
    public abstract class BaseSocketEffect
    {
        public abstract string UniqueID { get; }
        public abstract string DisplayName { get; }
        public abstract int ItemID { get; } 
        public abstract int Hue { get; }    

        public abstract void ApplyEffect(BaseWeapon weapon);
        public abstract void RemoveEffect(BaseWeapon weapon);
    }
}
