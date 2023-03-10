using System.Diagnostics;
using System.Threading.Tasks;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.AiEngine.Tasks {
    public class SelfBandageAiTask : BaseAITask {
        private Stopwatch _bandageTimer = Stopwatch.StartNew();
        public SelfBandageAiTask() : base("Self Bandage Task")
        {
        }

        public override int Priority() {
            if (World.Player == null) {
                return 0;
            }
            return 100-World.Player.HitsPercentage;
        }
        
        public override async Task<bool> Pulse() {
            if (World.Player.HitsPercentage > 95 || !AiSettings.Instance.SelfBandageHealing || _bandageTimer.ElapsedMilliseconds < 1000) {
                return false;
            }

            var buffIcons = World.Player.BuffIcons;

            foreach (var buff in buffIcons) {
                if (buff.Key == BuffIconType.Healing && buff.Value != null && buff.Value.Graphic == 30102) {
                    return false;
                } 
            }

            if (buffIcons.ContainsKey(BuffIconType.Healing)) {
                return false;
            }
            
            Item bandage = World.Player.FindBandage();

            if (bandage != null) {
                GameActions.DoubleClick(bandage);
                var timer = Stopwatch.StartNew();

                while (timer.ElapsedMilliseconds < 1000) {
                    await Task.Delay(10);

                    if (TargetManager.IsTargeting) {
                        break;
                    }
                }
                TargetManager.Target(World.Player);
            }

            _bandageTimer = Stopwatch.StartNew();

            return true;
        }
    }
}