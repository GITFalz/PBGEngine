using PBG.MathLibrary;
using static PBG.UI.UIHelper;

namespace PBG.UI
{    
    public static partial class Styles
    {
        public static UIStyleTripleFactory<int, uint, float> w_ = new UIStyleTripleFactory<int, uint, float>("w-", (value1, style) => style.width(value1.Px()), (value2, style) => style.width(((int)value2).Px()), (value3, style) => style.width(value3.Pc()));
        public static UIStyleFactory<int> w_px_ = new UIStyleFactory<int>("w-", (value, style) => style.width(value));
        public static UIStyleFactory<float> w_pc_ = new UIStyleFactory<float>("w-", (value, style) => style.width(value));
        public static UIStyleData w_full => new UIStyleData("w-full").width(100.Pc());
        public static UIStyleData w_half => new UIStyleData("w-half").width(50.Pc());
        public static UIStyleFactory<float, int> w_minus_ = new UIStyleFactory<float, int>("w-minus-", (value1, value2, style) => style.width(value1.Pc(-value2)));
        public static UIStyleFactory<float, int> w_plus_ = new UIStyleFactory<float, int>("w-plus-", (value1, value2, style) => style.width(value1.Pc(value2)));
        public static UIStyleFactory<float> w_full_minus_ = new UIStyleFactory<float>("w-full-minus-", (value, style) => style.width(100.Pc(-value)));
        public static UIStyleFactory<float> w_full_plus_ = new UIStyleFactory<float>("w-full-plus-", (value, style) => style.width(100.Pc(value)));
        public static UIStyleFactory<float> w_half_minus_ = new UIStyleFactory<float>("w-half-minus-", (value, style) => style.width(50.Pc(-value)));
        public static UIStyleFactory<float> w_half_plus_ = new UIStyleFactory<float>("w-half-plus-", (value, style) => style.width(50.Pc(value)));

        public static UIStyleTripleFactory<int, uint, float> h_ = new UIStyleTripleFactory<int, uint, float>("h-", (value1, style) => style.height(value1.Px()), (value2, style) => style.height(((int)value2).Px()), (value3, style) => style.height(value3.Pc()));
        public static UIStyleFactory<int> h_px_ = new UIStyleFactory<int>("h-", (value, style) => style.height(value));
        public static UIStyleFactory<float> h_pc_ = new UIStyleFactory<float>("h-", (value, style) => style.height(value));
        public static UIStyleData h_full => new UIStyleData("h-full").height(100.Pc());
        public static UIStyleData h_half => new UIStyleData("h-half").height(50.Pc());
        public static UIStyleFactory<float, int> h_minus_ = new UIStyleFactory<float, int>("h-minus-", (value1, value2, style) => style.height(value1.Pc(-value2)));
        public static UIStyleFactory<float, int> h_plus_ = new UIStyleFactory<float, int>("h-plus-", (value1, value2, style) => style.height(value1.Pc(value2)));
        public static UIStyleFactory<float> h_full_minus_ = new UIStyleFactory<float>("h-full-minus-", (value, style) => style.height(100.Pc(-value)));
        public static UIStyleFactory<float> h_full_plus_ = new UIStyleFactory<float>("h-full-plus-", (value, style) => style.height(100.Pc(value)));
        public static UIStyleFactory<float> h_half_minus_ = new UIStyleFactory<float>("h-half-minus-", (value, style) => style.height(50.Pc(-value)));
        public static UIStyleFactory<float> h_half_plus_ = new UIStyleFactory<float>("h-half-plus-", (value, style) => style.height(50.Pc(value)));

        public static UIStyleDoubleFactory<int, float> min_w_ = new UIStyleDoubleFactory<int, float>("min-w-", (value1, style) => style.minWidth(value1.Px()), (value2, style) => style.minWidth(value2.Pc()));
        public static UIStyleFactory<int> min_w_px_ = new UIStyleFactory<int>("min-w-", (value, style) => style.minWidth(value));
        public static UIStyleFactory<float> min_w_pc_ = new UIStyleFactory<float>("min-w-", (value, style) => style.minWidth(value));
        public static UIStyleFactory<float, int> min_w_minus_ = new UIStyleFactory<float, int>("min-w-minus-", (value1, value2, style) => style.minWidth(value1.Pc(-value2)));
        public static UIStyleFactory<float, int> min_w_plus_ = new UIStyleFactory<float, int>("min-w-plus-", (value1, value2, style) => style.minWidth(value1.Pc(value2)));
        public static UIStyleData min_w_full => new UIStyleData("min-w-full").minWidth(100f.Pc());
        public static UIStyleFactory<float> min_w_full_minus_ = new UIStyleFactory<float>("min-w-full-minus-", (value, style) => style.minWidth(100.Pc(-value)));
        public static UIStyleFactory<float> min_w_full_plus_ = new UIStyleFactory<float>("min-w-full-plus-", (value, style) => style.minWidth(100.Pc(value)));
        public static UIStyleFactory<float> min_w_half_minus_ = new UIStyleFactory<float>("min-w-half-minus-", (value, style) => style.minWidth(50.Pc(-value)));
        public static UIStyleFactory<float> min_w_half_plus_ = new UIStyleFactory<float>("min-w-half-plus-", (value, style) => style.minWidth(50.Pc(value)));

        public static UIStyleDoubleFactory<int, float> max_w_ = new UIStyleDoubleFactory<int, float>("max-w-", (value1, style) => style.maxWidth(value1.Px()), (value2, style) => style.maxWidth(value2.Pc()));
        public static UIStyleFactory<int> max_w_px_ = new UIStyleFactory<int>("max-w-", (value, style) => style.maxWidth(value));
        public static UIStyleFactory<float> max_w_pc_ = new UIStyleFactory<float>("max-w-", (value, style) => style.maxWidth(value));
        public static UIStyleFactory<float, int> max_w_minus_ = new UIStyleFactory<float, int>("max-w-minus-", (value1, value2, style) => style.maxWidth(value1.Pc(-value2)));
        public static UIStyleFactory<float, int> max_w_plus_ = new UIStyleFactory<float, int>("max-w-plus-", (value1, value2, style) => style.maxWidth(value1.Pc(value2)));
        public static UIStyleData max_w_full => new UIStyleData("max-w-full").maxWidth(100f.Pc());
        public static UIStyleFactory<float> max_w_full_minus_ = new UIStyleFactory<float>("max-w-full-minus-", (value, style) => style.maxWidth(100.Pc(-value)));
        public static UIStyleFactory<float> max_w_full_plus_ = new UIStyleFactory<float>("max-w-full-plus-", (value, style) => style.maxWidth(100.Pc(value)));
        public static UIStyleFactory<float> max_w_half_minus_ = new UIStyleFactory<float>("max-w-half-minus-", (value, style) => style.maxWidth(50.Pc(-value)));
        public static UIStyleFactory<float> max_w_half_plus_ = new UIStyleFactory<float>("max-w-half-plus-", (value, style) => style.maxWidth(50.Pc(value)));

        public static UIStyleDoubleFactory<int, float> min_h_ = new UIStyleDoubleFactory<int, float>("min-h-", (value1, style) => style.minHeight(value1.Px()), (value2, style) => style.minHeight(value2.Pc()));
        public static UIStyleFactory<int> min_h_px_ = new UIStyleFactory<int>("min-h-", (value, style) => style.minHeight(value));
        public static UIStyleFactory<float> min_h_pc_ = new UIStyleFactory<float>("min-h-", (value, style) => style.minHeight(value));
        public static UIStyleFactory<float, int> min_h_minus_ = new UIStyleFactory<float, int>("min-h-minus-", (value1, value2, style) => style.minHeight(value1.Pc(-value2)));
        public static UIStyleFactory<float, int> min_h_plus_ = new UIStyleFactory<float, int>("min-h-plus-", (value1, value2, style) => style.minHeight(value1.Pc(value2)));
        public static UIStyleData min_h_full => new UIStyleData("min-h-full").minHeight(100f.Pc());
        public static UIStyleFactory<float> min_h_full_minus_ = new UIStyleFactory<float>("min-h-full-minus-", (value, style) => style.minHeight(100.Pc(-value)));
        public static UIStyleFactory<float> min_h_full_plus_ = new UIStyleFactory<float>("min-h-full-plus-", (value, style) => style.minHeight(100.Pc(value)));
        public static UIStyleFactory<float> min_h_half_minus_ = new UIStyleFactory<float>("min-h-half-minus-", (value, style) => style.minHeight(50.Pc(-value)));
        public static UIStyleFactory<float> min_h_half_plus_ = new UIStyleFactory<float>("min-h-half-plus-", (value, style) => style.minHeight(50.Pc(value)));

        public static UIStyleDoubleFactory<int, float> max_h_ = new UIStyleDoubleFactory<int, float>("max-h-", (value1, style) => style.maxHeight(value1.Px()), (value2, style) => style.maxHeight(value2.Pc()));
        public static UIStyleFactory<int> max_h_px_ = new UIStyleFactory<int>("max-h-", (value, style) => style.maxHeight(value));
        public static UIStyleFactory<float> max_h_pc_ = new UIStyleFactory<float>("max-h-", (value, style) => style.maxHeight(value));
        public static UIStyleFactory<float, int> max_h_minus_ = new UIStyleFactory<float, int>("max-h-minus-", (value1, value2, style) => style.maxHeight(value1.Pc(-value2)));
        public static UIStyleFactory<float, int> max_h_plus_ = new UIStyleFactory<float, int>("max-h-plus-", (value1, value2, style) => style.maxHeight(value1.Pc(value2)));
        public static UIStyleData max_h_full => new UIStyleData("max-h-full").maxHeight(100f.Pc());
        public static UIStyleFactory<float> max_h_full_minus_ = new UIStyleFactory<float>("max-h-full-minus-", (value, style) => style.maxHeight(100.Pc(-value)));
        public static UIStyleFactory<float> max_h_full_plus_ = new UIStyleFactory<float>("max-h-full-plus-", (value, style) => style.maxHeight(100.Pc(value)));
        public static UIStyleFactory<float> max_h_half_minus_ = new UIStyleFactory<float>("max-h-half-minus-", (value, style) => style.maxHeight(50.Pc(-value)));
        public static UIStyleFactory<float> max_h_half_plus_ = new UIStyleFactory<float>("max-h-half-plus-", (value, style) => style.maxHeight(50.Pc(value)));

        public static UIStyleFactory<float> left_ = new UIStyleFactory<float>("left-", (value, style) => style.leftOffset(value));
        public static UIStyleFactory<float> top_ = new UIStyleFactory<float>("top-", (value, style) => style.topOffset(value));
        public static UIStyleFactory<float> right_ = new UIStyleFactory<float>("right-", (value, style) => style.rightOffset(value));
        public static UIStyleFactory<float> bottom_ = new UIStyleFactory<float>("bottom-", (value, style) => style.bottomOffset(value));

        public static UIStyleData top_left => new UIStyleData("top-left").align(UIAlign.TopLeft);
        public static UIStyleData top_center => new UIStyleData("top-center").align(UIAlign.TopCenter);
        public static UIStyleData top_right => new UIStyleData("top-right").align(UIAlign.TopRight);
        public static UIStyleData middle_left => new UIStyleData("middle-left").align(UIAlign.MiddleLeft);
        public static UIStyleData middle_center => new UIStyleData("middle-center").align(UIAlign.MiddleCenter);
        public static UIStyleData middle_right => new UIStyleData("middle-right").align(UIAlign.MiddleRight);
        public static UIStyleData bottom_left => new UIStyleData("bottom-left").align(UIAlign.BottomLeft);
        public static UIStyleData bottom_center => new UIStyleData("bottom-center").align(UIAlign.BottomCenter);
        public static UIStyleData bottom_right => new UIStyleData("bottom-right").align(UIAlign.BottomRight);

        public static UIStyleData justify_start => new UIStyleData("justify-start").align(UIAlign.MiddleLeft);
        public static UIStyleData justify_center => new UIStyleData("justify-center").align(UIAlign.MiddleCenter);
        public static UIStyleData justify_end => new UIStyleData("justify-end").align(UIAlign.MiddleRight);
        public static UIStyleData items_start => new UIStyleData("items-start").align(UIAlign.TopCenter);
        public static UIStyleData items_center => new UIStyleData("items-center").align(UIAlign.MiddleCenter);
        public static UIStyleData items_end => new UIStyleData("items-end").align(UIAlign.BottomCenter);

        // VISIBILITY CLASSES
        // =============================================================================
        public static UIStyleData visible => new UIStyleData("visible").visible(true);
        public static UIStyleData invisible => new UIStyleData("invisible").visible(false);
        public static UIStyleData hidden => new UIStyleData("hidden").visible(false);
        public static UIStyleData not_toggle_old_invisible => new UIStyleData("force-toggle-visible").forceToggleVisible(false);

        // COLOR CLASSES
        // =============================================================================
        // Gray
        public static UIStyleDoubleFactory<float, int> gray_ = new UIStyleDoubleFactory<float, int>("gray-",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)));
        public static UIStyleFactory<float, float, float> rgb_ = new UIStyleFactory<float, float, float>("c-", (r, g, b, style) => style.color(new Vector4(r, g, b, 1f)));
        public static UIStyleFactory<Vector3> rgb_v3_ = new UIStyleFactory<Vector3>("c-", (color, style) => style.color(new Vector4(color.X, color.Y, color.Z, 1f)));
        public static UIStyleFactory<float, float, float, float> rgba_ = new UIStyleFactory<float, float, float, float>("a-", (r, g, b, a, style) => style.color(new Vector4(r, g, b, a)));
        public static UIStyleFactory<Vector4> rgba_v4_ = new UIStyleFactory<Vector4>("a-", (color, style) => style.color(color));
        public static UIStyleData transparent => new UIStyleData("transparent").color(TRANSPARENT);
        public static UIStyleFactory<float, float, float> background_rgb_ = new UIStyleFactory<float, float, float>("background-color-", (r, g, b, style) => style.backgroundColor(new Vector4(r, g, b, 1f)));
        public static UIStyleFactory<float, float, float, float> background_rgba_ = new UIStyleFactory<float, float, float, float>("background-color-", (r, g, b, a, style) => style.backgroundColor(new Vector4(r, g, b, a)));


        

        // Accent colors
        public static UIStyleData bg_blue => new UIStyleData("bg-blue").color(ACCENT_BLUE);
        public static UIStyleData bg_blue_hover => new UIStyleData("bg-blue-hover").color(ACCENT_BLUE_HOVER);
        public static UIStyleData bg_red => new UIStyleData("bg-red").color(ACCENT_RED);
        public static UIStyleData bg_green => new UIStyleData("bg-green").color(ACCENT_GREEN);
        public static UIStyleData bg_orange => new UIStyleData("bg-orange").color(ACCENT_ORANGE);
        public static UIStyleData bg_purple => new UIStyleData("bg-purple").color(ACCENT_PURPLE);
        public static UIStyleData bg_yellow => new UIStyleData("bg-yellow").color(ACCENT_YELLOW);

        // Special colors
        public static UIStyleData bg_transparent => new UIStyleData("bg-transparent").color(TRANSPARENT);
        public static UIStyleData bg_black => new UIStyleData("bg-black").color(BLACK);
        public static UIStyleData bg_white => new UIStyleData("bg-white").color(WHITE);

        // Text colors (same naming convention)
        public static UIStyleData text_gray_000 => new UIStyleData("text-gray-000").color(GRAY_000);
        public static UIStyleData text_gray_050 => new UIStyleData("text-gray-050").color(GRAY_050);
        public static UIStyleData text_gray_100 => new UIStyleData("text-gray-100").color(GRAY_100);
        public static UIStyleData text_blue => new UIStyleData("text-blue").color(ACCENT_BLUE);
        public static UIStyleData text_red => new UIStyleData("text-red").color(ACCENT_RED);
        public static UIStyleData text_green => new UIStyleData("text-green").color(ACCENT_GREEN);
        public static UIStyleData text_white => new UIStyleData("text-white").color(WHITE);
        public static UIStyleData text_black => new UIStyleData("text-black").color(BLACK);

        // TEXTURE CLASSES
        // =============================================================================

        // Common UI element textures with semantic names
        public static UIStyleData bg_panel => new UIStyleData("bg-panel").texture(0).slice(SLICE_75);
        public static UIStyleData bg_input => new UIStyleData("bg-input").texture(1).slice(SLICE_75);
        public static UIStyleData bg_card => new UIStyleData("bg-card").texture(2).slice(SLICE_75);
        public static UIStyleData bg_button => new UIStyleData("bg-button").texture(0).slice(SLICE_75);
        public static UIStyleData bg_modal => new UIStyleData("bg-modal").texture(10).slice(SLICE_100);

        // SPACING CLASSES
        // =============================================================================
        public static UIStyleFactory<float> spacing_ = new UIStyleFactory<float>("spacing-", (value, style) => style.spacing(value));

        // BORDER CLASSES
        // =============================================================================
        public static UIStyleFactory<float, float, float, float> border_ = new UIStyleFactory<float, float, float, float>("border-", (left, right, top, bottom, style) => style.border((left, right, top, bottom)));
        // Individual border sides
        public static UIStyleFactory<float> border_left_ = new UIStyleFactory<float>("border-l-", (value, style) => style.leftBorder(value));
        public static UIStyleFactory<float> border_right_ = new UIStyleFactory<float>("border-r-", (value, style) => style.rightBorder(value));
        public static UIStyleFactory<float> border_top_ = new UIStyleFactory<float>("border-t-", (value, style) => style.topBorder(value));
        public static UIStyleFactory<float> border_bottom_ = new UIStyleFactory<float>("border-b-", (value, style) => style.bottomBorder(value));

        // TYPOGRAPHY CLASSES (fontSize: 1 = 10px, 2 = 20px, so linear scaling)
        // =============================================================================
        public static UIStyleFactory<float> font_size_ = new UIStyleFactory<float>("font-size-", (value, style) => style.fontSize(value));
        public static UIStyleFactory<float> fs_ = new UIStyleFactory<float>("font-size-", (value, style) => style.fontSize(value));

        // Text character limits
        public static UIStyleFactory<int> max_chars_ = new UIStyleFactory<int>("max-chars-", (value, style) => style.maxChars(value));
        public static UIStyleFactory<int> mc_ = new UIStyleFactory<int>("max-chars-", (value, style) => style.maxChars(value));
        public static UIStyleData text_align_left => new UIStyleData("text-align-left").textAlign(TextAlign.Left);
        public static UIStyleData text_align_center => new UIStyleData("text-align-center").textAlign(TextAlign.Center);
        public static UIStyleData text_align_right => new UIStyleData("text-align-right").textAlign(TextAlign.Right);

        // LAYOUT BEHAVIOR CLASSES
        // =============================================================================
        public static UIStyleData ignore_invisible => new UIStyleData("ignore-invisible").ignoreInvisibleElements(true);
        public static UIStyleData include_invisible => new UIStyleData("include-invisible").ignoreInvisibleElements(false);
        public static UIStyleData allow_scrolling_to_top => new UIStyleData("allow-scrolling-to-top").allowScrollingToTop(true);
        public static UIStyleData block_scrolling_to_top => new UIStyleData("block-scrolling-to-top").allowScrollingToTop(false);
        public static UIStyleFactory<float> scroll_speed_ = new UIStyleFactory<float>("scroll-speed-", (value, style) => style.scrollingSpeed(value));

        public static UIStyleData grow_children => new UIStyleData("grow-children").growFromChildren(true);
        public static UIStyleData fixed_size => new UIStyleData("fixed-size").growFromChildren(false);

        public static UIStyleData mask_children => new UIStyleData("mask-children").maskChildren(true);
        public static UIStyleData no_mask => new UIStyleData("no-mask").maskChildren(false);

        public static UIStyleData allow_passing_mouse => new UIStyleData("allow-passing-mouse").allowPassingMouse(true);

        // COMPOSITE UTILITY CLASSES
        // =============================================================================
        // Common button styles
        public static UIStyleData btn_primary => new UIStyleData("btn-primary")
            .texture(0).slice(SLICE_75)
            .color(ACCENT_BLUE)
            .align(UIAlign.MiddleCenter)
            .fontSize(1.0f) // 10px
            .width(100)
            .height(32);

        public static UIStyleData btn_secondary => new UIStyleData("btn-secondary")
            .texture(1).slice(SLICE_75)
            .color(GRAY_050)
            .align(UIAlign.MiddleCenter)
            .fontSize(1.0f) // 10px
            .width(100)
            .height(32);

        public static UIStyleData btn_danger => new UIStyleData("btn-danger")
            .texture(0).slice(SLICE_75)
            .color(ACCENT_RED)
            .align(UIAlign.MiddleCenter)
            .fontSize(1.0f) // 10px
            .width(100)
            .height(32);

        public static UIStyleFactory<int> texture_ = new UIStyleFactory<int>("texture-", (value, style) => style.texture(value));
        public static UIStyleFactory<int> icon_ = new UIStyleFactory<int>("icon-", (value, style) => style.texture(value | 0x20000000));
        public static UIStyleFactory<string> item_ = new UIStyleFactory<string>("item-", (value, style) =>
        {
            if (!ItemDataManager.AllItems.TryGetValue(value, out var item))
                return new();

            return style.texture(item.Index | 0x40000000);
        });

        public static UIStyleData light_round => new UIStyleData("light-round-").texture(0).slice(SLICE_100);
        public static UIStyleData dark_round => new UIStyleData("dark-round-").texture(1).slice(SLICE_100);
        public static UIStyleData blank_round => new UIStyleData("blank-round-").texture(2).slice(SLICE_100);

        public static UIStyleData light_sharp => new UIStyleData("light-sharp-").texture(10).slice(SLICE_100);
        public static UIStyleData dark_sharp => new UIStyleData("dark-sharp-").texture(11).slice(SLICE_100);
        public static UIStyleData blank_sharp => new UIStyleData("blank-sharp-").texture(12).slice(SLICE_100);

        public static UIStyleData light_full => new UIStyleData("light-full-").texture(20).slice(SLICE_100);
        public static UIStyleData dark_full => new UIStyleData("dark-full-").texture(21).slice(SLICE_100);
        public static UIStyleData blank_full => new UIStyleData("blank-full-").texture(22).slice(SLICE_100);

        public static UIStyleFactory<float, float, float, float> border_ui_ = new UIStyleFactory<float, float, float, float>("border-ui-", (x, y, z, w, style) => style.borderUI(x, y, z, w));
        public static UIStyleFactory<float, float, float, float> border_rgba_ = new UIStyleFactory<float, float, float, float>("border-color-", (r, g, b, a, style) => style.borderColor(r, g, b, a));
        public static UIStyleFactory<Vector4> border_color_ = new UIStyleFactory<Vector4>("border-color-", (color, style) => style.borderColor(color));
        public static UIStyleDoubleFactory<float, int> border_color_g_ = new UIStyleDoubleFactory<float, int>("border-color-g-",
            (value, style) => style.borderColor(new Vector4(new Vector3(value), 1f)),
            (value, style) => style.borderColor(new Vector4(new Vector3(((float)value) / 100f), 1f)));

        
        public static UIStyleDoubleFactory<float, int> light_round_g_ = new UIStyleDoubleFactory<float, int>("light-round-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(0).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(0).slice(SLICE_100));
        public static UIStyleDoubleFactory<float, int> dark_round_g_ = new UIStyleDoubleFactory<float, int>("dark-round-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(1).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(1).slice(SLICE_100));
        public static UIStyleDoubleFactory<float, int> blank_round_g_ = new UIStyleDoubleFactory<float, int>("blank-round-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(2).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(2).slice(SLICE_100));


        public static UIStyleDoubleFactory<float, int> light_sharp_g_ = new UIStyleDoubleFactory<float, int>("light-sharp-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(10).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(10).slice(SLICE_100));
        public static UIStyleDoubleFactory<float, int> dark_sharp_g_ = new UIStyleDoubleFactory<float, int>("dark-sharp-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(11).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(11).slice(SLICE_100));
        public static UIStyleDoubleFactory<float, int> blank_sharp_g_ = new UIStyleDoubleFactory<float, int>("blank-sharp-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(12).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(12).slice(SLICE_100));

        public static UIStyleDoubleFactory<float, int> light_full_g_ = new UIStyleDoubleFactory<float, int>("light-full-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(20).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(20).slice(SLICE_100));
        public static UIStyleDoubleFactory<float, int> dark_full_g_ = new UIStyleDoubleFactory<float, int>("dark-full-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(21).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(21).slice(SLICE_100));
        public static UIStyleDoubleFactory<float, int> blank_full_g_ = new UIStyleDoubleFactory<float, int>("blank-full-g",
            (value, style) => style.color(new Vector4(new Vector3(value), 1f)).texture(22).slice(SLICE_100),
            (value, style) => style.color(new Vector4(new Vector3(((float)value) / 100f), 1f)).texture(22).slice(SLICE_100));

        // Common card styles
        public static UIStyleData card => new UIStyleData("card")
            .texture(2).slice(SLICE_75)
            .border(new Vector4(1, 1, 1, 1))
            .color(GRAY_010)
            .width(200)
            .height(150);

        public static UIStyleData card_dark => new UIStyleData("card-dark")
            .texture(1).slice(SLICE_75)
            .border(new Vector4(1, 1, 1, 1))
            .color(GRAY_080)
            .width(200)
            .height(150);

        // Input field styles
        public static UIStyleData input_field => new UIStyleData("input-field")
            .texture(1).slice(SLICE_75)
            .color(GRAY_005)
            .fontSize(1.0f) // 10px
            .align(UIAlign.MiddleLeft)
            .border(new Vector4(1, 1, 1, 1))
            .width(200)
            .height(32);

        // Modal/dialog styles
        public static UIStyleData modal_backdrop => new UIStyleData("modal-backdrop")
            .color(new Vector4(0, 0, 0, 0.5f))
            .width(100f)
            .height(100f);

        public static UIStyleData modal_content => new UIStyleData("modal-content")
            .texture(2).slice(SLICE_100)
            .color(WHITE)
            .border(new Vector4(2, 2, 2, 2))
            .align(UIAlign.MiddleCenter)
            .width(400)
            .height(300);

        // Panel styles
        public static UIStyleData panel => new UIStyleData("panel")
            .texture(0).slice(SLICE_75)
            .color(GRAY_020)
            .border(new Vector4(1, 1, 1, 1))
            .width(300)
            .height(200);

        public static UIStyleData sidebar => new UIStyleData("sidebar")
            .texture(10).slice(SLICE_75)
            .color(GRAY_080)
            .height(100f)
            .width(200);

        // Flex-like utilities
        public static UIStyleData flex_col => new UIStyleData("flex-col")
            .growFromChildren(true)
            .spacing(4)
            .width(100)
            .height(100);

        public static UIStyleData flex_row => new UIStyleData("flex-row")
            .growFromChildren(true)
            .spacing(4)
            .width(100)
            .height(100);

        // Container utilities
        public static UIStyleData container => new UIStyleData("container")
            .maxWidth(1200)
            .align(UIAlign.MiddleCenter)
            .width(100f)
            .height(100);

        public static UIStyleData container_sm => new UIStyleData("container-sm")
            .maxWidth(640)
            .align(UIAlign.MiddleCenter)
            .width(100f)
            .height(100);

        public static UIStyleData container_lg => new UIStyleData("container-lg")
            .maxWidth(1024)
            .align(UIAlign.MiddleCenter)
            .width(100f)
            .height(100);

        // Screen overlay
        public static UIStyleData overlay => new UIStyleData("overlay")
            .color(new Vector4(0, 0, 0, 0.75f))
            .width(100f)
            .height(100f);

        // Notification styles
        public static UIStyleData notification_success => new UIStyleData("notification-success")
            .texture(0).slice(SLICE_75)
            .color(ACCENT_GREEN)
            .fontSize(0.8f) // 8px
            .border(new Vector4(1, 1, 1, 1))
            .width(250)
            .height(60);

        public static UIStyleData notification_error => new UIStyleData("notification-error")
            .texture(0).slice(SLICE_75)
            .color(ACCENT_RED)
            .fontSize(0.8f) // 8px
            .border(new Vector4(1, 1, 1, 1))
            .width(250)
            .height(60);

        public static UIStyleData notification_warning => new UIStyleData("notification-warning")
            .texture(0).slice(SLICE_75)
            .color(ACCENT_ORANGE)
            .fontSize(0.8f) // 8px
            .border(new Vector4(1, 1, 1, 1))
            .width(250)
            .height(60);


        public static UIStyleData slice_null => new UIStyleData("slice-null").slice((-1, -1));
        public static UIStyleData slice_75 => new UIStyleData("slice-75").slice(SLICE_75);
        public static UIStyleData slice_100 => new UIStyleData("slice-100").slice(SLICE_100);

        public static UIStyleFactory<string, object> data_ = new UIStyleFactory<string, object>("data-", (key, value, style) => style.data(key, value));
        public static UIStyleFactory<float> depth_ = new UIStyleFactory<float>("depth-", (value, style) => style.depth(value));
    }
}