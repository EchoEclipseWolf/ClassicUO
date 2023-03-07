using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game;

namespace ClassicUO.AiEngine {
    public class AiEngine {
        public static AiEngine Instance = new AiEngine();

        public List<BaseAITask> _tasks = new List<BaseAITask>();
        public bool Navigation = false;
        public bool SelfBandageHealing = false;
        public bool TamingTraining = false;

        public AiEngine() {
            _tasks.Add(new SelfBandageAiTask());
        }

        public async Task<bool> Loop() {
            while (true) {
                try {
                    await Pulse();
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
            return true;
        }

        public async Task<bool> Pulse() {
            if (World.Player == null || World.Player.Name == null || World.Player.Name.Length == 0) {
                await Task.Delay(10);

                return true;
            }

            foreach (var task in _tasks.OrderByDescending(t => t.Priority())) {
                
                if (await task.Pulse()) {
                    await Task.Delay(10);
                    return true;
                }
            }
            await Task.Delay(10);
            return true;
        }
    }
}