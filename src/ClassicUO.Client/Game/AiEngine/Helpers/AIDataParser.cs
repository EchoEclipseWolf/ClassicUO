using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming

namespace ClassicUO.Game.AiEngine.Helpers
{
    internal static class AIDataParser {
        private const string IntRegexString = "[0-9]+";
        private const string FloatRegexString = "[0-9]+\\.[0-9]+";
        internal static int ParseInt(string line) {
            var regex = new Regex(IntRegexString);
            var match = regex.Match(line);

            if (match.Success) {
                if (int.TryParse(match.Value, out var value)) {
                    return value;
                }
            }

            return 0;
        }

        internal static float ParseFloat(string line)
        {
            var regex = new Regex(FloatRegexString);
            var match = regex.Match(line);

            if (match.Success) {
                if (float.TryParse(match.Value, out var value)) {
                    return value;
                }
            }
            else {
                return ParseInt(line);
            }



            return 0;
        }
    }
}
