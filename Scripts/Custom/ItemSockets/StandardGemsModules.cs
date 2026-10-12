using System;
using Server;
using Server.Items;

namespace Server.Custom.ItemSockets
{
    // 🔴 МОДУЛЬ РУБИНА: Урон
    public class GemEffectRuby : BaseSocketEffect
    {
        public override string UniqueID => "ruby_di";
        public override string DisplayName => "Ограненный Рубин Урона (+15% DI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1157;

        public override void ApplyEffect(BaseWeapon weapon)
        {
            weapon.Attributes.WeaponDamage += 15;
        }

        public override void RemoveEffect(BaseWeapon weapon)
        {
            weapon.Attributes.WeaponDamage -= 15;
        }
    }

    // 🔵 МОДУЛЬ САПФИРА: Скорость атаки
    public class GemEffectSapphire : BaseSocketEffect
    {
        public override string UniqueID => "sapphire_ssi";
        public override string DisplayName => "Ограненный Сапфир Скорости (+5% SSI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1154;

        public override void ApplyEffect(BaseWeapon weapon)
        {
            weapon.Attributes.WeaponSpeed += 5;
        }

        public override void RemoveEffect(BaseWeapon weapon)
        {
            weapon.Attributes.WeaponSpeed -= 5;
        }
    }

    // 🟢 МОДУЛЬ ИЗУМРУДА: Шанс попадания
    public class GemEffectEmerald : BaseSocketEffect
    {
        public override string UniqueID => "emerald_hci";
        public override string DisplayName => "Ограненный Изумруд Точности (+5% HCI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1168;

        public override void ApplyEffect(BaseWeapon weapon)
        {
            weapon.Attributes.AttackChance += 5;
        }

        public override void RemoveEffect(BaseWeapon weapon)
        {
            weapon.Attributes.AttackChance -= 5;
        }
    }

    // ⚪ МОДУЛЬ АЛМАЗА: Шанс защиты
    public class GemEffectDiamond : BaseSocketEffect
    {
        public override string UniqueID => "diamond_dci";
        public override string DisplayName => "Ограненный Алмаз Защиты (+5% DCI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1150;

        public override void ApplyEffect(BaseWeapon weapon)
        {
            weapon.Attributes.DefendChance += 5;
        }

        public override void RemoveEffect(BaseWeapon weapon)
        {
            weapon.Attributes.DefendChance -= 5;
        }
    }
}
