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
            Console.WriteLine("Loading app bundle:");
            var bundle = new PebbleBundle("demo.pbw");
            Console.WriteLine(bundle);
            Console.WriteLine("Loading fw bundle:");
            bundle = new PebbleBundle("normal_ev2_4_v1.10.0.pbz");
            Console.WriteLine(bundle);
            Console.ReadLine();
        }
    }
}
