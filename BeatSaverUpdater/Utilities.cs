﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp;
using BeatSaverSharp.Models;
using IPA.Utilities;

namespace BeatSaverUpdater
{
    internal static class Utilities
    {
        private static BeatSaver? beatSaverInstance;
        private static ConcurrentDictionary<string, Beatmap?>? cachedMaps;

        public static string GetBeatmapHash(this IPreviewBeatmapLevel beatmapLevel) =>
            beatmapLevel.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");

        public static async Task<Beatmap?> GetBeatSaverBeatmap(this IPreviewBeatmapLevel beatmapLevel, CancellationToken token)
        {
            if (beatSaverInstance == null)
            {
                var beatSaverOptions = new BeatSaverOptions(applicationName: Plugin.Metadata.Name, version: Plugin.Metadata.HVersion.ToString());
                beatSaverInstance = new BeatSaver(beatSaverOptions);
            }

            var hash = beatmapLevel.GetBeatmapHash();

            if (cachedMaps != null && cachedMaps.TryGetValue(hash, out var cachedMap))
            {
                return cachedMap;
            }
            
            var map = await beatSaverInstance.BeatmapByHash(hash, token);

            if (!token.IsCancellationRequested)
            {
                cachedMaps ??= new ConcurrentDictionary<string, Beatmap?>();
                cachedMaps.TryAdd(hash, map);
            }

            return map;
        }

        public static async Task<bool> NeedsUpdate(this IPreviewBeatmapLevel beatmapLevel, CancellationToken token)
        {
            var map = await beatmapLevel.GetBeatSaverBeatmap(token);
            return map != null && !string.Equals(map.LatestVersion.Hash, beatmapLevel.GetBeatmapHash(), StringComparison.InvariantCultureIgnoreCase);
        }

        public static async Task<string?> UpdateBeatmap(this IPreviewBeatmapLevel beatmapLevel, CancellationToken token, IProgress<double> progress)
        {
            bool songDownloaded = false;
            while (!songDownloaded)
            {
                try
                {
                    var map = await beatmapLevel.GetBeatSaverBeatmap(token);
                    if (map == null)
                    {
                        return null;
                    }

                    string customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }

                    var zip = await map.LatestVersion.DownloadZIP(token, progress).ConfigureAwait(false);
                    if (zip != null && !token.IsCancellationRequested)
                    {
                        await ExtractZipAsync(zip, customSongsPath, FolderNameForBeatsaverMap(map)).ConfigureAwait(false);
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


        private static string FolderNameForBeatsaverMap(Beatmap song)
        {
            // A workaround for the max path issue and long folder names
            string longFolderName = song.ID + " (" + song.Metadata.LevelAuthorName + " - " + song.Metadata.SongName;
            return longFolderName.Truncate(49, true) + ")";
        }

        private static async Task ExtractZipAsync(byte[] zip, string customSongsPath, string songName, bool overwrite = false)
        {
            Stream zipStream = new MemoryStream(zip);
            try
            {
                ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                string basePath = "";
                basePath = string.Join("", songName.Split(Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray()));
                string path = Path.Combine(customSongsPath, basePath);

                if (!overwrite && Directory.Exists(path))
                {
                    int pathNum = 1;
                    while (Directory.Exists(path + $" ({pathNum})")) ++pathNum;
                    path += $" ({pathNum})";
                }

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                await Task.Run(() =>
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!string.IsNullOrWhiteSpace(entry.Name))
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