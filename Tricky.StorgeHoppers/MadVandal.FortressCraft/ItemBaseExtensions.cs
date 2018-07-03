namespace MadVandal.FortressCraft
{
    /// <summary>
    /// ItemBase extensions.
    /// </summary>
    public static class ItemBaseExtensions
    {
        /// <summary>
        /// Returns a unique storage Id value for an item.
        /// 
        /// StorageId is a unique identifier for cube type/value pair or item id as a single value. For terrain cubes the cube type is
        /// high 16 bits, and cube value lower 16 bits. For item ids the value is taken as-is. This works due to the following safe
        /// assumptions:
        /// 1) Cube type 0 (air) is reserved to represent "nothing" and cannot be used or stored by player in any way.
        /// 2) No player will ever have 55656 or more mod items defined (mod item ids start at 10000).
        /// </summary>
        /// <returns>Unique storage Id value.</returns>
        public static uint ToStorageId(this ItemBase value)
        {
            switch (value.mType)
            {
                case ItemType.ItemCubeStack:
                    ItemCubeStack itemCubeStack = (ItemCubeStack) value;
                    return ((uint)itemCubeStack.mCubeType << 16) + itemCubeStack.mCubeValue;
                default:
                    return (uint)value.mnItemID;
            }
        }


        /// <summary> 
        /// Gets the name of the item or cube value from a stored id.
        /// </summary>
        /// <param name="storageId"></param>
        /// <returns>Name of the item or cube value.</returns>
        public static string GetStorageIdName(uint storageId)
        {
            // Anything less then unsigned short maximum must be an item.
            if (storageId < ushort.MaxValue)
                return ItemManager.GetItemName((int) storageId);

            ushort cubeType = (ushort) (storageId >> 16);
            ushort cubeValue = (ushort) (storageId & 0xFF);
            return TerrainData.GetNameForValue(cubeType, cubeValue);
        }
    }
}
