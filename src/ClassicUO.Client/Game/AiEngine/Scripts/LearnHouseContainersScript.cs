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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Network;

namespace ClassicUO.Game.AiEngine.Scripts
{
    public class LearnHouseContainersScript : BaseAITask {
        private AiHouse _currentHouse;
        private AiContainer _currentContainer;
        private readonly List<AiHouse> _learnedHouses = new();
        private readonly List<uint> _learnedContainers = new();
        private List<AiContainer> _containersToLearn = new();

        public LearnHouseContainersScript() : base("Learn House Containers") {
        }

        public override async Task<bool> Start() {
            await base.Start();

            _learnedContainers.Clear();
            _learnedHouses.Clear();
            _containersToLearn.Clear();

            return true;
        }

        public override async Task<bool> Pulse() {
            if (HouseMemory.Instance.Houses.Count == 0 || _learnedHouses.Count == HouseMemory.Instance.Houses.Count) {
                AiCore.Instance.StopScript();
                HouseMemory.Instance.Save();
                GameActions.MessageOverhead($"[LearnHouseContainers] No Houses to Learn - Stopping", Player.Serial);
                return true;
            }

            _currentHouse = HouseMemory.Instance.Houses.FirstOrDefault(h => !_learnedHouses.Contains(h));

            if (_currentHouse == null) {
                AiCore.Instance.StopScript();
                HouseMemory.Instance.Save();
                GameActions.MessageOverhead($"[LearnHouseContainers] No Houses to Learn - Stopping", Player.Serial);
                return true;
            }

            var middlePoint = _currentHouse.MiddlePoint;
            var middleDistance = middlePoint.Distance();
            if (middleDistance > 50) {
                Navigation.GamePathFinder.WalkTo((int) _currentContainer.Point().X, (int) _currentContainer.Point().Y, (int) _currentContainer.Point().Z, 1);
                await Task.Delay(400);
                return true;
            }

            await UpdateContainersToLearn();

            _currentContainer = _containersToLearn.FirstOrDefault();

            if (_currentContainer == null) {
                HouseMemory.Instance.Save();
                GameActions.MessageOverhead($"[LearnHouseContainers] Found no more containers to learn", Player.Serial);
                _learnedHouses.Add(_currentHouse);
                return true;
            }

            var xyDistance = _currentContainer.Point().Distance2D(World.Player.Position.ToPoint3D());
            var distance = _currentContainer.Distance();

            if (xyDistance > 1 || distance > 9) {
                //Pathfinder.WalkTo((int) _currentContainer.Point().X, (int) _currentContainer.Point().Y, (int) _currentContainer.Point().Z, 1);
                //await Task.Delay(400);
                await Navigation.NavigateTo(_currentContainer.Point(), _currentHouse.MapIndex, true);
                return true;
            }

            var containerGump = GumpHelper.GetContainerGrumpByItemSerial(_currentContainer.Serial);

            if (containerGump == null) {
                GameActions.MessageOverhead($"[LearnHouseContainers] Opening Container: {_currentContainer.ContainerItem.Name}", Player.Serial);
                GameActions.DoubleClick(_currentContainer.Serial);
                await WaitForHelper.WaitFor(() => GumpHelper.GetContainerGrumpByItemSerial(_currentContainer.Serial) != null, 4000);
                await Task.Delay(1000);
                return true;
            }

            await _currentContainer.UpdateContents(true);
            var items = _currentContainer.GetItems();

            if (items.Count > 0) {
                _currentHouse.AddContainerToHouse(_currentContainer);
                _learnedContainers.Add(_currentContainer.Serial);
                _currentContainer = null;

                AiCore.GumpsToClose.Push(containerGump);
                await Task.Delay(500);
                HouseMemory.Instance.Save();
            }

            return true;
        }

        private async Task<bool> UpdateContainersToLearn() {
            List<AiContainer> list = new();
            var groundContainers = ItemsHelper.FindContainersOnGround();

            if (_currentHouse == null) {
                return true;
            }

            foreach (var groundContainer in groundContainers) {
                for (int i = 0; i < 2; i++) {
                    if (SerialHelper.IsValid(groundContainer.Serial) && World.OPL.TryGetNameAndData(groundContainer.Serial, out string name, out string data)) {
                        break;
                    }

                    if (SerialHelper.IsValid(groundContainer.Serial)) {
                        PacketHandlers.AddMegaClilocRequest(groundContainer.Serial);
                        await Task.Delay(250);
                    }
                }

                if (!_currentHouse.IsInsideHouseRegion(groundContainer.Position.ToPoint3D())) {
                    continue;
                }


                var aiGroundContainer = AiContainer.GetContainer(groundContainer.Serial);

                if (aiGroundContainer != null && !_learnedContainers.Contains(aiGroundContainer.Serial)) {
                    await aiGroundContainer.UpdateCount();

                    if (aiGroundContainer.Count > 0) {
                        list.Add(aiGroundContainer);
                    }
                }
            }

            _containersToLearn = list.OrderBy(c => c.Point().Distance()).ToList();

            return true;
        }
    }
}
