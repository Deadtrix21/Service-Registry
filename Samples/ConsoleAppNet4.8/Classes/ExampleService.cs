using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3.Classes
{
    public interface IExampleServiceTesting
    {
         int plus(int a, int b);

         int minus(int a, int b);
         int multiply(int a, int b);
    }

    internal class ExampleServiceTesting : IExampleServiceTesting
    {
        public ExampleServiceTesting()
        {
            
        }

        public int plus(int a, int b)
        {
            return a + b;
        }

        public int minus(int a, int b)
        {
            return a - b;
        }
        public int multiply(int a, int b)
        {
            return a * b;
        }
    }
}
