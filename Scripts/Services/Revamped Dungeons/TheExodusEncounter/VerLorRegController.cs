using Server.Mobiles;

namespace Server.Engines.Exodus
{
    public class VerLorRegController : Item
    {
        private static bool m_Active;

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool Active
        {
            get => m_Active;
            set
            {
                if (value) Start();
                else Stop();
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static ClockworkExodus Mobile { get; set; }

        [CommandProperty(AccessLevel.Administrator)]
        public static VerLorRegController IlshenarInstance { get; set; }

        public VerLorRegController(Map map) : base(7107)
        {
            Name = "Ver Lor Reg Controller";
            Visible = false;
            Movable = false;

            Start();
        }

        public VerLorRegController(Serial serial)
            : base(serial)
        {
        }

        public static void Initialize()
        {
            if (IlshenarInstance == null)
            {
                IlshenarInstance = new VerLorRegController(Map.Ilshenar);
                IlshenarInstance.MoveToWorld(new Point3D(849, 648, -40), Map.Ilshenar);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(m_Active);
            writer.WriteMobile(Mobile);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            m_Active = reader.ReadBool();
            Mobile = (ClockworkExodus)reader.ReadMobile();

            if (Map == Map.Ilshenar)
                IlshenarInstance = this;
        }

        public static void Start()
        {
            if (m_Active)
                return;

            m_Active = true;

            if (Mobile == null)
            {
                var m = new ClockworkExodus();
                m.Home = new Point3D(854, 642, -40);
                m.RangeHome = 4;
                m.MoveToWorld(new Point3D(854, 642, -40), Map.Ilshenar);
                Mobile = m;
            }
        }

        public static void Stop()
        {
            if (!m_Active)
                return;

            m_Active = false;
            Mobile.Delete();
            Mobile = null;
        }
    }
}
