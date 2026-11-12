namespace SecondMonitor.Timing.Application.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using CarSettings;
    using Contracts.Commands;

    using DataModel.OperationalRange;
    using DataModel.Snapshot;
    using Ninject;
    using Ninject.Syntax;

    using SecondMonitor.DataModel.Extensions;

    using SimdataManagement.SimSettings;
    using SimdataManagement.ViewModel;
    using SimdataManagement.WheelDiameterWizard;
    using ViewModels.Factory;

    public class SimSettingController
    {
        private readonly IResolutionRoot _resolutionRoot;
        private readonly IViewModelFactory _viewModelFactory;

        private readonly SimSettingAdapter _simSettingAdapter;

        private CarSettingsWindow _carSettingsWindow;
        private CarSettingsWindowViewModel _carSettingsWindowViewModel;
        private IWheelDiameterWizardController _wheelDiameterWizardController;
        private SimulatorDataSet _lastDataSet;
        private List<CarModelProperties> _carModelProperties;
        private int _currentCarIndex;

        public SimSettingController(IResolutionRoot resolutionRoot, IViewModelFactory viewModelFactory)
        {
            _carModelProperties = new List<CarModelProperties>();
            _currentCarIndex = 0;
            _resolutionRoot = resolutionRoot;
            _viewModelFactory = viewModelFactory;
            _simSettingAdapter = resolutionRoot.Get<SimSettingAdapter>();
            CreateCarSettingsViewModel();
        }

        public void ApplySimSettings(SimulatorDataSet data)
        {
            _lastDataSet = data;
            data.Accept(_simSettingAdapter);
            _wheelDiameterWizardController?.ProcessDataSet(data);
        }

        public void OpenCarSettingsControl(Window parentWindow)
        {
            if (_carSettingsWindow != null)
            {
                _carSettingsWindow.Focus();
                return;
            }

            UpdateCarSettingsWindowViewModel();

            _carSettingsWindow = new CarSettingsWindow { Owner = parentWindow, DataContext = _carSettingsWindowViewModel };
            _carSettingsWindow.Closed += OnCarSettingsWindowClosed;
            _carSettingsWindow.Show();
        }

        private void UpdateCarSettingsWindowViewModel()
        {
            _carModelProperties = _simSettingAdapter.GetAllSessionCarsProperties().ToList();
            CarModelProperties playersCarProperties = _simSettingAdapter.LastUsedCarProperties;
            CarModelPropertiesViewModel playerCarsViewModel = _viewModelFactory.Create<CarModelPropertiesViewModel>();
            playerCarsViewModel.NextCarCommand = new RelayCommand(SelectNextCar);
            playerCarsViewModel.PreviousCarCommand = new RelayCommand(SelectPreviousCar);
            playerCarsViewModel.OkCommand = new RelayCommand(SaveAndCloseWindow);
            playerCarsViewModel.CancelCommand = new RelayCommand(CloseSettingsWindow);
            playerCarsViewModel.ResetCarSettingsCommand = new RelayCommand(ResetCarSettings);
            playerCarsViewModel.OpenTyreDiameterWizardCommand = new AsyncCommand(OpenTyreDiameterWizard);
            _carSettingsWindowViewModel.CarModelPropertiesViewModel = playerCarsViewModel;

            _currentCarIndex = _carModelProperties.IndexOf(x => x.Name == playersCarProperties.Name);
            playerCarsViewModel.FromModel(playersCarProperties);
            playerCarsViewModel.CarIndex = $"{_currentCarIndex + 1} / {_carModelProperties.Count}";
            playerCarsViewModel.TyreCompounds.Where(x => !string.IsNullOrWhiteSpace(x.CompoundName)).ForEach(x => x.CopyCompoundCommand = new RelayCommand(CreateLocalCopyOfSelectedTyre));

            AddGlobalCompounds(playerCarsViewModel);
            TyreCompoundProperties lastUsedTyre = _simSettingAdapter.LastUsedCompound;
            playerCarsViewModel.SelectedTyreCompound = playerCarsViewModel.TyreCompounds.FirstOrDefault(x => x.CompoundName == lastUsedTyre.CompoundName);
            if (_lastDataSet != null)
            {
                _carSettingsWindowViewModel.LastCarDataViewMode.FromModel(_lastDataSet);
            }

            playerCarsViewModel.IsModified = false;
            _carSettingsWindowViewModel.IsModified = false;
        }

        private void SelectNextCar()
        {
            int previousIndex = _currentCarIndex;
            _currentCarIndex = (_currentCarIndex + 1) % _carModelProperties.Count;
            UpdateCarSettingsWindowViewModelBySelectedCarIndex(previousIndex);
        }

        private void SelectPreviousCar()
        {
            int previousIndex = _currentCarIndex;
            _currentCarIndex -= 1;
            if (_currentCarIndex < 0)
            {
                _currentCarIndex = _carModelProperties.Count - 1;
            }

            UpdateCarSettingsWindowViewModelBySelectedCarIndex(previousIndex);
        }

        private void UpdateCarSettingsWindowViewModelBySelectedCarIndex(int previousIndex)
        {
            CarModelPropertiesViewModel carModelPropertiesViewModel = _carSettingsWindowViewModel.CarModelPropertiesViewModel;
            CarModelProperties oldCarProperties = carModelPropertiesViewModel.SaveToNewModel();
            _carModelProperties[previousIndex] = oldCarProperties;

            CarModelProperties carModelProperties = _carModelProperties[_currentCarIndex];
            carModelPropertiesViewModel.CarIndex = $"{_currentCarIndex + 1} / {_carModelProperties.Count}";
            carModelPropertiesViewModel.FromModel(carModelProperties);
            carModelPropertiesViewModel.TyreCompounds.Where(x => !string.IsNullOrWhiteSpace(x.CompoundName)).ForEach(x => x.CopyCompoundCommand = new RelayCommand(CreateLocalCopyOfSelectedTyre));
            carModelPropertiesViewModel.SelectedTyreCompound = carModelPropertiesViewModel.TyreCompounds.FirstOrDefault();
        }

        private void AddGlobalCompounds(CarModelPropertiesViewModel playerCarsViewModel)
        {
            foreach (TyreCompoundProperties globalTyreCompoundsProperty in _simSettingAdapter.GlobalTyreCompoundsProperties)
            {
                TyreCompoundPropertiesViewModel viewModel = _viewModelFactory.Create<TyreCompoundPropertiesViewModel>();
                viewModel.CopyCompoundCommand = new RelayCommand(CreateLocalCopyOfSelectedTyre);
                viewModel.FromModel(globalTyreCompoundsProperty);
                viewModel.IsGlobalCompound = true;
                playerCarsViewModel.TyreCompounds.Add(viewModel);
            }
        }

        private void ResetCarSettings()
        {
            var carPropertiesViewModel = _carSettingsWindowViewModel.CarModelPropertiesViewModel;
            var modelToReset = carPropertiesViewModel.SaveToNewModel();
            _simSettingAdapter.ResetCarSettings(modelToReset, _lastDataSet);
            carPropertiesViewModel.FromModel(modelToReset);
            carPropertiesViewModel.IsModified = false;

            AddGlobalCompounds(carPropertiesViewModel);
            TyreCompoundProperties lastUsedTyre = _simSettingAdapter.LastUsedCompound;
            carPropertiesViewModel.SelectedTyreCompound = carPropertiesViewModel.TyreCompounds.FirstOrDefault(x => x.CompoundName == lastUsedTyre.CompoundName);
        }

        private void CreateCarSettingsViewModel()
        {
            _carSettingsWindowViewModel = _viewModelFactory.Create<CarSettingsWindowViewModel>();
        }

        private async Task OpenTyreDiameterWizard()
        {
            if (_wheelDiameterWizardController != null)
            {
                return;
            }

            _wheelDiameterWizardController = _resolutionRoot.Get<IWheelDiameterWizardController>();
            _wheelDiameterWizardController.WizardCompleted += WheelDiameterWizardControllerOnWizardCompleted;
            await _wheelDiameterWizardController.StartControllerAsync();
            _wheelDiameterWizardController.OpenWizard(_carSettingsWindowViewModel.CarModelPropertiesViewModel);
        }

        private async void WheelDiameterWizardControllerOnWizardCompleted(object sender, EventArgs e)
        {
            await _wheelDiameterWizardController.StopControllerAsync();
            _wheelDiameterWizardController = null;
        }

        private void OnCarSettingsWindowClosed(object sender, EventArgs e)
        {
            _carSettingsWindow = null;
        }

        private void CloseSettingsWindow()
        {
            _carSettingsWindow?.Close();
        }

        private void UpdateSimSettingsFromViewModels()
        {
            _carModelProperties.ForEach(_simSettingAdapter.ReplaceCarModelProperties);
            _simSettingAdapter.GlobalTyreCompoundsProperties = _carSettingsWindowViewModel.CarModelPropertiesViewModel.TyreCompounds.Where(x => x.IsGlobalCompound).Select(y => y.SaveToNewModel()).ToList();
        }

        private void CreateLocalCopyOfSelectedTyre()
        {
            TyreCompoundPropertiesViewModel newCompoundPropertiesViewModel = _viewModelFactory.Create<TyreCompoundPropertiesViewModel>();
            newCompoundPropertiesViewModel.FromModel(_carSettingsWindowViewModel.CarModelPropertiesViewModel.SelectedTyreCompound.SaveToNewModel());
            newCompoundPropertiesViewModel.IsGlobalCompound = false;
            _carSettingsWindowViewModel.CarModelPropertiesViewModel.TyreCompounds.Add(newCompoundPropertiesViewModel);
            _carSettingsWindowViewModel.CarModelPropertiesViewModel.TyreCompounds.Add(newCompoundPropertiesViewModel);
            _carSettingsWindowViewModel.CarModelPropertiesViewModel.SelectedTyreCompound = newCompoundPropertiesViewModel;
        }

        private void SaveAndCloseWindow()
        {
            CarModelPropertiesViewModel carModelPropertiesViewModel = _carSettingsWindowViewModel.CarModelPropertiesViewModel;
            CarModelProperties oldCarProperties = carModelPropertiesViewModel.SaveToNewModel();
            _carModelProperties[_currentCarIndex] = oldCarProperties;
            UpdateSimSettingsFromViewModels();
            CloseSettingsWindow();
        }
    }
}