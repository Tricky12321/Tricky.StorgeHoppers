using System;
using System.Collections.Generic;
using System.IO;
using MadVandal.FortressCraft;
using Steamworks;

namespace Tricky.ExtraStorageHoppers
{
    /// <summary>
    /// Represents inventory stacking for unique cube type/value or item id. For special item types of durability and charge this class
    /// "stacks" them by only storing a count of items with the same durability/change value. Thus a hopper with 100 new cutter heads will
    /// only occupy the amount of memory as if only one cutter head was stored, same with uncharged bombs. Weaknesses with this approach is
    /// location markers which have to be stored individually, but fortunately no would (or should) ever store these.
    ///
    /// StorageId is a unique identifier for cube type/value pair or item id as a single value. For terrain cubes the cube type is high 16
    /// bits, and cube value lower 16 bits. For item ids the value is taken as-is. This works due to the following safe assumptions:
    /// 1) Cube type 0 (air) is reserved to represent "nothing" and cannot be used or stored by player in any way.
    /// 2) No player will ever have 55656 or more mod items defined (mod item ids start at 10000).
    ///
    /// </summary>
    public class InventoryStack
    {
        /// <summary>
        /// Gets the item type.
        /// </summary>
        public ItemType ItemType { get; }

        /// <summary>
        /// Gets the unique storage Id for this inventory stack.
        /// </summary>
        public uint StorageId { get; }

        /// <summary>
        /// ItemBase object used when ItemType is ItemCubeStack or ItemStack.
        /// </summary>
        public ItemBase Item { get; set; }

        /// <summary>
        /// Gets the cube type. Only valid if ItemType property is ItemCubeStack.
        /// </summary>
        public ushort CubeType => (ushort) (ItemType == ItemType.ItemCubeStack ? (StorageId >> 16) : 0);

        /// <summary>
        /// Gets the cube value. Only valid if ItemType property is ItemCubeStack.
        /// </summary>
        public ushort CubeValue => (ushort) (ItemType == ItemType.ItemCubeStack ? (StorageId & 0xFFFF) : 0);

        /// <summary>
        /// Gets a value indicating if this is a stacking item type (ItemType is ItemCubeType or ItemStack).
        /// NOTE: ItemSingle items will be stored as ItemStack.
        /// </summary>
        public bool IsStackItem => ItemType == ItemType.ItemCubeStack || ItemType == ItemType.ItemStack;

        /// <summary>
        /// Gets the count of cubes or items in this stack.
        /// </summary>
        public int Count
        {
            get
            {
                try
                {
                    int amount;
                    switch (ItemType)
                    {
                        case ItemType.ItemCubeStack:
                        {
                            if (Item == null)
                            {
                                Logging.LogError("NULL item on inventory CubeStack - Correcting");
                                Item = new ItemCubeStack(ItemBaseExtensions.GetCubeType(StorageId), ItemBaseExtensions.GetCubeValue(StorageId), 0);
                            }

                            amount = ((ItemCubeStack) Item).mnAmount;
                            Logging.LogMessage("Count on " + StorageId + " = " + amount, 2);
                            return amount;
                        }
                        case ItemType.ItemStack:
                        case ItemType.ItemSingle:
                            if (Item == null)
                            {
                                Logging.LogError("NULL item on inventory ItemStack - Correcting");
                                Item = new ItemStack((int) StorageId, 0);
                            }

                            amount = ((ItemStack) Item).mnAmount;
                            Logging.LogMessage("Count on " + StorageId + " = " + amount, 2);
                            return amount;
                        default:
                            Logging.LogMessage("Count on " + StorageId + " = " + mSubStackCount, 2);
                            return mSubStackCount;
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                    return 0;
                }
            }
        }



        /// <summary>
        /// Dictionary of charged item inventory counts by charge amount. Only used when ItemType is ItemCharge.
        /// </summary>
        private readonly Dictionary<int, ushort> mChargeSubStack = new Dictionary<int, ushort>();


        /// <summary>
        /// Dictionary of ItemDurability inventory counts by charge amount. Only used when ItemType is ItemDurability.
        /// </summary>
        private readonly Dictionary<ushort, ushort> mDurabilitySubStack = new Dictionary<ushort, ushort>();


        /// <summary>
        /// List of location marker item location objects. Only used when ItemType is ItemLocation.
        /// </summary>
        private readonly List<ItemLocation> mLocationSubStack = new List<ItemLocation>();

        /// <summary>
        /// Current count of charge, durability, or location items in the collections.
        /// </summary>
        private int mSubStackCount;

        /// <summary>
        /// Maximum durability of the ItemDurability type stored. Used to re-spawn ItemCharge objects.
        /// </summary>
        private readonly int mMaximumDurability;


        /// <summary>
        /// Constructor InventoryStack.
        /// </summary>
        /// <param name="itemType">Item type.</param>
        /// <param name="storageId">Storage Id.</param>
        public InventoryStack(ItemType itemType, uint storageId)
        {
            ItemType = itemType;
            StorageId = storageId;

            if (itemType == ItemType.ItemCubeStack)
                Item = new ItemCubeStack(CubeType, CubeValue, 0);
            else if (itemType == ItemType.ItemStack || itemType == ItemType.ItemSingle)
                Item = new ItemStack((int) storageId, 0);
            else if (itemType == ItemType.ItemDurability)
            {
                ItemEntry entry = ItemEntry.mEntries[storageId];
                mMaximumDurability = entry?.MaxDurability ?? 0;
            }
        }


        /// <summary>
        /// Adds a sub-stacked ItemCharge, ItemDurability, or ItemLocation item to the stack. All other item types should use the Item property and
        /// calling this method will throw an exception.
        /// </summary>
        /// <param name="itemBase">Item to add.</param>
        public void AddSubStackItem(ItemBase itemBase)
        {
            if (itemBase.mType != ItemType)
                throw new InvalidOperationException("Attempt to add sub stack item type of " + itemBase.mType + " to an inventory stack of type " + ItemType);

            switch (itemBase.mType)
            {
                case ItemType.ItemCharge:
                    ItemCharge itemCharge = (ItemCharge) itemBase;
                    int chargeLevel = (int) itemCharge.mChargeLevel;
                    if (!mChargeSubStack.ContainsKey(chargeLevel))
                        mChargeSubStack[chargeLevel] = 1;
                    else
                        mChargeSubStack[chargeLevel]++;
                    mSubStackCount++;
                    break;

                case ItemType.ItemDurability:
                    ItemDurability itemDurability = (ItemDurability) itemBase;
                    ushort durabilityValue = (ushort) itemDurability.mnCurrentDurability;

                    if (!mDurabilitySubStack.ContainsKey(durabilityValue))
                        mDurabilitySubStack[durabilityValue] = 1;
                    else
                        mDurabilitySubStack[durabilityValue]++;
                    mSubStackCount++;
                    Logging.LogMessage("Add SubStack Durability Item - Value:" + durabilityValue + " Count:" + mSubStackCount, 1);
                    break;

                case ItemType.ItemLocation:
                    ItemLocation itemLocation = (ItemLocation) itemBase;
                    mLocationSubStack.Add(itemLocation);
                    mSubStackCount++;
                    break;

                default:
                    throw new InvalidOperationException("Attempt to add unrecognized sub stack item type of " + itemBase.mType + " to an inventory stack");

            }
        }


        /// <summary>
        /// Remove a single sub-stacked ItemCharge, ItemDurability, or ItemLocation item from the stack. All other item types should use the Item
        /// property and calling this method will throw an exception. Returns null if there is no inventory to remove.
        /// </summary>
        /// <returns>Removed item.</returns>
        public ItemBase RemoveSubStackItem()
        {
            switch (ItemType)
            {
                case ItemType.ItemCharge:
                    if (mSubStackCount == 0)
                        return null;
                    foreach (int itemChargeValue in mChargeSubStack.Keys)
                    {
                        ItemCharge itemCharge = new ItemCharge((int) StorageId, itemChargeValue);
                        mChargeSubStack[itemChargeValue]--;
                        mSubStackCount--;
                        if (mChargeSubStack[itemChargeValue] <= 0)
                            mChargeSubStack.Remove(itemChargeValue);
                        return itemCharge;
                    }

                    return null;

                case ItemType.ItemDurability:
                    if (mSubStackCount == 0)
                        return null;
                    foreach (ushort itemDurabilityValue in mDurabilitySubStack.Keys)
                    {
                        ItemDurability itemDurability = new ItemDurability((int) StorageId, itemDurabilityValue, mMaximumDurability);
                        mDurabilitySubStack[itemDurabilityValue]--;
                        mSubStackCount--;
                        if (mDurabilitySubStack[itemDurabilityValue] <= 0)
                            mDurabilitySubStack.Remove(itemDurabilityValue);
                        return itemDurability;
                    }

                    return null;

                case ItemType.ItemLocation:
                    if (mSubStackCount == 0)
                        return null;

                    ItemLocation itemLocation = mLocationSubStack[0];
                    mLocationSubStack.RemoveAt(0);
                    return itemLocation;

                default:
                    return null;
            }
        }


        /// <summary>
        /// Subtracts the specified amount of sub-stacked ItemCharge, ItemDurability, or ItemLocation items from the stack and returns the
        /// amount actually removed based on the inventory count. All other item types should use the Item property and calling this method
        /// will throw an exception. 
        /// </summary>
        /// <param name="amount">Amount to remove.</param>
        /// <returns>Actual actually removed.</returns>
        public int RemoveSubStackAmount(int amount)
        {
            Logging.LogMessage("Removing SubStack Amount on ItemType: " + ItemType + " StorageId: " + StorageId + " Amount: " + amount, 2);
            int remainderToRemove = amount;
            switch (ItemType)
            {
                case ItemType.ItemCharge:
                    foreach (int itemChargeValue in mChargeSubStack.Keys)
                    {
                        ushort stackAmount = mChargeSubStack[itemChargeValue];
                        if (remainderToRemove > stackAmount)
                        {
                            remainderToRemove -= stackAmount;
                            mSubStackCount -= stackAmount;
                            mChargeSubStack[itemChargeValue] = 0;
                        }
                        else
                        {
                            mChargeSubStack[itemChargeValue] -= (ushort) remainderToRemove;
                            return amount;
                        }

                    }

                    return amount - remainderToRemove;

                case ItemType.ItemDurability:
                    foreach (ushort itemDurabilityValue in mDurabilitySubStack.Keys)
                    {
                        ushort stackAmount = mDurabilitySubStack[itemDurabilityValue];
                        if (remainderToRemove > stackAmount)
                        {
                            remainderToRemove -= stackAmount;
                            mSubStackCount -= stackAmount;
                            mDurabilitySubStack[itemDurabilityValue] = 0;
                        }
                        else
                        {
                            mDurabilitySubStack[itemDurabilityValue] -= (ushort) remainderToRemove;
                            return amount;
                        }
                    }

                    return amount - remainderToRemove;

                case ItemType.ItemLocation:
                    if (remainderToRemove > mLocationSubStack.Count)
                    {
                        remainderToRemove -= mLocationSubStack.Count;
                        mSubStackCount = 0;
                        mLocationSubStack.Clear();
                    }
                    else
                    {
                        mLocationSubStack.RemoveRange(0, remainderToRemove);
                        mSubStackCount -= remainderToRemove;
                    }

                    return amount - remainderToRemove;

                default:
                    return 0;
            }
        }


        /// <summary>
        /// Iterates the inventory stack.
        /// </summary>
        /// <param name="itemFunc">Function to invoke for each item.</param>
        /// <param name="state">State object passed to the function.</param>
        /// <returns>True if iteration should continue, otherwise false.</returns>
        public bool IterateContents(IterateItem itemFunc, object state)
        {
            switch (ItemType)
            {
                case ItemType.ItemCharge:
                    foreach (int itemChargeValue in mChargeSubStack.Keys)
                        for (int index = 0; index < mChargeSubStack[itemChargeValue]; index++)
                        {
                            ItemCharge itemCharge = new ItemCharge((int) StorageId, itemChargeValue);
                            if (!itemFunc(itemCharge, state))
                                return false;
                        }

                    return true;

                case ItemType.ItemDurability:
                    foreach (ushort itemDurabilityValue in mDurabilitySubStack.Keys)
                    {
                        for (int index = 0; index < mDurabilitySubStack[itemDurabilityValue]; index++)
                        {
                            ItemDurability itemDurability = new ItemDurability((int) StorageId, itemDurabilityValue, mMaximumDurability);
                            if (!itemFunc(itemDurability, state))
                                return false;
                        }
                    }

                    return true;

                case ItemType.ItemLocation:
                    foreach (ItemLocation itemLocation in mLocationSubStack)
                        if (!itemFunc(itemLocation, state))
                            return false;
                    return true;

                case ItemType.ItemSingle:
                    ItemStack itemStack = (ItemStack) Item;
                    for (int count = 0; count < itemStack.mnAmount; count++)
                        if (!itemFunc(new ItemSingle((int) StorageId), state))
                            return false;
                    return true;

                default:
                    return itemFunc(Item, state);
            }
        }


        /// <summary>
        /// Clears the inventory stack sub-stack contents.
        /// </summary>
        public void Clear()
        {
            mChargeSubStack.Clear();
            mDurabilitySubStack.Clear();
            mLocationSubStack.Clear();
            mSubStackCount = 0;

            switch (ItemType)
            {
                case ItemType.ItemSingle:
                case ItemType.ItemStack:
                    ((ItemStack) Item).mnAmount = 0;
                    break;
                case ItemType.ItemCubeStack:
                    ((ItemCubeStack) Item).mnAmount = 0;
                    break;
            }
        }


        /// <summary>
        /// Reads an InventoryStack from binary data and returns an InventoryStack object from it.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <returns>New InventoryStack object.</returns>
        public static InventoryStack Read(BinaryReader reader)
        {
            ItemType itemType = (ItemType) reader.ReadByte();
            uint storageId = reader.ReadUInt32();
            InventoryStack inventoryStack = new InventoryStack(itemType, storageId);

            Logging.LogMessage("Read - Storage Id:" + storageId + " Item Type:" + itemType + " IsStackItem:" + inventoryStack.IsStackItem + " Cube:" + inventoryStack.CubeType +
                               " Value:" + inventoryStack.CubeValue, 1);

            switch (itemType)
            {
                case ItemType.ItemCubeStack:
                case ItemType.ItemStack:
                    inventoryStack.Item = ItemFile.DeserialiseItem(reader);
                    if (Logging.LoggingLevel >= 2)
                        Logging.LogMessage("Deserialize item " + inventoryStack.Item.GetDisplayString(), 2);
                    break;

                case ItemType.ItemSingle:
                    ushort count = reader.ReadUInt16();
                    Logging.LogMessage("Read ItemSingle count: " + count, 2);
                    inventoryStack.Item = new ItemStack((int) storageId, count);
                    break;

                case ItemType.ItemCharge:
                {
                    int itemCount = reader.ReadUInt16();
                    Logging.LogMessage("Sub-stack count: " + itemCount, 1);
                    for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                    {
                        int chargeValue = reader.ReadInt32();
                        ushort amount = reader.ReadUInt16();
                        inventoryStack.mChargeSubStack[chargeValue] = amount;
                        inventoryStack.mSubStackCount += amount;
                    }

                    break;
                }

                case ItemType.ItemDurability:
                {
                    int itemCount = reader.ReadUInt16();
                    Logging.LogMessage("Sub-stack count: " + itemCount, 1);
                    for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                    {
                        ushort durabilityValue = reader.ReadUInt16();
                        ushort amount = reader.ReadUInt16();
                        Logging.LogMessage("Durability Value: " + durabilityValue + " Amount: " + amount, 1);
                        inventoryStack.mDurabilitySubStack[durabilityValue] = amount;
                        inventoryStack.mSubStackCount += amount;
                    }

                    break;
                }

                case ItemType.ItemLocation:
                {
                    int itemCount = reader.ReadUInt16();
                    Logging.LogMessage("Sub-stack count: " + itemCount, 1);
                    for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                    {
                        inventoryStack.mLocationSubStack.Add((ItemLocation) ItemFile.DeserialiseItem(reader));
                        inventoryStack.mSubStackCount++;
                    }

                    break;
                }
            }

            return inventoryStack;
        }


        /// <summary>
        /// Writes this inventory stack instance to binary data.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        public void Write(BinaryWriter writer)
        {
            writer.Write((byte) ItemType);
            writer.Write(StorageId);

            Logging.LogMessage("Write - Storage Id:" + StorageId + " Item Type:" + ItemType + " IsStackItem:" + IsStackItem + " Cube:" + CubeType +
                               " Value:" + CubeValue, 1);
            switch (ItemType)
            {
                case ItemType.ItemCubeStack:
                case ItemType.ItemStack:
                    if (Logging.LoggingLevel >= 2)
                        Logging.LogMessage("Write Serialized Item: " + Item.GetDisplayString(), 2);
                    ItemFile.SerialiseItem(Item, writer);
                    break;

                case ItemType.ItemSingle:
                    ushort count = (ushort) ((ItemStack) Item).mnAmount;
                    if (Logging.LoggingLevel >= 2)
                        Logging.LogMessage("Write ItemSingle count: " + count, 2);
                    writer.Write(count);
                    break;

                case ItemType.ItemCharge:
                    Logging.LogMessage("Write Item Charge - Count: " + mChargeSubStack.Count, 2);
                    writer.Write((ushort) mChargeSubStack.Count);
                    foreach (int chargeValue in mChargeSubStack.Keys)
                    {
                        writer.Write(chargeValue);
                        writer.Write(mChargeSubStack[chargeValue]);
                    }

                    break;

                case ItemType.ItemDurability:
                    Logging.LogMessage("Write Item Durability - Count: " + mDurabilitySubStack.Count, 2);
                    writer.Write((ushort) mDurabilitySubStack.Count);
                    foreach (ushort durabilityValue in mDurabilitySubStack.Keys)
                    {
                        writer.Write(durabilityValue);
                        writer.Write(mDurabilitySubStack[durabilityValue]);
                    }

                    break;

                case ItemType.ItemLocation:
                    writer.Write((ushort) mLocationSubStack.Count);
                    Logging.LogMessage("Write Item Location - Count: " + mLocationSubStack.Count, 2);
                    foreach (ItemLocation itemLocation in mLocationSubStack)
                        ItemFile.SerialiseItem(itemLocation, writer);

                    break;

            }
        }
    }

}