using System;
using System.Diagnostics;
using NetCoreEx.Geometry;
using SurviveCore.DirectX;
using WinApi.Desktop;
using WinApi.User32;
using WinApi.Windows;
using WinApi.Windows.Controls;
using WinApi.Windows.Helpers;

namespace SurviveCore {
    internal static class Program {

        [STAThread]
        private static void Main(string[] args) {
            Console.WriteLine("Vector hardware acceleration: " + System.Numerics.Vector.IsHardwareAccelerated);
            try {
                ApplicationHelpers.SetupDefaultExceptionHandlers();

                using (var win = Window.Create<SurvivalGame>(text: "Hello", width: 1280, height: 720)) {
                    win.CenterToScreen();
                    win.Show();
            
                    void DestroyHandler() => MessageHelpers.PostQuitMessage();
                    win.Destroyed += DestroyHandler;
            
                    long lastupdated = Stopwatch.GetTimestamp();
                    long updatetick = (long)(1f / 200 * Stopwatch.Frequency);
                    
                    InputManager inputManager = new InputManager(win);
                    InputManager.InputState renderinput = new InputManager.InputState(inputManager);
                    InputManager.InputState updateinput = new InputManager.InputState(inputManager);
                    
                    Message msg = new Message();
                    while (msg.Value != (uint)WM.QUIT) {
                        if (User32Helpers.PeekMessage(out msg, IntPtr.Zero, 0, 0, PeekMessageFlags.PM_REMOVE)) {
                            if(msg.Value == (uint)WM.MOUSEWHEEL) {
                                unsafe {
                                    WindowMessage wm = new WindowMessage(msg.Hwnd, msg.Value, msg.WParam, msg.LParam);
                                    MouseWheelPacket p = new MouseWheelPacket(&wm);
                                    inputManager.MouseWheelEvent = p.WheelDelta;
                                }
                            }
                            User32Methods.TranslateMessage(ref msg);
                            User32Methods.DispatchMessage(ref msg);
                        } else {
                            while (lastupdated + updatetick < Stopwatch.GetTimestamp()) {
                                win.Update(updateinput);
                                updateinput.Update();
                                lastupdated += updatetick;
                            }
                            win.Draw(renderinput);
                            win.Validate();
                            inputManager.Update();
                            renderinput.Update();
                        }
                    }
            
                    win.Destroyed -= DestroyHandler;
                }
            
            } catch (Exception ex) {
                MessageBoxHelpers.Show(ex.Message);
                Console.WriteLine(ex);
            }
            
        }

    }
}