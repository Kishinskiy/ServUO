using Server.Engines.Quests;
using Server.Multis;

namespace Server.Items
{
    public class BindingPole : Item, IGalleonFixture
    {
        public BaseQuest Quest { get; set; }

        public override int LabelNumber => 1116718;

        public BindingPole(BaseQuest quest) : base(5696)
        {
            Quest = quest;
            Movable = false;
        }

        public BindingPole(Serial serial) : base(serial)
        {
        }

        public BaseGalleon Galleon { get; set; }

        public override void Delete()
        {
            base.Delete();

            if (Quest != null)
                Quest.OnResign(false);

            if (Galleon != null)
                Galleon.RemoveFixture(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();
        }
    }
}
