using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Disa.Framework.Bubbles;
using ProtoBuf;
using SharpMTProto.Messaging;
using SharpTelegram.Schema;

namespace Disa.Framework.Telegram
{
    public partial class Telegram : IMediaDownloaderCustom
    {
        public Task<string> Download(VisualBubble bubble, Action<int> progress)
        {
            return Task.Factory.StartNew(() =>
            {
                DebugPrint(">>>> started download task");
                string savePath = null;
                uint chunkSize = 32768;
                if (bubble.AdditionalData != null)
                {
                    using (var memoryStream = new MemoryStream(bubble.AdditionalData))
                    {
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

                        if (bubble is ImageBubble)
                        {   
                            savePath = MediaManager.GenerateDisaMediaLocationUsingExtension(
                                MediaManager.GetDisaPicturesPath, Platform.GetExtensionFromMimeType("image/jpeg"));
                        }
                        else if(bubble is FileBubble)
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
                        else if(bubble is AudioBubble)
                        {
                            if (document == null) return null;

                            savePath = MediaManager.GenerateDisaMediaLocationUsingExtension(
                                       MediaManager.GetDisaAudioPath,
                                       Platform.GetExtensionFromMimeType(document.MimeType));

                        }
                        if (savePath == null) return savePath;
                        var savePathTemp = savePath + ".tmp";
                        Platform.MarkTemporaryFileForDeletion(savePath);
                        Platform.MarkTemporaryFileForDeletion(savePathTemp);

                        float currentProgress = 0;

                        Timer timer = null;

                        if (type == "document")
                        {
                            timer = new Timer(55000);

                            timer.Elapsed += (sender, args) =>
                            {
                                DebugPrint(">>>>>>>>>>>>>>>>>>>>>> Timer Elapsed!! ");
                                DebugPrint(">>>>>>> current progress " + currentProgress);

                                if (document.DcId != Settings.NearestDcId)
                                {
                                    cachedClient = GetClient((int)document.DcId);
                                }
                            };
                        }

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
                                            currentProgress = currentOffset/(float) fileSize;

                                            progress((int) (currentProgress*100));
                                            currentOffset += chunkSize;
                                        }
                                    }
                                    break;
                                case "document":
                                    using (var fs = File.OpenWrite(savePathTemp))
                                    {
                                        var fileSize = document.Size;
                                        uint currentOffset = 0;
                                        if (timer != null)
                                        {
                                            timer.Start();
                                        }
                                        chunkSize = (uint)CalculateChunkSize(fileSize);
                                        while (currentOffset <= fileSize)
                                        {
                                            var bytes = FetchDocumentBytes(document, currentOffset,
                                                chunkSize);
                                            if (bytes.Length == 0)
                                            {
                                                break;
                                            }
                                            fs.Write(bytes, 0, bytes.Length);
                                            currentProgress = currentOffset/(float) fileSize;

                                            progress((int) (currentProgress*100));
                                            currentOffset += chunkSize;
                                        }
                                        if (timer != null)
                                        {
                                            timer.Stop();
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
                        }
                        catch (Exception e)
                        {
                            if (File.Exists(savePath))
                            {
                                File.Delete(savePath);
                            }
                            if (File.Exists(savePathTemp))
                            {
                                File.Delete(savePathTemp);
                            }
                            return null;
                        }
                        finally
                        {
                            Platform.UnmarkTemporaryFileForDeletion(savePathTemp, true);
                            Platform.UnmarkTemporaryFileForDeletion(savePath, false);
                            if (timer != null)
                            {
                                timer.Stop();
                                timer.Dispose();
                            }
                            cachedClient = null;
                        }

                    }
                }
                return savePath;
            });
        }
    }
}