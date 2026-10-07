using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.XmlSpawner2
{
    public class XmlUse : XmlAttachment
    {
        public DateTime m_EndTime;


        [CommandProperty(AccessLevel.GameMaster)]
        public bool TargetingEnabled { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowCarried { get; set; } = true;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RequireLOS { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxRange { get; set; } = 3;

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxTargetRange { get; set; } = 30;

        [CommandProperty(AccessLevel.GameMaster)]
        public int NUses { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxUses { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Refractory { get; set; } = TimeSpan.Zero;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BlockDefaultUse { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Condition { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TargetCondition { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TargetingAction { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TargetFailureAction { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string SuccessAction { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string FailureAction { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RefractoryAction { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string MaxUsesAction { get; set; }

        public bool CheckMaxUses
        {
            get
            {
                // is there a use limit?
                if (MaxUses > 0 && NUses >= MaxUses) return false;

                return true;
            }
        }

        public bool CheckRefractory
        {
            get
            {
                // is there a refractory limit?
                // if it is still refractory then return
                if (Refractory > TimeSpan.Zero && DateTime.UtcNow < m_EndTime) return false;

                return true;
            }
        }

        public XmlUse(ASerial serial)
            : base(serial)
        {
        }

        [Attachable]
        public XmlUse()
        {
        }

        [Attachable]
        public XmlUse(int maxuses)
        {
            MaxUses = maxuses;
        }

        [Attachable]
        public XmlUse(int maxuses, double refractory)
        {
            MaxUses = maxuses;
            Refractory = TimeSpan.FromSeconds(refractory);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(3);
            // version 3
            writer.Write(MaxTargetRange);
            // version 2
            writer.Write(TargetingEnabled);
            writer.Write(TargetingAction);
            writer.Write(TargetCondition);
            writer.Write(TargetFailureAction);
            // version 1
            writer.Write(AllowCarried);
            // version 0
            writer.Write(RequireLOS);
            writer.Write(MaxRange);
            writer.Write(Refractory);
            writer.Write(m_EndTime - DateTime.UtcNow);
            writer.Write(MaxUses);
            writer.Write(NUses);
            writer.Write(BlockDefaultUse);
            writer.Write(Condition);
            writer.Write(SuccessAction);
            writer.Write(FailureAction);
            writer.Write(RefractoryAction);
            writer.Write(MaxUsesAction);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            switch (version)
            {
                case 3:
                    MaxTargetRange = reader.ReadInt();
                    goto case 2;
                case 2:
                    TargetingEnabled = reader.ReadBool();
                    TargetingAction = reader.ReadString();
                    TargetCondition = reader.ReadString();
                    TargetFailureAction = reader.ReadString();
                    goto case 1;
                case 1:
                    AllowCarried = reader.ReadBool();
                    goto case 0;
                case 0:
                    // version 0
                    RequireLOS = reader.ReadBool();
                    MaxRange = reader.ReadInt();
                    Refractory = reader.ReadTimeSpan();
                    var remaining = reader.ReadTimeSpan();
                    m_EndTime = DateTime.UtcNow + remaining;
                    MaxUses = reader.ReadInt();
                    NUses = reader.ReadInt();
                    BlockDefaultUse = reader.ReadBool();
                    Condition = reader.ReadString();
                    SuccessAction = reader.ReadString();
                    FailureAction = reader.ReadString();
                    RefractoryAction = reader.ReadString();
                    MaxUsesAction = reader.ReadString();
                    break;
            }
        }

        public void ExecuteActions(Mobile mob, object target, string actions)
        {
            if (actions == null || actions.Length <= 0) return;
            // execute any action associated with it
            // allow for multiple action strings on a single line separated by a semicolon

            var args = actions.Split(';');

            for (var j = 0; j < args.Length; j++) ExecuteAction(mob, target, args[j]);
        }

        private void ExecuteAction(Mobile mob, object target, string action)
        {
            if (action == null || action.Length <= 0) return;

            string status_str = null;
            var TheSpawn = new XmlSpawner.SpawnObject(null, 0);

            TheSpawn.TypeName = action;
            var substitutedtypeName = BaseXmlSpawner.ApplySubstitution(null, target, mob, action);
            var typeName = BaseXmlSpawner.ParseObjectType(substitutedtypeName);

            var loc = new Point3D(0, 0, 0);
            Map map = null;


            if (target is Item)
            {
                var ti = target as Item;
                if (ti.Parent == null)
                {
                    loc = ti.Location;
                    map = ti.Map;
                }
                else if (ti.RootParent is Item)
                {
                    loc = ((Item)ti.RootParent).Location;
                    map = ((Item)ti.RootParent).Map;
                }
                else if (ti.RootParent is Mobile)
                {
                    loc = ((Mobile)ti.RootParent).Location;
                    map = ((Mobile)ti.RootParent).Map;
                }
            }
            else if (target is Mobile)
            {
                var ti = target as Mobile;

                loc = ti.Location;
                map = ti.Map;
            }

            if (BaseXmlSpawner.IsTypeOrItemKeyword(typeName))
            {
                BaseXmlSpawner.SpawnTypeKeyword(target, TheSpawn, typeName, substitutedtypeName, true, mob, loc, map,
                    out status_str);
            }
            else
            {
                // its a regular type descriptor so find out what it is
                var type = SpawnerType.GetType(typeName);
                try
                {
                    var arglist = BaseXmlSpawner.ParseString(substitutedtypeName, 3, "/");
                    var o = XmlSpawner.CreateObject(type, arglist[0]);

                    if (o == null)
                    {
                        status_str = "invalid type specification: " + arglist[0];
                    }
                    else if (o is Mobile)
                    {
                        var m = (Mobile)o;
                        if (m is BaseCreature)
                        {
                            var c = (BaseCreature)m;
                            c.Home = loc; // Spawners location is the home point
                        }

                        m.Location = loc;
                        m.Map = map;

                        BaseXmlSpawner.ApplyObjectStringProperties(null, substitutedtypeName, m, mob, target,
                            out status_str);
                    }
                    else if (o is Item)
                    {
                        var item = (Item)o;
                        BaseXmlSpawner.AddSpawnItem(null, target, TheSpawn, item, loc, map, mob, false,
                            substitutedtypeName, out status_str);
                    }
                }
                catch
                {
                }
            }

            ReportError(mob, status_str);
        }

        private void ReportError(Mobile mob, string status_str)
        {
            if (status_str != null && mob != null && !mob.Deleted && mob is PlayerMobile &&
                mob.AccessLevel > AccessLevel.Player) mob.SendMessage(33, string.Format("{0}:{1}", Name, status_str));
        }

        // return true to allow use
        private bool CheckCondition(Mobile from, object target)
        {
            // test the condition if there is one
            if (Condition != null && Condition.Length > 0)
            {
                string status_str;

                return BaseXmlSpawner.CheckPropertyString(null, target, Condition, from, out status_str);
            }

            return true;
        }

        // return true to allow use
        private bool CheckTargetCondition(Mobile from, object target)
        {
            // test the condition if there is one
            if (TargetCondition != null && TargetCondition.Length > 0)
            {
                string status_str;

                return BaseXmlSpawner.CheckPropertyString(null, target, TargetCondition, from, out status_str);
            }

            return true;
        }

        // return true to allow use
        private bool CheckRange(Mobile from, object target)
        {
            if (from == null || !(target is IEntity) || MaxRange < 0) return false;

            var map = ((IEntity)target).Map;
            var loc = ((IEntity)target).Location;

            if (map != from.Map) return false;

            // check for allowed use in pack
            if (target is Item)
            {
                var targetitem = (Item)target;
                // is it carried by the user?
                if (targetitem.RootParent == from) return AllowCarried;

                // block use in other containers or on other mobiles
                if (targetitem.Parent != null) return false;
            }

            var haslos = true;
            if (RequireLOS)
                // check los as well
                haslos = from.InLOS(target);

            return from.InRange(loc, MaxRange) && haslos;
        }

        public void OutOfRange(Mobile from)
        {
            if (from == null) return;

            from.SendLocalizedMessage(500446); // That is too far away.
        }

        private void TryToTarget(Mobile from, object target, XmlUse xa)
        {
            if (from == null) return;

            ExecuteActions(from, target, TargetingAction);

            if (xa != null) from.Target = new XmlUseTarget(xa.MaxTargetRange, target, xa);
        }

        private void TryToUse(Mobile from, object target)
        {
            if (CheckRange(from, target) && CheckCondition(from, target) && CheckMaxUses && CheckRefractory)
            {
                // check for targeting
                if (TargetingEnabled)
                {
                    TryToTarget(from, target, this);
                }
                else
                {
                    // success
                    ExecuteActions(from, target, SuccessAction);

                    m_EndTime = DateTime.UtcNow + Refractory;
                    NUses++;
                }
            }
            else
            {
                // failure
                if (!CheckRange(from, target))
                    OutOfRange(from);
                else if (!CheckRefractory)
                    ExecuteActions(from, target, RefractoryAction);
                else if (!CheckMaxUses)
                    ExecuteActions(from, target, MaxUsesAction);

                else
                    ExecuteActions(from, target, FailureAction);
            }
        }

        // disable the default use of the target
        public override bool BlockDefaultOnUse(Mobile from, object target)
        {
            return BlockDefaultUse || !(CheckRange(from, target) && CheckCondition(from, target) && CheckMaxUses &&
                                        CheckRefractory);
        }

        // this is called when the attachment is on the user
        public override void OnUser(object target)
        {
            var from = AttachedTo as Mobile;

            TryToUse(from, target);
        }


        // this is called when the attachment is on the target being used
        public override void OnUse(Mobile from)
        {
            var target = AttachedTo;

            // if a target tries to use itself, then ignore it, it will be handled by OnUser
            if (target == from) return;

            TryToUse(from, target);
        }

        public class XmlUseTarget : Target
        {
            private readonly XmlUse m_xa;
            private object m_objectused;

            public XmlUseTarget(int range, object objectused, XmlUse xa)
                : base(range, true, TargetFlags.None)
            {
                m_objectused = objectused;
                m_xa = xa;
                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == null || targeted == null || m_xa == null) return;

                // success
                if (m_xa.CheckTargetCondition(from, targeted))
                {
                    m_xa.ExecuteActions(from, targeted, m_xa.SuccessAction);

                    m_xa.m_EndTime = DateTime.UtcNow + m_xa.Refractory;
                    m_xa.NUses++;
                }
                else
                {
                    m_xa.ExecuteActions(from, targeted, m_xa.TargetFailureAction);
                }
            }
        }
    }
}
