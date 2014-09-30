using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Packaging;

namespace Desktop_Sharing_Shared.Compression
{


    public static class ZipHelper
    {
        public static void ZipFiles(string path, IEnumerable<string> files,
               CompressionOption compressionLevel = CompressionOption.Normal)
        {
            using(FileStream fileStream = new FileStream(path, FileMode.Create))
            {
                ZipHelper.ZipFilesToStream(fileStream, files, compressionLevel);
            }
        }

        public static byte[] ZipFilesToByteArray(IEnumerable<string> files,
               CompressionOption compressionLevel = CompressionOption.Normal)
        {
            byte[] zipBytes = default(byte[]);
            using(MemoryStream memoryStream = new MemoryStream())
            {
                ZipHelper.ZipFilesToStream(memoryStream, files, compressionLevel);
                memoryStream.Flush();
                zipBytes = memoryStream.ToArray();
            }

            return zipBytes;
        }

        public static void Unzip(string zipPath, string baseFolder)
        {
            using(FileStream fileStream = new FileStream(zipPath, FileMode.Open))
            {
                ZipHelper.UnzipFilesFromStream(fileStream, baseFolder);
            }
        }

        public static void UnzipFromByteArray(byte[] zipData, string baseFolder)
        {
            using(MemoryStream memoryStream = new MemoryStream(zipData))
            {
                ZipHelper.UnzipFilesFromStream(memoryStream, baseFolder);
            }
        }

        private static void ZipFilesToStream(Stream destination,
                IEnumerable<string> files, CompressionOption compressionLevel)
        {
            using(Package package = Package.Open(destination, FileMode.Create))
            {
                foreach(string path in files)
                {
                    // fix for white spaces in file names (by ErrCode)
                    Uri fileUri = PackUriHelper.CreatePartUri(new Uri(@"/" +
                                  Path.GetFileName(path), UriKind.Relative));
                    string contentType = @"data/" + ZipHelper.GetFileExtentionName(path);

                    using(Stream zipStream =
                            package.CreatePart(fileUri, contentType, compressionLevel).GetStream())
                    {
                        using(FileStream fileStream = new FileStream(path, FileMode.Open))
                        {
                           
                            fileStream.CopyTo(zipStream);
                        }
                    }
                }
            }
        }

        private static void UnzipFilesFromStream(Stream source, string baseFolder)
        {
            if(!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }

            using(Package package = Package.Open(source, FileMode.Open))
            {
                foreach(PackagePart zipPart in package.GetParts())
                {
                    // fix for white spaces in file names (by ErrCode)
                    string path = Path.Combine(baseFolder,
                         Uri.UnescapeDataString(zipPart.Uri.ToString()).Substring(1));

                    using(Stream zipStream = zipPart.GetStream())
                    {
                        using(FileStream fileStream = new FileStream(path, FileMode.Create))
                        {
                            zipStream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }

        private static string GetFileExtentionName(string path)
        {
            string extention = Path.GetExtension(path);
            if(!string.IsNullOrEmpty(extention) && extention.StartsWith("."))
            {
                extention = extention.Substring(1);
            }

            return extention;
        }
        public static void CopyTo(this Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }
    }
}
