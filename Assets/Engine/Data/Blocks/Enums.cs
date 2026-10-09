namespace PBG.Voxel
{
    public enum BlockFacing 
    { 
        North = 0, 
        South = 1, 
        East = 2,   
        West = 3
    }

    public enum BlockRotation 
    { 
        None = 0, 
        X = 1, 
        NegX = 2, 
        Z = 3, 
        NegZ = 4 
    }

    public enum BlockSide 
    { 
        Front = 0, 
        Right = 1, 
        Top = 2,
        Left = 3, 
        Bottom = 4,
        Back = 5  
    }

    public static class BlockEnumData
    {
        // ---------- Facing ----------
        public static BlockFacing GetFacing(string facing)
        {
            if (string.IsNullOrEmpty(facing))
                return BlockFacing.North;

            return facing.ToLowerInvariant() switch
            {
                "north" => BlockFacing.North,
                "south" => BlockFacing.South,
                "east"  => BlockFacing.East,
                "west"  => BlockFacing.West,
                _       => BlockFacing.North
            };
        }

        // ---------- Side ----------
        public static BlockSide GetSide(string side)
        {
            if (string.IsNullOrEmpty(side))
                return BlockSide.Front;

            return side.ToLowerInvariant() switch
            {
                "front"  => BlockSide.Front,
                "right"  => BlockSide.Right,
                "top"    => BlockSide.Top,
                "left"   => BlockSide.Left,
                "bottom" => BlockSide.Bottom,
                "back"   => BlockSide.Back,
                _        => BlockSide.Front
            };
        }

        // ---------- Rotation ----------
        public static BlockRotation GetRotation(string rotation)
        {
            if (string.IsNullOrEmpty(rotation))
                return BlockRotation.None;

            return rotation.ToLowerInvariant() switch
            {
                "x"  => BlockRotation.X,
                "-x" => BlockRotation.NegX,
                "z"  => BlockRotation.Z,
                "-z" => BlockRotation.NegZ,
                _    => BlockRotation.None
            };
        }
    }
}