
namespace YABFcompiler
{
    class CompactOperationsResults
    {
        public int Delta { get; private set; }
        public int TotalInstructionsCovered { get; private set; }
        public int Offset { get; private set; }

        public CompactOperationsResults(int delta, int totalInstructionsCovered, int offset)
        {
            Delta = delta;
            TotalInstructionsCovered = totalInstructionsCovered;
            Offset = offset;
        }
    }
}
