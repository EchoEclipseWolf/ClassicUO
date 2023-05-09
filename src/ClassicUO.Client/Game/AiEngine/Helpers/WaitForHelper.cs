using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.AiEngine.Helpers
{
    public static class WaitForHelper
    {
        public static async Task<bool> WaitFor(Func<bool> condition, int timeout)
        {
            var stopwatch = Stopwatch.StartNew();

            try {
                while (true)
                {
                    try {
                        if (condition()) {
                            return true;
                        }
                    }
                    catch (Exception e) {
                        Console.WriteLine(e);
                    }

                    if (stopwatch.ElapsedMilliseconds > timeout)
                    {
                        return false;
                    }
                }
            }
            catch (Exception e) {
                //Console.WriteLine(e);
                await Task.Delay(timeout);
            }
            

            return true;
        }
    }
}
