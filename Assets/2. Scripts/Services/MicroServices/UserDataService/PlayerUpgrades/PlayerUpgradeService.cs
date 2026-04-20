using Game.Upgrades;
using UnityEngine;

namespace Services.MicroServices.UserDataService.PlayerUpgrades
{
    public class PlayerUpgradeService : IPlayerUpgradeService
    {
        private readonly IUserDataService m_userDataService;
        private const float MIN_SHOOT_COOLDOWN = 0.05f;
        private const float MIN_RELOAD_TIME = 0.2f;

        public PlayerUpgradeState State { get; private set; }

        public event System.Action<PlayerUpgradeState> OnUpgradesChanged;

        public PlayerUpgradeService(IUserDataService userDataService)
        {
            m_userDataService = userDataService;
        }

        public void Initialize()
        {
            State = m_userDataService.GetState<PlayerUpgradeState>() ?? new PlayerUpgradeState();
            OnUpgradesChanged?.Invoke(State);
        }

        public void ApplyUpgrade(PlayerUpgradeType type, float value)
        {
            if (State == null || value <= 0f)
                return;

            switch (type)
            {
                case PlayerUpgradeType.MaxHealth:
                    State.maxHealthBonus += value;
                    break;
                case PlayerUpgradeType.MoveSpeed:
                    State.moveSpeedBonus += value;
                    break;
                case PlayerUpgradeType.ShootCooldown:
                    State.shootCooldownReduction += value;
                    break;
                case PlayerUpgradeType.MagSize:
                    State.magSizeBonus += value;
                    break;
                case PlayerUpgradeType.ReloadTime:
                    State.reloadTimeReduction += value;
                    break;
                case PlayerUpgradeType.BulletSpeed:
                    State.bulletSpeedBonus += value;
                    break;
                case PlayerUpgradeType.BulletDamage:
                    State.bulletDamageBonus += value;
                    break;
            }

            m_userDataService.Save();
            OnUpgradesChanged?.Invoke(State);
        }

        public void ApplyUpgrades(MainCharacterDataSO targetData)
        {
            if (State == null || targetData == null)
                return;

            targetData.maxHealth += State.maxHealthBonus;
            targetData.moveSpeed += State.moveSpeedBonus;
            targetData.shootCooldown = Mathf.Max(MIN_SHOOT_COOLDOWN, targetData.shootCooldown - State.shootCooldownReduction);
            targetData.magSize += State.magSizeBonus;
            targetData.reloadTime = Mathf.Max(MIN_RELOAD_TIME, targetData.reloadTime - State.reloadTimeReduction);

            if (targetData.bulletData != null)
            {
                targetData.bulletData.AddSpeed(State.bulletSpeedBonus, 0f, float.MaxValue);
                targetData.bulletData.AddDamage(State.bulletDamageBonus, 0f, float.MaxValue);
            }
        }
    }
}
