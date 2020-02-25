using System.Drawing;
using SurviveCore.DirectX;
using WinApi.User32;

namespace SurviveCore.Gui.Scene
{
    public class MainMenuScene : GuiScene
    {
        public override void OnActivate(Client c)
        {
            base.OnActivate(c);
            c.ClearColor = Color.LightSlateGray;
        }

        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            int w = client.ScreenSize.Width  / 2;
            int h = client.ScreenSize.Height / 2;
            gui.Text(new Point(w, h - 180), "§dSurvival Game", Origin.Center, 150);
            if(gui.Button(UIHelpers.GetCentered(w, h - 040, 500, 80), "Singleplayer"))
                client.CurrentScene = new InGameScene(new SurvivalGame(client));
            if(gui.Button(UIHelpers.GetCentered(w, h + 050, 500, 80), "Multiplayer"))
                client.CurrentScene = new WorkInProgressScene(this);
            if(gui.Button(UIHelpers.GetCentered(w, h + 140, 500, 80), "Settings"))
                client.CurrentScene = new SettingsScene(this);
            if(gui.Button(UIHelpers.GetCentered(w, h + 230, 500, 80), "Quit Game"))
                User32Methods.PostQuitMessage(0);
        }
        
    }
}