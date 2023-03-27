using ClassicUO.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.Game.AiEngine.AiClasses;
using System.Diagnostics;
using ClassicUO.AiEngine;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using System.ComponentModel;

namespace ClassicUO.Game.AiEngine.Memory
{

    internal class HouseMemory : BaseMemory
    {
        #region Loading
        private const string FILENAME = "HouseMemory.dat";
        internal static HouseMemory Instance;

        internal static void Load()
        {
            if (World.Player != null && World.Player.Serial > 0)
            {
                if (Instance == null || Instance.PlayerSerial != World.Player.Serial)
                {
                    var aiSettingsPath = Path.Combine(ProfileManager.ProfilePath, FILENAME);

                    if (File.Exists(aiSettingsPath))
                    {
                        try
                        {
                            var text = File.ReadAllText(aiSettingsPath);
                            Instance = JsonConvert.DeserializeObject<HouseMemory>(text);
                        }
                        catch (Exception e)
                        {
                            Instance = new HouseMemory();
                            Instance.LoadDefaults();
                            Instance.SaveFile(FILENAME);
                        }
                    }
                    else
                    {
                        Instance = new HouseMemory();
                        Instance.LoadDefaults();
                        Instance.SaveFile(FILENAME);
                    }
                }
            }
        }
        #endregion

        public List<AiHouse> Houses = new();

        private readonly Stopwatch _highlightContainersStopwatch = Stopwatch.StartNew();
        private const int HIGHLIGHT_CONTAINERS_DELAY = 18000;
        public HashSet<uint> ContainersToHighlight = new();
        private readonly List<Tuple<AiContainer, AiContainer>> _foundSearchContainers = new();
        private bool _openingContainer = false;

        public void SearchForItemsToHighlight(string name) {
            ContainersToHighlight.Clear();
            _foundSearchContainers.Clear();
            var foundContainers = new List<AiContainer>();
            int foundCount = 0;

            foreach (var houseToSearch in Houses.Where(h => h.MapIndex == World.MapIndex)) {
                foreach (var container in houseToSearch.Containers.Values) {
                    var containerItems = container.GetItems();

                    foreach (var item in containerItems) {
                        if (item.Name.ToLower().Contains(name.ToLower())) {
                            ++foundCount;
                            foundContainers.Add(container);

                            var finalContainer = houseToSearch.FindSubContainerBySerial(item.ContainerSerial);
                            if (finalContainer != null) {
                                _foundSearchContainers.Add(new Tuple<AiContainer, AiContainer>(container, finalContainer));
                            }
                        }
                    }
                }
            }

            foreach (var container in foundContainers) {
                ContainersToHighlight.Add(container.Serial);
            }


            if (foundCount > 0) {
                _highlightContainersStopwatch.Restart();
            }

            GameActions.MessageOverhead($"Found {ContainersToHighlight.Count} containers  {foundCount} Items", World.Player.Serial);
        }

        public void OpenClosestContainer() {
            _openingContainer = true;
        }

        public async Task<bool> OpenContainer() {
            if (!_openingContainer) {
                return false;
            }

            if (_foundSearchContainers.Count == 0) {
                _openingContainer = false;

                return false;
            }

            AiContainer closestContainer = null;
            AiContainer finalContainer = null;
            double bestDistance = double.MaxValue - 1;

            foreach ((AiContainer rootContainer, AiContainer finalContainerTuple) in _foundSearchContainers) {
                if (rootContainer.Distance() < bestDistance) {
                    bestDistance = rootContainer.Distance();
                    closestContainer = rootContainer;
                    finalContainer = finalContainerTuple;
                }
            }

            if (closestContainer == null || finalContainer == null) {
                _openingContainer = false;

                return false;
            }

            var rootDistance = closestContainer.Distance();

            if (rootDistance > 16) {
                await Navigation.NavigateTo(closestContainer.Point(), closestContainer.MapIndex, true);

                return true;
            }

            if (finalContainer == closestContainer) {
                await InteractWithContainer(closestContainer);
                GameActions.MessageOverhead("Found and opened container", World.Player.Serial);
                _openingContainer = false;

                return true;
            }

            var openGumpInChain = FindChainGumpOpen(finalContainer);


            if (openGumpInChain == finalContainer.Serial) {
                GameActions.MessageOverhead("Found and opened container", World.Player.Serial);
                _openingContainer = false;

                return true;
            }

            if (openGumpInChain == 0) {
                await InteractWithContainer(closestContainer);

                return true;
            }

            var chainSerials = FindChainSerials(finalContainer);

            var next = finalContainer;

            for (int i = 0; i < chainSerials.Count; i++) {
                var nextContainer = FindContainer(chainSerials[i]);

                var gump = GumpHelper.GetContainerGrumpByItemSerial(nextContainer.Serial);

                if (gump != null) {
                    continue;
                }

                if (nextContainer != null) {
                    await InteractWithContainer(nextContainer);
                }
            }

            var containerGump = GumpHelper.GetContainerGrumpByItemSerial(finalContainer.Serial);

            if (openGumpInChain == finalContainer.Serial || containerGump != null) {
                GameActions.MessageOverhead("Found and opened container", World.Player.Serial);
                _openingContainer = false;

                return true;
            }

            GameActions.MessageOverhead("Failed to open containers", World.Player.Serial);
            _openingContainer = false;

            return true;

        }

        private uint FindChainGumpOpen(AiContainer container) {
            var next = container;
            for (var i = 0; i < 50; i++) {
                var containerGump = GumpHelper.GetContainerGrumpByItemSerial(next.Serial);

                if (containerGump != null) {
                    return next.Serial;
                }

                next = FindParentContainer(next);

                if (next == null) {
                    return 0;
                }
            }
            return 0;
        }

        private AiContainer FindContainer(uint serial) {
            foreach (var house in Houses) {
                return house.FindSubContainerBySerial(serial);
            }

            return null;
        }

        private List<uint> FindChainSerials(AiContainer finalContainer) {
            var list = new List<uint>();
            var next = finalContainer;
            for (var i = 0; i < 50; i++) {
                foreach (var house in Houses) {
                    if (next == null) {
                        list.Reverse();
                        return list;
                    }
                    var found = house.FindSubContainerBySerial(next.Serial);

                    if (found != null) {
                        list.Add(found.Serial);
                    }
                }
                next = FindParentContainer(next);
            }

            list.Reverse();
            return list;
        }

        private AiContainer FindParentContainer(AiContainer currentContainer) {
            foreach (var house in Houses) {
                var next = house.FindSubContainerBySerial(currentContainer.ParentContainerSerial);

                if (next != null) {
                    return next;
                }
            }
            return null;
        }

        private async Task<bool> InteractWithContainer(AiContainer container) {
            var worldContainer = World.Items.Get(container.Serial);

            if (worldContainer == null) {
                return true;
            }

            GameActions.DoubleClick(container.Serial);
            await Task.Delay(1000);

            return true;
        }

        public void Update() {
            if (_highlightContainersStopwatch.ElapsedMilliseconds > HIGHLIGHT_CONTAINERS_DELAY) {
                ContainersToHighlight.Clear();
                _highlightContainersStopwatch.Reset();
            }
        }

        internal void AddHouse(AiHouse house) {
            if (house.IsValid()) {
                Houses.Add(house);
                SaveFile(FILENAME);
            }
        }

        internal void Save() {
            SaveFile(FILENAME);
        }
    }
}