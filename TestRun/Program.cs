
namespace TestRun
{
    using System;

    internal class Program
    {
        static void Main()
        {
      int index = 0;
    char[] chArray = new char[0x493e0];
    index += 9;
    chArray[index] = (char) (chArray[index] + '\b');
    chArray[index - 1] = (char) (chArray[index - 1] + ((char) (chArray[index] * '\t')));
    chArray[index] = '\0';
    index--;
    Console.Write(chArray[index]);
    index++;
    chArray[index] = (char) (chArray[index] + '\x0004');
    chArray[index - 1] = (char) (chArray[index - 1] + ((char) (chArray[index] * '\a')));
    chArray[index] = '\0';
    index--;
    chArray[index] = (char) (chArray[index] + '\x0001');
    Console.Write(chArray[index]);

        }
    }
}