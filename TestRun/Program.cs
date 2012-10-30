
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
            index++;
            chArray[index] = (char)(chArray[index] + '\t');
            while (chArray[index] != '\0')
            {
                index--;
                chArray[index] = (char)(chArray[index] + '\b');
                index++;
                chArray[index] = (char)(chArray[index] - '\x0001');
            }
            index--;
            Console.Write(chArray[index]);
            index++;
            chArray[index] = (char)(chArray[index] + '\a');
            while (chArray[index] != '\0')
            {
                index--;
                chArray[index] = (char)(chArray[index] + '\x0004');
                index++;
                chArray[index] = (char)(chArray[index] - '\x0001');
            }
            index--;
            chArray[index] = (char)(chArray[index] + '\x0001');
            Console.Write(chArray[index]);
            chArray[index] = (char)(chArray[index] + '\a');
            int num2 = 2;
            for (int i = 0; i < num2; i++)
            {
                Console.Write(chArray[index]);
            }
            chArray[index] = (char)(chArray[index] + '\x0003');
            Console.Write(chArray[index]);
            index += 3;
            chArray[index] = (char)(chArray[index] + '\b');
            while (chArray[index] != '\0')
            {
                index--;
                chArray[index] = (char)(chArray[index] + '\x0004');
                index++;
                chArray[index] = (char)(chArray[index] - '\x0001');
            }
            index--;
            Console.Write(chArray[index]);
            index += 3;
            chArray[index] = (char)(chArray[index] + '\n');
            while (chArray[index] != '\0')
            {
                index--;
                chArray[index] = (char)(chArray[index] + '\t');
                index++;
                chArray[index] = (char)(chArray[index] - '\x0001');
            }
            index--;
            chArray[index] = (char)(chArray[index] - '\x0003');
            Console.Write(chArray[index]);
            index -= 4;
            Console.Write(chArray[index]);
            chArray[index] = (char)(chArray[index] + '\x0003');
            Console.Write(chArray[index]);
            chArray[index] = (char)(chArray[index] - '\x0006');
            Console.Write(chArray[index]);
            chArray[index] = (char)(chArray[index] - '\b');
            Console.Write(chArray[index]);
            index += 2;
            chArray[index] = (char)(chArray[index] + '\x0001');
            Console.Write(chArray[index]);



        }
    }
}
