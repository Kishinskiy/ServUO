using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Targeting;
using Server.ContextMenus;

namespace Server.Custom.MageCircles
{
    public class CircleRestrictionContextMod
    {
        public static void Initialize()
        {
            // Подписываемся на событие формирования контекстного меню
            EventSink.ContextMenu += OnContextMenu;
        }

        private static void OnContextMenu(ContextMenuEventArgs e)
        {
            Mobile from = e.Mobile;

            // Проверяем, что меню вызвано для свитка магии
            if (from == null || !(e.Target is SpellScroll))
                return;

            SpellScroll scroll = (SpellScroll)e.Target;
            ContextMenuEntry originalEntry = null;

            // Ищем оригинальный пункт меню "Add to Spellbook" (ID 6144)
            foreach (ContextMenuEntry entry in e.Entries)
            {
                if (entry.Number == 6144)
                {
                    originalEntry = entry;
                    break;
                }
            }

            // Если нашли — удаляем оригинал и вставляем наш безопасный аналог
            if (originalEntry != null)
            {
                e.Entries.Remove(originalEntry);
                e.Entries.Add(new CustomAddToSpellbookEntry(scroll));
            }
        }

        // Наш кастомный пункт меню, заменяющий оригинальный
        private class CustomAddToSpellbookEntry : ContextMenuEntry
        {
            private readonly SpellScroll m_Scroll;

            public CustomAddToSpellbookEntry(SpellScroll scroll)
                : base(6144, 3) // 6144 - ID текста "Add to Spellbook", 3 - дальность клика
            {
                this.m_Scroll = scroll;
            }

            public override void OnClick()
            {
                if (this.Owner.From.CheckAlive() && this.m_Scroll != null && !this.m_Scroll.Deleted)
                {
                    // При клике вызываем наш кастомный безопасный прицел (таргет)
                    this.Owner.From.Target = new CustomSpellbookTarget(this.m_Scroll);
                }
            }
        }

        // Наш изолированный класс прицела с проверкой навыка Magery
        private class CustomSpellbookTarget : Target
        {
            private readonly SpellScroll m_Scroll;

            public CustomSpellbookTarget(SpellScroll scroll)
                : base(3, false, TargetFlags.None)
            {
                this.m_Scroll = scroll;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Spellbook)
                {
                    if (from.CheckAlive() && !this.m_Scroll.Deleted && this.m_Scroll.Movable && this.m_Scroll.Amount >= 1 && this.m_Scroll.CheckItemUse(from))
                    {
                        Spellbook book = (Spellbook)targeted;
                        SpellbookType type = Spellbook.GetTypeForSpell(this.m_Scroll.SpellID);

                        if (type != book.SpellbookType)
                        {
                            return;
                        }
                        else if (book.HasSpell(this.m_Scroll.SpellID))
                        {
                            from.SendLocalizedMessage(500179); // That spell is already present in that spellbook.
                        }
                        else
                        {
                            // Проверка круга магии на соответствие навыку Magery
                            if (book.SpellbookType == SpellbookType.Regular)
                            {
                                int circleIndex = (this.m_Scroll.SpellID - book.BookOffset) / 8;
                                double magerySkill = from.Skills[SkillName.Magery].Base;

                                double requiredSkill = 0.0;
                                switch (circleIndex)
                                {
                                    case 0: requiredSkill = 0.0; break;
                                    case 1: requiredSkill = 12.5; break;
                                    case 2: requiredSkill = 25.0; break;
                                    case 3: requiredSkill = 37.5; break;
                                    case 4: requiredSkill = 50.0; break;
                                    case 5: requiredSkill = 62.5; break;
                                    case 6: requiredSkill = 75.0; break;
                                    case 7: requiredSkill = 87.5; break;
                                }

                                if (magerySkill < requiredSkill)
                                {
                                    from.SendMessage("Ваш навык магии слишком мал! Чтобы записать заклинание {0}-го круга, требуется {1} Magery.", circleIndex + 1, requiredSkill);
                                    return; // Прерываем добавление, свиток цел
                                }
                            }

                            // Если проверка успешна — добавляем заклинание в книгу
                            int val = this.m_Scroll.SpellID - book.BookOffset;

                            if (val >= 0 && val < book.BookCount)
                            {
                                book.Content |= (ulong)1 << val;
                                this.m_Scroll.Consume();
                                from.Send(new Network.PlaySound(0x249, book.GetWorldLocation()));
                            }
                        }
                    }
                }
            }
        }
    }
}
