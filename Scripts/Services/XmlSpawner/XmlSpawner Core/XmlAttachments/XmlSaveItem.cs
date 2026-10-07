using System;
using Server.Items;

namespace Server.Engines.XmlSpawner2
{
    public class XmlSaveItem : XmlAttachment
    {
        private Item m_SavedItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public Container Container { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item SavedItem
        {
            get
            {
                // if the item has been moved off of the internal map, then forget about it
                if (m_SavedItem != null && (m_SavedItem.Parent != Container || m_SavedItem.Deleted))
                {
                    WasOwnedBy = null;
                    m_SavedItem = null;
                }

                return m_SavedItem;
            }
            set
            {
                // delete any existing item before assigning a new value
                if (SavedItem != null)
                {
                    SafeItemDelete(m_SavedItem);
                    //m_SavedItem.Delete();
                    m_SavedItem = null;
                }

                // dont allow saving the item if it is attached to it
                if (value != AttachedTo) m_SavedItem = value;

                // automatically internalize any saved item
                if (m_SavedItem != null) AddToContainer(m_SavedItem);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RestoreItem
        {
            get => false;
            set
            {
                if (value && SavedItem != null && AttachedTo is IEntity && ((IEntity)AttachedTo).Map != Map.Internal &&
                    ((IEntity)AttachedTo).Map != null)
                {
                    // move the item to the location of the object the attachment is attached to
                    if (AttachedTo is Item)
                    {
                        m_SavedItem.Map = ((Item)AttachedTo).Map;
                        m_SavedItem.Location = ((Item)AttachedTo).Location;
                        m_SavedItem.Parent = ((Item)AttachedTo).Parent;
                    }
                    else if (AttachedTo is Mobile)
                    {
                        m_SavedItem.Map = ((Mobile)AttachedTo).Map;
                        m_SavedItem.Location = ((Mobile)AttachedTo).Location;
                        m_SavedItem.Parent = null;
                    }


                    m_SavedItem = null;
                    WasOwnedBy = null;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile WasOwnedBy { get; set; }

        // These are the various ways in which the message attachment can be constructed.  
        // These can be called via the [addatt interface, via scripts, via the spawner ATTACH keyword.
        // Other overloads could be defined to handle other types of arguments

        // a serial constructor is REQUIRED
        public XmlSaveItem(ASerial serial) : base(serial)
        {
        }

        [Attachable]
        public XmlSaveItem()
        {
            Container = new SaveItemPack();
        }

        [Attachable]
        public XmlSaveItem(string name)
        {
            Name = name;
        }


        public XmlSaveItem(string name, Item saveditem)
        {
            Name = name;
            SavedItem = saveditem;
        }

        public XmlSaveItem(string name, Item saveditem, Mobile wasownedby)
        {
            Name = name;
            SavedItem = saveditem;
            WasOwnedBy = wasownedby;
        }

        private void AddToContainer(Item item)
        {
            if (item == null) return;

            if (Container == null) Container = new SaveItemPack();

            // need to place in a container to prevent internal map cleanup of the item
            Container.DropItem(item);
            Container.Internalize();
        }

        public Item GetItem()
        {
            var returneditem = SavedItem;

            m_SavedItem = null;
            WasOwnedBy = null;

            return returneditem;
        }

        public override void OnDelete()
        {
            base.OnDelete();

            // delete the item
            if (SavedItem != null)
                //SavedItem.Delete();
                SafeItemDelete(SavedItem);

            if (Container != null) SafeItemDelete(Container);
            //m_Container.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
            // version 0
            if (SavedItem != null)
                writer.Write(m_SavedItem);
            else
                writer.Write((Item)null);
            writer.Write(WasOwnedBy);
            writer.Write(Container);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            // version 0
            m_SavedItem = reader.ReadItem();
            WasOwnedBy = reader.ReadMobile();
            Container = (Container)reader.ReadItem();

            AddToContainer(m_SavedItem);
        }

        public override string OnIdentify(Mobile from)
        {
            if (from == null || from.AccessLevel == AccessLevel.Player) return null;

            if (Expiration > TimeSpan.Zero)
                return string.Format("{2}: Item {0} expires in {1} mins", SavedItem, Expiration.TotalMinutes, Name);

            return string.Format("{1}: Item {0}", SavedItem, Name);
        }

        private class SaveItemPack : Container
        {
            public override int MaxWeight => 0;

            public SaveItemPack() : base(0x9B2)
            {
            }

            public SaveItemPack(Serial serial) : base(serial)
            {
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
}
