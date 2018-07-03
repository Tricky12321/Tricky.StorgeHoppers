using UnityEngine;

namespace Tricky.ExtraStorageHoppers
{
    /// <summary>
    /// Container for data on different hopper types.
    /// </summary>
    public class HopperTypeData
    {
        /// <summary>
        /// Gets the hopper name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the hopper capacity.
        /// </summary>
        public ushort Capacity { get;  }

        /// <summary>
        /// Gets the hopper color.
        /// </summary>
        public Color Color { get;  }

        /// <summary>
        /// Gets a value indicating if the hopper is One-Type.
        /// </summary>
        public bool IsOneType { get; }



        /// <summary>
        /// Constructor for HopperTypeData.
        /// </summary>
        /// <param name="name">Hopper name.</param>
        /// <param name="capacity">Hopper capacity.</param>
        /// <param name="color">Hopper color.</param>
        /// <param name="isOneType"></param>
        public HopperTypeData(string name, ushort capacity, Color color, bool isOneType)
        {
            Name = name;
            Capacity = capacity;
            Color = color;
            IsOneType = isOneType;
        }
    }
}
