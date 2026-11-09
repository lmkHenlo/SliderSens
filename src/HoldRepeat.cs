namespace SliderSens
{
    /// <summary>
    /// Step-on-press, then repeat while held, with the game's own menu timings
    /// (<c>MenuNavigation</c>: 0.4 s before the first repeat, then every 0.1 s).
    /// </summary>
    internal struct HoldRepeat
    {
        internal const float FirstDelay = 0.4f;
        internal const float Interval = 0.1f;

        private int _direction;
        private float _timer;

        /// <param name="direction">-1, 0 or 1: the direction held this frame.</param>
        /// <param name="deltaTime">Seconds since the last call.</param>
        /// <returns>True if a step should be made this frame.</returns>
        internal bool Tick(int direction, float deltaTime)
        {
            if (direction == 0)
            {
                _direction = 0;
                return false;
            }
            if (direction != _direction)
            {
                _direction = direction;
                _timer = FirstDelay;
                return true;
            }
            _timer -= deltaTime;
            if (_timer > 0f)
                return false;
            _timer = Interval;
            return true;
        }

        /// <summary>Forgets the held direction: the next one held steps straight away.</summary>
        internal void Reset() => _direction = 0;
    }
}
