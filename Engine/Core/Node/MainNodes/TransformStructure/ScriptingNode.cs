using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using PBG.Rendering;

namespace PBG.Core
{
    public class ScriptingNode
    {
        public Scene Scene = Scene.CurrentScene;
        public Camera Camera => Scene.DefaultCamera;

        public string Name = "ScriptingNode";
        public TransformNode Transform = null!;


        public bool GetAction(string methodName, [NotNullWhen(true)] out Action? action)
        {
            if (GetMethod(methodName, out MethodInfo? methodInfo))
            {
                action = (Action)Delegate.CreateDelegate(typeof(Action), this, methodInfo);
                return true;
            }
            action = null;
            return false;
        }

        public bool GetMethod(string methodName, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return methodInfo != null;
        }
    }
}