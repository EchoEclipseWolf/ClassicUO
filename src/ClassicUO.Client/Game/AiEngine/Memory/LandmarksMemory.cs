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

namespace ClassicUO.Game.AiEngine.Memory
{
    internal class LandmarksMemory : BaseMemory {
        #region Loading
        private const string FILENAME = "LandmarksMemory.dat";
        internal static LandmarksMemory Instance;

        internal static void Load() {
            if (World.Player != null && World.Player.Serial > 0) {
                if (Instance == null || Instance.PlayerSerial != World.Player.Serial) {
                    var aiSettingsPath = Path.Combine(ProfileManager.ProfilePath, FILENAME);

                    if (File.Exists(aiSettingsPath)) {
                        try {
                            var text = File.ReadAllText(aiSettingsPath);
                            Instance = JsonConvert.DeserializeObject<LandmarksMemory>(text);
                        }
                        catch (Exception e) {
                            Instance = new LandmarksMemory();
                            Instance.LoadDefaults();
                            Instance.SaveFile(FILENAME);
                        }
                    }
                    else {
                        Instance = new LandmarksMemory();
                        Instance.LoadDefaults();
                        Instance.SaveFile(FILENAME);
                    }
                }
            }
        }
        #endregion

        public List<ChampionPlatform> ChampionPlatforms = new();

        internal void AddChampionPlatform(uint serial, Point3D point, int mapIndex) {
            if (ChampionPlatforms.Any(c => c.Serial == serial || Equals(c.Point, point))) {
                return;
            }
            ChampionPlatforms.Add(new ChampionPlatform(point, mapIndex, serial));
            SaveFile(FILENAME);
            GameActions.Print($"[LandsmarkMemory] Added Champion Platform");
        }

        internal bool IsCloseToChampion(Point3D point, int mapIndex) {
            return ChampionPlatforms.Where(c => c.MapIndex == mapIndex).Any(championPlatform => championPlatform.Point.Distance(point) < 70);
        }
    }
}
