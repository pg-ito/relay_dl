using System;
using System.Net;
using System.Threading.Tasks;

namespace relay_dl
{
    class Program
    {
        static void Main(string[] args)
        {
            DlServer sv = new DlServer();
            if (args.Length > 0) {
                sv.Docroot = args[0];
            }
            Task.Run(sv.Listen).Wait();// @TODO Cancel
        }
    }
}
