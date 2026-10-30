using System;
using Server;
using Server.Items;
using Server.Targeting;
using Server.Engines.XmlSpawner2;
using Server.Custom.DiabloLoot;
using Server.Custom.VoidCraft;
using Server.Custom.MagicProfessions;
using Server.Custom.EpicAlignment; // Подключаем калькулятор баланса сил Полубогов!     🎉

namespace Server.Custom.CurseSystem
{
    public class MageRemoveCurseScroll : Item
    {
        [Constructable]
        public MageRemoveCurseScroll() : base(0x14F0)
        {
            Name = "Свиток Снятия Проклятия";
            Hue = 1175; 
            Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || !from.Alive) return;

            if (from.Backpack == null || !IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); 
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
            if (targeted is MagicProfessionGrimoire grimoire)
            {
                if (grimoire.HasCleanse)
                {
                    from.SendMessage("Этот ритуал экзорцизма уже вплетен в структуру вашего гримуара.");
                    return;
                }

                grimoire.HasCleanse = true;
                grimoire.Name = "Гримуар Магических Профессий";
                
                from.PlaySound(0x1F8);
                Effects.SendTargetParticles(grimoire, 0x375A, 9, 20, 1175, EffectLayer.Waist);
                from.SendMessage(0x58, "Вы успешно провели медитацию над книгой и изучили Ритуал Экзорцизма в Гримуаре!");
                
                m_Scroll.Delete();
                return;
            }
            
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
                    // 🔥 НАШ ХУК: Если Аватар существует, планка сложности для артефактов падает со 120 до 110!
                    double requiredEval = EpicAvatarWorldBalance.IsAvatarPlaceOccupied() ? 110.0 : 120.0;

                    if (evalIntel <= requiredEval)
                    {
                        from.PlaySound(0x5C3);
                        from.SendMessage(0x22, $"Это древний Артефакт. Для снятия проклятия требуются знания Evaluating Intelligence выше {requiredEval} уровня!");
                        return; 
                    }

                    // Если навык выше требуемого, артефакт очищается со 100% вероятностью
                    ExecuteCleanse(from, item, curse, true);
                }
                // =========================================================================
                // ДИНАМИЧЕСКОЕ СНЯТИЕ ПРОКЛЯТИЙ С ОБЫЧНЫХ ВЕЩЕЙ (ПО ШАНСУ)
                // =========================================================================
                else
                {
                    // Сложность зависит напрямую от силы порчи (HpPenalty)
                    double curseDifficulty = 30.0 + (curse.HpPenalty * 2.0);
                    double cleanseChance = (evalIntel / curseDifficulty);

                    // При ГМ уровне (100+) или если бросок кубика успешный — проклятие снимается
                    if (evalIntel >= 100.0 || Utility.RandomDouble() < cleanseChance)
                    {
                        ExecuteCleanse(from, item, curse, false);
                    }
                    else
                    {
                        from.PlaySound(0x54A);
                        from.SendMessage(0x22, "Вам не хватило ментальной концентрации, чтобы разрушить узы проклятия.");
                    }
                }

                m_Scroll.Delete(); // Свиток уничтожается при любом исходе ритуала
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
