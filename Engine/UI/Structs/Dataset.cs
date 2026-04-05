using System.Diagnostics.CodeAnalysis;
using PBG.MathLibrary;

namespace PBG.UI
{
    public class Dataset : Dictionary<string, object>
    {
        public new object this[string key]
        {
            get
            {
                if (!TryGetValue(key, out _))
                    Add(key, "");
                return base[key];
            }
            set => base[key] = value;
        }

        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? value)
        {
            value = default;
            if (!TryGetValue(key, out var v) || v is not T t)
                return false;
                
            value = t;
            return true;
        }

        public int Int(string key) => Parse.Int.Parse(this[key]);
        public float Float(string key) => Parse.Float.Parse(this[key]);
        public Vector2 Vector2(string key) => Parse.Vec2.Parse(this[key]);
        public Vector3 Vector3(string key) => Parse.Vec3.Parse(this[key]);
        public Vector4 Vector4(string key) => Parse.Vec4.Parse(this[key]);
        public string String(string key) => this[key].ToString() ?? "";
        public bool Bool(string key) => Parse.Bool.Parse(this[key]);
        public bool IsSelected() => Parse.Bool.Parse(this["selected"]);
        public void Select() => this["selected"] = true;
        public void Deselect() => this["selected"] = false;
    }
}