using MadVandal.FortressCraft;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Tricky.ExtraStorageHoppers
{
    public class TrickStorageHopperMod : FortressCraftMod
    {
        /// <summary>
        /// Mod key.
        /// </summary>
        public const string MOD_KEY = "Tricky.ExtraStorageHoppers";

        /// <summary>
        /// Cube key.
        /// </summary>
        public const string CUBE_KEY = "Tricky.ExtraStorageHoppers";

        /// <summary>
        /// Gets the hopper cube type.
        /// </summary>
        public static ushort HopperCubeType { get; private set; }

        /// <summary>
        /// Hopper type data by cube value.
        /// </summary>
        private static readonly Dictionary<ushort, HopperTypeData> mHopperTypeDataByCubeValue = new Dictionary<ushort, HopperTypeData>();


        //******************** Backup TerrainData and ManufacturerRecipes files ********************
        //For people with custom hoppers
        public void BackupFiles()
        {
            try
            {
/*                string xmlDatPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "Xml";
                string backupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar+ "Backup";
                string folderName = DateTime.Today.Month + "-" + DateTime.Today.Day + "\\";
                string backupTrueFolder = backupPath + folderName;
                if (!Directory.Exists(backupTrueFolder))
                {
                    Directory.CreateDirectory(backupPath);
                    File.Copy(xmlDatPath + "TerrainData.xml", backupTrueFolder + "TerrainData.xml");
                    File.Copy(xmlDatPath + "ManufacturerRecipes.xml", backupTrueFolder + "ManufacturerRecipes.xml");
                }*/
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
        }
        

        /// <summary>
        /// Handle Register.
        /// </summary>
        public override ModRegistrationData Register()
        {
            Logging.ModName = "Tricky Storage Hoppers";

            try
            {
                BackupFiles();

                //Registers my mod, so FC knows what to load
                ModRegistrationData modRegistrationData = new ModRegistrationData();
                modRegistrationData.RegisterEntityHandler(MOD_KEY);

                modRegistrationData.RegisterEntityUI(MOD_KEY, new TrickyStorageHopperWindow());
                UIManager.NetworkCommandFunctions.Add(nameof(TrickyStorageHopperWindow), TrickyStorageHopperWindow.HandleNetworkCommand);

                TerrainData.GetCubeByKey(CUBE_KEY, out var cubeEntry, out _);
                if (cubeEntry == null)
                {
                    Logging.LogMissingCubeKey(CUBE_KEY);
                    return modRegistrationData;
                }

                HopperCubeType = cubeEntry.CubeType;

                if (cubeEntry.Values == null)
                {
                    Logging.LogError("Cube value entries not found");
                    return modRegistrationData;
                }

                foreach (TerrainDataValueEntry dataValueEntry in cubeEntry.Values)
                {
                    // Skip vod hopper.
                    if (dataValueEntry.Value == 0)
                        continue;

                    if (!ushort.TryParse(dataValueEntry.Custom?.GetValue("Tricky.MaxStorage"), out ushort capacity))
                    {
                        capacity = 10;
                        Logging.LogError("Cannot parse 'Tricky.MaxStorage' value on " + dataValueEntry.Key + " (" + dataValueEntry.Value + ")", null, false,
                            "Using default of 10");
                    }

                    if (capacity > 30000)
                    {
                        capacity = 30000;
                        Logging.LogError("'Tricky.MaxStorage' value on " + dataValueEntry.Key + " exceeds limit of 30000", null, false,
                            "Maximum capacity of 30000 used");
                    }

                    if (!float.TryParse(dataValueEntry.Custom?.GetValue("Tricky.ColorR"), out float red))
                    {
                        red = 1;
                        Logging.LogError("Cannot parse 'Tricky.ColorR' value on " + dataValueEntry.Key, null, false, "Using default of 1");
                    }

                    if (!float.TryParse(dataValueEntry.Custom?.GetValue("Tricky.ColorG"), out float green))
                    {
                        green = 1;
                        Logging.LogError("Cannot parse 'Tricky.ColorG' value on " + dataValueEntry.Key, null, false, "Using default of 1");
                    }

                    if (!float.TryParse(dataValueEntry.Custom?.GetValue("Tricky.ColorB"), out float blue))
                    {
                        blue = 1;
                        Logging.LogError("Cannot parse 'Tricky.ColorB' value on " + dataValueEntry.Key, null, false, "Using default of 1");
                    }

                    if (!bool.TryParse(dataValueEntry.Custom?.GetValue("Tricky.OT"), out bool isOneType))
                    {
                        isOneType = false;
                        Logging.LogError("Cannot parse 'Tricky.OT' value on " + dataValueEntry.Key, null, false, "Using default of false");
                    }

                    mHopperTypeDataByCubeValue[dataValueEntry.Value] =
                        new HopperTypeData(dataValueEntry.Name, capacity, new Color(red, green, blue), isOneType);

                    // Set hopper description.
                    dataValueEntry.Description = dataValueEntry.Description.Replace("{0}",
                        "up to " + capacity + " " + (isOneType ? "of one type" : (capacity > 1 ? "resources" : "resource")));
                }

                return modRegistrationData;
            }
            catch (Exception e)
            {
                Logging.LogException(e);
                return new ModRegistrationData();
            }

        }


        /// <summary>
        /// Handle CreateSegmentEntity.
        /// </summary>
        public override void CreateSegmentEntity(ModCreateSegmentEntityParameters parameters, ModCreateSegmentEntityResults results)
        {
            try
            {
                if (parameters.Cube != HopperCubeType)
                    return;

                if (parameters.Value == 0)
                    results.Entity = new TrickyStorageHopper(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube,
                        parameters.Flags, parameters.Value, "Void Hopper", 100, new Color(.01f, .01f, .01f), false);
                else
                {
                    HopperTypeData hopperTypeData = mHopperTypeDataByCubeValue[parameters.Value];
                    results.Entity = new TrickyStorageHopper(parameters.Segment, parameters.X, parameters.Y, parameters.Z, parameters.Cube,
                        parameters.Flags, parameters.Value, hopperTypeData.Name, hopperTypeData.Capacity, hopperTypeData.Color, hopperTypeData.IsOneType);
                }
            }
            catch (Exception e)
            {
                Logging.LogException(e);
            }
        }
    }
}