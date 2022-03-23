using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using Newtonsoft.Json.Linq;
using SongCore;

namespace BeatSaverUpdater
{
    internal static class Utilities
    {
        private static BeatSaver? beatSaverInstance;

        public static string GetBeatmapHash(this CustomPreviewBeatmapLevel beatmapLevel) =>
            SongCore.Utilities.Hashing.GetCustomLevelHash(beatmapLevel);

        public static async Task<Beatmap?> GetBeatSaverBeatmap(this CustomPreviewBeatmapLevel beatmapLevel, CancellationToken token)
        {
            if (beatSaverInstance == null)
            {
                var beatSaverOptions = new BeatSaverOptions(Plugin.Metadata.Name, Plugin.Metadata.HVersion.ToString());
                beatSaverInstance = new BeatSaver(beatSaverOptions);
            }

            var hash = beatmapLevel.GetBeatmapHash();
            var map = await beatSaverInstance.BeatmapByHash(hash, token);
            
            if (map != null && !string.Equals(map.LatestVersion.Hash, hash, StringComparison.OrdinalIgnoreCase))
            {
                return map;
            }

            return null;
        }

        public static async Task<bool> NeedsUpdate(this CustomPreviewBeatmapLevel beatmapLevel, CancellationToken token)
        {
            var map = await beatmapLevel.GetBeatSaverBeatmap(token);
            return map != null;
        }

        public static async Task<string?> UpdateBeatmap(this CustomPreviewBeatmapLevel beatmapLevel, CancellationToken token, IProgress<double> progress)
        {
            var songDownloaded = false;
            while (!songDownloaded)
            {
                try
                {
                    var map = await beatmapLevel.GetBeatSaverBeatmap(token);
                    if (map == null)
                    {
                        return null;
                    }

                    var customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }

                    var zip = await map.LatestVersion.DownloadZIP(token, progress).ConfigureAwait(false);
                    if (zip != null && !token.IsCancellationRequested)
                    {
                        await ExtractZipAsync(zip, customSongsPath, FolderNameForBeatSaverMap(map)).ConfigureAwait(false);
                        return map.LatestVersion.Hash;
                    }

                    songDownloaded = true;
                }
                catch (Exception e)
                {
                    if (!(e is TaskCanceledException))
                    {
                        Plugin.Log.Error($"Failed to download Song {beatmapLevel}. Exception: {e}");
                    }
                    songDownloaded = true;
                }
            }
            return null;
        }


        private static string FolderNameForBeatSaverMap(Beatmap song)
        {
            // A workaround for the max path issue and long folder names
            var longFolderName = song.ID + " (" + song.Metadata.LevelAuthorName + " - " + song.Metadata.SongName;
            return longFolderName.Truncate(49, true) + ")";
        }

        private static async Task ExtractZipAsync(byte[] zip, string customSongsPath, string songName, bool overwrite = false)
        {
            Stream zipStream = new MemoryStream(zip);
            try
            {
                var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                var basePath = "";
                basePath = string.Join("", songName.Split(Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray()));
                var path = Path.Combine(customSongsPath, basePath);

                if (!overwrite && Directory.Exists(path))
                {
                    var pathNum = 1;
                    while (Directory.Exists(path + $" ({pathNum})")) ++pathNum;
                    path += $" ({pathNum})";
                }

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                await Task.Run(() =>
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!string.IsNullOrWhiteSpace(entry.Name) && entry.Name == entry.FullName)
                        {
                            var entryPath = Path.Combine(path, entry.Name); // Name instead of FullName for better security and because song zips don't have nested directories anyway
                            if (overwrite || !File.Exists(entryPath)) // Either we're overwriting or there's no existing file
                                entry.ExtractToFile(entryPath, overwrite);
                        }
                    }
                }).ConfigureAwait(false);
                archive.Dispose();
            }
            catch (Exception e)
            {
                Plugin.Log.Error($"Unable to extract ZIP! Exception: {e}");
                return;
            }
            zipStream.Close();
        }
    }
}
