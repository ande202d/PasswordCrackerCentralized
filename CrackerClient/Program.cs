using System;

namespace CrackerClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Client c = new Client();
            c.Start();

            Console.ReadKey(true);
        }
    }
}
