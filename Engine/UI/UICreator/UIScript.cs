
using PBG.Core;
using PBG.Rendering;
using static PBG.UI.Styles;

namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {   
        public bool Created { get; private set; } = false;
        public UIController UIController => Element.UIController!;
        public UIElementBase Element = null!;
        public Scene Scene = Scene.CurrentlyLoadingScene ?? Scene.CurrentScene!;
        public Camera Camera = Scene.CurrentlyLoadingScene?.DefaultCamera ?? Scene.CurrentScene?.DefaultCamera!;
        public UIScript()
        {
            PreScript();
            Element = Script();
            Created = true;
            AfterScript();
        }

        public static implicit operator UIElementBase(UIScript script) => script.Element;
        public static implicit operator UIController(UIScript script) => script.UIController;
        public virtual void PreScript() {}
        public abstract UIElementBase Script();
        public virtual void AfterScript() {}
        
        public static Class Class(params UIStyleData[] styles) => new(styles);

        public static OnClickEvent<TSelf> OnClick<TSelf>(Action<TSelf>? action) where TSelf : UIElement<TSelf> => new(action);
        public static OnHoverEnterEvent<TSelf> OnHoverEnter<TSelf>(Action<TSelf>? action) where TSelf : UIElement<TSelf> => new(action);
        public static OnHoverEvent<TSelf> OnHover<TSelf>(Action<TSelf>? action) where TSelf : UIElement<TSelf> => new(action);
        public static OnHoldEvent<TSelf> OnHold<TSelf>(Action<TSelf>? action) where TSelf : UIElement<TSelf> => new(action);
        public static OnReleaseEvent<TSelf> OnRelease<TSelf>(Action<TSelf>? action) where TSelf : UIElement<TSelf> => new(action);
        public static OnHoverExitEvent<TSelf> OnHoverExit<TSelf>(Action<TSelf>? action) where TSelf : UIElement<TSelf> => new(action);

        public static OnClickEvent<UIElement> OnClick(Action<UIElementBase>? action) => new(action);
        public static OnHoverEnterEvent<UIElement> OnHoverEnter(Action<UIElementBase>? action) => new(action);
        public static OnHoverEvent<UIElement> OnHover(Action<UIElementBase>? action) => new(action);
        public static OnHoldEvent<UIElement> OnHold(Action<UIElementBase>? action) => new(action);
        public static OnReleaseEvent<UIElement> OnRelease(Action<UIElementBase>? action) => new(action);
        public static OnHoverExitEvent<UIElement> OnHoverExit(Action<UIElementBase>? action) => new(action);

        public static OnClickEvent<UIImg> OnClickImg(Action<UIImg>? action) => new(action);
        public static OnHoverEnterEvent<UIImg> OnHoverEnterImg(Action<UIImg>? action) => new(action);
        public static OnHoverEvent<UIImg> OnHoverImg(Action<UIImg>? action) => new(action);
        public static OnHoldEvent<UIImg> OnHoldImg(Action<UIImg>? action) => new(action);
        public static OnReleaseEvent<UIImg> OnReleaseImg(Action<UIImg>? action) => new(action);
        public static OnHoverExitEvent<UIImg> OnHoverExitImg(Action<UIImg>? action) => new(action);

        public static OnClickEvent<UIButton> OnClickButton(Action<UIButton>? action) => new(action);
        public static OnHoverEnterEvent<UIButton> OnHoverEnterButton(Action<UIButton>? action) => new(action);
        public static OnHoverEvent<UIButton> OnHoverButton(Action<UIButton>? action) => new(action);
        public static OnHoldEvent<UIButton> OnHoldButton(Action<UIButton>? action) => new(action);
        public static OnReleaseEvent<UIButton> OnReleaseButton(Action<UIButton>? action) => new(action);
        public static OnHoverExitEvent<UIButton> OnHoverExitButton(Action<UIButton>? action) => new(action);

        public static OnClickEvent<UICol> OnClickCol(Action<UICol>? action) => new(action);
        public static OnHoverEnterEvent<UICol> OnHoverEnterCol(Action<UICol>? action) => new(action);
        public static OnHoverEvent<UICol> OnHoverCol(Action<UICol>? action) => new(action);
        public static OnHoldEvent<UICol> OnHoldCol(Action<UICol>? action) => new(action);
        public static OnReleaseEvent<UICol> OnReleaseCol(Action<UICol>? action) => new(action);
        public static OnHoverExitEvent<UICol> OnHoverExitCol(Action<UICol>? action) => new(action);

        public static OnClickEvent<UIVCol> OnClickVCol(Action<UIVCol>? action) => new(action);
        public static OnHoverEnterEvent<UIVCol> OnHoverEnterVCol(Action<UIVCol>? action) => new(action);
        public static OnHoverEvent<UIVCol> OnHoverVCol(Action<UIVCol>? action) => new(action);
        public static OnHoldEvent<UIVCol> OnHoldVCol(Action<UIVCol>? action) => new(action);
        public static OnReleaseEvent<UIVCol> OnReleaseVCol(Action<UIVCol>? action) => new(action);
        public static OnHoverExitEvent<UIVCol> OnHoverExitVCol(Action<UIVCol>? action) => new(action);

        public static OnClickEvent<UIHCol> OnClickHCol(Action<UIHCol>? action) => new(action);
        public static OnHoverEnterEvent<UIHCol> OnHoverEnterHCol(Action<UIHCol>? action) => new(action);
        public static OnHoverEvent<UIHCol> OnHoverHCol(Action<UIHCol>? action) => new(action);
        public static OnHoldEvent<UIHCol> OnHoldHCol(Action<UIHCol>? action) => new(action);
        public static OnReleaseEvent<UIHCol> OnReleaseHCol(Action<UIHCol>? action) => new(action);
        public static OnHoverExitEvent<UIHCol> OnHoverExitHCol(Action<UIHCol>? action) => new(action);

        public static OnClickEvent<UIText> OnClickText(Action<UIText>? action) => new(action);
        public static OnHoverEnterEvent<UIText> OnHoverEnterText(Action<UIText>? action) => new(action);
        public static OnHoverEvent<UIText> OnHoverText(Action<UIText>? action) => new(action);
        public static OnHoldEvent<UIText> OnHoldText(Action<UIText>? action) => new(action);
        public static OnReleaseEvent<UIText> OnReleaseText(Action<UIText>? action) => new(action);
        public static OnHoverExitEvent<UIText> OnHoverExitText(Action<UIText>? action) => new(action);

        public static OnClickEvent<UIField> OnClickField(Action<UIField>? action) => new(action);
        public static OnHoverEnterEvent<UIField> OnHoverEnterField(Action<UIField>? action) => new(action);
        public static OnHoverEvent<UIField> OnHoverField(Action<UIField>? action) => new(action);
        public static OnHoldEvent<UIField> OnHoldField(Action<UIField>? action) => new(action);
        public static OnReleaseEvent<UIField> OnReleaseField(Action<UIField>? action) => new(action);
        public static OnHoverExitEvent<UIField> OnHoverExitField(Action<UIField>? action) => new(action);

        public static OnClickEvent<UIVScroll> OnClickVScroll(Action<UIVScroll>? action) => new(action);
        public static OnHoverEnterEvent<UIVScroll> OnHoverEnterVScroll(Action<UIVScroll>? action) => new(action);
        public static OnHoverEvent<UIVScroll> OnHoverVScroll(Action<UIVScroll>? action) => new(action);
        public static OnHoldEvent<UIVScroll> OnHoldVScroll(Action<UIVScroll>? action) => new(action);
        public static OnReleaseEvent<UIVScroll> OnReleaseVScroll(Action<UIVScroll>? action) => new(action);
        public static OnHoverExitEvent<UIVScroll> OnHoverExitVScroll(Action<UIVScroll>? action) => new(action);

        public static OnClickEvent<UIHScroll> OnClickHScroll(Action<UIHScroll>? action) => new(action);
        public static OnHoverEnterEvent<UIHScroll> OnHoverEnterHScroll(Action<UIHScroll>? action) => new(action);
        public static OnHoverEvent<UIHScroll> OnHoverHScroll(Action<UIHScroll>? action) => new(action);
        public static OnHoldEvent<UIHScroll> OnHoldHScroll(Action<UIHScroll>? action) => new(action);
        public static OnReleaseEvent<UIHScroll> OnReleaseHScroll(Action<UIHScroll>? action) => new(action);
        public static OnHoverExitEvent<UIHScroll> OnHoverExitHScroll(Action<UIHScroll>? action) => new(action);

        public static OnClickEvent<UIImg> OnClick(Action<UIImg>? action) => new(action);
        public static OnHoverEnterEvent<UIImg> OnHoverEnter(Action<UIImg>? action) => new(action);
        public static OnHoverEvent<UIImg> OnHover(Action<UIImg>? action) => new(action);
        public static OnHoldEvent<UIImg> OnHold(Action<UIImg>? action) => new(action);
        public static OnReleaseEvent<UIImg> OnRelease(Action<UIImg>? action) => new(action);
        public static OnHoverExitEvent<UIImg> OnHoverExit(Action<UIImg>? action) => new(action);

        public static OnClickEvent<UIButton> OnClick(Action<UIButton>? action) => new(action);
        public static OnHoverEnterEvent<UIButton> OnHoverEnter(Action<UIButton>? action) => new(action);
        public static OnHoverEvent<UIButton> OnHover(Action<UIButton>? action) => new(action);
        public static OnHoldEvent<UIButton> OnHold(Action<UIButton>? action) => new(action);
        public static OnReleaseEvent<UIButton> OnRelease(Action<UIButton>? action) => new(action);
        public static OnHoverExitEvent<UIButton> OnHoverExit(Action<UIButton>? action) => new(action);

        public static OnClickEvent<UICol> OnClick(Action<UICol>? action) => new(action);
        public static OnHoverEnterEvent<UICol> OnHoverEnter(Action<UICol>? action) => new(action);
        public static OnHoverEvent<UICol> OnHover(Action<UICol>? action) => new(action);
        public static OnHoldEvent<UICol> OnHold(Action<UICol>? action) => new(action);
        public static OnReleaseEvent<UICol> OnRelease(Action<UICol>? action) => new(action);
        public static OnHoverExitEvent<UICol> OnHoverExit(Action<UICol>? action) => new(action);

        public static OnClickEvent<UIVCol> OnClick(Action<UIVCol>? action) => new(action);
        public static OnHoverEnterEvent<UIVCol> OnHoverEnter(Action<UIVCol>? action) => new(action);
        public static OnHoverEvent<UIVCol> OnHover(Action<UIVCol>? action) => new(action);
        public static OnHoldEvent<UIVCol> OnHold(Action<UIVCol>? action) => new(action);
        public static OnReleaseEvent<UIVCol> OnRelease(Action<UIVCol>? action) => new(action);
        public static OnHoverExitEvent<UIVCol> OnHoverExit(Action<UIVCol>? action) => new(action);

        public static OnClickEvent<UIHCol> OnClick(Action<UIHCol>? action) => new(action);
        public static OnHoverEnterEvent<UIHCol> OnHoverEnter(Action<UIHCol>? action) => new(action);
        public static OnHoverEvent<UIHCol> OnHover(Action<UIHCol>? action) => new(action);
        public static OnHoldEvent<UIHCol> OnHold(Action<UIHCol>? action) => new(action);
        public static OnReleaseEvent<UIHCol> OnRelease(Action<UIHCol>? action) => new(action);
        public static OnHoverExitEvent<UIHCol> OnHoverExit(Action<UIHCol>? action) => new(action);

        public static OnClickEvent<UIText> OnClick(Action<UIText>? action) => new(action);
        public static OnHoverEnterEvent<UIText> OnHoverEnter(Action<UIText>? action) => new(action);
        public static OnHoverEvent<UIText> OnHover(Action<UIText>? action) => new(action);
        public static OnHoldEvent<UIText> OnHold(Action<UIText>? action) => new(action);
        public static OnReleaseEvent<UIText> OnRelease(Action<UIText>? action) => new(action);
        public static OnHoverExitEvent<UIText> OnHoverExit(Action<UIText>? action) => new(action);

        public static OnClickEvent<UIField> OnClick(Action<UIField>? action) => new(action);
        public static OnHoverEnterEvent<UIField> OnHoverEnter(Action<UIField>? action) => new(action);
        public static OnHoverEvent<UIField> OnHover(Action<UIField>? action) => new(action);
        public static OnHoldEvent<UIField> OnHold(Action<UIField>? action) => new(action);
        public static OnReleaseEvent<UIField> OnRelease(Action<UIField>? action) => new(action);
        public static OnHoverExitEvent<UIField> OnHoverExit(Action<UIField>? action) => new(action);

        public static OnClickEvent<UIVScroll> OnClick(Action<UIVScroll>? action) => new(action);
        public static OnHoverEnterEvent<UIVScroll> OnHoverEnter(Action<UIVScroll>? action) => new(action);
        public static OnHoverEvent<UIVScroll> OnHover(Action<UIVScroll>? action) => new(action);
        public static OnHoldEvent<UIVScroll> OnHold(Action<UIVScroll>? action) => new(action);
        public static OnReleaseEvent<UIVScroll> OnRelease(Action<UIVScroll>? action) => new(action);
        public static OnHoverExitEvent<UIVScroll> OnHoverExit(Action<UIVScroll>? action) => new(action);

        public static OnClickEvent<UIHScroll> OnClick(Action<UIHScroll>? action) => new(action);
        public static OnHoverEnterEvent<UIHScroll> OnHoverEnter(Action<UIHScroll>? action) => new(action);
        public static OnHoverEvent<UIHScroll> OnHover(Action<UIHScroll>? action) => new(action);
        public static OnHoldEvent<UIHScroll> OnHold(Action<UIHScroll>? action) => new(action);
        public static OnReleaseEvent<UIHScroll> OnRelease(Action<UIHScroll>? action) => new(action);
        public static OnHoverExitEvent<UIHScroll> OnHoverExit(Action<UIHScroll>? action) => new(action);

        public static OnTextChangeEvent<UIField> OnTextChange(Action<UIField>? action) => new(action);
        public static OnTextEnterEvent<UIField> OnTextEnter(Action<UIField>? action)  => new(action);
        public static OnVerticalScrollEvent<UIVScroll> OnVScroll(Action<UIVScroll>? action) => new(action);
        public static OnHorizontalScrollEvent<UIHScroll> OnHScroll(Action<UIHScroll>? action) => new(action);

        public static UIElementBase[] Sub(params UIElementBase[] subElements) => subElements;

        public T? GetElement<T>() where T : UIElementBase => Element.GetElement<T>();
        public T? GetElementAt<T>(int number) where T : UIElementBase => Element.GetElementAt<T>(number);
        public UIElementBase? GetElement(UIElementTag tag) => Element.GetElement(tag);
        public UIElementBase? GetElementAt(UIElementTag tag, int number) => Element.GetElementAt(tag, number);
        public UIElementBase? GetElement(string name) => Element.GetElement(name);
        public UIElementBase? GetElementAt(string name, int number) => Element.GetElementAt(name, number);
        public T? QueryElement<T>() where T : UIElementBase => Element.QueryElement<T>();
        public UIElementBase? QueryElement(string name) => Element.QueryElement(name);

        public static UIElementBase[] Run(Func<UIElementBase?> action)
        {
            var element = action();
            return element != null ? [element] : [];
        }
        public static UIElementBase[] Run(Action action)
        {
            action();
            return [];
        }
        public static UIElementBase[] Run(Func<UIElementBase[]> action) => action();
        public static UIElementBase[] If(bool condition, Func<UIElementBase> action) => condition ? [action()] : [];
        public static UIElementBase[] If(bool condition, Func<UIElementBase[]> action) => condition ? action() : [];
        public static UIElementBase[] Foreach<T>(IEnumerable<T> data, Func<T, UIElementBase?> action)
        {
            List<UIElementBase> elements = [];
            foreach (var item in data)
            {
                var element = action(item);
                if (element != null)
                    elements.Add(element);
            }
            return [.. elements];
        }
        
        public static UIElementBase[] Foreach<T>(IEnumerable<T> data, Func<int, T, UIElementBase?> action)
        {
            List<UIElementBase> elements = [];
            int i = 0;
            foreach (var item in data)
            {
                var element = action(i, item);
                if (element != null)
                    elements.Add(element);
                i++;
            }
            return [.. elements];
        }

        public static UIElementBase[] Forloop(uint start, uint count, Func<uint, UIElementBase?> action)
        {
            List<UIElementBase> elements = [];
            for (uint i = start; i < count; i++)
            {
                var element = action(i);
                if (element != null)
                    elements.Add(element);
            }
            return [.. elements];
        }

        public static UIElementBase[] Forloop(uint start, uint count, Func<UIElementBase?> action)
        {
            List<UIElementBase> elements = [];
            for (uint i = start; i < count; i++)
            {
                var element = action();
                if (element != null)
                    elements.Add(element);
            }
            return [.. elements];
        }

        public static UIElementBase[] Forloop(int start, int count, Func<int, UIElementBase?> action)
        {
            List<UIElementBase> elements = [];
            for (int i = start; i < count; i++)
            {
                var element = action(i);
                if (element != null)
                    elements.Add(element);
            }
            return [.. elements];
        }

        public static UIElementBase[] Forloop(int start, int count, Func<UIElementBase?> action)
        {
            List<UIElementBase> elements = [];
            for (int i = start; i < count; i++)
            {
                var element = action();
                if (element != null)
                    elements.Add(element);
            }
            return [.. elements];
        }

        public static UIElementBase[] Foreach<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> data, Func<TKey, TValue, UIElementBase?> action)
        {
            List<UIElementBase> elements = [];
            foreach (var kvp in data)
            {
                var element = action(kvp.Key, kvp.Value);
                if (element != null)
                    elements.Add(element);
            }
            return [.. elements];
        }
    }
}