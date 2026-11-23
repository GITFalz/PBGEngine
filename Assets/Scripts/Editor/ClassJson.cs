using System.ComponentModel;
using PBG;
using PBG.Editor;
using PBG.Graphics;
using PBG.MathLibrary;
using PBG.Pipes;
using PBG.UI;

public class ClassInfosJson
{
    public string? Name { get; set; }
    public List<JsonValue>? Fields { get; set; } = null;
    public List<JsonValue>? Properties { get; set; } = null;

    public void GenerateNodes(UIVScroll hierarchy)
    {
        if ((Fields == null && Properties == null) || Name == null)
            return;

        var node = new UICol(w_full, h_[45])[
            new UIImg(left_[8], top_[7], w_[30], h_[30], bg_white, icon_[50]),
            new UIText(EditorMenu.CurrentNodePath.Split('/')[^1], fs_[1.2f], left_[50], top_[10]),
            new UIText($"Class - {(Fields?.Count ?? 0) + (Properties?.Count ?? 0)} fields", fs_[0.9f], gray_[70], left_[50], top_[25])
        ];

        hierarchy += node;

        if (Properties != null)
            foreach (var property in Properties)
            {
                var sub = property.GenerateField(value => property.SetValue(null, Name, value));
                
                if (sub != null)
                {
                    hierarchy += EditorMenu.LightHsep;
                    hierarchy += sub;
                    hierarchy += EditorMenu.BlackHsep;
                }
            }
        
        if (Fields != null)
            foreach (var field in Fields)
            {
                var sub = field.GenerateField(value => field.SetValue(null, Name, value));
                
                if (sub != null)
                {
                    hierarchy += EditorMenu.LightHsep;
                    hierarchy += sub;
                    hierarchy += EditorMenu.BlackHsep;
                }
            }
    }
}