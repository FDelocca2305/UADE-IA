using Newtonsoft.Json;

namespace Services.MicroServices.PersistanceService
{
    public class LocalPersistenceSerializerSettings : JsonSerializerSettings
    {
        public LocalPersistenceSerializerSettings()
        {
            Formatting = Formatting.Indented;
            TypeNameHandling = TypeNameHandling.Auto;
            NullValueHandling = NullValueHandling.Include;
            DefaultValueHandling = DefaultValueHandling.Include;
            ContractResolver = new LocalPersistenceContractResolver();
            PreserveReferencesHandling = PreserveReferencesHandling.None;
        }
    }
}