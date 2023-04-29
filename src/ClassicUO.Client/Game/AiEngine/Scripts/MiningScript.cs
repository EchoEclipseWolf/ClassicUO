using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.AiClasses;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Enums;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Map;
using Newtonsoft.Json;

namespace ClassicUO.Game.AiEngine.Scripts
{
    public class MiningScript : BaseAITask
    {
        public static List<MapPoint3D> MiningLocations = new();
        public const string MiningLocationsFile = "C:\\UltimaOnlineSharedData\\MiningLocations.json";
        private List<MapPoint3D> _usedMapPoints = new();
        private MapPoint3D _nextPoint;
        private const int _numPickaxesToGrab = 20;
        private Point3D _lastPosition = Point3D.Empty;
        private const int _stuckWaitTime = 15000;
        private Stopwatch _stuckTimer = Stopwatch.StartNew();

        private const int _miningStuckWaitTime = 30000;
        private Stopwatch _miningStuckTimer = Stopwatch.StartNew();

        public MiningScript() : base("Mining") {
            if (File.Exists(MiningLocationsFile)) {
                try {
                    var json = File.ReadAllText(MiningLocationsFile);
                    MiningLocations = JsonConvert.DeserializeObject<List<MapPoint3D>>(json);
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
            //var json = JsonConvert.SerializeObject(MiningLocations, Formatting.Indented);
            //File.WriteAllText("C:\\UltimaOnlineSharedData\\MiningLocations.json", json);
        }

        private async Task<bool> GrabMorePickaxes() {
            var house = HouseMemory.Instance.Houses.FirstOrDefault();

            if (house != null) {
                var container = HouseMemory.Instance.SearchForItemsInHouse("Exceptional Pickaxe", house, true, out var pickaxe);
                if (container == null) {
                    return false;
                }

                if (pickaxe == null) {
                    return false;
                }

                var xyDistance = container.Point().Distance2D(World.Player.Position.ToPoint3D());
                var distance = container.Distance();

                if (xyDistance > 1 || distance > 9) {
                    await Navigation.NavigateTo(container.LastPosition, container.MapIndex, true);
                    return true;
                }

                var pickaxeContainer = house.FindSubContainerBySerial(pickaxe.ContainerSerial);

                if (pickaxeContainer == null) {
                    return true;
                }

                await HouseMemory.Instance.OpenContainer(container, pickaxeContainer);

                var containerGump = GumpHelper.GetContainerGrumpByItemSerial(pickaxeContainer.Serial);

                if (containerGump == null) {
                    return true;
                }

                await pickaxeContainer.UpdateContents(false);
                var pickaxes = pickaxeContainer.GetItems().Where(a => a.Name != null && a.Name.Equals("Exceptional Pickaxe", StringComparison.InvariantCultureIgnoreCase)).ToList();

                for (int i = 0; i < _numPickaxesToGrab; i++) {
                    if (pickaxes.Count == 0) {
                        break;
                    }

                    GameActions.GrabItem(pickaxes.First().Serial, 1);
                    await Task.Delay(600);
                    await pickaxeContainer.UpdateContents(false);
                    pickaxes = pickaxeContainer.GetItems().Where(a => a.Name != null && a.Name.Equals("Exceptional Pickaxe", StringComparison.InvariantCultureIgnoreCase)).ToList();
                    
                }

                return true;
            }
            return true;
        }

        private bool HasPickaxesInHouse() {
            var house = HouseMemory.Instance.Houses.FirstOrDefault();

            if (house == null) {
                return false;
            }

            return HouseMemory.Instance.SearchForItemsInHouse("Exceptional Pickaxe", house, true, out var _) != null;
        }

        public override async Task<bool> Pulse() {
            var picks = await HasPickaxe();
            if (picks.Count == 0) {
                /*if (HasPickaxesInHouse()) {
                    await GrabMorePickaxes();
                }
                else {*/
                    await GoHome();
                //}

                return false;
            }

            if (_nextPoint == null) {
                _nextPoint = GetNextPoint();
            }

            if (_nextPoint == null) {
                return true;
            }

            if (_nextPoint.MapIndex != World.MapIndex || _nextPoint.Point.Distance() > 1) {
                _miningStuckTimer.Restart();

                if (_lastPosition.Distance() > 0) {
                    _stuckTimer.Restart();
                }

                if (_stuckTimer.ElapsedMilliseconds > _stuckWaitTime) {
                    _usedMapPoints.Add(_nextPoint);
                    _nextPoint = null;
                    GameActions.MessageOverhead("Stuck ... Moving to next point", Player.Serial);
                    _stuckTimer.Restart();
                    return true;
                }
                _lastPosition = World.Player.Position.ToPoint3D();
                await Navigation.NavigateTo(_nextPoint.Point, _nextPoint.MapIndex, true);
                return true;
            }

            if (Player.IsMounted) {
                if (Player.InWarMode) {
                    GameActions.ToggleWarMode();
                    await Task.Delay(1000);
                    _miningStuckTimer.Restart();
                    return true;
                }

                GameActions.DoubleClick(Player.Serial);
                await WaitForHelper.WaitFor(() => !Player.IsMounted, 2000);
                await Task.Delay(500);
                GameActions.Say("all follow me");
                _miningStuckTimer.Restart();
                return true;
            }

            if (World.Map == null) {
                return true;
            }


            var tile = World.Map.GetTile((int) World.Player.Position.X, (int) (World.Player.Position.Y + 1));

            if (tile is not Static) {
                tile = null;
            }

            if (tile == null) {
                for (int y = -1; y < 1; y++) {
                    if (tile != null) {
                        break;
                    }

                    for (int x = -1; x < 1; x++) {
                        if (tile != null) {
                            break;
                        }

                        tile = World.Map.GetTile((int) World.Player.Position.X + x, (int) (World.Player.Position.Y + y));
                        if (tile is not Static) {
                            tile = null;
                        }
                    }
                }
            }

            if (tile == null) {
                _usedMapPoints.Add(_nextPoint);
                _nextPoint = null;
                GameActions.MessageOverhead("Moving to next point", Player.Serial);
                _miningStuckTimer.Restart();
                return true;
            }

            if (_miningStuckTimer.ElapsedMilliseconds > _miningStuckWaitTime) {
                _usedMapPoints.Add(_nextPoint);
                _nextPoint = null;
                GameActions.MessageOverhead("Mining Stuck ... Moving to next point", Player.Serial);
                _miningStuckTimer.Restart();
                return true;
            }

            await Task.Delay(500);
            GameActions.DoubleClick(picks.First().Serial);
            await WaitForTargetting(3000);
            await Task.Delay(500);
            if (!TargetManager.IsTargeting) {
                return true;
            }

            var currentTime = DateTime.Now;
            TargetManager.Target(tile.Graphic, tile.X, tile.Y, tile.Z);
            await WaitForHelper.WaitFor(() => JournalHelper.GetJournalEntriesContaining("There is no metal here", currentTime).Count > 0 || JournalHelper.GetJournalEntriesContaining("You put some", currentTime).Count > 0, 3000);
            //await Task.Delay(2000);
            var noMetalEntries = JournalHelper.GetJournalEntriesContaining("There is no metal here", currentTime);
            var putSomeEntries = JournalHelper.GetJournalEntriesContaining("You put some", currentTime);

            if (noMetalEntries.Count > 0) {
                _usedMapPoints.Add(_nextPoint);
                _nextPoint = null;
                GameActions.MessageOverhead("Moving to next point", Player.Serial);
                _miningStuckTimer.Restart();
                return true;
            }

            if (putSomeEntries.Count > 0) {
                _miningStuckTimer.Restart();
            }

            //TargetManager.Target();
            //WaitForTargetting()

            //
            return true;
        }

        private MapPoint3D GetNextPoint() {
            if (_usedMapPoints.Count == MiningLocations.Count) {
                _usedMapPoints.Clear();
            }

            var unusedPoints = MiningLocations.Where(m => !_usedMapPoints.Contains(m)).ToList();

            var sortedMapPointsOnMap = unusedPoints.Where(m => m.MapIndex == World.MapIndex).ToList();

            if (sortedMapPointsOnMap.Count > 0) {
                return sortedMapPointsOnMap.OrderBy(m => m.Point.Distance()).First();
            }

            return unusedPoints.FirstOrDefault();
        }

        private async Task<List<AIItem>> HasPickaxe() {
            var picks = await ItemsHelper.GetPlayerBackpackItemsById(false, 0x0F39, 0);

            return picks;
        }
    }
}
