using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Regions;
using Server.Targeting;

namespace Server.Custom.Farms
{
    public enum VegetableType
    {
        Onion,
        Potato,
        Cabbage,
        Carrot,
        Lettuce,
        Pumpkin
    }

    
    // =========================================================================
    // 2. УНИВЕРСАЛЬНЫЙ ТАРГЕТ ПОСАДКИ ДЛЯ СЕМЯН
    // =========================================================================
    public class FarmVegetableTarget : Target
    {
        private Item m_Seed;
        private VegetableType m_Type;

        public FarmVegetableTarget(Item seed, VegetableType type) : base(3, true, TargetFlags.None)
        {
            m_Seed = seed;
            m_Type = type;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Seed == null || m_Seed.Deleted || !m_Seed.IsChildOf(from.Backpack)) return;

            IPoint3D p = targeted as IPoint3D;
            if (p == null) return;

            Point3D loc = new Point3D(p.X, p.Y, p.Z);
            Map map = from.Map;

            Region reg = Region.Find(loc, map);
            if (!(reg is FarmRegion))
            {
                from.SendMessage("Эти семена прорастут только на подготовленной фермерской пашне!");
                return;
            }

            FarmRegion farmReg = (FarmRegion)reg;
            if (farmReg.Controller != null && farmReg.Controller.Owner != from &&
                from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("Вы не можете сажать семена на чужом поле!");
                return;
            }

            Static ground = targeted as Static;
            if (ground == null || ground.ItemID != 0x31F4)
            {
                from.SendMessage("Сажать нужно строго на вспаханную землю огорода.");
                return;
            }

            IPooledEnumerable eable = map.GetItemsInRange(loc, 0);
            foreach (Item item in eable)
            {
                if (item is FarmVegetablePlant)
                {
                    from.SendMessage("На этой грядке уже что-то растет!");
                    eable.Free();
                    return;
                }
            }

            eable.Free();

            FarmVegetablePlant plant = new FarmVegetablePlant(from, m_Type);
            plant.MoveToWorld(new Point3D(loc.X, loc.Y, loc.Z + 1), map);

            m_Seed.Consume();
            from.PlaySound(0x243);
            from.SendMessage("Вы успешно посадили семена.");
        }
    }

    // =========================================================================
    // УНИВЕРСАЛЬНЫЙ КУСТ С ПОДДЕРЖКОЙ ПОЛИВА И ФАЗ РОСТА
    // =========================================================================
    public class FarmVegetablePlant : Item
    {
        private Mobile m_Planter;
        private int m_Stage;
        private VegetableType m_VegType;
        private Timer m_GrowthTimer;
        private bool m_Watered;
        private int m_DryCount; // Счетчик пропущенных поливов
        private bool m_IsDead; // Статус: погибло ли растение

        private int m_DaysInStage; // Сколько дней растение успешно росло на ТЕКУЩЕЙ стадии


        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Planter
        {
            get { return m_Planter; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsDead
        {
            get { return m_IsDead; }
            set
            {
                m_IsDead = value;
                UpdateName();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Watered
        {
            get { return m_Watered; }
            set
            {
                if (m_IsDead) return; // Мертвое растение поливать бесполезно
                m_Watered = value;
                if (m_Watered) m_DryCount = 0; // Сбрасываем счетчик засухи при успешном поливе
                UpdateName();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Stage
        {
            get { return m_Stage; }
            set
            {
                m_Stage = value;
                InvalidateProperties();
            }
        }

        public FarmVegetablePlant(Mobile planter, VegetableType type) : base(0x18E5)
        {
            m_Planter = planter;
            m_VegType = type;
            m_Stage = 0;
            m_Watered = false;
            m_DryCount = 0;
            m_IsDead = false;
            Movable = false;

            UpdateName();

            m_GrowthTimer = Timer.DelayCall(TimeSpan.FromHours(2.0), TimeSpan.FromHours(2.0),
                new TimerCallback(GrowPerDay));
        }


        public void UpdateName()
        {
            if (m_IsDead)
            {
                Name = "Засохшее погибшее растение (Завяло)";
                return;
            }

            string baseName = GetVegNameString();

            if (m_Stage == 0) Name = $"{baseName} Plant (Phase 1)";
            else if (m_Stage == 1) Name = $"{baseName} Plant (Phase 2)";
            else if (m_Stage == 2) Name = $"Ripe {baseName} (Ready to Harvest)";

            if (!m_Watered && m_Stage < 2)
            {
                Name += " [Нужен полив!]";
            }
        }

        private string GetVegNameString()
        {
            switch (m_VegType)
            {
                case VegetableType.Onion: return "Onion";
                case VegetableType.Potato: return "Potato";
                case VegetableType.Cabbage: return "Cabbage";
                case VegetableType.Carrot: return "Carrot";
                case VegetableType.Lettuce: return "Lettuce";
                case VegetableType.Pumpkin: return "Pumpkin";
            }

            return "Vegetable";
        }

        private void GrowPerDay()
        {
            if (Deleted || m_IsDead) return;

            // 1. ПРОВЕРКА НА ЗАСУХУ
            if (!m_Watered)
            {
                m_DryCount++;

                // Растущие фазы (0 и 1) погибают, если их не поливать 3 игровых дня подряд (6 реальных часов)
                if (m_Stage < 2 && m_DryCount >= 3)
                {
                    WitherAndDie();
                    return;
                }
                // Спелый плод (фаза 2) сгниет, если его забросить на 14 игровых дней (28 реальных часов)
                else if (m_Stage == 2 && m_DryCount >= 14)
                {
                    WitherAndDie();
                    return;
                }

                if (m_Planter != null && m_Stage < 2)
                    m_Planter.SendMessage(
                        $"Ваш урожай ({GetVegNameString()}) засыхает! Вы не полили его за эти сутки!");

                UpdateName();
                return;
            }

            // 2. РАСТЕНИЕ ПОЛИТО ЗА СУТКИ
            m_Watered = false; // Сбрасываем полив на следующие сутки

            if (m_Stage < 2)
            {
                m_DaysInStage++; // Увеличиваем счетчик успешных дней на текущей стадии

                // Если растение успешно поливали 7 игровых дней (14 реальных часов) — оно переходит на следующую стадию!
                if (m_DaysInStage >= 7)
                {
                    m_Stage++;
                    m_DaysInStage = 0; // Сбрасываем дни для новой стадии

                    if (m_Stage == 1) // Переход на 2 фазу (ботва)
                    {
                        ItemID = 0xCB7;
                        if (m_VegType == VegetableType.Potato) Hue = 2411;
                        if (m_VegType == VegetableType.Pumpkin) ItemID = 0xCB5;
                        if (m_VegType == VegetableType.Cabbage) ItemID = 0xCB6;
                        if (m_VegType == VegetableType.Lettuce) ItemID = 0xC70;

                        if (m_Planter != null)
                            m_Planter.SendMessage(
                                $"Ваша грядка ({GetVegNameString()}) развилась и перешла во вторую фазу роста!");
                    }
                    else if (m_Stage == 2) // Полное созревание (суммарно прошло 14 дней)
                    {
                        switch (m_VegType)
                        {
                            case VegetableType.Pumpkin:
                                ItemID = 0xC6A;
                                Hue = 0;
                                break;
                            case VegetableType.Cabbage:
                                ItemID = 0xC7B;
                                Hue = 0;
                                break;
                            case VegetableType.Lettuce:
                                ItemID = 0xC70;
                                Hue = 0;
                                break;
                            case VegetableType.Carrot:
                                ItemID = 0xCB7;
                                Hue = 43;
                                break;
                            case VegetableType.Onion:
                                ItemID = 0x1846;
                                Hue = 0;
                                break;
                            case VegetableType.Potato:
                                ItemID = 0x1846;
                                Hue = 52;
                                break;
                        }

                        if (m_Planter != null)
                            m_Planter.SendMessage(
                                $"Поздравляем! Ваш урожай ({GetVegNameString()}) полностью созрел за 2 недели!");
                    }
                }
                else
                {
                    if (m_Planter != null)
                        m_Planter.SendMessage(
                            $"Прошли еще одни сутки. Ваш урожай ({GetVegNameString()}) успешно впитал воду.");
                }
            }

            UpdateName();
        }

        private void Grow()
        {
            if (Deleted || m_IsDead) return;

            // ЗЕМЛЯ СУХАЯ: Включаем логику увядания
            if (!m_Watered)
            {
                m_DryCount++; // Увеличиваем счетчик засухи

                // На 3-й пропущенный цикл полива растение окончательно засыхает и превращается в мусор
                if (m_DryCount >= 3)
                {
                    WitherAndDie();
                    return;
                }

                if (m_Planter != null)
                    m_Planter.SendMessage(
                        $"Ваш урожай ({GetVegNameString()}) засыхает без воды! Если не полить сейчас, он погибнет!");

                return;
            }

            // ЗЕМЛЯ ПОЛИТА: Растем дальше
            m_Stage++;
            m_Watered = false;

            if (m_Stage == 1)
            {
                switch (m_VegType)
                {
                    case VegetableType.Pumpkin: ItemID = 0xCB5; break;
                    case VegetableType.Cabbage: ItemID = 0xCB6; break;
                    case VegetableType.Lettuce: ItemID = 0xC70; break;
                    default: ItemID = 0xCB7; break;
                }

                if (m_VegType == VegetableType.Potato) Hue = 2411;
                if (m_Planter != null)
                    m_Planter.SendMessage($"Вашие посадки ({GetVegNameString()}) впитали воду и подросли.");
            }
            else if (m_Stage == 2)
            {
                switch (m_VegType)
                {
                    case VegetableType.Pumpkin:
                        ItemID = 0xC6A;
                        Hue = 0;
                        break;
                    case VegetableType.Cabbage:
                        ItemID = 0xC7B;
                        Hue = 0;
                        break;
                    case VegetableType.Lettuce:
                        ItemID = 0xC70;
                        Hue = 0;
                        break;
                    case VegetableType.Carrot:
                        ItemID = 0xCB7;
                        Hue = 43;
                        break;
                    case VegetableType.Onion:
                        ItemID = 0x1846;
                        Hue = 0;
                        break;
                    case VegetableType.Potato:
                        ItemID = 0x1846;
                        Hue = 52;
                        break;
                }

                if (m_Planter != null) m_Planter.SendMessage($"Ваш урожай ({GetVegNameString()}) полностью созрел!");
                m_GrowthTimer.Stop();
            }

            UpdateName();
        }

        // МЕТОД ГИБЕЛИ РАСТЕНИЯ
        private void WitherAndDie()
        {
            m_IsDead = true;
            m_GrowthTimer.Stop(); // Процесс роста останавливается навсегда

            ItemID = 0x0CBC; // Превращаем куст в сухую коричневую корягу
            Hue = 0; // Сбрасываем цвета

            UpdateName();

            if (m_Planter != null)
                m_Planter.SendMessage("Один из ваших кустов на ферме полностью засох и погиб из-за нехватки воды.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            // ПРОВЕРКА: Если растение мертвое, даем игроку очистить грядку и получить хворост
            if (m_IsDead)
            {
                // Проверка прав на регион фермы
                Region region = Region.Find(Location, Map);
                if (region is FarmRegion && ((FarmRegion)region).Controller != null &&
                    ((FarmRegion)region).Controller.Owner != from && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете трогать чужое засохшее растение.");
                    return;
                }

                Kindling dryTrash = new Kindling(); // Хворост для костра (идеальный мусор)
                if (from.Backpack.TryDropItem(from, dryTrash, false))
                {
                    from.SendMessage("Вы выдернули засохший куст и выбросили сухие ветки в инвентарь.");
                    from.PlaySound(0x13E);
                    this.Delete(); // Очищаем грядку
                }
                else
                {
                    dryTrash.Delete();
                    from.SendMessage("Ваш инвентарь полон, освободите место, чтобы убрать сухой куст.");
                }

                return;
            }

            // Дальше идет стандартный оригинальный код сбора живых овощей...
            if (m_Stage < 2)
            {
                from.SendMessage("Это растение еще не созрело. Ему нужно время.");
                return;
            }

            Region reg = Region.Find(Location, Map);
            if (reg is FarmRegion)
            {
                FarmRegion farmReg = (FarmRegion)reg;
                if (farmReg.Controller != null && farmReg.Controller.Owner != from &&
                    from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Вы не можете собирать чужой урожай!");
                    return;
                }
            }

            // ИЗМЕНЕНО: РАНДОМНЫЙ СБОР ПЛОДОВ ОТ 1 ДО 3 ШТУК
            int yieldCount = Utility.RandomMinMax(1, 3);
            bool totalSuccess = true;

            for (int i = 0; i < yieldCount; i++)
            {
                Item vegetable;
                if (m_VegType == VegetableType.Onion) vegetable = new Onion();
                else if (m_VegType == VegetableType.Cabbage) vegetable = new Cabbage();
                else if (m_VegType == VegetableType.Carrot) vegetable = new Carrot();
                else if (m_VegType == VegetableType.Lettuce) vegetable = new Lettuce();
                else if (m_VegType == VegetableType.Pumpkin) vegetable = new Pumpkin();
                else vegetable = new FarmPotato();

                if (!from.Backpack.TryDropItem(from, vegetable, false))
                {
                    vegetable.Delete();
                    totalSuccess = false;
                    break;
                }
            }

            if (totalSuccess)
            {
                from.SendMessage($"Вы успешно собрали богатый урожай в количестве {yieldCount} шт.!");
                from.PlaySound(0x13E);
                this.Delete();
            }
            else
            {
                from.SendMessage("Часть урожая не поместилась, так как ваш инвентарь переполнен! Освободите место.");
            }
        }

        public override void OnDelete()
        {
            if (m_GrowthTimer != null) m_GrowthTimer.Stop();
            base.OnDelete();
        }

        public FarmVegetablePlant(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)4); // Поднимаем версию сохранения до 4
            writer.Write(m_Planter);
            writer.Write(m_Stage);
            writer.Write((int)m_VegType);
            writer.Write(m_Watered);
            writer.Write(m_DryCount);
            writer.Write(m_IsDead);
            writer.Write(m_DaysInStage); // Сохраняем прожитые дни на текущей стадии
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Planter = reader.ReadMobile();
            m_Stage = reader.ReadInt();
            m_VegType = (VegetableType)reader.ReadInt();
            m_Watered = reader.ReadBool();
            m_DryCount = reader.ReadInt();
            m_IsDead = reader.ReadBool();
            if (version >= 4)
                m_DaysInStage = reader.ReadInt();
            else
                m_DaysInStage = 0;
// Восстановление таймеров с шагом в 2 реальных часа (1 игровые сутки)
            if (!m_IsDead)
                m_GrowthTimer = Timer.DelayCall(TimeSpan.FromHours(2.0), TimeSpan.FromHours(2.0),
                    new TimerCallback(GrowPerDay));
            UpdateName();
        }
    }
}
