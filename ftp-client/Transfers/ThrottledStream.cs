using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FtpClient.Transfers
{
    /// <summary>
    /// A token bucket shared by every transfer, so the speed limit caps the application's total
    /// throughput rather than each file's.
    /// </summary>
    public sealed class RateLimiter
    {
        private readonly object _gate = new object();
        private long _bytesPerSecond;
        private double _available;
        private DateTime _last = DateTime.UtcNow;

        /// <summary>Bytes per second; 0 or less means unlimited.</summary>
        public long BytesPerSecond
        {
            get { lock (_gate) return _bytesPerSecond; }
            set
            {
                lock (_gate)
                {
                    _bytesPerSecond = Math.Max(0, value);
                    _available = Math.Min(_available, _bytesPerSecond);
                }
            }
        }

        public async Task WaitAsync(int bytes, CancellationToken cancellationToken)
        {
            while (true)
            {
                TimeSpan delay;
                lock (_gate)
                {
                    if (_bytesPerSecond <= 0) return;
                    var now = DateTime.UtcNow;
                    _available = Math.Min(_bytesPerSecond, _available + (now - _last).TotalSeconds * _bytesPerSecond);
                    _last = now;
                    // A buffer larger than one second's allowance may borrow, or it would never pass.
                    double need = Math.Min(bytes, _bytesPerSecond);
                    if (_available >= need)
                    {
                        _available -= bytes;
                        return;
                    }
                    delay = TimeSpan.FromSeconds((need - _available) / _bytesPerSecond);
                }
                if (delay < TimeSpan.FromMilliseconds(5)) delay = TimeSpan.FromMilliseconds(5);
                if (delay > TimeSpan.FromMilliseconds(250)) delay = TimeSpan.FromMilliseconds(250);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Passes reads and writes through a <see cref="RateLimiter"/> and honours cancellation on
    /// every buffer, including from protocol libraries that only call the synchronous members.
    /// </summary>
    public sealed class ThrottledStream : Stream
    {
        private readonly Stream _inner;
        private readonly RateLimiter _limiter;
        private readonly CancellationToken _cancellation;

        public ThrottledStream(Stream inner, RateLimiter limiter, CancellationToken cancellation)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _limiter = limiter;
            _cancellation = cancellation;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _cancellation.ThrowIfCancellationRequested();
            int read = _inner.Read(buffer, offset, count);
            if (read > 0 && _limiter != null) _limiter.WaitAsync(read, _cancellation).GetAwaiter().GetResult();
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_cancellation, cancellationToken))
            {
                int read = await _inner.ReadAsync(buffer, offset, count, linked.Token).ConfigureAwait(false);
                if (read > 0 && _limiter != null) await _limiter.WaitAsync(read, linked.Token).ConfigureAwait(false);
                return read;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _cancellation.ThrowIfCancellationRequested();
            if (_limiter != null) _limiter.WaitAsync(count, _cancellation).GetAwaiter().GetResult();
            _inner.Write(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(_cancellation, cancellationToken))
            {
                if (_limiter != null) await _limiter.WaitAsync(count, linked.Token).ConfigureAwait(false);
                await _inner.WriteAsync(buffer, offset, count, linked.Token).ConfigureAwait(false);
            }
        }

        public override void Flush() => _inner.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);

        // The owner disposes the wrapped stream.
        protected override void Dispose(bool disposing) => base.Dispose(disposing);
    }
}
