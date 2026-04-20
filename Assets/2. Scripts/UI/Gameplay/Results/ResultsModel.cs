using Services.MicroServices.EventsServices.CustomEvents;

namespace _2._Scripts.UI.Gameplay.Results
{
    public class ResultsModel : UIModel
    {
        public GameResultEvent? LastResult { get; private set; }

        public void SetResult(GameResultEvent result)
        {
            LastResult = result;
        }

        public void Clear()
        {
            LastResult = null;
        }
    }
}
