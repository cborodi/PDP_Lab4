using PDP_Lab4_TestImplementation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PPD_Lab4_TestImplementation
{
    class Program
    {
        static void Main(string[] args)
        {
            var url_list = new string[] {
                "www.cs.ubbcluj.ro/~rlupsa/edu/pdp/",
                "www.cs.ubbcluj.ro/~rlupsa/edu/pdp/lab-4-futures-continuations.html",
                "www.cs.ubbcluj.ro/files/orar/2021-1/tabelar/index.html"
            }.ToList();

            //Callbacks.run(url_list);
            //Tasks.run(url_list);
            TaskAsyncAwait.run(url_list);
        }
    }
}
