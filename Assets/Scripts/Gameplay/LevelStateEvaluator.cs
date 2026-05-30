namespace LaneSurvivor.Gameplay
{
    public static class LevelStateEvaluator
    {
        public static LevelState Evaluate(float playerZ, float finishDistance, int squadCount, LevelState currentState)
        {
            if (currentState == LevelState.Won || currentState == LevelState.Lost)
            {
                return currentState;
            }

            if (squadCount <= 0)
            {
                return LevelState.Lost;
            }

            if (playerZ >= finishDistance)
            {
                return LevelState.Won;
            }

            return currentState;
        }
    }
}
