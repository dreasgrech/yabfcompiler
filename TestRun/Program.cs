
namespace TestRun
{
    using System;

    internal class Program
    {
        private static void Main()
        {
            int index = 0;
            char[] chArray = new char[0x493e0];
            //chArray[index] = (char)(chArray[index] + '\x0005');
            //while (chArray[index] != '\0')
            //{
                index++;
                chArray[index] = (char)(chArray[index] + '\x0005');
                chArray[index + 1] = (char)(chArray[index + 1] + ((char)(chArray[index] * '\x0004')));
                chArray[index] = '\0';
                index++;
                chArray[index] = (char)(chArray[index] + '\x0001');
                chArray[index + 1] = (char)(chArray[index + 1] + chArray[index]);
                chArray[index] = '\0';
                index -= 2;
                chArray[index] = (char)(chArray[index] - '\x0001');
            //}
            //index += 3;
            Console.Write(chArray[index]);

        }
    }





}
