namespace SecondMonitor.Timing.Api.Data
{
    using SecondMonitor.DataModel.BasicProperties;

    public class InputData
    {
        public InputData(InputInfo inputInfo)
        {
            Throttle = inputInfo.ThrottlePedalPosition;
            Brake = inputInfo.BrakePedalPosition;
            Clutch = inputInfo.ClutchPedalPosition;
            SteeringInput = inputInfo.SteeringInput;
            SteeringAngle = inputInfo.WheelAngle;
            IsSteeringAngleAvailable = inputInfo.WheelAngleFilled;
        }

        public double Throttle { get; }

        public double Brake { get; }

        public double Clutch { get; }

        public double SteeringInput { get; }

        public double SteeringAngle { get; }

        public bool IsSteeringAngleAvailable { get; }
    }
}
