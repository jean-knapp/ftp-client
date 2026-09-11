using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FtpClient.Services
{
    /// <summary>
    /// Modern Windows folder picker (IFileOpenDialog with FOS_PICKFOLDERS). The stock
    /// <see cref="FolderBrowserDialog"/> in .NET Framework still shows the old tree dialog, which has
    /// no path box; this shows the normal Explorer dialog instead and falls back if it is unavailable.
    /// </summary>
    public static class FolderPicker
    {
        /// <summary>Shows the picker. Returns the chosen folder, or null when the user cancelled.</summary>
        public static string Show(IWin32Window owner, string title, string initialPath)
        {
            try
            {
                return ShowVista(owner, title, initialPath);
            }
            catch (Exception)
            {
                return ShowLegacy(owner, title, initialPath);
            }
        }

        private static string ShowLegacy(IWin32Window owner, string title, string initialPath)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = title;
                dialog.ShowNewFolderButton = true;
                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath)) dialog.SelectedPath = initialPath;
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        private const int ErrorCancelled = unchecked((int)0x800704C7);
        private const uint FosPickFolders = 0x00000020;
        private const uint FosForceFileSystem = 0x00000040;
        private const uint FosPathMustExist = 0x00000800;
        private const uint SigdnFileSysPath = 0x80058000;

        private static string ShowVista(IWin32Window owner, string title, string initialPath)
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogRcw();
            try
            {
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FosPickFolders | FosForceFileSystem | FosPathMustExist);
                if (!string.IsNullOrEmpty(title)) dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    object item;
                    if (SHCreateItemFromParsingName(initialPath, IntPtr.Zero, typeof(IShellItem).GUID, out item) == 0 && item != null)
                    {
                        dialog.SetFolder((IShellItem)item);
                    }
                }

                var handle = owner?.Handle ?? IntPtr.Zero;
                int hr = dialog.Show(handle);
                if (hr == ErrorCancelled) return null;
                if (hr != 0) Marshal.ThrowExceptionForHR(hr);

                IShellItem result;
                dialog.GetResult(out result);
                if (result == null) return null;
                try
                {
                    string path;
                    result.GetDisplayName(SigdnFileSysPath, out path);
                    return path;
                }
                finally
                {
                    Marshal.ReleaseComObject(result);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string path,
            IntPtr bindingContext,
            [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
            [MarshalAs(UnmanagedType.Interface)] out object item);

        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRcw { }

        [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr bindingContext, [MarshalAs(UnmanagedType.LPStruct)] Guid handlerId, [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId, out IntPtr result);
            void GetParent(out IShellItem parent);
            void GetDisplayName(uint kind, [MarshalAs(UnmanagedType.LPWStr)] out string name);
            void GetAttributes(uint mask, out uint attributes);
            void Compare(IShellItem other, uint hint, out int order);
        }

        // The member order mirrors the IModalWindow / IFileDialog / IFileOpenDialog vtable and must not change.
        [ComImport, Guid("d57c7288-d4ad-4768-be02-9d969532d960"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig] int Show(IntPtr parent);
            void SetFileTypes(uint count, IntPtr filterSpec);
            void SetFileTypeIndex(uint index);
            void GetFileTypeIndex(out uint index);
            void Advise(IntPtr events, out uint cookie);
            void Unadvise(uint cookie);
            void SetOptions(uint options);
            void GetOptions(out uint options);
            void SetDefaultFolder(IShellItem folder);
            void SetFolder(IShellItem folder);
            void GetFolder(out IShellItem folder);
            void GetCurrentSelection(out IShellItem item);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string name);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
            void GetResult(out IShellItem item);
            void AddPlace(IShellItem place, int order);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
            void Close(int result);
            void SetClientGuid([MarshalAs(UnmanagedType.LPStruct)] Guid client);
            void ClearClientData();
            void SetFilter(IntPtr filter);
            void GetResults(out IntPtr items);
            void GetSelectedItems(out IntPtr items);
        }
    }
}
