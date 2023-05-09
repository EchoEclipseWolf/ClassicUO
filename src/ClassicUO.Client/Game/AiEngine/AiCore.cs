using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.AiEngine.Scripts;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.AiEngine {
    public class AiCore {
        public static AiCore Instance = new AiCore();

        public Dictionary<string, BaseAITask> Tasks = new();
        public Dictionary<string, BaseAITask> MainScripts = new();

        internal static ConcurrentStack<Gump> GumpsToClose = new();
        internal static ConcurrentStack<Tuple<Gump, int>> GumpsToClickButton = new();

        public static bool IsScriptRunning { get; private set; }
        private static bool _needToSendOnStart;
        private static bool _hasStartup;
        private static bool _hasClosedGumpsStart;
        private static bool _aiEnabled = true;

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

            var sortRelicBagsInMasterBagScript = new SortRelicBagsInMasterBagScript();
            MainScripts[sortRelicBagsInMasterBagScript.Name] = sortRelicBagsInMasterBagScript;

            var miningScript = new MiningScript();
            MainScripts[miningScript.Name] = miningScript;
            
            var bodCollectorScript = new BODCollectorScript();
            MainScripts[bodCollectorScript.Name] = bodCollectorScript;

            var uoMapToHeightmap = new UoMapToHeightmap();
            MainScripts[uoMapToHeightmap.Name] = uoMapToHeightmap;

            var exportAreaToUnreal = new ExportAreaToUnreal();
            MainScripts[exportAreaToUnreal.Name] = exportAreaToUnreal;

            IsScriptRunning = false;

            Navigation.Start();
        }

        private static void ResetBackToStart() {
            _hasStartup = false;
            _hasClosedGumpsStart = false;

        }

        public async Task<bool> Loop() {
            while (_aiEnabled) {
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
                ResetBackToStart();
                return true;
            }

            if (!_hasStartup) {
                _hasStartup = true;
                await Task.Delay(6000);
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

            GameScene scene = Client.Game.GetScene<GameScene>();

            if (scene != null)
            {
                Weather weather = scene.Weather;

                if (weather.CurrentWeather != WeatherType.WT_RAIN) {
                    weather.Generate(WeatherType.WT_RAIN, Byte.MaxValue, 50);
                }
            }

            if (!_hasClosedGumpsStart) {
                try {
                    var staffToolbarGump = GumpHelper.GetStaffToolbarGump();
                    var chatHistoryGump = GumpHelper.GetChatHistoryGump();
                    var messageOfTheDayGump = GumpHelper.GetMessageOfTheDayGump();
                    var vetRewardGump = GumpHelper.GetVetRewardGump();

                    var closingGump = false;

                    if (staffToolbarGump != null) {
                        GumpsToClose.Push(staffToolbarGump);
                        closingGump = true;
                    }

                    if (chatHistoryGump != null) {
                        GumpsToClose.Push(chatHistoryGump);
                        closingGump = true;
                    }

                    if (messageOfTheDayGump != null) {
                        GumpsToClose.Push(messageOfTheDayGump);
                        closingGump = true;
                    }

                    if (vetRewardGump != null) {
                        GumpsToClose.Push(vetRewardGump);
                        closingGump = true;
                    }

                    if (closingGump) {
                        await Task.Delay(2000);
                    }

                    _hasClosedGumpsStart = true;
                }
                catch (Exception) {

                }
            }

            /* var animalTamingGump = GumpHelper.GetAnimalBODBook();
            if (animalTamingGump != null) {
                var entries = BODBookParser.ParseAnimalTamingBODBook(animalTamingGump);

                if (entries.Count == 0) {
                    int count = entries.Count;
                }
            }*/

            Navigation.UpdateNeeded();

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
            
        }
    }
}