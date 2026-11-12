namespace SecondMonitor.Remote.Debug.Simulation
{
    using System;
    using DataModel.BasicProperties;
    public class Car
    {
        private const double brakeForceInG = 1.2;
        private const int engineMinRpm = 1500;
        private const int engineMaxRpm = 7000;
        private static readonly double minSpeed = Velocity.FromKph(10).InMs;
        private static readonly double topSpeed = Velocity.FromKph(258).InMs;

        private readonly Gear[] _gears;

        public Car(string name, string @class)
        {
            Name = name;
            Class = @class;
            _gears = new[]
            {
                null,
                Gear.FirstGear(7, 0, 2.5, minSpeed),
                new Gear(5.5, 3, 1.5, 5),
                new Gear(3.5, 12.5, 4.5, 7.5),
                new Gear(2.5, 19.8, 7, 10.25),
                new Gear(1.55, 29.35, 9.75, 18),
                new Gear(0.4, 49.6, 17, 47),
                Gear.FinalGear(0.1, 63.4, 45, topSpeed)
            };
        }

        public int EngineRpm { get; private set; } = engineMinRpm;

        public int CurrentGear { get; private set; } = 0;

        public double ThrottlePosition { get; private set; } = 0.0;

        public double BrakePosition { get; private set; } = 0.0;

        public double ClutchPosition { get; } = 0.0;

        public string Name { get; }
        public string Class { get; }

        public double CurrentSpeed
        {
            get;
            private set;
        }

        public double BrakeDeceleration => brakeForceInG * 9.8;

        public double? LimiterSpeed { get; set; }

        public double Drive(double targetSpeed, double timeStep)
        {
            var currentGear = _gears[CurrentGear];

            if (LimiterSpeed.HasValue)
            {
                targetSpeed = Math.Min(targetSpeed, LimiterSpeed.Value);
            }

            if (CurrentSpeed < targetSpeed)
            {
                ThrottlePosition = 1.0;
                BrakePosition = 0;

                var isInNeutral = currentGear == null;
                if (isInNeutral || (CurrentSpeed >= currentGear.MaxSpeed && CurrentGear < _gears.Length))
                {
                    currentGear = _gears[++CurrentGear];
                }

                CurrentSpeed = Math.Min(currentGear.IntegrateSpeed(CurrentSpeed, 1.0, timeStep), targetSpeed);
            }
            else if (CurrentSpeed > targetSpeed)
            {
                BrakePosition = 1.0;
                ThrottlePosition = 0;

                CurrentSpeed -= BrakeDeceleration * BrakePosition;
                CurrentSpeed = Math.Max(CurrentSpeed, 0);

                if (CurrentSpeed < currentGear?.MinSpeed)
                {
                    currentGear = _gears[--CurrentGear];
                }
            }

            if (currentGear != null)
            {
                var engineLerp = (CurrentSpeed - currentGear.MinSpeed) / (currentGear.MaxSpeed - currentGear.MinSpeed);
                EngineRpm = Math.Max(Math.Min(engineMinRpm + (int)((engineMaxRpm - engineMinRpm) * engineLerp), engineMaxRpm), engineMinRpm);
            }

            return CurrentSpeed * timeStep;
        }
    }
}