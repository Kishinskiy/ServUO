using System;
using Server;
using Server.Items;

namespace Server.Custom.ItemSockets
{
    // =========================================================================
    // 🔴 1. РУБИН: Бонус к урону оружия (DI)
    // =========================================================================
    public class GemEffectRuby : BaseSocketEffect
    {
        public override string UniqueID => "ruby_di";
        public override string DisplayName => "Рубин Unbena (+15% DI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1157;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(15.0 * skillScale);
            weapon.Attributes.WeaponDamage += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(15.0 * skillScale);
            weapon.Attributes.WeaponDamage -= finalBonus;
        }
    }

    // =========================================================================
    // 🔵 2. САПФИР: Скорость атаки (SSI)
    // =========================================================================
    public class GemEffectSapphire : BaseSocketEffect
    {
        public override string UniqueID => "sapphire_ssi";
        public override string DisplayName => "Сапфир Скорости (+5% SSI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1144;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.WeaponSpeed += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.WeaponSpeed -= finalBonus;
        }
    }

    // =========================================================================
    // 🟢 3. ИЗУМРУД: Шанс попадания (HCI)
    // =========================================================================
    public class GemEffectEmerald : BaseSocketEffect
    {
        public override string UniqueID => "emerald_hci";
        public override string DisplayName => "Изумруд Точности (+5% HCI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1168;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.AttackChance += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.AttackChance -= finalBonus;
        }
    }

    // =========================================================================
    // ⚪ 4. АЛМАЗ: Шанс защиты (DCI)
    // =========================================================================
    public class GemEffectDiamond : BaseSocketEffect
    {
        public override string UniqueID => "diamond_dci";
        public override string DisplayName => "Алмаз Защиты (+5% DCI)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1150;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.DefendChance += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.DefendChance -= finalBonus;
        }
    }

    // =========================================================================
    // 🟣 5. АМЕТИСТ: Снижение расхода маны (LMC)
    // =========================================================================
    public class GemEffectAmethyst : BaseSocketEffect
    {
        public override string UniqueID => "amethyst_lmc";
        public override string DisplayName => "Аметист Экономии (-5% LMC)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1162;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.LowerManaCost += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.LowerManaCost -= finalBonus;
        }
    }

    // =========================================================================
    // 🟤 6. ТУРМАЛИН: Регенерация маны (MR)
    // =========================================================================
    public class GemEffectTourmaline : BaseSocketEffect
    {
        public override string UniqueID => "tourmaline_mr";
        public override string DisplayName => "Турмалин Потока (+3 Mana Regen)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1190;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(3.0 * skillScale);
            weapon.Attributes.RegenMana += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(3.0 * skillScale);
            weapon.Attributes.RegenMana -= finalBonus;
        }
    }

    // =========================================================================
    // 🟠 7. ЦИТРИН: Регенерация выносливости (Stamina Regen)
    // =========================================================================
    public class GemEffectCitrine : BaseSocketEffect
    {
        public override string UniqueID => "citrine_regen";
        public override string DisplayName => "Цитрин Выносливости (+5 Stamina Regen)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1166;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.RegenStam += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(5.0 * skillScale);
            weapon.Attributes.RegenStam -= finalBonus;
        }
    }

    // =========================================================================
    // 🟡 8. ЯНТАРЬ: Снижение расхода реагентов (LRC)
    // =========================================================================
    public class GemEffectAmber : BaseSocketEffect
    {
        public override string UniqueID => "amber_lrc";
        public override string DisplayName => "Янтарь Снабжения (+10% LRC)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1170;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);

        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(10.0 * skillScale);
            weapon.Attributes.LowerRegCost += finalBonus;
        }

        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            int finalBonus = (int)Math.Round(10.0 * skillScale);
            weapon.Attributes.LowerRegCost -= finalBonus;
        }
    }

    // =========================================================================
    // ✨ 9. ГОЛУБОЙ АЛМАЗ: Быстрое чтение заклинаний (+1 FC)
    // =========================================================================
    public class GemEffectBlueDiamond : BaseSocketEffect
    {
        public override string UniqueID => "blue_diamond_fc";
        public override string DisplayName => "Голубой Алмаз Потока (+1 Faster Casting)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1266;

        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);
        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            if (skillScale >= 1.0)
                weapon.Attributes.CastSpeed += 1;
        }
        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            if (skillScale >= 1.0)
                weapon.Attributes.CastSpeed -= 1;
        }
    }
    // =========================================================================
    // 🦇 10. ОСКОЛОК ВАМПИРИЗМА: Поглощение здоровья (Hit Leech)
    // =========================================================================
    public class GemEffectVampiric : BaseSocketEffect
    {
        public override string UniqueID => "vamp_shard";
        public override string DisplayName => "Мистический Осколок Вампиризма (20% Hit Leech)";
        public override int ItemID => 0x1EA7;
        public override int Hue => 1175;
        public override void ApplyEffect(BaseWeapon weapon) => ApplyDynamicEffect(weapon, 1.0);
        public override void RemoveEffect(BaseWeapon weapon) => RemoveDynamicEffect(weapon, 1.0);
        public override void ApplyDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            if (weapon != null && weapon.WeaponAttributes != null)
            {
                int finalLeech = (int)Math.Round(20.0 * skillScale);
                weapon.WeaponAttributes.HitLeechHits += finalLeech;
            }
        }
        public override void RemoveDynamicEffect(BaseWeapon weapon, double skillScale)
        {
            if (weapon != null && weapon.WeaponAttributes != null)
            {
                int finalLeech = (int)Math.Round(20.0 * skillScale);
                weapon.WeaponAttributes.HitLeechHits -= finalLeech;
            }
        }
    }
}