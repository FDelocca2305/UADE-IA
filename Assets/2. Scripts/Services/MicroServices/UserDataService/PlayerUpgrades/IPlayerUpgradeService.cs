using System;
using Game.Upgrades;

namespace Services.MicroServices.UserDataService.PlayerUpgrades
{
    public interface IPlayerUpgradeService : IGameService
    {
        PlayerUpgradeState State { get; }
        void ApplyUpgrade(PlayerUpgradeType type, float value);
        void ApplyUpgrades(MainCharacterDataSO targetData);
        event Action<PlayerUpgradeState> OnUpgradesChanged;
    }
}
