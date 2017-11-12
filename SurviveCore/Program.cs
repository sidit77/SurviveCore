using System;
using System.Diagnostics;
using OpenTK;

namespace SurviveCore {

    class Program {

        [STAThread]
        static void Main(string[] args) {
            Trace.Listeners.Add(new ConsoleTraceListener());
            
            Trace.TraceInformation("Vector hardware acceleration: " + System.Numerics.Vector.IsHardwareAccelerated);
            
            using(var game = new SurvivalGame()) {
                game.Icon = Icon.ExtractAssociatedIcon("Survive.exe");
                game.Title = "Test Game";
                game.VSync = VSyncMode.Adaptive;
                game.X = (DisplayDevice.GetDisplay(DisplayIndex.Default).Width - game.Width) / 2;
                game.Y = (DisplayDevice.GetDisplay(DisplayIndex.Default).Height - game.Height) / 2;
                game.WindowState = WindowState.Maximized;
            
                game.Run(120);
            }
        }

    }

    class ConsoleTraceListener : TraceListener {

        public ConsoleTraceListener() {
            this.TraceOutputOptions = TraceOptions.None;
        }

        public override void Write(string message) {
            Console.Write(message);
        }

        public override void WriteLine(string message) {
            Console.WriteLine(message);
        }

    }
}