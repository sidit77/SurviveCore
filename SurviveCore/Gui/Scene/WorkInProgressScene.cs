using System;
using System.Drawing;
using SurviveCore.DirectX;

namespace SurviveCore.Gui.Scene
{
    public class WorkInProgressScene : GuiScene
    {
        private readonly GuiScene previous;
        private float t;

        public override void OnActivate(Client c)
        {
            base.OnActivate(c);
            c.ClearColor = Color.LightSlateGray;
        }

        public WorkInProgressScene(GuiScene previous)
        {
            this.previous = previous;
        }

        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            int w = client.ScreenSize.Width  / 2;
            int h = client.ScreenSize.Height / 2;
            gui.Text(new Point(w + (int)(MathF.Sin(t) * 300), h - 90 + (int)(MathF.Cos(t) * 40)), "§8Work in Progress", Origin.Center, 90);
            if (gui.Button(UIHelpers.GetCentered(w, h + 230, 300, 80), "Back"))
                client.CurrentScene = previous;
        }

        public override void OnPhysicsUpdate(InputManager.InputState input)
        {
            t += 0.01f;
        }
    }
}