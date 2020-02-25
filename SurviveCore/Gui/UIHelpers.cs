using System.Drawing;

namespace SurviveCore.Gui
{
    public static class UIHelpers
    {
        public static Rectangle GetCentered(int x, int y, int w, int h)
        {
            return new Rectangle(x - w / 2, y - h / 2, w, h);
        }
    }
}