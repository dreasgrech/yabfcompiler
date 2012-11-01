
namespace TestRun
{
    using System;

    internal class Program
    {
        // Methods
        public static void Main()
        {
            int index = 0;
            char[] chArray = new char[0x493e0];
            chArray[index] = 'A';
            Console.Write(chArray[index]);
            index++;
            chArray[index] = 'B';
            Console.Write(chArray[index]);

        }

    }
}
