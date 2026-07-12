using System;
using System.IO;

namespace WinUIEx
{
    internal static class IconPathHelper
    {
        internal static string ResolvePath(string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            if (Path.IsPathRooted(path))
                return path;

            string rootFolder = Windows.ApplicationModel.Package.Current?.InstalledLocation.Path ?? AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(rootFolder, path));
        }
    }
}
