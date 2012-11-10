
namespace YABFcompiler.DIL
{
    interface IRepeatable
    {
        /// <summary>
        /// Returns true if the operation successfully compacted a number of operations into a single one
        /// </summary>
        /// <param name="operations"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        bool Repeat(DILOperationSet operations, int offset);
    }
}
