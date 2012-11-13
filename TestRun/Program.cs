
namespace TestRun
{
    using System;

    internal class Program
    {
        private static void Main()
        {
            byte[] chArray = new byte[0x493e0];
            int index = 0;
            Console.Write(chArray[index]);
            chArray[index] = (byte)(chArray[index] + '\x0001');
            while (chArray[index] != '\0')
            {
                Console.Write((char)chArray[index]);
                chArray[index] = (byte)(chArray[index] + '\x0001');
            }
        }
    }
}
