using Disa.Framework.Stickers;
using ProtoBuf;
using SharpTelegram.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Disa.Framework.Telegram
{
    public partial class Telegram : IStickers
    {
        private const string TAG_TELEGRAM_STICKERS = "[TelegramStickers]";

        #region IStickers

        public Task GetTrendingStickers(int page, Action<List<Sticker>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(null);
                };

                try
                {
                    // Currently a no-op for Telegram, once we get to Schema 66, we can add in.
                    errorResponse();
                    return;
                }
                catch(Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetTrendingStickers) + " exception getting trending sticker packs for Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task SearchStickers(string query, int page, Action<List<Sticker>> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(null);
                };

                try
                {
                    // Currently a no-op for Telegram
                    errorResponse();
                    return;
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(SearchStickers) + " exception searching stickers for Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task GetUserStickerPacks(string hash, Action<ServiceStickerPacks> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(null);
                };

                try
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        var args = new MessagesGetAllStickersArgs
                        {
                        };

                        if (!string.IsNullOrEmpty(hash))
                        {
                            args.Hash = uint.Parse(hash);
                        }

                        SharpTelegram.Schema.IMessagesAllStickers iMessagesAllStickers =
                            (SharpTelegram.Schema.IMessagesAllStickers)
                                TelegramUtils.RunSynchronously(
                                    client.Client.Methods.MessagesGetAllStickersAsync(args));

                        // Short circuit on a "not modified" response
                        if (iMessagesAllStickers is MessagesAllStickersNotModified)
                        {
                            errorResponse();
                            return;
                        }

                        // Ok, let's go for full blown processing
                        var messagesAllStickers = iMessagesAllStickers as MessagesAllStickers;

                        // Sanity check
                        if (messagesAllStickers == null)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetUserStickerPacks) + " MessagesAllStickers is null.");

                            errorResponse();
                            return;
                        }

                        // Loop over Telegram representations for sticker packs and build out a collection
                        // of Disa representations for sticker packs
                        var stickerPacks = new List<Disa.Framework.Stickers.StickerPack>();
                        foreach (var set in messagesAllStickers.Sets)
                        {
                            var disaStickerPack = HandleStickerSet(set);

                            if (disaStickerPack != null)
                            {
                                stickerPacks.Add(disaStickerPack);
                            }
                            else
                            {
                                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetUserStickerPacks) + " disaStickerPack is null.");
                            }
                        }
                        if (stickerPacks.Count == 0)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetUserStickerPacks) + " stickerPacks.Count is 0.");
                        }

                        var serviceStickerPacks = new ServiceStickerPacks
                        {
                            ServiceName = this.Information.ServiceName,
                            Hash = messagesAllStickers.Hash.ToString(),
                            StickerPacks = stickerPacks
                        };

                        result(serviceStickerPacks);
                    }
                }
                catch(Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetUserStickerPacks) + " exception getting user sticker packs for Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task GetAvailableStickerPacks(string hash, Action<ServiceStickerPacks> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(null);
                };

                try
                {
                    // Once we get to Telegram Schema 66, we'll implement available stickers
                    // as a call to GetTrendingStickerPacks.
                    // It appears, for Telegram, that available and trending sticker packs are the same.
                    errorResponse();
                    return;
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetAvailableStickerPacks) + " exception getting available sticker packs for Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task GetTrendingStickerPacks(string hash, Action<ServiceStickerPacks> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(null);
                };

                try
                {
                    // Once we get to Telegram Schema 66, we'll implement trending sticker packs
                    errorResponse();
                    return;
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetTrendingStickerPacks) + " exception getting trending sticker packs for Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task GetFullStickerPack(Stickers.StickerPack stickerPack, Action<FullStickerPack> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(null);
                };

                try
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        // IMPORTANT: We have stored away a Telegram StickerSet into StickerPack.AdditionalData
                        //            to represent the Telegram specific identity info for a sticker pack
                        var telegramInputStickerSet = HandleAdditionalData(stickerPack.AdditionalData);
                        if (telegramInputStickerSet == null)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetFullStickerPack) + " telegramInputStickerSet is null.");

                            errorResponse();
                            return;
                        }

                        var args = new MessagesGetStickerSetArgs
                        {
                            Stickerset = telegramInputStickerSet,
                        };

                        // Attempt to get the sticker pack
                        MessagesStickerSet messagesStickerSet =
                            (MessagesStickerSet)TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesGetStickerSetAsync(args));

                        var disaStickerPack = HandleStickerSet(messagesStickerSet.Set, skipFeaturedSticker: true);
                        if (disaStickerPack == null)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetFullStickerPack) + " disaStickerPack is null.");

                            errorResponse();
                            return;
                        }

                        // Good to go, let's build out a Disa representation for this sticker pack

                        // Emoji to sticker mapping for this sticker pack
                        var disaEmojiStickerPacks = new List<Disa.Framework.Stickers.EmojiStickerPack>();
                        foreach (var telegramPack in messagesStickerSet.Packs)
                        {
                            var disaEmojiStickerPack = HandleStickerPack(telegramPack);
                            if (disaEmojiStickerPack == null)
                            {
                                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetFullStickerPack) + " disaEmojiStickerPack is null.");

                                continue;
                            }

                            disaEmojiStickerPacks.Add(disaEmojiStickerPack);
                        }
                        if (disaEmojiStickerPacks.Count == 0)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetFullStickerPack) + " disaEmojiStickerPacks.Count is 0.");
                        }

                        // Telegram uses a Telegram Document to represent the location info for a Sticker.
                        // Let's convert that to a Disa Sticker representation.
                        var disaStickers = new List<Stickers.Sticker>();
                        foreach (var telegramDocument in messagesStickerSet.Documents)
                        {
                            var disaSticker = HandleStickerDocument(disaStickerPack.Id, telegramDocument);
                            if (disaSticker == null)
                            {
                                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetFullStickerPack) + " disaSticker is null.");

                                continue;
                            }

                            disaStickers.Add(disaSticker);
                        }
                        if (disaStickers.Count == 0)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetFullStickerPack) + " disaStickers.Count is 0.");

                            errorResponse();
                            return;
                        }

                        var disaFullStickerPack = new FullStickerPack
                        {
                            Id = stickerPack.Id,
                            ServiceName = stickerPack.ServiceName,
                            Location = stickerPack.Location,
                            Installed = stickerPack.Installed,
                            Archived = stickerPack.Archived,
                            FeaturedSticker = disaStickers[0],
                            Title = stickerPack.Title,
                            Count = stickerPack.Count,
                            AdditionalData = stickerPack.AdditionalData,
                            Stickers = disaStickers,
                            EmojiStickerPacks = disaEmojiStickerPacks,
                        };

                        result(disaFullStickerPack);
                    }
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(GetFullStickerPack) + " exception getting full sticker pack for Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task<StickerLocationInfo> DownloadSticker(Sticker sticker, Action<int> progress)
        {
            // Important: Note that the caller of DownloadSticker will move the file created to 
            //            a new location after this call.

            return Task.Factory.StartNew<StickerLocationInfo>(() =>
            {
                try
                {
                    // Sanity check
                    if (sticker.AdditionalData == null)
                    {
                        Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(DownloadSticker) + " sticker.AdditionalData is null in Telegram.");

                        return null;
                    }

                    // IMPORTANT: We have stored away a Telegram Document into Sticker.AdditionalData
                    //            to reprsent the Telegram specific location info for a sticker.
                    SharpTelegram.Schema.Document telegramDocument;
                    using (var memoryStream = new MemoryStream(sticker.AdditionalData))
                    {
                        telegramDocument = Serializer.Deserialize<SharpTelegram.Schema.Document>(memoryStream);
                    }
                    if (telegramDocument == null)
                    {
                        Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(DownloadSticker) + " telegramDocument is null in Telegram.");

                        return null;
                    }

                    // Let's get our service specific save path
                    string savePath = null;
                    var stickerId = telegramDocument.DcId + "-" + telegramDocument.Id;
                    savePath = MediaManager.GenerateStickerPath(this, stickerId) + ".webp";
                    if (savePath == null)
                    {
                        Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(DownloadSticker) + " savePath is null in Telegram.");
                        return null;
                    }

                    // Short circuit if already there
                    if (File.Exists(savePath))
                    {
                        return new StickerLocationInfo
                        {
                            LocationStill = savePath,
                            IsUrl = false
                        };
                    }

                    // IMPORTANT: We are only temporarily marking this file for deletion.
                    //            See the finally block below where we unmark for deletion.
                    Platform.MarkTemporaryFileForDeletion(savePath);

                    // Ok, good to go for downloading file to temporary location
                    var savePathTemp = savePath + DateTime.Now.ToString("ff") + ".tmp";
                    try
                    {
                        using (var fs = File.Open(savePathTemp, FileMode.Append))
                        {
                            var fileSize = telegramDocument.Size;
                            uint currentOffset = 0;
                            while (currentOffset < fileSize)
                            {
                                using (var downloadManager = new DownloadManager(telegramDocument, this))
                                {
                                    currentOffset = downloadManager.DownloadDocument(fs, currentOffset, progress);
                                }
                            }
                        }

                        // Ok, let's move this file into its official location
                        if (File.Exists(savePath))
                        {
                            File.Delete(savePath);
                        }
                        File.Move(savePathTemp, savePath);
                    }
                    catch (Exception e)
                    {
                        Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(DownloadSticker) + " exception in DownloadSticker in Telegram: " + e);

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
                        // Ok, we can flip back marking this file for delation that we did above
                        Platform.UnmarkTemporaryFileForDeletion(savePath, false);
                    }

                    return new StickerLocationInfo
                    {
                        LocationStill = savePath,
                        IsUrl = false
                    };
                }
                catch(Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(DownloadSticker) + " exception downloading sticker for Telegram: " + ex);

                    return null;
                }
            });
        }

        public Task StickerPacksReordered(string stickerPackId, int newPos, List<string> newOrder, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize our error response
                Action errorResponse = () =>
                {
                    result(false);
                };

                try
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        var telegramOrder = new List<ulong>();
                        foreach (var disaOrderEntry in newOrder)
                        {
                            var telegramOrderEntry = ulong.Parse(disaOrderEntry);
                            telegramOrder.Add(telegramOrderEntry);
                        }
                        if (telegramOrder.Count == 0)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPacksReordered) + " telegramOrder.Count is 0.");

                            errorResponse();
                            return;
                        }

                        var args = new MessagesReorderStickerSetsArgs
                        {
                            Order = telegramOrder
                        };

                        bool response =
                            (bool)TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesReorderStickerSetsAsync(args));

                        result(response);
                    }
                }
                catch(Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPacksReordered) + " exception reordering stickers in Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task StickerPackInstalled(Stickers.StickerPack stickerPack, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(false);
                };

                try
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        // IMPORTANT: We have stored away a Telegram StickerSet into StickerPack.AdditionalData
                        //            to reprsent the Telegram specific identity info for a sticker pack
                        var telegramInputStickerSet = HandleAdditionalData(stickerPack.AdditionalData);
                        if (telegramInputStickerSet == null)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackInstalled) + " telegramInputStickerSet is null in Telegram.");

                            errorResponse();
                            return;
                        }

                        var args = new MessagesInstallStickerSetArgs
                        {
                            Stickerset = telegramInputStickerSet,
                            Disabled = false
                        };

                        bool response =
                            (bool)TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesInstallStickerSetAsync(args));

                        result(response);
                    }
                }
                catch(Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackInstalled) + " exception installing sticker pack in Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task StickerPackArchived(Stickers.StickerPack stickerPack, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize our error response
                Action errorResponse = () =>
                {
                    result(false);
                };

                try
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        // IMPORTANT: We have stored away a Telegram StickerSet into StickerPack.AdditionalData
                        //            to reprsent the Telegram specific identity info for a sticker pack
                        var telegramInputStickerSet = HandleAdditionalData(stickerPack.AdditionalData);
                        if (telegramInputStickerSet == null)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackArchived) + " telegramInputStickerSet is null.");

                            errorResponse();
                            return;
                        }

                        var args = new MessagesInstallStickerSetArgs
                        {
                            Stickerset = telegramInputStickerSet,
                            Disabled = true
                        };

                        bool response =
                            (bool)TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesInstallStickerSetAsync(args));

                        result(response);
                    }
                }
                catch(Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackArchived) + " exception archiving sticker pack in Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task StickerPackUnarchived(Stickers.StickerPack stickerPack, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize our error response
                Action errorResponse = () =>
                {
                    result(false);
                };

                try
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        // IMPORTANT: We have stored away a Telegram StickerSet into StickerPack.AdditionalData
                        //            to reprsent the Telegram specific identity info for a sticker pack
                        var telegramInputStickerSet = HandleAdditionalData(stickerPack.AdditionalData);
                        if (telegramInputStickerSet == null)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackUnarchived) + " telegramInputStickerSet is null.");

                            errorResponse();
                            return;
                        }

                        var args = new MessagesInstallStickerSetArgs
                        {
                            Stickerset = telegramInputStickerSet,
                            Disabled = false
                        };

                        bool response =
                            (bool)TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesInstallStickerSetAsync(args));

                        result(response);
                    }
                }
                catch (Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackUnarchived) + " exception unarchiving sticker pack in Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        public Task StickerPackUninstalled(Stickers.StickerPack stickerPack, Action<bool> result)
        {
            return Task.Factory.StartNew(() =>
            {
                // Standardize error response
                Action errorResponse = () =>
                {
                    result(false);
                };

                try
                {
                    using (var client = new FullClientDisposable(this))
                    {
                        // IMPORTANT: We have stored away a Telegram StickerSet into StickerPack.AdditionalData
                        //            to reprsent the Telegram specific identity info for a sticker pack
                        var telegramInputStickerSet = HandleAdditionalData(stickerPack.AdditionalData);
                        if (telegramInputStickerSet == null)
                        {
                            Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackUninstalled) + " telegramInputStickerSet is null.");

                            errorResponse();
                            return;
                        }

                        var args = new MessagesUninstallStickerSetArgs
                        {
                            Stickerset = telegramInputStickerSet,
                        };

                        bool response =
                            (bool)TelegramUtils.RunSynchronously(
                                client.Client.Methods.MessagesUninstallStickerSetAsync(args));

                        result(response);
                    }
                }
                catch(Exception ex)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(StickerPackUninstalled) + " exception uninstalling sticker pack in Telegram: " + ex);

                    errorResponse();
                }
            });
        }

        #endregion

        #region Helper methods

        // Helper function to convert a Telegram IStickerSet into a Disa Framework StickerPack
        private Disa.Framework.Stickers.StickerPack HandleStickerSet(IStickerSet stickerSet, bool skipFeaturedSticker=false)
        {
            var telegramStickerSet = stickerSet as SharpTelegram.Schema.StickerSet;

            var disaStickerPack = new Disa.Framework.Stickers.StickerPack
            {
                Id = telegramStickerSet.Id.ToString(),
                ServiceName = this.Information.ServiceName,
                Installed = telegramStickerSet.Installed != null ? true : false,
                Archived = telegramStickerSet.Disabled != null ? true : false,
                Title = telegramStickerSet.Title,
                Count = telegramStickerSet.Count,
            };

            using (var memoryStream = new MemoryStream())
            {
                // IMPORTANT: We store away a Telegram StickerSet into StickerPack.AdditionalData
                //            to reprsent the Telegram specific identity info for a sticker pack.
                Serializer.Serialize<SharpTelegram.Schema.StickerSet>(memoryStream, telegramStickerSet);
                disaStickerPack.AdditionalData = memoryStream.ToArray();
            }

            // Some callers will already have the info needed for StickerPack.FeaturedSticker
            if (skipFeaturedSticker)
            {
                return disaStickerPack;
            }

            // Ok, now we need to get a representation for our FeaturedSticker. For this
            // we'll just use the first sticker in the Telegram StickerSet
            using (var client = new FullClientDisposable(this))
            {
                var telegramInputStickerSet = HandleAdditionalData(disaStickerPack.AdditionalData);
                if (telegramInputStickerSet == null)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(HandleStickerSet) + " telegramInputStickerSet is null.");
                    return null;
                }

                var args = new MessagesGetStickerSetArgs
                {
                    Stickerset = telegramInputStickerSet,
                };

                // Attempt to get the full representation for the sticker set
                MessagesStickerSet messagesStickerSet =
                    (MessagesStickerSet)TelegramUtils.RunSynchronously(
                        client.Client.Methods.MessagesGetStickerSetAsync(args));

                if (messagesStickerSet == null ||
                    messagesStickerSet.Documents == null ||
                    messagesStickerSet.Documents.Count == 0)
                {
                    Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(HandleStickerSet) + " messagesStickerSet did not pass checks.");
                    return null;
                }

                disaStickerPack.FeaturedSticker = HandleStickerDocument(disaStickerPack.Id, messagesStickerSet.Documents[0]);
            }

            return disaStickerPack;
        }

        // Helper function to convert a Telegram IDocument into a Disa Framework Sticker
        private Disa.Framework.Stickers.Sticker HandleStickerDocument(string stickerPackId, IDocument document)
        {
            var telegramDocument = document as SharpTelegram.Schema.Document;
            if (telegramDocument == null)
            {
                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(HandleStickerDocument) + " telegramDocument is null.");
                return null;
            }

            using (var memoryStream = new MemoryStream())
            {
                // IMPORTANT: We store away a Telegram Document into Sticker.AdditionalData
                //            to reprsent the Telegram specific location info for a sticker.
                Serializer.Serialize<SharpTelegram.Schema.Document>(memoryStream, telegramDocument);

                var disaSticker = new Disa.Framework.Stickers.Sticker
                {
                    Id = telegramDocument.Id.ToString(),
                    ServiceName = this.Information.ServiceName,
                    StickerPackId = stickerPackId,
                    AdditionalData = memoryStream.ToArray()
                };

                return disaSticker;
            }
        }

        // Helper function to convert a Telegram IStickerPack into a Disa Framework EmojiStickerPack
        private Disa.Framework.Stickers.EmojiStickerPack HandleStickerPack(IStickerPack stickerPack)
        {
            var telegramStickerPack = stickerPack as SharpTelegram.Schema.StickerPack;
            if (telegramStickerPack == null)
            {
                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(HandleStickerPack) + " telegramStickerPack is null.");
                return null;
            }

            var disaStickerPack = new Disa.Framework.Stickers.EmojiStickerPack
            {
                Emoticon = telegramStickerPack.Emoticon,
                Stickers = new List<string>()
            };

            // These are actually document id's
            foreach(var document in telegramStickerPack.Documents)
            {
                disaStickerPack.Stickers.Add(document.ToString());
            }

            return disaStickerPack;
        }

        // Helper function to convert a Disa StickerPack.AdditionalData to a Telegram IInputStickerSet
        private SharpTelegram.Schema.IInputStickerSet HandleAdditionalData(byte[] additionalData)
        {
            // Sanity check
            if (additionalData == null)
            {
                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(HandleAdditionalData) + " additionalData is null.");
                return null;
            }

            // IMPORTANT: We have stored away a Telegram StickerSet into StickerPack.AdditionalData
            //            to reprsent the Telegram specific identity info for a sticker pack
            SharpTelegram.Schema.StickerSet stickerSet;
            using (var memoryStream = new MemoryStream(additionalData))
            {
                stickerSet = Serializer.Deserialize<SharpTelegram.Schema.StickerSet>(memoryStream);
            }
            if (stickerSet == null)
            {
                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(HandleAdditionalData) + " stickerSet is null.");
                return null;
            }

            // Build out telegram identity for this sticker pack
            IInputStickerSet telegramInputStickerSet = null;
            if (stickerSet.Id != 0)
            {
                telegramInputStickerSet = new InputStickerSetID
                {
                    Id = stickerSet.Id,
                    AccessHash = stickerSet.AccessHash
                };
            }
            else if (!string.IsNullOrEmpty(stickerSet.ShortName))
            {
                telegramInputStickerSet = new InputStickerSetShortName
                {
                    ShortName = stickerSet.ShortName
                };
            }
            else
            {
                Utils.DebugPrint(TAG_TELEGRAM_STICKERS, nameof(HandleAdditionalData) + " unable to construct telegramInputStickerSet.");
                return null;
            }

            return telegramInputStickerSet;
        }

        #endregion
    }
}
