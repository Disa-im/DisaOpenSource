namespace Disa.Framework
{
    public interface ISerializableType<T>
    {
        T SerializeProperties();
        T DeserializeProperties();
    }
}