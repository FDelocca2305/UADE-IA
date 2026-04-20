namespace Services.MicroServices.UserDataService.Wallet
{
    public interface IWalletService : IGameService
    {
        int Coins { get; }
        int Diamonds { get; }

        void AddCoins(int amount);
        bool SpendCoins(int amount);

        void AddDiamonds(int amount);
        bool SpendDiamonds(int amount);

        event System.Action<int, int> OnWalletChanged;
    }
}
