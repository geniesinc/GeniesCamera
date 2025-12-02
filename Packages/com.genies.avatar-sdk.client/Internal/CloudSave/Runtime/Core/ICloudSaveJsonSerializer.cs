namespace Genies.CloudSave
{
    public interface ICloudSaveJsonSerializer<T>
    {
        string ToJson(T data);
        T FromJson(string json);
        bool IsValidJson(string json);
    }
}
