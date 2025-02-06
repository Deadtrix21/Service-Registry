using DependencyInjection.Tests;
using DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3.Classes
{
    internal class RegisterUnit
    {
        public RegisterUnit()
        {
            ServiceRegistry.RegisterSingleton<IExampleService, ExampleService>(trackForDisposal: false);
            ServiceRegistry.RegisterSingleton<IExampleServiceTesting, ExampleServiceTesting>();
        }
    }
}
