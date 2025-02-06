using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DependencyInjection
{
    internal enum ServiceLifetime
    {
        Singleton,
        Transient,
        Scoped
    }

    /// <summary>
    /// A simple service registry for managing service registrations and resolutions.
    /// Supports singleton, transient, and scoped lifetimes.
    /// </summary>
    internal static class ServiceRegistry
    {
        private static readonly ConcurrentDictionary<Type, (object Instance, ServiceLifetime Lifetime, bool TrackForDisposal)> SingletonServices = new ConcurrentDictionary<Type, (object, ServiceLifetime, bool)>();
        private static readonly ConcurrentDictionary<Type, (Func<object> Factory, ServiceLifetime Lifetime, bool TrackForDisposal)> Factories = new ConcurrentDictionary<Type, (Func<object>, ServiceLifetime, bool)>();
        private static readonly ConcurrentDictionary<string, (Func<object> Factory, ServiceLifetime Lifetime, bool TrackForDisposal)> NamedFactories = new ConcurrentDictionary<string, (Func<object>, ServiceLifetime, bool)>();
        private static readonly List<IDisposable> Disposables = new List<IDisposable>();
        private static readonly object LockObject = new object();

        static ServiceRegistry()
        {
            // Register initial services here
            // RegisterSingleton<IExampleService, ExampleService>(trackForDisposal: false);
        }

        /// <summary>
        /// Registers a singleton service instance.
        /// Singleton services are created once and shared throughout the application's lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="trackForDisposal">Indicates whether the service should be tracked for disposal.</param>
        internal static void RegisterSingleton<TService, TImplementation>(bool trackForDisposal = true) where TImplementation : TService, new()
        {
            SingletonServices[typeof(TService)] = (new TImplementation(), ServiceLifetime.Singleton, trackForDisposal);
            Log($"Singleton service of type {typeof(TService)} registered.");
        }

        /// <summary>
        /// Registers a service with a factory method for transient lifetime.
        /// Transient services are created each time they are requested.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <param name="trackForDisposal">Indicates whether the service should be tracked for disposal.</param>
        internal static void RegisterTransient<TService>(Func<TService> factory, bool trackForDisposal = true)
        {
            Factories[typeof(TService)] = (() => factory(), ServiceLifetime.Transient, trackForDisposal);
            Log($"Transient service factory for type {typeof(TService)} registered.");
        }

        /// <summary>
        /// Registers a service with a factory method for scoped lifetime.
        /// Scoped services are created once per scope. In this example, the scope is simulated using the ServiceProvider class.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <param name="trackForDisposal">Indicates whether the service should be tracked for disposal.</param>
        internal static void RegisterScoped<TService>(Func<IServiceProvider, TService> factory, bool trackForDisposal = true)
        {
            Factories[typeof(TService)] = (() => factory(new ServiceProvider()), ServiceLifetime.Scoped, trackForDisposal);
            Log($"Scoped service factory for type {typeof(TService)} registered.");
        }

        /// <summary>
        /// Registers a named service with a factory method for transient lifetime.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="name">The name of the service.</param>
        /// <param name="factory">The factory method to create the service instance.</param>
        /// <param name="trackForDisposal">Indicates whether the service should be tracked for disposal.</param>
        internal static void RegisterNamedTransient<TService>(string name, Func<TService> factory, bool trackForDisposal = true)
        {
            NamedFactories[name] = (() => factory(), ServiceLifetime.Transient, trackForDisposal);
            Log($"Named transient service factory for type {typeof(TService)} registered with name '{name}'.");
        }

        /// <summary>
        /// Resolves a service instance.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <returns>The service instance.</returns>
        internal static TService Resolve<TService>()
        {
            if (SingletonServices.TryGetValue(typeof(TService), out var singletonService))
            {
                Log($"Singleton service of type {typeof(TService)} resolved.");
                return (TService)singletonService.Instance;
            }

            if (Factories.TryGetValue(typeof(TService), out var factory))
            {
                var instance = (TService)factory.Factory();
                Log($"Transient or scoped service of type {typeof(TService)} created and resolved.");
                TrackDisposable(instance, factory.Lifetime, factory.TrackForDisposal);
                return instance;
            }

            throw new InvalidOperationException($"Service of type {typeof(TService)} is not registered.");
        }

        /// <summary>
        /// Resolves a named service instance.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="name">The name of the service.</param>
        /// <returns>The service instance.</returns>
        internal static TService Resolve<TService>(string name)
        {
            if (NamedFactories.TryGetValue(name, out var factory))
            {
                var instance = (TService)factory.Factory();
                Log($"Named transient service of type {typeof(TService)} created and resolved with name '{name}'.");
                TrackDisposable(instance, factory.Lifetime, factory.TrackForDisposal);
                return instance;
            }

            throw new InvalidOperationException($"Named service of type {typeof(TService)} with name '{name}' is not registered.");
        }

        private static void TrackDisposable(object instance, ServiceLifetime lifetime, bool trackForDisposal)
        {
            if (instance is IDisposable disposable && trackForDisposal && lifetime != ServiceLifetime.Singleton)
            {
                lock (LockObject)
                {
                    Disposables.Add(disposable);
                }
            }
        }

        public static void Dispose()
        {
            lock (LockObject)
            {
                foreach (var disposable in Disposables)
                {
                    disposable.Dispose();
                }
                Disposables.Clear();
            }
        }

        private static void Log(string message)
        {
            // Implement your logging mechanism here
            Console.WriteLine(message);
        }

        private sealed class ServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return Resolve(serviceType);
            }

            private object Resolve(Type serviceType)
            {
                if (SingletonServices.TryGetValue(serviceType, out var singletonService))
                {
                    return singletonService.Instance;
                }

                if (Factories.TryGetValue(serviceType, out var factory))
                {
                    var instance = factory.Factory();
                    TrackDisposable(instance, factory.Lifetime, factory.TrackForDisposal);
                    return instance;
                }

                throw new InvalidOperationException($"Service of type {serviceType} is not registered.");
            }
        }
    }

    

}

namespace DependencyInjection.Tests
{

    // Example service interface and implementation
    internal interface IExampleService
    {
        void DoSomething();
    }

    internal class ExampleService : IExampleService
    {
        public void DoSomething()
        {
            Console.WriteLine("ExampleService is doing something.");
        }
    }

    // Example factory class
    internal static class ExampleFactory
    {
        public static IExampleService CreateExampleService()
        {
            // You can add any initialization logic here
            return new ExampleService();
        }
    }

    internal class ProgramTestNoDispose
    {
        public static void TestMain()
        {
            // Register services
            ServiceRegistry.RegisterSingleton<IExampleService, ExampleService>(trackForDisposal: false);
            ServiceRegistry.RegisterTransient<IExampleService>(() => new ExampleService());
            ServiceRegistry.RegisterScoped<IExampleService>(provider => new ExampleService());
            ServiceRegistry.RegisterNamedTransient<IExampleService>("ExampleService", () => new ExampleService());

            // Resolve and use singleton service
            IExampleService singletonService = ServiceRegistry.Resolve<IExampleService>();
            singletonService.DoSomething();

            // Resolve and use transient services
            IExampleService transientService1 = ServiceRegistry.Resolve<IExampleService>();
            transientService1.DoSomething();

            IExampleService transientService2 = ServiceRegistry.Resolve<IExampleService>();
            transientService2.DoSomething();

            // Resolve and use scoped services
            IExampleService scopedService1 = ServiceRegistry.Resolve<IExampleService>();
            scopedService1.DoSomething();

            IExampleService scopedService2 = ServiceRegistry.Resolve<IExampleService>();
            scopedService2.DoSomething();

            // Resolve and use named transient service
            IExampleService namedService = ServiceRegistry.Resolve<IExampleService>("ExampleService");
            namedService.DoSomething();

            // Dispose of services when done
            ServiceRegistry.Dispose();
        }
    }

    internal class ProgramTestWithScope
    {
        public static void TestMain()
        {
            using (var scope = new ServiceScope())
            {
                // Register services
                ServiceRegistry.RegisterSingleton<IExampleService, ExampleService>(trackForDisposal: false);
                ServiceRegistry.RegisterTransient<IExampleService>(() => new ExampleService());
                ServiceRegistry.RegisterScoped<IExampleService>(provider => new ExampleService());
                ServiceRegistry.RegisterNamedTransient<IExampleService>("ExampleService", () => new ExampleService());

                // Resolve and use singleton service
                IExampleService singletonService = ServiceRegistry.Resolve<IExampleService>();
                singletonService.DoSomething();

                // Resolve and use transient services
                IExampleService transientService1 = ServiceRegistry.Resolve<IExampleService>();
                transientService1.DoSomething();

                IExampleService transientService2 = ServiceRegistry.Resolve<IExampleService>();
                transientService2.DoSomething();

                // Resolve and use scoped services
                IExampleService scopedService1 = ServiceRegistry.Resolve<IExampleService>();
                scopedService1.DoSomething();

                IExampleService scopedService2 = ServiceRegistry.Resolve<IExampleService>();
                scopedService2.DoSomething();

                // Resolve and use named transient service
                IExampleService namedService = ServiceRegistry.Resolve<IExampleService>("ExampleService");
                namedService.DoSomething();
            } // Dispose is called automatically here
        }
    }

    internal class ServiceScope : IDisposable
    {
        public void Dispose()
        {
            ServiceRegistry.Dispose();
        }
    }
}


