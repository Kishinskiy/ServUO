using System;
using Server.Items;

namespace Server.Engines.XmlSpawner2
{
    public class XmlWeaponAbility : XmlAttachment
    {
        public WeaponAbility WeaponAbility { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Ability
        {
            get
            {
                if (WeaponAbility != null) return WeaponAbility.GetType().Name;

                return null;
            }
            set
            {
                if (value != null)
                {
                    var finfo = typeof(WeaponAbility).GetField(value);
                    if (finfo != null && finfo.IsStatic && finfo.FieldType == typeof(WeaponAbility))
                        try
                        {
                            WeaponAbility = (WeaponAbility)finfo.GetValue(null);
                        }
                        catch
                        {
                        }
                }
                else
                {
                    WeaponAbility = null;
                }
            }
        }


        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlWeaponAbility(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlWeaponAbility(string weaponability)
        {
            Ability = weaponability;
        }

        [Attachable]
        public XmlWeaponAbility(string name, string weaponability)
        {
            Name = name;
            Ability = weaponability;
        }

        [Attachable]
        public XmlWeaponAbility(string name, string weaponability, double expiresin)
        {
            Name = name;
            Ability = weaponability;
            Expiration = TimeSpan.FromMinutes(expiresin);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            writer.Write(Ability);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            // version 0
            Ability = reader.ReadString();
        }

        public override string OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player) return null;

            if (Expiration > TimeSpan.Zero)
                return string.Format("{2}: Weapon ability {0} expires in {1} mins", Ability, Expiration.TotalMinutes,
                    Name);

            return string.Format("{1}: Weapon ability {0}", Ability, Name);
        }
    }
}
