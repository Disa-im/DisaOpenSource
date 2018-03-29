using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;


namespace Disa.Framework
{
    /// <summary>
    /// A Manager style class to manage the folder structure for media attachments and media caches.
    /// 
    /// Provides additional helper functions for the handling of media.
    /// </summary>
    public static class MediaManager
    {
        private const string noMedia = ".nomedia";

        #region Public disa folder names

        /// <summary>
        /// The name of the public folder where audio attachments are saved to.
        /// </summary>
        public const string AudioDirectoryName = "Disa Audio";

        /// <summary>
        /// The name of the public folder where file attachments are saved to.
        /// </summary>
        public const string FilesDirectoryName = "Disa Files";

        /// <summary>
        /// The name of the root public folder where gif attachments are saved to.
        /// 
        /// IMPORTANT: 
        /// Service specific folders are created under this root folder using the <see cref="Service.Information.ServiceName"/>
        /// and the actual gifs are stored there.
        /// </summary>
        public const string GifsDirectoryName = "Disa Gifs";

        /// <summary>
        /// The name of the public folder where picture attachments are saved to.
        /// </summary>
        public const string PicturesDirectoryName = "Disa Images";

        /// <summary>
        /// The name of the root public folder where gif attachments are saved to.
        /// 
        /// IMPORTANT: 
        /// Service specific folders are created under this root folder using the <see cref="Service.Information.ServiceName"/>
        /// and the actual stickers are stored there.
        /// </summary>
        public const string StickersDirectoryName = "Disa Stickers";

        /// <summary>
        /// The name of the public folder where video attachments are saved to.
        /// </summary>
        public const string VideosDirectoryName = "Disa Videos";

        #endregion

        #region Get public disa folder paths

        /// <summary>
        /// The public disa folder path where audio attachments are saved to.
        /// </summary>
        /// <returns>The public disa folder path where audio attachments are saved to.</returns>
        public static string GetDisaAudioPath()
        {
            var path = Platform.GetAudioPath();

            var disa = Path.Combine(path, AudioDirectoryName);

            if (!Directory.Exists(disa))
            {
                Directory.CreateDirectory(disa);
            }

            return disa;
        }

        /// <summary>
        /// The public disa folder path where file attachments are saved to.
        /// </summary>
        /// <returns>The public disa folder path where file attachments are saved to.</returns>
        public static string GetDisaFilesPath()
        {
            var path = Platform.GetFilesPath();

            var disa = Path.Combine(path, FilesDirectoryName);

            if (!Directory.Exists(disa))
            {
                Directory.CreateDirectory(disa);
            }

            return disa;
        }

        /// <summary>
        /// The public disa folder path where gif attachments are saved to.
        /// </summary>
        /// <returns>The public disa folder path where gif attachments are saved to.</returns>
        public static string GetDisaGifsPath()
        {
            var path = Platform.GetGifsPath();

            var disa = Path.Combine(path, GifsDirectoryName);
            if (!Directory.Exists(disa))
            {
                Directory.CreateDirectory(disa);
            }

            return disa;
        }

        /// <summary>
        /// The public disa folder path where picture attachments are saved to.
        /// </summary>
        /// <returns>The public disa folder path where picture attachments are saved to.</returns>
        public static string GetDisaPicturesPath()
        {
            var path = Platform.GetPicturesPath();

            var disa = Path.Combine(path, PicturesDirectoryName);

            if (!Directory.Exists(disa))
            {
                Directory.CreateDirectory(disa);
            }

            return disa;
        }

        /// <summary>
        /// The public disa folder path where sticker attachments are saved to.
        /// </summary>
        /// <returns>The public disa folder path where sticker attachments are saved to.</returns>
        public static string GetDisaStickersPath()
        {
            var path = Platform.GetStickersPath();

            var disa = Path.Combine(path, StickersDirectoryName);
            if (!Directory.Exists(disa))
            {
                Directory.CreateDirectory(disa);
            }

            return disa;
        }

        /// <summary>
        /// The public disa folder path where video attachments are saved to.
        /// </summary>
        /// <returns>The public disa folder path where video attachments are saved to.</returns>
        public static string GetDisaVideosPath()
        {
            var path = Platform.GetVideosPath();

            var disa = Path.Combine(path, VideosDirectoryName);

            if (!Directory.Exists(disa))
            {
                Directory.CreateDirectory(disa);
            }

            return disa;
        }

        #endregion

        #region Get private cache folder paths

        /// <summary>
        /// The private cache folder path where cached gifs are maintained.
        /// 
        /// IMPORTANT: 
        /// The folder path will include a root folder for gifs with a subfolder based on the 
        /// <see cref="Service.Information.ServiceName"/>.
        /// </summary>
        /// <param name="service">The <see cref="Service"/> to base the subfolder name on.</param>
        /// <returns>The private cache folder path where cached gifs are maintained.</returns>
        public static string GetCachedGifsPath(Service service)
        {
            var path = Platform.GetCachedGifsPath();

            var servicePath = Path.Combine(path, service.Information.ServiceName);
            if (!Directory.Exists(servicePath))
            {
                Directory.CreateDirectory(servicePath);
            }

            return servicePath;
        }

        /// <summary>
        /// The private cache folder path where cached stickers are maintained.
        /// 
        /// IMPORTANT: 
        /// The folder path will include a root folder for stickers with a subfolder based on the 
        /// <see cref="Service.Information.ServiceName"/>.
        /// </summary>
        /// <param name="service">The <see cref="Service"/> to base the subfolder name on.</param>
        /// <returns>The private cache folder path where cached stickers are maintained.</returns>
        public static string GetCachedStickersPath(Service service)
        {
            var path = Platform.GetCachedStickersPath();

            var servicePath = Path.Combine(path, service.Information.ServiceName);
            if (!Directory.Exists(servicePath))
            {
                Directory.CreateDirectory(servicePath);
            }

            return servicePath;
        }

        #endregion

        #region Generate public disa file locations

        /// <summary>
        /// Given an <see cref="AudioParameters.RecordType"/>, generate a public disa file location for an audio file
        /// of the format:
        /// 
        /// public disa audio path/timestamp+spinner+extension
        /// 
        /// The extension will be derived from the enum passed in.
        /// </summary>
        /// <param name="recordType">An enum specifying the audio record type.</param>
        /// <returns>A public disa file location for an audio file.</returns>
        public static string GenerateDisaAudioLocation(AudioParameters.RecordType recordType)
        {
            return GenerateDisaMediaLocationUsingExtension(GetDisaAudioPath, recordType == AudioParameters.RecordType.M4A ? ".m4a" : ".3gp");
        }

        /// <summary>
        /// Given a filename, generate a public disa file location for an audio file of the format:
        /// 1. file exists
        ///    path/filename(counter).optional extension if the filename had it
        /// 2. file does not exist
        ///    path/filename
        /// </summary>
        /// <param name="fileName">The filename to create the location from.</param>
        /// <returns>A public disa file location for an audio file.</returns>
        public static string GenerateDisaAudioLocation(string fileName)
        {
            return GenerateDisaFileLocation(GetDisaAudioPath, fileName);
        }

        /// <summary>
        /// Given an old location filename, produce a public disa file location for an audio file
        /// of the format:
        /// path/timestamp+spinner+extension
        /// </summary>
        /// <param name="oldLocation">The old location filename to create the location from.</param>
        /// <returns>A public disa file location for an audio file.</returns>
        public static string GenerateDisaAudioLocationNoFileName(string oldLocation)
        {
            var extension = GetSafeExtension(oldLocation, ".m4a"); //Path.GetExtension(oldLocation);
            return String.IsNullOrEmpty(extension)
                       ? null
                       : GenerateDisaMediaLocationUsingExtension(GetDisaAudioPath, extension);
        }

        /// <summary>
        /// Given a filename, generate a public disa file location for a file of the format:
        /// 1. file exists
        ///    path/filename(counter).optional extension if the filename had it
        /// 2. file does not exist
        ///    path/filename
        /// </summary>
        /// <param name="fileName">The filename to create the location from.</param>
        /// <returns>A public disa file location for a file.</returns>
        public static string GenerateDisaFileLocation(string fileName)
        {
            return GenerateDisaFileLocation(GetDisaFilesPath, fileName);
        }

        /// <summary>
        /// Given a base path and a filename, generate a public disa file location for a file of the format:
        /// 1. file exists
        ///    path/filename(counter).optional extension if the filename had it
        /// 2. file does not exist
        ///    path/filename
        /// </summary>
        /// <param name="fileName">The filename to create the location from.</param>
        /// <returns>A public disa file location for a file.</returns>
        public static string GenerateDisaFileLocation(Func<string> basePath, string fileName)
        {
            var filePath = Path.Combine(basePath(), fileName);

            // don't overwrite...
            if (File.Exists(filePath))
            {
                var pointIndex = fileName.LastIndexOf('.');
                if (pointIndex > -1)
                {
                    var counter = 0;
                    var name = fileName.Substring(0, pointIndex);
                    var extension = fileName.Substring(pointIndex);
                    while (true)
                    {
                        var newFileName = name + " (" + ++counter + ")" + extension;
                        var newFilePath = Path.Combine(basePath(), newFileName);
                        if (!File.Exists(newFilePath))
                        {
                            return newFilePath;
                        }
                    }
                }
                else
                {
                    var counter = 0;
                    while (true)
                    {
                        var newFileName = fileName + " (" + ++counter + ")";
                        var newFilePath = Path.Combine(basePath(), newFileName);
                        if (!File.Exists(newFilePath))
                        {
                            return newFilePath;
                        }
                    }
                }
            }

            return filePath;
        }

        [Obsolete("GenerateFileLocation is deprecated, please use GenerateDisaFileLocation instead.", true)]
        public static string GenerateFileLocation(Func<string> basePath, string fileName)
        {
            return GenerateDisaFileLocation(basePath, fileName);
        }

        /// <summary>
        /// Given an extension, generate a unique filename of the format:
        /// timestamp+spinner+extension
        /// </summary>
        /// <param name="extension">The extension to use.</param>
        /// <returns>A unique string representing a filename with the extension.</returns>
        public static string GenerateDisaFileName(string extension)
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + Spinner++ + extension;
        }

        /// <summary>
        /// Given a filename, generate a public disa file location for a gif file of the format:
        /// 1. file exists
        ///    path/filename(counter).optional extension if the filename had it
        /// 2. file does not exist
        ///    path/filename
        /// </summary>
        /// <param name="fileName">The filename to  create the location from.</param>
        /// <returns>A public disa file location for a gif file.</returns>
        public static string GenerateDisaGifLocation(string fileName)
        {
            return GenerateDisaFileLocation(GetDisaGifsPath, fileName);
        }

        /// <summary>
        /// Given a base path and an extension, generate unique public disa file location of the format:
        /// base path/unique filename.extension
        /// </summary>
        /// <param name="basePath">The public disa base path.</param>
        /// <param name="extension">The extension to use.</param>
        /// <returns>A a unique disa file location.</returns>
        public static string GenerateDisaMediaLocationUsingExtension(Func<string> basePath, string extension)
        {
            var @base = basePath();
            var fileName = GenerateDisaFileName(extension);
            return Path.Combine(@base, fileName);
        }

        /// <summary>
        /// Generate a unique public disa file location of the format:
        /// public disa picture path/timestamp+spinner+.jpg
        /// </summary>
        /// <returns>A public disa file location for a picture.</returns>
        public static string GenerateDisaPictureLocation()
        {
            return GenerateDisaMediaLocationUsingExtension(GetDisaPicturesPath, ".jpg");
        }

        /// <summary>
        /// Given a filename, generate a public disa file location for a picture file of the format:
        /// 1. file exists
        ///    path/filename(counter).optional extension if the filename had it
        /// 2. file does not exist
        ///    path/filename
        /// </summary>
        /// <param name="fileName">The filename to  create the location from.</param>
        /// <returns>A public disa file location for a picture file.</returns>
        public static string GenerateDisaPictureLocation(string fileName)
        {
            return GenerateDisaFileLocation(GetDisaPicturesPath, fileName);
        }

        /// <summary>
        /// Given an old location filename, produce a public disa file location for a picture file
        /// of the format:
        /// path/timestamp+spinner+extension
        /// </summary>
        /// <param name="oldLocation">The old location filename to create the location from.</param>
        /// <returns>A public disa file location for a picture file.</returns>
        public static string GenerateDisaPictureLocationNoFileName(string oldLocation)
        {
            var extension = GetSafeExtension(oldLocation, ".jpg"); //Path.GetExtension(oldLocation);
            return String.IsNullOrEmpty(extension)
                       ? null
                       : GenerateDisaMediaLocationUsingExtension(GetDisaPicturesPath, extension);
        }

        /// <summary>
        /// Given a filename, generate a public disa file location for a sticker file of the format:
        /// 1. file exists
        ///    path/filename(counter).optional extension if the filename had it
        /// 2. file does not exist
        ///    path/filename
        /// </summary>
        /// <param name="fileName">The filename to  create the location from.</param>
        /// <returns>A public disa file location for a sticker file.</returns>
        public static string GenerateDisaStickerLocation(string fileName)
        {
            return GenerateDisaFileLocation(GetDisaStickersPath, fileName);
        }

        /// <summary>
        /// Given a <see cref="VideoParameters.RecordType"/>, generate a public disa file location for a video file
        /// of the format:
        /// 
        /// public disa video path/timestamp+spinner+extension
        /// 
        /// The extension will be derived from the enum passed in.
        /// </summary>
        /// <param name="recordType">An enum specifying the video record type.</param>
        /// <returns>A public disa file location for a video file.</returns>
        public static string GenerateDisaVideoLocation(VideoParameters.RecordType recordType)
        {
            return GenerateDisaMediaLocationUsingExtension(GetDisaVideosPath,
                                                           recordType == VideoParameters.RecordType.Mp4 ? ".mp4" : ".3gp");
        }

        /// <summary>
        /// Given a filename, generate a public disa file location for a video file of the format:
        /// 1. file exists
        ///    path/filename(counter).optional extension if the filename had it
        /// 2. file does not exist
        ///    path/filename
        /// </summary>
        /// <param name="fileName">The filename to create the location from.</param>
        /// <returns>A public disa file location for a video file.</returns>
        public static string GenerateDisaVideoLocation(string fileName)
        {
            return GenerateDisaFileLocation(GetDisaVideosPath, fileName);
        }

        /// <summary>
        /// Given an old location filename, produce a public disa file location for a video file
        /// of the format:
        /// path/timestamp+spinner+extension
        /// </summary>
        /// <param name="oldLocation">The old location filename to create the location from.</param>
        /// <returns>A public disa file location for a video file.</returns>
        public static string GenerateDisaVideoLocationNoFileName(string oldLocation)
        {
            var extension = GetSafeExtension(oldLocation, ".mp4"); //Path.GetExtension(oldLocation);
            return String.IsNullOrEmpty(extension)
                       ? null
                       : GenerateDisaMediaLocationUsingExtension(GetDisaVideosPath, extension);
        }

        #endregion

        #region Determine if mime type

        /// <summary>
        /// Given a mime type, determine if it represents an audio mime type.
        /// </summary>
        /// <param name="mime">The mime type to analyze.</param>
        /// <returns>True if the passed in mime type represents an audio mime type. False if not.</returns>
        public static bool IsAudioType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("audio", StringComparison.Ordinal) == 0;
        }

        /// <summary>
        /// Given a mime type, determine if it represents a gif mime type.
        /// </summary>
        /// <param name="mime">The mime type to analyze.</param>
        /// <returns>True if the passed in mime type represents a gif mime type. False if not.</returns>
        public static bool IsGifType(string mime)
        {
            if (mime == null)
                return false;

            return mime.ToLower().Contains("image/gif");
        }

        /// <summary>
        /// Given a mime type, determine if it represents an image mime type.
        /// </summary>
        /// <param name="mime">The mime type to analyze.</param>
        /// <returns>True if the passed in mime type represents an image mime type. False if not.</returns>
        public static bool IsImageType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("image", StringComparison.Ordinal) == 0;
        }

        [Obsolete("IsImageGif is deprecated, please use IsGifType instead.", true)]
        public static bool IsImageGif(string mimeType)
        {
            if (mimeType == null)
                return false;

            return mimeType.ToLower().Contains("image/gif");
        }

        /// <summary>
        /// Given a mime type, determine if it represents a sticker mime type.
        /// </summary>
        /// <param name="mime">The mime type to analyze.</param>
        /// <returns>True if the passed in mime type represents a sticker mime type. False if not.</returns>
        public static bool IsStickerType(string mime)
        {
            if (mime == null)
                return false;

            return mime.ToLower().Contains("image/webp");
        }

        /// <summary>
        /// Given a mime type, determine if it represents a text mime type.
        /// </summary>
        /// <param name="mime">The mime type to analyze.</param>
        /// <returns>True if the passed in mime type represents a text mime type. False if not.</returns>
        public static bool IsTextType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("text", StringComparison.Ordinal) == 0;
        }

        /// <summary>
        /// Given a mime type, determine if it represents a VCard mime type.
        /// </summary>
        /// <param name="mime">The mime type to analyze.</param>
        /// <returns>True if the passed in mime type represents a vcard mime type. False if not.</returns>
        public static bool IsVCardType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("text/x-vcard", StringComparison.Ordinal) == 0 ||
                mime.IndexOf("text/vcard", StringComparison.Ordinal) == 0;
        }

        /// <summary>
        /// Given a mime type, determine if it represents a video mime type.
        /// </summary>
        /// <param name="mime">The mime type to analyze.</param>
        /// <returns>True if the passed in mime type represents a video mime type. False if not.</returns>
        public static bool IsVideoType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("video", StringComparison.Ordinal) == 0;
        }

        #endregion

        #region Additional miscellaneous API

        public static void RemoveNoMediaIfNeeded()
        {
#if __ANDROID__

            var pictures = GetDisaPicturesPath();
            var videos = GetDisaVideosPath();
            var audios = GetDisaAudioPath();
            var files = GetDisaFilesPath();
            var gifs = GetDisaGifsPath();
            var stickers = GetDisaStickersPath();

            var picturesNoMedia = Path.Combine(pictures, noMedia);
            var videosNoMedia = Path.Combine(videos, noMedia);
            var audiosNoMedia = Path.Combine(audios, noMedia);
            var filesNoMedia = Path.Combine(files, noMedia);
            var gifsNoMedia = Path.Combine(gifs, noMedia);
            var stickersNoMedia = Path.Combine(stickers, noMedia);

            if (File.Exists(picturesNoMedia))
            {
                File.Delete(picturesNoMedia);
            }
            if (File.Exists(videosNoMedia))
            {
                File.Delete(videosNoMedia);
            }
            if (File.Exists(audiosNoMedia))
            {
                File.Delete(audiosNoMedia);
            }
            if (File.Exists(filesNoMedia))
            {
                File.Delete(filesNoMedia);
            }
            if (File.Exists(gifsNoMedia))
            {
                File.Delete(gifsNoMedia);
            }
            if (File.Exists(stickersNoMedia))
            {
                File.Delete(stickersNoMedia);
            }

#else

            Utils.DebugPrint("Not running on Android. No need to remove no medias.");

#endif

        }

        public static void InsertNoMediasIfNeeded()
        {
#if __ANDROID__
            var pictures = GetDisaPicturesPath();
            var videos = GetDisaVideosPath();
            var audios = GetDisaAudioPath();
            var files = GetDisaFilesPath();
            var gifs = GetDisaGifsPath();
            var stickers = GetDisaStickersPath();

            var picturesNoMedia = Path.Combine(pictures, noMedia);
            var videosNoMedia = Path.Combine(videos, noMedia);
            var audiosNoMedia = Path.Combine(audios, noMedia);
            var filesNoMedia = Path.Combine(files, noMedia);
            var gifsNoMedia = Path.Combine(gifs, noMedia);
            var stickersNoMedia = Path.Combine(stickers, noMedia);

            if (!File.Exists(picturesNoMedia))
            {
                File.Create(picturesNoMedia);
            }
            if (!File.Exists(videosNoMedia))
            {
                File.Create(videosNoMedia);
            }
            if (!File.Exists(audiosNoMedia))
            {
                File.Create(audiosNoMedia);
            }
            if (!File.Exists(filesNoMedia))
            {
                File.Create(filesNoMedia);
            }
            if (!File.Exists(gifsNoMedia))
            {
                File.Create(gifsNoMedia);
            }
            if (!File.Exists(stickersNoMedia))
            {
                File.Create(stickersNoMedia);
            }

#else
            Utils.DebugPrint("Not running on Android. No need to insert no medias.");
#endif
        }

        public static string PatchPath(VisualBubble bubble)
        {
            string path = null;
            string newBase = null;

            var imageBubble = bubble as ImageBubble;
            var videoBubble = bubble as VideoBubble;
            var audioBubble = bubble as AudioBubble;
            var fileBubble = bubble as FileBubble;
            var stickerBubble = bubble as StickerBubble;
            if (imageBubble != null)
            {
                path = imageBubble.ImagePathNative;
                newBase = Platform.GetPicturesPath();
            }
            else if (videoBubble != null)
            {
                path = videoBubble.VideoPathNative;
                newBase = Platform.GetVideosPath();
            }
            else if (audioBubble != null)
            {
                path = audioBubble.AudioPathNative;
                newBase = Platform.GetAudioPath();
            }
            else if (fileBubble != null)
            {
                path = fileBubble.PathNative;
                newBase = Platform.GetFilesPath();
            }
            else if (stickerBubble != null)
            {
                path = stickerBubble.StickerPathNative;
                newBase = Platform.GetStickersPath();
            }

            if (path == null || newBase == null)
                throw new Exception("Uknown bubble");

            var indexes = FindIndexesFromRear(path, Path.DirectorySeparatorChar);
            var seperators = indexes.Take(2).ToList();
            if (seperators.Count < 2)
                return path;

            var end = path.Substring(seperators[1] + 1);
            return Path.Combine(newBase, end);
        }

        public static async Task<string> IsFileInDisaDirectory(Func<string> disaDirectory, string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                var newPath = Path.Combine(disaDirectory(), Path.GetFileName(path));
                var newPathFileInfo = new FileInfo(newPath);
                if (!newPathFileInfo.Exists)
                {
                    return null;
                }
                if (newPath == path)
                {
                    return newPath;
                }
                if (newPathFileInfo.Length != new FileInfo(path).Length)
                {
                    return null;
                }
                return await Task<string>.Factory.StartNew(() =>
                {
                    var hash1 = ComputeHash(path);
                    var hash2 = ComputeHash(newPath);
                    if (hash1.SequenceEqual(hash2))
                    {
                        return newPath;
                    }
                    return null;
                });
            }
            catch
            {
                return null;
            }
        }

        public static string GetExtensionFromPath(string path)
        {
            return Path.GetExtension(path);
        }

        public static string GetExtensionWithoutPeriod(string path)
        {
            var extension = Path.GetExtension(path);
            if (String.IsNullOrEmpty(extension))
            {
                return null;
            }

            var index = extension.IndexOf('.');
            return index == -1 ? null : extension.Remove(0, index + 1).ToLower().Trim();
        }

        #endregion

        #region Helper methods

        private static IEnumerable<int> FindIndexesFromRear(string str, char chr)
        {
            for (var i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] == chr)
                    yield return i;
            }
        }

        private static string RemoveArgumentsFromExtension(string extension)
        {
            var index = extension.IndexOf('?');
            if (index > -1)
            {
                return extension.Substring(0, index);
            }
            return extension.ToString();
        }

        private static string GetSafeExtension(string location, string @default)
        {
            var extension = RemoveArgumentsFromExtension(Path.GetExtension(location));

            if (extension == null)
            {
                return @default;
            }

            return extension;
        }

        private static readonly object SpinnerLock = new object();
        private static long _spinner;
        private static long Spinner
        {
            get
            {
                lock (SpinnerLock) return _spinner;
            }
            set
            {
                lock (SpinnerLock) _spinner = value;
            }
        }

        private static byte[] ComputeHash(string fileLocation)
        {
            using (var fs = File.OpenRead(fileLocation))
            {
                using (var sha1 = new SHA1Managed())
                {
                    var hash = sha1.ComputeHash(fs);
                    return hash;
                }
            }
        }

        private static Tuple<bool, string> CopyFileToDirectoryIfNeeded(Func<string> directory, string oldPath)
        {
            try
            {
                bool newFile;
                var newPath = Path.Combine(directory(), Path.GetFileName(oldPath));
                if (oldPath != newPath)
                {
                    if (File.Exists(newPath))
                    {
                        newFile = false;
                    }
                    else
                    {
                        newFile = true;
                    }
                    File.Copy(oldPath, newPath, true);
                }
                else
                {
                    newFile = false;
                }
                return Tuple.Create(newFile, newPath);
            }
            catch (Exception ex)
            {
                Utils.DebugPrint("File " + oldPath + " could not be copied to the Disa gallery location: " + ex.Message);
                return Tuple.Create(false, (string)null);
            }
        }

        #endregion

    }
}
