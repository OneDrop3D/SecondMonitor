namespace SecondMonitor.Telemetry.TelemetryApplication.Controllers.Settings.DefaultCarProperties
{
    using System;
    using System.Linq;
    using DataModel;
    using DataModel.BasicProperties;
    using TelemetryApplication.Settings.DTO.DefaultCarProperties;
    using TelemetryManagement.Settings;

    public class R3EDefaultCarPropertiesProvider : IDefaultCarPropertiesProvider
    {
        private readonly DefaultR3ECarSettingsRepository _defaultR3ECarSettingsRepository;
        private readonly Lazy<DefaultR3ECarsProperties> _r3ECarPropertiesLazy;

        public R3EDefaultCarPropertiesProvider(DefaultR3ECarSettingsRepository defaultR3ECarSettingsRepository)
        {
            _defaultR3ECarSettingsRepository = defaultR3ECarSettingsRepository;
            _r3ECarPropertiesLazy = new Lazy<DefaultR3ECarsProperties>(LoadCarProperties);
        }

        protected DefaultR3ECarsProperties CarProperties => _r3ECarPropertiesLazy.Value;

        private DefaultR3ECarsProperties LoadCarProperties()
        {
            return _defaultR3ECarSettingsRepository.LoadOrCreateNew();
        }

        public bool TryGetDefaultSettings(string simName, string carName, out CarPropertiesDto carPropertiesDto)
        {
            if (simName != SimulatorsNameMap.R3ESimName)
            {
                carPropertiesDto = null;
                return false;
            }

            var r3EProperties = CarProperties?.CarsProperties?.FirstOrDefault(x => x.CarName == carName.Replace(" ", string.Empty).ToLower());
            if (r3EProperties == null)
            {
                carPropertiesDto = null;
                return false;
            }

            carPropertiesDto = new CarPropertiesDto
            {
                CarName = carName,
                Simulator = simName,
                FrontLeftTyre =
                {
                    BumpTransition = Velocity.FromMs(r3EProperties.BumpTransitionFront),
                    ReboundTransition = Velocity.FromMs(r3EProperties.ReboundTransitionFront)
                },
                FrontRightTyre =
                {
                    BumpTransition = Velocity.FromMs(r3EProperties.BumpTransitionFront),
                    ReboundTransition = Velocity.FromMs(r3EProperties.ReboundTransitionFront)
                },
                RearLeftTyre =
                {
                    BumpTransition = Velocity.FromMs(r3EProperties.BumpTransitionRear),
                    ReboundTransition = Velocity.FromMs(r3EProperties.ReboundTransitionRear)
                },
                RearRightTyre =
                {
                    BumpTransition = Velocity.FromMs(r3EProperties.BumpTransitionRear),
                    ReboundTransition = Velocity.FromMs(r3EProperties.ReboundTransitionRear)
                },
            };

            return true;
        }
    }
}