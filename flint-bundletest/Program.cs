using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using flint;

namespace flint_bundletest
{
    class Program
    {
        static void Main(string[] args)
        {
            var bundle = new PebbleBundle("demo.pbw");
            Console.WriteLine(bundle.Application);
            Console.ReadLine();

        }
    }
}
