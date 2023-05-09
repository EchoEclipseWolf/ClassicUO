using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.AiEngine;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.AiClasses;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.AiEngine.Tasks
{
    public class DepositItemsIntoChestTask : BaseAITask {
        private Point3D _chestLocation;
        public DepositItemsIntoChestTask(Point3D chestLocation) : base("Deposit Items Into Chest Task") {
            _chestLocation = chestLocation;
        }

        public override int Priority() {
            return 1;
        }
        
        public async Task<bool> Pulse(List<AIItem> itemsToDeposit)
        {
            if (Equals(_chestLocation, Point3D.Empty) || itemsToDeposit.Count == 0) {
                return false;
            }

            var distanceToChest = _chestLocation.Distance();

            if (distanceToChest > 2) {
                await Navigation.NavigateTo(_chestLocation, 1, true);
                return true;
            }

            var items = World.Items.Values.ToList();
            var chest = items.FirstOrDefault(i => Equals(i.Position.ToPoint3D(), _chestLocation));

            if (chest != null) {
                foreach (var item in itemsToDeposit) {
                    if (item == null || item.Item == null) {
                        continue;
                    }

                    GameActions.PickUp(item.Serial, 0, 0);
                    await Task.Delay(600);
                    GameActions.DropItem(item.Serial, 65535, 65535, 0, chest.Serial);
                    await Task.Delay(600);
                }
            }
            
            return true;
        }
    }
}