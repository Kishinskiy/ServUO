using System;
using Server;
using Server.Items;

namespace Server.Custom.ItemSockets
{
    // 🦇 КАСТОМНЫЙ МОДУЛЬ: Осколок Вампиризма
    public class GemEffectVampiric : BaseSocketEffect
    {
        public override string UniqueID => "vamp_shard";
        public override string DisplayName => "Мистический Осколок Вампиризма (20% Hit Leech)";
        public override int ItemID => 0x1EA7; // Графика кристалла
        public override int Hue => 1175;     // Кроваво-красный цвет

        public override void ApplyEffect(BaseWeapon weapon)
        {
            if (weapon != null && weapon.WeaponAttributes != null)
            {
                // 🔥 ИСПРАВЛЕНО: Перенаправлено на нативное свойство WeaponAttributes ядра ServUO! 🎉⚙️🛠️
                weapon.WeaponAttributes.HitLeechHits += 20;
            }
        }

        public override void RemoveEffect(BaseWeapon weapon)
        {
            if (weapon != null && weapon.WeaponAttributes != null)
            {
                weapon.WeaponAttributes.HitLeechHits -= 20;
            }
        }
    }
}
