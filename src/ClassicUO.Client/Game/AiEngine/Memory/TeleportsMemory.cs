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
using ClassicUO.Game.AiEngine.Enums;

namespace ClassicUO.Game.AiEngine.Memory
{
    internal class TeleportsMemory : BaseMemory
    {
        #region Loading
        private const string FILENAME = "TeleportsMemory.dat";
        internal static TeleportsMemory Instance;

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
                            Instance = JsonConvert.DeserializeObject<TeleportsMemory>(text);
                        }
                        catch (Exception e)
                        {
                            Instance = new TeleportsMemory();
                            Instance.LoadDefaults();
                            Instance.SaveFile(FILENAME);
                        }
                    }
                    else
                    {
                        Instance = new TeleportsMemory();
                        Instance.LoadDefaults();
                        Instance.SaveFile(FILENAME);
                    }
                }
            }
        }
        #endregion

        public List<SingleRuneMemory> RuneMemories = new();

        public void AddRuneMemory(uint runebookSerial, uint containerSerial, byte index, ItemLocationEnum itemLocation, Point3D endLocation, int endMapIndex, string runeName) {
            if (RuneMemories.Any(r => r.RunebookSerial == runebookSerial && r.Index == index)) {
                return;
            }

            RuneMemories.Add(new SingleRuneMemory(runebookSerial, containerSerial, index, itemLocation, endLocation, endMapIndex, runeName));
            SaveFile(FILENAME);
        }
    }
}