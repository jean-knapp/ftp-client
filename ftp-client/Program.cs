using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FtpClient.Forms;

namespace FtpClient
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Without these, a fault on a background thread (a file-system watcher callback, a
            // continuation) would end the process with no explanation.
            Application.ThreadException += (s, e) => ReportFatal(e.Exception, false);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ReportFatal(e.ExceptionObject as Exception, true);
            TaskScheduler.UnobservedTaskException += (s, e) => { Log(e.Exception); e.SetObserved(); };
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.Run(new MainForm());
        }

        private static void ReportFatal(Exception exception, bool terminating)
        {
            Log(exception);
            try
            {
                var text = (exception?.Message ?? "Unknown error") +
                    "\n\nDetails were written to:\n" + LogPath;
                Dialogs.Error(null, terminating ? "FTP Client stopped" : "Unexpected error", text);
            }
            catch
            {
                // The dialog itself can fail while the process is coming down; the log still has it.
            }
        }

        private static string LogPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FtpClient", "error.log");

        private static void Log(Exception exception)
        {
            if (exception == null) return;
            try
            {
                var directory = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                var entry = new StringBuilder();
                entry.AppendLine("---- " + DateTime.Now.ToString("u") + " ----");
                entry.AppendLine(exception.ToString());
                File.AppendAllText(LogPath, entry.ToString(), Encoding.UTF8);
            }
            catch
            {
                // Logging must never throw.
            }
        }
    }
}
