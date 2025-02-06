using System;
using ConsoleApp3.Classes;
using DependencyInjection;


namespace ConsoleApp3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            new RegisterUnit();
            IExampleServiceTesting app = ServiceRegistry.Resolve<IExampleServiceTesting>();
            Console.WriteLine(app.multiply(10, 9));
            Console.WriteLine(app.minus(10, 9));
            Console.WriteLine(app.plus(10, 9));
        }
    }
}
