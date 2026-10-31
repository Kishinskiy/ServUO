using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.XmlSpawner2;

namespace Server.Custom.ItemSockets
{
    // АТТАЧМЕНТ СЛОТОВ: Хранит данные о гнездах и вставленных камнях
    public class XmlItemSockets : XmlAttachment
    {
        private int m_MaxSockets;
        private List<string> m_EmbeddedGemIDs;
        private List<double> m_EmbeddedGemScales; // 🔥 НОВОЕ: Список для сохранения коэффициента силы каждого камня!

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxSockets
        {
            get { return m_MaxSockets; }
            set { m_MaxSockets = value; if (m_MaxSockets > 3) m_MaxSockets = 3; InvalidateParentProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FilledSockets { get { return m_EmbeddedGemIDs.Count; } }
        public List<string> EmbeddedGemIDs { get { return m_EmbeddedGemIDs; } }
        public List<double> EmbeddedGemScales { get { return m_EmbeddedGemScales; } } // Геттер для ювелирных инструментов

        public XmlItemSockets(ASerial serial) : base(serial) { }

        public XmlItemSockets()
        {
            Name = "ItemSockets";
            m_MaxSockets = Server.Mobiles.XmlSpawner.SocketsEnabled ? Utility.RandomMinMax(0, 2) : 0;
            m_EmbeddedGemIDs = new List<string>();
            m_EmbeddedGemScales = new List<double>(); // Инициализация
        }

        // 🔥 МОДИФИКАЦИЯ: Пересчитывает бонусы с учетом коэффициента навыка из базы данных
        public void RefreshSocketBonus(BaseWeapon weapon)
        {
            if (weapon == null) return;

            for (int i = 0; i < m_EmbeddedGemIDs.Count; i++)
            {
                string gemID = m_EmbeddedGemIDs[i];
                // Защита: если вещь старая и шкалы нет — ставим дефолт x1.0
                double scale = i < m_EmbeddedGemScales.Count ? m_EmbeddedGemScales[i] : 1.0;

                BaseSocketEffect effect = ItemSocketsCore.FindEffect(gemID);
                if (effect != null)
                {
                    effect.ApplyDynamicEffect(weapon, scale); // Вызов динамического баффа!
                }
            }
        }

        // 🔥 НОВЫЙ МЕТОД: Позволяет безопасно снять бонусы перед обновлением или извлечением гемов
        public void RemoveAllSocketBonuses(BaseWeapon weapon)
        {
            if (weapon == null) return;

            for (int i = 0; i < m_EmbeddedGemIDs.Count; i++)
            {
                string gemID = m_EmbeddedGemIDs[i];
                double scale = i < m_EmbeddedGemScales.Count ? m_EmbeddedGemScales[i] : 1.0;

                BaseSocketEffect effect = ItemSocketsCore.FindEffect(gemID);
                if (effect != null)
                {
                    effect.RemoveDynamicEffect(weapon, scale);
                }
            }
        }

        // ВЕЧНОЕ СОХРАНЕНИЕ НА ДИСК С ПОДДЕРЖКОЙ ВЕРСИОННОСТИ (Version 1)
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // Повышаем версию сериализации с 0 до 1

            writer.Write(m_MaxSockets);
            writer.Write(m_EmbeddedGemIDs.Count);
            
            // Синхронно пишем ID камня и его персональный коэффициент силы
            for (int i = 0; i < m_EmbeddedGemIDs.Count; i++)
            {
                writer.Write(m_EmbeddedGemIDs[i]);
                writer.Write(m_EmbeddedGemScales[i]);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_MaxSockets = reader.ReadInt();
            int count = reader.ReadInt();
            
            m_EmbeddedGemIDs = new List<string>();
            m_EmbeddedGemScales = new List<double>();

            for (int i = 0; i < count; i++)
            {
                m_EmbeddedGemIDs.Add(reader.ReadString());
                if (version >= 1)
                {
                    m_EmbeddedGemScales.Add(reader.ReadDouble()); // Считываем сохраненный коэффициент создателя
                }
                else
                {
                    m_EmbeddedGemScales.Add(1.0); // Дефолт для вещей, созданных до этого апдейта
                }
            }
        }
    }

    // ГЛОБАЛЬНЫЙ МЕНЕДЖЕР СИСТЕМЫ ИНКРУСТАЦИИ
    public static class ItemSocketsCore
    {
        private static readonly Dictionary<string, BaseSocketEffect> _registeredEffects = new Dictionary<string, BaseSocketEffect>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            // Регистрация модулей расширения камней
            RegisterEffect(new GemEffectAmethyst());
            RegisterEffect(new GemEffectTourmaline());
            RegisterEffect(new GemEffectCitrine());
            RegisterEffect(new GemEffectAmber());
            RegisterEffect(new GemEffectBlueDiamond());
            RegisterEffect(new GemEffectRuby());
            RegisterEffect(new GemEffectSapphire());
            RegisterEffect(new GemEffectEmerald());
            RegisterEffect(new GemEffectDiamond());
            RegisterEffect(new GemEffectVampiric());
        }

        public static void RegisterEffect(BaseSocketEffect effect)
        {
            if (effect != null && !_registeredEffects.ContainsKey(effect.UniqueID))
                _registeredEffects[effect.UniqueID] = effect;
        }

        public static BaseSocketEffect FindEffect(string uniqueID)
        {
            if (string.IsNullOrEmpty(uniqueID)) return null;
            _registeredEffects.TryGetValue(uniqueID, out BaseSocketEffect effect);
            return effect;
        }
    }
}
