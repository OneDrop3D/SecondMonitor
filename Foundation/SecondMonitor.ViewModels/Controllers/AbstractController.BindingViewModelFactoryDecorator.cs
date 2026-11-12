namespace SecondMonitor.ViewModels.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Contracts.NInject;

    using Factory;
    using NLog;

    using SecondMonitor.DataModel.Extensions;

    using Syntax;

    public abstract partial class AbstractController : IController
    {
        private sealed class BindingViewModelFactoryDecorator : IViewModelFactory, IDecoratedViewModelFactory
        {
            private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
            private readonly IViewModelFactory _decorate;
            private readonly List<IViewModelBindings> _existingBindings;

            internal BindingViewModelFactoryDecorator(IViewModelFactory viewModelFactory)
            {
                _decorate = viewModelFactory;
                _existingBindings = new List<IViewModelBindings>();
            }

            public ICommandFactory CommandFactory => _decorate.CommandFactory;

            public T Create<T>() where T : IViewModel
            {
                try
                {
                    IViewModelFactory viewModelFactory = _decorate.Set<IViewModelFactory>().To(this);
                    T viewModel = viewModelFactory.Create<T>();
                    if (_existingBindings.Any(x => x.ViewModelType.IsInstanceOfType(viewModel)))
                    {
                        BindCommands(viewModel);
                    }

                    return viewModel;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    throw;
                }
            }

            public T CreateAndApply<T, TM>(TM model) where T : IViewModel<TM>
            {
                IViewModelFactory viewModelFactory = _decorate.Set<IViewModelFactory>().To(this);
                T viewModel = viewModelFactory.Create<T>();
                if (_existingBindings.Any(x => x.ViewModelType.IsInstanceOfType(viewModel)))
                {
                    BindCommands(viewModel);
                }

                viewModel.FromModel(model);
                return viewModel;
            }

            public IEnumerable<T> CreateAll<T>() where T : IViewModel
            {
                IViewModelFactory viewModelFactory = _decorate.Set<IViewModelFactory>().To(this);
                IEnumerable<T> viewModels = viewModelFactory.CreateAll<T>().ToList();
                viewModels.ForEach(OnViewModelCreated);

                return viewModels;
            }

            public void OnViewModelCreated<T>(T newViewModel) where T : IViewModel
            {
                if (_existingBindings.Any(x => x.ViewModelType.IsInstanceOfType(newViewModel)))
                {
                    BindCommands(newViewModel);
                }
            }

            public INamedConstructorParameter<IViewModelFactory> Set(string parameterName)
            {
                return _decorate.Set<IViewModelFactory>().To(this).Set(parameterName);
            }

            public ITypedConstructorParameter<IViewModelFactory, T> Set<T>()
            {
                return _decorate.Set<IViewModelFactory>().To(this).Set<T>();
            }

            public IViewModelBindingSyntax<TViewModel> Bind<TViewModel>() where TViewModel : IViewModel
            {
                IViewModelBindings existingBinding = _existingBindings.FirstOrDefault(x => x.ViewModelType == typeof(TViewModel));
                if (existingBinding == null)
                {
                    existingBinding = new ViewModelBindings<TViewModel>();
                    _existingBindings.Add(existingBinding);
                }

                return ((IViewModelBindingSyntax<TViewModel>)existingBinding);
            }

            private void BindCommands<T>(T viewModel) where T : IViewModel
            {
                _existingBindings.Where(x => x.ViewModelType.IsInstanceOfType(viewModel))
                    .ForEach(x => x.BindCommands(viewModel, this));
            }
        }
    }
}