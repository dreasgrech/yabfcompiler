
namespace YABFcompiler
{
    internal class MatchingOperationChanges
    {
        public int ChangesResult { get; set; }
        public int TotalNumberOfChanges { get; private set; }

        public MatchingOperationChanges(int changesResult, int totalNumberOfChanges)
        {
            ChangesResult = changesResult;
            TotalNumberOfChanges = totalNumberOfChanges;
        }
    }
}
