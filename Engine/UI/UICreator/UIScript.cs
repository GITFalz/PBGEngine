
using PBG.Core;
using PBG.Rendering;
using static PBG.UI2.Styles;

namespace PBG.UI.Creator
{
    public abstract partial class UIScript
    {   
        public bool Created { get; private set; } = false;
        public UIController? UIController => Element?.UIController;
        public UIElementBase Element = null!;
        public UIScript()
        {
            PreScript();
            Element = Script();
            Created = true;
            AfterScript();
        }

        public static implicit operator UIElementBase(UIScript script) => script.Element;
        public virtual void PreScript() {}
        public abstract UIElementBase Script();
        public virtual void AfterScript() {}

        public static UIElementBase[] Sub(params UIElementBase[] subElements) => subElements;

        public T? GetElement<T>() where T : UIElementBase => Element.GetElement<T>();
        public T? GetElementAt<T>(int number) where T : UIElementBase => Element.GetElementAt<T>(number);
        public UIElementBase? GetElement(UIElementTag tag) => Element.GetElement(tag);
        public UIElementBase? GetElementAt(UIElementTag tag, int number) => Element.GetElementAt(tag, number);
        public UIElementBase? GetElement(string name) => Element.GetElement(name);
        public UIElementBase? GetElementAt(string name, int number) => Element.GetElementAt(name, number);
        public T? QueryElement<T>() where T : UIElementBase => Element.QueryElement<T>();
        public UIElementBase? QueryElement(string name) => Element.QueryElement(name);

        public static IUIChild Run(Func<UIElementBase[]> action) => new Run(action);
        public static IUIChild Run(Func<UIElementBase?> action) => new Run(action);
        public static IUIChild Run(Action action) => new Run(action);

        public static IUIChild If(bool condition, Func<UIElementBase[]> action) => new If(condition, action);
        public static IUIChild If(bool condition, Func<UIElementBase> action) => new If(condition, action);

        public static IUIChild Foreach<T>(IEnumerable<T> data, Func<T, UIElementBase?> action) => new Foreach<T>(data, action);
        public static IUIChild Foreach<T>(IEnumerable<T> data, Func<int, T, UIElementBase?> action) => new Foreach<T>(data, action);
        public static IUIChild Foreach<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> data, Func<TKey, TValue, UIElementBase?> action) => new Foreach<TKey, TValue>(data, action); 

        public static IUIChild Forloop(uint start, uint count, Func<IUIChild?> action) => new Forloop(start, count, action);
        public static IUIChild Forloop(int start, int count, Func<IUIChild?> action) => new Forloop(start, count, action);
        public static IUIChild Forloop(uint start, uint count, Func<uint, IUIChild?> action) => new Forloop(start, count, action);
        public static IUIChild Forloop(int start, int count, Func<int, IUIChild?> action) => new Forloop(start, count, action);
    }
}