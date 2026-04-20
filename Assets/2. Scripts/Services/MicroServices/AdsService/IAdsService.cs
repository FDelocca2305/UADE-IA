using _2._Scripts.UI.Gameplay.Results;
using UnityEngine.Advertisements;

namespace Services.MicroServices.AdsService
{
    public interface IAdsService : IGameService, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
    {
        public void Show(PlayerCollector p_playerCollector, ResultsView p_resultsPresenter);
    }
}