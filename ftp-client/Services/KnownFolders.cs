using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FtpClient.Services
{
    /// <summary>Shell folders that Environment.SpecialFolder does not cover.</summary>
    public static class KnownFolders
    {
        private static readonly Guid DownloadsId = new Guid("374DE290-123F-4565-9164-39C4925E467B");

        [DllImport("shell32.dll")]
        private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid folderId, uint flags, IntPtr token, out IntPtr path);

        /// <summary>The user's Downloads folder, wherever they have moved it.</summary>
        public static string Downloads
        {
            get
            {
                try
                {
                    if (SHGetKnownFolderPath(DownloadsId, 0, IntPtr.Zero, out IntPtr pointer) == 0)
                    {
                        try
                        {
                            var path = Marshal.PtrToStringUni(pointer);
                            if (!string.IsNullOrEmpty(path)) return path;
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pointer);
                        }
                    }
                }
                catch (EntryPointNotFoundException)
                {
                }
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            }
        }
    }
}
