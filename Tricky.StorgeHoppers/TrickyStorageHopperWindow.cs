using MadVandal.FortressCraft;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tricky.ExtraStorageHoppers
{
    public class TrickyStorageHopperWindow : BaseMachineWindow
    {    
        
        /// <summary>
        /// Machine panel states.
        /// </summary>
        public enum MachinePanelState
        {
            Closed,
            OpenNormal,
            OpenInventory
        }


        public const string COMMAND_SET_PERMISSIONS = "SetPermissions";
        public const string COMMAND_SET_VACUUM = "SetVacuum";
        public const string COMMAND_SET_CONTENT_SHARING = "SetContentSharing";
        public const string COMMAND_SET_HIVEMIND_FEEDING = "SetHivemindFeeding";
        public const string COMMAND_SET_ONE_TYPE_STORAGE_ID = "SetOneTypeStorageId";
        public const string COMMAND_TAKE_ITEMS = "TakeItems";
        public const string COMMAND_STORE_ITEMS = "StoreItems";

        // Label and slot constants.
        public const string DROP_ITEM_SLOT = "DropItemSlot";
        public const string ITEM_SLOT = "ItemSlot";
        public const string LABEL_STACK_SIZE = "StackSize";
        public const string LABEL_USED_STORAGE = "UsedStorage";
        public const string LABEL_VACUUM_STATE = "VacuumState";
        public const string LABEL_CONTENT_SHARING_STATE = "ContentSharingState";
        public const string LABEL_HIVEMIND_FEEDING_STATE = "HivemindFeedingState";

        // Button constants.
        public const string BUTTON_ADD_REMOVE = "AddRemove";
        public const string BUTTON_ADD_ONLY = "AddOnly";
        public const string BUTTON_REMOVE_ONLY = "RemoveOnly";
        public const string BUTTON_LOCKED = "Locked";
        public const string BUTTON_TOGGLE_VACUUM = "Vacuum";
        public const string BUTTON_TOGGLE_CONTENT_SHARING= "ContentSharing";
        public const string BUTTON_TOGGLE_HIVEMIND_FEEDING = "HivemindFeeding";

        private static bool mDirty;
        private static bool mNetworkRedraw;

        private int mSlotCount;
        private List<ItemBase> mDisplayedItemBaseList = new List<ItemBase>();

        /// <summary>
        /// Current machine panel state.
        /// </summary>
        protected MachinePanelState mMachinePanelState = MachinePanelState.Closed;


        public override void SpawnWindow(SegmentEntity targetEntity)
        {
            TrickyStorageHopper hopper = targetEntity as TrickyStorageHopper;
            if (hopper == null)
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

            // Set the new machine panel state.
            mMachinePanelState = !UIManager.IsInventoryShown() ? MachinePanelState.OpenNormal : MachinePanelState.OpenInventory;

            if (mMachinePanelState == MachinePanelState.OpenNormal)
                GenericMachinePanelHelper.SetPanelSizeAndPosition(425, 568);
            else
                GenericMachinePanelHelper.SetPanelSizeAndPosition(425, 568, 484 + 30, 232);

            float x = GenericMachinePanelScript.instance.Label_Holder.transform.position.x;
            float y = GenericMachinePanelScript.instance.Label_Holder.transform.position.y;
            GenericMachinePanelScript.instance.Label_Holder.transform.position = new Vector3(x, y, 69.3f);
            manager.SetTitle(hopper.HopperName ?? "UNKNOWN TYPE");

            int yPosition = 0;

            if (hopper.mValue == 0)
            {
                manager.AddTabButton(BUTTON_ADD_ONLY, "Add Only", true, 30, yPosition);
                manager.AddTabButton(BUTTON_LOCKED, "Locked", true, 180, yPosition);
            }
            else
            {
                manager.AddTabButton(BUTTON_ADD_REMOVE, "Add + Remove", true, 30, yPosition);
                manager.AddTabButton(BUTTON_LOCKED, "Locked", true, 180, yPosition);
                yPosition += 50;
                manager.AddTabButton(BUTTON_ADD_ONLY, "Add Only", true, 30, yPosition);
                manager.AddTabButton(BUTTON_REMOVE_ONLY, "Remove Only", true, 180, yPosition);
            }


            yPosition+=50;
            manager.AddButton(BUTTON_TOGGLE_VACUUM, "Toggle", 0, yPosition);
            manager.AddBigLabel(LABEL_VACUUM_STATE, string.Empty, Color.white, 130, yPosition);

            yPosition += 50;
            if (hopper.mValue == 0)
            {
                manager.AddButton(BUTTON_TOGGLE_HIVEMIND_FEEDING, "Toggle", 0, yPosition);
                manager.AddBigLabel(LABEL_HIVEMIND_FEEDING_STATE, string.Empty, Color.white, 125, yPosition);
            }
            else
            {
                manager.AddButton(BUTTON_TOGGLE_CONTENT_SHARING, "Toggle", 0, yPosition);
                manager.AddBigLabel(LABEL_CONTENT_SHARING_STATE, string.Empty, Color.white, 125, yPosition);

            }


            yPosition += 50;
            manager.AddIcon(DROP_ITEM_SLOT, "empty", Color.white, 10, yPosition);
            manager.AddBigLabel("DropSlotLabel", "Drop Here To "+ (hopper.mValue==0 ? "Destroy": "Store"), Color.white, 70, yPosition+5);

            if (hopper.mValue != 0)
            {
                yPosition += 65;
                manager.AddBigLabel(LABEL_USED_STORAGE, "8888/8888", Color.white, 10, yPosition);

                yPosition += 45;
                mDisplayedItemBaseList = hopper.GetInventory();
                mSlotCount = hopper.mValue == 0 ? 0 : mDisplayedItemBaseList.Count;
                int columnSize = 60;
                for (int slotIndex = 0; slotIndex < mSlotCount; slotIndex++)
                {
                    int row = slotIndex / 5;
                    int column = slotIndex % 5;
                    manager.AddIcon(ITEM_SLOT + slotIndex, "empty", Color.white, column * columnSize + 10, row * 60 + yPosition);
                    manager.AddLabel(GenericMachineManager.LabelType.OneLineHalfWidth, LABEL_STACK_SIZE + slotIndex, string.Empty, Color.white, false, column * columnSize + 33,
                        row * 60 + yPosition + 22);
                }
            }

            GenericMachinePanelHelper.ResetScroll();
            mDirty = true;
        }


        public override void OnClose(SegmentEntity targetEntity)
        {
            base.OnClose(targetEntity);
            GenericMachinePanelHelper.RestoreOriginalWindowState();
        }


        public override void UpdateMachine(SegmentEntity targetEntity)
        {
            TrickyStorageHopper hopper = targetEntity as TrickyStorageHopper;
            if (hopper == null)
            {
                GenericMachinePanelScript.instance.Hide();
                UIManager.RemoveUIRules("Machine");
                return;
            }

            // If inventory is now open and panel state has not changed then update the panel.
            if (mMachinePanelState != MachinePanelState.OpenNormal && !UIManager.IsInventoryShown())
            {
                mNetworkRedraw = false;
                mMachinePanelState = MachinePanelState.OpenNormal;
                Redraw(targetEntity);
            }

            // If inventory is now closed and panel state has not changed then update the panel.
            if (mMachinePanelState != MachinePanelState.OpenInventory && UIManager.IsInventoryShown())
            {
                mNetworkRedraw = false;
                Redraw(targetEntity);
                mMachinePanelState = MachinePanelState.OpenInventory;
            }
            
            if (mNetworkRedraw)
            {
                Redraw(targetEntity);
                mNetworkRedraw = false;
            }

            if (targetEntity.mbNetworkUpdated)
            {
                mDirty = true;
                targetEntity.mbNetworkUpdated = false;
            }

            if (!mDirty)
                return;

            if (hopper.mValue > 0)
            {
                manager.UpdateTabButton(BUTTON_ADD_REMOVE, hopper.Permissions != eHopperPermissions.AddAndRemove);
                manager.UpdateTabButton(BUTTON_REMOVE_ONLY, hopper.Permissions != eHopperPermissions.RemoveOnly);
            }

            manager.UpdateTabButton(BUTTON_ADD_ONLY, hopper.Permissions != eHopperPermissions.AddOnly);
            manager.UpdateTabButton(BUTTON_LOCKED, hopper.Permissions != eHopperPermissions.Locked);

            manager.UpdateLabel(LABEL_VACUUM_STATE, "Vacuum: "+ (hopper.VacuumOn ? "On" : "Off"), Color.white);

            if (hopper.mValue == 0)
                manager.UpdateLabel(LABEL_HIVEMIND_FEEDING_STATE, "Hivemind Feeding: " + (hopper.HivemindFeedingOn ? "On" : "Off"), Color.white);
            else
            {
                manager.UpdateLabel(LABEL_CONTENT_SHARING_STATE, "Content Sharing: " + (hopper.ContentSharingOn ? "On" : "Off"), Color.white);
                manager.UpdateLabel(LABEL_USED_STORAGE, string.Concat("Used ", hopper.UsedCapacity, "/", hopper.TotalCapacity), Color.white);
            }
            
            // Leave first slot as the empty to drop new stuff.
            int slotIndex = 0;
            List<ItemBase> itemBaseList = hopper.GetInventory();
            for(int index=0; index < mSlotCount; index++)
            {
                ItemBase itemBase = index < itemBaseList.Count ? itemBaseList[index] : null;
                int currentStackSize = itemBase != null ? ItemManager.GetCurrentStackSize(itemBase) : 0;

                string itemIcon = ItemManager.GetItemIcon(itemBase);
                manager.UpdateIcon(ITEM_SLOT + slotIndex, itemIcon, Color.white);
                manager.UpdateLabel(LABEL_STACK_SIZE + slotIndex, currentStackSize.ToString("###").PadLeft(3), Color.white);
                slotIndex++;
            }

            mDirty = false;
        }


        public override bool ButtonClicked(string name, SegmentEntity targetEntity)
        {
            if (!(targetEntity is TrickyStorageHopper hopper))
                return false;

            if (name == BUTTON_ADD_REMOVE)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                hopper.SetPermissions(eHopperPermissions.AddAndRemove, true, true);
                mDirty = true;
                return true;
            }

            if (name == BUTTON_ADD_ONLY)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                hopper.SetPermissions(eHopperPermissions.AddOnly, true, true);
                mDirty = true;
                return true;
            }

            if (name == BUTTON_REMOVE_ONLY)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                hopper.SetPermissions(eHopperPermissions.RemoveOnly, true,true);
                mDirty = true;
                return true;
            }

            if (name == BUTTON_LOCKED)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                hopper.SetPermissions(eHopperPermissions.Locked, true, true);
                mDirty = true;
                return true;
            }

            if (name == BUTTON_TOGGLE_VACUUM)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.HUDIn();
                hopper.SetVacuum(!hopper.VacuumOn, true);
                mDirty = true;
                return true;
            }

            if (name == BUTTON_TOGGLE_CONTENT_SHARING)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.HUDIn();
                hopper.SetContentSharing(!hopper.ContentSharingOn, true);
                mDirty = true;
                return true;
            }

            if (name == BUTTON_TOGGLE_HIVEMIND_FEEDING)
            {
                UIManager.ForceNGUIUpdate = 0.1f;
                AudioHUDManager.instance.HUDIn();
                hopper.SetHivemindFeeding(!hopper.HivemindFeedingOn, true);
                mDirty = true;
                return true;
            }

            if (name.Contains(ITEM_SLOT))
            {
                int.TryParse(name.Replace(ITEM_SLOT, string.Empty), out int result);
                if (result > -1 && result < mDisplayedItemBaseList.Count)
                {
                    ItemBase itemBase = mDisplayedItemBaseList[result];
                    
                    if (itemBase.mType == ItemType.ItemCubeStack || itemBase.mType == ItemType.ItemStack)
                    {
                        itemBase = ItemManager.CloneItem(itemBase);
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                            itemBase.SetAmount(10);
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            itemBase.SetAmount(1);
                    }

                    TakeItems(WorldScript.mLocalPlayer, hopper, itemBase);
                    UIManager.ForceNGUIUpdate = 0.1f;
                    AudioHUDManager.instance.OrePickup();
                    hopper.SetRetakeDebounce();
                    mDirty = true;
                    return true;
                }
            }
      
            return false;
        }



        public override void HandleItemDrag(string name, ItemBase draggedItem, DragAndDropManager.DragRemoveItem dragDelegate, SegmentEntity targetEntity)
        {
            if (!(targetEntity is TrickyStorageHopper hopper) || draggedItem==null)
                return;

            ItemBase itemForSlot = null;
            if (name != DROP_ITEM_SLOT)
            {
                itemForSlot = GetItemForSlot(name);
                if (itemForSlot != null && draggedItem.mnItemID != itemForSlot.mnItemID)
                    return;
            }

            if (!hopper.CheckItemAllowed(draggedItem.ToStorageId()) || hopper.IsFull())
                return;

            ItemBase itemBase = ItemManager.CloneItem(draggedItem);
            int currentStackSize = ItemManager.GetCurrentStackSize(itemBase);
            if (hopper.RemainingCapacity < currentStackSize)
                ItemManager.SetItemCount(itemBase, hopper.RemainingCapacity);

            StoreItems(WorldScript.mLocalPlayer, hopper, itemBase);
            InventoryPanelScript.mbDirty = true;
            SurvivalHotBarManager.MarkAsDirty();
            SurvivalHotBarManager.MarkContentDirty();
            mNetworkRedraw = true;
        }



        public override void ButtonEnter(string name, SegmentEntity targetEntity)
        {
            if (!(targetEntity is TrickyStorageHopper))
                return;

            ItemBase itemForSlot = GetItemForSlot(name);
            if (itemForSlot == null)
                return;

            if (HotBarManager.mbInited)
                HotBarManager.SetCurrentBlockLabel(ItemManager.GetItemName(itemForSlot));
            else
            {
                if (!SurvivalHotBarManager.mbInited)
                    return;

                string text = WorldScript.mLocalPlayer.mResearch.IsKnown(itemForSlot) ? ItemManager.GetItemName(itemForSlot) : "Unknown Material";
                int currentStackSize = ItemManager.GetCurrentStackSize(itemForSlot);
                SurvivalHotBarManager.SetCurrentBlockLabel(currentStackSize > 1 ? $"{currentStackSize} {text}" : text);
            }
        }


        private ItemBase GetItemForSlot(string name)
        {
            ItemBase result = null;
            int.TryParse(name.Replace(ITEM_SLOT, string.Empty), out int num);
            if (num > -1 && num < mDisplayedItemBaseList.Count)
                result = mDisplayedItemBaseList[num];
            return result;
        }



        /// <summary>
        /// Takes the specified item from the hopper to player inventory.
        /// </summary>
        /// <param name="player">Player making the change.</param>
        /// <param name="hopper">Hopper instance.</param>
        /// <param name="item">Item to take.</param>
        /// <returns>True if item was taken successfully, otherwise false.</returns>
        public static bool TakeItems(Player player, TrickyStorageHopper hopper, ItemBase item)
        {
            if (!player.mbIsLocalPlayer)
                mNetworkRedraw = true;

            if (hopper.IsEmpty())
                return false;

            ItemBase itemBase;
            if (item == null)
            {
                if (!hopper.GetNextRemoveInventorySlot(out ItemType itemType, out int itemId, out ushort cubeType, out ushort cubeValue, out int amount))
                    return false;

                Logging.LogMessage("GetNextRemoveInventorySlot - ItemType: " + itemType + " ItemId: " + itemId + " Cube: " + cubeType + " Value: " + cubeValue + " Amount: " + amount, 2);
                if (amount == 0)
                    return false;

                if (!hopper.TryExtract(eHopperRequestType.eAny, itemId, cubeType, cubeValue, false, 1, amount, false, false, false, true, out itemBase, out ushort _,
                    out ushort _, out int _) || itemBase==null)
                    return false;

                if (!player.mInventory.AddItem(itemBase))
                {
                    hopper.AddItem(itemBase);
                    return false;
                }
            }
            else
            {
                itemBase = ItemManager.CloneItem(item);
                if (itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = (ItemCubeStack) itemBase;
                    if (!hopper.TryExtractCubes(hopper, itemCubeStack.mCubeType, itemCubeStack.mCubeValue, itemCubeStack.mnAmount))
                        return false;

                    if (!player.mInventory.AddItem(itemBase))
                    {
                        hopper.AddItem(itemBase);
                        return false;
                    }

                }
                else
                {
                    if (!hopper.TryExtractItems(hopper, itemBase.mnItemID, itemBase.GetAmount()))
                        return false;

                    if (!player.mInventory.AddItem(itemBase))
                    {
                        hopper.AddItem(itemBase);
                        return false;
                    }

                }
            }

            if (player.mbIsLocalPlayer)
            {
                Color lCol = Color.green;
                if (itemBase.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = (ItemCubeStack)itemBase;
                    if (CubeHelper.IsGarbage(itemCubeStack.mCubeType))
                        lCol = Color.red;
                    if (CubeHelper.IsSmeltableOre(itemCubeStack.mCubeType))
                        lCol = Color.green;
                }

                if (itemBase.mType == ItemType.ItemStack)
                    lCol = Color.cyan;
                if (itemBase.mType == ItemType.ItemSingle)
                    lCol = Color.white;
                if (itemBase.mType == ItemType.ItemCharge)
                    lCol = Color.magenta;
                if (itemBase.mType == ItemType.ItemDurability)
                    lCol = Color.yellow;
                if (itemBase.mType == ItemType.ItemLocation)
                    lCol = Color.gray;
                FloatingCombatTextManager.instance.QueueText(hopper.mnX, hopper.mnY + 1L, hopper.mnZ, 1f, player.GetItemName(itemBase), lCol, 1.5f);
            }

            player.mInventory.VerifySuitUpgrades();
            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(nameof(TrickyStorageHopperWindow), COMMAND_TAKE_ITEMS, null, itemBase, hopper, 0.0f);

            mNetworkRedraw = true;
            mDirty = true;
            return true;
        }


        /// <summary>
        /// Stores the specified item from the player inventory to the hopper.
        /// </summary>
        /// <param name="player">Player making the change.</param>
        /// <param name="hopper">Hopper instance.</param>
        /// <param name="itemToStore">Item to store.</param>
        /// <returns>True if item was stored successfully, otherwise false.</returns>
        public static bool StoreItems(Player player, TrickyStorageHopper hopper, ItemBase itemToStore)
        {
            if (!hopper.CheckItemAllowed(itemToStore.ToStorageId()))
            {
                if (player.mbIsLocalPlayer)
                {
                    FloatingCombatTextManager.instance.QueueText(hopper.mnX, hopper.mnY + 1L, hopper.mnZ, 0.75f, "Item does not match hopper one-type",
                        Color.red, 1.5f);
                    AudioHUDManager.instance.HUDFail();
                }

                return false;
            }

            if (player == WorldScript.mLocalPlayer && !WorldScript.mLocalPlayer.mInventory.RemoveItemByExample(itemToStore, true))
            {
                Logging.LogMessage("Player " + player.mUserName + " doesn't have " + itemToStore);
                return false;
            }

            if (!hopper.AddItem(itemToStore))
            {
                if (player == WorldScript.mLocalPlayer)
                {
                    WorldScript.mLocalPlayer.mInventory.AddItem(itemToStore);
                    return false;
                }

                player.mInventory.AddItem(itemToStore);
                return false;
            }

            if (player.mbIsLocalPlayer)
            {
                Color lCol = Color.green;
                ItemBase lItem = itemToStore;
                if (lItem.mType == ItemType.ItemCubeStack)
                {
                    ItemCubeStack itemCubeStack = (ItemCubeStack)lItem;
                    if (CubeHelper.IsGarbage(itemCubeStack.mCubeType))
                        lCol = Color.red;
                    if (CubeHelper.IsSmeltableOre(itemCubeStack.mCubeType))
                        lCol = Color.green;
                }

                if (lItem.mType == ItemType.ItemStack)
                    lCol = Color.cyan;
                if (lItem.mType == ItemType.ItemSingle)
                    lCol = Color.white;
                if (lItem.mType == ItemType.ItemCharge)
                    lCol = Color.magenta;
                if (lItem.mType == ItemType.ItemDurability)
                    lCol = Color.yellow;
                if (lItem.mType == ItemType.ItemLocation)
                    lCol = Color.gray;
                FloatingCombatTextManager.instance.QueueText(hopper.mnX, hopper.mnY + 1L, hopper.mnZ, 0.75f,
                    string.Format(PersistentSettings.GetString("Stored_X"), player.GetItemName(lItem)), lCol, 1.5f);
            }
            else mNetworkRedraw = true;

            player.mInventory.VerifySuitUpgrades();
            SurvivalHotBarManager.MarkAsDirty();
            SurvivalHotBarManager.MarkContentDirty();
            if (!WorldScript.mbIsServer)
                NetworkManager.instance.SendInterfaceCommand(nameof(TrickyStorageHopperWindow), nameof(StoreItems), null, itemToStore, hopper, 0.0f);
            return true;
        }


        /// <summary>
        /// Handle HandleNetworkCommand.
        /// </summary>
        public static NetworkInterfaceResponse HandleNetworkCommand(Player player, NetworkInterfaceCommand networkInterfaceCommand)
        {
            if (!(networkInterfaceCommand.target is TrickyStorageHopper hopper))
                return new NetworkInterfaceResponse
                {
                    entity = null,
                    inventory = player.mInventory
                };

            try
            {
                string str = networkInterfaceCommand.command;
                int value;
                switch (str)
                {
                    case COMMAND_SET_PERMISSIONS:
                        if (int.TryParse(networkInterfaceCommand.payload, out value))
                            hopper.SetPermissions((eHopperPermissions)value, false, true);
                        break;
                    case COMMAND_SET_VACUUM:
                        if (int.TryParse(networkInterfaceCommand.payload, out value))
                            hopper.SetVacuum(value == 1, false);
                        break;
                    case COMMAND_SET_CONTENT_SHARING:
                        if (int.TryParse(networkInterfaceCommand.payload, out value))
                            hopper.SetContentSharing(value == 1, false);
                        break;
                    case COMMAND_SET_HIVEMIND_FEEDING:
                        if (int.TryParse(networkInterfaceCommand.payload, out value))
                            hopper.SetHivemindFeeding(value == 1, false);
                        break;
                    case COMMAND_SET_ONE_TYPE_STORAGE_ID:
                        if (uint.TryParse(networkInterfaceCommand.payload, out uint storageId))
                            hopper.SetOneTypeItem(storageId, false);
                        break;
                    case COMMAND_TAKE_ITEMS:
                        TakeItems(player, hopper, networkInterfaceCommand.itemContext);
                        break;
                    case COMMAND_STORE_ITEMS:
                        StoreItems(player, hopper, networkInterfaceCommand.itemContext);
                        break;
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }

            return new NetworkInterfaceResponse
            {
                entity = hopper,
                inventory = player.mInventory
            };
        }



        public static void SetDirty(bool redraw)
        {
            if (redraw)
                mNetworkRedraw = true;
            mDirty = true;
        }

        
    }
}