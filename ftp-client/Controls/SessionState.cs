using System.Drawing;
using FtpClient.Services;

namespace FtpClient.Controls
{
    /// <summary>A session's connection state, as its tab dot and the status bar show it.</summary>
    public enum SessionState
    {
        /// <summary>No session yet: the new-connection tab, which carries no dot.</summary>
        None,
        Connecting,
        Connected,
        Disconnected,
        Failed,
    }

    public static class SessionStates
    {
        /// <summary>The dot colour for a state, on the tabs and in the status bar.</summary>
        public static Color StateColor(SessionState state)
        {
            var p = Theme.Palette;
            switch (state)
            {
                case SessionState.Connected: return p.Up;
                case SessionState.Connecting: return p.Warning;
                case SessionState.Failed: return p.Error;
                default: return p.Foreground3;
            }
        }
    }
}
