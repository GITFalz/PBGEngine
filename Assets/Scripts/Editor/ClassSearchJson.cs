using PBG.Graphics;
using PBG.UI;

public class ClassesSearchJson
{
    public List<ClassSearchJson>? Classes { get; set; } = null;

    public void GenerateClasses(UIVCol col)
    {
        if (Classes == null)
            return;

        foreach (var classe in Classes)
        {
            var last = classe.GenerateClass(col);
            
            if (last != null)
            {
                col += last;
            }
        }
    }
}

public class ClassSearchJson
{
    public string? Class { get; set; } = null;

    public UIElementBase? GenerateClass(UIVCol col)
    {
        if (Class == null)
            return null;

        return new UICol(w_full, h_[20], blank_full, hover_color_[(0.5f, 0.5f, 0.5f, 0f), (0.5f, 0.5f, 0.5f, 0.5f)], hover_color_duration_[0.1f], hover_color_easeinout)[
            new UIText(Class, middle_left, left_[5], bg_white)
        ].OnClick(col => {
            GameWindow.Send(PBG.Pipes.CommandType.ClassInfo, Class);
        });
    }
}