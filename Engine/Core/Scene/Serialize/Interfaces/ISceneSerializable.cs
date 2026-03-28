namespace PBG.Core;

public interface ISceneSerializable
{
    public void Deserialize(List<SceneFieldJson> data);
    public List<SceneFieldJson> Serialize();

    public static ISceneSerializable GetDeserialized(List<SceneFieldJson>? data, ISceneSerializable value)
    {
        if (data != null)
            value.Deserialize(data);
        return value;
    }
}