using System;

namespace Services.MicroServices.GameStateService
{
    public interface IGameStateService : IGameService
    {
        public event Action<GameState, GameState> OnStateChanged;
        
        public void ChangeState(GameState p_newState);
        public GameState GetCurrentState();
    }
}