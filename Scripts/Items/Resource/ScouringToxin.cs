using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class ScouringToxin : Item, IUsesRemaining, ICommodity
    {
        private int m_UsesRemaining;
        public override int LabelNumber => 1112292; // scouring toxin

        [Constructable]
        public ScouringToxin()
            : this(10)
        {
        }

        [Constructable]
        public ScouringToxin(int amount)
            : base(0x1848)
        {
            m_UsesRemaining = amount;
        }

        public ScouringToxin(Serial serial)
            : base(serial)
        {
        }

        TextDefinition ICommodity.Description => LabelNumber;
        bool ICommodity.IsDeedable => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UsesRemaining
        {
            get => m_UsesRemaining;
            set
            {
                m_UsesRemaining = value;
                if (m_UsesRemaining <= 0) Delete();
                else InvalidateProperties();
            }
        }

        public bool ShowUsesRemaining
        {
            get => false;
            set
            {
                {
                }
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_UsesRemaining <= 1)
                list.Add(LabelNumber);
            else
                list.Add(1050039, "{0}\t#{1}", m_UsesRemaining, LabelNumber); // ~1_NUMBER~ ~2_ITEMNAME~
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1112348); // Which item do you wish to scour?
                from.BeginTarget(-1, false, TargetFlags.None, OnTarget);
            }
        }

        public void OnTarget(Mobile from, object targeted)
        {
            if (targeted is Item)
            {
                var item = (Item)targeted;

                if (item.Parent is Mobile)
                {
                    from.SendLocalizedMessage(1112350); // You cannot scour items that are being worn!
                }
                else if (item.IsLockedDown || item.IsSecure)
                {
                    from.SendLocalizedMessage(1112351); // You may not scour items which are locked down.
                }
                else if (item.QuestItem)
                {
                    from.SendLocalizedMessage(1151837); // You may not scour toggled quest items.
                }
                else if (item is DryReeds)
                {
                    if (!(from is PlayerMobile) || !((PlayerMobile)from).BasketWeaving)
                    {
                        from.SendLocalizedMessage(
                            1112253); //You haven't learned basket weaving. Perhaps studying a book would help!
                    }
                    else
                    {
                        var reed1 = (DryReeds)targeted;
                        var cont = from.Backpack;

                        var hue = reed1.PlantHue;

                        if (!reed1.IsChildOf(from.Backpack))
                        {
                            from.SendLocalizedMessage(1116249); //That must be in your backpack for you to use it.
                        }
                        else if (cont != null)
                        {
                            var items = cont.FindItemsByType(typeof(DryReeds));
                            var list = new List<Item>();
                            var total = 0;

                            foreach (var it in items)
                                if (it is DryReeds)
                                {
                                    var check = (DryReeds)it;

                                    if (reed1.PlantHue == check.PlantHue)
                                    {
                                        total += it.Amount;
                                        list.Add(it);
                                    }
                                }

                            var toConsume = 2;

                            if (list.Count > 0 && total > 1)
                            {
                                foreach (var it in list)
                                {
                                    if (it.Amount >= toConsume)
                                    {
                                        it.Consume(toConsume);
                                        toConsume = 0;
                                    }
                                    else if (it.Amount < toConsume)
                                    {
                                        it.Delete();
                                        toConsume -= it.Amount;
                                    }

                                    if (toConsume <= 0)
                                        break;
                                }

                                var sReed = new SoftenedReeds(hue);

                                if (!from.Backpack.TryDropItem(from, sReed, false))
                                    sReed.MoveToWorld(from.Location, from.Map);

                                m_UsesRemaining--;

                                if (m_UsesRemaining <= 0)
                                    Delete();
                                else
                                    InvalidateProperties();

                                from.PlaySound(0x23E);
                            }
                            else
                            {
                                from.SendLocalizedMessage(
                                    1112250); //You don't have enough of this type of dry reeds to make that.
                            }
                        }
                    }
                }
                else if (BasePigmentsOfTokuno.IsValidItem(item))
                {
                    from.PlaySound(0x23E);

                    ((Item)targeted).Hue = 0;

                    m_UsesRemaining--;

                    if (m_UsesRemaining <= 0)
                        Delete();
                    else
                        InvalidateProperties();
                }
                else
                {
                    from.SendLocalizedMessage(1112349); // You cannot scour that!
                }
            }
            else
            {
                from.SendLocalizedMessage(1112349); // You cannot scour that!
            }
        }

        private static bool IsInTypeList(Type t, Type[] list)
        {
            for (var i = 0; i < list.Length; i++)
                if (list[i] == t)
                    return true;

            return false;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(2); // version
            writer.Write(m_UsesRemaining);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            if (version > 0)
                m_UsesRemaining = reader.ReadInt();

            if (version == 1)
                ItemID = 0x1848;
        }
    }
}
