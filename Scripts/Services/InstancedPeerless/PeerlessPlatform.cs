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

using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Items;

namespace Server.Engines.InstancedPeerless
{
    public abstract class PeerlessPlatform : BaseAddon
    {
        private readonly List<PeerlessKeyBrazier> m_Braziers = new List<PeerlessKeyBrazier>();
        private readonly List<PeerlessInstance> m_Instances = new List<PeerlessInstance>();
        private readonly Queue<List<Mobile>> m_PartyQueue = new Queue<List<Mobile>>();

        private Timer m_KeyExpireTimer;
        public abstract Type KeyType { get; }
        public abstract Type BossType { get; }

        public virtual int OfferGumpTitle => 1113737; // Monster's Lair

        public virtual int OfferGumpDesc =>
            1113738; // Your party has gained entrance to a monster's lair. You may choose to join the fight or stay away.

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Summoner { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D ExitLocation { get; set; }

        public override bool ShareHue => false;

        public PeerlessPlatform()
        {
            AddInstances();
            AddBraziers();
        }

        public PeerlessPlatform(Serial serial)
            : base(serial)
        {
        }

        public abstract void AddInstances();
        public abstract void AddBraziers();

        public void Validate()
        {
            if (m_KeyExpireTimer != null)
                m_KeyExpireTimer.Stop();

            foreach (var brazier in m_Braziers)
                if (!Validate(brazier))
                {
                    m_KeyExpireTimer = Timer.DelayCall(TimeSpan.FromMinutes(1.0), delegate { Clear(false); });
                    return;
                }

            var party = GetParty(Summoner);
            List<Mobile> invalid;

            if (!ValidateMembers(party, out invalid))
            {
                Summoner.SendLocalizedMessage(
                    1113574); // Your party cannot join the queue because the following members have already registered with another group:

                foreach (var m in invalid)
                    Summoner.SendMessage(m.Name);

                Clear(true);
            }
            else
            {
                var instance = AcquireEmptyInstance();

                if (instance == null)
                {
                    m_PartyQueue.Enqueue(party);

                    Summoner.SendLocalizedMessage(
                        1113575); // Your party has been successfully added to the queue for this instance.
                }
                else
                {
                    instance.Activate();

                    new InstanceEnterGate(instance, party).MoveToWorld(new Point3D(X, Y, Z + 5), Map);
                }

                Clear(false);
            }
        }

        private void Clear(bool returnKeys)
        {
            foreach (var brazier in m_Braziers)
            {
                var key = brazier.Key;

                if (key == null)
                    continue;

                if (returnKeys)
                {
                    key.Movable = true;
                    Summoner.PlaceInBackpack(key);
                }
                else
                {
                    key.Delete();
                }

                brazier.Key = null;
            }

            if (returnKeys)
                Summoner.SendLocalizedMessage(1113576); // Your sacrificial keys have been returned to you.

            Summoner = null;
        }

        private bool Validate(PeerlessKeyBrazier brazier)
        {
            return brazier.Key != null && !brazier.Key.Deleted;
        }

        private bool ValidateMembers(List<Mobile> party, out List<Mobile> invalid)
        {
            invalid = new List<Mobile>();

            foreach (var otherParty in m_PartyQueue)
            foreach (var m in otherParty)
                if (party.Contains(m))
                    invalid.Add(m);

            return invalid.Count == 0;
        }

        private List<Mobile> GetParty(Mobile from)
        {
            var list = new List<Mobile>();
            var p = from.Party as Party;

            if (p != null)
                foreach (var pmInfo in p.Members)
                    list.Add(pmInfo.Mobile);
            else
                list.Add(from);

            return list;
        }

        private PeerlessInstance AcquireEmptyInstance()
        {
            foreach (var instance in m_Instances)
                if (instance.State == InstanceState.Available)
                    return instance;

            return null;
        }

        protected void AddInstance(int x, int y, int z, Map map, Point3D entranceLoc, Point3D bossSpawnLoc,
            Rectangle2D regionBounds)
        {
            var light = new AddonComponent(0x1ECF);

            m_Instances.Add(new PeerlessInstance(this, map, light, entranceLoc, bossSpawnLoc, regionBounds));

            AddComponent(light, x, y, z);
        }

        protected void AddBrazier(int x, int y, int z)
        {
            var brazier = new PeerlessKeyBrazier(this);

            m_Braziers.Add(brazier);

            AddComponent(brazier, x, y, z);
        }

        public void OnFreeInstance(PeerlessInstance instance)
        {
            if (m_PartyQueue.Count > 0)
            {
                var party = m_PartyQueue.Dequeue();

                instance.Activate();

                foreach (var member in party)
                    if (member.Region.IsPartOf("Stygian Abyss"))
                    {
                        member.SendGump(new RejoinInstanceGump(instance, OfferGumpTitle, OfferGumpDesc));

                        Timer.DelayCall(TimeSpan.FromMinutes(1.0),
                            delegate { member.CloseGump(typeof(RejoinInstanceGump)); });
                    }
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Clear(false);

            if (m_KeyExpireTimer != null)
                m_KeyExpireTimer.Stop();

            foreach (var instance in m_Instances)
                instance.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(ExitLocation);

            writer.Write(m_Braziers.Count);

            for (var i = 0; i < m_Braziers.Count; i++)
                writer.WriteItem(m_Braziers[i]);

            writer.Write(m_Instances.Count);

            for (var i = 0; i < m_Instances.Count; i++)
                m_Instances[i].Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 0:
                {
                    ExitLocation = reader.ReadPoint3D();

                    var braziers = reader.ReadInt();

                    for (var i = 0; i < braziers; i++)
                        m_Braziers.Add(reader.ReadItem<PeerlessKeyBrazier>());

                    var instances = reader.ReadInt();

                    for (var i = 0; i < instances; i++)
                        m_Instances.Add(new PeerlessInstance(reader));

                    break;
                }
            }
        }
    }
}
