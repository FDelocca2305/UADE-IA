using System;

namespace Services.MicroServices.UserDataService.Wallet
{
    [Serializable]
    public class WalletState : IUserState
    {
        public int coins;
        public int diamonds;
    }
}
