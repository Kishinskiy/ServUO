using System;
using Server.Commands;
using Server.Targeting;

namespace Server.Items
{
    public class FlipCommandHandlers
    {
        public static void Initialize()
        {
            CommandSystem.Register("Flip", AccessLevel.GameMaster, Flip_OnCommand);
        }

        [Usage("Flip")]
        [Description("Turns an item.")]
        public static void Flip_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new FlipTarget();
        }

        private class FlipTarget : Target
        {
            public FlipTarget()
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Item)
                {
                    var item = (Item)targeted;

                    if (!item.Movable && from.IsPlayer())
                        return;

                    var type = targeted.GetType();

                    var AttributeArray =
                        (FlipableAttribute[])type.GetCustomAttributes(typeof(FlipableAttribute), false);

                    if (AttributeArray.Length == 0) return;

                    var fa = AttributeArray[0];

                    fa.Flip((Item)targeted);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DynamicFlipingAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class FlipableAttribute : Attribute
    {
        public int[] ItemIDs { get; }

        public FlipableAttribute()
            : this(null)
        {
        }

        public FlipableAttribute(params int[] itemIDs)
        {
            ItemIDs = itemIDs;
        }

        public virtual void Flip(Item item)
        {
            if (ItemIDs == null)
            {
                try
                {
                    var flipMethod = item.GetType().GetMethod("Flip", Type.EmptyTypes);
                    if (flipMethod != null)
                        flipMethod.Invoke(item, new object[0]);
                }
                catch
                {
                }
            }
            else
            {
                var index = 0;
                for (var i = 0; i < ItemIDs.Length; i++)
                    if (item.ItemID == ItemIDs[i])
                    {
                        index = i + 1;
                        break;
                    }

                if (index > ItemIDs.Length - 1)
                    index = 0;

                item.ItemID = ItemIDs[index];
            }
        }
    }
}
