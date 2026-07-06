using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace M5748SwUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            SwUpdater swUpdater = new SwUpdater();

            try
            {
                swUpdater.Run();
            }
            catch(Exception ex)
            {
                Console.WriteLine(""); Console.WriteLine("");
                Console.WriteLine("Unexpected Error");
                Console.Write(ex.ToString());
            }

            Console.WriteLine("");
            Console.WriteLine("Press any key to exit.");
            while (Console.KeyAvailable) { Console.ReadKey(true); }
            Console.ReadKey(true);
        }
    }
}
