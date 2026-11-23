using PBG.Core;
using PBG.Data;
using PBG.UI;

public class UITest : ScriptingNode
{
    public UIController Controller;
    private UIText _fps;

    void Start()
    {
        Controller = Transform.GetComponent<UIController>();

        Controller += new UICol(w_full, h_full)[
            new UIText("FPS: 9999").Out(out _fps),
            new UICol(w_[100], h_[100], mask_children, middle_center, blank_full_g_[30], hover_scale_[2f], hover_scale_duration_[2f], hover_scale_easeinout)[
                new UICol(w_[80], h_[80], blank_full_g_[40], hover_rotation_[25f], hover_rotation_duration_[2f], hover_rotation_easeinout)[
                    new UICol(w_[60], h_[60], blank_full_g_[50], hover_rotation_[25f], hover_rotation_duration_[2f], hover_rotation_easeinout)
                ]
            ]
        ];
    }

    void Update()
    {
        if (GameTime.FpsUpdated)
        {
            _fps.UpdateText("FPS: " + GameTime.Fps);
        }
    }
}