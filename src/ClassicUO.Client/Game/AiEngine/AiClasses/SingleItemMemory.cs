using ClassicUO.Game.AiEngine.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using System.Drawing;

namespace ClassicUO.Game.AiEngine.AiClasses
{
    internal class SingleItemMemory {
        public uint Serial;
        public uint ContainerSerial;
        public ItemLocationEnum ItemLocation = ItemLocationEnum.Ground;
        public ushort GraphicId;
        public ushort Hue;

        public SingleItemMemory() {

        }

        public SingleItemMemory(uint serial, uint containerSerial, ItemLocationEnum itemLocation, ushort graphicId = 0, ushort hue = 0)
        {
            Serial = serial;
            ContainerSerial = containerSerial;
            ItemLocation = itemLocation;
            GraphicId = graphicId;
            Hue = hue;
        }

        public SingleItemMemory(Item item, uint containerSerial, ItemLocationEnum itemLocation) {
            Serial = item.Serial;
            ContainerSerial = containerSerial;
            ItemLocation = itemLocation;
            GraphicId = item.Graphic;
            Hue = item.Hue;
        }

        public void MovedContainer(uint newContainerSerial) {
            ContainerSerial = newContainerSerial;
        }
    }
}
