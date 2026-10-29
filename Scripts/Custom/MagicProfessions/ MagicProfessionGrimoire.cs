using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;
using Server.Custom.VoidCraft;
using Server.Custom.CurseSystem;

namespace Server.Custom.MagicProfessions
{
    public class MagicProfessionGrimoire : Item
    {
        private bool m_HasIdentify; 
        private bool m_HasCleanse;  

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasIdentify { get { return m_HasIdentify; } set { m_HasIdentify = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasCleanse { get { return m_HasCleanse; } set { m_HasCleanse = value; InvalidateProperties(); } }

        [Constructable]
        public MagicProfessionGrimoire() : base(0x2252) 
        {
            Name = "Пустой Гримуар... Магических Профессий";
            Hue = 1266; 
            Layer = Layer.OneHanded;
            Weight = 2.0;

            m_HasIdentify = false;
            m_HasCleanse = false;
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);
            
            int spellsCount = 0;
            if (m_HasIdentify) spellsCount++;
            if (m_HasCleanse) spellsCount++;

            list.Add($"Заклинаний вплетено: {spellsCount} из 2");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            if (!IsChildOf(from.Backpack) && from.FindItemOnLayer(Layer.OneHanded) != this)
            {
                from.SendMessage(0x22, "Гримуар должен находиться в вашем рюкзаке или в руках.");
                return;
            }

            from.CloseGump(typeof(GrimoireGump));
            from.SendGump(new GrimoireGump(from, this));
        }

        public MagicProfessionGrimoire(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) 
        { 
            base.Serialize(writer); 
            writer.Write((int)1); 
            writer.Write(m_HasIdentify);
            writer.Write(m_HasCleanse);
        }
        public override void Deserialize(GenericReader reader) 
        { 
            base.Deserialize(reader); 
            int version = reader.ReadInt(); 
            m_HasIdentify = reader.ReadBool();
            m_HasCleanse = reader.ReadBool();
        }
    }

    public class GrimoireIdentifyLogicTarget : Target
    {
        private readonly MagicProfessionGrimoire m_Grimoire;

        public GrimoireIdentifyLogicTarget(MagicProfessionGrimoire grimoire) : base(2, false, TargetFlags.None)
        {
            m_Grimoire = grimoire;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Grimoire == null || m_Grimoire.Deleted || from == null || !from.Alive || targeted == null) return;

            if (targeted is Item item)
            {
                XmlCurseAttribute curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
                if (curse == null)
                {
                    from.SendMessage("Этот предмет не обладает скрытыми или проклятыми изъянами.");
                    return;
                }

                if (curse.IsIdentified)
                {
                    from.SendMessage("Свойства этого предмета уже полностью раскрыты.");
                    return;
                }

                int requiredMana = 20;
                if (from.Mana < requiredMana)
                {
                    from.SendMessage(0x22, "Вам не хватает маны для каста распознавания.");
                    return;
                }

                int rarityClass = DiabloLootColors.GetRarityClass(item);
                double evalIntel = from.Skills[SkillName.EvalInt].Value;

                double baseDifficulty = 30.0 + (rarityClass * 15.0);
                double identifyChance = (evalIntel / baseDifficulty) * (from.Int / 100.0);

                from.Mana -= requiredMana;
                from.Animate(AnimationType.Spell, 0);

                if (evalIntel >= 100.0 || Utility.RandomDouble() < identifyChance)
                {
                    curse.IsIdentified = true;
                    if (item is BaseWeapon weapon) weapon.Identified = true;
                    else if (item is BaseArmor armor) armor.Identified = true;

                    item.InvalidateProperties();
                    item.SendPropertiesTo(from);
                    from.PlaySound(0x249);
                    from.SendMessage(0x35, "Ритуал Гримуара успешно раскрыл свойства магической вещи!");
                }
                else
                {
                    from.PlaySound(0x54A);
                    from.SendMessage(0x22, "Ментальный фокус сорвался, руны предмета остались скрыты.");
                }
            }
            else
            {
                from.SendMessage(0x22, "Распознавать можно только предметы экипировки.");
            }
        }
    }

    public class GrimoireCleanseTarget : Target
    {
        public GrimoireCleanseTarget() : base(2, false, TargetFlags.None) { }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (from == null || !from.Alive || targeted == null) return;

            if (targeted is Item item)
            {
                XmlCurseAttribute curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
                if (curse == null || !curse.IsIdentified || !curse.HasNegativeEffects)
                {
                    from.SendMessage("Предмет должен быть осквернен, распознан и иметь негативные эффекты.");
                    return;
                }

                bool nearStation = false;
                foreach (Item checkItem in from.GetItemsInRange(3))
                {
                    if (checkItem.GetType().Name == "ArcaneWorkbench" || checkItem.GetType().Name == "NecromanticAltar")
                    {
                        nearStation = true;
                        break;
                    }
                }

                if (!nearStation)
                {
                    from.PlaySound(0x5C3);
                    from.SendMessage(0x22, "Ритуал экзорцизма можно проводить только вплотную к Арканическому верстаку или Алтарю некроманта.");
                    return;
                }

                int requiredMana = 35;
                if (from.Mana < requiredMana)
                {
                    from.SendMessage(0x22, "Вам не хватает маны.");
                    return;
                }

                Container bp = from.Backpack;
                Item dustResource = bp.FindItemByType(typeof(ArcaneDust)); 
                Item ashResource = bp.FindItemByType(typeof(SoulAsh));   

                if (dustResource == null && ashResource == null)
                {
                    from.SendMessage(0x22, "Для проведения ритуала необходимы: Арканическая пыль или Прах души!");
                    return;
                }

                int rarityClass = DiabloLootColors.GetRarityClass(item);
                bool isArtifact = (rarityClass == 3 || item.IsArtifact);
                double evalIntel = from.Skills[SkillName.EvalInt].Value;

                if (isArtifact)
                {
                    if (evalIntel <= 120.0)
                    {
                        from.PlaySound(0x5C3);
                        from.SendMessage(0x22, "Это древний Артефакт! Требуется Evaluating Intelligence выше 120.0.");
                        return;
                    }
                    ExecuteGrimoireCleanse(from, item, curse, true, requiredMana, dustResource, ashResource);
                }
                else
                {
                    double curseDifficulty = 30.0 + (curse.HpPenalty * 2.0);
                    double cleanseChance = (evalIntel / curseDifficulty);

                    if (evalIntel >= 100.0 || Utility.RandomDouble() < cleanseChance)
                    {
                        ExecuteGrimoireCleanse(from, item, curse, false, requiredMana, dustResource, ashResource);
                    }
                    else
                    {
                        from.Mana -= (requiredMana / 2);
                        from.PlaySound(0x54A);
                        from.SendMessage(0x22, "Потоки маны вырвались из-под контроля! Заклинание рассеялось.");
                    }
                }
            }
        }

        private void ExecuteGrimoireCleanse(Mobile from, Item item, XmlCurseAttribute curse, bool isArtifact, int manaCost, Item dust, Item ash)
        {
            from.Mana -= manaCost;
            if (dust != null) dust.Consume(1);
            else if (ash != null) ash.Consume(1);

            curse.CleanseNegativeEffects();
            
            Item curseResource = new CurseShardCraftable();
            from.AddToBackpack(curseResource);

            from.Animate(AnimationType.Spell, 0); 
            from.PlaySound(0x1F8);
            Effects.SendLocationParticles(EffectItem.Create(item.Location, item.Map, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 20, 1175, 0, 5004, 0);

            from.SendMessage(0x58, isArtifact 
                ? "Скверна древнего артефакта полностью поглощена силой станции, Осколок Скверны извлечен." 
                : "Ритуал заклинания прошел успешно. Вещь полностью очищена!");
        }
    }
}
