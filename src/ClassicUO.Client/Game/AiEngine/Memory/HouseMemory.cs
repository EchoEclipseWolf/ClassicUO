using ClassicUO.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.AiEngine.Memory
{
    internal class HouseMemory : BaseMemory
    {
        #region Loading
        private const string FILENAME = "HouseMemory.dat";
        internal static HouseMemory Instance;

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
                            Instance = JsonConvert.DeserializeObject<HouseMemory>(text);
                        }
                        catch (Exception e)
                        {
                            Instance = new HouseMemory();
                            Instance.LoadDefaults();
                            Instance.SaveFile(FILENAME);
                        }
                    }
                    else
                    {
                        Instance = new HouseMemory();
                        Instance.LoadDefaults();
                        Instance.SaveFile(FILENAME);
                    }
                }
            }
        }
        #endregion


    }
}