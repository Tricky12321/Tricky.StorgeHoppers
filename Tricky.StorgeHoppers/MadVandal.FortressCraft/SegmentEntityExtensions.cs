/// <summary>
/// SegmentEntity extensions.
/// </summary>
public static class SegmentEntityExtensions
{
    /// <summary>
    /// Returns the cube coordinate of the segment entity position.
    /// </summary>
    /// <param name="value">SegmentEntity object.</param>
    /// <returns>Cube coordinate of the segment entity position.</returns>
    public static CubeCoord ToCubeCoord(this SegmentEntity value)
    {
        return new CubeCoord(value.mnX, value.mnY, value.mnZ);
    }


    /// <summary>
    /// Returns an "x, y, z" text string of the entity position adjusted for the world offset.
    /// </summary>
    /// <param name="value">SegmentEntity object.</param>
    /// <returns>"x, y, z" text string of the entity position adjusted for the world offset.</returns>
    public static string ToPositionString(this SegmentEntity value)
    {
        return value.mnX - WorldScript.mDefaultOffset + ", " + (value.mnY - WorldScript.mDefaultOffset) + ", " +
               (value.mnZ - WorldScript.mDefaultOffset);
    }


    /// <summary>
    /// Returns an "Depth: y X/Y: x, y" text string of the entity position adjusted for the world offset.
    /// </summary>
    /// <param name="value">CubeCoord value.</param>
    /// <returns>"Depth: y X/Y: x, y"  text string of the position adjusted for the world offset.</returns>
    public static string ToDepthXZPositionString(this SegmentEntity value)
    {
        return PersistentSettings.GetString("Depth") + ": " + (value.mnY - WorldScript.mDefaultOffset) + "  X/Z: " + (value.mnX - WorldScript.mDefaultOffset) +
               ", " + (value.mnZ - WorldScript.mDefaultOffset);
    }


    /// <summary>
    /// Returns an "Y: y  X/Y: x, y" text string of the position adjusted for the world offset.
    /// </summary>
    /// <param name="value">CubeCoord value.</param>
    /// <returns>"Depth: y X/Y: x, y"  text string of the position adjusted for the world offset.</returns>
    public static string ToYXZPositionString(this SegmentEntity value)
    {
        return "Y: " + (value.mnY - WorldScript.mDefaultOffset) + "  X/Z: " + (value.mnX - WorldScript.mDefaultOffset) + ", " +
               (value.mnZ - WorldScript.mDefaultOffset);
    }


    /// <summary>
    /// Returns an "x, y, z" text string of the offset from this coordinate to the specified target coordinate.
    /// </summary>
    /// <param name="value">CubeCoord value.</param>
    /// <param name="targetCoordinate">Target coordinate.</param>
    /// <returns>"x, y, z" text string of the position adjusted for the world offset.</returns>
    public static string ToYXZOffsetString(this SegmentEntity value, CubeCoord targetCoordinate)
    {
        return targetCoordinate.x - value.mnX + ", " + (targetCoordinate.y - value.mnY) + ", " +
               (targetCoordinate.z - value.mnZ);
    }


    /// <summary>
    /// Gets the segment entity at the specified coordinate using the machine entity as a reference to get the segment.
    /// </summary>
    /// <param name="value">MachineEntity object.</param>
    /// <param name="coordinate">Cube coordinate.</param>
    /// <returns>SegmentEntity object at the coordinate or null if none or the segment cannot be accessed.</returns>
    public static SegmentEntity GetSegmentEntity(this MachineEntity value, CubeCoord coordinate)
    {
        if (value == null)
            return null;
        Segment segment = value.AttemptGetSegment(coordinate.x, coordinate.y, coordinate.z);
        // Seems we have to be persistent on this sometimes...why? Need to verify if this isn't needed anymore.
        if (segment == null)
            segment = value.AttemptGetSegment(coordinate.x, coordinate.y, coordinate.z);
        if (segment == null)
            return null;

        return segment.SearchEntity(coordinate.x, coordinate.y, coordinate.z);
    }


    /// <summary>
    /// Gets the displayed name for a specified non-mod machine entity instance.
    /// </summary>
    /// <param name="value">MachineEntity instance.</param>
    /// <returns>Displayed name for the machine or empty if not found.</returns>
    public static string DisplayName(this MachineEntity value)
    {
        string name = TerrainData.GetNameForValue(value.mCube, value.mValue);
        return name != null ? name : string.Empty;
    }
}
