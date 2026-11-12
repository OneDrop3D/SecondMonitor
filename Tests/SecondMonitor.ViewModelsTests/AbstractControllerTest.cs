namespace SecondMonitor.ViewModelsTests
{
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Bootstrapping;
    using Contracts.Commands;
    using Ninject;
    using ViewModels;
    using ViewModels.Controllers;
    using ViewModels.Factory;
    using NUnit.Framework;

    public class AbstractControllerTest
    {
        private StandardKernel _kernel;
        private AbstractControllerStub _testee;

        [SetUp]
        public void Setup()
        {
            _testee = _kernel.Get<AbstractControllerStub>();
        }

        [OneTimeSetUp]
        public void SetupOneTime()
        {
            _kernel = new StandardKernel();
            _kernel.LoadCore();
        }

        [Test]
        public void Bind_WhenCommandIsBound_ThenIsAutomaticallyAssignedToViewModel()
        {
            // Arrange
            bool isCommand1Executed = false;
            bool isCommand2Executed = false;
            _testee.Bind<AbstractViewModelStub>().Command(x => x.FooCommand).To(() => isCommand1Executed = true);
            _testee.Bind<AbstractViewModelStub>().Command(x => x.FooCommand2).To(() => isCommand2Executed = true);
            _testee.StartControllerAsync();

            // Act
            _testee.AbstractViewModelStub.FooCommand.Execute(null);
            _testee.AbstractViewModelStub.FooCommand2.Execute(null);

            // Assert
            Assert.That(isCommand1Executed, Is.True);
            Assert.That(isCommand2Executed, Is.True);
        }

        protected class AbstractControllerStub : AbstractController
        {
            public AbstractControllerStub(IViewModelFactory viewModelFactory) : base(viewModelFactory)
            {
            }

            public AbstractViewModelStub AbstractViewModelStub { get; private set; }

            public override Task StartControllerAsync()
            {
                AbstractViewModelStub = ViewModelFactory.Create<AbstractViewModelStub>();
                return Task.CompletedTask;
            }

            public override Task StopControllerAsync()
            {
                return Task.CompletedTask;
            }
        }

        protected class AbstractViewModelStub : AbstractViewModel
        {
            public ICommand FooCommand { get; set; }
            public RelayCommand FooCommand2 { get; set; }
        }
    }
}