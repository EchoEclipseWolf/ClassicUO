using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
// ReSharper disable InconsistentNaming

namespace ClassicUO.Game.AiEngine.Helpers
{
    internal class AnimalBODEntry {
        internal string Name;
        internal int Amount;
        internal int MaxAmount;

        internal AnimalBODEntry(string name, int amount, int maxAmount) {
            Name = name;
            Amount = amount;
            MaxAmount = maxAmount;
        }

        public override string ToString() {
            return $"AnimalBODEntry {Name} {Amount}/{MaxAmount}";
        }
    }

    internal class BaseAnimalBOD {
        internal List<AnimalBODEntry> Entires = new List<AnimalBODEntry>();

        protected string ReadLabelText(Control control) {
            if (control is Label label) {
                return label.Text;
            }
            return "";
        }

        protected string ReadHtmlLabelText(Control control) {
            if (control is HtmlControl label) {
                return label.Text;
            }
            return "";
        }

        public override string ToString() {
            if (Entires.Count == 0) {
                return "BaseAnimalBOD Empty";
            }

            return $"BaseAnimalBOD {Entires.Count} entries  {Entires.First().Name}";
        }
    }

    internal class LargeAnimalBOD : BaseAnimalBOD {
        

        internal int Parse(List<Control> children) {
            if (Entires.Count > 0) {
                return 0;
            }

            int usedCount = 0;
            children.RemoveAt(0);
            usedCount++;
            children.RemoveAt(0);
            usedCount++;

            var price = ReadLabelText(children[0]);
            children.RemoveAt(0);
            usedCount++;

            children.RemoveAt(0);//Large or Small label
            usedCount++;

            while (children.Count > 0) {

                var animalName = ReadLabelText(children[0]);
                children.RemoveAt(0);
                usedCount++;

                children.RemoveAt(0); //Spacer Label
                usedCount++;

                var animalMinMax = ReadLabelText(children[0]);
                children.RemoveAt(0);
                usedCount++;

                var split = animalMinMax.Split(' ');

                if (int.TryParse(split[0], out var amount) && int.TryParse(split[2], out var maxAmount)) {
                    var animalEntry = new AnimalBODEntry(animalName, amount, maxAmount);
                    Entires.Add(animalEntry);
                }


                if (children.Count == 0) {
                    break;
                }

                if (children[0] is Button) {
                    break;
                }
            }

            return usedCount;
        }

        public override string ToString() {
            if (Entires.Count == 0) {
                return "LargeAnimalBOD Empty";
            }

            return $"LargeAnimalBOD {Entires.Count} entries";
        }
    }

    internal class SmallAnimalBOD : BaseAnimalBOD {
        internal int Parse(List<Control> children) {
            if (Entires.Count > 0) {
                return 0;
            }

            int usedCount = 0;
            children.RemoveAt(0);
            usedCount++;
            children.RemoveAt(0);
            usedCount++;

            var price = ReadLabelText(children[0]);
            children.RemoveAt(0);
            usedCount++;

            children.RemoveAt(0); //Large or Small label
            usedCount++;

            var animalName = ReadLabelText(children[0]);
            children.RemoveAt(0);
            usedCount++;

            children.RemoveAt(0); //Spacer Label
            usedCount++;

            var animalMinMax = ReadLabelText(children[0]);
            children.RemoveAt(0);
            usedCount++;

            var split = animalMinMax.Split(' ');

            if (int.TryParse(split[0], out var amount) && int.TryParse(split[2], out var maxAmount)) {
                var animalEntry = new AnimalBODEntry(animalName, amount, maxAmount);
                Entires.Add(animalEntry);
            }

            return usedCount;
        }

        public override string ToString() {
            if (Entires.Count == 0) {
                return "SmallAnimalBOD Empty";
            }

            return $"SmallAnimalBOD {Entires.Count} entries  {Entires.First().Name}";
        }
    }

    internal static class BODBookParser
    {
        internal static List<BaseAnimalBOD> ParseAnimalTamingBODBook(Gump gump) {
            if (gump == null || gump.Children.Count < 36) {
                return new List<BaseAnimalBOD>();
            }

            var list = new List<BaseAnimalBOD>();

            var children = gump.Children.ToList();

            var numToRemove = -1;

            if (gump.Children.Count == 76) {
                numToRemove = 32;
            }

            if (gump.Children.Count == 84) {
                numToRemove = 34;
            }

            for (int i = 0; i < gump.Children.Count; i++) {
                if (gump.Children[i] is HtmlControl htmlControl && htmlControl.Text.Equals("Next Page", StringComparison.InvariantCultureIgnoreCase)) {
                    numToRemove = i+1;
                }
            }

            if (numToRemove == -1) {
                for (int i = 0; i < gump.Children.Count; i++) {
                    if (gump.Children[i] is HtmlControl htmlControl && htmlControl.Text.Equals("Previous Page", StringComparison.InvariantCultureIgnoreCase)) {
                        numToRemove = i+1;
                    }
                }
            }

            if (numToRemove == -1) {
                return list;
            }

            for (int i = 0; i < numToRemove; i++) {
                children.RemoveAt(0);
            }

            while (children.Count > 0) {
                var nextSize = GetNextAnimalTamingBODSize(children);

                if (nextSize == -1) {
                    return list;
                }

                if (nextSize == 0) {
                    // small
                    var smallBod = new SmallAnimalBOD();
                    smallBod.Parse(children);
                    list.Add(smallBod);
                }
                else {
                    // large
                    var largeBod = new LargeAnimalBOD();
                    largeBod.Parse(children);
                    list.Add(largeBod);
                }
            }

            return list;
        }

        internal static int GetNextAnimalTamingBODSize(List<Control> children) {
            if (children.Count < 4) {
                return -1;
            }

            var control = children[3];

            if (control is Label label) {
                if (label.Text != null && label.Text.ToLower().Contains("large")) {
                    return 1;
                }
                if (label.Text != null && label.Text.ToLower().Contains("small")) {
                    return 0;
                }
            }

            if (control is HtmlControl htmlControl) {
                if (htmlControl.Text != null && htmlControl.Text.ToLower().Contains("large")) {
                    return 1;
                }
                if (htmlControl.Text != null && htmlControl.Text.ToLower().Contains("small")) {
                    return 0;
                }
            }
            return -1;
        }



    }
}
