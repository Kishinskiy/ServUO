using Server.Network;

namespace Server.Misc
{
    public class Paperdoll
    {
        public static void Initialize()
        {
            EventSink.PaperdollRequest += EventSink_PaperdollRequest;
        }

        public static void EventSink_PaperdollRequest(PaperdollRequestEventArgs e)
        {
            var beholder = e.Beholder;
            var beheld = e.Beheld;

            beholder.Send(new DisplayPaperdoll(beheld, Titles.ComputeTitle(beholder, beheld),
                beheld.AllowEquipFrom(beholder)));

            if (beholder.ViewOPL)
            {
                var items = beheld.Items;

                for (var i = 0; i < items.Count; ++i)
                    beholder.Send(items[i].OPLPacket);
                // NOTE: OSI sends MobileUpdate when opening your own paperdoll.
                // It has a very bad rubber-banding affect. What positive affects does it have?
            }
        }
    }
}
