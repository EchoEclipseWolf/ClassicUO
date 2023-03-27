using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.AiEngine.Scripts;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.AiEngine {
    public class AiCore {
        public static AiCore Instance = new AiCore();

        public Dictionary<string, BaseAITask> Tasks = new();
        public Dictionary<string, BaseAITask> MainScripts = new();

        public static bool IsScriptRunning { get; private set; }
        private static bool _needToSendOnStart = false;

        public AiCore() {

            var bandageTask = new SelfBandageAiTask();
            Tasks[bandageTask.Name] = bandageTask;

            var groundItemLearnerTask = new GroundItemLearnerTask();
            Tasks[groundItemLearnerTask.Name] = groundItemLearnerTask;

            var itemLearnerTask = new ItemLearnerTask();
            Tasks[itemLearnerTask.Name] = itemLearnerTask;

            var selfBuffTask = new SelfBuffTask();
            Tasks[selfBuffTask.Name] = selfBuffTask;

            var itemDataUpdateTask = new ItemDataUpdateTask();
            Tasks[itemDataUpdateTask.Name] = itemDataUpdateTask;

            var learnRunebooksScript = new LearnRunebooksScript();
            MainScripts[learnRunebooksScript.Name] = learnRunebooksScript;

            var mageryTrainingScript = new MageryTrainingScript();
            MainScripts[mageryTrainingScript.Name] = mageryTrainingScript;

            var learnHouseContainersScript = new LearnHouseContainersScript();
            MainScripts[learnHouseContainersScript.Name] = learnHouseContainersScript;

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

            var test1 = World.MapIndex;
            var test2 = World.Map;

            AiSettings.Load();
            LandmarksMemory.Load();
            ItemsMemory.Load();
            TeleportsMemory.Load();
            HouseMemory.Load();
            MobilesMemory.Load();

            if (AiSettings.Instance == null) {
                return true;
            }

            var tes234t = World.Get(1073846790);

            await MobilesMemory.Instance.Pulse();
            HouseMemory.Instance.Update();

            if (await HouseMemory.Instance.OpenContainer()) {
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
                    if (_needToSendOnStart) {
                        await script.Start();
                        _needToSendOnStart = false;
                    }

                    await script.Pulse();
                }
            }

            if (ItemDataUpdateTask.PlayerBackpack != null) {
                var test = ItemDataUpdateTask.PlayerBackpack.SubContainers.Where(s => s.ContainerItem != null && s.ContainerItem.Name.ToLower().Contains("relic")).ToList();
            }

            await Task.Delay(1);
            return true;
        }

        internal void StartScript() {
            if (!IsScriptRunning) {
                _needToSendOnStart = true;
            }

            IsScriptRunning = true;
            AIEngineOverlayGump.Instance.UpdatePlayPauseButton();
        }

        internal void StopScript() {
            IsScriptRunning = false;
            AIEngineOverlayGump.Instance.UpdatePlayPauseButton();
        }

        internal void UpdateGroundItem(Item item) {
            if (item.Graphic == 0x1F18) {
                int bob = 1;
            }
        }
    }
}