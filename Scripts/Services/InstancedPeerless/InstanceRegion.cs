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
using Server.Spells.Chivalry;
using Server.Spells.Fourth;

namespace Server.Engines.InstancedPeerless
{
    public class InstanceRegion : Region
    {
        public PeerlessInstance Owner { get; }

        public InstanceRegion(PeerlessInstance instance)
            : base(null, instance.Map, Find(instance.EntranceLocation, instance.Map), instance.RegionBounds)
        {
            Owner = instance;

            Register();
        }

        public static void Initialize()
        {
            EventSink.Logout += EventSink_Logout;
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (s is RecallSpell || s is SacredJourneySpell)
            {
                m.SendLocalizedMessage(501802); // Thy spell doth not appear to work...
                return false;
            }

            return base.OnBeginSpellCast(m, s);
        }

        public override TimeSpan GetLogoutDelay(Mobile m)
        {
            return TimeSpan.FromMinutes(10.0);
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            var from = e.Mobile;

            var region = from.Region as InstanceRegion;

            if (region != null)
                region.Owner.Kick(from);
        }

        public override void OnExit(Mobile m)
        {
            Owner.RemoveFighter(m);
        }
    }
}
