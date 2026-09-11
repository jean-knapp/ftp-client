using System;
using System.Collections.Generic;

namespace FtpClient.Remote
{
    /// <summary>The four columns of the protocol log, plus warnings that share the status colour.</summary>
    public enum LogKind
    {
        Status,
        /// <summary>A status line reporting that something worked, tagged in --up.</summary>
        Success,
        Send,
        Receive,
        Warning,
        Error,
    }

    public sealed class LogEntry
    {
        public LogEntry(LogKind kind, string text)
        {
            Time = DateTime.Now;
            Kind = kind;
            Text = text ?? string.Empty;
        }

        public DateTime Time { get; }
        public LogKind Kind { get; }
        public string Text { get; }

        public string Tag
        {
            get
            {
                switch (Kind)
                {
                    case LogKind.Send: return "SEND";
                    case LogKind.Receive: return "RECV";
                    case LogKind.Error: return "ERROR";
                    default: return "STATUS";
                }
            }
        }

        public override string ToString() => Time.ToString("HH:mm:ss.fff") + "  " + Tag.PadRight(7) + Text;
    }

    /// <summary>
    /// Everything a site's connections said and heard, newest last. One log is shared by every
    /// connection to a site - the browsing session and the transfer workers - so the protocol log
    /// window tells the whole story. Appends come from worker threads; <see cref="Added"/> is raised
    /// on the appending thread and the UI marshals it.
    /// </summary>
    public sealed class SessionLog
    {
        private const int Capacity = 5000;
        private readonly List<LogEntry> _entries = new List<LogEntry>();
        private readonly object _gate = new object();

        public event EventHandler<LogEntry> Added;
        public event EventHandler Cleared;

        public void Status(string text) => Append(LogKind.Status, text);
        public void Success(string text) => Append(LogKind.Success, text);
        public void Send(string text) => Append(LogKind.Send, text);
        public void Receive(string text) => Append(LogKind.Receive, text);
        public void Warning(string text) => Append(LogKind.Warning, text);
        public void Error(string text) => Append(LogKind.Error, text);

        public void Append(LogKind kind, string text)
        {
            var entry = new LogEntry(kind, text);
            lock (_gate)
            {
                _entries.Add(entry);
                if (_entries.Count > Capacity) _entries.RemoveRange(0, _entries.Count - Capacity);
            }
            Added?.Invoke(this, entry);
        }

        public List<LogEntry> Snapshot()
        {
            lock (_gate) return new List<LogEntry>(_entries);
        }

        public void Clear()
        {
            lock (_gate) _entries.Clear();
            Cleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
