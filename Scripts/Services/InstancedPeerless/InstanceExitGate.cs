//
//  X-RunUO - Ultima Online Server Emulator
//  Copyright (C) 2015 Pedro Pardal
//
//  This library is free software; you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation; either
//  version 3.0 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this program.
//

using Server.Gumps;

namespace Server.Items
{
    public class InstanceExitGate : Item
    {
        public override int LabelNumber => 1113495; // (Exit)

        public override bool ForceShowProperties => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D LocDest { get; set; }

        [Constructable]
        public InstanceExitGate()
            : this(Map.Internal, Point3D.Zero)
        {
        }

        [Constructable]
        public InstanceExitGate(Map mapDest, Point3D locDest)
            : base(0xF6C)
        {
            MapDest = mapDest;
            LocDest = locDest;

            Movable = false;
            Hue = 0x488;
            Light = LightType.Circle300;
        }

        public InstanceExitGate(Serial serial)
            : base(serial)
        {
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (!m.HasGump(typeof(ConfirmExitInstanceGump)))
                m.SendGump(new ConfirmExitInstanceGump(this));

            return base.OnMoveOver(m);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(MapDest);
            writer.Write(LocDest);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            /*int version = */
            reader.ReadInt();

            MapDest = reader.ReadMap();
            LocDest = reader.ReadPoint3D();
        }
    }
}
