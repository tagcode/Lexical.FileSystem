// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
namespace Lexical.FileSystem.Operation
{
    /// <summary>Operation State</summary>
    public enum OperationState : int
    {
        /// <summary>Operation has been initialized</summary>
        Initialized = 0,
        /// <summary>Operation size and viability are being estimated</summary>
        Estimating = 1,
        /// <summary>Operation size and viability have been estimated</summary>
        Estimated = 2,
        /// <summary>Started and running</summary>
        Running = 3,
        /// <summary>Action skipped</summary>
        Skipped = 4,
        /// <summary>Run completed ok</summary>
        Completed = 5,
        /// <summary>Run interrupted with cancellation token</summary>
        Cancelled = 6,
        /// <summary>Run failed</summary>
        Error = 7,
    }
}
