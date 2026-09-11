using System;

namespace FtpClient.Remote
{
    public enum TrustKind
    {
        /// <summary>An SSH server's host key.</summary>
        HostKey,
        /// <summary>A TLS certificate that did not validate against the Windows store.</summary>
        Certificate,
    }

    /// <summary>What a server presented, for the user to judge.</summary>
    public sealed class TrustRequest
    {
        public Site Site { get; set; }
        public TrustKind Kind { get; set; }

        /// <summary>Key type, e.g. <c>ed25519</c>; empty for certificates.</summary>
        public string Algorithm { get; set; }

        /// <summary><c>SHA256:…</c> for host keys, the SHA-1 thumbprint for certificates.</summary>
        public string Fingerprint { get; set; }

        /// <summary>The fingerprint trusted before, when this one replaces it.</summary>
        public string PreviousFingerprint { get; set; }

        public string Subject { get; set; }
        public string Issuer { get; set; }

        /// <summary>Why a certificate failed validation.</summary>
        public string Problem { get; set; }

        public bool IsChange => !string.IsNullOrEmpty(PreviousFingerprint);

        internal string StoredValue => (Kind == TrustKind.Certificate ? "TLS " : string.Empty) + Fingerprint;
    }

    /// <summary>
    /// Trust on first use: a site remembers the host key (or the unverifiable certificate) the user
    /// accepted, connections that present it again go straight through, and a different one is asked
    /// about. Connections call this on worker threads; prompts are serialised, so the pooled transfer
    /// connections opening at once ask only once.
    /// </summary>
    public static class HostTrust
    {
        private static readonly object Gate = new object();

        /// <summary>Shows the question; returns true to trust. Supplied by the main window.</summary>
        public static Func<TrustRequest, bool> Prompt { get; set; }

        /// <summary>Raised after the user trusted something new, so a saved site can be stored.</summary>
        public static event EventHandler<TrustRequest> Trusted;

        public static bool Verify(TrustRequest request)
        {
            if (request?.Site == null) return false;
            lock (Gate)
            {
                var site = request.Site;
                if (string.Equals(site.TrustedHostKey, request.StoredValue, StringComparison.Ordinal)) return true;

                // A certificate trust does not carry over to a host key and the other way round.
                bool sameKind = !string.IsNullOrEmpty(site.TrustedHostKey) && site.TrustedHostKey.StartsWith("TLS ", StringComparison.Ordinal) == (request.Kind == TrustKind.Certificate);
                if (sameKind) request.PreviousFingerprint = request.Kind == TrustKind.Certificate ? site.TrustedHostKey.Substring(4) : site.TrustedHostKey;

                var prompt = Prompt;
                if (prompt == null || !prompt(request)) return false;
                site.TrustedHostKey = request.StoredValue;
            }
            Trusted?.Invoke(null, request);
            return true;
        }
    }
}
