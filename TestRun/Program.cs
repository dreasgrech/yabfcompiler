
namespace TestRun
{
    using System;

    internal class Program
    {
        static void Main(string[] args)
        {
            //var array = new char[300000];
            //int ptr = 0;

            int index = 0;
            char[] chArray = new char[0x493e0];
            chArray[index] = (char)(chArray[index] + '\x0001');
            while (chArray[index] != '\0')
            {
                chArray[index] = (char)(chArray[index] + '\x0001');
            }
            Console.Write(chArray[index]);

        }
    }
}
