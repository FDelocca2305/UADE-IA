using UnityEngine;

namespace Services.MicroServices.BlackboardService
{
    [CreateAssetMenu(menuName = "Service/BlackboardServiceSettings")]
    public class BlackboardServiceSettings : ScriptableObject
    {
        [field: Header("Configuration")]
        [field: SerializeField] public float CleanupInterval {get; private set;} = 30f; // Cleanup every 30 seconds
        [field: SerializeField] public int MaxPanicAreas {get; private set;} = 10;
        [field: SerializeField] public int MaxAIPositions {get; private set;} = 50;

        [field: Header("Minimum Scope Mode")]
        [field: SerializeField] public bool UseMinimumScope {get; private set;} = false;
        [field: SerializeField] public bool EnableAdvancedFeatures {get; private set;} = false;
    }
}