using System;
using Server.Engines.Craft;
using Server.Engines.Distillation;

namespace Server.Items
{
    public class LiquorBarrel : Item, ICraftable
    {
        private Mobile m_Crafter;
        private Mobile m_Distiller;
        private bool m_Exceptional;
        private bool m_IsStrong;
        private string m_Label;
        private Liquor m_Liquor;
        private DateTime m_MaturationBegin;
        private int m_UsesRemaining;

        [CommandProperty(AccessLevel.GameMaster)]
        public Liquor Liquor
        {
            get => m_Liquor;
            set
            {
                BeginDistillation(value);
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime MaturationBegin
        {
            get => m_MaturationBegin;
            set
            {
                m_MaturationBegin = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MutrationDuration { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Label
        {
            get => m_Label;
            set
            {
                m_Label = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsStrong
        {
            get => m_IsStrong;
            set
            {
                m_IsStrong = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get => m_UsesRemaining;
            set
            {
                m_UsesRemaining = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Exceptional
        {
            get => m_Exceptional;
            set
            {
                m_Exceptional = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get => m_Crafter;
            set
            {
                m_Crafter = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Distiller
        {
            get => m_Distiller;
            set
            {
                m_Distiller = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool IsMature => m_Liquor != Liquor.None && (MutrationDuration == TimeSpan.MinValue ||
                                                                    m_MaturationBegin + MutrationDuration <
                                                                    DateTime.UtcNow);

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsEmpty => m_Liquor == Liquor.None;

        public override int LabelNumber =>
            m_UsesRemaining == 0 || m_Liquor == Liquor.None ? 1150816 : 1150807; // liquor barrel

        public override double DefaultWeight => 5.0;

        [Constructable]
        public LiquorBarrel()
            : base(4014)
        {
            m_Liquor = Liquor.None;
            m_UsesRemaining = 0;
        }

        public LiquorBarrel(Serial serial)
            : base(serial)
        {
        }

        public virtual int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            ITool tool, CraftItem craftItem, int resHue)
        {
            if (quality >= 2)
            {
                m_Exceptional = true;

                if (makersMark)
                    m_Crafter = from;
            }

            if (typeRes == null)
                typeRes = craftItem.Resources.GetAt(0).ItemType;

            var resource = CraftResources.GetFromType(typeRes);
            Hue = CraftResources.GetHue(resource);

            return quality;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack) && m_UsesRemaining > 0)
            {
                if (IsMature)
                {
                    var bottle = new BottleOfLiquor(m_Liquor, m_Label, m_IsStrong, m_Distiller);

                    if (from.Backpack == null || !from.Backpack.TryDropItem(from, bottle, false))
                    {
                        bottle.Delete();
                        from.SendLocalizedMessage(500720); // You don't have enough room in your backpack!
                    }
                    else
                    {
                        from.PlaySound(0x240);
                        from.SendMessage("You pour the liquior into a bottle and place it in your backpack.");
                        m_UsesRemaining--;
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1150806); // You need to wait until the liquor in the barrel has matured.

                    if (DateTime.UtcNow < m_MaturationBegin + MutrationDuration)
                    {
                        var remaining = m_MaturationBegin + MutrationDuration - DateTime.UtcNow;
                        if (remaining.TotalDays > 0)
                            from.SendLocalizedMessage(1150814,
                                string.Format("{0}\t{1}", remaining.Days.ToString(), remaining.Hours.ToString()));
                        else
                            from.SendLocalizedMessage(1150813, remaining.TotalHours.ToString());
                    }
                }
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.Name); // Crafted By: ~1_Name~

            if (m_Exceptional)
                list.Add(1018303); // Exceptional

            if (!IsEmpty)
            {
                if (IsMature)
                    list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~

                list.Add(1150805, m_MaturationBegin.ToShortDateString()); // start date: ~1_NAME~

                var cliloc = IsMature ? 1150804 : 1150812; // maturing: ~1_NAME~ / // matured: ~1_NAME~

                if (m_Label == null)
                    list.Add(cliloc, string.Format("#{0}", DistillationSystem.GetLabel(m_Liquor, m_IsStrong)));
                else
                    list.Add(cliloc, m_Label);

                list.Add(1150454,
                    string.Format("#{0}", DistillationSystem.GetLabel(m_Liquor, m_IsStrong))); // Liquor Type: ~1_TYPE~

                if (m_Distiller != null)
                    list.Add(1150679, m_Distiller.Name); // Distiller: ~1_NAME~
            }
        }

        public void BeginDistillation(Liquor liquor)
        {
            TimeSpan ts;

            if (liquor == Liquor.Spirytus || liquor == Liquor.Akvavit)
                ts = TimeSpan.MinValue;
            else
                ts = DistillationSystem.MaturationPeriod;

            BeginDistillation(liquor, ts, m_Label, m_IsStrong, m_Distiller);
        }

        public void BeginDistillation(Liquor liquor, TimeSpan duration, string label, bool isStrong, Mobile distiller)
        {
            m_Liquor = liquor;
            MutrationDuration = duration;
            m_Label = label;
            m_IsStrong = isStrong;
            m_Distiller = distiller;
            m_MaturationBegin = DateTime.UtcNow;
            m_UsesRemaining = m_Exceptional ? 20 : 10;

            InvalidateProperties();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write((int)m_Liquor);
            writer.Write(m_MaturationBegin);
            writer.Write(MutrationDuration);
            writer.Write(m_Label);
            writer.Write(m_IsStrong);
            writer.Write(m_UsesRemaining);
            writer.Write(m_Exceptional);
            writer.Write(m_Crafter);
            writer.Write(m_Distiller);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_Liquor = (Liquor)reader.ReadInt();
            m_MaturationBegin = reader.ReadDateTime();
            MutrationDuration = reader.ReadTimeSpan();
            m_Label = reader.ReadString();
            m_IsStrong = reader.ReadBool();
            m_UsesRemaining = reader.ReadInt();
            m_Exceptional = reader.ReadBool();
            m_Crafter = reader.ReadMobile();
            m_Distiller = reader.ReadMobile();
        }
    }
}
