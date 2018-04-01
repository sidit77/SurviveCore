using System.Diagnostics;

namespace SurviveCore.World.Utils {
    
    public class AverageTimer {

        private readonly Stopwatch watch;
        private int count;

        public AverageTimer() {
            watch = new Stopwatch();
            count = 0;
        }

        public void Start() {
            watch.Start();
        }

        public void Stop(int items = 1) {
            count += items;
            watch.Stop();
        }

        public long AverageTicks => count == 0 ? -1 : (watch.ElapsedTicks / count);

    }
    
}