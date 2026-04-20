using System;

namespace Services.MicroServices.UserDataService.PlayerUpgrades
{
    [Serializable]
    public class PlayerUpgradeState : IUserState
    {
        public float maxHealthBonus;
        public float moveSpeedBonus;
        public float shootCooldownReduction;
        public float magSizeBonus;
        public float reloadTimeReduction;
        public float bulletSpeedBonus;
        public float bulletDamageBonus;
    }
}
