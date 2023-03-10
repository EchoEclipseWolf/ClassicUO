using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.AiClasses;
using ClassicUO.Game.AiEngine.Enums;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Enums;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.AiEngine.Scripts
{
    public class LearnRunebooksScript : BaseAITask {
        private static bool _hasRun;
        private List<SingleItemMemory> _learnedRunebooks = new();
        private bool _hasStartedPass;
        private SingleItemMemory _currentRunebook;
        private uint _currentRunebookGumpSerial;
        private byte _currentIndex = 0;
        private byte _maxIndex = 0;
        private string _currentRuneName = "";

        public LearnRunebooksScript() : base("Learn Runebooks") {
            _hasStartedPass = false;
        }

        public override async Task<bool> Pulse() {
            _hasRun = false;
            if (_hasRun) {
                GameActions.MessageOverhead($"Finished Learning Runebooks - Stopping", Player.Serial);
                AiCore.IsScriptRunning = false;

                return true;
            }

            var magerySkill = GetSkill(SkillConsts.Magery);

            if (magerySkill is { Value: < 60 }) {
                _hasRun = true;
                GameActions.MessageOverhead($"[LearnRunebook] Learning Runebooks Needs a Higher Magery Level of 60 - Stopping", Player.Serial);
                AiCore.IsScriptRunning = false;

                return true;
            }

            if (Player.Mana < 30) {
                GameActions.MessageOverhead($"[LearnRunebook] Waiting for more mana", Player.Serial);
                await Task.Delay(5000);
                return true;
            }

            if (!_hasStartedPass && GumpHelper.GetRunebookGump() != null) {
                var runebookGump = GumpHelper.GetRunebookGump();

                for (int i = 0; i < 20; i++) {
                    //The runebook gump should actually be closed here. Lets try 20 times to close all the ones that are open.
                    if (runebookGump != null)
                    {
                        runebookGump.InvokeMouseCloseGumpWithRClick();
                        await WaitForHelper.WaitFor(() => GumpHelper.GetRunebookGump() == null, 2000);

                        if (GumpHelper.GetRunebookGump() == null) {
                            break;
                        }
                    }
                }
                return true;
            }

            if (_hasStartedPass && _currentRunebook != null && _currentIndex >= _maxIndex) {
                FinishedCurrentRunebook();
                return true;
            }

            if (_hasStartedPass && _currentRunebook is { Serial: > 0 } && GumpHelper.GetRunebookGump() == null)
            {
                GameActions.DoubleClick(_currentRunebook.Serial);
                await WaitForHelper.WaitFor(() => GumpHelper.GetRunebookGump() != null, 2000);
                return true;
            }

            if (_hasStartedPass && GumpHelper.GetRunebookGump()?.ServerSerial == _currentRunebookGumpSerial) {
                var gump = GumpHelper.GetRunebookGump();
                if (gump != null) { // it cant be null here but whatever
                    var text = GumpHelper.GetRunebookTextForIndex(gump, _currentIndex);
                    if (!string.IsNullOrEmpty(text) && !text.Equals("Empty", StringComparison.InvariantCultureIgnoreCase)) {
                        _currentRuneName = text;
                        await TeleportToRune(gump, _currentIndex);
                    }
                }
                return true;
            }

            

            var runebooks = ItemsMemory.Instance.Runebooks.Where(r => _learnedRunebooks.All(l => l.Serial != r.Serial)).ToList();

            if (runebooks.Count > 0) {
                _hasStartedPass = true;

                _currentRunebook = runebooks.First();
                GameActions.DoubleClick(_currentRunebook.Serial);
                await WaitForHelper.WaitFor(() => GumpHelper.GetRunebookGump() != null, 2000);

                var foundGump = GumpHelper.GetRunebookGump();

                if (foundGump == null) { // Lets try again
                    return true;
                }

                _currentRunebookGumpSerial = foundGump.ServerSerial;
                _currentIndex = 0;
                _maxIndex = 0;

                for (int i = 0; i < 16; i++) {
                    var text = GumpHelper.GetRunebookTextForIndex(foundGump, i);

                    if (!string.IsNullOrEmpty(text) && !text.Equals("Empty", StringComparison.InvariantCultureIgnoreCase)) {
                        ++_maxIndex;
                    }
                }

                await Task.Delay(1000);
                return true;
            }
            else {
                GameActions.MessageOverhead($"[LearnRunebook] Finished Learning Runebooks Part 2 - Stopping", Player.Serial);
                AiCore.IsScriptRunning = false;
                _hasRun = true;
                return true;

            }

            await Task.Delay(1000);
            return true;
        }

        private async Task<bool> TeleportToRune(Gump gump, int index) {
            var previousLocation = new Point3D(World.Player.Position.X, World.Player.Position.Y, World.Player.Position.Z);
            var previousMapId = World.MapIndex;

            gump.OnButtonClick(50 + index);

            await WaitForHelper.WaitFor(() => previousLocation.Distance() > 8.0f || World.MapIndex != previousMapId, 15000);
            await Task.Delay(2500);
            Navigation.StopNavigation();

            if (previousLocation.Distance() > 2.0f || World.MapIndex != previousMapId) {
                DidTeleportToRun();
            }
            return true;
        }

        private void DidTeleportToRun() {
            if (_currentRunebook == null) {
                return;
            }

            TeleportsMemory.Instance.AddRuneMemory(_currentRunebook.Serial, _currentRunebook.ContainerSerial, _currentIndex, ItemLocationEnum.PlayerBackpack, World.Player.Position.ToPoint3D(), World.MapIndex, _currentRuneName);
            _currentIndex++;
            GameActions.MessageOverhead($"[LearnRunebook] Learned {_currentRuneName}", Player.Serial);
        }

        private void FinishedCurrentRunebook() {
            _learnedRunebooks.Add(_currentRunebook);
            _currentRunebook = null;
            _currentIndex = 0;
            _maxIndex = 0;
            _currentRunebookGumpSerial = 0;
            _currentRuneName = "";
        }
    }
}
