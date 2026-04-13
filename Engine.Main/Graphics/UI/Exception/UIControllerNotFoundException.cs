namespace PBG.UI.Exception
{
    public class UIControllerNotFoundException : System.Exception
    {
        public UIControllerNotFoundException(string identifier = "") : base($"UIController not found: {identifier}")
        {
        }
    }
}