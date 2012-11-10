
namespace YABFcompiler
{
    using DIL;

    class LoopUnrollingResults
    {
        public DILOperationSet UnrolledInstructions { get; private set; }
        public bool WasLoopUnrolled { get; private set; }

        public LoopUnrollingResults(DILOperationSet operations, bool wasLoopUnrolled)
        {
            UnrolledInstructions = operations;
            WasLoopUnrolled = wasLoopUnrolled;
        }
    }
}
