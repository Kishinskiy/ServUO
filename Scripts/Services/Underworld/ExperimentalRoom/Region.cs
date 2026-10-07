using System.Xml;
using Server.Items;
using Server.Mobiles;

namespace Server.Regions
{
    public class ExperimentalRoomRegion : DungeonRegion
    {
        private static Rectangle2D m_Entrance = new Rectangle2D(994, 1114, 4, 4);

        private static readonly Rectangle2D[] m_RoomRecs =
        {
            new Rectangle2D(977, 1104, 15, 10), //RoomOne
            new Rectangle2D(977, 1092, 15, 9), //RoomTwo
            new Rectangle2D(977, 1074, 15, 10) //RoomThree
        };

        public ExperimentalRoomRegion(XmlElement xml, Map map, Region parent)
            : base(xml, map, parent)
        {
        }

        public override void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
            if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned))
            {
                foreach (var rec in m_RoomRecs)
                    if (rec.Contains(m.Location))
                    {
                        KickToEntrance(m);
                        var master = ((BaseCreature)m).GetMaster();

                        if (master != null && master.NetState != null)
                            master.SendLocalizedMessage(
                                1113472); // Your pet has been kicked out of the room. This is not a stable!
                    }
            }
            else if (m is PlayerMobile && m.AccessLevel == AccessLevel.Player)
            {
                if (m.Backpack == null)
                {
                    KickToEntrance(m);
                }
                else
                {
                    var item = m.Backpack.FindItemByType(typeof(ExperimentalGem));

                    if (item == null)
                        foreach (var rec in m_RoomRecs)
                            if (rec.Contains(m.Location))
                                KickToEntrance(m);
                }
            }
        }

        public void KickToEntrance(Mobile from)
        {
            if (from == null || from.Map == null)
                return;

            var x = Utility.RandomMinMax(m_Entrance.X, m_Entrance.X + m_Entrance.Width);
            var y = Utility.RandomMinMax(m_Entrance.Y, m_Entrance.Y + m_Entrance.Height);
            var z = from.Map.GetAverageZ(x, y);

            var loc = from.Location;
            var p = new Point3D(x, y, z);

            if (from is PlayerMobile)
                BaseCreature.TeleportPets(from, p, Map.TerMur);

            from.MoveToWorld(p, Map.TerMur);

            Effects.SendLocationParticles(EffectItem.Create(loc, from.Map, EffectItem.DefaultDuration), 0x3728, 10, 10,
                2023);
            Effects.SendLocationParticles(EffectItem.Create(p, from.Map, EffectItem.DefaultDuration), 0x3728, 10, 10,
                5023);
        }
    }
}
