using PBG.Data;
using Silk.NET.Input;

namespace PBG.Core
{
    public abstract class SceneSwitcher(string sceneName)
    {
        public readonly string SceneName = sceneName;

        public override int GetHashCode()
        {
            return SceneName.GetHashCode();
        }

        public abstract bool CanSwitch();
    }

    public class SceneSwitcherKey(Key key, string sceneName) : SceneSwitcher(sceneName)
    {
        public override bool CanSwitch()
        {
            return Input.IsKeyPressed(key);
        }
    }

    public class SceneSwitcherKeys(Key[] keys, string sceneName) : SceneSwitcher(sceneName)
    {
        public override bool CanSwitch()
        {
            return Input.AreAllKeysDown(keys);
        }
    }
}