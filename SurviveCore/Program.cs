using System;
using OpenTK;

namespace SurviveCore {
    internal static class Program {

        [STAThread]
        private static void Main(string[] args) {
            Console.WriteLine("Vector hardware acceleration: " + System.Numerics.Vector.IsHardwareAccelerated);
            using(var game = new SurvivalGame()) {
                game.Title = "Test Game";
                game.VSync = VSyncMode.Adaptive;
                game.X = (DisplayDevice.GetDisplay(DisplayIndex.Default).Width - game.Width) / 2;
                game.Y = (DisplayDevice.GetDisplay(DisplayIndex.Default).Height - game.Height) / 2;
            
                game.Run(120);
            }
        }

    }
}