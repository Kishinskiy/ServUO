using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;
using Server.Custom.VoidCraft;

namespace Server.Custom.CurseSystem
{
    public class MageRemoveCurseScroll : Item
    {
        [Constructable]
        public MageRemoveCurseScroll() : base(0x14F0)
        {
            Name = "Свиток Снятия Проклятия";
            Hue = 1175; // Очищающий зеленый цвет
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в рюкзаке
                return;
            }

            from.SendMessage("Выберите распознанный проклятый предмет для снятия негативных эффектов.");
            from.Target = new MageRemoveCurseTarget(this);
        }

        public MageRemoveCurseScroll(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write((int)0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); int version = reader.ReadInt(); }
    }

    public class MageRemoveCurseTarget : Target
    {
        private readonly MageRemoveCurseScroll m_Scroll;

        public MageRemoveCurseTarget(MageRemoveCurseScroll scroll) : base(2, false, TargetFlags.None)
        {
            m_Scroll = scroll;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Scroll == null || m_Scroll.Deleted || from == null || !from.Alive) return;

            if (targeted is Item item)
            {
                XmlCurseAttribute curse = XmlAttach.FindAttachment(item, typeof(XmlCurseAttribute)) as XmlCurseAttribute;
                if (curse == null)
                {
                    from.SendMessage("Этот предмет чист, на нем нет проклятий.");
                    return;
                }

                if (!curse.IsIdentified)
                {
                    from.SendMessage(0x22, "Сначала необходимо распознать свойства предмета соответствующим свитком.");
                    return;
                }

                if (!curse.HasNegativeEffects)
                {
                    from.SendMessage("Отрицательные эффекты этого предмета уже очищены.");
                    return;
                }

                int rarityClass = DiabloLootColors.GetRarityClass(item);
                bool isArtifact = (rarityClass == 3 || item.IsArtifact);
                double evalIntel = from.Skills[SkillName.EvalInt].Value;

                // =========================================================================
                // ЖЕСТКАЯ ПРОВЕРКА ДЛЯ АРТЕФАКТОВ (> 120 EvalInt)
                // =========================================================================
                if (isArtifact)
                {
                    if (evalIntel <= 120.0)
                    {
                        from.PlaySound(0x5C3);
                        from.SendMessage(0x22, "Это древний Артефакт. Для снятия проклятия требуются знания Evaluating Intelligence выше 120 уровня!");
                        return;
                    }

                    // Для артефактов при EvalInt > 120 успех равен 100%
                    ExecuteCleanse(from, item, curse, true);
                }
                // =========================================================================
                // СНЯТИЕ ПРОКЛЯТИЙ С ОБЫЧНЫХ КЛАССОВ ВЕЩЕЙ
                // =========================================================================
                else
                {
                    double curseDifficulty = 30.0 + (curse.HpPenalty * 2.0);
                    double cleanseChance = (evalIntel / curseDifficulty);

                    // При EvalInt >= 100 или удачном кубике — 100% успех
                    if (Utility.RandomDouble() < cleanseChance || evalIntel >= 100.0)
                    {
                        ExecuteCleanse(from, item, curse, false);
                    }
                    else
                    {
                        from.PlaySound(0x54A);
                        from.SendMessage(0x22, "Вам не хватило ментальной концентрации, чтобы разрушить узы проклятия.");
                    }
                }

                m_Scroll.Delete(); // Свиток уничтожается при любой попытке
            }
            else
            {
                from.SendMessage(0x22, "Очищать можно только предметы экипировки.");
            }
        }

        private void ExecuteCleanse(Mobile from, Item item, XmlCurseAttribute curse, bool isArtifact)
        {
            curse.CleanseNegativeEffects();
            
            Item curseResource = new CurseShardCraftable();
            from.AddToBackpack(curseResource);

            from.PlaySound(0x1F8);
            Effects.SendLocationParticles(EffectItem.Create(item.Location, item.Map, TimeSpan.FromSeconds(1.0)), 0x3709, 10, 20, 1175, 0, 5004, 0);

            from.SendMessage(0x58, isArtifact 
                ? "Проклятие высшего артефакта полностью рассеялось. Осколок Скверны извлечен." 
                : "Скверна отступила. Отрицательные эффекты удалены, а полезные свойства сохранены.");
        }
    }
}
