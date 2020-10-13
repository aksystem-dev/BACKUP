namespace SmartModulBackupClasses
{
    /// <summary>
    /// Třída implementující toto rozhraní je továrna na objekty typu T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFactory<T>
    {
        T GetInstance();
    }
}