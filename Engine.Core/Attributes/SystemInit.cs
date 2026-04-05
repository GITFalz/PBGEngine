[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SystemInitAttribute : Attribute {
    public int Priority { get; }
    public SystemInitAttribute(int priority = 0) {
        Priority = priority;
    }
}