#define UNITY
using EngineFramework.Config;
using EngineFramework.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity;
using Unity.Extension;
using Unity.Injection;
using Unity.Lifetime;
using Unity.Registration;
using Unity.Resolution;

namespace EngineFramework.DI
{
    public abstract class Container
    {
        private static IUnityContainer _UnityContainer { get; set; }

        static Container()
        {
            _UnityContainer = new UnityContainer();
        }

        public abstract void Config();

        public static void AddDbContext<TContext, TTo>(string connectionString, bool enableLazyLoading = false, bool enableLogging = false) where TTo : TContext where TContext : DbContext
        {
            _UnityContainer = new UnityContainer();
            //Container.AddNewExtension<Interception>();

            var contextOptions = new DbContextOptionsBuilder<TTo>();

            if (enableLazyLoading)
                contextOptions = contextOptions.UseLazyLoadingProxies();

            if (enableLogging)
                contextOptions = contextOptions.UseLoggerFactory(EngineFrameworkLoggerFactory.loggerFactory);

            contextOptions = contextOptions.UseSqlServer(connectionString);

            _UnityContainer.RegisterType<TContext, TTo>(new InjectionConstructor(contextOptions.Options));
        }

        public static void AddDbContext<TContext>(string connectionString, bool enableLazyLoading = false, bool enableLogging = false) where TContext : DbContext
        {
            _UnityContainer = new UnityContainer();
            //Container.AddNewExtension<Interception>();

            var contextOptions = new DbContextOptionsBuilder<TContext>();

            if (enableLazyLoading)
                contextOptions = contextOptions.UseLazyLoadingProxies();

            if (enableLogging)
                contextOptions = contextOptions.UseLoggerFactory(EngineFrameworkLoggerFactory.loggerFactory);

            _UnityContainer.RegisterType<TContext>(new InjectionConstructor(contextOptions.Options));
        }

        public static void RegisterType<TFrom, TTo>() where TTo : TFrom => _UnityContainer.RegisterType<TFrom, TTo>();

        public static void RegisterType<TFrom>() => _UnityContainer.RegisterType<TFrom>();
        public static void RegisterType<TFrom, TTo>(InjectionConstructor injectionConstructor) where TTo : TFrom => _UnityContainer.RegisterType<TFrom, TTo>(injectionConstructor);
        public static void RegisterType<TFrom>(InjectionConstructor injectionConstructor) => _UnityContainer.RegisterType<TFrom>(injectionConstructor);

        public static void RegisterInstance<TFrom>(TFrom instance) => _UnityContainer.RegisterInstance<TFrom>(instance);

        public static void RegisterSingleton<TFrom>() => _UnityContainer.RegisterSingleton<TFrom>();

        public static void RegisterSingleton<TFrom, TTo>() where TTo : TFrom => _UnityContainer.RegisterSingleton<TFrom, TTo>();

        public static T Resolve<T>() => _UnityContainer.Resolve<T>();

        public static T Resolve<T>(string fullName)
        {
            var temp = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(s => s.FullName == fullName).SingleOrDefault();
            if (temp != null)
                return (T)_UnityContainer.Resolve(temp);

            return default(T);
        }
    }
}
