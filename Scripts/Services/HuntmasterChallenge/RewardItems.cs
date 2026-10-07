using System;
using Server.Mobiles;

namespace Server.Items
{
    public class HarvestersBlade : ElvenSpellblade
    {
        public override int LabelNumber => 1114096; // Harvester's Blade

        [Constructable]
        public HarvestersBlade()
        {
            Hue = 1191;
            Attributes.SpellChanneling = 1;
        }

        public HarvestersBlade(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            if (v == 0)
                Hue = 1191;
        }
    }

    public class RangersGuildSash : BodySash
    {
        public override int LabelNumber => 1155744; // Member of the Skara Brae Ranger's Guild

        [Constructable]
        public RangersGuildSash()
        {
            LootType = LootType.Blessed;
        }

        public RangersGuildSash(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    public class GargishRangersGuildSash : GargishSash
    {
        public override int LabelNumber => 1155744; // Member of the Skara Brae Ranger's Guild

        [Constructable]
        public GargishRangersGuildSash()
        {
            LootType = LootType.Blessed;
        }

        public GargishRangersGuildSash(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    public class HuntmastersRewardTitleDeed : BaseRewardTitleDeed
    {
        public override TextDefinition Title => new TextDefinition(1155727); // Huntmaster's Champion

        [Constructable]
        public HuntmastersRewardTitleDeed()
        {
        }

        public HuntmastersRewardTitleDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();
        }
    }

    [Flipable(18080, 18081)]
    public class HornOfPlenty : Item, IUsesRemaining
    {
        private int m_UsesRemaining;
        public override int LabelNumber => 1153503; // Horn of Plenty

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextRecharge { get; set; }

        [Constructable]
        public HornOfPlenty() : base(18080)
        {
            UsesRemaining = 10;
        }

        public HornOfPlenty(Serial serial)
            : base(serial)
        {
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

        public bool ShowUsesRemaining
        {
            get => true;
            set { }
        }

        //TODO: add pub 84, 88 and 95 shit
        public override void OnDoubleClick(Mobile from)
        {
            if (m_UsesRemaining > 0)
            {
                Item item = null;

                switch (Utility.Random(10))
                {
                    case 0: item = new SweetPotatoPie(); break;
                    case 1: item = new MashedSweetPotatoes(); break;
                    case 2: item = new BasketOfRolls(); break;
                    case 3: item = new TurkeyPlatter(); break;
                    case 4:
                        BaseCreature bc = new Turkey(true);
                        if (0.10 > Utility.RandomDouble())
                            bc.Name = "Mister Gobbles";
                        bc.MoveToWorld(from.Location, from.Map);
                        from.SendLocalizedMessage(1153512); //That one's not cooked!
                        break;
                    case 5:
                        new InternalTimer(from);
                        from.Frozen = true;
                        break;
                    case 6: item = new PottedCoffeePlant(); break;
                    case 7: item = new RoastingPigOnASpitDeed(); break;
                    case 8: item = new FormalDiningTableDeed(); break;
                    case 9: item = new BuffetTableDeed(); break;
                }

                if (item != null)
                {
                    if (from.Backpack == null || !from.Backpack.TryDropItem(from, item, false))
                        item.MoveToWorld(from.Location, from.Map);

                    UsesRemaining--;
                }
            }
        }

        public override void AddUsesRemainingProperties(ObjectPropertyList list)
        {
            if (ShowUsesRemaining)
                list.Add(1049116, m_UsesRemaining.ToString()); // [ Charges: ~1_CHARGES~ ]
        }

        private void CheckRecharge()
        {
            if (DateTime.UtcNow.Month == 11 && UsesRemaining < 10 && NextRecharge < DateTime.UtcNow)
            {
                UsesRemaining++;
                NextRecharge = DateTime.UtcNow + TimeSpan.FromDays(1);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1);

            writer.Write(NextRecharge);
            writer.Write(m_UsesRemaining);

            CheckRecharge();
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var v = reader.ReadInt();

            switch (v)
            {
                case 1:
                    NextRecharge = reader.ReadDateTime();
                    goto case 0;
                case 0:
                    m_UsesRemaining = reader.ReadInt();
                    break;
            }
        }

        private class InternalTimer : Timer
        {
            private readonly Mobile m_From;
            private int m_Ticks;

            public InternalTimer(Mobile from)
                : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
            {
                m_From = from;
                Start();
            }

            protected override void OnTick()
            {
                m_Ticks++;

                if (m_Ticks % 3 == 0)
                    m_From.Say(1153513); // * ZzzzZzzzZzzzZ *

                if (m_Ticks == 10)
                {
                    Stop();
                    m_From.Frozen = false;
                }
            }
        }
    }

    public class HarvestersAxe : TwoHandedAxe
    {
        private int _Charges;
        public override int LabelNumber => 1158774; // Harvester's Axe

        [CommandProperty(AccessLevel.GameMaster)]
        public int Charges
        {
            get => _Charges;
            set
            {
                _Charges = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public HarvestersAxe()
        {
            Charges = 1000;
        }

        public HarvestersAxe(Serial serial)
            : base(serial)
        {
        }

        public override void AddWeightProperty(ObjectPropertyList list)
        {
            base.AddWeightProperty(list);
            list.Add(1158775); // * Magically Chops Logs into Boards *
            list.Add(1060741, _Charges.ToString()); // charges: 
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);

            writer.Write(_Charges);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            var version = reader.ReadInt();

            _Charges = reader.ReadInt();
        }
    }
}
