using Autofac;
using Common.Log;
using Core.Settings;
using LykkeApi2.Controllers;
using LykkeApi2.Modules;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using Lykke.Service.OperationsHistory.Client;
using Lykke.Service.OperationsRepository.Client;
using Lykke.Service.PersonalData.Settings;
using Lykke.SettingsReader.ReloadingManager;
using Xunit;

namespace Lykke.WalletApiv2.Tests.DITests
{
    public class DITests
    {
        private const string MockUrl = "http://localhost";
        private readonly IContainer _container;

        public DITests()
        {
            var mockLog = new Mock<ILog>();

            var settings = new APIv2Settings
            {
                FeeSettings = new FeeSettings(),
                WalletApiv2 = new BaseSettings
                {
                    Db = new DbSettings(),
                    Services = new ServiceSettings
                    {
                        OperationsRepositoryClient = new OperationsRepositoryServiceClientSettings{ServiceUrl = MockUrl, RequestTimeout = 300}
                    },
                    DeploymentSettings = new DeploymentSettings(),
                    CacheSettings = new CacheSettings()
                },
                OperationsHistoryServiceClient = new OperationsHistoryServiceClientSettings{ServiceUrl = MockUrl},
                FeeCalculatorServiceClient = new FeeCalculatorSettings{ServiceUrl = MockUrl},
                PersonalDataServiceSettings = new PersonalDataServiceSettings{ServiceUri = MockUrl},
                MatchingEngineClient = new MatchingEngineSettings{IpEndpoint = new IpEndpointSettings{Host = "127.0.0.1", Port = 80}}
            };
            settings.WalletApiv2.Services.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)).ToList().ForEach(p => p.SetValue(settings.WalletApiv2.Services, MockUrl));

            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterModule(new Api2Module(ConstantReloadingManager.From(settings), mockLog.Object));
            containerBuilder.RegisterModule(new ClientsModule(ConstantReloadingManager.From(settings), mockLog.Object));
            containerBuilder.RegisterModule(new AspNetCoreModule());

            containerBuilder.RegisterType<AssetsController>();

            //register your controller class here to test
            _container = containerBuilder.Build();
        }

        [Fact]
        public void Test_InstantiateControllers()
        {
            //Arrange
            var controllersToTest = _container.ComponentRegistry.Registrations.Where(r => typeof(Controller).IsAssignableFrom(r.Activator.LimitType)).Select(r => r.Activator.LimitType).ToList();
            controllersToTest.ForEach(controller =>
            {
                //Act-Assert - ok if no exception
                this._container.Resolve(controller);
            });
        }
    }
}
