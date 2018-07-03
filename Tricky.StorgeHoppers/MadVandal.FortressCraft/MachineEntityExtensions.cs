/// <summary>
/// MachineEntity extensions.
/// </summary>
public static class MachineEntityExtensions
{
    /// <summary>
    /// Returns true if the specified conveyor entity is facing the machine entity.
    /// </summary>
    /// <param name="machine">Machine entity.</param>
    /// <param name="conveyor">Conveyor entity</param>
    /// <returns>True if the specified conveyor entity is facing the machine entity.</returns>
    public static bool IsConveyorFacingMe(this MachineEntity machine, ConveyorEntity conveyor)
    {
        long num1 = conveyor.mnX + (long)conveyor.mForwards.x;
        long num2 = conveyor.mnY + (long)conveyor.mForwards.y;
        long num3 = conveyor.mnZ + (long)conveyor.mForwards.z;
        return num1 == machine.mnX && num2 == machine.mnY && num3 == machine.mnZ;
    }
}
