﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommonServiceLocator;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Gate.Radio.Emulator.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prism.DryIoc;
using Prism.Ioc;

namespace Gate.Radio.Emulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private IHost _host;
        private readonly RadioStateService _radioStateService = new RadioStateService();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(typeof(RadioStateService), _radioStateService);
        }

        protected override Window CreateShell()
        {
            _host = CreateHostBuilder().Build();
            _host.Start();
            var mainWindow = ServiceLocator.Current.GetInstance<MainWindow>();
            mainWindow.DataContext = new MainWindowViewModel(_radioStateService);
            return mainWindow;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.Dispose();
            base.OnExit(e);
        }

        private IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices(collection => { collection.AddSingleton(_radioStateService); })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls($"https://*:{Defaults.Port}");
                });
    }
}
