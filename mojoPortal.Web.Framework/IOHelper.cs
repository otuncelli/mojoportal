//  Author:                     
//  Created:                    2009-08-16
//	Last Modified:              2017-03-06
// 
// The use and distribution terms for this software are covered by the 
// Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
// which can be found in the file CPL.TXT at the root of this distribution.
// By using this software in any fashion, you are agreeing to be bound by 
// the terms of this license.
//
// You must not remove this notice, or any other, from this software.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Hosting;

namespace mojoPortal.Web.Framework
{
    public static class IOHelper
    {
        public static string ToCleanFileName(this string s)
        {
            Regex onlyOneSpaceRegEx = new Regex("( )(?<=\\1\\1)");

            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            s = onlyOneSpaceRegEx.Replace(s, string.Empty);

            return s.ToLower().RemoveInvalidPathChars("file").RemoveLineBreaks().Replace("'", string.Empty)
                .Replace("\"", string.Empty).Replace(" - ", "-").Replace(" ", "-").Replace("&", string.Empty)
                .Replace("@", string.Empty).Replace("$", string.Empty).Replace("#", string.Empty);
        }

        public static string ToCleanFileName(this string s, bool forceLowerCase)
        {
            Regex onlyOneSpaceRegEx = new Regex("( )(?<=\\1\\1)");

            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            s = onlyOneSpaceRegEx.Replace(s, string.Empty);

            if (forceLowerCase)
            {
                return s.ToLower().RemoveInvalidPathChars("file").RemoveLineBreaks().Replace("'", string.Empty)
                    .Replace(" - ", "-").Replace(" ", "-").Replace("&", string.Empty).Replace("@", string.Empty)
                    .Replace("$", string.Empty).Replace("#", string.Empty).Replace("(", string.Empty)
                    .Replace(")", string.Empty);
            }

            return s.RemoveInvalidPathChars("file").RemoveLineBreaks().Replace("'", string.Empty)
                .Replace(" ", string.Empty).Replace("&", string.Empty).Replace("@", string.Empty)
                .Replace("$", string.Empty).Replace("#", string.Empty).Replace("(", string.Empty)
                .Replace(")", string.Empty);
        }

        public static string ToCleanFolderName(this string s, bool forceLowerCase)
        {
            Regex onlyOneSpaceRegEx = new Regex("( )(?<=\\1\\1)");

            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            s = onlyOneSpaceRegEx.Replace(s, string.Empty);

            if (forceLowerCase)
            {
                return s.ToLower().RemoveInvalidPathChars("folder").RemoveLineBreaks().Replace("'", string.Empty)
                    .Replace(" - ", "-").Replace(" ", "-").Replace(".", string.Empty).Replace(",", string.Empty)
                    .Replace("&", string.Empty).Replace("@", string.Empty).Replace("$", string.Empty)
                    .Replace("#", string.Empty);
            }

            return s.RemoveInvalidPathChars("folder").RemoveLineBreaks().Replace("'", string.Empty)
                .Replace(" ", string.Empty).Replace(".", string.Empty).Replace(",", string.Empty)
                .Replace("&", string.Empty).Replace("@", string.Empty).Replace("$", string.Empty)
                .Replace("#", string.Empty);
        }

        public static string RemoveInvalidPathChars(this string s, string type)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            char[] invalidPathChars = null;

            switch (type)
            {
                case "file":
                    invalidPathChars = Path.GetInvalidFileNameChars();
                    break;
                case "folder":
                default:
                    invalidPathChars = Path.GetInvalidPathChars();
                    break;
            }

            char[] result = new char[s.Length];
            int resultIndex = 0;

            foreach (char c in s)
            {
                if (!invalidPathChars.Contains(c))
                    result[resultIndex++] = c;
            }

            if (0 == resultIndex)
                s = string.Empty;
            else if (result.Length != resultIndex)
                s = new string(result, 0, resultIndex);

            return s;
        }

        /// <summary>
        /// Determines if file is video or image file based upon its extension.
        /// </summary>
        /// <param name="fileInfo">File to inspect</param>
        /// <returns>Results of the inspection.</returns>
        public static bool IsMediaFile(this FileInfo fileInfo)
        {
            if (string.Equals(fileInfo.Extension, ".bmp", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".gif", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".jpe", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".jpeg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".jpg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".png", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".svg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".tif", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".asf", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".asx", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".avi", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".mov", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".mp4", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".mpeg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".mpg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".wmv", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if file is video or image file based upon its extension.
        /// </summary>
        /// <param name="fileInfo">File to inspect</param>
        /// <returns>Results of the inspection.</returns>
        public static bool IsWebImageFile(this FileInfo fileInfo)
        {
            if (string.Equals(fileInfo.Extension, ".gif", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".jpeg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".jpg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".png", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            if (string.Equals(fileInfo.Extension, ".svg", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static bool IsDecendentDirectory(DirectoryInfo directoryToSearch, DirectoryInfo directoryToFind)
        {
            if (directoryToFind.FullName.StartsWith(directoryToSearch.FullName))
            {
                return true;
            }

            return false;
        }

        public static bool IsDecendentDirectory(DirectoryInfo directoryToSearch, FileInfo fileToFind)
        {
            if (fileToFind.FullName.StartsWith(directoryToSearch.FullName))
            {
                return true;
            }

            return false;
        }

        public static bool IsDecendentDirectory(string virtualPathToSearch, string virtualPathToFind)
        {
            if (string.IsNullOrEmpty(virtualPathToSearch))
            {
                return false;
            }

            if (string.IsNullOrEmpty(virtualPathToFind))
            {
                return false;
            }

            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(HostingEnvironment.MapPath(virtualPathToSearch));
            DirectoryInfo requestedDirectoryInfo = new DirectoryInfo(HostingEnvironment.MapPath(virtualPathToFind));

            return IsDecendentDirectory(rootDirectoryInfo, requestedDirectoryInfo);
        }

        public static bool IsDecendentFile(string virtualPathToSearch, string virtualPathToFind)
        {
            if (string.IsNullOrEmpty(virtualPathToSearch))
            {
                return false;
            }

            if (string.IsNullOrEmpty(virtualPathToFind))
            {
                return false;
            }

            DirectoryInfo rootDirectoryInfo = new DirectoryInfo(HostingEnvironment.MapPath(virtualPathToSearch));
            FileInfo requestedDirectoryInfo = new FileInfo(HostingEnvironment.MapPath(virtualPathToFind));

            return IsDecendentDirectory(rootDirectoryInfo, requestedDirectoryInfo);
        }

        public static string ToMimeType(this string s)
        {
            try
            {
                string ext;
                if (s.Contains("/"))
                {
                    ext = Path.GetExtension(System.Web.Hosting.HostingEnvironment.MapPath(s));
                }
                else
                {
                    ext = Path.GetExtension(s);
                }

                return GetMimeType(ext);
            }
            catch (ArgumentException)
            {
                return s;
            }
        }

        [Obsolete("This method is obsolete. You should use System.Web.MimeMapping.GetMimeMapping instead.")]
        public static string GetMimeType(string fileExtension)
        {
            return System.Web.MimeMapping.GetMimeMapping(fileExtension);
        }

        public static bool IsNonAttacmentFileType(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                return false;
            }

            string fileType = fileExtension.ToLower().Replace(".", string.Empty);

            if (fileType == "pdf")
            {
                return true;
            }

            return false;
        }

        public static void CopyFolderContents(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            string[] files = Directory.GetFiles(sourceFolder);

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string dest = Path.Combine(destFolder, name);
                File.Copy(file, dest);
            }

            string[] folders = Directory.GetDirectories(sourceFolder);

            foreach (string folder in folders)
            {
                string name = Path.GetFileName(folder);
                string dest = Path.Combine(destFolder, name);
                CopyFolderContents(folder, dest);
            }
        }

        public static void Empty(this DirectoryInfo directory)
        {
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            {
                subDirectory.Delete(true);
            }
        }
    }
}