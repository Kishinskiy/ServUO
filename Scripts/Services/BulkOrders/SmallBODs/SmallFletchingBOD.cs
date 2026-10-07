using System;
using System.Collections.Generic;
using Server.Engines.Craft;

namespace Server.Engines.BulkOrders
{
    public class SmallFletchingBOD : SmallBOD
    {
        public static double[] m_FletchingMaterialChances =
        {
            0.513718750, // None
            0.292968750, // Oak
            0.117187500, // Ash
            0.046875000, // Yew
            0.018750000, // Heartwood
            0.007500000, // Bloodwood
            0.003000000 // Frostwood
        };

        public override BODType BODType => BODType.Fletching;

        [Constructable]
        public SmallFletchingBOD()
        {
            SmallBulkEntry[] entries;
            var useMaterials = false;

            if (0.20 > Utility.RandomDouble())
            {
                entries = SmallBulkEntry.FletchingSmallsRegular;
            }
            else
            {
                useMaterials = true;
                entries = SmallBulkEntry.FletchingSmalls;
            }

            if (entries.Length > 0)
            {
                var entry = entries[Utility.Random(entries.Length)];

                var amountMax = Utility.RandomList(10, 15, 20);

                BulkMaterialType material;

                if (useMaterials)
                    material = GetRandomMaterial(BulkMaterialType.OakWood, m_FletchingMaterialChances);
                else
                    material = BulkMaterialType.None;

                var reqExceptional = false;

                if (useMaterials)
                    reqExceptional = Utility.RandomBool() || material == BulkMaterialType.None;

                Hue = 1425;
                AmountMax = amountMax;
                Type = entry.Type;
                Number = entry.Number;
                Graphic = entry.Graphic;
                RequireExceptional = reqExceptional;
                Material = material;
                GraphicHue = entry.Hue;
            }
        }

        public SmallFletchingBOD(int amountCur, int amountMax, Type type, int number, int graphic, bool reqExceptional,
            BulkMaterialType mat, int hue)
        {
            Hue = 1425;
            AmountMax = amountMax;
            AmountCur = amountCur;
            Type = type;
            Number = number;
            Graphic = graphic;
            RequireExceptional = reqExceptional;
            Material = mat;
            GraphicHue = hue;
        }

        public SmallFletchingBOD(Serial serial)
            : base(serial)
        {
        }

        private SmallFletchingBOD(SmallBulkEntry entry, BulkMaterialType material, int amountMax, bool reqExceptional)
        {
            Hue = 1425;
            AmountMax = amountMax;
            Type = entry.Type;
            Number = entry.Number;
            Graphic = entry.Graphic;
            RequireExceptional = reqExceptional;
            Material = material;
        }

        public static SmallFletchingBOD CreateRandomFor(Mobile m)
        {
            SmallBulkEntry[] entries;

            var theirSkill = BulkOrderSystem.GetBODSkill(m, SkillName.Fletching);
            var useMaterials = false;

            if (theirSkill < 30.0 || .20 > Utility.RandomDouble())
            {
                entries = SmallBulkEntry.FletchingSmallsRegular;
            }
            else
            {
                useMaterials = true;
                entries = SmallBulkEntry.FletchingSmalls;
            }

            if (entries.Length > 0)
            {
                int amountMax;

                if (theirSkill >= 70.1)
                    amountMax = Utility.RandomList(10, 15, 20, 20);
                else if (theirSkill >= 50.1)
                    amountMax = Utility.RandomList(10, 15, 15, 20);
                else
                    amountMax = Utility.RandomList(10, 10, 15, 20);

                var material = BulkMaterialType.None;

                if (useMaterials && theirSkill >= 70.1)
                    for (var i = 0; i < 20; ++i)
                    {
                        var check = GetRandomMaterial(BulkMaterialType.OakWood, m_FletchingMaterialChances);
                        var skillReq = GetRequiredSkill(check);

                        if (theirSkill >= skillReq)
                        {
                            material = check;
                            break;
                        }
                    }

                var excChance = 0.0;

                if (theirSkill >= 70.1)
                    excChance = (theirSkill + 80.0) / 200.0;

                var reqExceptional = useMaterials && excChance > Utility.RandomDouble();

                var system = DefBowFletching.CraftSystem;

                var validEntries = new List<SmallBulkEntry>();

                for (var i = 0; i < entries.Length; ++i)
                {
                    var item = system.CraftItems.SearchFor(entries[i].Type);

                    if (item != null)
                    {
                        var allRequiredSkills = true;
                        var chance = item.GetSuccessChance(m, null, system, false, ref allRequiredSkills);

                        if (allRequiredSkills && chance >= 0.0)
                        {
                            if (reqExceptional)
                                chance = item.GetExceptionalChance(system, chance, m);

                            if (chance > 0.0)
                                validEntries.Add(entries[i]);
                        }
                    }
                }

                if (validEntries.Count > 0)
                {
                    var entry = validEntries[Utility.Random(validEntries.Count)];
                    return new SmallFletchingBOD(entry, material, amountMax, reqExceptional);
                }
            }

            return null;
        }

        public override int ComputeFame()
        {
            return FletchingRewardCalculator.Instance.ComputeFame(this);
        }

        public override int ComputeGold()
        {
            return FletchingRewardCalculator.Instance.ComputeGold(this);
        }

        public override List<Item> ComputeRewards(bool full)
        {
            var list = new List<Item>();

            var rewardGroup =
                FletchingRewardCalculator.Instance.LookupRewards(FletchingRewardCalculator.Instance
                    .ComputePoints(this));

            if (rewardGroup != null)
            {
                if (full)
                {
                    for (var i = 0; i < rewardGroup.Items.Length; ++i)
                    {
                        var item = rewardGroup.Items[i].Construct();

                        if (item != null)
                            list.Add(item);
                    }
                }
                else
                {
                    var rewardItem = rewardGroup.AcquireItem();

                    if (rewardItem != null)
                    {
                        var item = rewardItem.Construct();

                        if (item != null)
                            list.Add(item);
                    }
                }
            }

            return list;
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
