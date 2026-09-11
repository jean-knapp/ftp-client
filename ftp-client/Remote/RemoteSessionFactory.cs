namespace FtpClient.Remote
{
    /// <summary>Picks the session implementation for a site's protocol.</summary>
    public static class RemoteSessionFactory
    {
        public static bool IsSupported(RemoteProtocol protocol) => true;

        /// <param name="secret">The password or key passphrase; null when the site needs none.</param>
        public static IRemoteSession Create(Site site, SessionLog log, string secret)
        {
            switch (site.Protocol)
            {
                case RemoteProtocol.Local:
                    return new LocalFolderSession(site, log);
                case RemoteProtocol.Sftp:
                    return new SftpSession(site, log, secret);
                default:
                    return new FtpSession(site, log, secret);
            }
        }
    }
}
