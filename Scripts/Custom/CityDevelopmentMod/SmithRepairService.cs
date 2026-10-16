using System;
using System.Collections.Generic;
using Server;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.CityDevelopment
{
    public static class SmithRepairService
    {
        private const int RepairCostPerMissingHitPoint = 25;
        private const int MinimumRepairCost = 100;
        private const int MaximumRepairCost = 25000;

        public static void AddRepairEntry(Mobile smith, Mobile player, List<ContextMenuEntry> entries)
        {
            if (smith != null && player != null && player.Alive && smith.InRange(player, 4))
                entries.Add(new RepairEntry(smith));
        }

        private static bool TryGetDurability(Item item, out int hits, out int maxHits)
        {
            if (item is BaseWeapon weapon && weapon.CanRepair)
            {
                hits = weapon.HitPoints;
                maxHits = weapon.MaxHitPoints;
                return true;
            }

            if (item is BaseArmor armor && armor.CanRepair)
            {
                hits = armor.HitPoints;
                maxHits = armor.MaxHitPoints;
                return true;
            }

            if (item is BaseClothing clothing && clothing.CanRepair)
            {
                hits = clothing.HitPoints;
                maxHits = clothing.MaxHitPoints;
                return true;
            }

            hits = 0;
            maxHits = 0;
            return false;
        }

        private static int GetRepairCost(int hits, int maxHits)
        {
            int missing = Math.Max(0, maxHits - hits);
            return Math.Max(MinimumRepairCost, Math.Min(MaximumRepairCost, missing * RepairCostPerMissingHitPoint));
        }

        private static bool CanUseItem(Mobile player, Item item, Mobile smith)
        {
            return player != null && item != null && !item.Deleted &&
                   smith != null && !smith.Deleted && player.Map == smith.Map &&
                   player.InRange(smith, 4) && item.RootParent == player &&
                   (item.IsChildOf(player.Backpack) || item.Parent == player);
        }

        private static void BeginRepair(Mobile player, Mobile smith)
        {
            if (player == null || smith == null || smith.Deleted || player.Map != smith.Map ||
                !player.InRange(smith, 4))
            {
                player?.SendMessage(0x22, "Подойдите ближе к кузнецу, чтобы заказать ремонт.");
                return;
            }

            player.SendMessage("Выберите в рюкзаке или на себе оружие, броню или одежду для ремонта.");
            player.Target = new RepairTarget(smith);
        }

        private sealed class RepairEntry : ContextMenuEntry
        {
            private readonly Mobile _smith;

            public RepairEntry(Mobile smith) : base(1044276, 4)
            {
                _smith = smith;
            }

            public override void OnClick()
            {
                BeginRepair(Owner?.From, _smith);
            }
        }

        private sealed class RepairTarget : Target
        {
            private readonly Mobile _smith;

            public RepairTarget(Mobile smith) : base(10, false, TargetFlags.None)
            {
                _smith = smith;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!(targeted is Item item) || !CanUseItem(from, item, _smith))
                {
                    from.SendMessage(0x22, "Предмет должен находиться в вашем рюкзаке или быть надет на вас; кузнец должен быть рядом.");
                    return;
                }

                if (!TryGetDurability(item, out int hits, out int maxHits) || maxHits <= 0)
                {
                    from.SendMessage(0x22, "Кузнец ремонтирует только ремонтопригодное оружие, броню и одежду.");
                    return;
                }

                if (hits >= maxHits)
                {
                    from.SendMessage(0x22, "Этот предмет уже полностью исправен.");
                    return;
                }

                int cost = GetRepairCost(hits, maxHits);
                if (!Banker.Withdraw(from, cost))
                {
                    from.SendMessage(0x22, $"Для ремонта нужно {cost} золотых. Оплата списывается из доступных средств.");
                    return;
                }

                RepairItem(item, maxHits);
                from.SendMessage(0x35, $"Кузнец отремонтировал предмет за {cost} золотых.");
            }
        }

        internal static bool IsSupportedItem(Item item)
        {
            return item is BaseWeapon || item is BaseArmor || item is BaseClothing ||
                   item is BaseJewel || item is BaseTalisman;
        }

        internal static bool IsEnchanted(Item item)
        {
            if (item is BaseWeapon weapon)
            {
                return !weapon.Attributes.IsEmpty || !weapon.WeaponAttributes.IsEmpty ||
                       !weapon.AosElementDamages.IsEmpty || !weapon.AbsorptionAttributes.IsEmpty ||
                       !weapon.ExtendedWeaponAttributes.IsEmpty ||
                       weapon.Slayer != SlayerName.None || weapon.Slayer2 != SlayerName.None ||
                       weapon.Slayer3 != TalismanSlayerName.None ||
                       weapon.DamageLevel != WeaponDamageLevel.Regular ||
                       weapon.DurabilityLevel != WeaponDurabilityLevel.Regular;
            }

            if (item is BaseArmor armor)
            {
                return !armor.Attributes.IsEmpty || !armor.ArmorAttributes.IsEmpty ||
                       !armor.SkillBonuses.IsEmpty || !armor.AbsorptionAttributes.IsEmpty ||
                       !armor.WeaponAttributes.IsEmpty || !armor.SetAttributes.IsEmpty ||
                       armor.Protection != null && !armor.Protection.IsEmpty;
            }

            if (item is BaseClothing clothing)
            {
                return !clothing.Attributes.IsEmpty || !clothing.ClothingAttributes.IsEmpty ||
                       !clothing.Resistances.IsEmpty || !clothing.SAAbsorptionAttributes.IsEmpty ||
                       !clothing.WeaponAttributes.IsEmpty;
            }

            if (item is BaseJewel jewel)
            {
                return !jewel.Attributes.IsEmpty || !jewel.Resistances.IsEmpty ||
                       !jewel.SkillBonuses.IsEmpty || !jewel.AbsorptionAttributes.IsEmpty ||
                       !jewel.SetAttributes.IsEmpty ||
                       jewel.Protection != null && !jewel.Protection.IsEmpty;
            }

            return item is BaseTalisman talisman &&
                   (talisman.Slayer != TalismanSlayerName.None ||
                    (talisman.Summoner != null && !talisman.Summoner.IsEmpty) ||
                    talisman.Removal != TalismanRemoval.None ||
                    (talisman.Protection != null && !talisman.Protection.IsEmpty) ||
                    (talisman.Killer != null && !talisman.Killer.IsEmpty) ||
                    talisman.ExceptionalBonus != 0 || talisman.SuccessBonus != 0 ||
                    (talisman.Attributes != null && !talisman.Attributes.IsEmpty) ||
                    (talisman.SkillBonuses != null && !talisman.SkillBonuses.IsEmpty));
        }

        internal static bool TryRepairEnchantedItem(Item item, out string error)
        {
            error = null;
            if (!IsSupportedItem(item))
            {
                error = "Этот свиток подходит только для оружия, брони, одежды, украшений и талисманов.";
                return false;
            }

            if (!IsEnchanted(item))
            {
                error = "Свиток работает только на зачарованных предметах.";
                return false;
            }

            if (!TryGetDurability(item, out int hits, out int maxHits))
            {
                if (item is BaseJewel jewel && jewel.CanRepair)
                {
                    hits = jewel.HitPoints;
                    maxHits = jewel.MaxHitPoints;
                }
                else if (item is BaseTalisman talisman && talisman.CanRepair)
                {
                    hits = talisman.HitPoints;
                    maxHits = talisman.MaxHitPoints;
                }
                else
                {
                    error = "Этот предмет нельзя ремонтировать.";
                    return false;
                }
            }

            if (maxHits <= 0 || hits >= maxHits)
            {
                error = "Предмет уже полностью исправен или не имеет прочности.";
                return false;
            }

            RepairItem(item, maxHits);
            return true;
        }

        private static void RepairItem(Item item, int maxHits)
        {
            if (item is BaseWeapon weapon)
                weapon.HitPoints = maxHits;
            else if (item is BaseArmor armor)
                armor.HitPoints = maxHits;
            else if (item is BaseClothing clothing)
                clothing.HitPoints = maxHits;
            else if (item is BaseJewel jewel)
                jewel.HitPoints = maxHits;
            else if (item is BaseTalisman talisman)
                talisman.HitPoints = maxHits;

            item.InvalidateProperties();
        }
    }

    public class EnchantedRepairScroll : Item
    {
        [Constructable]
        public EnchantedRepairScroll() : base(0x1F2D)
        {
            Weight = 0.1;
            Hue = 1153;
            Name = "свиток ремонта зачарованного предмета";
        }

        public EnchantedRepairScroll(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (Deleted || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Выберите зачарованный поврежденный предмет в рюкзаке или надетый на вас.");
            from.Target = new RepairScrollTarget(this);
        }

        private sealed class RepairScrollTarget : Target
        {
            private readonly EnchantedRepairScroll _scroll;

            public RepairScrollTarget(EnchantedRepairScroll scroll) : base(10, false, TargetFlags.None)
            {
                _scroll = scroll;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_scroll == null || _scroll.Deleted || !_scroll.IsChildOf(from.Backpack))
                {
                    from.SendMessage(0x22, "Свиток должен находиться в вашем рюкзаке.");
                    return;
                }

                if (!(targeted is Item item) || item.Deleted || item.RootParent != from ||
                    (!item.IsChildOf(from.Backpack) && item.Parent != from))
                {
                    from.SendMessage(0x22, "Предмет должен находиться в вашем рюкзаке или быть надет на вас.");
                    return;
                }

                if (!SmithRepairService.TryRepairEnchantedItem(item, out string error))
                {
                    from.SendMessage(0x22, error);
                    return;
                }

                _scroll.Delete();
                from.PlaySound(0x2A);
                from.SendMessage(0x35, "Магия свитка восстановила прочность зачарованного предмета без снижения его максимальной прочности.");
            }
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            list.Add("Одноразовый свиток: полностью чинит один поврежденный зачарованный предмет.");
            list.Add("Не снижает максимальную прочность.");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
