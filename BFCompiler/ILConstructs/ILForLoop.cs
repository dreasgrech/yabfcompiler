
namespace YABFcompiler.ILConstructs
{
    using System.Reflection.Emit;

    internal class ILForLoop
    {
        public Label ConditionLabel { get; private set; }
        public Label StartLoopLogicLabel { get; private set; }
        public LocalBuilder Counter { get; private set; }
        public LocalBuilder Max { get; private set; }

        public ILForLoop(Label conditionLabel, Label startLoopLogicLabel, LocalBuilder counter, LocalBuilder max)
        {
            ConditionLabel = conditionLabel;
            Counter = counter;
            Max = max;
            StartLoopLogicLabel = startLoopLogicLabel;
        }
    }
}
