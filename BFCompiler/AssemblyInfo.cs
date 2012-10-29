
namespace YABFcompiler
{
    using System.Reflection.Emit;

    internal class AssemblyInfo
    {
        public AssemblyBuilder DynamicAssembly { get; private set; }
        public TypeBuilder MainClass { get; private set; }
        public MethodBuilder MainMethod { get; private set; }

        public AssemblyInfo(AssemblyBuilder dynamicAssembly, TypeBuilder mainClass, MethodBuilder mainMethod)
        {
            DynamicAssembly = dynamicAssembly;
            MainClass = mainClass;
            MainMethod = mainMethod;
        }
    }
}
