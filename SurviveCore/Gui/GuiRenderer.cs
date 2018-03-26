using System;
using System.Drawing;
using SharpDX.Direct3D11;
using SurviveCore.DirectX;
using WinApi.User32;
using WinApi.Windows.Controls;

namespace SurviveCore.Gui {
    public class GuiRenderer : IDisposable {

        private readonly InputManager.InputState input;
        
        public GuiRenderer(Device device, InputManager input) {
            this.input = new InputManager.InputState(input);
        }

        public bool Button(Rectangle rect, string text) {
            return !input.MouseCaptured && input.IsKeyDown(VirtualKey.LBUTTON) && rect.Contains(input.RelativeMousePosition);
        }
        
        public void Render(DeviceContext context) {
            input.Update();
        }
        
        public void Dispose() {
            
        }
    }
}