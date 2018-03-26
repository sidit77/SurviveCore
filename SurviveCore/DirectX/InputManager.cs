using System;
using NetCoreEx.Geometry;
using WinApi.User32;
using WinApi.Windows.Controls;

namespace SurviveCore.DirectX {
    public class InputManager {

        private readonly Window window;
        private bool captured;
        private int wheelpos;
        private Point warpmousepos;

        private InputState defaultstate;
        public InputState Default => defaultstate ?? (defaultstate = new InputState(this));
        
        public InputManager(Window window) {
            this.window = window;
            warpmousepos = new Point(0,0);
        }
        public int MouseWheelEvent {
            set => wheelpos += value;
        }

        private Point AbsoluteMousePosition {
            get {
                User32Methods.GetCursorPos(out Point currentmousepos);
                currentmousepos.Offset(warpmousepos.X, warpmousepos.Y);
                return currentmousepos;
            }
        }
        
        public void Update() {
            if(User32Methods.GetForegroundWindow() == window.Handle && captured) {
                window.GetWindowRect(out Rectangle r);
                User32Methods.GetCursorPos(out Point p1);
                User32Methods.SetCursorPos((r.Left + r.Right) / 2, (r.Top + r.Bottom) / 2);
                User32Methods.GetCursorPos(out Point p2);
                warpmousepos.Offset(p1.X - p2.X, p1.Y - p2.Y);
            }
            defaultstate?.Update();
        }

        public class InputState {

            private readonly InputManager manager;
            
            private Point lastmouseposition;
            private int lastwheelpos;
            private readonly KeyboardState lastkeystate;

            public InputState(InputManager manager) {
                this.manager = manager;
                lastkeystate = new KeyboardState();
            }
            
            public int MouseWheel => manager.wheelpos;
            public int MouseWheelDelta => manager.wheelpos - lastwheelpos;
            public bool IsForeground => User32Methods.GetForegroundWindow() == manager.window.Handle;
            
            public bool MouseCaptured {
                get => manager.captured;
                set {
                    if(manager.captured != value) {
                        User32Methods.ShowCursor(!value);
                        manager.captured = value;
                    }
                }
            }
            
            public System.Drawing.Point DeltaMousePosition {
                get {
                    Point currentmousepos = manager.AbsoluteMousePosition;
                    return new System.Drawing.Point(currentmousepos.X - lastmouseposition.X, currentmousepos.Y - lastmouseposition.Y);
                }
            }

            public System.Drawing.Point MousePosition {
                get {
                    User32Methods.GetCursorPos(out Point currentmousepos);
                    return new System.Drawing.Point(currentmousepos.X, currentmousepos.Y);
                }
                set => User32Methods.SetCursorPos(value.X, value.Y);
            }
        
            public System.Drawing.Point RelativeMousePosition {
                get {
                    User32Methods.GetCursorPos(out Point currentmousepos);
                    manager.window.GetWindowRect(out Rectangle r);
                    User32Helpers.InverseAdjustWindowRectEx(ref r, manager.window.GetStyles(), false, manager.window.GetExStyles());
                    return new System.Drawing.Point(currentmousepos.X - r.Left, currentmousepos.Y - r.Top);
                }
            }

            public bool IsKey(VirtualKey key) {
                return User32Methods.GetKeyState(key).IsPressed;
            }
            
            public bool IsKeyDown(VirtualKey key) {
                return User32Methods.GetKeyState(key).IsPressed && !lastkeystate[key];
            }
            
            public bool IsKeyUp(VirtualKey key) {
                return !User32Methods.GetKeyState(key).IsPressed && lastkeystate[key];
            }
            
            public void Update() {
                lastmouseposition = manager.AbsoluteMousePosition;
                lastkeystate.Update();
                lastwheelpos = manager.wheelpos;
            }
            
        }
        
        private class KeyboardState {
            private readonly byte[] keystates;

            public KeyboardState() {
                keystates = new byte[256];
                Update();
            }
        
            public bool this[VirtualKey key] => (keystates[(int)key] & 128) != 0;

            public unsafe void Update() {
                fixed(byte* bp = keystates) {
                    User32Methods.GetKeyboardState((IntPtr)bp);
                }
            }
        }
        
    }
    
}