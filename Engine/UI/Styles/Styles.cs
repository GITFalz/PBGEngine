using PBG.MathLibrary;
using PBG.UI;
using static PBG.UI.UIHelper;

namespace PBG.UI
{
    public static partial class Styles
    {
        #region Width
            public readonly static TrinaryType<int, uint, float> w_ = new((v, e) => e.Width = v.Px(), (v, e) => e.Width = v.Px(), (v, e) => e.Width = v.Pc());
            public readonly static UnaryType<int> w_px_ = new((v, e) => e.Width = v.Px());
            public readonly static UnaryType<float> w_pc_ = new((v, e) => e.Width = v.Pc());
            public readonly static ValueStyle w_full = new((e) => e.Width = 100.Pc());
            public readonly static ValueStyle w_half = new((e) => e.Width = 50.Pc());
            public readonly static BinaryStyle<float, int> w_minus_ = new((v1, v2, e) => e.Width = v1.Pc(-v2));
            public readonly static BinaryStyle<float, int> w_plus_ = new((v1, v2, e) => e.Width = v1.Pc(v2));
            public readonly static UnaryType<float> w_full_minus_ = new((v, e) => e.Width = 100.Pc(-v));
            public readonly static UnaryType<float> w_full_plus_ = new((v, e) => e.Width = 100.Pc(v));
            public readonly static UnaryType<float> w_half_minus_ = new((v, e) => e.Width = 50.Pc(-v));
            public readonly static UnaryType<float> w_half_plus_ = new((v, e) => e.Width = 50.Pc(v));
        #endregion

        #region Min Width
            public readonly static TrinaryType<int, uint, float> min_w_ = new((v, e) => e.MinWidth = v.Px(), (v, e) => e.MinWidth = v.Px(), (v, e) => e.MinWidth = v.Pc());
            public readonly static UnaryType<int> min_w_px_ = new((v, e) => e.MinWidth = v.Px());
            public readonly static UnaryType<float> min_w_pc_ = new((v, e) => e.MinWidth = v.Pc());
            public readonly static ValueStyle min_w_full = new((e) => e.MinWidth = 100.Pc());
            public readonly static ValueStyle min_w_half = new((e) => e.MinWidth = 50.Pc());
            public readonly static BinaryStyle<float, int> min_w_minus_ = new((v1, v2, e) => e.MinWidth = v1.Pc(-v2));
            public readonly static BinaryStyle<float, int> min_w_plus_ = new((v1, v2, e) => e.MinWidth = v1.Pc(v2));
            public readonly static UnaryType<float> min_w_full_minus_ = new((v, e) => e.MinWidth = 100.Pc(-v));
            public readonly static UnaryType<float> min_w_full_plus_ = new((v, e) => e.MinWidth = 100.Pc(v));
            public readonly static UnaryType<float> min_w_half_minus_ = new((v, e) => e.MinWidth = 50.Pc(-v));
            public readonly static UnaryType<float> min_w_half_plus_ = new((v, e) => e.MinWidth = 50.Pc(v));
        #endregion

        #region Max Width
            public readonly static TrinaryType<int, uint, float> max_w_ = new((v, e) => e.MaxWidth = v.Px(), (v, e) => e.MaxWidth = v.Px(), (v, e) => e.MaxWidth = v.Pc());
            public readonly static UnaryType<int> max_w_px_ = new((v, e) => e.MaxWidth = v.Px());
            public readonly static UnaryType<float> max_w_pc_ = new((v, e) => e.MaxWidth = v.Pc());
            public readonly static ValueStyle max_w_full = new((e) => e.MaxWidth = 100.Pc());
            public readonly static ValueStyle max_w_half = new((e) => e.MaxWidth = 50.Pc());
            public readonly static BinaryStyle<float, int> max_w_minus_ = new((v1, v2, e) => e.MaxWidth = v1.Pc(-v2));
            public readonly static BinaryStyle<float, int> max_w_plus_ = new((v1, v2, e) => e.MaxWidth = v1.Pc(v2));
            public readonly static UnaryType<float> max_w_full_minus_ = new((v, e) => e.MaxWidth = 100.Pc(-v));
            public readonly static UnaryType<float> max_w_full_plus_ = new((v, e) => e.MaxWidth = 100.Pc(v));
            public readonly static UnaryType<float> max_w_half_minus_ = new((v, e) => e.MaxWidth = 50.Pc(-v));
            public readonly static UnaryType<float> max_w_half_plus_ = new((v, e) => e.MaxWidth = 50.Pc(v));
        #endregion



        #region Height
            public readonly static TrinaryType<int, uint, float> h_ = new((v, e) => e.Height = v.Px(), (v, e) => e.Height = v.Px(), (v, e) => e.Height = v.Pc());
            public readonly static UnaryType<int> h_px_ = new((v, e) => e.Height = v.Px());
            public readonly static UnaryType<float> h_pc_ = new((v, e) => e.Height = v.Pc());
            public readonly static ValueStyle h_full = new((e) => e.Height = 100.Pc());
            public readonly static ValueStyle h_half = new((e) => e.Height = 50.Pc());
            public readonly static BinaryStyle<float, int> h_minus_ = new((v1, v2, e) => e.Height = v1.Pc(-v2));
            public readonly static BinaryStyle<float, int> h_plus_ = new((v1, v2, e) => e.Height = v1.Pc(v2));
            public readonly static UnaryType<float> h_full_minus_ = new((v, e) => e.Height = 100.Pc(-v));
            public readonly static UnaryType<float> h_full_plus_ = new((v, e) => e.Height = 100.Pc(v));
            public readonly static UnaryType<float> h_half_minus_ = new((v, e) => e.Height = 50.Pc(-v));
            public readonly static UnaryType<float> h_half_plus_ = new((v, e) => e.Height = 50.Pc(v));
        #endregion

        #region Min Height
            public readonly static TrinaryType<int, uint, float> min_h_ = new((v, e) => e.MinHeight = v.Px(), (v, e) => e.MinHeight = v.Px(), (v, e) => e.MinHeight = v.Pc());
            public readonly static UnaryType<int> min_h_px_ = new((v, e) => e.MinHeight = v.Px());
            public readonly static UnaryType<float> min_h_pc_ = new((v, e) => e.MinHeight = v.Pc());
            public readonly static ValueStyle min_h_full = new((e) => e.MinHeight = 100.Pc());
            public readonly static ValueStyle min_h_half = new((e) => e.MinHeight = 50.Pc());
            public readonly static BinaryStyle<float, int> min_h_minus_ = new((v1, v2, e) => e.MinHeight = v1.Pc(-v2));
            public readonly static BinaryStyle<float, int> min_h_plus_ = new((v1, v2, e) => e.MinHeight = v1.Pc(v2));
            public readonly static UnaryType<float> min_h_full_minus_ = new((v, e) => e.MinHeight = 100.Pc(-v));
            public readonly static UnaryType<float> min_h_full_plus_ = new((v, e) => e.MinHeight = 100.Pc(v));
            public readonly static UnaryType<float> min_h_half_minus_ = new((v, e) => e.MinHeight = 50.Pc(-v));
            public readonly static UnaryType<float> min_h_half_plus_ = new((v, e) => e.MinHeight = 50.Pc(v));
        #endregion

        #region Max Height
            public readonly static TrinaryType<int, uint, float> max_h_ = new((v, e) => e.MaxHeight = v.Px(), (v, e) => e.MaxHeight = v.Px(), (v, e) => e.MaxHeight = v.Pc());
            public readonly static UnaryType<int> max_h_px_ = new((v, e) => e.MaxHeight = v.Px());
            public readonly static UnaryType<float> max_h_pc_ = new((v, e) => e.MaxHeight = v.Pc());
            public readonly static ValueStyle max_h_full = new((e) => e.MaxHeight = 100.Pc());
            public readonly static ValueStyle max_h_half = new((e) => e.MaxHeight = 50.Pc());
            public readonly static BinaryStyle<float, int> max_h_minus_ = new((v1, v2, e) => e.MaxHeight = v1.Pc(-v2));
            public readonly static BinaryStyle<float, int> max_h_plus_ = new((v1, v2, e) => e.MaxHeight = v1.Pc(v2));
            public readonly static UnaryType<float> max_h_full_minus_ = new((v, e) => e.MaxHeight = 100.Pc(-v));
            public readonly static UnaryType<float> max_h_full_plus_ = new((v, e) => e.MaxHeight = 100.Pc(v));
            public readonly static UnaryType<float> max_h_half_minus_ = new((v, e) => e.MaxHeight = 50.Pc(-v));
            public readonly static UnaryType<float> max_h_half_plus_ = new((v, e) => e.MaxHeight = 50.Pc(v));
        #endregion

        public readonly static UnaryType<float> left_ = new((v, e) => e.BaseOffset.X = v);
        public readonly static UnaryType<float> right_ = new((v, e) => e.BaseOffset.X = -v);
        public readonly static UnaryType<float> top_ = new((v, e) => e.BaseOffset.Y = v);
        public readonly static UnaryType<float> bottom_ = new((v, e) => e.BaseOffset.Y = -v);

        public readonly static ValueStyle top_left = new(e => e.Alignement = UIAlign.TopLeft);
        public readonly static ValueStyle top_center = new(e => e.Alignement = UIAlign.TopCenter);
        public readonly static ValueStyle top_right = new(e => e.Alignement = UIAlign.TopRight);
        public readonly static ValueStyle middle_left = new(e => e.Alignement = UIAlign.MiddleLeft);
        public readonly static ValueStyle middle_center = new(e => e.Alignement = UIAlign.MiddleCenter);
        public readonly static ValueStyle middle_right = new(e => e.Alignement = UIAlign.MiddleRight);
        public readonly static ValueStyle bottom_left = new(e => e.Alignement = UIAlign.BottomLeft);
        public readonly static ValueStyle bottom_center = new(e => e.Alignement = UIAlign.BottomCenter);
        public readonly static ValueStyle bottom_right = new(e => e.Alignement = UIAlign.BottomRight);

        public readonly static ValueStyle justify_start = new(e => e.Alignement = UIAlign.MiddleLeft);
        public readonly static ValueStyle justify_center = new(e => e.Alignement = UIAlign.MiddleCenter);
        public readonly static ValueStyle justify_end = new(e => e.Alignement = UIAlign.MiddleRight);
        public readonly static ValueStyle items_start = new(e => e.Alignement = UIAlign.TopCenter);
        public readonly static ValueStyle items_center = new(e => e.Alignement = UIAlign.MiddleCenter);
        public readonly static ValueStyle items_end = new(e => e.Alignement = UIAlign.BottomCenter);

        // VISIBILITY CLASSES
        // =============================================================================
        public readonly static ValueStyle visible = new(e => { e.Visible = true; if (e is UICol col) col.WasVisible = true; });
        public readonly static ValueStyle invisible = new(e => { e.Visible = false; if (e is UICol col) col.WasVisible = false; });
        public readonly static ValueStyle hidden = new(e => { e.Visible = false; if (e is UICol col) col.WasVisible = false; });
        public readonly static ValueStyle not_toggle_old_invisible = new(e => { if (e is UICol col) col.SetForceToggleVisible(false); });

        // COLOR CLASSES
        // =============================================================================
        // Gray
        public readonly static BinaryType<float, int> gray_ = new((v, e) => e.Color = new(new(v), 1f), (v, e) => e.Color = new(new(((float)v) / 100f), 1f));
        public readonly static TrinaryStyle<float, float, float> rgb_ = new((r, g, b, e) => e.Color = new(r, g, b, 1f));
        public readonly static UnaryType<Vector3> rgb_v3_ = new((color, e) => e.Color = new(color.X, color.Y, color.Z, 1f));
        public readonly static QuaternaryStyle<float, float, float, float> rgba_ = new((r, g, b, a, e) => e.Color = new(r, g, b, a));
        public readonly static UnaryType<Vector4> rgba_v4_ = new((color, e) => e.Color = color);
        public readonly static ValueStyle transparent = new(e => e.Color = TRANSPARENT);
        
        public readonly static UnaryType<Vector3> color3_ = new((color, e) => e.Color = new(color.X, color.Y, color.Z, 1f));
        public readonly static UnaryType<Vector4> color4_ = new((color, e) => e.Color = color);


        

        // Accent colors
        public readonly static ValueStyle bg_blue = new(e => e.Color = ACCENT_BLUE);
        public readonly static ValueStyle bg_blue_hover = new(e => e.Color = ACCENT_BLUE_HOVER);
        public readonly static ValueStyle bg_red = new(e => e.Color = ACCENT_RED);
        public readonly static ValueStyle bg_green = new(e => e.Color = ACCENT_GREEN);
        public readonly static ValueStyle bg_orange = new(e => e.Color = ACCENT_ORANGE);
        public readonly static ValueStyle bg_purple = new(e => e.Color = ACCENT_PURPLE);
        public readonly static ValueStyle bg_yellow = new(e => e.Color = ACCENT_YELLOW);

        // Special colors
        public readonly static ValueStyle bg_transparent = new(e => e.Color = TRANSPARENT);
        public readonly static ValueStyle bg_black = new(e => e.Color = BLACK);
        public readonly static ValueStyle bg_white = new(e => e.Color = WHITE);

        // Text colors (same naming convention)
        public readonly static ValueStyle text_gray_000 = new(e => e.Color = GRAY_000);
        public readonly static ValueStyle text_gray_050 = new(e => e.Color = GRAY_050);
        public readonly static ValueStyle text_gray_100 = new(e => e.Color = GRAY_100);
        public readonly static ValueStyle text_blue = new(e => e.Color = ACCENT_BLUE);
        public readonly static ValueStyle text_red = new(e => e.Color = ACCENT_RED);
        public readonly static ValueStyle text_green = new(e => e.Color = ACCENT_GREEN);
        public readonly static ValueStyle text_white = new(e => e.Color = WHITE);
        public readonly static ValueStyle text_black = new(e => e.Color = BLACK);

        // SPACING CLASSES
        // =============================================================================
        public readonly static UnaryType<float> spacing_ = new((v, e) => { if (e is UICol col) col.SetSpacing(v); });

        // BORDER CLASSES
        // =============================================================================
        public readonly static QuaternaryStyle<float, float, float, float> border_ = new((left, right, top, bottom, e) => { if (e is UICol col) col.SetBorder((left, right, top, bottom)); });
        // Individual border sides
        public readonly static UnaryType<float> border_left_ = new((v, e) => { if (e is UICol col) col.SetBorderX(v); });
        public readonly static UnaryType<float> border_right_ = new((v, e) => { if (e is UICol col) col.SetBorderZ(v); });
        public readonly static UnaryType<float> border_top_ = new((v, e) => { if (e is UICol col) col.SetBorderY(v); });
        public readonly static UnaryType<float> border_bottom_ = new((v, e) => { if (e is UICol col) col.SetBorderW(v); });

        // PADDING CLASSES
        // =============================================================================
        public readonly static QuaternaryStyle<float, float, float, float> padding_ = new((left, right, top, bottom, e) => e.Padding = (left, right, top, bottom));

        public readonly static UnaryType<float> padding_left_ = new((v, e) => e.Padding.X = v);
        public readonly static UnaryType<float> padding_right_ = new((v, e) => e.Padding.Z = v);
        public readonly static UnaryType<float> padding_top_ = new((v, e) => e.Padding.Y = v);
        public readonly static UnaryType<float> padding_bottom_ = new((v, e) => e.Padding.W = v);

        // TYPOGRAPHY CLASSES (fontSize: 1 = 10px, 2 = 20px, so linear scaling)
        // =============================================================================
        public readonly static UnaryType<float> font_size_ = new((v, e) => { if (e is UIText t) t.FontSize = v; });
        public readonly static UnaryType<float> fs_ = new((v, e) => { if (e is UIText t) t.FontSize = v; });

        // Text character limits
        public readonly static UnaryType<int> max_chars_ = new((v, e) => { if (e is UIText t) t.MaxCharCount = v; });
        public readonly static UnaryType<int> mc_ = new((v, e) => { if (e is UIText t) t.MaxCharCount = v; });
        public readonly static ValueStyle text_align_left = new(e => { if (e is UIText t) t.TextAlign = TextAlign.Left; });
        public readonly static ValueStyle text_align_center = new(e => { if (e is UIText t) t.TextAlign = TextAlign.Center; });
        public readonly static ValueStyle text_align_right = new(e => { if (e is UIText t) t.TextAlign = TextAlign.Right; });

        // LAYOUT BEHAVIOR CLASSES
        // =============================================================================
        public readonly static ValueStyle ignore_invisible = new(e => { if (e is UICol col) col.SetIgnoreInvisibleElements(true); });
        public readonly static ValueStyle include_invisible = new(e => { if (e is UICol col) col.SetIgnoreInvisibleElements(false); });
        public readonly static ValueStyle allow_scrolling_to_top = new(e => { if (e is UICol col) col.SetAllowScrollingToTop(true); });
        public readonly static ValueStyle block_scrolling_to_top = new(e => { if (e is UICol col) col.SetAllowScrollingToTop(false); });
        public readonly static UnaryType<float> scroll_speed_ = new((v, e) => { if (e is UICol col) col.SetScrollingSpeed(v); });

        public readonly static ValueStyle grow_children = new(e => { if (e is UICol col) col.GrowFromChildren = true; });
        public readonly static ValueStyle fixed_size = new(e => { if (e is UICol col) col.GrowFromChildren = false; });
        public readonly static ValueStyle fit_children = new(e => { if (e is UICol col) col.FitChildren = true; });

        public readonly static ValueStyle mask_children = new(e => { if (e is UICol col) col.SetMaskChildren(true); });
        public readonly static ValueStyle no_mask = new(e => { if (e is UICol col) col.SetMaskChildren(false); });

        public readonly static ValueStyle allow_passing_mouse = new(e => e.AllowPassingMouse = true);

        public readonly static UnaryType<int> texture_ = new((v, e) => { if (e is UIPanel p) p.TextureID = v; });
        public readonly static UnaryType<int> icon_ = new((v, e) => { if (e is UIPanel p) p.TextureID = v | 0x20000000; });
        public readonly static UnaryType<string> item_ = new((name, e) =>
        {
            if (ItemDataManager.AllItems.TryGetValue(name, out var item) && e is UIPanel panel)
                panel.TextureID = item.Index | 0x40000000;  
        });

        public readonly static ValueStyle light_round = new(e => { if (e is UIPanel p) { p.TextureID = 0; p.Slice = SLICE_100; }});
        public readonly static ValueStyle dark_round = new(e => { if (e is UIPanel p) { p.TextureID = 1; p.Slice = SLICE_100; }});
        public readonly static ValueStyle blank_round = new(e => { if (e is UIPanel p) { p.TextureID = 2; p.Slice = SLICE_100; }});

        public readonly static ValueStyle light_sharp = new(e => { if (e is UIPanel p) { p.TextureID = 10; p.Slice = SLICE_100; }});
        public readonly static ValueStyle dark_sharp = new(e => { if (e is UIPanel p) { p.TextureID = 11; p.Slice = SLICE_100; }});
        public readonly static ValueStyle blank_sharp = new(e => { if (e is UIPanel p) { p.TextureID = 12; p.Slice = SLICE_100; }});

        public readonly static ValueStyle light_full = new(e => { if (e is UIPanel p) { p.TextureID = 20; p.Slice = SLICE_100; }});
        public readonly static ValueStyle dark_full = new(e => { if (e is UIPanel p) { p.TextureID = 21; p.Slice = SLICE_100; }});
        public readonly static ValueStyle blank_full = new(e => { if (e is UIPanel p) { p.TextureID = 22; p.Slice = SLICE_100; }});

        /// <summary>
        /// (left, top, right, bottom)
        /// </summary>
        public readonly static QuaternaryStyle<float, float, float, float> border_ui_ = new((x, y, z, w, e) => { if (e is UIPanel p) { p.BorderUI = (x, y, z, w);} });
        public readonly static QuaternaryStyle<float, float, float, float> border_rgba_ = new((r, g, b, a, e) => { if (e is UIPanel p) { p.BorderColor = (r, g, b, a);} });
        public readonly static UnaryType<Vector4> border_color_ = new((color, e) => { if (e is UIPanel p) { p.BorderColor = color; } });
        public readonly static BinaryType<float, int> border_color_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.BorderColor = new(new(value), 1f); } },
            (value, e) => { if (e is UIPanel p) { p.BorderColor = new(new(((float)value) / 100f), 1f); } });

        
        public readonly static BinaryType<float, int> light_round_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 0; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 0; p.Slice = SLICE_100;} });
        public readonly static BinaryType<float, int> dark_round_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 1; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 1; p.Slice = SLICE_100; } });
        public readonly static BinaryType<float, int> blank_round_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 2; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 2; p.Slice = SLICE_100; } });


        public readonly static BinaryType<float, int> light_sharp_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 10; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 10; p.Slice = SLICE_100; } });
        public readonly static BinaryType<float, int> dark_sharp_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 11; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 11; p.Slice = SLICE_100; } });
        public readonly static BinaryType<float, int> blank_sharp_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 12; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 12; p.Slice = SLICE_100; } });

        public readonly static BinaryType<float, int> light_full_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 20; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 20; p.Slice = SLICE_100; } });
        public readonly static BinaryType<float, int> dark_full_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 21; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 21; p.Slice = SLICE_100; } });
        public readonly static BinaryType<float, int> blank_full_g_ = new(
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(value), 1f); p.TextureID = 22; p.Slice = SLICE_100; } },
            (value, e) => { if (e is UIPanel p) { p.Color = new(new(((float)value) / 100f), 1f); p.TextureID = 22; p.Slice = SLICE_100; } });

        public readonly static ValueStyle slice_null = new(e => { if (e is UIPanel p) { p.Slice = (-1, -1); } });
        public readonly static ValueStyle slice_75 = new(e => { if (e is UIPanel p) { p.Slice = SLICE_75; } });
        public readonly static ValueStyle slice_100 = new(e => { if (e is UIPanel p) { p.Slice = SLICE_100; } });
        public readonly static UnaryType<Vector2> slice_ = new((v, e) => { if (e is UIPanel p) { p.Slice = v; } });

        public readonly static BinaryStyle<string, object> data_ = new((key, value, e) => e.Dataset[key] = value);
        public readonly static UnaryType<float> depth_ = new((v, e) => e.Depth = v);


        public readonly static UnaryType<int> graph_points_ = new((v, e) => { if (e is UIGraph graph) graph.PointCount = v; });
    }

    
}