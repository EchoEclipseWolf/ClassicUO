using ClassicUO.Game.AiEngine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using System.Drawing;
using AkatoshQuester.Helpers.LightGeometry;

namespace ClassicUO.Game.AiEngine.AiClasses
{
    public class SingleRuneMemory
    {
        public uint RunebookSerial;
        public uint ContainerSerial;
        public byte Index;
        public ItemLocationEnum ItemLocation = ItemLocationEnum.Ground;
        public Point3D Location;
        public int MapIndex;
        public string RuneName;

        public SingleRuneMemory()
        {

        }

        public SingleRuneMemory(uint runebookSerial, uint containerSerial, byte index, ItemLocationEnum itemLocation, Point3D endLocation, int endMapIndex, string runeName)
        {
            RunebookSerial = runebookSerial;
            ContainerSerial = containerSerial;
            ItemLocation = itemLocation;
            Index = index;
            Location = endLocation;
            MapIndex = endMapIndex;
            RuneName = runeName;
        }
    }
}