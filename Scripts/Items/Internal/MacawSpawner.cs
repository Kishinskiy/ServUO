using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.Items
{
    public class MacawSpawner : Item
    {
        private static readonly Point3D[] _SpawnLocs =
        {
            new Point3D(491, 1863, 95),
            new Point3D(496, 1865, 85),
            new Point3D(496, 1852, 85),
            new Point3D(490, 1867, 75),
            new Point3D(494, 1859, 95),
            new Point3D(498, 1845, 75),
            new Point3D(502, 1859, 55),
            new Point3D(484, 1871, 55),
            new Point3D(476, 1867, 75),
            new Point3D(500, 1864, 65),
            new Point3D(484, 1865, 85),
            new Point3D(500, 1852, 65),
            new Point3D(493, 1847, 94),
            new Point3D(479, 1869, 65),
            new Point3D(502, 1871, 55),
            new Point3D(502, 1848, 55),
            new Point3D(498, 1860, 75),
            new Point3D(498, 1849, 75),
            new Point3D(500, 1845, 65),
            new Point3D(472, 1871, 55),
            new Point3D(495, 1869, 65),
            new Point3D(490, 1871, 55),
            new Point3D(478, 1865, 85)
        };

        public override int LabelNumber => 1124032; // foil sheet

        public List<BaseCreature> Spawn { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public MacawNest Addon { get; set; }

        public bool Moving { get; private set; }

        [Constructable]
        public MacawSpawner()
            : base(0x9C48)
        {
            Spawn = new List<BaseCreature>();
            Movable = false;
            Hue = 1281;

            Addon = new MacawNest();
            Addon.Foil = this;
        }

        public MacawSpawner(Serial serial)
            : base(serial)
        {
        }

        public override bool VerifyMove(Mobile from)
        {
            if (Visible && Map != null && Map != Map.Internal && Utility.RandomBool()) SpawnBirdies(from);

            return base.VerifyMove(from);
        }

        public void SpawnBirdies(Mobile from)
        {
            var count = Utility.RandomMinMax(3, 5);

            for (var i = 0; i < count; i++)
            {
                var p = Location;

                for (var j = 0; j < 10; j++)
                {
                    var x = Utility.RandomMinMax(X - 1, X + 1);
                    var y = Utility.RandomMinMax(Y - 1, Y + 2);
                    var z = Map.GetAverageZ(x, y);

                    if (Map.CanSpawnMobile(x, y, z))
                    {
                        p = new Point3D(x, y, z);
                        break;
                    }
                }

                BaseCreature macaw = new Macaw(this);
                macaw.MoveToWorld(p, Map);
                Spawn.Add(macaw);

                Visible = false;
                Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomMinMax(60, 90)), () => Visible = true);
            }

            Spawn.ForEach(s => s.Combatant = from);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (Location != oldLocation && Addon != null && !Moving)
                MoveToNest();

            base.OnLocationChange(oldLocation);
        }

        public override void OnMapChange()
        {
            if (Addon != null)
                Addon.Map = Map;

            base.OnMapChange();
        }

        private void MoveToNest()
        {
            Addon.MoveToWorld(Location, Map);
            Moving = true;
            MoveToWorld(new Point3D(Addon.X + 1, Addon.Y, Addon.Z + 9), Addon.Map);
            Moving = false;
        }

        public override void Delete()
        {
            base.Delete();

            if (Addon != null)
            {
                Addon.Foil = null;
                Addon.Delete();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(Addon);
            writer.Write(Spawn.Count);

            Spawn.ForEach(sp => writer.Write(sp));
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            Addon = reader.ReadItem() as MacawNest;

            Spawn = new List<BaseCreature>();
            var count = reader.ReadInt();

            for (var i = 0; i < count; i++)
            {
                var bc = reader.ReadMobile() as BaseCreature;

                if (bc != null)
                {
                    Spawn.Add(bc);

                    if (bc is Macaw)
                        ((Macaw)bc).MacawSpawner = this;
                }
            }

            if (Addon != null)
                Addon.Foil = this;
        }

        public static void Generate()
        {
            foreach (var pnt in _SpawnLocs)
            {
                var nest = new MacawNest();
                nest.MoveToWorld(pnt, Map.TerMur);
            }
        }
    }

    public class MacawNest : BaseAddon
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public MacawSpawner Foil { get; set; }

        [Constructable]
        public MacawNest()
        {
            AddComponent(new AddonComponent(3235), 0, 0, 0);
            AddComponent(new AddonComponent(3232), 0, 0, 0);
            AddComponent(new AddonComponent(3236), 1, 0, 0);
            AddComponent(new AddonComponent(3232), 1, -1, 0);
            AddComponent(new AddonComponent(3231), 0, -1, 0);
        }

        public MacawNest(Serial serial)
            : base(serial)
        {
        }

        public override void Delete()
        {
            base.Delete();

            if (Foil != null)
            {
                Foil.Addon = null;
                Foil.Delete();
            }
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
