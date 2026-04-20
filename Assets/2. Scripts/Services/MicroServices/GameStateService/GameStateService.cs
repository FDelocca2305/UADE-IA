using System;

namespace Services.MicroServices.GameStateService
{

//todo para que quiero este manager? Necesito saber el  estado del juego en alugn momento?  
    public class GameStateService : IGameStateService
    {
        private GameState m_currentState;

        public event Action<GameState, GameState> OnStateChanged;

        public void Initialize()
        {
            //Todo: Cambiar;
            m_currentState = GameState.Playing;
        }

        public GameState GetCurrentState() => m_currentState;

        public void ChangeState(GameState p_newState)
        {
            if (m_currentState == p_newState) return;

            var l_previousState = m_currentState;
            OnExitState(m_currentState);
            m_currentState = p_newState;
            OnEnterState(p_newState);

            OnStateChanged?.Invoke(l_previousState, p_newState);
            MyLogger.LogInfo($"Game state changed from {l_previousState} to {p_newState}");
        }

        private void OnEnterState(GameState p_state)
        {
            switch (p_state)
            {
                case GameState.Menu:
                case GameState.Playing:
                case GameState.Paused:
                    break;
                case GameState.GameOver:
                    HandleGameOver();
                    break;

                case GameState.Victory:
                    HandleVictory();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(p_state), p_state, null);
            }
        }

        private void OnExitState(GameState p_state)
        {

        }

        private void HandleGameOver()
        {
            MyLogger.LogInfo("Game Over!");
        }

        private void HandleVictory()
        {
            MyLogger.LogInfo("Victory!");
        }
    }
}