using System;
using Server.Network;

namespace Server.Custom.ExileHunterBestiary.Items
{
    public class HunterTreatise : Item
    {
        private string m_CreatureId;

        [CommandProperty(AccessLevel.GameMaster)]
        public string CreatureId
        {
            get => m_CreatureId;
            set
            {
                m_CreatureId = value;
                UpdateAppearance();
                InvalidateProperties();
            }
        }

        public HunterCreatureEntry Entry
        {
            get
            {
                if (string.IsNullOrEmpty(m_CreatureId))
                    return null;

                if (HunterBestiaryEngine.RegistryById.TryGetValue(m_CreatureId, out var entry))
                    return entry;

                return null;
            }
        }

        [Constructable]
        public HunterTreatise() : this("Lich")
        {
        }

        [Constructable]
        public HunterTreatise(string creatureId) : base(0x1F5F)
        {
            m_CreatureId = creatureId;
            Weight = 1.0;
            LootType = LootType.Regular;

            UpdateAppearance();
        }

        public HunterTreatise(Type creatureType) : base(0x1F5F)
        {
            if (creatureType != null && HunterBestiaryEngine.RegistryByType.TryGetValue(creatureType, out var entry))
            {
                m_CreatureId = entry.Id;
            }
            else
            {
                m_CreatureId = "Lich";
            }

            Weight = 1.0;
            LootType = LootType.Regular;

            UpdateAppearance();
        }

        public HunterTreatise(Serial serial) : base(serial)
        {
        }

        public void UpdateAppearance()
        {
            ItemID = 0x1F5F; // Графика свитка

            var entry = Entry;
            if (entry != null)
            {
                Name = $"Hunter's Treatise: {entry.DisplayName}";
                Hue = entry.BookHue != 0 ? entry.BookHue : 2101;
            }
            else
            {
                Name = $"Hunter's Treatise: {m_CreatureId}";
                Hue = 2101;
            }
        }

        // =========================================================================
        // ИСПРАВЛЕНО: ИСПОЛЬЗОВАНИЕ СВИТКА ДЛЯ НАЧИСЛЕНИЯ ОПЫТА В РАНГИ 📜🔥
        // =========================================================================
        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // Предмет должен быть в паке
                return;
            }

            var entry = Entry;
            string displayName = entry != null ? entry.DisplayName : m_CreatureId;

            var profile = HunterBestiaryEngine.GetProfile(from);
            if (profile == null)
                return;

            if (entry != null)
            {
                int oldLevel = profile.GetGroupLevel(entry.Category);
                
                // Начисляем случайное количество опыта за прочтение трактата (от 20 до 40 очков)
                int pointsGained = Utility.RandomMinMax(20, 40);
                int realGained = profile.LearnWithProgress(m_CreatureId, pointsGained);

                // Если лимит в 300 очков на одного конкретного моба уже забит
                if (realGained <= 0)
                {
                    from.SendMessage(0x22, $"Вы уже максимально изучили анатомию {displayName} по свиткам! Передайте этот трактат другим охотникам.");
                    from.PlaySound(0x5C);
                    return;
                }

                int newLevel = profile.GetGroupLevel(entry.Category);
                
                from.SendMessage(0x3F, $"Вы изучили трактат о {displayName}! Категория {entry.Category} получила +{realGained} очков прогресса.");
                from.FixedParticles(0x373A, 10, 15, 5012, EffectLayer.Waist);
                from.PlaySound(0x249); // Волшебный звук прочтения

                Consume(); // Свиток исчезает

                // Если за счет этих очков вся общая категория подняла Ранг (например, с 1 на 2)
                if (newLevel > oldLevel)
                {
                    from.PlaySound(0x5B6); // Звук триумфа
                    from.SendMessage(0x35, $"[Бестиарий] Поздравляем! Ваша категория {entry.Category} поднялась до Ранга {newLevel}/5!");
                }
            }
            else
            {
                from.SendMessage(0x22, "Этот трактат поврежден и его невозможно прочесть.");
            }
        }

        // =========================================================================
        // ИСПРАВЛЕНО: ОТОБРАЖЕНИЕ КАТЕГОРИИ И ТЕКУЩЕГО ОПЫТА В ТУЛТИПЕ 🔍
        // =========================================================================
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            var entry = Entry;
            string creatureName = entry != null ? entry.DisplayName : m_CreatureId;
            string category = entry != null ? entry.Category.ToString() : "Unknown";

            list.Add(1070722, string.Format("<BASEFONT COLOR=#D1C4E9>Hunter's Study: {0}<BASEFONT COLOR=#FFFFFF>", creatureName));
            list.Add(1070722, string.Format("<BASEFONT COLOR=#ECEFF1>Category: {1}<BASEFONT COLOR=#FFFFFF>", creatureName, category));
            list.Add(1070722, "<BASEFONT COLOR=#FFE082>Изучение добавляет очки прогресса категории (Ранги 1-5)<BASEFONT COLOR=#FFFFFF>");
        }

        // Обновляем всплывающий статус "Изучено / Не изучено" на основе наличия очков прогресса
        public override void SendPropertiesTo(Mobile from)
        {
            var list = new ObjectPropertyList(this);
            GetProperties(list);

            var profile = HunterBestiaryEngine.GetProfile(from);
            
            // Если в профиле у этого моба уже есть хоть сколько-то очков прогресса
            int currentProgress = profile != null ? profile.GetProgress(m_CreatureId) : 0;

            if (currentProgress > 0)
            {
                list.Add(1070722, string.Format("<BASEFONT COLOR=#00E676>[Изучено: {0}/300 XP]<BASEFONT COLOR=#FFFFFF>", currentProgress));
            }
            else
            {
                list.Add(1070722, "<BASEFONT COLOR=#00E5FF>[Еще не изучалось]<BASEFONT COLOR=#FFFFFF>");
            }

            list.Terminate();
            from.Send(list);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
            writer.Write(m_CreatureId);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_CreatureId = reader.ReadString();

            if (ItemID == 0x1F14)
                ItemID = 0x1F5F;
        }
    }
}
