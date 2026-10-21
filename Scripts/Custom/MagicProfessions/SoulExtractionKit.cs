using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Network;
using Server.Engines.Craft;

namespace Server.Custom.MagicProfessions
{
    public class SoulExtractionKit : Item, ICraftable
    {
        [Constructable]
        public SoulExtractionKit() : base(0x0E24)
        {
            Name = "Набор для извлечения душ";
            Hue = 1109;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;
            if (from.Backpack == null || !IsChildOf(from.Backpack)) { from.SendLocalizedMessage(1042001); return; }

            if (from.Skills[SkillName.Necromancy].Value < 50.0 || from.Skills[SkillName.SpiritSpeak].Value < 40.0)
            {
                from.SendMessage(0x22, "Для извлечения душ нужны Necromancy 50.0 и Spirit Speak 40.0.");
                return;
            }

            from.SendMessage("Выберите труп существа для извлечения остаточной души.");
            from.Target = new SoulExtractionTarget(this);
        }

        private class SoulExtractionTarget : Target
        {
            private readonly SoulExtractionKit _kit;
            public SoulExtractionTarget(SoulExtractionKit kit) : base(2, false, TargetFlags.None) => _kit = kit;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || !from.Alive || from.Backpack == null || _kit == null || _kit.Deleted || !_kit.IsChildOf(from.Backpack)) return;

                Corpse corpse = targeted as Corpse;
                if (corpse == null || corpse.Deleted || corpse.Map != from.Map || !from.InRange(corpse.Location, 2) || !(corpse.Owner is BaseCreature creature) || creature.Player)
                {
                    from.SendMessage(0x22, "Извлекать душу можно только из трупа существа.");
                    return;
                }
                if (creature.Summoned || creature.Controlled)
                {
                    from.SendMessage(0x22, "Душа призванного или прирученного существа недоступна.");
                    return;
                }
                if (corpse.IsCriminalAction(from)) { from.CriminalAction(true); from.SendMessage(0x22, "Вы не можете провести ритуал над этим трупом."); return; }

                foreach (Item item in corpse.Items)
                {
                    if (item is SoulHarvestMarker) { from.SendMessage(0x22, "Остаточная душа этого существа уже извлечена."); return; }
                }

                int skill = (int)((from.Skills[SkillName.Necromancy].Value + from.Skills[SkillName.SpiritSpeak].Value) / 2.0);
                int amount = Utility.RandomMinMax(1, skill >= 90 ? 3 : 2);
                SoulResidue residue = new SoulResidue(amount);

                if (!from.AddToBackpack(residue)) { residue.Delete(); from.SendMessage(0x22, "Освободите место в рюкзаке."); return; }

                corpse.AddItem(new SoulHarvestMarker());
                from.PlaySound(0x1FB);
                from.SendMessage(0x59, $"Ритуал завершен. Получено остатка души: {amount}.");
            }
        }

        public SoulExtractionKit(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }

        public int OnCraft(int quality, bool issuersMakersName, Mobile from, CraftSystem craftSystem, Type typeRes, ITool tool, CraftItem craftItem, int resHue) => quality;

        public static void Initialize()
        {
            CraftSystem system = DefTailoring.CraftSystem;
            if (system != null)
            {
                int index = system.AddCraft(
                    typeof(SoulExtractionKit), "Магические Снасти", "Набор для извлечения душ",
                    85.0, 110.0,
                    typeof(Leather), "Кожа", 15, "Недостаточно кожи."
                );
                system.AddSkill(index, SkillName.SpiritSpeak, 50.0, 70.0);
                
                CraftItem craftItem = system.CraftItems.GetAt(index);
                if (craftItem != null)
                {
                    // ИСПРАВЛЕНО: Заменен метод AddReg на встроенный метод AddRes эмулятора ServUO
                    craftItem.AddRes(typeof(Ectoplasm), "Эктоплазма", 3, "Вам нужна эктоплазма.");
                    craftItem.AddRes(typeof(SpidersSilk), "Паутина", 10, "Вам нужна паутина.");
                }
            }
        }
    }

    public class SoulHarvestMarker : Item
    {
        public SoulHarvestMarker() : base(0x1EA7) { Name = "остаток души извлечен"; Hue = 1109; Movable = false; Visible = false; Weight = 0.0; }
        public SoulHarvestMarker(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); Visible = false; Movable = false; }
    }
}
