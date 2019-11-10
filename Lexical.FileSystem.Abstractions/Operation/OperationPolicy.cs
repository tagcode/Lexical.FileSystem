// --------------------------------------------------------
// Copyright:      Toni Kalajainen
// Date:           17.10.2019
// Url:            http://lexical.fi
// --------------------------------------------------------
using System;
using System.IO;

namespace Lexical.FileSystem.Operation
{
    /// <summary>File operation policy</summary>
    [Flags]
    public enum OperationPolicy : ulong
    {
        /// <summary>Policy is not set. If used in FileOperation, then inherits policy from its session.</summary>
        Unset = 0UL,

        // Policy on source files (choose one)
        /// <summary>Source policy is not set.</summary>
        SrcUnset = 0x00UL,
        /// <summary>Throw <see cref="FileNotFoundException"/> or <see cref="DirectoryNotFoundException"/> if source files or directories are not found.</summary>
        SrcThrow = 0x01UL,
        /// <summary>If source files or directories are not found, then operation is skipped</summary>
        SrcSkip = 0x02UL,
        /// <summary>Source policy mask</summary>
        SrcMask = 0xffUL,

        // Policy on destination files (choose one)
        /// <summary>Destination policy is not set.</summary>
        DstUnset = 0x00UL << 8,
        /// <summary>If destination file already exists (or doesn't exist on delete), throw <see cref="FileSystemExceptionFileExists"/> or <see cref="FileSystemExceptionDirectoryExists"/>.</summary>
        DstThrow = 0x01UL << 8,
        /// <summary>If destination file already exists, skip the operation on them.</summary>
        DstSkip = 0x02UL << 8,
        /// <summary>If destination file already exists, overwrite it.</summary>
        DstOverwrite = 0x03UL << 8,
        /// <summary>Destination policy mask</summary>
        DstMask = 0xffUL << 8,

        // Estimate policies
        /// <summary>No estimate flags</summary>
        EstimateUnset = 0x00UL << 16,
        /// <summary>Estimate on Run(). Public method Estimate() does nothing.</summary>
        EstimateOnRun = 0x01UL << 16,
        /// <summary>Re-estimate on Run(). Runs on Estimate() and Run().</summary>
        ReEstimateOnRun = 0x01UL << 16,
        /// <summary>Estimate flags mask</summary>
        EstimateMask = 0xffUL << 16,

        // Rollback policies
        /// <summary>No rollback flags</summary>
        RollbackUnset = 0x00UL << 24,
        /// <summary>Rollback flags mask</summary>
        RollbackMask = 0xffUL << 24,

        // Other flags
        /// <summary>If one operation fails, signals cancel on <see cref="FileOperationSession.CancelSrc"/> cancel token source.</summary>
        CancelOnError = 0x0001UL << 32,
        /// <summary>Policy whether to omit directories that are mounted packages, such as .zip.</summary>
        OmitMountedPackages = 0x0002UL << 32,
        /// <summary>Batch operation continues on child op error. Throws <see cref="AggregateException"/> on errors, but only after all child ops have been ran.</summary>
        BatchContinueOnError = 0x0004UL << 32,
        /// <summary>Suppress exception in Estimate() and Run().</summary>
        SuppressException = 0x0008UL << 32,
        /// <summary>Log events to session.</summary>
        LogEvents = 0x0010UL << 32,
        /// <summary>Dispatch events to subscribers.</summary>
        DispatchEvents = 0x0020UL << 32,
        /// <summary>Mask for flags</summary>
        FlagsMask = 0xffffUL << 32,

        /// <summary>Default policy</summary>
        Default = SrcSkip | DstThrow | OmitMountedPackages | LogEvents | DispatchEvents
    }
}
