using System.Drawing;
using SurviveCore.DirectX;

namespace SurviveCore.Gui.Scene
{
    public class ServerSelectionScene : GuiScene
    {
        private readonly GuiScene previous;

        private string address;
        
        public override void OnActivate(Client c)
        {
            base.OnActivate(c);
            c.ClearColor = Color.LightSlateGray;
        }

        public ServerSelectionScene(GuiScene previous)
        {
            this.previous = previous;
            address = "localhost";
        }

        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            int w = client.ScreenSize.Width  / 2;
            int h = client.ScreenSize.Height / 2;

            gui.Text(new Point(w - 295, h - 105), "Server Address:", Origin.BottomLeft, 35);
            gui.TextField(UIHelpers.GetCentered(w, h - 60, 600, 80), "serveradress", ref address);
            
            if (gui.Button(UIHelpers.GetCentered(w, h + 30, 600, 80), "Connect"))
                client.CurrentScene = previous;
            
            if (gui.Button(new Rectangle(w - 250, h + 170, 500, 80), "Back"))
                client.CurrentScene = previous;
        }
        
    }
}