using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.AiEngine.Scripts;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.AiEngine {
    public class AiCore {
        public static AiCore Instance = new AiCore();

        public Dictionary<string, BaseAITask> Tasks = new();
        public Dictionary<string, BaseAITask> MainScripts = new();

        public static bool IsScriptRunning;

        public AiCore() {

            var bandageTask = new SelfBandageAiTask();
            Tasks[bandageTask.Name] = bandageTask;

            var groundItemLearnerTask = new GroundItemLearnerTask();
            Tasks[groundItemLearnerTask.Name] = groundItemLearnerTask;

            var itemLearnerTask = new ItemLearnerTask();
            Tasks[itemLearnerTask.Name] = itemLearnerTask;

            var learnRunebooksScript = new LearnRunebooksScript();
            MainScripts[learnRunebooksScript.Name] = learnRunebooksScript;

            var mageryTrainingScript = new MageryTrainingScript();
            MainScripts[mageryTrainingScript.Name] = mageryTrainingScript;

            IsScriptRunning = false;
        }

        public async Task<bool> Loop() {
            while (true) {
                try {
                    await Pulse();
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
            return true;
        }

        public async Task<bool> Pulse() {
            if (World.Player == null || World.Player.Name == null || World.Player.Name.Length == 0) {
                await Task.Delay(10);

                return true;
            }

            AiSettings.Load();
            LandmarksMemory.Load();
            ItemsMemory.Load();
            TeleportsMemory.Load();
            HouseMemory.Load();
            MobilesMemory.Load();

            if (AiSettings.Instance == null) {
                return true;
            }

            foreach (var task in Tasks.Values.OrderByDescending(t => t.Priority())) {
                await task.Pulse();
            }

            if (IsScriptRunning) {
                if (AiSettings.Instance.ScriptToRun == "") {
                    AiSettings.Instance.ScriptToRun = MainScripts.Keys.FirstOrDefault();
                }

                if (AiSettings.Instance.ScriptToRun != null && MainScripts.TryGetValue(AiSettings.Instance.ScriptToRun, out var script)) {
                    await script.Pulse();
                }
            }
            await Task.Delay(10);
            return true;
        }

        internal void UpdateGroundItem(Item item) {
            if (item.Graphic == 0x1F18) {
                int bob = 1;
            }
        }
    }
}