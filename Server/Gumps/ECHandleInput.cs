using Server.Network;

namespace Server.Gumps
{
    public class ECHandleInput : GumpEntry
    {
        private static readonly byte[] m_LayoutName = Gump.StringToBuffer("echandleinput");

        public override string Compile()
        {
            return "{ echandleinput }";
        }

        public override void AppendTo(IGumpWriter disp)
        {
            disp.AppendLayout(m_LayoutName);
        }
    }
}
