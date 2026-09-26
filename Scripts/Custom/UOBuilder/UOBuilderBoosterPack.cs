using Server.Targeting;

namespace Server.Custom.UOBuilder
{
    public class UOBuilderBoosterPack : Item
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int BoostAmount { get; private set; } = UOBuilderConfig.BoostAmount;

        [Constructable]
        public UOBuilderBoosterPack() : base(0x14F0)
        {
            Name = "a booster pack";
            Hue = 1153;
            Weight = 1.0;
            LootType = LootType.Blessed;
        }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add($"ARaises building permit limits by {BoostAmount}.");
        }

        public UOBuilderBoosterPack(Serial serial) : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendMessage(53, "The boost pack must be in your backpack to use it.");

                return;
            }

            from.SendMessage(53, "Target the building permit to add boost pack.");

            from.Target = new BoostTarget(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            _ = reader.ReadInt();
        }

        internal void ConsumeBoost(Mobile from)
        {
            from.SendMessage(53, $"{BoostAmount} Boost added.");

            Delete();
        }

        private sealed class BoostTarget : Target
        {
            private readonly UOBuilderBoosterPack _boostPack;

            public BoostTarget(UOBuilderBoosterPack boostPack) : base(-1, false, TargetFlags.None)
            {
                _boostPack = boostPack;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (_boostPack.Deleted || !_boostPack.IsChildOf(from.Backpack))
                {
                    from.SendMessage(42, "The resource pack must remain in your backpack to use it.");

                    return;
                }

                if (!(targeted is UOBuilderPermit permit))
                {
                    from.SendMessage(42, "You must target a building permit.");

                    return;
                }

                if (!permit.TryAddBooster(from, _boostPack.BoostAmount))
                {
                    from.SendMessage(42, "The building permit must be in your backpack and belong to you.");

                    return;
                }

                _boostPack.ConsumeBoost(from);
            }
        }
    }
}
