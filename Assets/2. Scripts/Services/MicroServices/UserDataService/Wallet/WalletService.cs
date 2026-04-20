using Services.MicroServices.UserDataService;

namespace Services.MicroServices.UserDataService.Wallet
{
    public class WalletService : IWalletService
    {
        private readonly IUserDataService m_userDataService;
        private WalletState m_state;

        public int Coins => m_state?.coins ?? 0;
        public int Diamonds => m_state?.diamonds ?? 0;

        public event System.Action<int, int> OnWalletChanged;

        public WalletService(IUserDataService userDataService)
        {
            m_userDataService = userDataService;
        }

        public void Initialize()
        {
            m_state = m_userDataService.GetState<WalletState>();
            NotifyChange();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            m_state.coins += amount;
            Persist();
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0 || m_state.coins < amount)
                return false;

            m_state.coins -= amount;
            Persist();
            return true;
        }

        public void AddDiamonds(int amount)
        {
            if (amount <= 0) return;
            m_state.diamonds += amount;
            Persist();
        }

        public bool SpendDiamonds(int amount)
        {
            if (amount <= 0 || m_state.diamonds < amount)
                return false;

            m_state.diamonds -= amount;
            Persist();
            return true;
        }

        private void Persist()
        {
            m_userDataService.Save();
            NotifyChange();
        }

        private void NotifyChange()
        {
            OnWalletChanged?.Invoke(Coins, Diamonds);
        }
    }
}
