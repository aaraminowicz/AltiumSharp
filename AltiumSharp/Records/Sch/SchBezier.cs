using System.Drawing;

namespace AltiumSharp.Records
{
    public class SchBezier : SchBasicPolyline
    {
        public override int Record => 5;

        public SchBezier() : base()
        {
            Color = ColorTranslator.FromWin32(255);
        }
    }
}
