using System;
using System.Diagnostics;
using WinApi.Desktop;
using WinApi.User32;
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
                    
                    Message msg = new Message();
                    while (msg.Value != (uint)WM.QUIT) {
                        if (User32Helpers.PeekMessage(out msg, IntPtr.Zero, 0, 0, PeekMessageFlags.PM_REMOVE)) {
                            User32Methods.TranslateMessage(ref msg);
                            User32Methods.DispatchMessage(ref msg);
                        } else {
                            while (lastupdated + updatetick < Stopwatch.GetTimestamp()) {
                                win.Update();
                                lastupdated += updatetick;
                            }
                            win.Draw();
                            win.Validate();
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