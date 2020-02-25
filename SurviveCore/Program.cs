using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using SurviveCore.DirectX;
using SurviveCore.Gui.Scene;
using WinApi.Desktop;
using WinApi.User32;
using WinApi.Windows;
using WinApi.Windows.Controls;
using WinApi.Windows.Helpers;

namespace SurviveCore {
    internal static class Program {

        
        [STAThread]
        private static void Main(string[] args) {

            /*
            EventBasedNetListener listener = new EventBasedNetListener();
            NetManager client = new NetManager(listener);
            client.Start();
            client.Connect("localhost" , 9050, "SomeConnectionKey");
            listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod) =>
            {
                Console.WriteLine("We got: {0}", dataReader.GetString(100));
                dataReader.Recycle();
            };
            
            while (!Console.KeyAvailable)
            {
                client.PollEvents();
                Thread.Sleep(15);
            }
            
            client.Stop();
            */
            
            Console.WriteLine("Vector hardware acceleration: " + Vector.IsHardwareAccelerated);
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            try {
                ApplicationHelpers.SetupDefaultExceptionHandlers();

                using (var win = Window.Create<Client>(text: "Hello", width: 1280, height: 720)) {
                    win.CenterToScreen();
                    win.Show();
            
                    void DestroyHandler() => MessageHelpers.PostQuitMessage();
                    win.Destroyed += DestroyHandler;
            
                    long lastupdated = Stopwatch.GetTimestamp();
                    long updatetick = (long)(1f / 200 * Stopwatch.Frequency);
                    
                    InputManager inputManager = new InputManager(win);
                    InputManager.InputState renderinput = new InputManager.InputState(inputManager);
                    InputManager.InputState updateinput = new InputManager.InputState(inputManager);
                    win.Update(updateinput);
                    win.CurrentScene = new MainMenuScene();
                    
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

                            if (msg.Value == (uint) WM.CHAR)
                            {
                                unsafe {
                                    WindowMessage wm = new WindowMessage(msg.Hwnd, msg.Value, msg.WParam, msg.LParam);
                                    KeyCharPacket p = new KeyCharPacket(&wm);
                                    inputManager.KeyEvent(p.Key);
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
            /**/
        }

    }
}