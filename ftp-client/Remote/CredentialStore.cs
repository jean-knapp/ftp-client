using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FtpClient.Remote
{
    /// <summary>
    /// Site passwords and key passphrases, kept in Windows Credential Manager under the current
    /// user rather than in the settings file, where they would sit in plain text.
    /// </summary>
    public static class CredentialStore
    {
        private const int CredTypeGeneric = 1;
        private const int CredPersistLocalMachine = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public int Flags;
            public int Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref Credential credential, int flags);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, int type, int flags);

        [DllImport("advapi32.dll", EntryPoint = "CredFree")]
        private static extern void CredFree(IntPtr buffer);

        private static string Target(Site site) => "FtpClient/site/" + site.Id;

        /// <summary>The stored secret for a site, or null.</summary>
        public static string GetSecret(Site site)
        {
            if (site == null) return null;
            IntPtr pointer;
            if (!CredRead(Target(site), CredTypeGeneric, 0, out pointer)) return null;
            try
            {
                var credential = (Credential)Marshal.PtrToStructure(pointer, typeof(Credential));
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0) return string.Empty;
                return Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
            }
            finally
            {
                CredFree(pointer);
            }
        }

        public static void SetSecret(Site site, string secret)
        {
            if (site == null) return;
            if (string.IsNullOrEmpty(secret))
            {
                Delete(site);
                return;
            }

            var bytes = Encoding.Unicode.GetBytes(secret);
            var blob = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, blob, bytes.Length);
                var credential = new Credential
                {
                    Type = CredTypeGeneric,
                    TargetName = Target(site),
                    Comment = "FTP Client - " + (site.Name ?? site.Host),
                    CredentialBlobSize = bytes.Length,
                    CredentialBlob = blob,
                    Persist = CredPersistLocalMachine,
                    UserName = site.User ?? string.Empty,
                };
                if (!CredWrite(ref credential, 0))
                {
                    throw new InvalidOperationException("Windows Credential Manager refused to store the password (error " + Marshal.GetLastWin32Error() + ").");
                }
            }
            finally
            {
                // Zero the unmanaged copy before releasing it.
                for (int i = 0; i < bytes.Length; i++) Marshal.WriteByte(blob, i, 0);
                Marshal.FreeHGlobal(blob);
                Array.Clear(bytes, 0, bytes.Length);
            }
        }

        public static void Delete(Site site)
        {
            if (site == null) return;
            CredDelete(Target(site), CredTypeGeneric, 0);
        }
    }
}
