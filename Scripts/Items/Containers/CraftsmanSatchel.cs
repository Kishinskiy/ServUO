using Reward = Server.Engines.Quests.BaseReward;

namespace Server.Items
{
    public class BaseCraftsmanSatchel : Backpack
    {
        public BaseCraftsmanSatchel()
        {
            Hue = Reward.SatchelHue();

            var count = 1;

            if (0.015 > Utility.RandomDouble())
                count = 2;

            var equipment = false;
            var jewlery = false;
            var talisman = false;

            while (Items.Count < count)
                if (0.33 > Utility.RandomDouble() && !talisman)
                {
                    DropItem(Loot.RandomTalisman());
                    talisman = true;
                }
                else if (0.4 > Utility.RandomDouble() && !equipment)
                {
                    DropItem(RandomItem());
                    equipment = true;
                }
                else if (0.88 > Utility.RandomDouble() && !jewlery)
                {
                    DropItem(Reward.Jewlery());
                    jewlery = true;
                }
        }

        public BaseCraftsmanSatchel(Serial serial)
            : base(serial)
        {
        }

        public virtual Item RandomItem()
        {
            return null;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class AlchemistCraftsmanSatchel : BaseCraftsmanSatchel
    {
        [Constructable]
        public AlchemistCraftsmanSatchel()
        {
            if (Items.Count < 2)
            {
                var recipe = Reward.AlchemyRecipe();

                if (recipe != null) DropItem(recipe);
            }
        }

        public AlchemistCraftsmanSatchel(Serial serial)
            : base(serial)
        {
        }

        public override Item RandomItem()
        {
            return Reward.RangedWeapon();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class FletcherCraftsmanSatchel : BaseCraftsmanSatchel
    {
        [Constructable]
        public FletcherCraftsmanSatchel()
        {
            if (Items.Count < 2)
            {
                var recipe = Reward.FletcherRecipe();

                if (recipe != null) DropItem(recipe);
            }

            var runic = Reward.FletcherRunic();

            if (runic != null) DropItem(runic);
        }

        public FletcherCraftsmanSatchel(Serial serial)
            : base(serial)
        {
        }

        public override Item RandomItem()
        {
            return Reward.RangedWeapon();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class TailorsCraftsmanSatchel : BaseCraftsmanSatchel
    {
        [Constructable]
        public TailorsCraftsmanSatchel()
        {
            if (Items.Count < 2)
            {
                var recipe = Reward.TailorRecipe();

                if (recipe != null) DropItem(recipe);
            }
        }

        public TailorsCraftsmanSatchel(Serial serial)
            : base(serial)
        {
        }

        public override Item RandomItem()
        {
            return Reward.Armor();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class SmithsCraftsmanSatchel : BaseCraftsmanSatchel
    {
        [Constructable]
        public SmithsCraftsmanSatchel()
        {
            if (Items.Count < 2)
            {
                var recipe = Reward.SmithRecipe();

                if (recipe != null) DropItem(recipe);
            }
        }

        public SmithsCraftsmanSatchel(Serial serial)
            : base(serial)
        {
        }

        public override Item RandomItem()
        {
            return Reward.Weapon();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class TinkersCraftsmanSatchel : BaseCraftsmanSatchel
    {
        [Constructable]
        public TinkersCraftsmanSatchel()
        {
            if (Items.Count < 2)
            {
                var recipe = Reward.TinkerRecipe();

                if (recipe != null) DropItem(recipe);
            }
        }

        public TinkersCraftsmanSatchel(Serial serial)
            : base(serial)
        {
        }

        public override Item RandomItem()
        {
            return Reward.Weapon();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class CarpentersCraftsmanSatchel : BaseCraftsmanSatchel
    {
        [Constructable]
        public CarpentersCraftsmanSatchel()
        {
            if (Items.Count < 2)
            {
                var recipe = Reward.CarpentryRecipe();

                if (recipe != null) DropItem(recipe);
            }

            var runic = Reward.CarpenterRunic();

            if (runic != null) DropItem(runic);

            var furniture = Reward.RandomFurniture();

            if (furniture != null) DropItem(furniture);
        }

        public CarpentersCraftsmanSatchel(Serial serial)
            : base(serial)
        {
        }

        public override Item RandomItem()
        {
            return Reward.Weapon();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
