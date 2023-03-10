using System.Diagnostics;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Enums;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.AiEngine.Tasks
{
    public class ItemLearnerTask : BaseAITask
    {
        private Stopwatch _timer = Stopwatch.StartNew();
        private const int DELAY = 500;

        public ItemLearnerTask() : base("Item Learner Task")
        {
        }

        public override int Priority()
        {
            return 1;
        }

        public override async Task<bool> Pulse()
        {
            if (_timer.ElapsedMilliseconds < DELAY)
            {
                return false;
            }
            _timer.Restart();

            var backpackItems = await ItemsHelper.GetPlayerBackpackItems(true);
            var count = await ItemsHelper.GetPlayerBackpackItemCount();

            var stopwatch = Stopwatch.StartNew();

            var runebooksInBackpack = await ItemsHelper.GetPlayerBackpackItemsById(0x22C5, -1);
            foreach (var runebookTuples in runebooksInBackpack) {
                ItemsMemory.Instance.AddRunebook(runebookTuples.Item2, runebookTuples.Item1, ItemLocationEnum.PlayerBackpack);
            }

            var time = stopwatch.ElapsedTicks;

            return true;
        }
    }
}