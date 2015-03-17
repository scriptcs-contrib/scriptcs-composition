using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ScriptCs.Composition;
using SampleContracts;

namespace ScriptCompositionSample
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = new ScriptCsContainerConfiguration()
                .WithScript("Test.csx")
                .Compile(AppDomain.CurrentDomain.BaseDirectory)
                .CreateContainer();
            var greeter = container.GetExport<IGreeter>();
            greeter.Greet("Hello");
            Console.ReadLine();
        }
    }
}
