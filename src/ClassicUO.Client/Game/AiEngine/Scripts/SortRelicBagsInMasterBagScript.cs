using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Enums;
using System.Text.RegularExpressions;
using System.Globalization;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.Game.AiEngine.Tasks;
using ClassicUO.Game.AiEngine;
using ClassicUO.Game.AiEngine.AiClasses;

namespace ClassicUO.AiEngine.AiEngineTasks
{
    public class SortRelicBagsInMasterBagScript : BaseAITask
    {
        public SortRelicBagsInMasterBagScript() : base("Sort Relic Bags MasterLoot")
        {
        }

        public override async Task<bool> Start() {
            if (!await base.Start()) {
                return false;
            }

            if (ItemDataUpdateTask.PlayerBackpack != null) {
                await ItemDataUpdateTask.PlayerBackpack.UpdateContents(true);
            }

            return true;
        }

        public override async Task<bool> Pulse() {
            if (ItemDataUpdateTask.MasterLootContainer == null || ItemDataUpdateTask.TrashTokensContainer == null) {
                GameActions.MessageOverhead($"Failed to find master loot storage or trash tokens bag", Player.Serial);
                AiCore.Instance.StopScript();
                return true;
            }

            var allRelicBags = ItemDataUpdateTask.MasterLootContainer.SubContainers.Where(s => (s.Name != null && s.Name.ToLower().Contains("relic")) || (s.ContainerItem != null && s.ContainerItem.Graphic == 0x0E76 && s.ContainerItem.Hue == 0x0496)).ToList();

            foreach (var relicBag in allRelicBags) {
                foreach (var item in relicBag.GetItems()) {
                    if (ShouldKeepItem(item)) {
                        GameActions.Print($"[SortRelicBags] Keeping - {item.Name}  Score: {item.AiScore}");
                        await GrabItem(item);

                        continue;
                    }

                    if (ShouldTrashItem(item)) {
                        GameActions.Print($"[SortRelicBags] Trashing - {item.Name}  Score: {item.AiScore}");
                        await TrashItem(item);

                        continue;
                    }
                }

                await relicBag.UpdateContents(true);

                if (relicBag.Count == 0) {
                    GameActions.PickUp(relicBag.Serial, 0, 0);
                    await Task.Delay(900);
                    GameActions.DropItem(relicBag.Serial, 0, 0, 0, ItemDataUpdateTask.TrashTokensContainer.Serial);
                    await Task.Delay(900);
                }
            }

            return true;
        }

        private async Task<bool> GrabItem(AIItem item) {
            GameActions.GrabItem(item.Serial, 1);
            await Task.Delay(700);

            return true;
        }

        private async Task<bool> TrashItem(AIItem item) {
            GameActions.PickUp(item.Serial, 0, 0);
            await Task.Delay(700);
            if (item.Blessed) {
                GameActions.DropItem(item.Serial, (int) (World.Player.Position.X + 1), (int) (World.Player.Position.Y + 1), (int) (World.Player.Position.Z), 0xFFFF_FFFF);
            }
            else {
                
                GameActions.DropItem(item.Serial, 0, 0, 0, ItemDataUpdateTask.TrashTokensContainer.Serial);
                
            }
            await Task.Delay(700);

            return true;
        }

        private bool ShouldKeepItem(AIItem item) {
            if (item.Item != null) {
                if (item.Item.Graphic == 0x14F0 ||
                    item.Item.Graphic == 0x0F19 ||
                    item.Item.Graphic == 0x1F14 ||
                    (item.Item.Graphic == 0x1AE4 && item.Name.ToLower().Contains("mythic")) ||
                    (item.Item.Graphic == 0x0F26 && item.Name.ToLower().Contains("ancient"))) {
                    return true;
                }
            }

            if (item.BattleRating is >= 750) {
                return true;
            }

            return false;
        }

        private bool ShouldTrashItem(AIItem item) {
            if (item.Item != null) {
                if (item.Item.Graphic == 0x1779 ||
                    item.Item.Graphic == 0x0F8E ||
                    item.Item.Graphic == 0x1F08 ||
                    item.Item.Graphic == 0x1540 ||
                    item.Item.Graphic == 0x14F3 ||
                    (item.Item.Graphic == 0x1AE4 && !item.Name.ToLower().Contains("mythic"))) {
                    return true;
                }
            }

            if (item.IsArmor && item.AiScore == 0 && item.BattleRating == 0) {
                return true;
            }

            if (item.MaxDurability > 0 && item.BattleRating < 750) {
                return true;
            }

            if (item.BattleRating is > 0 and < 750) {
                return true;
            }

            return false;
        }
    }
}
