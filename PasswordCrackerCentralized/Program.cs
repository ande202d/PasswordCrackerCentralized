using System;

namespace PasswordCrackerCentralized
{
    class Program
    {
        static void Main()
        {
            //Cracking cracker = new Cracking();
            //cracker.RunCracking();

            Server s = new Server();
            s.Start();

            Console.ReadKey();
        }
    }
}
