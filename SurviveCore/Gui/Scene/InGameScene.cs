using System.Drawing;
using SurviveCore.DirectX;
using WinApi.User32;

namespace SurviveCore.Gui.Scene
{
    public class InGameScene : GuiScene
    {

        private readonly SurvivalGame game;

        public SurvivalGame GetGame() => game;
        
        public InGameScene(SurvivalGame game)
        {
            this.game = game;
        }

        public override void OnActivate(Client c)
        {
            base.OnActivate(c);
            c.ClearColor = Color.DarkSlateGray;
            c.Input.MouseCaptured = true;
        }

        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            gui.Text(new Point(5,5), "Block: " + TextFormat.LightPink + game.SelectedBlock.Name, size:30);
            gui.Text(new Point(client.ScreenSize.Width/2, client.ScreenSize.Height/2), "+", size:25, origin:Origin.Center);
            if (Settings.Instance.DebugInfo)
                gui.Text(new Point(client.ScreenSize.Width-200, 5), $"FPS: {client.Fps}\n" + game?.DebugText);
        }

        public override void OnPhysicsUpdate(InputManager.InputState input)
        {
            base.OnPhysicsUpdate(input);
            if(input.IsKeyDown(VirtualKey.ESCAPE) || !input.IsForeground)
                client.CurrentScene = new PauseScene(this);
            game.Update(input);
        }

        public override void OnRenderUpdate(InputManager.InputState input)
        {
            base.OnRenderUpdate(input);
            game.Render();
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();
            client.Input.MouseCaptured = false;
        }
    }
}