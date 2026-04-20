using Services.MicroServices.EventsServices;

namespace Services.MicroServices.EventsServices.CustomEvents
{
    public readonly struct GameResultEvent : ICustomEventData
    {
        public bool IsVictory { get; }
        public int Score { get; }
        public int EarnedCoins { get; }
        public int EarnedDiamonds { get; }

        public GameResultEvent(bool isVictory, int score, int earnedCoins, int earnedDiamonds)
        {
            IsVictory = isVictory;
            Score = score;
            EarnedCoins = earnedCoins;
            EarnedDiamonds = earnedDiamonds;
        }
    }
}
