using System;
using System.Collections;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.XmlSpawner2
{
    public class XmlMinionStrike : XmlAttachment
    {
        private readonly ArrayList MinionList = new ArrayList();
        private DateTime m_EndTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Chance { get; set; } = 5;

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Refractory { get; set; } = TimeSpan.FromSeconds(5);

        [CommandProperty(AccessLevel.GameMaster)]
        public string Minion { get; set; } = "Drake";


        // These are the various ways in which the message attachment can be constructed.
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlMinionStrike(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlMinionStrike(string minion)
        {
            Minion = minion;
            Expiration = TimeSpan.FromMinutes(30);
        }

        [Attachable]
        public XmlMinionStrike(string minion, int chance)
        {
            Chance = chance;
            Minion = minion;
            Expiration = TimeSpan.FromMinutes(30);
        }

        [Attachable]
        public XmlMinionStrike(string minion, int chance, double refractory)
        {
            Chance = chance;
            Refractory = TimeSpan.FromSeconds(refractory);
            Expiration = TimeSpan.FromMinutes(30);
            Minion = minion;
        }

        [Attachable]
        public XmlMinionStrike(string minion, int chance, double refractory, double expiresin)
        {
            Chance = chance;
            Expiration = TimeSpan.FromMinutes(expiresin);
            Refractory = TimeSpan.FromSeconds(refractory);
            Minion = minion;
        }

        public override void OnAttach()
        {
            base.OnAttach();

            if (AttachedTo is Mobile)
            {
                var m = AttachedTo as Mobile;
                Effects.PlaySound(m, m.Map, 516);
            }
        }


        // note that this method will be called when attached to either a mobile or a weapon
        // when attached to a weapon, only that weapon will do additional damage
        // when attached to a mobile, any weapon the mobile wields will do additional damage
        public override void OnWeaponHit(Mobile attacker, Mobile defender, BaseWeapon weapon, int damageGiven)
        {
            // if it is still refractory then return
            if (DateTime.UtcNow < m_EndTime) return;

            if (Chance <= 0 || Utility.Random(100) > Chance)
                return;

            if (defender != null && attacker != null)
            {
                // spawn a minion
                object o = null;
                try
                {
                    o = Activator.CreateInstance(SpawnerType.GetType(Minion));
                }
                catch
                {
                }

                if (o is BaseCreature)
                {
                    var b = o as BaseCreature;
                    b.MoveToWorld(attacker.Location, attacker.Map);

                    if (attacker is PlayerMobile)
                    {
                        b.Controlled = true;
                        b.ControlMaster = attacker;
                    }

                    b.Combatant = defender;

                    // add it to the list of controlled mobs
                    MinionList.Add(b);
                }
                else
                {
                    if (o is Item)
                        ((Item)o).Delete();
                    if (o is Mobile)
                        ((Mobile)o).Delete();
                    // bad minion specification so delete the attachment
                    Delete();
                }

                m_EndTime = DateTime.UtcNow + Refractory;
            }
        }

        public override void OnDelete()
        {
            base.OnDelete();

            if (AttachedTo is Mobile)
            {
                var m = AttachedTo as Mobile;
                if (!m.Deleted) Effects.PlaySound(m, m.Map, 958);
            }

            // delete the minions
            foreach (BaseCreature b in MinionList)
                if (b != null && !b.Deleted)
                    b.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(Chance);
            writer.Write(Minion);
            writer.Write(Refractory);
            writer.Write(m_EndTime - DateTime.UtcNow);
            writer.Write(MinionList.Count);
            foreach (BaseCreature b in MinionList)
                writer.Write(b);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            // version 0
            Chance = reader.ReadInt();
            Minion = reader.ReadString();
            Refractory = reader.ReadTimeSpan();
            var remaining = reader.ReadTimeSpan();
            m_EndTime = DateTime.UtcNow + remaining;
            var nminions = reader.ReadInt();
            for (var i = 0; i < nminions; i++)
            {
                var b = (BaseCreature)reader.ReadMobile();
                MinionList.Add(b);
            }
        }

        public override string OnIdentify(Mobile from)
        {
            string msg = null;

            if (Expiration > TimeSpan.Zero)
                msg = string.Format("Minion : {0} {1}% chance expires in {2} mins", Minion, Chance,
                    Expiration.TotalMinutes);
            else
                msg = string.Format("Minion : {0}", Minion);

            if (Refractory > TimeSpan.Zero)
                return string.Format("{0} : {1} secs between uses", msg, Refractory.TotalSeconds);

            return msg;
        }
    }
}
