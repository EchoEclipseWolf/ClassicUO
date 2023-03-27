using ClassicUO.Game.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.Game.AiEngine.Enums;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Network;

namespace ClassicUO.Game.AiEngine.AiClasses
{
    public class AiContainer {
        internal static List<AiContainer> CachedContainers = new();

        public uint Count;
        public uint MaxCount = 50;
        public uint Stones;
        public uint StonesMax;
        public uint Serial;
        public uint ParentContainerSerial;
        internal Item ContainerItem;
        public List<AiContainer> SubContainers = new();
        public Point3D LastPosition = Point3D.Empty;
        public int MapIndex;

        public List<AIItem> _items = new();

        private const string BAG_CONTENTS_FOUR_NUMBER_REGEX_STRING = "Contents: *(\\d+)\\/(\\d+) *Items, *(\\d+)/(\\d+) *Stones";
        private readonly Regex _bagContentsFourNumberRegex = new (BAG_CONTENTS_FOUR_NUMBER_REGEX_STRING);
        private const string BAG_CONTENTS_THREE_NUMBER_REGEX_STRING = "Contents: *(\\d+)\\/(\\d+) *Items, *(\\d+) *Stones";
        private readonly Regex _bagContentsThreeNumberRegex = new (BAG_CONTENTS_THREE_NUMBER_REGEX_STRING);
        private const string BAG_CONTENTS_TWO_NUMBER_REGEX_STRING = "Contents: *(\\d+)\\/(\\d+) *Items";
        private readonly Regex _bagContentsTwoNumberRegex = new (BAG_CONTENTS_TWO_NUMBER_REGEX_STRING);

        internal static AiContainer GetContainer(uint serial) {
            var container = CachedContainers.FirstOrDefault(c => c.Serial == serial);

            if (container == null) {
                container = new AiContainer(serial, null);
                CachedContainers.Add(container);
            }

            return container;
        }

        internal AiContainer() {

        }

        internal AiContainer(uint serial, AiContainer parentContainer) {
            Serial = serial;

            if (parentContainer != null) {
                ParentContainerSerial = parentContainer.Serial;
            }

            ContainerItem = World.Items.Get(Serial);
            MapIndex = World.MapIndex;
        }

        internal AiContainer FindSubContainerBySerial(uint serial) {
            if (Serial == serial) {
                return this;
            }

            foreach (var subContainer in SubContainers) {
                var found = subContainer.FindSubContainerBySerial(serial);
                if (found != null) {
                    return found;
                }
            }

            return null;
        }

        internal async Task<bool> UpdatePosition() {
            if (ContainerItem == null) {
                ContainerItem = World.Items.Get(Serial);
            }

            if (ContainerItem != null) {
                LastPosition = ContainerItem.Position.ToPoint3D();
                if (Equals(LastPosition, Point3D.Empty)) {
                    for (int i = 0; i < 3; i++) {
                        if (SerialHelper.IsValid(Serial) && World.OPL.TryGetNameAndData(Serial, out string name, out string data)) {
                            LastPosition = ContainerItem.Position.ToPoint3D();

                            

                            break;
                        }

                        if (SerialHelper.IsValid(Serial)) {
                            PacketHandlers.AddMegaClilocRequest(Serial);
                            await Task.Delay(250);
                        }
                    }
                }
            }

            

            return true;
        }

        internal async Task<bool> UpdateCount() {
            var beforeCount = Count;
            var beforeStones = Stones;

            if (ContainerItem == null) {
                ContainerItem = World.Items.Get(Serial);
            }

            if (ContainerItem != null) {
                await UpdatePosition();
            }

            for (int i = 0; i < 10; i++) {
                if (SerialHelper.IsValid(Serial) && World.OPL.TryGetNameAndData(Serial, out string name, out string data)) {
                    var match = _bagContentsFourNumberRegex.Match(data);

                    if (match.Success && match.Groups.Count > 4) {
                        Count = uint.Parse(match.Groups[1].Value);
                        MaxCount = uint.Parse(match.Groups[2].Value);
                        Stones = uint.Parse(match.Groups[3].Value);
                        StonesMax = uint.Parse(match.Groups[4].Value);

                        if (ItemDataUpdateTask.PlayerBackpack.Serial == Serial) {
                            Count += 1;
                        }

                        if (Count != beforeCount || Stones != beforeStones) {
                            return true;
                        }
                    }
                    else {

                        match = _bagContentsThreeNumberRegex.Match(data);

                        if (match.Success && match.Groups.Count > 3) {
                            Count = uint.Parse(match.Groups[1].Value);
                            MaxCount = uint.Parse(match.Groups[2].Value);
                            Stones = uint.Parse(match.Groups[3].Value);
                            StonesMax = 99999999;

                            if (ItemDataUpdateTask.PlayerBackpack.Serial == Serial) {
                                Count += 1;
                            }

                            if (Count != beforeCount || Stones != beforeStones) {
                                return true;
                            }
                        }
                        else {
                            match = _bagContentsTwoNumberRegex.Match(data);

                            if (match.Success && match.Groups.Count > 2) {
                                Count = uint.Parse(match.Groups[1].Value);
                                MaxCount = uint.Parse(match.Groups[2].Value);
                                Stones = 0;
                                StonesMax = 99999999;

                                if (ItemDataUpdateTask.PlayerBackpack.Serial == Serial) {
                                    Count += 1;
                                }

                                if (Count != beforeCount || Stones != beforeStones) {
                                    return true;
                                }
                            }
                        }
                    }

                    break;
                } 
                
                if (SerialHelper.IsValid(Serial)) {
                    PacketHandlers.AddMegaClilocRequest(Serial);
                    await Task.Delay(250);
                }
            }

            if (Count != GetItems().Count) {
                return true;
            }
            return false;
        }

        private async Task<bool> UpdateSubContainerList() {
            if (ContainerItem != null) {
                var allSubContainers = new List<AiContainer>();

                var items = await ItemsHelper.GetContainerItems(ContainerItem, false);

                foreach ((uint _, Item item2) in items) {
                    if (item2 != null && ContainerIds.Ids.Contains(item2.Graphic)) {
                        (string name, string data) = await ItemsHelper.GetItemData(item2);

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(data)) {
                            if (data.ToLower().Contains("contents")) {
                                var container = new AiContainer(item2.Serial, this);
                                container.ContainerItem = item2;
                                allSubContainers.Add(container);
                            }
                        }
                    }
                }

                SubContainers = allSubContainers;
            }

            return true;
        }

        private async Task<bool> UpdateSubContainers() {
            await UpdateSubContainerList();

            foreach (var subContainer in SubContainers) {
                if (subContainer.ContainerItem != null && !ContainerIds.Ids.Contains(subContainer.ContainerItem.Graphic)) {
                    int failed = 1;
                    continue;
                }

                if (await subContainer.UpdateCount()) {
                    await subContainer.UpdateContents();
                }
            }

            return true;
        }

        internal async Task<bool> UpdateContents() {
            await UpdateSubContainers();

            var containerGumps = GumpHelper.GetContainerGrumps();

            if (Count == 0 && _items.Count > 0) {
                _items.Clear();
            }

            if (await UpdateCount() && Count > 0) {
                if (ContainerItem == null) {
                    return true;
                }

                var items = await ItemsHelper.GetContainerItems(ContainerItem, false);

                if (Count == 0 && items.Count == 0) {
                    _items.Clear();

                    return true;
                }

                var tas = GetItems();

                if (containerGumps.All(g => g.LocalSerial != Serial)) {
                    for (int i = 0; i < 5; i++) {
                        if (ContainerItem != null && !ContainerIds.Ids.Contains(ContainerItem.Graphic)) {
                            int failed = 1;

                            continue;
                        }

                        // ReSharper disable once PossibleNullReferenceException
                        GameActions.DoubleClick(ContainerItem.Serial);
                        await WaitForHelper.WaitFor(() => !ContainerItem.IsEmpty, 4000);
                        await Task.Delay(1000);
                        items = await ItemsHelper.GetContainerItems(ContainerItem, false);

                        if (items.Count > 0) {
                            break;
                        }
                    }
                }


                var allItems = new List<AIItem>();

                foreach ((uint item1, Item item2) in items) {
                    (string nameItem, string dataItem) = await ItemsHelper.GetItemData(item2);

                    if (!string.IsNullOrEmpty(nameItem)) {
                        var aiItem = new AIItem(item2.Serial, new Tuple<Item, string, string>(item2, nameItem, dataItem));
                        aiItem.ContainerSerial = Serial;
                        allItems.Add(aiItem);
                    }
                }

                _items = allItems;
                await UpdateSubContainers();
            }

            return true;
        }

        private bool SubContainersContains(Item item) {
            return SubContainers.Any(subContainer => subContainer.Serial == item.Serial);
        }

        internal bool ContainsSerial(uint serial, bool searchRecursive) {
            if (_items.Any(item => item.Serial == serial)) {
                return true;
            }

            return searchRecursive && SubContainers.Any(subContainer => subContainer.ContainsSerial(serial, true));
        }

        internal List<AIItem> GetItems(bool recursive = true) {
            var items = new List<AIItem>();
            items.AddRange(_items);

            if (recursive) {
                foreach (var subContainer in SubContainers) {
                    items.AddRange(subContainer.GetItems(true));
                }
            }

            return items;
        }

        internal double Distance() {
            if (ContainerItem != null) {
                return ContainerItem.Position.ToPoint3D().Distance();
            }

            return LastPosition.Distance();
        }

        internal Point3D Point() {
            return ContainerItem != null ? ContainerItem.Position.ToPoint3D() : Point3D.Empty;
        }

        public override string ToString() {
            return $"Container - Items: {Count} / {MaxCount}   Weight: {Stones} / {StonesMax}   SubContainers: {SubContainers.Count}   ItemCount: {GetItems().Count}";
        }
    }
}
