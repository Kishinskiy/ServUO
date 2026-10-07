using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.SkillHandlers;

namespace Server.Items
{
    public class CircuitTrapTrainingKit : Item, ICircuitTrap, IRemoveTrapTrainingKit
    {
        public override int LabelNumber => 1159014; // Circuit Trap Training Kit

        [Constructable]
        public CircuitTrapTrainingKit()
            : base(41875)
        {
        }

        public CircuitTrapTrainingKit(Serial serial)
            : base(serial)
        {
        }

        public int GumpTitle => 1159005; // <center>Trap Disarm Mechanism</center>
        public int GumpDescription => 1159006; // // <center>disarm the trap</center>
        public CircuitCount Count { get; private set; }

        public List<int> Path { get; set; } = new List<int>();
        public List<int> Progress { get; set; } = new List<int>();

        public bool CanDecipher => false;

        public void OnSelfClose(Mobile m)
        {
            if (Progress != null) Progress.Clear();
        }

        public void OnProgress(Mobile m, int pick)
        {
            m.SendSound(0x1F4);
        }

        public void OnFailed(Mobile m)
        {
            m.SendLocalizedMessage(1159013); // You fail to disarm the trap and reset it.
        }

        public void OnComplete(Mobile m)
        {
            m.SendLocalizedMessage(1159009); // You successfully disarm the trap!

            m.CheckTargetSkill(SkillName.RemoveTrap, this, 0, 100);
        }

        public void OnRemoveTrap(Mobile m)
        {
            if (m is PlayerMobile)
            {
                if (Path == null || Path.Count == 0)
                {
                    var skill = m.Skills[SkillName.RemoveTrap].Base;

                    if (skill < 80.0)
                        Count = CircuitCount.Nine;
                    else if (skill < 100.0)
                        Count = CircuitCount.Sixteen;
                    else
                        Count = CircuitCount.TwentyFive;
                }

                BaseGump.SendGump(new CircuitTrapGump((PlayerMobile)m, this));
            }
        }

        public override void OnDoubleClick(Mobile m)
        {
            if (m.InRange(GetWorldLocation(), 1))
                m.SendLocalizedMessage(
                    1159008); // That appears to be trapped, using the remove trap skill would yield better results...
        }

        public override void OnDelete()
        {
            var m = RootParent as Mobile;

            if (m != null)
                if (m.HasGump(typeof(CircuitTrapGump)))
                    m.CloseGump(typeof(CircuitTrapGump));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(Path.Count);
            for (var i = 0; i < Path.Count; i++) writer.Write(Path[i]);

            writer.Write(Progress.Count);
            for (var i = 0; i < Progress.Count; i++) writer.Write(Progress[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            var count = reader.ReadInt();

            for (var i = 0; i < count; i++) Path.Add(reader.ReadInt());

            count = reader.ReadInt();

            for (var i = 0; i < count; i++) Progress.Add(reader.ReadInt());
        }
    }
}
