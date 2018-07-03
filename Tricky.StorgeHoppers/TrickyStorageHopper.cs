using MadVandal.FortressCraft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Tricky.ExtraStorageHoppers
{
    /// <summary>
    /// Tricky Storage Hopper machine.
    /// </summary>
    public class TrickyStorageHopper : MachineEntity, ItemConsumerInterface, StorageMachineInterface
    {
        /// <summary>
        /// Vacuum range constant.
        /// </summary>
        public const int VACUUM_RANGE = 16;

        /// <summary>
        /// Number of hopper cube re-balancing loops performed with each low frequency thread pass.
        /// </summary>
        public const int HOPPER_CUBE_RE_BALANCE_LOOPS = 10;

        /// <summary>
        /// Number of segment cube checks for inputs and outputs.
        /// </summary>
        public const int SEGMENT_CUBE_CHECKS = 6;

        /// <summary>
        /// Number of cycles per output on any side.
        /// </summary>
        public const int OUTPUT_CYCLE_DELAY = 5;

        /// <summary>
        /// Freezon item ID constant.
        /// </summary>
        private const int FREEZON_ITEM_ID = 3751;

        /// <summary>
        /// Spoiled organics item Id constant.
        /// </summary>
        public const ushort SPOILED_ORGANICS_ITEM_ID = 4100;


        /// <summary>
        /// Previous hopper permissions.
        /// </summary>
        private eHopperPermissions mPreviousPermissions = eHopperPermissions.eNumPermissions;

        /// <summary>
        /// Maximum capacity.
        /// </summary>
        private ushort mMaximumCapacity;

        /// <summary>
        /// Dictionary of inventory stacks by storage id.
        /// </summary>
        private readonly Dictionary<uint, InventoryStack> mInventory = new Dictionary<uint, InventoryStack>();

        /// <summary>
        /// Round robin offset for item removal.
        /// </summary>
        private int mRoundRobinOffset;

        /// <summary>
        /// Neighboring segments to be checked for interaction.
        /// </summary>
        private readonly Segment[] mCheckSegments;

        /// <summary>
        /// Neighboring entity pass count for non-powered conveyor transmission.
        /// </summary>
        private readonly int[] mPassCount;

        /// <summary>
        /// Indicates if the the segment index check is flipped.
        /// </summary>
        private bool mFlipSegmentCheckIndex;

        /// <summary>
        /// Current check segment index.
        /// </summary>
        private int mCheckSegmentIndex;

        /// <summary>
        /// Vacuum segments.
        /// </summary>
        private Segment[,,] mVacuumSegment;

        /// <summary>
        /// Hopper re-balance cycle count.
        /// </summary>
        private int mHopperReBalanceCycle;

        /// <summary>
        /// Last item added.
        /// </summary>
        private ItemBase mLastItemAdded;

        /// <summary>
        /// Last item added text.
        /// </summary>
        private string mLastItemAddedText;

        /// <summary>
        /// Organic spoilage timer.
        /// </summary>
        private float mSpoilTimer = 30f;

        /// <summary>
        /// Cached hopper options.
        /// </summary>
        private readonly InventoryExtractionOptions mCachedHopperOptions = new InventoryExtractionOptions();

        /// <summary>
        /// Cached hopper results.
        /// </summary>
        private InventoryExtractionResults mCachedHopperResults = new InventoryExtractionResults();

        /// <summary>
        /// Manual item re-take debounce time.
        /// </summary>
        private float mRetakeDebounce;

        /// <summary>
        /// Last popup text.
        /// </summary>
        private string mPopupText = string.Empty;

        /// <summary>
        /// Last player distance update time.
        /// </summary>
        private float mTimeUntilPlayerDistanceUpdate;

        /// <summary>
        /// Previous distance to the player.
        /// </summary>
        private float mPreviousDistanceToPlayer;

        /// <summary>
        /// Indicates if the machine is linked to a Unity GameObject.
        /// </summary>
        private bool mLinkedToGameObject;

        /// <summary>
        /// Indicates if Unity initialization failed.
        /// </summary>
        private bool mUnityInitializationError;

        /// <summary>
        /// Hopper work light object.
        /// </summary>
        private Light mWorkLight;

        /// <summary>
        /// Maximum light distance.
        /// </summary>
        private float mMaxLightDistance = 32f;

        /// <summary>
        /// Hopper game object.
        /// </summary>
        private GameObject mHopperPart;

        /// <summary>
        /// Holo status game object.
        /// </summary>
        private GameObject mHoloStatus;

        /// <summary>
        /// Vacuum particle system component.
        /// </summary>
        private ParticleSystem mVacuumParticleSystem;

        /// <summary>
        /// Vacuum particle emission rate.
        /// </summary>
        private int mVacuumEmissionRate;

        /// <summary>
        /// Hopper text mesh object.
        /// </summary>
        private TextMesh mTextMesh;

        /// <summary>
        /// Indicates if the hopper is visible based on the player distance.
        /// </summary>
        private bool mShowHopper;

        /// <summary>
        /// Force text mesh update.
        /// </summary>
        private bool mForceTextUpdate;

        /// <summary>
        /// Force holobase update.
        /// </summary>
        private bool mForceHoloUpdate;


        /// <summary>
        /// Gets the current permissions.
        /// </summary>
        public eHopperPermissions Permissions { get; private set; }

        /// <summary>
        /// Gets a value indicating if inventory extraction is allowed by neighboring conveyor entities. Always returns false as this is 
        /// managed by this hopper instance based on the locked state of the side.
        /// </summary>
        public bool InventoryExtractionPermitted => false;

        /// <summary>
        /// Get the current total capacity.
        /// </summary>
        public int TotalCapacity => mMaximumCapacity;

        /// <summary>
        /// Gets the used capacity.
        /// </summary>
        public int UsedCapacity { get; private set; }


        /// <summary>
        /// Get the remaining capacity (less any reserved space).
        /// </summary>
        public int RemainingCapacity => Math.Max(0, mMaximumCapacity - UsedCapacity);


        /// <summary>
        /// Gets or sets a value indicating if the vacuum is on.
        /// </summary>
        public bool VacuumOn { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating if content sharing is active.
        /// </summary>
        public bool ContentSharingOn { get; private set; } = true;

        /// <summary>
        /// Gets or sets a value indicating if hivemind feeding is active.
        /// </summary>
        public bool HivemindFeedingOn { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating if this is a one-type hopper.
        /// </summary>
        public bool OneTypeHopper { get; private set; }

        /// <summary>
        /// Gets of sets the one-type hopper item name. Null or empty is not set or not a one-type hopper.
        /// </summary>
        public string OneTypeItemName { get; private set; }

        /// <summary>
        /// Gets the hopper name.
        /// </summary>
        public string HopperName { get; }

        /// <summary>
        /// Void hopper delete count.
        /// </summary>
        public int VoidHopperDeleteCount { get; private set; }
         

        /// <summary>
        /// One-type hopper storage Id (0 if not assigned).
        /// </summary>
        private uint mOneTypeHopperStorageId;


        /// <summary>
        /// Hopper cube color.
        /// </summary>
        private Color mCubeColor;



        /// <summary>
        /// Constructor for TrickStorageHopper.
        /// </summary>
        public TrickyStorageHopper(Segment segment, long x, long y, long z, ushort cube, byte flags, ushort lValue,
            string hopperName, ushort hopperMaxStorage, Color hopperColor, bool oneTypeHopper)
            : base(eSegmentEntity.Mod, SpawnableObjectEnum.LogisticsHopper, x, y, z, cube, flags, lValue, Vector3.zero, segment)
        {
            // Initialize.
            mbNeedsLowFrequencyUpdate = true;
            mbNeedsUnityUpdate = true;
            HopperName = hopperName;

            // Initialize check segments and pass count array.
            mCheckSegments = new Segment[SEGMENT_CUBE_CHECKS];
            mPassCount = new int[SEGMENT_CUBE_CHECKS];
            for (int index = 0; index < SEGMENT_CUBE_CHECKS; index++)
                mPassCount[index] = OUTPUT_CYCLE_DELAY;

            if (mValue == 0)
                Permissions = eHopperPermissions.AddOnly;

            hopperMaxStorage = hopperMaxStorage == 0 ? (ushort) 1 : Math.Min(hopperMaxStorage, (ushort) (oneTypeHopper ? 30000 : 1000));

            mMaximumCapacity = hopperMaxStorage;
            mCubeColor = hopperColor;
            OneTypeHopper = oneTypeHopper;

            CountFreeSlots();
        }


        /// <summary>
        /// Handle OnDelete.
        /// </summary>
        public override void OnDelete()
        {
            if (!WorldScript.mbIsServer)
                return;

            lock (mInventory)
            {
                mInventory.Clear();
            }

            base.OnDelete();
        }


        /// <summary>
        /// Handle low frequency update.
        /// </summary>
        public override void LowFrequencyUpdate()
        {
            try
            {
                if (mSegment != null && !mSegment.mbNeedsUnityUpdate)
                    mSegment.mbNeedsUnityUpdate = true;

                mTimeUntilPlayerDistanceUpdate -= LowFrequencyThread.mrPreviousUpdateTimeStep;
                if (mTimeUntilPlayerDistanceUpdate < 0.0)
                {
                    mPreviousDistanceToPlayer = mDistanceToPlayer;
                    UpdatePlayerDistanceInfo();
                    mTimeUntilPlayerDistanceUpdate = mDistanceToPlayer / 30f;
                    if (mTimeUntilPlayerDistanceUpdate > 2.0)
                        mTimeUntilPlayerDistanceUpdate = 2f;
                }

                UpdateVacuum();
                if (WorldScript.mbIsServer)
                    UpdateSpoilage();

                // Increment re-balance cycle count.
                ++mHopperReBalanceCycle;

                for (int loopCount = 0; loopCount < SEGMENT_CUBE_CHECKS; loopCount++)
                    ProcessNextAttachedEntitySide();
            }
            catch (Exception e)
            {
                Logging.LogException(this, e);
            }
        }


        /// <summary>
        /// Processes the next side for an attached entity of the six possible attached entities.
        /// </summary>
        private void ProcessNextAttachedEntitySide()
        {
            // Increment to the next side coordinate.
            ++mCheckSegmentIndex;

            long x = mnX;
            long y = mnY;
            long z = mnZ;
            int index = mCheckSegmentIndex % SEGMENT_CUBE_CHECKS;
            if (index == 0)
                --x;
            if (index == 1)
                ++x;
            if (index == 2)
                --y;
            if (index == 3)
                ++y;
            if (index == 4)
                --z;
            if (index == 5)
                ++z;

            bool changeFlipState = index == SEGMENT_CUBE_CHECKS - 1;
            index = SEGMENT_CUBE_CHECKS - 1 - index;

            if (changeFlipState)
                mFlipSegmentCheckIndex = !mFlipSegmentCheckIndex;

            // Get the segment. If valid then check for an entity.
            if (mCheckSegments[index] == null)
                mCheckSegments[index] = AttemptGetSegment(x, y, z);
            else if (mCheckSegments[index].mbDestroyed || !mCheckSegments[index].mbInitialGenerationComplete)
                mCheckSegments[index] = null;
            else
            {
                // Get the cube and if no entity then leave.
                ushort cube = mCheckSegments[index].GetCube(x, y, z);
                if (!CubeHelper.HasEntity(cube))
                    return;

                // If we have capacity then check for a supplier.
                if (RemainingCapacity > 0)
                    CheckSupplier(mCheckSegments[index], x, y, z);

                // If there are stored items then check for a consumer.
                if (UsedCapacity > 0)
                    CheckConsumer(index, mCheckSegments[index], cube, x, y, z);

                // Check for neighboring hoppers.
                if (mHopperReBalanceCycle % 10 == 0)
                    CheckNeighborHopper(x, y, z, mCheckSegments[index], cube);
            }
        }


        /// <summary>
        /// Update the vacuum action.
        /// </summary>
        private void UpdateVacuum()
        {
            // If not server, vacuum is not on, or no remaining capacity then leave.
            if (!WorldScript.mbIsServer || !VacuumOn || RemainingCapacity == 0)
                return;

            // Allocate vacuum segments if array is null.
            if (mVacuumSegment == null)
                mVacuumSegment = new Segment[3, 3, 3];


            // Loop through each coordinate in the range of the vacuum.
            ++SegmentUpdater.mnNumHoovers;
            for (int xOffset = -1; xOffset <= 1; ++xOffset)
            for (int yOffset = -1; yOffset <= 1; ++yOffset)
            for (int zOffset = -1; zOffset <= 1; ++zOffset)
            {
                // Get the segment if null.
                if (mVacuumSegment[xOffset + 1, yOffset + 1, zOffset + 1] == null)
                {
                    Segment segment = AttemptGetSegment(mnX + xOffset * VACUUM_RANGE, mnY + yOffset * VACUUM_RANGE, mnZ + zOffset * VACUUM_RANGE);
                    if (segment == null || !segment.mbInitialGenerationComplete || segment.mbDestroyed)
                        return;
                    mVacuumSegment[xOffset + 1, yOffset + 1, zOffset + 1] = segment;
                    return;
                }

                // Look for dropped item data.
                DroppedItemData droppedItemData = ItemManager.instance.UpdateCollectionSpecificSegment(mnX, mnY + 1L, mnZ,
                    new Vector3(0.5f, 0.0f, 0.5f), 12f, 1f, 2f, mVacuumSegment[xOffset + 1, yOffset + 1, zOffset + 1], RemainingCapacity);

                // If dropped items then attempt to add them. If the add fails the put the dropped item back.F
                if (droppedItemData != null && !AddItem(droppedItemData.mItem))
                {
                    ItemManager.instance.DropItem(droppedItemData.mItem, mnX, mnY + 1L, mnZ, Vector3.up);
                    return;
                }
            }
        }


        /// <summary>
        /// Update spoilage of organic items.
        /// </summary>
        private void UpdateSpoilage()
        {
            mSpoilTimer -= LowFrequencyThread.mrPreviousUpdateTimeStep;
            if (mSpoilTimer > 0.0)
                return;

            lock (mInventory)
            {
                for (uint storageId = 4000; storageId <= 4010; storageId += 2)
                {
                    if (!mInventory.TryGetValue(storageId, out InventoryStack inventoryStack))
                        continue;
                    ItemStack itemStack = (ItemStack) inventoryStack.Item;
                    if (itemStack.mnAmount > 0)
                    {
                        itemStack.mnAmount--;
                        AddItem(ItemManager.SpawnItem(SPOILED_ORGANICS_ITEM_ID));
                        mSpoilTimer = 30f;
                        return;
                    }
                }
            }

            mSpoilTimer = 30f;
        }


        /// <summary>
        /// Checks the specified neighboring cube for a supplier interface and process if found.
        /// </summary>
        /// <param name="checkSegment">Segment of the position.</param>
        /// <param name="checkX">X position.</param>
        /// <param name="checkY">Y position.</param>
        /// <param name="checkZ">Z position.</param>
        private void CheckSupplier(Segment checkSegment, long checkX, long checkY, long checkZ)
        {
            // If the hopper permissions do not allow for supply or empty then leave.
            if (Permissions == eHopperPermissions.Locked || Permissions == eHopperPermissions.RemoveOnly || RemainingCapacity <= 0)
                return;

            // Look for supplier interface. If found call ProcessStorageSupplier.
            StorageSupplierInterface supplierInterface = checkSegment.SearchEntity(checkX, checkY, checkZ) as StorageSupplierInterface;
            supplierInterface?.ProcessStorageSupplier(this);
        }


        /// <summary>
        /// Checks the specified neighboring cube for a consumer interface and process if found.
        /// </summary>
        /// <param name="neighborIndex">Neighbor index.</param>
        /// <param name="checkSegment">Segment of the position.</param>
        /// <param name="lCube">Cube type.</param>
        /// <param name="checkX">X position.</param>
        /// <param name="checkY">Y position.</param>
        /// <param name="checkZ">Z position.</param>
        private void CheckConsumer(int neighborIndex, Segment checkSegment, ushort lCube, long checkX, long checkY, long checkZ)
        {
            // Get the segment entity.
            SegmentEntity segmentEntity = checkSegment.SearchEntity(checkX, checkY, checkZ);

            // If there is a consumer interface and hopper permissions allow removal then process it and leave.
            if (segmentEntity is StorageConsumerInterface consumerInterface &&
                (Permissions == eHopperPermissions.AddAndRemove || Permissions == eHopperPermissions.RemoveOnly))
                consumerInterface.ProcessStorageConsumer(this);

            // If this is a conveyor then process it.
            if (lCube == eCubeTypes.Conveyor)
            {
                // Get the conveyor entity. If facing the hopper (input) then leave.
                ConveyorEntity conveyorEntity = (ConveyorEntity) segmentEntity;
                if (this.IsConveyorFacingMe(conveyorEntity))
                    return;

                // If this is not a powered hopper and the pass countdown is above zero then subtract the count and leave.
                if (mValue == 0 && mPassCount[neighborIndex] > 0)
                {
                    mPassCount[neighborIndex]--;
                    return;
                }

                // Reset the pass count based on if this is a regular or powered hopper.
                mPassCount[neighborIndex] = conveyorEntity.mValue == 15 ? 1 : OUTPUT_CYCLE_DELAY;

                // If the conveyor is motorized, and the conveyor does not have sufficient power then leave.
                if (conveyorEntity.mValue == 15 && conveyorEntity.mrCurrentPower < ConveyorEntity.PowerPerItem) 
                    return;

                // If the conveyor is current carrying or not ready then leave.
                if (conveyorEntity.IsCarryingCargo() || !conveyorEntity.mbReadyToConvey)
                    return;

                // Attempt extraction based on the conveyor settings. If successful then load the conveyor.
                mCachedHopperOptions.SourceEntity = this;
                mCachedHopperOptions.RequestType = conveyorEntity.meRequestType;
                mCachedHopperOptions.ExemplarItemID = conveyorEntity.ExemplarItemID;
                mCachedHopperOptions.ExemplarBlockID = conveyorEntity.ExemplarBlockID;
                mCachedHopperOptions.ExemplarBlockValue = conveyorEntity.ExemplarBlockValue;
                mCachedHopperOptions.InvertExemplar = conveyorEntity.mbInvertExemplar;
                mCachedHopperOptions.MinimumAmount = 1;
                mCachedHopperOptions.MaximumAmount = 1;
                if (TryExtract(mCachedHopperOptions, ref mCachedHopperResults))
                {
                    // Add then item to the conveyor based on if the extraction is an item or cube.
                    if (mCachedHopperResults.Item != null)
                        conveyorEntity.AddItem(mCachedHopperResults.Item);
                    else
                        conveyorEntity.AddCube(mCachedHopperResults.Cube, mCachedHopperResults.Value, 1f);

                    // If the adding to the conveyor failed then put the item or cube back in the hopper otherwise check for power usage.
                    if (!conveyorEntity.IsCarryingCargo())
                    {
                        if (mCachedHopperResults.Item != null)
                            AddItem(mCachedHopperResults.Item);
                        else
                            AddCube(mCachedHopperResults.Cube, mCachedHopperResults.Value);
                    }
                    else
                    {
                        // If the conveyor is motorized the perform its power consumption as it cannot do this itself using this manner of 
                        // conveyor feeding.
                        if (conveyorEntity.mValue == 15)
                            conveyorEntity.mrCurrentPower -= ConveyorEntity.PowerPerItem;
                    }
                }

                return;
            }

            // If the cube is not a geothermal generator then leave.
            if (lCube != eCubeTypes.GeothermalGenerator)
                return;

            // Get the geothermal generator. If this fails then leave.
            if (!(checkSegment.FetchEntity(eSegmentEntity.GeothermalGenerator, checkX, checkY, checkZ) is GeothermalGenerator geothermalGenerator))
                return;

            // Get the linked center.
            if (geothermalGenerator.mLinkedCenter != null)
                geothermalGenerator = geothermalGenerator.mLinkedCenter;

            // Get the boost time field. If valid then check for freezon transfer.
            FieldInfo fieldInfo = geothermalGenerator.GetType().GetField("mrBoostTime", BindingFlags.GetField | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                // Get the boost time. If above 30 seconds, the hopper is empty, or has no freezon then leave.
                float mrBoostTime = (float) fieldInfo.GetValue(geothermalGenerator);
                if (mrBoostTime > 30.0 || UsedCapacity <= 0 || CountHowManyOfItem(FREEZON_ITEM_ID) <= 0)
                    return;

                // Request a network update.
                RequestImmediateNetworkUpdate();

                // Remove the freezon from the hopper and add it to the geothermal generator.
                RemoveInventoryItem(FREEZON_ITEM_ID, 1);
                geothermalGenerator.AddFreezon();
            }
        }


        /// <summary>
        /// Check for a neighboring hopper at the specific position and perform any cube re-balancing.
        /// </summary>
        /// <param name="checkX">X position.</param>
        /// <param name="checkY">Y position.</param>
        /// <param name="checkZ">Z position.</param>
        /// <param name="checkSegment">Segment of the position.</param>
        /// <param name="lCube"></param>
        private void CheckNeighborHopper(long checkX, long checkY, long checkZ, Segment checkSegment, ushort lCube)
        {
            // If content sharing is off, permissions are locked, or there is insufficient quantity to share, or the cubes is not stored then leave.
            if (!ContentSharingOn || Permissions == eHopperPermissions.Locked ||
                //Permissions == eHopperPermissions.AddOnly || Permissions == eHopperPermissions.Locked ||
                UsedCapacity <= 2 || !CubeHelper.HasEntity(lCube))
                return;

            // Check for a storage machine interface at the position. If none then leave.
            if (!(checkSegment.SearchEntity(checkX, checkY, checkZ) is StorageMachineInterface machineInterface))
                return;

            // Perform re-balance loops.
            for (int index = 0; index < HOPPER_CUBE_RE_BALANCE_LOOPS && UsedCapacity > 2; ++index)
            {
                switch (machineInterface.GetPermissions())
                {
                    case eHopperPermissions.Locked:
                        return;
                    case eHopperPermissions.RemoveOnly:
                        return;
                    default:
                        if (machineInterface.RemainingCapacity <= 1)
                            return;
                        if (machineInterface.UsedCapacity < UsedCapacity - 1)
                        {
                            GetSpecificCube(eHopperRequestType.eAny, false, out ushort cubeType, out ushort cubeValue);
                            if (cubeType != 0)
                                machineInterface.TryInsert(this, cubeType, cubeValue, 1);
                        }

                        continue;
                }
            }
        }


        /// <summary>
        /// Updates the counts of free slots.
        /// </summary>
        private void CountFreeSlots()
        {
            int originalStorageUsed = UsedCapacity;
            UsedCapacity = 0;
            lock (mInventory)
            {
                foreach (InventoryStack inventoryStack in mInventory.Values)
                    UsedCapacity += inventoryStack.Count;

                if (originalStorageUsed != UsedCapacity)
                    mForceTextUpdate = true;
            }

            TrickyStorageHopperWindow.SetDirty();
        }


        /// <summary>
        /// Adds an item to the hopper.
        /// </summary>
        /// <param name="itemToAdd">Item to add.</param>
        /// <returns>True if successful, otherwise false.</returns>
        public bool AddItem(ItemBase itemToAdd)
        {
            // If null then return as successful (even though it did nothing). Not sure if this is needed but mimics vanilla to be safe.
            if (itemToAdd == null)
                return true;

            // If this is a void hopper then handle handle hivemind feeding and leave without actually storing the object,
            if (mValue == 0)
            {
                if (HivemindFeedingOn)
                    CentralPowerHub.mClosestHive?.AddItem(itemToAdd);
                mLastItemAdded = itemToAdd;
                VoidHopperDeleteCount += itemToAdd.GetAmount();
                mForceTextUpdate = true;
                return true;
            }

            // If item not allowed (wrong type for one type hopper) then fail.
            if (!CheckItemAllowed(itemToAdd.ToStorageId()))
                return false;

            // Ensure free slot count is up to date.
            CountFreeSlots();

            // If there is insufficient space then return false.
            int remainingCapacity = mMaximumCapacity - UsedCapacity;
            if (remainingCapacity <= 0)
                return false;

            lock (mInventory)
            {
                uint storageId = itemToAdd.ToStorageId();
                if (!mInventory.TryGetValue(storageId, out InventoryStack inventoryStack))
                    mInventory[storageId] = inventoryStack = new InventoryStack(itemToAdd.mType, storageId);

                switch (itemToAdd.mType)
                {
                    case ItemType.ItemCubeStack:
                        ItemCubeStack itemCubeStack = (ItemCubeStack) itemToAdd;
                        if (itemCubeStack.mnAmount == 0 || itemCubeStack.mnAmount > remainingCapacity)
                            return false;
                        ((ItemCubeStack) inventoryStack.Item).mnAmount += itemCubeStack.mnAmount;
                        break;

                    case ItemType.ItemStack:
                        ItemStack itemStack = (ItemStack) itemToAdd;
                        if (itemStack.mnAmount == 0 || itemStack.mnAmount > remainingCapacity)
                            return false;

                        ((ItemStack) inventoryStack.Item).mnAmount += itemStack.mnAmount;
                        break;

                    default:
                        inventoryStack.AddSubStackItem(itemToAdd);
                        break;
                }

                mLastItemAdded = itemToAdd;
                mLastItemAddedText = !WorldScript.mLocalPlayer.mResearch.IsKnown(itemToAdd)
                    ? "Unknown Material"
                    : ItemManager.GetItemName(itemToAdd);
            }

            CountFreeSlots();
            MarkDirtyDelayed();
            mForceTextUpdate = true;
            return true;
        }


        /// <summary>
        /// Adds a cube to the hopper.
        /// </summary>
        /// <param name="cubeType">Cube type.</param>
        /// <param name="cubeValue">Cube value.</param>
        /// <returns>True if successful, otherwise false.</returns>
        public bool AddCube(ushort cubeType, ushort cubeValue)
        {
            // If cube value is 0 or not remaining capacity then leave.
            int remainingCapacity = mMaximumCapacity - UsedCapacity;
            if (cubeType == 0 || remainingCapacity <= 0)
                return false;

            // If this is a void hopper then handle handle hivemind feeding and leave without actually storing the object,
            if (mValue == 0)
            {
                if (HivemindFeedingOn)
                    CentralPowerHub.mClosestHive?.AddCube(cubeType, cubeValue);
                VoidHopperDeleteCount++;
            }
            else
            {
                uint storageId = ((uint) cubeType << 16) + cubeValue;

                // If item not allowed (wrong type for one type hopper) then fail.
                if (!CheckItemAllowed(storageId))
                    return false;

                lock (mInventory)
                {
                    if (!mInventory.TryGetValue(storageId, out InventoryStack inventoryStack))
                        mInventory[storageId] = inventoryStack = new InventoryStack(ItemType.ItemCubeStack, storageId);

                    ((ItemCubeStack) inventoryStack.Item).mnAmount++;
                }
            }


            mLastItemAdded = new ItemCubeStack(cubeType, cubeValue, 1);
            mLastItemAddedText = !WorldScript.mLocalPlayer.mResearch.IsKnown(cubeType, cubeValue)
                ? "Unknown Material"
                : TerrainData.GetNameForValue(cubeType, cubeValue);

            CountFreeSlots();
            MarkDirtyDelayed();
            mForceTextUpdate = true;
            return true;
        }


        /// <summary>
        /// Returns total count of the specified cube type/value in the hopper.
        /// </summary>
        /// <param name="cubeType">Cube type.</param>
        /// <param name="cubeValue">Cube value.</param>
        /// <returns>Total count of the specified cube type/value in the hopper.</returns>
        public int CountHowManyOfType(ushort cubeType, ushort cubeValue)
        {
            uint storageId = ((uint) cubeType << 16) + cubeValue;
            lock (mInventory)
            {
                return !mInventory.TryGetValue(storageId, out InventoryStack inventoryStack) ? 0 : inventoryStack.Count;
            }
        }


        /// <summary>
        /// Returns the total count of a specified item in the hopper.
        /// </summary>
        /// <param name="itemId">Item Id.</param>
        /// <returns>Total count of the specified item in the hopper.</returns>
        private int CountHowManyOfItem(int itemId)
        {
            lock (mInventory)
            {
                return !mInventory.TryGetValue((uint) itemId, out InventoryStack inventoryStack) ? 0 : inventoryStack.Count;
            }
        }


        /// <summary>
        /// Removes a cube from the hopper up to the specified amount.
        /// </summary>
        /// <param name="cubeType">Cube type.</param>
        /// <param name="cubeValue">Cube value.</param>
        /// <param name="amount">Desired amount to remove.</param>
        /// <returns>Number of cubes actually removed.</returns>
        internal int RemoveInventoryCube(ushort cubeType, ushort cubeValue, int amount)
        {
            // If nothing stored then leave.
            if (UsedCapacity <= 0)
                return 0;

            uint storageId = ((uint) cubeType << 16) + cubeValue;

            lock (mInventory)
            {
                if (!mInventory.TryGetValue(storageId, out InventoryStack inventoryStack))
                    mInventory[storageId] = inventoryStack = new InventoryStack(ItemType.ItemCubeStack, storageId);

                ItemCubeStack itemCubeStack = (ItemCubeStack) inventoryStack.Item;
                if (itemCubeStack.mnAmount == 0)
                    return 0;

                if (amount > itemCubeStack.mnAmount)
                    amount = itemCubeStack.mnAmount;
                itemCubeStack.mnAmount -= amount;
            }

            RequestImmediateNetworkUpdate();
            CountFreeSlots();
            MarkDirtyDelayed();

            lock (mInventory)
            {
                if (UsedCapacity == 0)
                {
                    mLastItemAdded = null;
                    mLastItemAddedText = "Empty";
                }
            }

            return amount;
        }


        /// <summary>
        /// Removes item from the hopper up to the specified amount.
        /// </summary>
        /// <param name="itemId">Item Id to remove.</param>
        /// <param name="amount">Desired amount to remove.</param>
        /// <returns>Number of items actually removed.</returns>
        internal int RemoveInventoryItem(int itemId, int amount)
        {
            // If nothing stored or no amount to remove then leave.
            if (UsedCapacity <= 0 || amount <= 0)
                return 0;

            uint storageId = (uint) itemId;

            lock (mInventory)
            {
                if (!mInventory.TryGetValue(storageId, out InventoryStack inventoryStack))
                    return 0;

                if (inventoryStack.ItemType == ItemType.ItemStack)
                {
                    ItemStack itemStack = (ItemStack) inventoryStack.Item;
                    if (itemStack.mnAmount == 0)
                        return 0;

                    if (amount > itemStack.mnAmount)
                        amount = itemStack.mnAmount;

                    itemStack.mnAmount -= amount;
                }
                else
                {
                    amount = inventoryStack.RemoveSubStackAmount(amount);

                    if (inventoryStack.Count == 0)
                        return 0;
                }
            }

            RequestImmediateNetworkUpdate();
            CountFreeSlots();
            MarkDirtyDelayed();

            lock (mInventory)
            {
                if (UsedCapacity == 0)
                {
                    mLastItemAdded = null;
                    mLastItemAddedText = "Empty";
                }
            }

            return amount;
        }


        /// <summary>
        /// Removes the next cube type/value in the hopper that meets the request type and outputs the cube type/value.
        /// </summary>
        /// <param name="requestType">Hopper request type.</param>
        /// <param name="manual">True if this is a manual (user) action.</param>
        /// <param name="cubeType">Output cube type removed.</param>
        /// <param name="cubeValue">Output cube value removed.</param>
        public void GetSpecificCube(eHopperRequestType requestType, bool manual, out ushort cubeType, out ushort cubeValue)
        {
            // If the type does not match with cubes or nothing is stored then return with a cube type and value of zero.
            if (requestType == eHopperRequestType.eNone || requestType == eHopperRequestType.eBarsOnly ||
                requestType == eHopperRequestType.eAnyCraftedItem || UsedCapacity <= 0)
            {
                cubeType = 0;
                cubeValue = 0;
                return;
            }

            lock (mInventory)
            {
                // Loop through each inventory slot.
                foreach (InventoryStack inventoryStack in mInventory.Values)
                {
                    // If not a cube stack then skip to the next.
                    if (inventoryStack.ItemType != ItemType.ItemCubeStack)
                        continue;

                    ItemCubeStack itemCubeStack = (ItemCubeStack) inventoryStack.Item;

                    // Check the cube type against the request and remove if a match.
                    if ((requestType != eHopperRequestType.eHighCalorieOnly || CubeHelper.IsHighCalorie(itemCubeStack.mCubeType)) &&
                        (requestType != eHopperRequestType.eOreOnly || CubeHelper.IsSmeltableOre(itemCubeStack.mCubeType)) &&
                        (requestType != eHopperRequestType.eGarbage || CubeHelper.IsGarbage(itemCubeStack.mCubeType)) &&
                        (requestType != eHopperRequestType.eCrystals || itemCubeStack.mCubeType == eCubeTypes.OreCrystal) &&
                        (requestType != eHopperRequestType.eGems || itemCubeStack.mCubeType == eCubeTypes.Crystal) &&
                        (requestType != eHopperRequestType.eBioMass || itemCubeStack.mCubeType == eCubeTypes.OreBioMass) &&
                        (requestType != eHopperRequestType.eSmeltable || CubeHelper.IsIngottableOre(itemCubeStack.mCubeType)))
                    {
                        // If the request is for a type that can be researched and the cube has a decompose value data then skip to the next inventory slot.
                        if (requestType == eHopperRequestType.eResearchable)
                        {
                            TerrainDataEntry terrainDataEntry = TerrainData.mEntries[itemCubeStack.mCubeType];
                            if (terrainDataEntry == null || terrainDataEntry.DecomposeValue <= 0)
                                continue;
                        }

                        // Remove the cube, set the output cube type/value, and leave.
                        RemoveInventoryCube(itemCubeStack.mCubeType, itemCubeStack.mCubeValue, 1);
                        if (UsedCapacity == 0)
                        {
                            mLastItemAdded = null;
                            mLastItemAddedText = "Empty";
                        }

                        cubeType = itemCubeStack.mCubeType;
                        cubeValue = itemCubeStack.mCubeValue;
                        return;
                    }
                }
            }

            // No matches found. Return with a cube type and value of zero.
            cubeType = 0;
            cubeValue = 0;
        }


        /// <summary>
        /// Finds the next item or cube that can be removed.
        /// </summary>
        /// <param name="itemId">Output item Id to remove or -1 if none.</param>
        /// <param name="cubeType">Output cube type to remove of 0 if none.</param>
        /// <param name="cubeValue">Output cube value to remove if cubeType is not zero.</param>
        /// <param name="amount">Amount to remove adjusted to balance the players inventory amount.</param>
        /// <returns>True if a matching item or cube was found that can be removed.</returns>
        internal bool GetNextRemoveInventorySlot(out int itemId, out ushort cubeType, out ushort cubeValue, out int amount)
        {
            lock (mInventory)
            {
                foreach (uint storageId in mInventory.Keys)
                {
                    InventoryStack inventoryStack = mInventory[storageId];
                    if (inventoryStack.Count == 0)
                        continue;

                    switch (inventoryStack.ItemType)
                    {
                        case ItemType.ItemCubeStack:
                            ItemCubeStack itemCubeStack = (ItemCubeStack) inventoryStack.Item;
                            itemId = -1;
                            cubeType = itemCubeStack.mCubeType;
                            cubeValue = itemCubeStack.mCubeValue;
                            amount = itemCubeStack.mnAmount;
                            return true;

                        case ItemType.ItemStack:
                            ItemStack itemStack = (ItemStack) inventoryStack.Item;
                            itemId = itemStack.mnItemID;
                            cubeType = 0;
                            cubeValue = 0;
                            amount = itemStack.mnAmount;
                            return true;

                        default:
                            itemId = (int) inventoryStack.StorageId;
                            cubeType = 0;
                            cubeValue = 0;
                            amount = 1;
                            return true;
                    }
                }
            }

            // No match found. Set the outputs and return false.
            itemId = 0;
            cubeType = 0;
            cubeValue = 0;
            amount = 0;
            return false;
        }


        /// <summary>
        /// Handle IterateContents.
        /// </summary>
        public void IterateContents(IterateItem itemFunc, object state)
        {
            if (itemFunc == null)
                return;

            lock (mInventory)
            {
                foreach (InventoryStack inventoryStack in mInventory.Values)
                {
                    switch (inventoryStack.ItemType)
                    {
                        case ItemType.ItemCubeStack:
                        case ItemType.ItemStack:
                            if (!itemFunc(inventoryStack.Item, state))
                                return;
                            break;
                        default:
                            if (!inventoryStack.IterateContents(itemFunc, state))
                                return;
                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Handle UnloadToList.
        /// </summary>
        public int UnloadToList(List<ItemBase> cargoList, int amountToExtract)
        {
            // If nothing stored then leave.
            if (UsedCapacity <= 0)
                return 0;

            // Cap the amount at the maximum possible.
            if (amountToExtract > UsedCapacity)
                amountToExtract = UsedCapacity;

            // Loop through inventory and remove to the list.
            int amount = amountToExtract;
            lock (mInventory)
            {
                foreach (InventoryStack inventoryStack in mInventory.Values)
                {
                    switch (inventoryStack.ItemType)
                    {
                        case ItemType.ItemCubeStack:
                            ItemCubeStack itemCubeStack = (ItemCubeStack) inventoryStack.Item;
                            if (itemCubeStack.mnAmount == 0)
                                continue;

                            int cubesRemoved = Math.Min(amount, itemCubeStack.mnAmount);
                            cargoList.Add(new ItemCubeStack(itemCubeStack.mCubeType, itemCubeStack.mCubeValue, cubesRemoved));
                            amount -= cubesRemoved;
                            itemCubeStack.mnAmount -= cubesRemoved;
                            break;

                        case ItemType.ItemStack:
                            ItemStack itemStack = (ItemStack) inventoryStack.Item;
                            if (itemStack.mnAmount == 0)
                                continue;

                            int itemsRemoved = Math.Min(amount, itemStack.mnAmount);
                            cargoList.Add(new ItemStack(itemStack.mnItemID, itemsRemoved));
                            amount -= itemsRemoved;
                            itemStack.mnAmount -= itemsRemoved;
                            break;

                        default:
                            while (inventoryStack.Count > 0 && amount > 0)
                            {
                                cargoList.Add(inventoryStack.RemoveSubStackItem());
                                amount--;
                            }

                            break;
                    }

                    if (amount == 0)
                        break;
                }

            }

            MarkDirtyDelayed();
            RequestImmediateNetworkUpdate();
            CountFreeSlots();
            return amountToExtract - amount;
        }


        /// <summary>
        /// Handle TryDeliverItem.
        /// </summary>
        public bool TryDeliverItem(StorageUserInterface sourceEntity, ItemBase item, ushort cubeType, ushort cubeValue, bool sendImmediateNetworkUpdate)
        {
            try
            {
                if (item == null && cubeType == 0)
                {
                    if (WorldScript.mbIsServer)
                        Logging.LogError(this, "TryDeliverItem received with no item\n" + Environment.StackTrace);
                    return false;
                }

                if (RemainingCapacity <= 0)
                    return false;
                if (item != null)
                    AddItem(item);
                else
                    AddCube(cubeType, cubeValue);
                if (sendImmediateNetworkUpdate)
                    RequestImmediateNetworkUpdate();
                return true;

            }
            catch (Exception e)
            {
                Logging.LogException(this, e);
                return false;
            }
        }


        /// <summary>
        /// Handle GetPermissions.
        /// </summary>
        public eHopperPermissions GetPermissions()
        {
            return Permissions;
        }


        /// <summary>
        /// Handle IsEmpty.
        /// </summary>
        public bool IsEmpty()
        {
            return UsedCapacity <= 0;
        }


        /// <summary>
        /// Handle IsFull.
        /// </summary>
        public bool IsFull()
        {
            return RemainingCapacity == 0;
        }


        /// <summary>
        /// Handle IsNotEmpty.
        /// </summary>
        public bool IsNotEmpty()
        {
            return UsedCapacity > 0;
        }


        /// <summary>
        /// Handle IsNotFull.
        /// </summary>
        public bool IsNotFull()
        {
            return RemainingCapacity > 0;
        }


        /// <summary>
        /// Handle TryExtract.
        /// </summary>
        public bool TryExtract(InventoryExtractionOptions options, ref InventoryExtractionResults results)
        {
            if (!TryExtract(options.RequestType, options.ExemplarItemID, options.ExemplarBlockID, options.ExemplarBlockValue, options.InvertExemplar,
                options.MinimumAmount, options.MaximumAmount, options.KnownItemsOnly, false, false, options.ConvertToItem, out ItemBase returnedItem,
                out ushort returnedCubeType, out ushort returnedCubeValue, out int returnedAmount))
            {
                results.Item = null;
                results.Amount = 0;
                results.Cube = 0;
                results.Value = 0;
                return false;
            }

            results.Cube = returnedCubeType;
            results.Value = returnedCubeValue;
            results.Amount = returnedAmount;
            results.Item = returnedItem;
            return true;
        }


        /// <summary>
        /// Handle TryExtract.
        /// </summary>
        public bool TryExtract(eHopperRequestType lType, int exemplarItemId, ushort exemplarCubeType, ushort exemplarCubeValue, bool invertExemplar,
            int minimumAmount, int maximumAmount, bool knownItemsOnly, bool countOnly, bool trashItems, bool convertCubesToItems, out ItemBase returnedItem,
            out ushort returnedCubeType, out ushort returnedCubeValue, out int returnedAmount)
        {
            if (lType == eHopperRequestType.eNone && exemplarItemId == -1 && exemplarCubeType == 0)
            {
                returnedItem = null;
                returnedCubeType = 0;
                returnedCubeValue = 0;
                returnedAmount = 0;
                return false;
            }

            int amount = 0;
            uint returnStorageId = uint.MaxValue;
            List<uint> removeStorageIdList = new List<uint>();

            lock (mInventory)
            {
                List<uint> storageIdList = new List<uint>(mInventory.Keys.ToList());
                InventoryStack inventoryStack;
                for (int index = 0; index < storageIdList.Count; index++)
                {
                    ++mRoundRobinOffset;
                    mRoundRobinOffset %= storageIdList.Count;

                    uint storageId = storageIdList[mRoundRobinOffset];
                    inventoryStack = mInventory[storageId];
                    int inventoryStackCount = inventoryStack.Count;

                    ushort inventoryCubeType = inventoryStack.CubeType;
                    ushort inventoryCubeValue = inventoryStack.CubeValue;

                    // Perform exemplar check.
                    if (exemplarItemId >= 0)
                    {
                        bool match = inventoryStack.ItemType != ItemType.ItemCubeStack && storageId == exemplarItemId;
                        if (match == invertExemplar)
                            continue;
                    }
                    else if (exemplarCubeType > 0)
                    {
                        bool match = inventoryStack.ItemType == ItemType.ItemCubeStack && inventoryCubeType == exemplarCubeType &&
                                     (inventoryCubeValue == exemplarCubeValue || exemplarCubeType == ushort.MaxValue);
                        if (match == invertExemplar)
                            continue;
                    }

                    // Perform request type check.
                    if (lType != eHopperRequestType.eAny)
                    {
                        bool itemDoesNotMatchRequest = false;
                        if (inventoryStack.ItemType != ItemType.ItemCubeStack)
                        {
                            int itemId = (int) storageId;
                            if (lType == eHopperRequestType.eOrganic && itemId >= 4000 && itemId <= 4101)
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eBarsOnly)
                            {
                                if (itemId == ItemEntries.CopperBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.TinBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.IronBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.LithiumBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.GoldBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.NickelBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.TitaniumBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.ChromiumBar)
                                    itemDoesNotMatchRequest = true;
                                if (itemId == ItemEntries.MolybdenumBar)
                                    itemDoesNotMatchRequest = true;
                            }

                            if (lType == eHopperRequestType.eAnyCraftedItem)
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eResearchable)
                            {
                                ItemEntry itemEntry = ItemEntry.mEntries[itemId];
                                if (itemEntry != null && itemEntry.DecomposeValue > 0)
                                    itemDoesNotMatchRequest = true;
                            }
                        }
                        else
                        {
                            if (lType == eHopperRequestType.eResearchable)
                            {
                                TerrainDataEntry terrainDataEntry = TerrainData.mEntries[inventoryCubeType];
                                if (terrainDataEntry != null && terrainDataEntry.DecomposeValue > 0)
                                    itemDoesNotMatchRequest = true;
                            }

                            if (lType == eHopperRequestType.eHighCalorieOnly && CubeHelper.IsHighCalorie(inventoryCubeType))
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eOreOnly && CubeHelper.IsSmeltableOre(inventoryCubeType))
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eGarbage && CubeHelper.IsGarbage(inventoryCubeType))
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eCrystals && inventoryCubeType == eCubeTypes.OreCrystal)
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eGems && inventoryCubeType == eCubeTypes.Crystal)
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eBioMass && inventoryCubeType == eCubeTypes.OreBioMass)
                                itemDoesNotMatchRequest = true;
                            if (lType == eHopperRequestType.eSmeltable && CubeHelper.IsIngottableOre(inventoryCubeType))
                                itemDoesNotMatchRequest = true;
                        }

                        // This makes filter conveyor invert feature work.
                        if (itemDoesNotMatchRequest == invertExemplar)
                            continue;
                    }

                    // Perform known items check.
                    if (knownItemsOnly)
                    {
                        if (inventoryStack.ItemType != ItemType.ItemCubeStack && !WorldScript.mLocalPlayer.mResearch.IsKnown((int) storageId) ||
                            inventoryStack.ItemType == ItemType.ItemCubeStack &&
                            !WorldScript.mLocalPlayer.mResearch.IsKnown(inventoryCubeType, inventoryCubeValue))
                            continue;
                    }

                    // If count only then tally the amount and continue.
                    if (countOnly)
                    {
                        amount += inventoryStackCount;
                        continue;
                    }

                    if (returnStorageId == uint.MaxValue)
                    {
                        bool isStack = inventoryStack.ItemType == ItemType.ItemCubeStack || inventoryStack.ItemType == ItemType.ItemStack;
                        if (isStack && inventoryStackCount >= minimumAmount && maximumAmount > 0 ||
                            !isStack && minimumAmount <= 1 && maximumAmount > 0)
                            returnStorageId = storageId;
                    }

                    removeStorageIdList.Add(storageId);

                    if (!trashItems && returnStorageId != uint.MaxValue)
                        break;
                }

                if (countOnly || removeStorageIdList.Count == 0 || returnStorageId == uint.MaxValue && !trashItems)
                {
                    returnedAmount = amount;
                    returnedCubeType = 0;
                    returnedCubeValue = 0;
                    returnedItem = null;
                    return returnedAmount > 0;
                }


                inventoryStack = mInventory[returnStorageId];
                switch (inventoryStack.ItemType)
                {
                    case ItemType.ItemCubeStack:
                        ItemCubeStack itemCubeStack = (ItemCubeStack) inventoryStack.Item;
                        int extractedCubeAmount = Math.Min(itemCubeStack.mnAmount, maximumAmount);
                        returnedCubeType = itemCubeStack.mCubeType;
                        returnedCubeValue = itemCubeStack.mCubeValue;
                        returnedAmount = extractedCubeAmount;
                        itemCubeStack.mnAmount -= extractedCubeAmount;
                        returnedItem = convertCubesToItems ? new ItemCubeStack(returnedCubeType, returnedCubeValue, extractedCubeAmount) : null;
                        break;

                    case ItemType.ItemStack:
                        ItemStack itemStack = (ItemStack) inventoryStack.Item;
                        int extractedStackAmount = Math.Min(itemStack.mnAmount, maximumAmount);
                        returnedCubeType = 0;
                        returnedCubeValue = 0;
                        returnedAmount = extractedStackAmount;
                        itemStack.mnAmount -= extractedStackAmount;
                        returnedItem = new ItemStack(itemStack.mnItemID, extractedStackAmount);
                        break;

                    default:
                        returnedCubeType = 0;
                        returnedCubeValue = 0;
                        returnedAmount = 1;
                        returnedItem = inventoryStack.RemoveSubStackItem();
                        break;
                }

                if (trashItems)
                    foreach (uint storageId in removeStorageIdList)
                    {
                        inventoryStack = mInventory[storageId];
                        inventoryStack.Item = null;
                        inventoryStack.Clear();
                    }
            }

            CountFreeSlots();
            MarkDirtyDelayed();
            RequestImmediateNetworkUpdate();
            return true;
        }


        /// <summary>
        /// Handle TryExtractCubes.
        /// </summary>
        public bool TryExtractCubes(StorageUserInterface sourceEntity, ushort cube, ushort value, int amount)
        {
            return TryExtract(eHopperRequestType.eAny, -1, cube, value, false, amount, amount, false, false, false, false, out _,
                out _, out _, out _);
        }


        /// <summary>
        /// Handle TryExtractItems.
        /// </summary>
        public bool TryExtractItems(StorageUserInterface sourceEntity, int itemId, int amount, out ItemBase item)
        {
            return TryExtract(eHopperRequestType.eAny, itemId, 0, 0, false, amount, amount, false, false, false, true, out item,
                out _, out _, out _);
        }


        /// <summary>
        /// Handle TryExtractItemsOrCubes.
        /// </summary>
        public bool TryExtractItemsOrCubes(StorageUserInterface sourceEntity, int itemId, ushort cube, ushort value, int amount, out ItemBase item)
        {
            return TryExtract(eHopperRequestType.eAny, itemId, cube, value, false, amount, amount, false, false, false, true, out item, out _,
                out _, out _);
        }


        /// <summary>
        /// Handle TryExtractItemsOrCubes.
        /// </summary>
        public bool TryExtractItemsOrCubes(StorageUserInterface sourceEntity, int itemId, ushort cube, ushort value, int amount)
        {
            return TryExtract(eHopperRequestType.eAny, itemId, cube, value, false, amount, amount, false, false, true, false, out _,
                out _, out _, out _);
        }


        /// <summary>
        /// Handle TryExtractItems.
        /// </summary>
        public bool TryExtractItems(StorageUserInterface sourceEntity, int itemId, int amount)
        {
            return TryExtract(eHopperRequestType.eAny, itemId, 0, 0, false, amount, amount, false, false, true, false, out _,
                out _, out _, out _);
        }


        /// <summary>
        /// Handle TryPartialExtractCubes.
        /// </summary>
        public int TryPartialExtractCubes(StorageUserInterface sourceEntity, ushort cube, ushort value, int amount)
        {
            TryExtract(eHopperRequestType.eAny, -1, cube, value, false, amount, amount, false, false, false, false, out _, out _,
                out _, out int returnedAmount);
            return returnedAmount;
        }


        /// <summary>
        /// Handle TryPartialExtractItems.
        /// </summary>
        public int TryPartialExtractItems(StorageUserInterface sourceEntity, int itemId, int amount, out ItemBase item)
        {
            TryExtract(eHopperRequestType.eAny, itemId, 0, 0, false, amount, amount, false, false, false, true, out item, out _,
                out _, out int returnedAmount);
            return returnedAmount;
        }


        /// <summary>
        /// Handle TryPartialExtractItemsOrCubes.
        /// </summary>
        public int TryPartialExtractItemsOrCubes(StorageUserInterface sourceEntity, int itemId, ushort cube, ushort value, int amount, out ItemBase item)
        {
            TryExtract(eHopperRequestType.eAny, itemId, cube, value, false, amount, amount, false, false, false, true, out item, out _,
                out _, out int returnedAmount);
            return returnedAmount;
        }


        /// <summary>
        /// Handle TryPartialExtractItemsOrCubes.
        /// </summary>
        public int TryPartialExtractItemsOrCubes(StorageUserInterface sourceEntity, int itemId, ushort cube, ushort value, int amount)
        {
            TryExtract(eHopperRequestType.eAny, itemId, cube, value, false, amount, amount, false, false, false, false, out _, out _,
                out _, out int returnedAmount);
            return returnedAmount;
        }


        /// <summary>
        /// Handle TryPartialExtractItems.
        /// </summary>
        public int TryPartialExtractItems(StorageUserInterface sourceEntity, int itemId, int amount)
        {
            TryExtract(eHopperRequestType.eAny, itemId, 0, 0, false, amount, amount, false, false, true, false, out _,
                out _, out _, out int returnedAmount);
            return returnedAmount;
        }


        /// <summary>
        /// Handle TryExtractAny.
        /// </summary>
        public bool TryExtractAny(StorageUserInterface sourceEntity, int amount, out ItemBase item)
        {
            return TryExtract(eHopperRequestType.eAny, -1, 0, 0, false, amount, amount, false, false, false, true, out item,
                out _, out _, out _);
        }


        /// <summary>
        /// Handle TryInsert.
        /// </summary>
        public bool TryInsert(InventoryInsertionOptions options, ref InventoryInsertionResults results)
        {
            int remainingCapacity = RemainingCapacity;
            if (remainingCapacity <= 0)
                return false;

            if (options.Item != null)
            {
                int currentStackSize = ItemManager.GetCurrentStackSize(options.Item);
                int amount = currentStackSize;
                ItemBase lItemToAdd;
                if (currentStackSize > remainingCapacity)
                {
                    if (!options.AllowPartialInsertion)
                        return false;
                    amount = remainingCapacity;
                    lItemToAdd = ItemManager.CloneItem(options.Item, amount);
                }
                else
                    lItemToAdd = options.Item;

                if (!AddItem(lItemToAdd))
                    return false;
                if (results == null)
                    results = new InventoryInsertionResults();
                results.AmountInserted = amount;
                results.AmountRemaining = currentStackSize - amount;
                return false;
            }

            if (options.Amount == 1)
            {
                if (!AddCube(options.Cube, options.Value))
                    return false;
                if (results == null)
                    results = new InventoryInsertionResults();
                results.AmountInserted = 1;
                return true;
            }

            if (options.Amount > remainingCapacity && !options.AllowPartialInsertion)
                return false;

            int remainder;
            for (remainder = options.Amount; remainder > 0; --remainder)
                if (!AddCube(options.Cube, options.Value))
                    break;

            if (results == null)
                results = new InventoryInsertionResults();
            results.AmountInserted = options.Amount - remainder;
            results.AmountRemaining = remainder;
            return true;
        }


        /// <summary>
        /// Handle TryInsert.
        /// </summary>
        public bool TryInsert(StorageUserInterface sourceEntity, ItemBase item)
        {
            return AddItem(item);
        }


        /// <summary>
        /// Handle TryInsert.
        /// </summary>
        public bool TryInsert(StorageUserInterface sourceEntity, ushort cube, ushort value, int amount)
        {
            if (RemainingCapacity <= 0)
                return false;

            // Adding is committed at this point so all must go in.
            for (int index = amount; index > 0; --index)
                AddCube(cube, value);
            return true;
        }


        /// <summary>
        /// Handle TryPartialInsert.
        /// </summary>
        public int TryPartialInsert(StorageUserInterface sourceEntity, ref ItemBase item, bool alwaysCloneItem, bool updateSourceItem)
        {
            int currentStackSize = ItemManager.GetCurrentStackSize(item);
            int amount = currentStackSize;

            int remainingCapacity = RemainingCapacity;
            if (remainingCapacity <= 0)
                return 0;

            ItemBase itemToAdd;
            if (currentStackSize > remainingCapacity)
            {
                amount = remainingCapacity;
                itemToAdd = ItemManager.CloneItem(item, amount);
            }
            else itemToAdd = !alwaysCloneItem ? item : ItemManager.CloneItem(item, currentStackSize);

            if (!AddItem(itemToAdd))
                return 0;

            if (updateSourceItem)
            {
                if (currentStackSize > amount)
                    ItemManager.SetItemCount(item, currentStackSize - amount);
                else
                    item = null;
            }

            return amount;
        }


        /// <summary>
        /// Handle TryPartialInsert.
        /// </summary>
        public int TryPartialInsert(StorageUserInterface sourceEntity, ushort cube, ushort value, int amount)
        {
            int remainder;
            for (remainder = amount; remainder > 0; --remainder)
                if (!AddCube(cube, value))
                    break;
            return amount - remainder;
        }


        /// <summary>
        /// Handle CountItems.
        /// </summary>
        public int CountItems(InventoryExtractionOptions options)
        {
            if (!TryExtract(options.RequestType, options.ExemplarItemID, options.ExemplarBlockID, options.ExemplarBlockValue, options.InvertExemplar,
                options.MinimumAmount, options.MaximumAmount, options.KnownItemsOnly, true, false, false, out _, out _,
                out _, out int returnedAmount))
                return 0;
            return returnedAmount;
        }


        /// <summary>
        /// Handle CountItems.
        /// </summary>
        public int CountItems(int itemId)
        {
            return CountHowManyOfItem(itemId);
        }


        /// <summary>
        /// Handle CountItems.
        /// </summary>
        public int CountItems(int itemId, ushort cube, ushort value)
        {
            if (itemId >= 0)
                return CountHowManyOfItem(itemId);
            return CountHowManyOfType(cube, value);
        }


        /// <summary>
        /// Handle CountCubes.
        /// </summary>
        public int CountCubes(ushort cube, ushort value)
        {
            return CountHowManyOfType(cube, value);
        }


        /// <summary>
        /// Handle ShouldSave.
        /// </summary>
        public override bool ShouldSave()
        {
            return true;
        }


        /// <summary>
        /// Handle GetVersion.
        /// </summary>
        public override int GetVersion()
        {
            return 4;
        }


        /// <summary>
        /// Handle Write to store the state.
        /// </summary>
        public override void Write(BinaryWriter writer)
        {
            try
            {
                writer.Write(mCubeColor.r);
                writer.Write(mCubeColor.g);
                writer.Write(mCubeColor.b);
                writer.Write(mMaximumCapacity);
                writer.Write(OneTypeHopper);
                writer.Write(mOneTypeHopperStorageId);
                writer.Write((byte) Permissions);
                writer.Write(VacuumOn);


                if (mValue == 0)
                {
                    writer.Write(HivemindFeedingOn);
                    writer.Write(VoidHopperDeleteCount);
                }
                else writer.Write(ContentSharingOn);


                WriteInventory(writer);

            }
            catch (Exception e)
            {
                Logging.LogException(this, e);
            }
        }


        /// <summary>
        /// Handle Read to load the state.
        /// </summary>
        public override void Read(BinaryReader reader, int entityVersion)
        {
            try
            {
                if (entityVersion < 4)
                {
                    LegacyRead(reader, entityVersion);
                    return;
                }

                mCubeColor = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                mMaximumCapacity = reader.ReadUInt16();
                OneTypeHopper = reader.ReadBoolean();
                mOneTypeHopperStorageId = reader.ReadUInt32();
                Permissions = (eHopperPermissions) reader.ReadByte();
                VacuumOn = reader.ReadBoolean();

                if (mValue == 0)
                {
                    HivemindFeedingOn = reader.ReadBoolean();
                    VoidHopperDeleteCount = reader.ReadInt32();
                }
                else ContentSharingOn = reader.ReadBoolean();

                long startPosition = reader.BaseStream.Position;
                ReadInventory(reader);
                long endPosition = reader.BaseStream.Position;
                Logging.LogMessage(this, "Inventory Length: " + (endPosition - startPosition), 1);

                // Count free slots.
                CountFreeSlots();

                mForceHoloUpdate = true;
                mForceTextUpdate = true;

                if (OneTypeHopper)
                {
                    OneTypeItemName = ItemBaseExtensions.GetStorageIdName(mOneTypeHopperStorageId);
                    Logging.LogMessage(this, "One-type storage id: " + mOneTypeHopperStorageId + " - " + (OneTypeItemName ?? "<None>"), 1);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(this, e);
            }
        }


        /// <summary>
        /// Reads legacy hopper state binary data.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        /// <param name="entityVersion">Entity version.</param>
        private void LegacyRead(BinaryReader reader, int entityVersion)
        {
            try
            {
                if (OneTypeHopper && entityVersion < 4)
                {
                    int itemId = reader.ReadInt32();
                    reader.ReadString();
                    ushort cubeType = reader.ReadUInt16();
                    ushort cubeValue = reader.ReadUInt16();

                    if (itemId != -1)
                        mOneTypeHopperStorageId = (uint) itemId;
                    else
                        mOneTypeHopperStorageId = ((uint) cubeType << 16) + cubeValue;
                }

                // This comes from one of DJs flawed designs which was copied over to this mod.
                // This will be converted away from (purged with fire) immediately.
                Dictionary<ushort, int> dictionary1 = null;
                for (int index1 = 0; index1 < mMaximumCapacity; ++index1)
                {
                    ushort key = reader.ReadUInt16();
                    if (key != 0)
                    {
                        if (dictionary1 == null)
                            dictionary1 = new Dictionary<ushort, int>();
                        if (dictionary1.ContainsKey(key))
                        {
                            Dictionary<ushort, int> dictionary2;
                            ushort index2;
                            (dictionary2 = dictionary1)[index2 = key] = dictionary2[index2] + 1;
                        }
                        else dictionary1.Add(key, 1);
                    }
                }

                VacuumOn = reader.ReadBoolean();
                reader.ReadBoolean();
                HivemindFeedingOn = reader.ReadBoolean();

                if (entityVersion >= 3)
                    ContentSharingOn = reader.ReadBoolean();

                // Old "feed ticker"...can't find any real need for this.
                if (entityVersion == 1)
                    reader.ReadByte();
                else
                    reader.ReadInt32();

                // Unused 
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();

                // Void hopper deleted count.
                if (entityVersion == 1 || mValue == 0)
                    VoidHopperDeleteCount = reader.ReadInt32();

                // Hopper number for debugging is no longer used, we will log the hoppers actual cube location instead.
                reader.ReadInt32();

                // Another unused value.
                reader.ReadInt32();

                if (entityVersion > 0)
                {
                    if (entityVersion >= 4)
                    {
                        long startPosition = reader.BaseStream.Position;
                        ReadInventory(reader);
                        long endPosition = reader.BaseStream.Position;
                        Logging.LogMessage(this, "Inventory Length: " + (endPosition - startPosition), 1);

                        // Count free slots.
                        CountFreeSlots();
                    }
                    else
                    {
                        // Read the ItemBase stacks.
                        for (int index1 = 0; index1 < mMaximumCapacity; ++index1)
                        {
                            ItemBase itemBase = ItemFile.DeserialiseItem(reader);
                            AddItem(itemBase);
                        }
                    }

                }

                // Load the legacy inventory as proper ItemBase stacks.
                if (dictionary1 != null)
                {
                    foreach (KeyValuePair<ushort, int> keyValuePair in dictionary1)
                    {
                        ushort key = keyValuePair.Key;
                        int amount = keyValuePair.Value;
                        AddItem(ItemManager.SpawnCubeStack(key, TerrainData.GetDefaultValue(key), amount));
                    }
                }

                mForceHoloUpdate = true;
                mForceTextUpdate = true;
            }
            catch (Exception e)
            {
                Logging.LogException(this, e);
            }
        }


        /// <summary>
        /// Reads inventory from binary data.
        /// </summary>
        /// <param name="reader">Binary reader.</param>
        private void ReadInventory(BinaryReader reader)
        {
            lock (mInventory)
            {
                int inventoryCount = reader.ReadUInt16();
                Logging.LogMessage(this, "Reading " + inventoryCount + " inventory entries", 1);

                for (int index = 0; index < inventoryCount; index++)
                {
                    InventoryStack inventoryStack = InventoryStack.Read(reader);
                    mInventory[inventoryStack.StorageId] = inventoryStack;
                }


                bool hasLastItemAdded = reader.ReadBoolean();
                if (hasLastItemAdded)
                    mLastItemAdded = ItemFile.DeserialiseItem(reader);

                if (mLastItemAdded!=null && !mSegment.mbValidateOnly && WorldScript.mbHasPlayer)
                    mLastItemAddedText = WorldScript.mLocalPlayer.mResearch == null || !WorldScript.mLocalPlayer.mResearch.IsKnown(mLastItemAdded)
                        ? "Unknown Material"
                        : ItemManager.GetItemName(mLastItemAdded);
                else
                {
                    mLastItemAdded = null;
                    mLastItemAddedText = "Empty";
                }

            }
        }


        /// <summary>
        /// Writes inventory to binary data.
        /// </summary>
        /// <param name="writer">Binary writer.</param>
        private void WriteInventory(BinaryWriter writer)
        {
            lock (mInventory)
            {
                List<InventoryStack> saveList = new List<InventoryStack>();
                foreach (InventoryStack inventoryStack in mInventory.Values)
                    if (inventoryStack.Count > 0)
                        saveList.Add(inventoryStack);

                writer.Write((ushort) saveList.Count);
                Logging.LogMessage(this, "Writing " + saveList.Count + " inventory stacks", 1);
                foreach (InventoryStack inventoryStack in saveList)
                {
                    inventoryStack.Write(writer);
                }

                writer.Write(mLastItemAdded != null);
                if (mLastItemAdded != null)
                    ItemFile.SerialiseItem(mLastItemAdded, writer);
            }
        }


        /// <summary>
        /// Handle ShouldNetworkUpdate.
        /// </summary>
        public override bool ShouldNetworkUpdate()
        {
            return true;
        }


        /// <summary>
        /// Handle WriteNetworkUpdate.
        /// </summary>
        public override void WriteNetworkUpdate(BinaryWriter writer)
        {
            writer.Write((byte) Permissions);
            writer.Write(VacuumOn);
            writer.Write(ContentSharingOn);
            writer.Write(HivemindFeedingOn);
            writer.Write(VoidHopperDeleteCount);
            WriteInventory(writer);
        }


        /// <summary>
        /// Handle ReadNetworkUpdate.
        /// </summary>
        public override void ReadNetworkUpdate(BinaryReader reader)
        {
            eHopperPermissions permissions = Permissions;
            Permissions = (eHopperPermissions) reader.ReadByte();
            if (!mSegment.mbValidateOnly && NetworkManager.mbClientRunning && FloatingCombatTextManager.instance != null && mLinkedToGameObject &&
                Permissions != permissions && mDistanceToPlayer < 32.0)
            {
                FloatingCombatTextQueue floatingCombatTextQueue = FloatingCombatTextManager.instance.QueueText(mnX, mnY + 1L, mnZ, 1f,
                    StorageHopper.GetHopperPermissionsString(Permissions), Color.cyan, 1.5f);
                if (floatingCombatTextQueue != null)
                    floatingCombatTextQueue.mrStartRadiusRand = 0.25f;
            }

            VacuumOn = reader.ReadBoolean();
            ContentSharingOn = reader.ReadBoolean();
            HivemindFeedingOn = reader.ReadBoolean();
            VoidHopperDeleteCount = reader.ReadInt32();

            ReadInventory(reader);
            mForceHoloUpdate = true;
            mForceTextUpdate = true;
        }


        /// <summary>
        /// Sets the hopper permissions.
        /// </summary>
        /// <param name="permissions">New hopper permissions.</param>
        /// <param name="audioNotify">Indicates if audio notifications should occur.</param>
        /// <param name="floatingTextNotify">Indicates if the floating text notification occurs.</param>
        public void SetPermissions(eHopperPermissions permissions, bool audioNotify, bool floatingTextNotify)
        {
            Permissions = permissions;
            MarkDirtyDelayed();

            mForceTextUpdate = true;
            mForceHoloUpdate = true;

            if (audioNotify)
            {
                AudioHUDManager.instance.HUDIn();
                AudioSpeechManager.instance.UpdateStorageHopper(permissions);
            }

            if (floatingTextNotify)
            {
                FloatingCombatTextQueue floatingCombatTextQueue = FloatingCombatTextManager.instance.QueueText(mnX, mnY + 1L, mnZ, 1f,
                    GetHopperPermissionsString(Permissions), Color.green, 1.5f);
                if (floatingCombatTextQueue != null)
                    floatingCombatTextQueue.mrStartRadiusRand = 0.25f;
            }

            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(nameof(TrickyStorageHopperWindow), TrickyStorageHopperWindow.COMMAND_SET_PERMISSIONS,
                    ((int) permissions).ToString(), null, this, 0.0f);
        }


        /// <summary>
        /// Sets the vacuum state.
        /// </summary>
        /// <param name="enabled">New enabled state.</param>
        /// <param name="audioNotify">Indicates audio notifications should occur.</param>
        public void SetVacuum(bool enabled, bool audioNotify)
        {
            VacuumOn = enabled;
            mForceTextUpdate = true;
            MarkDirtyDelayed();

            if (audioNotify)
                AudioHUDManager.instance.HUDIn();

            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(nameof(TrickyStorageHopperWindow), TrickyStorageHopperWindow.COMMAND_SET_VACUUM,
                    enabled ? "1" : "0", null, this, 0.0f);
        }



        /// <summary>
        /// Sets the content sharing state.
        /// </summary>
        /// <param name="enabled">New enabled state.</param>
        /// <param name="audioNotify">Indicates if audio notifications should occur.</param>
        public void SetContentSharing(bool enabled, bool audioNotify)
        {
            ContentSharingOn = enabled;
            mForceTextUpdate = true;
            MarkDirtyDelayed();

            if (audioNotify)
                AudioHUDManager.instance.HUDIn();

            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(nameof(TrickyStorageHopperWindow), TrickyStorageHopperWindow.COMMAND_SET_CONTENT_SHARING,
                    enabled ? "1" : "0", null, this, 0.0f);
        }


        /// <summary>
        /// Sets the feed hive mind state.
        /// </summary>
        /// <param name="enabled">New enabled state.</param>
        /// <param name="audioNotify">Indicates if audio notifications should occur.</param>
        public void SetHivemindFeeding(bool enabled, bool audioNotify)
        {
            HivemindFeedingOn = enabled;
            mForceTextUpdate = true;
            MarkDirtyDelayed();

            if (audioNotify)
                AudioHUDManager.instance.HUDIn();

            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(nameof(TrickyStorageHopperWindow), TrickyStorageHopperWindow.COMMAND_SET_HIVEMIND_FEEDING,
                    enabled ? "1" : "0", null, this, 0.0f);
        }


        /// <summary>
        /// Sets the one-type hopper item.
        /// </summary>
        /// <param name="itemBase">ItemBase to be used.</param>
        /// <param name="audioNotify">Indicates if audio notifications should occur.</param>
        public void SetOneTypeItem(ItemBase itemBase, bool audioNotify)
        {
            if (!OneTypeHopper)
                return;

            mOneTypeHopperStorageId = itemBase.ToStorageId();
            OneTypeItemName = itemBase.GetName();
            mForceTextUpdate = true;
            MarkDirtyDelayed();

            if (audioNotify)
                AudioHUDManager.instance.HUDIn();

            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(nameof(TrickyStorageHopperWindow), TrickyStorageHopperWindow.COMMAND_SET_ONE_TYPE_ITEM, null, itemBase, this, 0.0f);
        }


        /// <summary>
        /// Sets the retake debounce timer.
        /// </summary>
        public void SetRetakeDebounce()
        {
            mRetakeDebounce = 0.5f;
        }


        /// <summary>
        /// Handle Selected.
        /// </summary>
        public override void Selected()
        {
            mRetakeDebounce = 0.0f;
        }


        /// <summary>
        /// Handle GetPopupText.
        /// </summary>
        /// <returns></returns>
        public override string GetPopupText()
        {
            try
            {
                // If the player is not looking at this object then return the last set popup text.
                if (WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectBlockType != TrickStorageHopperMod.HopperCubeType ||
                    WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectedEntity == null)
                    return mPopupText;

                bool shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                mRetakeDebounce -= Time.deltaTime;
                StringBuilder stringBuilder = new StringBuilder(HopperName);

                if (mLastItemAdded != null)
                    stringBuilder.Append("\n" + string.Format(PersistentSettings.GetString("Last_Item_X"), mLastItemAdded));

                int availableStorage = Math.Max(0, mMaximumCapacity - UsedCapacity);
                stringBuilder.Append("\nUsed: " + UsedCapacity + "  " + "Free: " + availableStorage +
                                     "\n" + PersistentSettings.GetString("UI_E_Open_Interface") +
                                     "\n" + string.Format(PersistentSettings.GetString("Shift_E_Toggle_Vacuum_Status_X"),
                                         !VacuumOn ? PersistentSettings.GetString("Off") : PersistentSettings.GetString("On")));
                if (mValue == 0)
                    stringBuilder.Append("\n(Shift+X) Toggle Hivemind Feeding : " + (!HivemindFeedingOn ? PersistentSettings.GetString("Off") : PersistentSettings.GetString("On")));

                if (OneTypeHopper)
                {
                    if (IsEmpty())
                        stringBuilder.Append("\n(Shift+X) Set Storage Type : [" + (string.IsNullOrEmpty(OneTypeItemName) ? "<None>" : OneTypeItemName) + "]");
                    else
                        stringBuilder.Append("\nStorage Type : [" + (string.IsNullOrEmpty(OneTypeItemName) ? "<None>" : OneTypeItemName) + "]");
                }

                int lnAvailable = 0;
                ItemBase itemToStore = null;
                if (availableStorage > 0)
                {
                    itemToStore = UIManager.instance.GetCurrentHotBarItemOrCubeAsItem(out lnAvailable, true);
                    if (lnAvailable == 0)
                        itemToStore = null;
                    if (itemToStore != null && itemToStore.mType != ItemType.ItemCubeStack && itemToStore.mnItemID == -1)
                        itemToStore = null;
                }

                if (itemToStore != null)
                {
                    int amount = lnAvailable;
                    if (Input.GetKey(KeyCode.LeftShift) && amount > 10)
                        amount = 10;
                    if (Input.GetKey(KeyCode.LeftControl) && amount > 1)
                        amount = 1;
                    if (amount > availableStorage)
                        amount = availableStorage;

                    int maxStackSize = ItemManager.GetMaxStackSize(itemToStore);
                    if (amount > maxStackSize)
                        amount = maxStackSize;
                    ItemManager.SetItemCount(itemToStore, amount);
                    stringBuilder.Append(amount == 0
                        ? "\n" + PersistentSettings.GetString("Hopper_full")
                        : "\n" + string.Format(PersistentSettings.GetString("T_to_store_X"), itemToStore.GetDisplayString()));
                }

                bool canExtract = false;
                if (UsedCapacity > 0)
                {
                    stringBuilder.Append("\n" + PersistentSettings.GetString("Q_to_retrieve_contents"));
                    canExtract = true;
                }

                if (UIManager.IsInventoryShown())
                    UIManager.instance.Press_Q_Panel.GetComponent<FadePanel>().Alpha = 0;
                else if (canExtract)
                    UIManager.instance.Press_Q_Panel.GetComponent<FadePanel>().Alpha += 0.1f;

                stringBuilder.Append("\n" + PersistentSettings.GetString("Shift_Q_to_toggle_permissions"));

                if (NetworkManager.mbClientRunning)
                    stringBuilder.Append("\n" + string.Format(PersistentSettings.GetString("NetworkSync_X"), mrNetworkSyncTimer.ToString("F2")));

                if (UIManager.AllowInteracting)
                {
                    if (shiftDown && Input.GetButtonDown("Extract"))
                    {
                        eHopperPermissions nextPermission;
                        if (mValue == 0)
                            nextPermission = Permissions == eHopperPermissions.AddOnly ? eHopperPermissions.Locked : eHopperPermissions.AddOnly;
                        else
                        {
                            nextPermission = Permissions + 1;
                            if (nextPermission == eHopperPermissions.eNumPermissions)
                                nextPermission = eHopperPermissions.AddAndRemove;
                        }
                        SetPermissions(nextPermission, true, true);
                        UIManager.ForceNGUIUpdate = 0.1f;
                    }
                    else if (!shiftDown && Input.GetButton("Extract") && mRetakeDebounce <= 0.0 && canExtract)
                    {
                        itemToStore = null;
                        if (TrickyStorageHopperWindow.TakeItems(WorldScript.mLocalPlayer, this, null))
                        {
                            if (!WorldScript.mbIsServer)
                                NetworkManager.instance.mClientThread.mPreviousTargetEntity = null;
                            UIManager.ForceNGUIUpdate = 0.1f;
                            AudioHUDManager.instance.OrePickup();
                            mRetakeDebounce = 0.5f;
                        }
                        else
                        {
                            FloatingCombatTextManager.instance.QueueText(WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectBlockX,
                                WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectBlockY,
                                WorldScript.instance.localPlayerInstance.mPlayerBlockPicker.selectBlockZ, 1f,
                                PersistentSettings.GetString("Failed_Inventory_Full"), Color.red, 1.5f);
                        }
                    }

                    if (Input.GetButtonDown("Interact") && Input.GetKey(KeyCode.LeftShift))
                    {
                        SetVacuum(!VacuumOn, true);
                        UIManager.ForceNGUIUpdate = 0.1f;
                    }

                    if (Input.GetButtonDown("Store") && UIManager.AllowInteracting && mMaximumCapacity - UsedCapacity > 0 && itemToStore != null &&
                        TrickyStorageHopperWindow.StoreItems(WorldScript.mLocalPlayer, this, itemToStore))
                        UIManager.ForceNGUIUpdate = 0.1f;

                    if (OneTypeHopper && UIManager.HotBarShown && Input.GetButtonDown("Build Gun") && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftControl) &&
                        IsEmpty())

                    {
                        ItemBase itemBase = GetSelectedHotbarItemBase();
                        SetOneTypeItem(itemBase, true);
                        UIManager.ForceNGUIUpdate = 0.1f;
                        AudioHUDManager.instance.HUDIn();
                    }
                }

                mPopupText = stringBuilder.ToString();
                return mPopupText;
            }
            catch (Exception e)
            {
                Logging.LogException(this, e);
                return "Error - See Log";
            }
        }


        private ItemBase GetSelectedHotbarItemBase()
        {
            SurvivalHotBarManager.HotBarEntry currentHotBarEntry = !ReferenceEquals(SurvivalHotBarManager.instance, null)
                ? SurvivalHotBarManager.instance.GetCurrentHotBarEntry()
                : null;
            if (currentHotBarEntry != null && currentHotBarEntry.state != SurvivalHotBarManager.HotBarEntryState.Empty &&
                currentHotBarEntry.state != SurvivalHotBarManager.HotBarEntryState.Unavailable)
            {
                if (currentHotBarEntry.itemType >= 0)
                    return ItemManager.SpawnItem(currentHotBarEntry.itemType);
                if (currentHotBarEntry.cubeType > 0)
                    return new ItemCubeStack(currentHotBarEntry.cubeType, currentHotBarEntry.cubeValue, 1);
            }

            return null;
        }


        /// <summary>
        /// Handle CreateHolobaseEntity.
        /// </summary>
        public override HoloMachineEntity CreateHolobaseEntity(Holobase holobase)
        {
            HolobaseEntityCreationParameters parameters = new HolobaseEntityCreationParameters(this) {RequiresUpdates = true};
            parameters.AddVisualisation(holobase.mPreviewCube);
            return holobase.CreateHolobaseEntity(parameters);
        }


        /// <summary>
        /// Handle HolobaseUpdate.
        /// </summary>
        public override void HolobaseUpdate(Holobase holobase, HoloMachineEntity holoMachineEntity)
        {
            holobase.SetColour(holoMachineEntity.VisualisationObjects[0], IsFull() ? Color.white : Color.green);
        }


        /// <summary>
        /// Handle SpawnGameObject.
        /// </summary>
        public override void SpawnGameObject()
        {
            mObjectType = SpawnableObjectEnum.LogisticsHopper;
            base.SpawnGameObject();
        }


        /// <summary>
        /// Handle UnityUpdate.
        /// </summary>
        public override void UnityUpdate()
        {
            try
            {
                // If Unity initialization failed then leave.
                if (mUnityInitializationError)
                    return;

                // If not linked then initialize the game objects.
                if (!mLinkedToGameObject)
                {
                    // If no wrapper game object then leave.
                    if (mWrapper == null || !mWrapper.mbHasGameObject)
                        return;

                    try
                    {
                        Transform vacuumTransform = mWrapper.mGameObjectList[0].transform.Search("HooverGraphic");
                        if (vacuumTransform == null)
                            Logging.LogMissingUnityObject("HooverGraphic");
                        else
                        {

                            mWorkLight = vacuumTransform.GetComponent<Light>();
                            if (mWorkLight == null)
                                Logging.LogMissingUnityObject("HooverGraphic Light");

                            mVacuumParticleSystem = vacuumTransform.GetComponent<ParticleSystem>();
                            if (mVacuumParticleSystem == null)
                                Logging.LogMissingUnityObject("HooverGraphic ParticleSystem");
                            else
                                mVacuumParticleSystem.SetEmissionRate(0.0f);
                        }

                        mTextMesh = mWrapper.mGameObjectList[0].gameObject.transform.Search("Storage Text")?.GetComponent<TextMesh>();
                        if (mTextMesh == null)
                            Logging.LogMissingUnityObject("Storage Text");

                        mHopperPart = mWrapper.mGameObjectList[0].transform.Find("Hopper")?.gameObject;
                        if (mHopperPart == null)
                            Logging.LogMissingUnityObject("Hopper");
                        else
                        {
                            mHopperPart.GetComponent<MeshRenderer>().material.SetColor("_Color", mCubeColor);
                            mHoloStatus = mHopperPart.transform.Find("Holo_Status")?.gameObject;
                            if (mHoloStatus == null)
                                Logging.LogMissingUnityObject("Holo_Status");
                            else
                                mHoloStatus.SetActive(false);
                        }

                        mPreviousPermissions = eHopperPermissions.eNumPermissions;
                        SetHoloStatus();
                        mForceTextUpdate = true;
                        mLinkedToGameObject = true;
                    }
                    catch (Exception e)
                    {
                        Logging.LogException(this, e, "Performing Initialization");
                        mUnityInitializationError = true;
                    }
                }
                else
                {
                    if (!mbWellBehindPlayer && !mSegment.mbOutOfView)
                    {
                        if (mDistanceToPlayer <= 8.0 && mPreviousDistanceToPlayer > 8.0)
                            mForceHoloUpdate = true;
                        if (mDistanceToPlayer > 8.0 && mPreviousDistanceToPlayer <= 8.0)
                            mForceHoloUpdate = true;
                        if (mForceHoloUpdate)
                            SetHoloStatus();
                        if (mForceTextUpdate)
                            UpdateMeshText();
                    }

                    UpdateLevelOfDetail();
                    UpdateWorkLight();
                }
            }
            catch (Exception e)
            {
                Logging.LogException(this, e);
            }
        }


        /// <summary>
        /// Handle DropGameObject.
        /// </summary>
        public override void DropGameObject()
        {
            base.DropGameObject();
            mLinkedToGameObject = false;
        }


        /// <summary>
        /// Handle UnitySuspended.
        /// </summary>
        public override void UnitySuspended()
        {
            mWorkLight = null;
            mVacuumParticleSystem = null;
            mTextMesh = null;
            mHopperPart = null;
        }


        /// <summary>
        /// Set the holobase state.
        /// </summary>
        private void SetHoloStatus()
        {
            if (mHoloStatus == null)
                return;
            if (mForceHoloUpdate)
                mPreviousPermissions = eHopperPermissions.eNumPermissions;
            mForceHoloUpdate = false;
            if (mDistanceToPlayer > 8.0 || mbWellBehindPlayer)
            {
                if (!mHoloStatus.activeSelf)
                    return;
                mHoloStatus.SetActive(false);
            }
            else
            {
                if (!mHoloStatus.activeSelf)
                {
                    mPreviousPermissions = eHopperPermissions.eNumPermissions;
                    mHoloStatus.SetActive(true);
                }

                if (mPreviousPermissions == Permissions)
                    return;
                mPreviousPermissions = Permissions;
                if (Permissions == eHopperPermissions.AddAndRemove)
                    mHoloStatus.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(0.0f, 0.5f);
                if (Permissions == eHopperPermissions.RemoveOnly)
                    mHoloStatus.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(0.0f, 0.0f);
                if (Permissions == eHopperPermissions.Locked)
                    mHoloStatus.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(0.5f, 0.0f);
                if (Permissions != eHopperPermissions.AddOnly)
                    return;
                mHoloStatus.GetComponent<Renderer>().material.mainTextureOffset = new Vector2(0.5f, 0.5f);
            }
        }


        /// <summary>
        /// Update the hopper text mesh.
        /// </summary>
        private void UpdateMeshText()
        {
            if (!mTextMesh.GetComponent<Renderer>().enabled || mDistanceToPlayer >= 12.0)
                return;

            string storageStateText;
            if (UsedCapacity == 0)
                storageStateText = PersistentSettings.GetString("Storage_Empty");
            else if (UsedCapacity >= mMaximumCapacity)
                storageStateText = PersistentSettings.GetString("Storage_full");
            else
                storageStateText = string.Format(PersistentSettings.GetString("X_free_slots"), Math.Max(0, mMaximumCapacity - UsedCapacity));

            mTextMesh.text = GetHopperPermissionsString(Permissions) + "\n" + storageStateText + "\n" +
                             (string.IsNullOrEmpty(mLastItemAddedText) ? string.Empty : "[" + mLastItemAdded + "]");
            mForceTextUpdate = false;
        }


        /// <summary>
        /// Update the level of detail.
        /// </summary>
        private void UpdateLevelOfDetail()
        {
            bool showHopper = !(mDistanceToPlayer > 64.0 || mbWellBehindPlayer || Mathf.Abs(mVectorToPlayer.y) > 32.0 ||
                                (mSegment != null && mSegment.mbOutOfView));

            if (showHopper != mShowHopper)
            {
                mShowHopper = showHopper;
                mForceHoloUpdate = true;
                if (showHopper)
                {
                    if (!mHopperPart.activeSelf)
                    {
                        mHopperPart.SetActive(true);
                        mHopperPart.GetComponent<Renderer>().enabled = true;
                    }
                }
                else
                {
                    if (mHopperPart.activeSelf)
                        mHopperPart.SetActive(false);
                    mHopperPart.GetComponent<Renderer>().enabled = false;
                }
            }

            UpdateVacuumEmission();

            if (mDistanceToPlayer > 24.0 || mbWellBehindPlayer || mDistanceToPlayer > CamDetail.SegmentDrawDistance - 8.0)
            {
                if (!mTextMesh.GetComponent<Renderer>().enabled)
                    return;
                mForceHoloUpdate = true;
                mTextMesh.GetComponent<Renderer>().enabled = false;
            }
            else
            {
                if (mTextMesh.GetComponent<Renderer>().enabled)
                    return;
                mForceHoloUpdate = true;
                mTextMesh.GetComponent<Renderer>().enabled = true;
            }
        }


        /// <summary>
        /// Update the vacuum emission state.
        /// </summary>
        private void UpdateVacuumEmission()
        {
            bool visibleToPlayer = !(mDistanceToPlayer > 16.0 || mbWellBehindPlayer || mDistanceToPlayer > (double) CamDetail.SegmentDrawDistance ||
                                     Mathf.Abs(mVectorToPlayer.y) > 24.0);

            if (!visibleToPlayer)
            {
                if (mVacuumEmissionRate <= 0)
                    return;
                --mVacuumEmissionRate;
                if (!Equals(mVacuumParticleSystem, null))
                    mVacuumParticleSystem.SetEmissionRate(mVacuumEmissionRate);
            }
            else if (VacuumOn)
            {
                if (mVacuumEmissionRate > 10)
                    return;
                ++mVacuumEmissionRate;
                if (!Equals(mVacuumParticleSystem, null))
                    mVacuumParticleSystem.SetEmissionRate(mVacuumEmissionRate);
            }
            else
            {
                if (mVacuumEmissionRate <= 0)
                    return;
                --mVacuumEmissionRate;
                if (!Equals(mVacuumParticleSystem, null))
                    mVacuumParticleSystem.SetEmissionRate(mVacuumEmissionRate);
            }
        }


        /// <summary>
        /// Update the work light state.
        /// </summary>
        private void UpdateWorkLight()
        {
            if (mWorkLight == null)
                return;

            bool updateWorkLight = (UsedCapacity == 0 || UsedCapacity >= mMaximumCapacity) && !mbWellBehindPlayer;

            mMaxLightDistance += (float) ((CamDetail.FPS - (double) mMaxLightDistance) *
                                          Time.deltaTime * 0.100000001490116);

            if (mMaxLightDistance < 2.0)
                mMaxLightDistance = 2f;
            if (mMaxLightDistance > 64.0)
                mMaxLightDistance = 64f;
            if (mDistanceToPlayer > mMaxLightDistance)
                updateWorkLight = false;

            if (updateWorkLight)
            {
                if (!mWorkLight.enabled)
                {
                    mWorkLight.enabled = true;
                    mWorkLight.range = 0.05f;
                }

                if (UsedCapacity == 0)
                {
                    mWorkLight.color = Color.Lerp(mWorkLight.color, Color.green, Time.deltaTime);
                    mWorkLight.range += 0.1f;
                }
                else if (UsedCapacity >= mMaximumCapacity)
                {
                    mWorkLight.color = Color.Lerp(mWorkLight.color, Color.red, Time.deltaTime);
                    mWorkLight.range += 0.1f;
                }
                else if (UsedCapacity >= mMaximumCapacity)
                {
                    mWorkLight.color = Color.Lerp(mWorkLight.color, Color.yellow, Time.deltaTime);
                    mWorkLight.range += 0.1f;
                }
                else
                {
                    mWorkLight.color = Color.Lerp(mWorkLight.color, Color.cyan, Time.deltaTime);
                    mWorkLight.range -= 0.1f;
                }

                if (mWorkLight.range > 1.0)
                    mWorkLight.range = 1f;
            }

            if (!mWorkLight.enabled)
                return;

            if (mWorkLight.range < 0.150000005960464)
                mWorkLight.enabled = false;
            else
                mWorkLight.range *= 0.95f;
        }


        /// <summary>
        /// Gets the display hopper permissions string for the specified hopper permission. 
        /// </summary>
        /// <param name="permissions">Hopper permissions.</param>
        /// <returns>Displayed hopper permissions string.</returns>
        public static string GetHopperPermissionsString(eHopperPermissions permissions)
        {
            switch (permissions)
            {
                case eHopperPermissions.AddAndRemove:
                    return PersistentSettings.GetString("Add_Remove");
                case eHopperPermissions.AddOnly:
                    return PersistentSettings.GetString("Add_Only");
                case eHopperPermissions.RemoveOnly:
                    return PersistentSettings.GetString("Remove_Only");
                case eHopperPermissions.Locked:
                    return PersistentSettings.GetString("Locked");
                default:
                    return permissions.ToString();
            }
        }


        /// <summary>
        /// Checks if the specified storage id is allowed to be stored in this hopper based on the one-type hopper flags. If the hopper is
        /// one-type and storage Id is not set then use current storage id will become the one-type hopper item.
        /// </summary>
        /// <param name="storageId">Storage id to check against</param>
        /// <returns>True if allowed, otherwise fail.</returns>
        public bool CheckItemAllowed(uint storageId)
        {
            if (!OneTypeHopper)
                return true;

            if (mOneTypeHopperStorageId == 0)
            {
                mOneTypeHopperStorageId = storageId;
                return true;
            }

            return storageId == mOneTypeHopperStorageId;
        }


        /// <summary>
        /// Gets the entire inventory as an ItemBase list.
        /// </summary>
        /// <returns>ItemBase list of all inventory.</returns>
        public List<ItemBase> GetInventory()
        {
            List<ItemBase> itemBase = new List<ItemBase>();
            lock (mInventory)
            {
                foreach (InventoryStack inventoryStack in mInventory.Values)
                {
                    if (inventoryStack.Count == 0)
                        continue;
                    if (inventoryStack.IsStackItem)
                        itemBase.Add(inventoryStack.Item);
                    else
                    {
                        inventoryStack.IterateContents((item, state) =>
                        {
                            itemBase.Add(item);
                            return true;
                        }, null);
                    }
                }
            }

            return itemBase;
        }

    }
}