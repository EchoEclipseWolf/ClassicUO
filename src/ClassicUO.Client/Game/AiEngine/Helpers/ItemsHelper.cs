using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.AiEngine.Enums;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.AiEngine.Helpers
{
    internal static class ItemsHelper
    {
        public static List<Item> FindItemsOnGround(int itemId, int color = 0)
        {
            var foundItems = World.Items.Where(i => i.Value != null && i.Value.Graphic == itemId && (i.Value.Hue == color || color == -1) && i.Value.OnGround).ToList();

            return (from item in foundItems
                    where item.Value != null
                    select item.Value).ToList();
        }

        public static async Task<List<Tuple<uint,Item>>> GetPlayerBackpackItemsById(ushort graphicId, int hue = -1) {
            var items = await GetPlayerBackpackItems(true);

            return items.Where(item => item.Item2.Graphic == graphicId && (hue == -1 || item.Item2.Hue == hue)).ToList();
        }

        public static async Task<List<Tuple<uint, Item>>> GetPlayerBackpackItems(bool searchSubContainers)
        {
            var backpack = World.Player.FindItemByLayer(Layer.Backpack);
            return await GetContainerItems(backpack, true);
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
            var backpack = World.Player.FindItemByLayer(Layer.Backpack);

            if (backpack != null && !backpack.IsEmpty) {
                for (LinkedObject i = backpack.Items; i != null; i = i.Next) {
                    Item item = (Item)i;
                    list.Add(new Tuple<uint, Item>(container.Serial, item));

                    if (searchSubContainers) {
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
    }
}
