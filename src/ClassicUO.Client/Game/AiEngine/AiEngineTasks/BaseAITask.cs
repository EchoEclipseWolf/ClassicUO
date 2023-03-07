using System.Threading.Tasks;

namespace ClassicUO.AiEngine.AiEngineTasks {
    public class BaseAITask {

        public virtual int Priority() {
            return 1;
        }
        
        public virtual async Task<bool> Pulse() {
            
            return true;
        }
    }
}