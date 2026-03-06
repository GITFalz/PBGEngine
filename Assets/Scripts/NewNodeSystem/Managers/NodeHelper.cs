using PBG.Data;
using PBG.UI;

public static class NodeHelper
{
    public const float SlideSpeed = 10f / 2500f;

    public static void SetSlideValue(ref float value, UIField inputField, float speed, int index)
    {
        float delta = Input.GetMouseDelta().X * speed;
        if (delta == 0f) return;
        value += delta;
        inputField.SetText(value.ToString()).UpdateCharacters();
        GLSLManager.UpdateValue(index, value);
    }

    public static void SetSlideValue(ref int value, UIField inputField, float speed, int index)
    {
        float delta = Input.GetMouseDelta().X * speed;
        if (delta == 0f) return;
        value += (int)delta;
        inputField.SetText(value.ToString()).UpdateCharacters();
        GLSLManager.UpdateValue(index, value);
    }

    public static void SetValue(ref float value, UIField inputField, float replacement, int index)
    {
        value = inputField.GetFloat(replacement);
        GLSLManager.UpdateValue(index, value);
    }
    public static void SetValue(ref int value, UIField inputField, int replacement, int index)
    {
        value = inputField.GetInt(replacement);
        GLSLManager.UpdateValue(index, value);
    }
}