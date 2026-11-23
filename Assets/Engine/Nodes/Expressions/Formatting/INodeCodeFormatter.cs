namespace PBG.Nodes;

public interface INodeCodeFormatter
{
    string TypeToString(Type type);
    string GetConstructor(Type type);
    string GetConstructor(object value);
    string Str(float value);
    string Str(int value);
    string Str(bool value);
    string GetVectorFieldName(int index);
    string CastIntStart();
    string CastIntEnd();
    string CastFloatStart();
    string CastFloatEnd();
    string ObjectToString(object value);
}