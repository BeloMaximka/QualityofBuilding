using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ImmersiveBuilding.Source.CollectibleBehaviors.ShovelModes;

public enum SenwOrientations
{
    South,
    East,
    North,
    West,
}

public readonly struct NsweBlockIds
{
    private readonly int[] Ids = new int[4];

    public NsweBlockIds(int north, int south, int west, int east)
    {
        Ids[(int)SenwOrientations.North] = north;
        Ids[(int)SenwOrientations.South] = south;
        Ids[(int)SenwOrientations.West] = west;
        Ids[(int)SenwOrientations.East] = east;
    }

    public readonly int NorthId => Ids[(int)SenwOrientations.North];
    public readonly int SouthId => Ids[(int)SenwOrientations.South];
    public readonly int WestId => Ids[(int)SenwOrientations.West];
    public readonly int EastId => Ids[(int)SenwOrientations.East];

    public bool Contains(int value) => value == SouthId || value == EastId || value == NorthId || value == WestId;

    public int GetCorrectBlockOrientationVariant(IPlayer player, BlockSelection blockSelection)
    {
        return blockSelection.Face.Index switch
        {
            BlockFacing.indexNORTH => SouthId,
            BlockFacing.indexWEST => EastId,
            BlockFacing.indexSOUTH => NorthId,
            BlockFacing.indexEAST => WestId,
            BlockFacing.indexUP => GetOrientationVariantFromTopHit(player, blockSelection),
            _ => GetOrientationVariantFromPlayerYaw(player),
        };
    }

    /// <summary>
    /// <code>
    /// │                            X
    ///─┼──────────────────────────────►
    /// │(0,0)•───────────────────•(1,0)
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         N         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │--W------•------E--│
    /// │     │         (0,5,0.5) │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         |         │
    /// │     │         S         │
    /// │     │         |         │
    ///Z│     │         |         │
    /// ▼(0,1)•───────────────────•(1,1)
    /// </code>
    /// </summary>
    /// <returns>
    /// The appropriate block variant based on which half of the top face a player selects (closest or furthest).
    /// <br/>For example, when a player is facing north and chooses the closer half (the southern half, closer to them),
    /// the north variant will be selected (so the stairs ascend).
    /// </returns>
    private int GetOrientationVariantFromTopHit(IPlayer player, BlockSelection blockSelection)
    {
        return YawToSEDirection(player.Entity.Pos.Yaw) switch
        {
            SenwOrientations.South => blockSelection.HitPosition.Z >= 0.5f ? NorthId : SouthId, // NS
            _ => blockSelection.HitPosition.X >= 0.5f ? WestId : EastId, // WE
        };
    }

    private int GetOrientationVariantFromPlayerYaw(IPlayer player)
    {
        return YawToNsweDirection(player.Entity.Pos.Yaw) switch
        {
            SenwOrientations.North => SouthId,
            SenwOrientations.West => EastId,
            SenwOrientations.South => NorthId,
            _ => WestId, // East
        };
    }

    private static SenwOrientations YawToNsweDirection(float yaw) =>
        (SenwOrientations)GameMath.Mod((int)Math.Round((yaw * GameMath.RAD2DEG) / 90f), 4);

    private static SenwOrientations YawToSEDirection(float yaw) =>
        (SenwOrientations)GameMath.Mod((int)Math.Round((yaw * GameMath.RAD2DEG) / 90f), 2);
}
