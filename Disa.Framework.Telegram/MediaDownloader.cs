using System;
using System.IO;
using System.Threading.Tasks;
using Disa.Framework.Bubbles;
using ProtoBuf;
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
                const uint chunkSize = 32768;
                if (bubble.AdditionalData != null)
                {
                    using (var memoryStream = new MemoryStream(bubble.AdditionalData))
                    {
                        var fileInformation = Serializer.Deserialize<FileInformation>(memoryStream);
                        var type = fileInformation.FileType;
                        if (bubble is ImageBubble)
                        {
                            if (type == "image")
                            {
                                savePath = MediaManager.GenerateDisaMediaLocationUsingExtension(
                                    MediaManager.GetDisaPicturesPath, Platform.GetExtensionFromMimeType("image/jpeg"));
                                DebugPrint(">>>>>>> The save path is " + savePath);
                            }
                        }

                        if (savePath == null) return savePath;
                        using (var fs = File.OpenWrite(savePath))
                        {
                            var fileSize = fileInformation.Size;
                            uint currentOffset = 0;


                            while (currentOffset <= fileSize)
                            {
                                var bytes = FetchFileBytes(
                                    (FileLocation) fileInformation.FileLocation,
                                    currentOffset, chunkSize);
                                if (bytes.Length == 0)
                                {
                                    break;
                                }
                                fs.Write(bytes, 0, bytes.Length);
                                var currentProgress = currentOffset/(float) fileSize;

                                progress((int)(currentProgress*100));
                                currentOffset += chunkSize;
                            }
                        }
                    }
                }
                return savePath;
            });
        }
    }
}