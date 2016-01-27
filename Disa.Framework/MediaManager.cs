using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Disa.Framework.Bubbles;

namespace Disa.Framework
{
    public static class MediaManager
    {
        public const string PicturesDirectoryName = "Disa Images";
        public const string VideosDirectoryName = "Disa Videos";
        public const string AudioDirectoryName = "Disa Audio";
        public const string FilesDirectoryName = "Disa Files";

        private const string noMedia = ".nomedia";

        public static void RemoveNoMediaIfNeeded()
        {
            #if __ANDROID__

            var pictures = GetDisaPicturesPath();
            var videos = GetDisaVideosPath();
            var audios = GetDisaAudioPath();
            var files = GetDisaFilesPath();

            var picturesNoMedia = Path.Combine(pictures, noMedia);
            var videosNoMedia = Path.Combine(videos, noMedia);
            var audiosNoMedia = Path.Combine(audios, noMedia);
            var filesNoMedia = Path.Combine(files, noMedia);

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

            var picturesNoMedia = Path.Combine(pictures, noMedia);
            var videosNoMedia = Path.Combine(videos, noMedia);
            var audiosNoMedia = Path.Combine(audios, noMedia);
            var filesNoMedia = Path.Combine(files, noMedia);

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

#else
            Utils.DebugPrint("Not running on Android. No need to insert no medias.");
#endif
        }

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

        private static IEnumerable<int> FindIndexesFromRear(string str, char chr)
        {
            for (var i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] == chr)
                    yield return i;
            }
        }

        public static string PatchPath(VisualBubble bubble)
        {
            string path = null;
            string newBase = null;

            var imageBubble = bubble as ImageBubble;
            var videoBubble = bubble as VideoBubble;
            var audioBubble = bubble as AudioBubble;
            var fileBubble = bubble as FileBubble;
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

            if (path == null || newBase == null)
                throw new Exception("Uknown bubble");

            var indexes = FindIndexesFromRear(path, Path.DirectorySeparatorChar);
            var seperators = indexes.Take(2).ToList();
            if (seperators.Count < 2)
                return path;

            var end = path.Substring(seperators[1] + 1);
            return Path.Combine(newBase, end);
        }

        public static Tuple<bool, string> CopyPhotoToDisaPictureLocation(string file)
        {
			return CopyFileToDirectoryIfNeeded(GetDisaPicturesPath, file);
        }

        public static Tuple<bool, string> CopyVideoToDisaVideoLocation(string file)
        {
			return CopyFileToDirectoryIfNeeded(GetDisaVideosPath, file);
        }

        public static Tuple<bool, string> CopyAudioToDisaAudioLocation(string file)
        {
			return CopyFileToDirectoryIfNeeded(GetDisaAudioPath, file);
        }

        public static Tuple<bool, string> CopyFileToDisaFileLocation(string file)
        {
            return CopyFileToDirectoryIfNeeded(GetDisaFilesPath, file);
        }

        public static string GenerateDisaAudioLocation(AudioParameters.RecordType recordType)
        {
            return GenerateDisaMediaLocationUsingExtension(GetDisaAudioPath, recordType == AudioParameters.RecordType.M4A ? ".m4a" : ".3gp");
        }

        public static string GenerateDisaVideoLocation(VideoParameters.RecordType recordType)
        {
            return GenerateDisaMediaLocationUsingExtension(GetDisaVideosPath,
                                                           recordType == VideoParameters.RecordType.Mp4 ? ".mp4" : ".3gp");
        }

        public static string GenerateDisaVideoLocation(string fileName)
        {
            return GenerateFileLocation(GetDisaVideosPath, fileName);
        }

        public static string GenerateDisaVideoLocationNoFileName(string oldLocation)
        {
			var extension = GetSafeExtension(oldLocation, ".mp4"); //Path.GetExtension(oldLocation);
            return String.IsNullOrEmpty(extension)
                       ? null
                       : GenerateDisaMediaLocationUsingExtension(GetDisaVideosPath, extension);
        }

        public static string GenerateDisaPictureLocation()
        {
            return GenerateDisaMediaLocationUsingExtension(GetDisaPicturesPath, ".jpg");
        }

        public static string GenerateDisaPictureLocation(string fileName)
        {
            return GenerateFileLocation(GetDisaPicturesPath, fileName);
        }

        public static string GenerateDisaPictureLocationNoFileName(string oldLocation)
        {
			var extension = GetSafeExtension(oldLocation, ".jpg"); //Path.GetExtension(oldLocation);
            return String.IsNullOrEmpty(extension)
                       ? null
                       : GenerateDisaMediaLocationUsingExtension(GetDisaPicturesPath, extension);
        }

        public static string GenerateDisaAudioLocation(string fileName)
        {
            return GenerateFileLocation(GetDisaAudioPath, fileName);
        }

        public static string GenerateDisaAudioLocationNoFileName(string oldLocation)
        {
			var extension = GetSafeExtension(oldLocation, ".m4a"); //Path.GetExtension(oldLocation);
            return String.IsNullOrEmpty(extension)
                       ? null
                       : GenerateDisaMediaLocationUsingExtension(GetDisaAudioPath, extension);
        }

        public static string GenerateFileLocation(string fileName)
        {
            return GenerateFileLocation(GetDisaFilesPath, fileName);
        }

        public static string GenerateFileLocation(Func<string> basePath, string fileName)
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

        public static string GenerateDisaMediaLocationUsingExtension(Func<string> basePath, string extension)
        {
            var picturesLocation = basePath();
            var fileName = DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + Spinner++ + extension;

            return Path.Combine(picturesLocation, fileName);
        }

        public static bool IsFileInDisaDirectory(Func<string> disaDirectory, string pathToTest)
        {
            var newPath = Path.Combine(disaDirectory(), Path.GetFileName(pathToTest));
            return pathToTest == newPath;
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

        public static string GetExtensionFromPath(string path)
        {
            return Path.GetExtension(path);
        }

        public static bool IsImageGif(string mimeType)
        {
            if (mimeType == null)
                return false;

            return mimeType.ToLower().Contains("image/gif");
        }

        public static bool IsImageType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("image", StringComparison.Ordinal) == 0;
        }

        public static bool IsVideoType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("video", StringComparison.Ordinal) == 0;
        }

        public static bool IsAudioType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("audio", StringComparison.Ordinal) == 0;
        }

        public static bool IsTextType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("text", StringComparison.Ordinal) == 0;
        }

        public static bool IsVCardType(string mime)
        {
            mime = mime.ToLower().Trim();
            return mime.IndexOf("text/x-vcard", StringComparison.Ordinal) == 0 || 
                mime.IndexOf("text/vcard", StringComparison.Ordinal) == 0;
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
    }
}