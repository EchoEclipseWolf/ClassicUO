using ClassicUO.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.AiEngine.Memory
{
    internal class MobilesMemory : BaseMemory
    {
        #region Loading
        private const string FILENAME = "MobilesMemory.dat";
        internal static MobilesMemory Instance;

        internal static void Load() {
            if (World.Player != null && World.Player.Serial > 0) {
                if (Instance == null || Instance.PlayerSerial != World.Player.Serial) {
                    var aiSettingsPath = Path.Combine(ProfileManager.ProfilePath, FILENAME);

                    if (File.Exists(aiSettingsPath)) {
                        try {
                            var text = File.ReadAllText(aiSettingsPath);
                            Instance = JsonConvert.DeserializeObject<MobilesMemory>(text);
                        }
                        catch (Exception e) {
                            Instance = new MobilesMemory();
                            Instance.LoadDefaults();
                            Instance.SaveFile(FILENAME);
                        }
                    }
                    else {
                        Instance = new MobilesMemory();
                        Instance.LoadDefaults();
                        Instance.SaveFile(FILENAME);
                    }
                }
            }
        }
        #endregion

        internal List<uint> PetSerials = new();
        internal List<Mobile> Pets = new();

        internal async Task<bool> Pulse() {
            var startingPetCount = PetSerials.Count;

            var healthBarGumps = GumpHelper.GetHealthbarGrumps();
            var healthGumpsInRange = new List<BaseHealthBarGump>();

            foreach (var healthBarGump in healthBarGumps) {
                if (healthBarGump is BaseHealthBarGump healthBar) {
                    var name = healthBar.Name;
                    var outOfRange = healthBar.IsInactive;

                    if (!outOfRange) {
                        var alreadyContains = healthGumpsInRange.Any(d => d.LocalSerial == healthBarGump.LocalSerial);

                        if (alreadyContains) {
                            continue;
                        }
                        healthGumpsInRange.Add(healthBar);
                    }
                }
            }

            var pets = new List<Mobile>();
            foreach (var gump in healthGumpsInRange) {
                Entity entity = World.Get(gump.LocalSerial);

                if (entity != null && entity is Mobile mobile) {
                    if (SerialHelper.IsValid(mobile.Serial) && World.OPL.TryGetNameAndData(mobile.Serial, out string name, out string data)) {
                        if (data.ToLower().Contains("(tame)") || data.ToLower().Contains("(bond")) {
                            var distance = mobile.Distance;

                            if (distance < 3) {
                                //PetSerials.Add(mobile.Serial);
                                pets.Add(mobile);
                                //GameActions.Print($"[MobileMemory]: Saved Pet: {name}");
                            }
                        }
                    }
                }
            }

            Pets = pets;

            if (startingPetCount != PetSerials.Count) {
                SaveFile(FILENAME);
            }


            return true;
        }
    }
}