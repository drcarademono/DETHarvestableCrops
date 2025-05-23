// Project:         Harvestable Crops for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Developer:       TheLacus

using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;

namespace DETHarvestableCrops
{
    /// <summary>
    /// An harvested crop defined by its position on terrain.
    /// </summary>
    public struct DETHarvestedCrop
    {
        public int MapPixelX;
        public int MapPixelY;
        public int LocalX;
        public int LocalZ;

        /// <summary>
        /// Make an harvested crop reference at the given localposition and current location.
        /// </summary>
        public DETHarvestedCrop(Vector3 localPosition)
        {
            StreamingWorld streamingWorld = GameManager.Instance.StreamingWorld;

            this.MapPixelX = streamingWorld.MapPixelX;
            this.MapPixelY = streamingWorld.MapPixelY;
            this.LocalX = (int)localPosition.x;
            this.LocalZ = (int)localPosition.z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DETHarvestedCrop))
                return false;

            var crop = (DETHarvestedCrop)obj;
            return MapPixelX == crop.MapPixelX &&
                   MapPixelY == crop.MapPixelY &&
                   LocalX == crop.LocalX &&
                   LocalZ == crop.LocalZ;
        }

        public override int GetHashCode()
        {
            var hashCode = -597405155;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + MapPixelX.GetHashCode();
            hashCode = hashCode * -1521134295 + MapPixelY.GetHashCode();
            hashCode = hashCode * -1521134295 + LocalX.GetHashCode();
            hashCode = hashCode * -1521134295 + LocalZ.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(DETHarvestedCrop crop1, DETHarvestedCrop crop2)
        {
            return crop1.Equals(crop2);
        }

        public static bool operator !=(DETHarvestedCrop crop1, DETHarvestedCrop crop2)
        {
            return !(crop1 == crop2);
        }
    }
}
