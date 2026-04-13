namespace PBG.UI.Exception
{
    public class UIElementNotFoundException : System.Exception
    {
        public UIElementNotFoundException(string type, string reason = "") : base(
            $"UI element of type {type} was not found" + (string.IsNullOrEmpty(reason) ? "" : $": {reason}")
        ){}
    }
}