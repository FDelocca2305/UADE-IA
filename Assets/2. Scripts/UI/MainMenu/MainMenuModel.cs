using System;

namespace _2._Scripts.UI.MainMenu
{
    public class MainMenuModel : UIModel
    {
        public event Action<int> OnChangedCoins;
        public event Action<int> OnChangedDiamon;

        private int m_coins;
        private int m_diamonds;

        public void SetWallet(int coins, int diamonds)
        {
            m_coins = coins;
            m_diamonds = diamonds;
            OnChangedCoins?.Invoke(m_coins);
            OnChangedDiamon?.Invoke(m_diamonds);
        }
    }
}
