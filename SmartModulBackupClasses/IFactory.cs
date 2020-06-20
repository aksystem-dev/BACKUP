namespace SmartModulBackupClasses
{
    public interface IFactory<T>
    {
        T GetInstance();
    }
}