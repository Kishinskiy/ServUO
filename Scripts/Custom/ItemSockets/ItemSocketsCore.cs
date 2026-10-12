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

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxSockets 
        { 
            get { return m_MaxSockets; } 
            set { m_MaxSockets = value; if (m_MaxSockets > 3) m_MaxSockets = 3; InvalidateParentProperties(); } 
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FilledSockets { get { return m_EmbeddedGemIDs.Count; } }
        public List<string> EmbeddedGemIDs { get { return m_EmbeddedGemIDs; } }

        public XmlItemSockets(ASerial serial) : base(serial) { }

        public XmlItemSockets()
        {
            Name = "ItemSockets";
            // Если нативная система сокетов включена в конфиге — генерируем случайные слоты
            m_MaxSockets = Server.Mobiles.XmlSpawner.SocketsEnabled ? Utility.RandomMinMax(0, 2) : 0;
            m_EmbeddedGemIDs = new List<string>();
        }

        public void RefreshSocketBonus(BaseWeapon weapon)
        {
            if (weapon == null) return;

            foreach (string gemID in m_EmbeddedGemIDs)
            {
                BaseSocketEffect effect = ItemSocketsCore.FindEffect(gemID);
                if (effect != null)
                    effect.ApplyEffect(weapon); 
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write(m_MaxSockets);
            writer.Write(m_EmbeddedGemIDs.Count);
            foreach (string gemID in m_EmbeddedGemIDs)
                writer.Write(gemID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_MaxSockets = reader.ReadInt();
            int count = reader.ReadInt();
            m_EmbeddedGemIDs = new List<string>();
            for (int i = 0; i < count; i++)
                m_EmbeddedGemIDs.Add(reader.ReadString());
        }
    }

    // ГЛОБАЛЬНЫЙ МЕНЕДЖЕР СИСТЕМЫ ИНКРУСТАЦИИ
    public static class ItemSocketsCore
    {
        private static readonly Dictionary<string, BaseSocketEffect> _registeredEffects = new Dictionary<string, BaseSocketEffect>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            // Регистрация модулей расширения камней
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
