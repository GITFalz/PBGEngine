using PBG.Data;
using PBG.MathLibrary;

public abstract class Bone
{
    public string Name;
    public List<ChildBone> Children = [];
    public BoneSelection Selection = BoneSelection.None;


    public Vector3 Position {
        get => _position;
        set
        {
            _position = value;
            LocalAnimatedMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
        }
    }
    private Vector3 _position = Vector3.Zero;
    public Quaternion Rotation {
        get => _rotation;
        set
        {
            _rotation = value;
            _eulerRotation = _rotation.ToEuler();
            LocalAnimatedMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
        }
    }
    private Quaternion _rotation = Quaternion.Identity;
    public Vector3 EulerRotation
    {
        get => _eulerRotation;
        set
        {
            _eulerRotation = value;
            _rotation = Quaternion.FromEuler(_eulerRotation);
            LocalAnimatedMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
        }
    }
    private Vector3 _eulerRotation = Vector3.Zero;
    public Vector3 Scale
    {
        get => _scale;
        set
        {
            _scale = value;
            LocalAnimatedMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
        }
    }
    private Vector3 _scale = Vector3.One;

    // Computed at runtime (updated each frame)
    public Matrix4 LocalAnimatedMatrix = Matrix4.Identity;

    public Matrix4 GlobalAnimatedMatrix = Matrix4.Identity;
    public Matrix4 TransposedInverseGlobalAnimatedMatrix;

    public int Index = 0;

    public BonePivot Pivot;
    public BonePivot End;
    public Vector3 EndTarget;
    public Vector3 LocalEnd => Position + Vector3.Transform(new Vector3(0, 2, 0) * 0.1f * Scale, Rotation);

    public Bone(string name) 
    {
        Name = name;
        Pivot = new(GetPivot, this);
        End = new(GetEnd, this);
        LocalAnimatedMatrix = Matrix4.CreateScale(Scale) * Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateTranslation(Position);
    }

    public abstract void UpdateGlobalTransformation();
    public abstract string GetBonePath();
    public abstract RootBone GetRootBone();
    public abstract Vector3 GetPivot();
    public abstract Vector3 GetEnd();
    public abstract void Rotate();
    public abstract void Rotate(Vector3 axis, float angle);
    public abstract void Move();
    public abstract string GetParentName();

    public void Set(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        _position = position;
        _rotation = rotation;
        Scale = scale; // only set one public variable to calculate local matrix.
    }

    public void UpdateEndTarget()
    {
        EndTarget = End.Get - Pivot.Get;
    }

    public Matrix4 GetInverse()
    {
        return GlobalAnimatedMatrix.Inverted();
    }

    public bool Add(ChildBone child)
    {
        if (Children.Contains(child))
            return false;
        
        child.SetName(child.Name);
        Children.Add(child);
        return true;
    }

    public bool Remove(ChildBone bone) => Children.Remove(bone);

    public void SetName(string newName)
    {
        RootBone root = GetRootBone();
        HashSet<string> names = [];
        root.GetBoneNames(names);
        string name = newName;
        int cycle = 0;
        while (names.Contains(newName))
        {
            newName = $"{name}_{cycle}";
            cycle++;
        }
        Name = newName;
    }

    public BonePivot Not(BonePivot pivot)
    {
        if (pivot == Pivot)
            return End;
        else
            return Pivot;
    }

    public List<Bone> GetBones()
    {
        List<Bone> bones = [];
        GetBones(bones);
        return bones;
    }

    public void GetBones(List<Bone> bones)
    {
        bones.Add(this);
        foreach (var child in Children)
            child.GetBones(bones);
    }

    public void GetBones(Dictionary<string, Bone> bones)
    {
        bones.Add(Name, this);
        foreach (var child in Children)
            child.GetBones(bones);
    }

    public void GetBoneNames(HashSet<string> names)
    {
        names.Add(Name);
        foreach (var child in Children)
            child.GetBoneNames(names);
    }

    public void Run(Action<Bone, int> action, int indent = 0)
    {
        action(this, indent);
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Run(action, indent + 1);
        }
    }

    public void Run(Action<Bone> action)
    {
        action(this);
        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Run(action);
        }
    }

    public override string ToString()
    {
        return $"Bone: {Name}\n" +
            $"  Index: {Index}\n" +
            $"  Selection: {Selection}\n" +
            $"  Position: ({Position.X:F3}, {Position.Y:F3}, {Position.Z:F3})\n" +
            $"  Rotation (Euler): ({EulerRotation.X:F3}, {EulerRotation.Y:F3}, {EulerRotation.Z:F3})\n" +
            $"  Scale: ({Scale.X:F3}, {Scale.Y:F3}, {Scale.Z:F3})\n" +
            $"  EndTarget: ({EndTarget.X:F3}, {EndTarget.Y:F3}, {EndTarget.Z:F3})\n" +
            $"  Children: {Children.Count}";
    }

    public void Clear()
    {
        for (int i = 0; i < Children.Count; i++)
        {
            var bone = Children[i];
            bone.Clear();
        }
        Children.Clear();
    }

    public abstract void Delete();
}

public class RootBone : Bone
{
    public RootBone(string name) : base(name) { EndTarget = End.Get + Vector3.UnitY; }

    public override void UpdateGlobalTransformation()
    {
        GlobalAnimatedMatrix = LocalAnimatedMatrix;
        TransposedInverseGlobalAnimatedMatrix = GlobalAnimatedMatrix.Inverted().Transposed();

        foreach (var child in Children)
            child.UpdateGlobalTransformation();
    }

    public override string GetBonePath()
    {
        return Name;
    }

    public override RootBone GetRootBone()
    {
        return this;
    }

    public override Vector3 GetPivot()
    {
        return Position;
    }

    public override Vector3 GetEnd()
    {
        return Position + Vector3.Transform(new Vector3(0, 2, 0) * 0.1f * Scale, Rotation);
    }

    public override void Rotate()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();
    
        Vector3 worldUp = Vector3.UnitY;  // World up axis
        Vector3 cameraRight = Vector3.Normalize(GeneralModelingEditor.Instance.Scene.DefaultCamera.right);

        Rotation = Quaternion.FromAxisAngle(worldUp, Mathf.DegreesToRadians(mouseDelta.X * 0.1f)) * Rotation;
        Rotation = Quaternion.FromAxisAngle(cameraRight, Mathf.DegreesToRadians(mouseDelta.Y * 0.1f)) * Rotation;
    }
    
    public override void Rotate(Vector3 axis, float angle)
    {
        Rotation *= Quaternion.FromAxisAngle(axis, Mathf.DegreesToRadians(angle));
    }

    public override void Move()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();

        Vector3 axisY = Vector3.Normalize(GeneralModelingEditor.Instance.Scene.DefaultCamera.up);
        Vector3 axisX = Vector3.Normalize(GeneralModelingEditor.Instance.Scene.DefaultCamera.right);

        Position += axisY * -mouseDelta.Y * 0.01f;
        Position += axisX * mouseDelta.X * 0.01f;
    }

    public override string GetParentName() => Name;

    public RootBone Copy()
    {
        var copy = new RootBone(Name);
        foreach (var child in Children)
            child.Copy(copy);

        copy.Set(Position, Rotation, Scale);
        copy.UpdateGlobalTransformation();

        return copy;
    }

    public override void Delete() => Clear();
}

public class ChildBone : Bone
{
    public Bone Parent;

    public ChildBone(string name, Bone parent) : base(name)
    {
        Parent = parent;
        Parent.Add(this);
        EndTarget = End.Get + Vector3.UnitY;
    }

    public override void UpdateGlobalTransformation()
    {

        GlobalAnimatedMatrix = LocalAnimatedMatrix * Parent.GlobalAnimatedMatrix;
        TransposedInverseGlobalAnimatedMatrix = GlobalAnimatedMatrix.Inverted().Transposed();

        foreach (var child in Children)
        {
            child.UpdateGlobalTransformation();
        }
    }

    public override string GetBonePath()
    {
        return $"{Parent.GetBonePath()}.{Name}";
    }

    public override RootBone GetRootBone()
    {
        return Parent.GetRootBone();
    }

    public override Vector3 GetPivot()
    {
        var v4Position = new Vector4(Position, 1f);
        var v4Transformed = Parent.GlobalAnimatedMatrix.Transposed() * v4Position;
        return v4Transformed.Xyz;
    }

    public override Vector3 GetEnd()
    {
        var v4Position = new Vector4(Position + Vector3.Transform(new Vector3(0, 2, 0) * 0.1f * Scale, Rotation), 1f);
        var v4Transformed = Parent.GlobalAnimatedMatrix.Transposed() * v4Position;
        return v4Transformed.Xyz;
    }

    public override void Rotate()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();

        Vector3 worldUp = Vector3.UnitY;  // World up axis
        Vector3 cameraRight = Vector3.Normalize(GeneralModelingEditor.Instance.Scene.DefaultCamera.right);

        Quaternion horizontalRotation = Quaternion.FromAxisAngle(worldUp, Mathf.DegreesToRadians(mouseDelta.X * GameTime.DeltaTime * 50f));
        Quaternion verticalRotation = Quaternion.FromAxisAngle(cameraRight, Mathf.DegreesToRadians(mouseDelta.Y * GameTime.DeltaTime * 50f));

        Quaternion worldRotation = horizontalRotation * verticalRotation;

        Matrix4 invParent = Parent.GlobalAnimatedMatrix.Inverted();
        Matrix4 worldRotMatrix = Matrix4.CreateFromQuaternion(worldRotation);
        Matrix4 localRotMatrix = invParent * worldRotMatrix * Parent.GlobalAnimatedMatrix;

        Quaternion localRotation = localRotMatrix.ExtractRotation();

        Rotation = localRotation * Rotation;
    }
    
    public override void Rotate(Vector3 axis, float angle)
    {
        Matrix4 invParent = GlobalAnimatedMatrix.Inverted();
        Vector3 localAxis = Vector3.Normalize(Vector3.TransformNormal(axis, invParent));

        Rotation *= Quaternion.FromAxisAngle(localAxis, Mathf.DegreesToRadians(angle));
    }

    public override void Move()
    {
        Vector2 mouseDelta = Input.GetMouseDelta();

        Vector3 axisY = Vector3.Normalize(GeneralModelingEditor.Instance.Scene.DefaultCamera.up);
        Vector3 axisX = Vector3.Normalize(GeneralModelingEditor.Instance.Scene.DefaultCamera.right);

        Vector3 worldMovement = axisY * -mouseDelta.Y * 0.01f +
                                axisX * mouseDelta.X * 0.01f;

        Matrix4 invParent = Parent.GlobalAnimatedMatrix.Inverted();
        Vector3 localMovement = Vector3.TransformNormal(worldMovement, invParent);

        Position += localMovement;
    }
    
    public override string GetParentName() => Parent.Name;

    public ChildBone Copy(Bone parent)
    {
        var copy = new ChildBone(Name, parent);
        foreach (var child in Children)
            child.Copy(copy);

        copy.Set(Position, Rotation, Scale);
        return copy;
    }

    public override void Delete()
    {
        Parent.Remove(this);
        Clear();
    }
}

public class BonePivot
{
    public Bone Bone;
    public Func<Vector3> PositionFunc;

    public BonePivot(Func<Vector3> positionFunc, Bone bone)
    {
        Bone = bone;
        PositionFunc = positionFunc;
    }

    public Vector3 Get => PositionFunc();

    public bool IsEnd()
    {
        return this == Bone.End;
    }

    public bool IsPivot()
    {
        return this == Bone.Pivot;
    }
}

public enum BoneSelection
{
    None = 0,
    Pivot = 1,
    End = 2,
    Both = 7
}