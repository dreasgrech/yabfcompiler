
namespace TestRun
{
    using System;

    internal class Program
    {
        private static void Main()
        {
            int index = 0;
            byte[] chArray = new byte[0x493e0];
            chArray[0] = 65;
            Console.WriteLine((char)chArray[0]);
            Console.ReadKey();


        }
    }





}
