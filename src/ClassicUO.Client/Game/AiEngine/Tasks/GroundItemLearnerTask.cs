using System.Diagnostics;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.AiEngine.Tasks
{
    public class GroundItemLearnerTask : BaseAITask
    {
        private Stopwatch _timer = Stopwatch.StartNew();
        private const int DELAY = 500;

        public GroundItemLearnerTask() : base("Ground Item Learner Task")
        {
        }

        public override int Priority() {
            return 1;
        }

        public override async Task<bool> Pulse()
        {
            if (_timer.ElapsedMilliseconds < DELAY) {
                return false;
            }
            _timer.Restart();

            var championPlatforms = ItemsHelper.FindItemsOnGround(0x1F18, -1);

            foreach (var platform in championPlatforms) {
                LandmarksMemory.Instance.AddChampionPlatform(platform.Serial, new Point3D(platform.X, platform.Y, platform.Z), World.MapIndex);
            }
            
            return true;
        }
    }
}