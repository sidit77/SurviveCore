using SurviveCore.DirectX;
using WinApi.User32;

namespace SurviveCore.Gui.Scene
{
    public class PauseScene : GuiScene
    {

        private InGameScene previous;

        public PauseScene(InGameScene previous)
        {
            this.previous = previous;
        }
        
        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            int w = client.ScreenSize.Width  / 2;
            int h = client.ScreenSize.Height / 2;
            if(gui.Button(UIHelpers.GetCentered(w, h - 90, 500, 80), "Resume"))
                client.CurrentScene = previous;
            if(gui.Button(UIHelpers.GetCentered(w, h      , 500, 80), "Settings"))
                client.CurrentScene = new SettingsScene(this);
            if (gui.Button(UIHelpers.GetCentered(w, h + 90, 500, 80), "Main Menu"))
            {
                previous.GetGame().Dispose();
                client.CurrentScene = new MainMenuScene();
            }
                
        }

        public override void OnUpdate(InputManager.InputState input)
        {
            base.OnUpdate(input);
            if (input.IsForeground && input.IsKeyDown(VirtualKey.ESCAPE))
                client.CurrentScene = previous;
        }

        public override void OnRenderUpdate(InputManager.InputState input)
        {
            base.OnRenderUpdate(input);
            previous.GetGame().Render();
        }
    }
}