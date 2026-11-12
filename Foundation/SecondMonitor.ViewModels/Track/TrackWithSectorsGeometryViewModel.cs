namespace SecondMonitor.ViewModels.Track
{
    using System.Windows.Media;
    using Contracts.Session;
    using DataModel.BasicProperties;
    using DataModel.Snapshot;
    using DataModel.TrackMap;

    public class TrackWithSectorsGeometryViewModel : TrackGeometryViewModel
    {
        private Geometry _sector1Geometry;
        private Geometry _sector2Geometry;
        private Geometry _sector3Geometry;
        private Geometry _finishLineGeometry;
        private SectorState _sector1State;
        private SectorState _sector2State;
        private SectorState _sector3State;

        public Geometry Sector1Geometry
        {
            get => _sector1Geometry;
            set => SetProperty(ref _sector1Geometry, value);
        }

        public Geometry Sector2Geometry
        {
            get => _sector2Geometry;
            set => SetProperty(ref _sector2Geometry, value);
        }

        public Geometry Sector3Geometry
        {
            get => _sector3Geometry;
            set => SetProperty(ref _sector3Geometry, value);
        }

        public Geometry FinishLineGeometry
        {
            get => _finishLineGeometry;
            set => SetProperty(ref _finishLineGeometry, value);
        }

        public SectorState Sector1State
        {
            get => _sector1State;
            set => SetProperty(ref _sector1State, value);
        }

        public SectorState Sector2State
        {
            get => _sector2State;
            set => SetProperty(ref _sector2State, value);
        }

        public SectorState Sector3State
        {
            get => _sector3State;
            set => SetProperty(ref _sector3State, value);
        }

        public void UpdateSectorStates(SimulatorDataSet dataSet, ISessionInformationProvider sessionInformationProvider)
        {
            if (dataSet.PlayerInfo == null)
            {
                return;
            }

            if (dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector1) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.FullCourseYellow) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.VirtualSafetyCar) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.SafetyCar))
            {
                Sector1State = SectorState.Yellow;
            }
            else if (sessionInformationProvider.IsDriverLastSectorPurple(dataSet.PlayerInfo, 1))
            {
                Sector1State = SectorState.Purple;
            }
            else if (sessionInformationProvider.IsDriverLastClassSectorPurple(dataSet.PlayerInfo, 1))
            {
                Sector1State = SectorState.ClassPurple;
            }
            else if (sessionInformationProvider.IsDriverLastSectorGreen(dataSet.PlayerInfo, 1))
            {
                Sector1State = SectorState.PersonalBest;
            }
            else
            {
                Sector1State = SectorState.Normal;
            }

            if (dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector2) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.FullCourseYellow) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.VirtualSafetyCar) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.SafetyCar))
            {
                Sector2State = SectorState.Yellow;
            }
            else if (sessionInformationProvider.IsDriverLastSectorPurple(dataSet.PlayerInfo, 2))
            {
                Sector2State = SectorState.Purple;
            }
            else if (sessionInformationProvider.IsDriverLastClassSectorPurple(dataSet.PlayerInfo, 2))
            {
                Sector2State = SectorState.ClassPurple;
            }
            else if (sessionInformationProvider.IsDriverLastSectorGreen(dataSet.PlayerInfo, 2))
            {
                Sector2State = SectorState.PersonalBest;
            }
            else
            {
                Sector2State = SectorState.Normal;
            }

            if (dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.YellowSector3) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.FullCourseYellow) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.VirtualSafetyCar) || dataSet.SessionInfo.ActiveFlags.HasFlag(FlagKind.SafetyCar))
            {
                Sector3State = SectorState.Yellow;
            }
            else if (sessionInformationProvider.IsDriverLastSectorPurple(dataSet.PlayerInfo, 3))
            {
                Sector3State = SectorState.Purple;
            }
            else if (sessionInformationProvider.IsDriverLastClassSectorPurple(dataSet.PlayerInfo, 3))
            {
                Sector3State = SectorState.ClassPurple;
            }
            else if (sessionInformationProvider.IsDriverLastSectorGreen(dataSet.PlayerInfo, 3))
            {
                Sector3State = SectorState.PersonalBest;
            }
            else
            {
                Sector3State = SectorState.Normal;
            }
        }

        protected override void ApplyModel(TrackGeometryDto model)
        {
            base.ApplyModel(model);
            Sector1Geometry = Geometry.Parse(model.Sector1Geometry);
            Sector2Geometry = Geometry.Parse(model.Sector2Geometry);
            Sector3Geometry = Geometry.Parse(model.Sector3Geometry);
            FinishLineGeometry = Geometry.Parse(model.StartLineGeometry);
        }
    }
}