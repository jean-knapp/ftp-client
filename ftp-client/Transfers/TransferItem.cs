using System;
using System.Threading;
using FtpClient.Remote;

namespace FtpClient.Transfers
{
    public enum TransferDirection
    {
        Upload,
        Download,
        /// <summary>A remote delete that belongs to a commit deploy.</summary>
        Delete,
    }

    public enum TransferStatus
    {
        Queued,
        Active,
        Paused,
        Completed,
        Failed,
        Skipped,
        Cancelled,
    }

    /// <summary>One file moving in one direction, with live progress.</summary>
    public sealed class TransferItem
    {
        private const double SampleWindowSeconds = 0.5;
        private DateTime _sampleTime;
        private long _sampleBytes;

        internal TransferItem(long id, Site site, TransferDirection direction, string localPath, string remotePath)
        {
            Id = id;
            Site = site;
            Direction = direction;
            LocalPath = localPath;
            RemotePath = remotePath;
            QueuedAt = DateTime.Now;
        }

        public long Id { get; }
        public Site Site { get; }
        public TransferDirection Direction { get; }
        public string LocalPath { get; internal set; }
        public string RemotePath { get; internal set; }

        public TransferStatus Status { get; internal set; }

        /// <summary>Why the item failed or was skipped, in the server's words when there are some.</summary>
        public string Error { get; internal set; }

        public long TotalBytes { get; internal set; }
        public long TransferredBytes { get; internal set; }

        /// <summary>Smoothed throughput while active; 0 otherwise.</summary>
        public double BytesPerSecond { get; internal set; }

        public int Attempts { get; internal set; }

        public DateTime QueuedAt { get; }
        public DateTime? StartedAt { get; internal set; }
        public DateTime? FinishedAt { get; internal set; }

        /// <summary>The commit a deploy item came from, when it came from one.</summary>
        public string BatchTag { get; internal set; }

        public string FileName =>
            Direction == TransferDirection.Download
                ? System.IO.Path.GetFileName(LocalPath ?? string.Empty)
                : RemotePath != null ? Remote.RemotePath.Name(RemotePath) : System.IO.Path.GetFileName(LocalPath ?? string.Empty);

        /// <summary>The path shown under the name: where the file ends up.</summary>
        public string TargetPath => Direction == TransferDirection.Download ? LocalPath : RemotePath;

        public double Fraction
        {
            get
            {
                if (Status == TransferStatus.Completed) return 1;
                if (TotalBytes <= 0) return 0;
                return Math.Max(0, Math.Min(1, TransferredBytes / (double)TotalBytes));
            }
        }

        public TimeSpan? Eta
        {
            get
            {
                if (Status != TransferStatus.Active || BytesPerSecond < 1 || TotalBytes <= 0) return null;
                return TimeSpan.FromSeconds(Math.Max(0, TotalBytes - TransferredBytes) / BytesPerSecond);
            }
        }

        public bool IsFinished =>
            Status == TransferStatus.Completed || Status == TransferStatus.Failed ||
            Status == TransferStatus.Skipped || Status == TransferStatus.Cancelled;

        /// <summary>
        /// The transfer has written to its destination before, so a partial copy found there on a
        /// retry or after a pause is its own and is continued rather than asked about.
        /// </summary>
        internal bool Started { get; set; }

        internal CancellationTokenSource Cancellation { get; set; }
        internal bool PauseRequested { get; set; }

        internal void BeginSampling(long bytes)
        {
            _sampleTime = DateTime.UtcNow;
            _sampleBytes = bytes;
            BytesPerSecond = 0;
        }

        /// <summary>Folds a progress report into the smoothed speed; true when a window closed.</summary>
        internal bool Sample(long bytes)
        {
            TransferredBytes = bytes;
            var now = DateTime.UtcNow;
            double seconds = (now - _sampleTime).TotalSeconds;
            if (seconds < SampleWindowSeconds) return false;
            double instant = (bytes - _sampleBytes) / seconds;
            BytesPerSecond = BytesPerSecond <= 0 ? instant : BytesPerSecond * 0.6 + instant * 0.4;
            _sampleTime = now;
            _sampleBytes = bytes;
            return true;
        }
    }

    /// <summary>What the conflict dialog compares, and what it may offer.</summary>
    public sealed class ConflictInfo
    {
        public TransferItem Item { get; internal set; }

        /// <summary>The file being sent: local for uploads, remote for downloads.</summary>
        public long IncomingSize { get; internal set; }
        public DateTime? IncomingModified { get; internal set; }

        /// <summary>The file already at the destination.</summary>
        public long ExistingSize { get; internal set; }
        public DateTime? ExistingModified { get; internal set; }

        /// <summary>The destination is a shorter copy and the protocol can append.</summary>
        public bool CanResume { get; internal set; }

        /// <summary>The name a Rename would use, e.g. <c>index (1).php</c>.</summary>
        public string RenameTo { get; internal set; }

        /// <summary>Other queued items that will also meet an existing file, as far as is known.</summary>
        public int RemainingConflicts { get; internal set; }
    }

    public enum ConflictChoice
    {
        Overwrite,
        OverwriteIfNewer,
        Resume,
        Rename,
        Skip,
        Cancel,
    }

    public sealed class ConflictResolution
    {
        public ConflictResolution(ConflictChoice choice, bool applyToRemaining = false)
        {
            Choice = choice;
            ApplyToRemaining = applyToRemaining;
        }

        public ConflictChoice Choice { get; }
        public bool ApplyToRemaining { get; }
    }

    /// <summary>Raised when every item of a tagged batch has finished one way or another.</summary>
    public sealed class BatchSettledEventArgs : EventArgs
    {
        public BatchSettledEventArgs(string tag, Site site, bool succeeded, int completed, int total)
        {
            Tag = tag;
            Site = site;
            Succeeded = succeeded;
            Completed = completed;
            Total = total;
        }

        public string Tag { get; }
        public Site Site { get; }

        /// <summary>Every item completed; a skip or failure leaves the commit pending.</summary>
        public bool Succeeded { get; }

        public int Completed { get; }
        public int Total { get; }
    }
}
