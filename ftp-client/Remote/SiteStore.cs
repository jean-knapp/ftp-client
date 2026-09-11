using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using FtpClient.Services;

namespace FtpClient.Remote
{
    /// <summary>
    /// Saved sites, persisted as XML next to the settings. Passwords and key passphrases never go
    /// in this file - they live in Windows Credential Manager, see <see cref="CredentialStore"/>.
    /// </summary>
    public sealed class SiteStore
    {
        private static SiteStore _current;

        [XmlIgnore]
        public static SiteStore Current => _current ?? (_current = Load());

        public static string FilePath => Path.Combine(AppSettings.Folder, "sites.xml");

        public List<Site> Sites { get; set; } = new List<Site>();

        public event EventHandler Changed;

        public Site Find(string id) => Sites.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.Ordinal));

        /// <summary>Groups in first-seen order, sites without one last under <c>SITES</c>.</summary>
        public IEnumerable<IGrouping<string, Site>> Grouped()
        {
            return Sites
                .OrderBy(s => string.IsNullOrWhiteSpace(s.Group) ? 1 : 0)
                .GroupBy(s => string.IsNullOrWhiteSpace(s.Group) ? "Sites" : s.Group.Trim());
        }

        public void AddOrUpdate(Site site)
        {
            if (site == null) return;
            site.IsTransient = false;
            int index = Sites.FindIndex(s => s.Id == site.Id);
            if (index >= 0) Sites[index] = site;
            else Sites.Add(site);
            Save();
        }

        public void Remove(Site site)
        {
            if (site == null) return;
            Sites.RemoveAll(s => s.Id == site.Id);
            CredentialStore.Delete(site);
            Save();
        }

        public void Touch(Site site)
        {
            if (site == null || site.IsTransient) return;
            site.LastUsed = DateTime.Now;
            Save();
        }

        public static SiteStore Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var serializer = new XmlSerializer(typeof(SiteStore));
                    using (var stream = File.OpenRead(FilePath))
                    {
                        return (SiteStore)serializer.Deserialize(stream);
                    }
                }
            }
            catch
            {
                // A damaged file must not stop the application from starting.
            }
            return new SiteStore();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppSettings.Folder);
                var serializer = new XmlSerializer(typeof(SiteStore));
                var temp = FilePath + ".tmp";
                using (var stream = File.Create(temp))
                {
                    serializer.Serialize(stream, this);
                }
                // Write-then-replace, so a crash mid-save never leaves half a file behind.
                if (File.Exists(FilePath)) File.Replace(temp, FilePath, null);
                else File.Move(temp, FilePath);
            }
            catch
            {
                // A read-only profile loses the change, not the application.
            }
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
