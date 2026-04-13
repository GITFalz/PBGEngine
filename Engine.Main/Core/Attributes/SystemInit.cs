[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SystemInitAttribute : Attribute {
    public InitPriority Priority { get; }
    public SystemInitAttribute(InitPriority priority = InitPriority.Global) {
        Priority = priority;
    }
}

public enum InitPriority
{
    Buffer = 0,
    Data = 1,
    Global = 2
}