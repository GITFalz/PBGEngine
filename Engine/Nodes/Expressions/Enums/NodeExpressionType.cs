namespace PBG.Nodes;

[Flags]
public enum NodeExpressionType
{
    None        = 0,
    Readable    = 1 << 0,
    Writeable   = 1 << 1,
    Statement   = 1 << 2,
    Value       = 1 << 3,
    Constant    = 1 << 4,
    Temporary   = 1 << 5,
    Isolated    = 1 << 6,
}