﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;

namespace CompatBot.Database.Providers
{
    internal static class ThumbnailProvider
    {
        public static async Task<string> GetThumbnailUrlAsync(this DiscordClient client, string productCode)
        {
            using (var db = new ThumbnailDb())
            {
                var thumb = await db.Thumbnail.FirstOrDefaultAsync(t => t.ProductCode == productCode.ToUpperInvariant()).ConfigureAwait(false);
                //todo: add search task if not found
                if (thumb?.EmbeddableUrl is string embeddableUrl && !string.IsNullOrEmpty(embeddableUrl))
                    return embeddableUrl;

                if (thumb?.Url is string url && !string.IsNullOrEmpty(url))
                {
                    if (!string.IsNullOrEmpty(Path.GetExtension(url)))
                    {
                        thumb.EmbeddableUrl = url;
                        await db.SaveChangesAsync().ConfigureAwait(false);
                        return url;
                    }

                    try
                    {
                        using (var httpClient = new HttpClient())
                        using (var img = await httpClient.GetStreamAsync(url).ConfigureAwait(false))
                        {
                            var spam = await client.GetChannelAsync(Config.ThumbnailSpamId).ConfigureAwait(false);
                            //var message = await spam.SendFileAsync(img, (thumb.ContentId ?? thumb.ProductCode) + ".jpg").ConfigureAwait(false);
                            var message = await spam.SendFileAsync((thumb.ContentId ?? thumb.ProductCode) + ".jpg", img).ConfigureAwait(false);
                            thumb.EmbeddableUrl = message.Attachments.First().Url;
                            await db.SaveChangesAsync().ConfigureAwait(false);
                            return thumb.EmbeddableUrl;
                        }
                    }
                    catch (Exception e)
                    {
                        client.DebugLogger.LogMessage(LogLevel.Warning, "", e.ToString(), DateTime.Now);
                    }
                }
            }
            return null;
        }
    }
}