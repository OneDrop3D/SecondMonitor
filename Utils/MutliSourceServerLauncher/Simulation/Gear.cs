namespace SecondMonitor.Remote.Debug.Simulation
{
    using System;
    internal class Gear
    {
        private readonly double _acceleration;
        private readonly double _offset;

        private readonly double _startTimeElapsed;
        private readonly double _endTimeElapsed;

        public Gear(double acceleration, double offset, double startTimeElapsed, double endTimeElapsed)
        {
            _acceleration = acceleration;
            _offset = offset;
            _startTimeElapsed = startTimeElapsed;
            _endTimeElapsed = endTimeElapsed;
        }

        public double MinSpeed => _offset + (_startTimeElapsed * _acceleration);
        public double MaxSpeed => _offset + (_endTimeElapsed * _acceleration);

        public static Gear FirstGear(double acceleration, double offset, double endTimeElapsed, double minSpeed)
        {
            return new Gear(acceleration, offset, (minSpeed - offset) / acceleration, endTimeElapsed);
        }

        public static Gear FinalGear(double acceleration, double offset, double startTimeElapsed, double topSpeed)
        {
            return new Gear(acceleration, offset, startTimeElapsed, (topSpeed - offset) / acceleration);
        }

        public double IntegrateSpeed(double currentSpeed, double throttlePosition, double timeStep)
        {
            var previousTimeElapsed = (currentSpeed - _offset) / _acceleration;
            var currentTimeElapsed = previousTimeElapsed + (timeStep * throttlePosition);
            return Math.Min(_offset + (_acceleration * currentTimeElapsed), MaxSpeed);
        }
    }
}