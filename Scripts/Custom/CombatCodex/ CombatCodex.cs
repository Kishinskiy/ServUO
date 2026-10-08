using System;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Gumps;

namespace Server.Custom.CombatCodex
{
    public enum CodexStance
    {
        None,
        Aggressive, // +Урон, -10% к защите (Outlands Style)
        Defensive,  // +Уклонение
        Cleave      // Шанс нанести АОЕ урон соседней цели
    }

    public class CombatCodex : Item
    {
        private CodexStance m_ActiveStance;
        private int m_Experience;
        private int m_Level;
        private SkillName m_WeaponSkill; // ДОБАВЛЕНО: Специализация оружия кодекса

        [CommandProperty(AccessLevel.GameMaster)]
        public CodexStance ActiveStance { get => m_ActiveStance; set { m_ActiveStance = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Experience { get => m_Experience; set { m_Experience = value; CheckLevelUp(); InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get => m_Level; set { m_Level = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public SkillName WeaponSkill { get => m_WeaponSkill; set { m_WeaponSkill = value; InvalidateProperties(); } }

        [Constructable]
        public CombatCodex() : this(SkillName.Swords) // По умолчанию создается кодекс мечей
        {
        }

        [Constructable]
        public CombatCodex(SkillName weaponSkill) : base(0x2259) // Графика красной книги
        {
            m_WeaponSkill = weaponSkill;
            m_ActiveStance = CodexStance.None;
            m_Level = 1;
            m_Experience = 0;
            Weight = 1.0;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Меняем цвета книг в зависимости от оружия, как на Outlands!
            switch (m_WeaponSkill)
            {
                case SkillName.Swords: Hue = 1258; Name = "Кодекс Клинков (Swordsmanship)"; break; // Кроваво-красный
                case SkillName.Macing: Hue = 1133; Name = "Кодекс Тяжелого Оружия (Macing)"; break; // Железный/Серый
                case SkillName.Fencing: Hue = 1272; Name = "Кодекс Копий (Fencing)"; break; // Стальной/Голубой
                case SkillName.Archery: Hue = 1424; Name = "Кодекс Стрельбы (Archery)"; break; // Древесный/Зеленый
                default: Hue = 0; Name = "Боевой Кодекс Мастерства"; break;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Специализация: {m_WeaponSkill}"); // Показывает к какому оружию привязан кодекс
            list.Add($"Уровень Кодекса: {m_Level} / 5");
            list.Add($"Опыт: {m_Experience} / {GetXpForNextLevel()}");
            list.Add($"Активная стойка: {GetStanceNameRu()}");
        }

        private int GetXpForNextLevel()
        {
            return m_Level * 1000;
        }

        private string GetStanceNameRu()
        {
            switch (m_ActiveStance)
            {
                case CodexStance.Aggressive: return "Агрессивная Стойка";
                case CodexStance.Defensive: return "Защитная Стойка";
                case CodexStance.Cleave: return "Рассекающий Удар";
                default: return "Нет (Кликните для выбора)";
            }
        }

        private void CheckLevelUp()
        {
            if (m_Level >= 5) return;

            if (m_Experience >= GetXpForNextLevel())
            {
                m_Experience -= GetXpForNextLevel();
                m_Level++;

                if (Parent is Mobile from)
                {
                    from.PlaySound(0x5B6);
                    from.SendMessage(0x35, $"[{Name}] Поздравляем! Ваш кодекс развился до {m_Level}-го уровня!");
                }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.CloseGump(typeof(CodexGump));
            from.SendGump(new CodexGump(this));
        }

        // Вспомогательный метод: ищет правильный кодекс под текущее оружие воина
        private static CombatCodex FindMatchingCodex(Mobile mobile)
        {
            if (mobile == null || mobile.Backpack == null || mobile.Weapon == null) return null;

            BaseWeapon currentWeapon = mobile.Weapon as BaseWeapon;
            if (currentWeapon == null) return null;

            // Получаем боевой навык текущего оружия
            SkillName weaponSkill = currentWeapon.Skill;

            // Ищем среди всех предметов в паке Кодексы
            foreach (Item item in mobile.Backpack.Items)
            {
                if (item is CombatCodex codex && codex.WeaponSkill == weaponSkill)
                {
                    return codex; // Нашли кодекс, который совпадает с оружием в руках!
                }
            }
            return null;
        }

        // =========================================================================
        // ПЕРЕХВАТ УДАРОВ (Triggered from BaseWeapon.cs) ⚔️
        // =========================================================================
public static void OnWeaponHit(Mobile attacker, Mobile defender, ref int damage)
        {
            if (attacker == null || defender == null || attacker == defender) return;

            // Ищем кодекс, соответствующий оружию в руках
            CombatCodex codex = FindMatchingCodex(attacker);
            if (codex == null || codex.ActiveStance == CodexStance.None) return;

            // Начисление опыта за монстров
            if (defender is BaseCreature && !defender.Player)
            {
                BaseCreature monster = (BaseCreature)defender;
                int xpGained = 1;

                if (monster.HitsMax > 1500) xpGained = 10;
                else if (monster.HitsMax > 500) xpGained = 4;
                else if (monster.HitsMax > 150) xpGained = 2;

                codex.Experience += xpGained;
            }

            // Проверяем, стрелок ли наш персонаж (держит ли лук/арбалет)
            bool isRanged = (codex.WeaponSkill == SkillName.Archery);

            // Базовый модификатор урона от уровня книги
            double modifier = 1.0 + (codex.Level * 0.03); 

            // =========================================================================
            // СТОЙКА 1: АГРЕССИВНАЯ (КРИТЫ ДЛЯ МИЛИ / СНАЙПЕР ДЛЯ ЛУКА)
            // =========================================================================
            if (codex.ActiveStance == CodexStance.Aggressive)
            {
                damage = (int)(damage * modifier); 

                if (isRanged) // ЛОГИКА ДЛЯ СТРЕЛКОВ 🏹
                {
                    // Шанс снайперского выстрела зависит от дистанции!
                    int distance = (int)attacker.GetDistanceToSqrt(defender);
                    
                    // Базовый шанс (макс 15% на 5 уровне) + бонус за дальность (дистанция * 0.7%)
                    double sniperChance = (codex.Level * 0.03) + (distance * 0.007);
                    if (sniperChance > 0.25) sniperChance = 0.25; // Кап шанса 25%

                    if (Utility.RandomDouble() < sniperChance)
                    {
                        damage = (int)(damage * 1.75); // Снайперский крит бьет сильнее — х1.75!
                        Effects.PlaySound(defender.Location, defender.Map, 0x224); // Звук глубокого пробития стрелой
                        Effects.SendLocationParticles(EffectItem.Create(defender.Location, defender.Map, TimeSpan.FromSeconds(0.5)), 0x3779, 10, 10, 0, 0, 1160, 0); // Золотая вспышка
                        attacker.SendMessage(0x35, $"[СНАЙПЕРСКИЙ ВЫСТРЕЛ!] Дистанция: {distance} тайлов. Вы нанесли х1.75 урон по {defender.Name}!");
                    }
                }
                else // ЛОГИКА ДЛЯ БЛИЖНЕГО БОЯ ⚔️
                {
                    double critChance = codex.Level * 0.03;
                    if (Utility.RandomDouble() < critChance)
                    {
                        damage = (int)(damage * 1.5); 
                        Effects.PlaySound(defender.Location, defender.Map, 0x3B3); 
                        Effects.SendLocationParticles(EffectItem.Create(defender.Location, defender.Map, TimeSpan.FromSeconds(0.5)), 0x37B9, 10, 10, 0, 0, 9965, 0); 
                        attacker.SendMessage(0x35, $"[КРИТИЧЕСКИЙ УДАР!] Вы нанесли сокрушительный удар по {defender.Name}!");
                    }
                }
            }
            // =========================================================================
            // СТОЙКА 2: РАССЕЧЕНИЕ (КЛИВ МИЛИ / ОСКОЛОЧНАЯ СТРЕЛА ДЛЯ ЛУКА)
            // =========================================================================
            else if (codex.ActiveStance == CodexStance.Cleave)
            {
                bool triggerHarvest = (Utility.RandomDouble() < 0.15) || (!defender.Alive);

                if (triggerHarvest)
                {
                    int harvestDamage = codex.Level * 6;
                    int targetsHit = 0;

                    // Осколки летят ИЗ МОНСТРА в его соседей
                    IPooledEnumerable eable = attacker.Map.GetMobilesInRange(defender.Location, 2);
                    foreach (Mobile m in eable)
                    {
                        if (m != attacker && m != defender && m is BaseCreature && m.Alive)
                        {
                            m.Damage(harvestDamage, attacker);
                            m.FixedParticles(0x37B9, 9, 10, 1160, EffectLayer.Waist); // Вспышка осколков на целях
                            targetsHit++;
                        }
                    }
                    eable.Free();

                    if (targetsHit > 0)
                    {
                        Effects.PlaySound(defender.Location, defender.Map, 0x307); // Звук микровзрыва стрелы
                        if (isRanged)
                            attacker.SendMessage(0x35, $"[ОСКОЛОЧНАЯ СТРЕЛА!] Ваша стрела разорвалась, задев {targetsHit} врагов вокруг цели на {harvestDamage} урона!");
                        else
                            attacker.SendMessage(0x35, $"[КРОВАВАЯ ЖАТВА!] Вы совершили круговой вихрь, задев {targetsHit} врагов на {harvestDamage} чистого урона!");
                    }
                }
                else if (Utility.RandomDouble() < 0.25) // Обычный одиночный клив
                {
                    IPooledEnumerable eable = attacker.Map.GetMobilesInRange(defender.Location, 2);
                    foreach (Mobile m in eable)
                    {
                        if (m != attacker && m != defender && m is BaseCreature && m.Alive)
                        {
                            int cleaveDamage = (int)(damage * 0.30);
                            m.Damage(cleaveDamage, attacker);
                            m.FixedParticles(0x376A, 9, 10, 5042, EffectLayer.Waist);
                            attacker.SendMessage(88, $"[Рассечение] Вы задели {m.Name} на {cleaveDamage} урона!");
                            break; 
                        }
                    }
                    eable.Free();
                }
            }
            // =========================================================================
            // СТОЙКА 3: ДЕБАФФ ЗАГРАДИТЕЛЬНОГО ВЫСТРЕЛА (Только для стрелков при попадании)
            // =========================================================================
            else if (codex.ActiveStance == CodexStance.Defensive && isRanged)
            {
                // У лучников защитная стойка срабатывает прямо при атаке как выстрел по ногам!
                double suppressChance = codex.Level * 0.05; // 5% - 25% шанс

                if (Utility.RandomDouble() < suppressChance && defender is BaseCreature && defender.Alive)
                {
                    // Стрела пригвождает монстра к месту на 2 реальные секунды
                    defender.Paralyze(TimeSpan.FromSeconds(2.0));
                    defender.FixedParticles(0x376A, 9, 20, 1166, EffectLayer.Waist); // Грязно-зеленый эффект на ногах моба
                    Effects.PlaySound(defender.Location, defender.Map, 0x2F3); // Звук хруста/удара по ногам
                    
                    attacker.SendMessage(0x61, $"[ЗАГРАДИТЕЛЬНЫЙ ВЫСТРЕЛ!] Вы ранили {defender.Name} в ногу, обездвижив его на 2 секунды!");
                }
            }
        }

        // =========================================================================
        // УПРАВЛЕНИЕ ЗАЩИТОЙ И ШТРАФАМИ (Triggered from BaseWeapon.cs -> CheckParry) 🛡️
        // =========================================================================
        public static void ModifyDefense(Mobile defender, ref double chance)
        {
            if (defender == null || defender.Backpack == null) return;

            // Ищем кодекс под текущее оружие
            CombatCodex codex = FindMatchingCodex(defender);
            if (codex == null) return;

            // OUTLANDS STYLE: Если включена Агрессивная стойка — режем защиту воина на жесткие 10%
            if (codex.ActiveStance == CodexStance.Aggressive)
            {
                chance = Math.Max(0.0, chance - 0.10);
            }
            // Если включена Защитная стойка — даем бонус к уклонению + шанс на Тактическое парирование!
            else if (codex.ActiveStance == CodexStance.Defensive)
            {
                chance += (codex.Level * 0.03); // Бонус к шансу блока (+3%... +15%)

                // Расчет шанса на контратаку при успешном парировании (5% на 1 ур., 25% на 5 ур.)
                double riposteChance = codex.Level * 0.05;

                if (Utility.RandomDouble() < riposteChance)
                {
                    // Метод сработает только если блок УЖЕ ЗАФИКСИРОВАН ядром (проверяем в следующем цикле)
                    // Чтобы нанести урон атакующему, мы находим текущую цель, которая бьет воина
                    Mobile attacker = defender.Combatant as Mobile;

                    if (attacker != null && attacker.Alive && attacker.InRange(defender.Location, 1) && attacker is BaseCreature)
                    {
                        // Вычисляем чистый урон от контратаки (Уровень * 5)
                        int riposteDamage = codex.Level * 5;
                        attacker.Damage(riposteDamage, defender);

                        // Восстанавливаем воину Stamina (от 5 до 10 единиц)
                        int stamRestore = Utility.RandomMinMax(5, 10);
                        defender.Stam = Math.Min(defender.StamMax, defender.Stam + stamRestore);

                        // Визуальные и звуковые эффекты удара щитом/рукоятью
                        Effects.PlaySound(defender.Location, defender.Map, 0x3B4); // Глухой удар
                        attacker.FixedParticles(0x376A, 9, 10, 5044, EffectLayer.Waist); // Вспышка искр на враге

                        defender.SendMessage(0x61, $"[Парирование!] Вы контратаковали {attacker.Name}, нанеся {riposteDamage} урона и восстановив {stamRestore} выносливости!");
                    }
                }
            }
        }

        public CombatCodex(Serial serial) : base(serial) { }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // Новая версия сохранения (с учетом WeaponSkill)
            writer.Write((int)m_ActiveStance);
            writer.Write(m_Experience);
            writer.Write(m_Level);
            writer.Write((int)m_WeaponSkill); // Сохраняем привязку к навыку
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_ActiveStance = (CodexStance)reader.ReadInt();
            m_Experience = reader.ReadInt();
            m_Level = reader.ReadInt();

            if (version >= 1)
                m_WeaponSkill = (SkillName)reader.ReadInt();
            else
                m_WeaponSkill = SkillName.Swords; // Дефолт для старых предметов

            UpdateVisuals();
        }
    }

    public class CodexGump : Gump
    {
        private CombatCodex m_Codex;

        public CodexGump(CombatCodex codex) : base(100, 100)
        {
            m_Codex = codex;
            AddPage(0);
            AddBackground(0, 0, 300, 250, 9270);
            AddAlphaRegion(10, 10, 280, 230);
            AddLabel(40, 20, 1152, $"{m_Codex.Name.ToUpper()}");

            AddButton(30, 70, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(70, 70, m_Codex.ActiveStance == CodexStance.Aggressive ? 63 : 1152, "Агрессивная стойка (+Урон, -Защита)");
            AddButton(30, 120, 4005, 4007, 2, GumpButtonType.Reply, 0);
            AddLabel(70, 120, m_Codex.ActiveStance == CodexStance.Defensive ? 61 : 1152, "Защитная стойка (+Уклонение)");
            AddButton(30, 170, 4005, 4007, 3, GumpButtonType.Reply, 0);
            AddLabel(70, 170, m_Codex.ActiveStance == CodexStance.Cleave ? 88 : 1152, "Рассекающий удар (АОЕ Урон)");
        }
        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            if (sender == null) return;
            Mobile from = sender.Mobile;
            
            if (from == null || m_Codex == null || m_Codex.Deleted || !m_Codex.IsChildOf(from.Backpack)) return;

            switch (info.ButtonID)
            {
                case 1: m_Codex.ActiveStance = CodexStance.Aggressive; from.SendMessage("Вы активировали Агрессивную стойку. Ваш урон вырос, но защита снижена!"); break;
                case 2: m_Codex.ActiveStance = CodexStance.Defensive; from.SendMessage("Вы вошли в Защитную боевую стойку."); break;
                case 3: m_Codex.ActiveStance = CodexStance.Cleave; from.SendMessage("Вы приготовились наносить Рассекающие удары."); break;
            }
        }
    }
}