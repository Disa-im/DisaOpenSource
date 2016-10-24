using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Disa.Framework.Bubbles;
using ProtoBuf;
using SharpMTProto.Messaging;
using SharpTelegram.Schema;
using SQLite;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IMediaDownloaderCustom
    {
        private class DownloadEntry
        {
            [PrimaryKey][AutoIncrement]
            public uint Id { get; set;}

            public string BubbleId { get; set; }

            public string TemporaryFileName { get; set; }
        }

        private object _downloadDatabaseLock = new object();
        private string _downloadDatabaseLocation = Path.Combine(Platform.GetDatabasePath(),"TelegramDownloadsDatabase.db");

        public Task<string> Download(VisualBubble bubble, Action<int> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                DebugPrint(">>>> started download task");
                string savePath = null;
                string savePathTemp = null;
                uint chunkSize = 32768;
                bool downloadedPreviously = false;

                if (bubble.AdditionalData != null)
                {
                    using (var memoryStream = new MemoryStream(bubble.AdditionalData))
                    {
                        var databasePath = Platform.GetDatabasePath();
                        if (!Directory.Exists(databasePath))
                        {
                            Utils.DebugPrint("Creating database directory.");
                            Directory.CreateDirectory(databasePath);
                        }

                        lock(_downloadDatabaseLock)
                        {
                            using (var database = new SqlDatabase<DownloadEntry>(_downloadDatabaseLocation))
                            {
                                var downloadEntry = database.Store.Where(x => x.BubbleId == bubble.ID).FirstOrDefault();
                                if (downloadEntry != null)
                                {
                                    downloadedPreviously = true;
                                    savePath = downloadEntry.TemporaryFileName;
                                    savePathTemp = savePath + ".tmp";
                                }
                            }
                        }

                        var fileInformation = Serializer.Deserialize<FileInformation>(memoryStream);
                        var type = fileInformation.FileType;
                        var document = fileInformation.Document as Document;
                        var fileLocation = fileInformation.FileLocation as FileLocation;
                        if (document == null && type == "document")
                        {
                            return null;
                        }
                        if (fileLocation == null && type == "image")
                        {
                            return null;
                        }
                        if (!downloadedPreviously)
                        {
                            if (bubble is ImageBubble)
                            {
                                savePath = MediaManager.GenerateDisaMediaLocationUsingExtension(
                                    MediaManager.GetDisaPicturesPath, Platform.GetExtensionFromMimeType("image/jpeg"));
                            }
                            else if (bubble is FileBubble)
                            {
                                if (GetDocumentFileName(document) != null)
                                {
                                    savePath = MediaManager.GenerateDisaMediaLocationUsingExtension(
                                        MediaManager.GetDisaFilesPath, GetDocumentFileName(document));
                                }
                                else
                                {
                                    savePath = MediaManager.GenerateDisaMediaLocationUsingExtension(
                                        MediaManager.GetDisaFilesPath,
                                        Platform.GetExtensionFromMimeType(document.MimeType));
                                }
                            }
                            else if (bubble is AudioBubble)
                            {
                                if (document == null) return null;

                                savePath = MediaManager.GenerateDisaMediaLocationUsingExtension(
                                           MediaManager.GetDisaAudioPath,
                                           Platform.GetExtensionFromMimeType(document.MimeType));
                            }
                            if (savePath == null) return savePath;
                            lock (_downloadDatabaseLock)
                            {
                                using (var database = new SqlDatabase<DownloadEntry>(_downloadDatabaseLocation))
                                {
                                    database.Add(new DownloadEntry
                                    {
                                        BubbleId = bubble.ID,
                                        TemporaryFileName = savePath
                                    });
                                }
                            }
                            savePathTemp = savePath + ".tmp";
                            Platform.MarkTemporaryFileForDeletion(savePath);
                        }
                        float currentProgress = 0;
                        try
                        {
                            switch (type)
                            {
                                case "image":
                                    using (var fs = File.OpenWrite(savePathTemp))
                                    {
                                        var fileSize = fileInformation.Size;
                                        uint currentOffset = 0;
                                        while (currentOffset <= fileSize)
                                        {
                                            var bytes = FetchFileBytes(
                                                fileLocation,
                                                currentOffset, chunkSize);
                                            if (bytes.Length == 0)
                                            {
                                                break;
                                            }
                                            fs.Write(bytes, 0, bytes.Length);
                                            currentProgress = currentOffset / (float)fileSize;

                                            progress((int)(currentProgress * 100));
                                            currentOffset += chunkSize;
                                        }
                                    }
                                    break;
                                case "document":
                                    using (var fs = File.Open(savePathTemp,FileMode.Append))
                                    {
                                        var fileSize = document.Size;
                                        uint currentOffset = 0;
                                        if (downloadedPreviously)
                                        {
                                            currentOffset = (uint)new FileInfo(savePathTemp).Length;   
                                        }
                                        while (currentOffset < fileSize)
                                        {
                                            using (var downloadManager = new DownloadManager(document, this))
                                            {
                                                currentOffset = downloadManager.DownloadDocument(fs, currentOffset, progress);
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    return null;
                            }
                            if (File.Exists(savePath))
                            {
                                File.Delete(savePath);
                            }
                            File.Move(savePathTemp, savePath);

                            lock (_downloadDatabaseLock)
                            {
                                using (var database = new SqlDatabase<DownloadEntry>(_downloadDatabaseLocation))
                                {
                                    foreach (var item in database.Store.Where(x => x.BubbleId == bubble.ID))
                                    {
                                        database.Remove(item);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (File.Exists(savePath))
                            {
                                File.Delete(savePath);
                            }
                            return null;
                        }
                        finally
                        {
                            Platform.UnmarkTemporaryFileForDeletion(savePath, false);
                        }

                    }
                }
                return savePath;
            });
        }
    }
}