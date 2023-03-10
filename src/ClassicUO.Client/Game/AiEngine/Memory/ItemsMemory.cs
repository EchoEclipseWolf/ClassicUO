using ClassicUO.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.AiEngine.AiClasses;
using ClassicUO.Game.AiEngine.Enums;

namespace ClassicUO.Game.AiEngine.Memory
{
    internal class ItemsMemory : BaseMemory
    {
        #region Loading
        private const string FILENAME = "ItemsMemory.dat";
        internal static ItemsMemory Instance;

        internal static void Load() {
            if (World.Player != null && World.Player.Serial > 0) {
                if (Instance == null || Instance.PlayerSerial != World.Player.Serial) {
                    var aiSettingsPath = Path.Combine(ProfileManager.ProfilePath, FILENAME);

                    if (File.Exists(aiSettingsPath)) {
                        try {
                            var text = File.ReadAllText(aiSettingsPath);
                            Instance = JsonConvert.DeserializeObject<ItemsMemory>(text);
                        }
                        catch (Exception e) {
                            Instance = new ItemsMemory();
                            Instance.LoadDefaults();
                            Instance.SaveFile(FILENAME);
                        }
                    }
                    else {
                        Instance = new ItemsMemory();
                        Instance.LoadDefaults();
                        Instance.SaveFile(FILENAME);
                    }
                }
            }
        }
        #endregion

        public List<SingleItemMemory> Runebooks = new();

        internal void AddRunebook(Item item, uint containerSerial, ItemLocationEnum itemLocation) {
            if (item == null) {
                return;
            }

            if (Runebooks.All(r => r.Serial != item.Serial)) {
                Runebooks.Add(new SingleItemMemory(item, containerSerial, itemLocation));
                SaveFile(FILENAME);
            }

            var existing = Runebooks.FirstOrDefault(r => r.Serial == item.Serial);

            if (existing != null && existing.ContainerSerial != containerSerial) {
                // Runebook has moved lets resave it
                existing.MovedContainer(containerSerial);
                SaveFile(FILENAME);
            }
        }
    }
}
