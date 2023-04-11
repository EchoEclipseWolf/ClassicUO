using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkatoshQuester.Helpers.LightGeometry;
using ClassicUO.Configuration;
using Newtonsoft.Json;
using static ClassicUO.Utility.Profiler;
// ReSharper disable InconsistentNaming

namespace ClassicUO.Game.AiEngine.Memory
{
    internal class AiSettings {
        private const string FILENAME = "AiSettings.config";
        internal static AiSettings Instance { get; private set; }


        //Settings
        public uint PlayerSerial = 0;
        public bool NavigationTesting = false;
        public bool NavigationRecording = false;
        public bool NavigationRecordingUsePathfinder = false;
        public bool NavigationMovement = false;
        public bool RecordDatabase = false;
        public bool RecordDatabaseInvuls = true;
        public bool SelfBandageHealing = false;
        public bool SelfBuff = false;
        public bool UpdateContainers = true;
        public string ScriptToRun = "";
        public Point3D TestingNavigationPoint = new(1015, 520, -70);
        public int TestingNavigationMapIndex = 3;
        public double SelfHealPercent = 90;
        public double PetHealPercent = 84;
        public bool AllowPetMovementControl = false;


        private readonly string _aiSettingsPath;

        internal static void Load() {
            if (World.Player != null && World.Player.Serial > 0) {
                if (Instance == null || Instance.PlayerSerial != World.Player.Serial) {
                    var aiSettingsPath = Path.Combine(ProfileManager.ProfilePath, FILENAME);

                    if (File.Exists(aiSettingsPath)) {
                        try {
                            var text = File.ReadAllText(aiSettingsPath);
                            Instance = JsonConvert.DeserializeObject<AiSettings>(text);
                            Instance?.LoadOverrides();
                        }
                        catch (Exception e) {
                            Instance = new AiSettings();
                            Instance.LoadDefaults();
                            Instance.SaveFile();
                        }
                    }
                    else {
                        Instance = new AiSettings();
                        Instance.LoadDefaults();
                        Instance.SaveFile();
                    }
                }
            }
        }

        internal static void Save() {
            if (Instance != null) {
                Instance.SaveFile();
            }
        }

        internal AiSettings() {
            var profilePath = ProfileManager.ProfilePath;

            if (string.IsNullOrEmpty(profilePath)) {
                return;
            }

            _aiSettingsPath = Path.Combine(profilePath, FILENAME);
        }

        private void LoadDefaults() {
            RecordDatabase = true;

            LoadOverrides();
        }

        private void LoadOverrides() {
            AllowPetMovementControl = false;
            PlayerSerial = World.Player.Serial;
        }

        private void SaveFile() {
            var serializedJson = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(_aiSettingsPath, serializedJson);
            GameActions.MessageOverhead("Saved AiSettings", World.Player.Serial);
        }

    }
}
