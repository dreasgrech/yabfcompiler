
namespace TestRun
{
    using System;

    internal class Program
    {
        private static void Main()
        {
            byte[] chArray = new byte[0x493e0];
            int index = 0;
            chArray[index] = (byte)(chArray[index] + 1);
            index++;
            chArray[index] = (byte)(chArray[index] + 3);
            index--;
            while (chArray[index] != 0)
            {
                chArray[index] = (byte)(chArray[index] + 1);
                index += 2;
                chArray[index] = (byte)(chArray[index] + 2);
                index -= 2;
            }
            index += 2;
            chArray[index] = (byte)(chArray[index] + 67);
            Console.Write((char)chArray[index]);

            Console.ReadKey();
        }
    }
}
