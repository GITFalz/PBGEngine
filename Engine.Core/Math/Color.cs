namespace PBG.MathLibrary;

public struct Color
{
    public readonly static Color Invisible = new Color(0, 0, 0, 0);
    public Vector4 Value;

    public Color(Vector3 color) { Value = new Vector4(color, 1); }
    public Color(Vector4 color) { Value = color; }
    public Color(float r, float g, float b) : this((r, g, b)) {}
    public Color(float r, float g, float b, float a) : this((r, g, b, a)) {}
    public Color(string hex) : this(GetColor(hex)) { }

    public static implicit operator Vector4(Color c) => c.Value;

    public static Color GetColor(string hex)
    {
        if (hex.StartsWith('#')) hex = hex[1..];

        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);

            return new Color(r / 255f, g / 255f, b / 255f);
        }

        if (hex.Length == 8)
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = Convert.ToByte(hex.Substring(6, 2), 16);

            return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
        }

        throw new ArgumentException("Hex must be 6 characters (RRGGBB) or 8 characters (RRGGBBAA)");
    }
}