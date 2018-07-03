using System;
using System.IO;
using Lidgren.Network;
using UnityEngine;

/// <summary>
/// CubeCoord extensions.
/// </summary>
public static class CubeCoordExtensions
{
    /// <summary>
    /// Returns an "x, y, z" text string of the position adjusted for the world offset.
    /// </summary>
    /// <param name="value">Cube Coordinate value.</param>
    /// <returns>"x, y, z" text string of the position adjusted for the world offset.</returns>
    public static string ToPositionString(this CubeCoord value)
    {
        return value.x - WorldScript.mDefaultOffset + ", " + (value.y - WorldScript.mDefaultOffset) + ", " +
               (value.z - WorldScript.mDefaultOffset);
    }


    /// <summary>
    /// Returns an "Depth: y  X/Y: x, y" text string of the position adjusted for the world offset.
    /// </summary>
    /// <param name="value">CubeCoord value.</param>
    /// <returns>"Depth: y X/Y: x, y"  text string of the position adjusted for the world offset.</returns>
    public static string ToDepthXZPositionString(this CubeCoord value)
    {
        return PersistentSettings.GetString("Depth") + ": " + (value.y - WorldScript.mDefaultOffset) + "  X/Z: " + (value.x - WorldScript.mDefaultOffset) +
               ", " + (value.z - WorldScript.mDefaultOffset);
    }

    /// <summary>
    /// Returns an "Y: y  X/Y: x, y" text string of the position adjusted for the world offset.
    /// </summary>
    /// <param name="value">CubeCoord value.</param>
    /// <returns>"Depth: y X/Y: x, y"  text string of the position adjusted for the world offset.</returns>
    public static string ToYXZPositionString(this CubeCoord value)
    {
        return "Y: " + (value.y - WorldScript.mDefaultOffset) + "  X/Z: " + (value.x - WorldScript.mDefaultOffset) + ", " +
               (value.z - WorldScript.mDefaultOffset);
    }


    /// <summary>
    /// Returns an "x, y, z" text string of the offset from this coordinate to the specified target coordinate.
    /// </summary>
    /// <param name="value">CubeCoord value.</param>
    /// <param name="targetCoordinate">Target coordinate.</param>
    /// <returns>"x, y, z" text string of the position adjusted for the world offset.</returns>
    public static string ToYXZOffsetString(this CubeCoord value, CubeCoord targetCoordinate)
    {
        return targetCoordinate.x - value.x + ", " + (targetCoordinate.y - value.y) + ", " +
               (targetCoordinate.z - value.z);
    }


    /// <summary>
    /// Returns the coordinate sum of the this coordinate and another.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="coordinate">Cube coordinate to add.</param>
    public static CubeCoord Add(this CubeCoord value, CubeCoord coordinate)
    {
        return new CubeCoord(value.x + coordinate.x, value.y + coordinate.y, value.z + coordinate.z);
    }


    /// <summary>
    /// Returns the coordinate sum of the this coordinate and the specified x, y, z values.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="x">X coordinate to add.</param>
    /// <param name="y">Y coordinate to add.</param>
    /// <param name="z">Z coordinate to add.</param>
    public static CubeCoord Add(this CubeCoord value, long x, long y, long z)
    {
        return new CubeCoord(value.x + x, value.y + y, value.z + z);
    }


    /// <summary>
    /// Returns the coordinate sum of the this coordinate and a vector.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="vector">Cube coordinate to add.</param>
    public static CubeCoord Add(this CubeCoord value, Vector3 vector)
    {
        return new CubeCoord(value.x + (long)vector.x, value.y + (long)vector.y, value.z + (long)vector.z);
    }


    /// <summary>
    /// Returns the coordinate offset of this coordinate from another.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="coordinate">Cube coordinate to add.</param>
    public static CubeCoord Offset(this CubeCoord value, CubeCoord coordinate)
    {
        return new CubeCoord(coordinate.x - value.x, coordinate.y - value.y, coordinate.z - value.z);
    }


    /// <summary>
    /// Returns the distance to the specified target cube coordinate.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="target">Target cube coordinate.</param>
    /// <returns>Distance to the target cube coordinate.</returns>
    public static float Distance(this CubeCoord value, CubeCoord target)
    {
        return Mathf.Sqrt(Mathf.Pow(value.x - target.x, 2) + Mathf.Pow(value.y - target.y, 2) + Mathf.Pow(value.z - target.z, 2));

    }


    /// <summary>
    /// Returns the x/z distance to the specified target cube coordinate.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="target">Target cube coordinate.</param>
    /// <returns>Distance to the target cube coordinate in x and z axis only.</returns>
    public static float XZDistance(this CubeCoord value, CubeCoord target)
    {
        return Mathf.Sqrt(Mathf.Pow(value.x - target.x, 2) + Mathf.Pow(value.z - target.z, 2));
    }


    /// <summary>
    /// Returns the distance to the specified target coordinate.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="targetX">Target X coordinate.</param>
    /// <param name="targetY">Target Y coordinate.</param>
    /// <param name="targetZ">Target Z coordinate.</param>
    /// <returns>Distance to the target cube coordinate.</returns>
    public static float Distance(this CubeCoord value, long targetX, long targetY, long targetZ)
    {
        return Mathf.Sqrt(Mathf.Pow(value.x - targetX, 2) + Mathf.Pow(value.y - targetY, 2) + Mathf.Pow(value.z - targetZ, 2));

    }


    /// <summary>
    /// Returns the largest axis distance to the specified target cube coordinate.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="target">Target cube coordinate.</param>
    /// <returns>Largest x, y, or z distance.</returns>
    public static float CubeDistance(this CubeCoord value, CubeCoord target)
    {
        return Mathf.Max(Mathf.Abs(value.x - target.x), Mathf.Max(Mathf.Abs(value.y - target.y), Mathf.Abs(value.z - target.z)));
    }



    /// <summary>
    /// Returns the largest axis distance to the specified target coordinate.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="targetX">Target X coordinate.</param>
    /// <param name="targetY">Target Y coordinate.</param>
    /// <param name="targetZ">Target Z coordinate.</param>
    /// <returns>Largest x, y, or z distance.</returns>
    public static float CubeDistance(this CubeCoord value, long targetX, long targetY, long targetZ)
    {
        return Mathf.Max(Mathf.Abs(value.x - targetX), Mathf.Max(Mathf.Abs(value.y - targetY), Mathf.Abs(value.z - targetZ)));
    }


    /// <summary>
    /// Returns a comparison for sort against the specified coordinate in YXZ order with Y descending and XZ ascending.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <param name="compareCoordinate">Compare coordinate.</param>
    /// <returns>Compare result.</returns>
    public static int SortedYXZCompare(this CubeCoord value, CubeCoord compareCoordinate)
    {
        if (value.y != compareCoordinate.y)
            return value.y.CompareTo(compareCoordinate.y);
        if (value.x != compareCoordinate.x)
            return value.x.CompareTo(compareCoordinate.x);
        if (value.z != compareCoordinate.z)
            return value.z.CompareTo(compareCoordinate.z);
        return 0;
    }


    /// <summary>
    /// Return the segment base coordinate of this coordinate.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <returns>CubeCoord of the segment base coordinate.</returns>
    public static CubeCoord ToSegmentBase(this CubeCoord value)
    {
        return new CubeCoord(value.x >> 4 << 4, value.y >> 4 << 4, value.z >> 4 << 4);
    }


    /// <summary>
    /// Returns the segment center coordinate of the segment for this coordinate.
    /// </summary>
    /// <param name="value">This cube coordinate.</param>
    /// <returns>CubeCoord of the segment base coordinate.</returns>
    public static CubeCoord ToSegmentCenter(this CubeCoord value)
    {
        return new CubeCoord((value.x >> 4 << 4) + 8, (value.y >> 4 << 4) + 8, (value.z >> 4 << 4) + 8);
    }


    /// <summary>
    /// Writes a cube coordinate value to a binary writer.
    /// </summary>
    /// <param name="value">Cube coordinate value.</param>
    /// <param name="writer">Binary writer.</param>
    public static void Write(this CubeCoord value, BinaryWriter writer)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }


    /// <summary>
    /// Writes a cube coordinate value as a byte coordinate offset from another coordinate to a binary writer. For transmitting multiple
    /// coordinate sets with points difference of less than 127 this shaves 21 bytes off. Using this approach on Falcor Transit Network
    /// cut payload sizes by 3-7 kilobytes.
    /// </summary>
    /// <param name="value">Cube coordinate value.</param>
    /// <param name="fromCoordinate">Coordinate to offset from.</param>
    /// <param name="writer">Binary writer.</param>
    public static void WriteAsByteOffset(this CubeCoord value, BinaryWriter writer, CubeCoord fromCoordinate)
    {
        // Handle Invalid as -128, -128, -128.
        if (value == CubeCoord.Invalid || fromCoordinate == CubeCoord.Invalid)
        {
            writer.Write((sbyte)-128);
            writer.Write((sbyte)-128);
            writer.Write((sbyte)-128);
            return;
        }

        // Check range.
        if (Mathf.Abs(fromCoordinate.x - value.x) > 127 ||
            Mathf.Abs(fromCoordinate.y - value.y) > 127 ||
            Mathf.Abs(fromCoordinate.z - value.z) > 127)
            throw new ArgumentOutOfRangeException(nameof(fromCoordinate),
                "One or more coordinate offset axis is outside the range of -127 to 127 (value: " + value + " from:" + fromCoordinate);

        // Write offsets.
        writer.Write((sbyte)(value.x - fromCoordinate.x));
        writer.Write((sbyte)(value.y - fromCoordinate.y));
        writer.Write((sbyte)(value.z - fromCoordinate.z));
    }


    /// <summary>
    /// Returns a serialized string of the cube coordinates.
    /// </summary>
    /// <param name="value">Cube coordinate value.</param>
    /// <returns>Serialization string of the cube coordinate value.</returns>
    public static string ToSerializedString(this CubeCoord value)
    {
        return value.x + "," + value.y + "," + value.z;
    }


    /// <summary>
    /// Reads the cube coordinate value from the specified binary reader.
    /// </summary>
    /// <param name="reader">Binary reader.</param>
    /// <returns>Cube coordinate value.</returns>
    public static CubeCoord Read(BinaryReader reader)
    {
        return new CubeCoord(reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64());
    }


    /// <summary>
    /// Reads the cube coordinate value from the specified network message.
    /// </summary>
    /// <param name="netIncomingMessage">Incoming network message.</param>
    /// <returns>Cube coordinate value.</returns>
    internal static CubeCoord Read(NetIncomingMessage netIncomingMessage)
    {
        return new CubeCoord(netIncomingMessage.ReadInt64(), netIncomingMessage.ReadInt64(), netIncomingMessage.ReadInt64());
    }



    /// <summary>
    /// Writes a cube coordinate value as a byte coordinate offset from another coordinate to a binary writer.
    /// </summary>
    /// <param name="netIncomingMessage">Incoming network message.</param>
    /// <param name="fromCoordinate">Coordinate to offset from.</param>
    /// <returns>Cube coordinate value.</returns>
    public static CubeCoord ReadFromByteOffset(NetIncomingMessage netIncomingMessage, CubeCoord fromCoordinate)
    {
        sbyte x = netIncomingMessage.ReadSByte();
        sbyte y = netIncomingMessage.ReadSByte();
        sbyte z = netIncomingMessage.ReadSByte();
        if (x == -128 && y == -128 && z == -128)
            return CubeCoord.Invalid;

        return new CubeCoord(fromCoordinate.x + x, fromCoordinate.y + y, fromCoordinate.z + z);
    }


    /// <summary>
    /// Parses a serialized cube coordinate string.
    /// </summary>
    /// <param name="text">Serialized cube coordinate string to parse.</param>
    /// <param name="coordinate">Output valid cube coordinate value if true is returned other CubeCoord.Invalid.</param>
    /// <returns>True is successful.</returns>
    public static bool TryParse(string text, out CubeCoord coordinate)
    {
        try
        {
            string[] values = text.Split(',');
            long x, y, z;
            if (values.Length == 3 && long.TryParse(values[0], out x) && long.TryParse(values[1], out y) && long.TryParse(values[3], out z))
            {
                coordinate = new CubeCoord(x, y, z);
                return true;
            }

            coordinate = CubeCoord.Invalid;
            return false;
        }
        catch
        {
            coordinate = CubeCoord.Invalid;
            return false;
        }
    }
}