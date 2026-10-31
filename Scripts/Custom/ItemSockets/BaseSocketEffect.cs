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

        // 🔥 НАШЕ ДОБАВЛЕНИЕ: МЕТОДЫ ДИНАМИЧЕСКИХ БОНУСОВ 🎉⚙️
        public virtual void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            // По умолчанию, если в конкретном камне нет формулы, используется стандартный эффект x1.0
            ApplyEffect(weapon);
        }

        public virtual void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            RemoveEffect(weapon);
        }
    }
}
