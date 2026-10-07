namespace Server.Items
{
    public class JewelryBoxFilter
    {
        private bool m_Bracelet;

        private bool m_Earrings;

        private bool m_Necklace;

        private bool m_Ring;

        private bool m_Talisman;
        public bool IsDefault => !Ring && !Bracelet && !Earrings && !Necklace && !Talisman;

        public bool Ring
        {
            get => m_Ring;
            set
            {
                if (value)
                    Clear();

                m_Ring = value;
            }
        }

        public bool Bracelet
        {
            get => m_Bracelet;
            set
            {
                if (value)
                    Clear();

                m_Bracelet = value;
            }
        }

        public bool Earrings
        {
            get => m_Earrings;
            set
            {
                if (value)
                    Clear();

                m_Earrings = value;
            }
        }

        public bool Necklace
        {
            get => m_Necklace;
            set
            {
                if (value)
                    Clear();

                m_Necklace = value;
            }
        }

        public bool Talisman
        {
            get => m_Talisman;
            set
            {
                if (value)
                    Clear();

                m_Talisman = value;
            }
        }

        public JewelryBoxFilter()
        {
        }

        public JewelryBoxFilter(GenericReader reader)
        {
            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    Ring = reader.ReadBool();
                    Bracelet = reader.ReadBool();
                    Earrings = reader.ReadBool();
                    Necklace = reader.ReadBool();
                    Talisman = reader.ReadBool();
                    break;
            }
        }

        public void Clear()
        {
            m_Ring = false;
            m_Bracelet = false;
            m_Earrings = false;
            m_Necklace = false;
            m_Talisman = false;
        }

        public void Serialize(GenericWriter writer)
        {
            if (IsDefault)
            {
                writer.Write(0);
            }
            else
            {
                writer.Write(1);

                writer.Write(Ring);
                writer.Write(Bracelet);
                writer.Write(Earrings);
                writer.Write(Necklace);
                writer.Write(Talisman);
            }
        }
    }
}
