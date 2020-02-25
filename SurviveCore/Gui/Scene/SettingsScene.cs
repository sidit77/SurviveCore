using System.Drawing;
using SurviveCore.DirectX;

namespace SurviveCore.Gui.Scene
{
    public class SettingsScene : GuiScene
    {
        private GuiScene previous;

        public SettingsScene(GuiScene previous)
        {
            this.previous = previous;
        }

        public override void OnActivate(Client c)
        {
            base.OnActivate(c);
            c.ClearColor = Color.LightSlateGray;
        }

        public override void OnGui(InputManager.InputState input, GuiRenderer gui)
        {
            int w = client.ScreenSize.Width  / 2 - 150;
            int h = client.ScreenSize.Height / 2 - 230;
            if(gui.Button(new Rectangle(w - 180, h +   0, 300, 80), "Wireframe " + (Settings.Instance.Wireframe ? "on" : "off")))
                Settings.Instance.ToggleWireframe();
            if(gui.Button(new Rectangle(w - 180, h +  90, 300, 80), "Ambient Occlusion " + (Settings.Instance.AmbientOcclusion ? "on" : "off")))
                Settings.Instance.ToggleAmbientOcclusion();
            if(gui.Button(new Rectangle(w - 180, h + 180, 300, 80), "Fog " + (Settings.Instance.Fog ? "on" : "off")))
                Settings.Instance.ToggleFog();
            if(gui.Button(new Rectangle(w - 180, h + 270, 300, 80), "Physics " + (Settings.Instance.Physics ? "on" : "off")))
                Settings.Instance.TogglePhysics();
                
            if(gui.Button(new Rectangle(w + 180, h +   0, 300, 80), "Debug info " + (Settings.Instance.DebugInfo ? "on" : "off")))
                Settings.Instance.ToggleDebugInfo();
            if(gui.Button(new Rectangle(w + 180, h +  90, 300, 80), "Camera updates " + (Settings.Instance.UpdateCamera ? "on" : "off")))
                Settings.Instance.ToggleUpdateCamera();
            if(gui.Button(new Rectangle(w + 180, h + 180, 300, 80), "VSync " + (Settings.Instance.VSync ? "on" : "off")))
                Settings.Instance.ToggleVSync();
            if(gui.Button(new Rectangle(w + 180, h + 270, 300, 80), "Fullscreen " + (Settings.Instance.Fullscreen ? "on" : "off")))
                Settings.Instance.ToggleFullscreen();

            if (gui.Button(new Rectangle(w - 100, h + 400, 500, 80), "Back"))
                client.CurrentScene = previous;
            
        }
    }
}