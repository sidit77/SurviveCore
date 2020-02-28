using System.Drawing;
using SurviveCore.DirectX;
using WinApi.User32;

namespace SurviveCore.Gui.Scene.Multiplayer
{
    public class MultiplayerPauseScene : GuiScene
    {

        private MultiplayerInGameScene previous;

        public MultiplayerPauseScene(MultiplayerInGameScene previous)
        {
            this.previous = previous;
        }

        public override void OnActivate(Client c)
        {
            base.OnActivate(c);
            client.ClearColor = Color.DarkSlateGray;
        }

        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            int w = client.ScreenSize.Width / 2;
            int h = client.ScreenSize.Height / 2;
            if (gui.Button(UIHelpers.GetCentered(w, h - 90, 500, 80), "Resume"))
                client.CurrentScene = previous;
            if (gui.Button(UIHelpers.GetCentered(w, h, 500, 80), "Settings"))
                client.CurrentScene = new SettingsScene(this);
            if (gui.Button(UIHelpers.GetCentered(w, h + 90, 500, 80), "Main Menu"))
            {
                previous.GetGame().Dispose();
                client.CurrentScene = new MainMenuScene();
            }

        }

        public override void OnPhysicsUpdate(InputManager.InputState input)
        {
            base.OnPhysicsUpdate(input);
            if (input.IsForeground && input.IsKeyDown(VirtualKey.ESCAPE))
                client.CurrentScene = previous;
            previous.GetGame().Update(input);
        }

        public override void OnRenderUpdate(InputManager.InputState input)
        {
            base.OnRenderUpdate(input);
            previous.GetGame().Render();
        }

        public override void OnNetworkUpdate()
        {
            base.OnNetworkUpdate();
            previous.GetGame().Network();
        }
    }
}