namespace FtpClient.Controls
{
    /// <summary>
    /// SVG glyphs from the FTP Client handoff, on its 24 and 16 unit grids. Every glyph uses
    /// <c>currentColor</c>, so the controls tint them to the token they sit in.
    /// </summary>
    public static class Icons
    {
        private const string Open = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">";
        private const string Open16 = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 16 16\">";
        private const string Open12 = "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 12 12\">";
        private const string Close = "</svg>";

        // ------------------------------------------------------------------ servers and files

        public const string Server = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M4 6h16v5H4zM4 15h16v5H4z\"/><circle fill=\"currentColor\" cx=\"7.5\" cy=\"8.5\" r=\"1.2\"/><circle fill=\"currentColor\" cx=\"7.5\" cy=\"17.5\" r=\"1.2\"/>" + Close;
        /// <summary>The server outline without its status lights, used inside the breadcrumb field.</summary>
        public const string ServerOutline = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M4 6h16v5H4zM4 15h16v5H4z\"/>" + Close;
        public const string Folder = Open + "<path fill=\"currentColor\" d=\"M3 5h6l2 2h10v12H3z\"/>" + Close;
        public const string FolderOpen = Open + "<path fill=\"currentColor\" d=\"M3 5h6l2 2h9v3H7.5L5 18H3z\"/><path fill=\"currentColor\" d=\"M7.8 11H22l-3 8H5z\"/>" + Close;
        public const string File = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M6 3h8l4 4v14H6z\"/>" + Close;
        /// <summary>A file with its folded corner, for the properties dialog tile.</summary>
        public const string FileDetailed = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M6 3h8l4 4v14H6z\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M13 3v5h5\"/>" + Close;
        public const string FolderLink = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M4 6h5l2 2h9v11H4z\"/><path fill=\"currentColor\" d=\"M8.5 17l4.3-4.3H10v-1.7h5.5v5.5h-1.7v-2.8L9.7 18z\"/>" + Close;
        public const string Symlink =Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M6 3h8l4 4v14H6z\"/><path fill=\"currentColor\" d=\"M9 16l4.3-4.3H10.5V10H16v5.5h-1.7v-2.8L10 17z\"/>" + Close;

        // ------------------------------------------------------------------ transfers

        public const string Upload = Open + "<path fill=\"currentColor\" d=\"M12 3l5.9 5.9-1.4 1.4L13 6.8V16h-2V6.8L7.5 10.3 6.1 8.9z\"/><path fill=\"currentColor\" d=\"M4 18h16v2H4z\"/>" + Close;
        public const string Download = Open + "<path fill=\"currentColor\" d=\"M11 3h2v9.2l3.5-3.5 1.4 1.4L12 16 6.1 10.1l1.4-1.4L11 12.2z\"/><path fill=\"currentColor\" d=\"M4 18h16v2H4z\"/>" + Close;
        /// <summary>The upload arrow into an open tray, drawn in the drop target.</summary>
        public const string UploadTray = Open + "<path fill=\"currentColor\" d=\"M12 3l5.9 5.9-1.4 1.4L13 6.8V15h-2V6.8L7.5 10.3 6.1 8.9z\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M4 15v5h16v-5\"/>" + Close;
        public const string Pause = Open + "<path fill=\"currentColor\" d=\"M7 5h3v14H7zM14 5h3v14h-3z\"/>" + Close;
        public const string Resume = Open + "<path fill=\"currentColor\" d=\"M8 5l11 7-11 7z\"/>" + Close;

        // ------------------------------------------------------------------ commands

        public const string NewFolder = Open + "<path fill=\"currentColor\" d=\"M3 5h6l2 2h10v12H3z\"/><path fill=\"#000\" fill-opacity=\"0.55\" d=\"M12 11h2v2h2v2h-2v2h-2v-2h-2v-2h2z\"/>" + Close;
        public const string Rename = Open + "<path fill=\"currentColor\" d=\"M3 17.3V21h3.7L17.8 9.9l-3.7-3.7zM20.7 7a1 1 0 0 0 0-1.4l-2.3-2.3a1 1 0 0 0-1.4 0l-1.8 1.8 3.7 3.7z\"/>" + Close;
        public const string Delete = Open + "<path fill=\"currentColor\" d=\"M6 7h12l-1 13H7zM9 3h6v2H9z\"/>" + Close;
        public const string Permissions = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" d=\"M7.5 10V7.5a4.5 4.5 0 0 1 9 0V10\"/><rect fill=\"currentColor\" x=\"5\" y=\"10\" width=\"14\" height=\"10\" rx=\"2\"/>" + Close;
        public const string Refresh = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M19 12c0 3.9-3.1 7-7 7s-7-3.1-7-7 3.1-7 7-7c2.4 0 4.5 1.2 5.8 3\"/><path fill=\"currentColor\" d=\"M20 3v6h-6z\"/>" + Close;
        public const string Search = Open16 + "<circle cx=\"6.5\" cy=\"6.5\" r=\"4.6\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/><path stroke=\"currentColor\" stroke-width=\"1.6\" d=\"M10 10l4 4\"/>" + Close;
        public const string List = Open + "<path fill=\"currentColor\" d=\"M4 6h16v2H4zM4 11h16v2H4zM4 16h16v2H4z\"/>" + Close;
        public const string Grid = Open + "<path fill=\"currentColor\" d=\"M4 4h7v7H4zM13 4h7v7h-7zM4 13h7v7H4zM13 13h7v7h-7z\"/>" + Close;
        public const string Terminal = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M4 5h16v14H4z\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M7 9l3 3-3 3M12 15h5\"/>" + Close;
        public const string Settings = Open + "<circle fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" cx=\"12\" cy=\"12\" r=\"3\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M12 3v3M12 18v3M3 12h3M18 12h3\"/>" + Close;
        public const string Copy = Open + "<path fill=\"currentColor\" d=\"M8 8h12v12H8z\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M16 8V4H4v12h4\"/>" + Close;
        public const string Bookmark = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M12 4v10\"/><path fill=\"currentColor\" d=\"M12 20l-4-5h8z\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M4 8h3M17 8h3\"/>" + Close;

        // ------------------------------------------------------------------ navigation

        public const string Back = Open + "<path fill=\"currentColor\" d=\"M13 5l-7 7 7 7v-4h6v-6h-6z\"/>" + Close;
        public const string Forward = Open + "<path fill=\"currentColor\" d=\"M11 5l7 7-7 7v-4H5V9h6z\"/>" + Close;
        public const string Up = Open + "<path fill=\"currentColor\" d=\"M11 4h2v10.2l3.5-3.5 1.4 1.4L12 18l-5.9-5.9 1.4-1.4L11 14.2z\" transform=\"rotate(180 12 11)\"/>" + Close;
        public const string ChevronDown = Open12 + "<path fill=\"currentColor\" d=\"M1.5 4L6 8.5 10.5 4l-.9-.9L6 6.7 2.4 3.1z\"/>" + Close;
        public const string ChevronRight = Open12 + "<path fill=\"currentColor\" d=\"M4 1.5L8.5 6 4 10.5l-.9-.9L6.7 6 3.1 2.4z\"/>" + Close;
        public const string ChevronUp = Open12 + "<path fill=\"currentColor\" d=\"M1.5 8L6 3.5 10.5 8l-.9.9L6 5.3 2.4 8.9z\"/>" + Close;

        // ------------------------------------------------------------------ pairing and git

        public const string Link = Open + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M10 14a4 4 0 0 0 6 .5l2.5-2.5a4 4 0 0 0-5.7-5.7L11.5 7.7\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M14 10a4 4 0 0 0-6-.5L5.5 12a4 4 0 0 0 5.7 5.7l1.3-1.3\"/>" + Close;
        public const string Branch = Open + "<circle fill=\"currentColor\" cx=\"6\" cy=\"5\" r=\"2.6\"/><circle fill=\"currentColor\" cx=\"6\" cy=\"19\" r=\"2.6\"/><circle fill=\"currentColor\" cx=\"18\" cy=\"8\" r=\"2.6\"/><path fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" d=\"M6 7.5v9M18 10.5c0 5-12 2-12 6\"/>" + Close;

        // ------------------------------------------------------------------ state and security

        public const string Padlock = Open16 + "<path fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" d=\"M4.75 7V5a3.25 3.25 0 0 1 6.5 0v2\"/><rect x=\"3\" y=\"7\" width=\"10\" height=\"7\" rx=\"1.5\" fill=\"currentColor\"/>" + Close;
        public const string Key = Open16 + "<circle cx=\"5.5\" cy=\"8\" r=\"3\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/><path stroke=\"currentColor\" stroke-width=\"1.6\" d=\"M8.5 8H14M12 8v3\"/>" + Close;
        public const string Warning = Open + "<path fill=\"currentColor\" d=\"M12 2.4l10.4 18H1.6z\"/><path fill=\"#000\" fill-opacity=\"0.55\" d=\"M11 9h2v6h-2zM11 16.2h2v2.2h-2z\"/>" + Close;
        /// <summary>The horizontal bar on the queue's collapse button.</summary>
        public const string Collapse = Open16 + "<path fill=\"currentColor\" d=\"M1 7h14v2H1z\"/>" + Close;

        // ------------------------------------------------------------------ small controls

        public const string Plus = Open + "<path fill=\"currentColor\" d=\"M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z\"/>" + Close;
        public const string Cross = Open + "<path fill=\"currentColor\" d=\"M6.4 5L19 17.6 17.6 19 5 6.4z\"/><path fill=\"currentColor\" d=\"M17.6 5L5 17.6 6.4 19 19 6.4z\"/>" + Close;
        public const string Check = Open + "<path fill=\"currentColor\" d=\"M9 16.2l-3.5-3.5-1.4 1.4L9 19 20 8l-1.4-1.4z\"/>" + Close;
    }
}
