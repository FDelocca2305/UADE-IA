namespace Services.MicroServices.PersistanceService
{
    public interface IPersistenceService : IGameService
    {
        T Get<T>(params string[] p_path) where T : IPersistentElement;
        T Get<T>(T p_toOverride, params string[] p_keys) where T : IPersistentElement;
        void Set<T>(T p_element, params string[] p_path) where T : IPersistentElement;
        void Flush();
        void Flush(params string[] p_path);
    }
    
    public interface IPersistentElement
    {
        string PersistenceID { get; }
        void OnAfterDeserialize();
        void OnBeforeSerialize();
    }
}