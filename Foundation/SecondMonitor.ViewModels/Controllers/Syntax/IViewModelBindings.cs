namespace SecondMonitor.ViewModels.Controllers.Syntax
{
    using System;
    using Factory;

    internal interface IViewModelBindings
    {
        Type ViewModelType { get; }
        void BindCommands<T>(T viewModel, IViewModelFactory viewModelFactory) where T : IViewModel;
    }
}