using System;
using Server.Engines.Craft;

namespace Server.Items
{
    public class Globe : Item, IResource, IQuality
    {
        private Mobile _Crafter;
        private ItemQuality _Quality;
        private CraftResource _Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Crafter
        {
            get => _Crafter;
            set
            {
                _Crafter = value;
                InvalidateProperties();
            }
        }

        [Constructable]
        public Globe()
            : base(0x1047) // It isn't flipable
        {
            Weight = 3.0;
        }

        public Globe(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ItemQuality Quality
        {
            get => _Quality;
            set
            {
                _Quality = value;
                InvalidateProperties();
            }
        }

        public bool PlayerConstructed => true;

        public virtual int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes,
            ITool tool, CraftItem craftItem, int resHue)
        {
            Quality = (ItemQuality)quality;

            if (makersMark)
                Crafter = from;

            if (!craftItem.ForceNonExceptional)
            {
                if (typeRes == null)
                    typeRes = craftItem.Resources.GetAt(0).ItemType;

                Resource = CraftResources.GetFromType(typeRes);
            }

            return quality;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get => _Resource;
            set
            {
                _Resource = value;
                _Resource = value;
                Hue = CraftResources.GetHue(_Resource);
                InvalidateProperties();
            }
        }

        public override void AddCraftedProperties(ObjectPropertyList list)
        {
            if (_Crafter != null) list.Add(1050043, _Crafter.TitleName); // crafted by ~1_NAME~

            if (_Quality == ItemQuality.Exceptional) list.Add(1060636); // Exceptional
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (_Resource != CraftResource.None)
                list.Add(1053099, "#{0}\t{1}", CraftResources.GetLocalizationNumber(_Resource),
                    string.Format("#{0}", LabelNumber.ToString())); // ~1_oretype~ ~2_armortype~
            else
                base.AddNameProperty(list);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(1); // version

            writer.Write((int)_Resource);
            writer.Write(_Crafter);
            writer.Write((int)_Quality);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    _Resource = (CraftResource)reader.ReadInt();
                    _Crafter = reader.ReadMobile();
                    _Quality = (ItemQuality)reader.ReadInt();
                    break;
                case 0:
                    break;
            }
        }
    }
}
