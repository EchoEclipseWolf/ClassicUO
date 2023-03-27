using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.AiEngine.Enums;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.AiEngine.Helpers
{
    internal static class ItemsHelper
    {
        public static async Task<Tuple<string, string>> GetItemData(Item item) {
            for (int i = 0; i < 30; i++) {
                if (SerialHelper.IsValid(item.Serial) && World.OPL.TryGetNameAndData(item.Serial, out string name, out string data)) {
                    return new Tuple<string, string>(name, data);
                }

                if (SerialHelper.IsValid(item.Serial))
                {
                    PacketHandlers.AddMegaClilocRequest(item.Serial);
                    await Task.Delay(50);
                }
            }
            
            return new Tuple<string, string>("", "");
        }
        public static List<Item> FindItemsOnGround(int itemId, int color = 0)
        {
            var foundItems = World.Items.Where(i => i.Value != null && i.Value.Graphic == itemId && (i.Value.Hue == color || color == -1) && i.Value.OnGround).ToList();

            return (from item in foundItems
                    where item.Value != null
                    select item.Value).ToList();
        }

        public static List<Item> FindContainersOnGround()
        {
            for (int j = 0; j < 50; j++) {
                try {
                    var foundItems = World.Items.Where(i => i.Value != null && ContainerIds.Ids.Contains(i.Value.Graphic) && i.Value.OnGround).ToList();

                    return (from item in foundItems
                            where item.Value != null
                            select item.Value).ToList();
                }
                catch (Exception) {
                }
            }

            return new List<Item>();
        }

        public static async Task<List<Tuple<uint,Item>>> GetPlayerBackpackItemsById(ushort graphicId, int hue = -1) {
            var items = await GetPlayerBackpackItems(true);

            return items.Where(item => item.Item2.Graphic == graphicId && (hue == -1 || item.Item2.Hue == hue)).ToList();
        }

        public static async Task<List<Tuple<uint, Item>>> GetPlayerBackpackItems(bool searchSubContainers)
        {
            var backpack = World.Player.FindItemByLayer(Layer.Backpack);
            return await GetContainerItems(backpack, searchSubContainers);
        }

        public static async Task<int> GetPlayerBackpackItemCount(bool searchSubContainers = true, bool countStacks = true) {
            var count = 0;
            var backpack = World.Player.FindItemByLayer(Layer.Backpack);
            var items = await GetContainerItems(backpack, true);

            foreach (var itemTuple in items) {
                if (countStacks) {
                    count += itemTuple.Item2.Amount;
                }
                else {
                    count++;
                }
            }

            return count;
        }

        public static async Task<Item> FindItemBySerial(uint serial, uint containerSerial) {
            if(World.Items.TryGetValue(containerSerial, out var container)) {
                var items = await GetContainerItems(container, false);

                return (from item in items
                        where item.Item2 != null && item.Item2.Serial == serial
                        select item.Item2).FirstOrDefault();
            }
            return null;
        }

        public static async Task<List<Tuple<uint, Item>>> GetContainerItems(Item container, bool searchSubContainers) {
            var list = new List<Tuple<uint, Item>>();
            var bag = World.Items.Get(container.Serial);

            if (bag != null && !bag.IsEmpty) {
                for (LinkedObject i = bag.Items; i != null; i = i.Next) {
                    Item item = (Item)i;
                    list.Add(new Tuple<uint, Item>(container.Serial, item));

                    if (searchSubContainers && false) {
                        await GetContainerItemsRecursive(list, item);
                    }
                }
            }

            return list;
        }

        private static async Task<bool> GetContainerItemsRecursive(List<Tuple<uint, Item>> list, Item container)
        {
            if (container != null && ContainerIds.Ids.Contains(container.Graphic))
            {
                if (container.IsEmpty) {
                    GameActions.DoubleClick(container.Serial);
                    await WaitForHelper.WaitFor(() => !container.IsEmpty, 4000);

                    if (container.IsEmpty) {
                        return false;
                    }
                }

                for (LinkedObject i = container.Items; i != null; i = i.Next)
                {
                    Item item = (Item)i;
                    list.Add(new Tuple<uint, Item>(container.Serial, item));

                    if (!item.IsEmpty) {
                        await GetContainerItemsRecursive(list, item);
                    }
                }
            }

            return true;
        }

        public static List<Tuple<Item, string, Layer, string>> GetEquippedItemsWithData()
        {
            var list = new List<Tuple<Item, string, Layer, string>>();

            for (byte i = 0; i < 0x19; i++) {
                if (i == (int) Layer.Backpack ||
                    i == (int) Layer.Hair ||
                    i == (int) Layer.Face) {
                    continue;
                }

                var slot = (Layer) i;
                var item = World.Player.FindItemByLayer((Layer) i);

                if (item != null) {
                    if (SerialHelper.IsValid(item.Serial) && World.OPL.TryGetNameAndData(item.Serial, out string name, out string data)) {
                        list.Add(new Tuple<Item, string, Layer, string>(item, data, slot, name));
                    }
                    else if(SerialHelper.IsValid(item.Serial)) {
                        PacketHandlers.AddMegaClilocRequest(item.Serial);
                    }
                }
            }


            return list;
        }

        public static bool IsEquipped(uint serial) {
            return ItemDataUpdateTask.EquippedItems.Any(i => i.Serial == serial);
        }
    }
}
