using SteamKit2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Unified.Internal;

using LicenseList = System.Collections.Generic.List<SteamKit2.SteamApps.LicenseListCallback.License>;
using PICSProductInfo = SteamKit2.SteamApps.PICSProductInfoCallback.PICSProductInfo;

namespace DBD_API.Modules.Steam.ContentManagement
{
    class CachedManifest
    {
        public ulong id;
        public byte[] checksum;
        public byte[] manifest;
    }

    class DepotDownloadInfo
    {
        public uint id { get; set; }
        public string contentName { get; set; }
        public ulong manifestId { get; set; }
        public ulong version { get; set; }
        public byte[] depotKey { get; set; }

    }

    class ChunkMatch
    {
        public ChunkMatch(ProtoManifest.ChunkData oldChunk, ProtoManifest.ChunkData newChunk)
        {
            OldChunk = oldChunk;
            NewChunk = newChunk;
        }
        public ProtoManifest.ChunkData OldChunk { get; private set; }
        public ProtoManifest.ChunkData NewChunk { get; private set; }
    }

    class ContentDownloader
    {
        private const int MAX_DOWNLOADS = 8;
        private const uint INVALID_ID = uint.MaxValue;
        private const ulong INVALID_LONG_ID = ulong.MaxValue;

        private ConcurrentDictionary<uint, PICSProductInfo> _packageInfo;
        private ConcurrentDictionary<uint, PICSProductInfo> _appInfo;
        private ConcurrentDictionary<uint, byte[]> _depotKeys;

        private CDNClientPool _cdnClientPool;
        public readonly LicenseList _licenses;

        private readonly SteamClient _client;
        private readonly SteamApps _apps;

        private readonly string _cacheDir;
        private readonly string _stagingDir;
        private readonly string _dataDir;


        public ContentDownloader(SteamClient client, CDNClientPool cdnClientPool, LicenseList licenses)
        {
            _licenses = licenses;
            _cdnClientPool = cdnClientPool;
            _client = client;

            _apps = client.GetHandler<SteamApps>();


            _depotKeys = new ConcurrentDictionary<uint, byte[]>();
            _packageInfo = new ConcurrentDictionary<uint, PICSProductInfo>();
            _appInfo = new ConcurrentDictionary<uint, PICSProductInfo>();


            var currentDir = Directory.GetCurrentDirectory();
            _stagingDir = Path.Combine(Path.GetTempPath(), "content_downloader.staging");
            _cacheDir = Path.Combine(currentDir, "manifest_cache");
            _dataDir = Path.Combine(currentDir, "data");

            if (!Directory.Exists(_stagingDir)) Directory.CreateDirectory(_stagingDir);
            if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_stagingDir);
            if (!Directory.Exists(_dataDir)) Directory.CreateDirectory(_dataDir);
        }

        public async Task<bool> AccountOwns(uint depotId)
        {
            if (_licenses == null && _client.SteamID.AccountType != EAccountType.AnonUser)
                return false;

            IEnumerable<uint> licenseQuery = _client.SteamID.AccountType == EAccountType.AnonUser ?
                new List<uint>() { 17906 } :
                _licenses?.Select(x => x.PackageID);

            if (!await GetPackageInfo(licenseQuery))
                return false;

            foreach (var license in licenseQuery)
            {
                if (!_packageInfo.TryGetValue(license, out var package) || package == null)
                    continue;

                if (package.KeyValues["appids"].Children.Any(child => child.AsUnsignedInteger() == depotId)
                    || package.KeyValues["depotids"].Children.Any(child => child.AsUnsignedInteger() == depotId))
                    return true;
            }

            return false;
        }

        public async Task DownloadFilesAsync(uint appId, string branch, List<Regex> fileRegexes, CancellationToken token)
        {
            if (fileRegexes == null || fileRegexes.Count < 1)
                throw new Exception("File regex required");

            if (!await AccountOwns(appId))
                throw new Exception("Account does not own app");

            var depotIds = new List<uint>();
            var depots = await GetAppSection(appId, EAppInfoSection.Depots);
            if (depots == null || depots.Equals(default))
                throw new Exception("Couldn't find any depots for app");

            foreach (var depotSection in depots.Children)
            {
                var id = INVALID_ID;

                if (depotSection.Children.Count == 0) continue;
                if (!uint.TryParse(depotSection.Name, out id)) continue;

                var depotConfig = depotSection["config"];
                if (depotConfig != KeyValue.Invalid && depotConfig["oslist"] != KeyValue.Invalid &&
                    !string.IsNullOrWhiteSpace(depotConfig["oslist"].Value))
                {
                    var osList = depotConfig["oslist"].Value.Split(',');
                    if (!osList.Contains("windows"))
                        continue;
                }

                depotIds.Add(id);
            }

            if (depotIds.Count == 0)
                throw new Exception("Couldn't find any depots for app");

            var depotDownloadInfo = new List<DepotDownloadInfo>();
            foreach (var depot in depotIds)
            {
                try
                {
                    var info = await GetDepotInfo(depot, appId, branch);
                    if (info != null) depotDownloadInfo.Add(info);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception {0}", ex);
                }
            }

            await DownloadDepotsAsync(appId, branch, depotDownloadInfo, fileRegexes, token);
        }

        private static string EncodeHexString(byte[] input)
        {
            return input.Aggregate(new StringBuilder(),
                (sb, v) => sb.Append(v.ToString("x2"))
            ).ToString();
        }

        private static byte[] AdlerHash(byte[] input)
        {
            uint a = 0, b = 0;
            for (int i = 0; i < input.Length; i++)
            {
                a = (a + input[i]) % 65521;
                b = (b + a) % 65521;
            }
            return BitConverter.GetBytes(a | (b << 16));
        }

        private static List<ProtoManifest.ChunkData> ValidateFileChecksums(FileStream fs,
            ProtoManifest.ChunkData[] chunkData)
        {
            int read;
            var neededChunks = new List<ProtoManifest.ChunkData>();

            foreach (var data in chunkData)
            {
                var chunk = new byte[data.UncompressedLength];
                fs.Seek((long)data.Offset, SeekOrigin.Begin);
                read = fs.Read(chunk, 0, (int)data.UncompressedLength);

                byte[] tempChunk;
                if (read < data.UncompressedLength)
                {
                    tempChunk = new byte[read];
                    Array.Copy(chunk, 0, tempChunk, 0, read);
                }
                else
                    tempChunk = chunk;

                if (!AdlerHash(tempChunk).SequenceEqual(data.Checksum))
                    neededChunks.Add(data);
            }

            return neededChunks;
        }

        private async Task<bool> GetPackageInfo(IEnumerable<uint> packageIds)
        {
            var packages = packageIds.ToList();
            packages.RemoveAll(pid => _packageInfo.ContainsKey(pid));
            if (packages.Count == 0)
                return true;

            var result = await _apps.PICSGetProductInfo(new List<uint>(), packages);
            if (!result.Complete)
                return false;

            foreach (var packageInfoCallback in result.Results)
            {
                foreach (var packageVal in packageInfoCallback.Packages)
                {
                    var package = packageVal.Value;
                    _packageInfo[package.ID] = package;
                }

                foreach (var package in packageInfoCallback.UnknownPackages)
                    _packageInfo[package] = null;
            }

            return true;
        }

        private async Task<PICSProductInfo> GetAppInfo(uint appId)
        {
            if (_appInfo.TryGetValue(appId, out var cachedAppInfo) && cachedAppInfo != null)
                return cachedAppInfo;

            var accessTokens =
                await _apps.PICSGetAccessTokens(new List<uint> { appId }, new List<uint>());
            var request = new SteamApps.PICSRequest(appId);

            if (accessTokens.AppTokensDenied.Contains(appId))
                throw new Exception("Unable to get access token for app");

            if (accessTokens.AppTokens.TryGetValue(appId, out var appToken))
            {
                request.Public = false;
                request.AccessToken = appToken;
            }

            var productInfo = await _apps.PICSGetProductInfo(new List<SteamApps.PICSRequest> { request },
                new List<SteamApps.PICSRequest> { });
            if (productInfo.Failed)
                throw new Exception("Failed to get product info for app");

            PICSProductInfo info = null;
            foreach (var result in productInfo.Results)
                foreach (var appKv in result.Apps)
                {
                    var app = appKv.Value;
                    if (app.ID == appId)
                        info = app;
                    _appInfo[app.ID] = app;
                }
            
            if (info == null)
                throw new Exception("Steam didnt return product info for app");

            return info;
        }

        private async Task<KeyValue> GetAppSection(uint appId, EAppInfoSection section)
        {
            var app = await GetAppInfo(appId);
            var appInfo = app.KeyValues;
            string sectionKey;

            switch (section)
            {
                case EAppInfoSection.Common:
                    sectionKey = "common";
                    break;
                case EAppInfoSection.Extended:
                    sectionKey = "extended";
                    break;
                case EAppInfoSection.Config:
                    sectionKey = "config";
                    break;
                case EAppInfoSection.Depots:
                    sectionKey = "depots";
                    break;
                default:
                    throw new NotImplementedException();
            }

            return appInfo.Children.Where(c => c.Name == sectionKey)?.FirstOrDefault();
        }

        private async Task<ulong> GetDepotManifest(uint depotId, uint appId, string branch)
        {
            var depots = await GetAppSection(appId, EAppInfoSection.Depots);
            var depotChild = depots[depotId.ToString()];

            if (depotChild == KeyValue.Invalid)
                return INVALID_LONG_ID;

            if (depotChild["manifests"] == KeyValue.Invalid && depotChild["depotfromapp"] != KeyValue.Invalid)
            {
                var otherAppId = depotChild["depotfromapp"].AsUnsignedInteger();
                if (otherAppId == appId)
                    return INVALID_LONG_ID;

                return await GetDepotManifest(depotId, otherAppId, branch);
            }

            var manifests = depotChild["manifests"];
            var manifestsEncrypted = depotChild["encryptedmanifests"];

            if (manifests.Children.Count == 0 && manifestsEncrypted.Children.Count == 0)
                return INVALID_LONG_ID;

            var manifestNode = manifests[branch];
            if (branch == "Public" || manifestNode != KeyValue.Invalid)
                return manifestNode.Value == null ? INVALID_LONG_ID : ulong.Parse(manifestNode.Value);


            // TODO: implement encrypted branches, but for now we don't need them ig
            var encryptedNode = manifestsEncrypted[branch];
            if (encryptedNode != KeyValue.Invalid)
                throw new Exception("Passworded branches not supported yet");

            return INVALID_LONG_ID;

        }

        private async Task<string> GetAppOrDepotName(uint depotId, uint appId)
        {
            if (depotId == INVALID_ID)
            {
                var info = await GetAppSection(appId, EAppInfoSection.Common);
                return info == null ? string.Empty : info["name"].AsString();
            }
            else
            {
                var depots = await GetAppSection(appId, EAppInfoSection.Depots);
                if (depots == null)
                    return string.Empty;

                var depotChild = depots[depotId.ToString()];
                return depotChild == null ? string.Empty : depotChild["name"].AsString();
            }
        }

        private async Task<uint> GetAppBuildNumber(uint appId, string branch)
        {
            if (appId == INVALID_ID)
                return 0;

            var depots = await GetAppSection(appId, EAppInfoSection.Depots);
            var branches = depots["branches"];
            var node = branches[branch];

            if (node == KeyValue.Invalid)
                return 0;

            var buildId = node["buildid"];
            return buildId == KeyValue.Invalid ? (uint)0 : uint.Parse(buildId.Value);
        }

        private async Task<byte[]> GetDepotKey(uint depotId, uint appId = 0)
        {
            if (_depotKeys.TryGetValue(depotId, out var depotKey) && depotKey != null)
                return depotKey;

            var keyResult = await _apps.GetDepotDecryptionKey(depotId, appId);
            if (keyResult.Result != EResult.OK)
                return null;

            _depotKeys[keyResult.DepotID] = keyResult.DepotKey;
            return keyResult.DepotKey;
        }

        private async Task<DepotDownloadInfo> GetDepotInfo(uint depotId, uint appId, string branch)
        {
            var manifestId = await GetDepotManifest(depotId, appId, branch);
            if (manifestId == INVALID_LONG_ID)
                return null;

            var depotKey = await GetDepotKey(depotId, appId);
            if (depotKey == null)
                return null;

            return new DepotDownloadInfo()
            {
                id = depotId,
                manifestId = manifestId,
                contentName = await GetAppOrDepotName(depotId, appId),
                version = await GetAppBuildNumber(appId, branch),
                depotKey = depotKey
            };
        }

        private CachedManifest ReadManifest(uint depotId)
        {
            CachedManifest manifest = null;
            var fileName = Path.Combine(_cacheDir, $"{depotId}.manifest.cache");

            if (File.Exists(fileName))
            {
                using (var reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
                {
                    var manifestId = reader.ReadUInt64();
                    var checkSumLen = reader.ReadInt32();
                    var manifestLen = reader.ReadInt32();

                    manifest = new CachedManifest()
                    {
                        id = manifestId,
                        checksum = reader.ReadBytes(checkSumLen),
                        manifest = reader.ReadBytes(manifestLen)
                    };
                }

            }

            return manifest;
        }

        private void SaveManifest(uint depotId, CachedManifest manifest)
        {
            var fileName = Path.Combine(_cacheDir, $"{depotId}.manifest.cache");
            using (var writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                writer.Write(manifest.id);
                writer.Write(manifest.checksum.Length);
                writer.Write(manifest.manifest.Length);
                writer.Write(manifest.checksum);
                writer.Write(manifest.manifest);
            }
        }

        private Tuple<bool, string> TestFilePath(string filePath, List<Regex> fileRegexes)
        {
            var regex = fileRegexes.FirstOrDefault(x => x.IsMatch(filePath));
            if (regex == null || regex.Equals(default))
                return Tuple.Create(false, "");

            var match = regex.Match(filePath);
            var newFilePath = match.Groups.Count > 1 ?
                Path.Combine(match.Groups.Values.Where((x, y) => y > 0).Select(x => x.Value).ToArray()) :
                match.Groups[0].Value;

            return Tuple.Create(true, newFilePath);
        }

        private async Task DownloadDepotsAsync(uint appId, string branch, List<DepotDownloadInfo> depots, List<Regex> fileRegexes, CancellationToken cannToken)
        {
            branch = branch.ToLower();

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cannToken);
            var token = cts.Token;

            foreach (var depot in depots)
            {
                Console.WriteLine("Downloading depot {0} '{1}'", depot.id, depot.contentName);

                ProtoManifest manifest = null;
                var cachedManifest = ReadManifest(depot.id);

                if (cachedManifest != null && cachedManifest.id == depot.manifestId)
                {
                    try
                    {
                        manifest = ProtoManifest.LoadFromBuffer(cachedManifest.manifest, out var checkSum);
                        if (!cachedManifest.checksum.SequenceEqual(checkSum))
                            manifest = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to load cached manifest: {0}", ex);
                        manifest = null;
                    }
                }

                if (manifest == null)
                {
                    Console.WriteLine("Downloading manifest for depot '{0}'", depot.id);
                    DepotManifest depotManifest = null;

                    while (depotManifest == null)
                    {
                        CDNClient client = null;
                        try
                        {
                            client = await _cdnClientPool.GetConnectionForDepotAsync(appId, depot.id, depot.depotKey,
                                token);

                            depotManifest = await client.DownloadManifestAsync(depot.id, depot.manifestId);
                            _cdnClientPool.ReturnConnection(client);
                        }
                        catch (WebException e)
                        {
                            _cdnClientPool.ReturnBrokenConnection(client);

                            if (e.Status == WebExceptionStatus.ProtocolError)
                            {
                                var response = e.Response as HttpWebResponse;
                                if (response?.StatusCode == HttpStatusCode.Unauthorized || response?.StatusCode == HttpStatusCode.Forbidden)
                                {
                                    Console.WriteLine("Encountered 401 for depot manifest {0} {1}. Aborting.", depot.id, depot.manifestId);
                                    break;
                                }
                                else
                                    Console.WriteLine("Encountered error downloading depot manifest {0} {1}: {2}", depot.id, depot.manifestId, response?.StatusCode);
                            }
                            else
                                Console.WriteLine("Encountered error downloading manifest for depot {0} {1}: {2}", depot.id, depot.manifestId, e.Status);
                        }
                        catch (Exception e)
                        {
                            _cdnClientPool.ReturnBrokenConnection(client);
                            Console.WriteLine("Encountered error downloading manifest for depot {0} {1}: {2}", depot.id, depot.manifestId, e.Message);
                        }
                    }

                    if (depotManifest == null)
                    {
                        Console.WriteLine("\nUnable to download manifest {0} for depot {1}", depot.manifestId, depot.id);
                        return;
                    }


                    // manifest cache'ing
                    var newCachedManifest = new CachedManifest() { id = depot.manifestId };
                    manifest = new ProtoManifest(depotManifest, depot.manifestId);
                    manifest.SaveToBuffer(out newCachedManifest.manifest, out newCachedManifest.checksum);
                    SaveManifest(depot.id, newCachedManifest);
                }


                var stagingDir = Path.Combine(_stagingDir, depot.id.ToString());

                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);

                Directory.CreateDirectory(stagingDir);

                var semaphone = new SemaphoreSlim(MAX_DOWNLOADS);
                var taskList = new List<Task>();

                ulong completeDownloadSize = 0;
                ulong sizeDownloaded = 0;

                foreach (var file in manifest.Files)
                {
                    if (file.Flags.HasFlag(EDepotFileFlag.Directory))
                        continue;

                    var (regexTest, filePath) = TestFilePath(file.FileName, fileRegexes);
                    if (!regexTest)
                        continue;

                    Directory.CreateDirectory(Path.Combine(_dataDir, branch, Path.GetDirectoryName(filePath)));

                    var fileHash = EncodeHexString(file.FileHash);
                    var stagingPath = Path.Combine(stagingDir, fileHash, Path.GetFileName(filePath));
                    var finalPath = Path.Combine(_dataDir, branch, filePath);

                    completeDownloadSize += file.TotalSize;


                    var task = Task.Run(async () =>
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            await semaphone.WaitAsync(token);
                            token.ThrowIfCancellationRequested();

                            if (File.Exists(stagingPath))
                                File.Delete(stagingPath);

                            FileStream fs = null;
                            List<ProtoManifest.ChunkData> neededChunks;
                            var fileInfo = new FileInfo(finalPath);

                            if (!fileInfo.Exists)
                            {
                                fs = File.Create(finalPath);
                                fs.SetLength((long)file.TotalSize);
                                neededChunks = new List<ProtoManifest.ChunkData>(file.Chunks);
                            }
                            else
                            {
                                ProtoManifest.FileData oldManifestFile = null;
                                if (cachedManifest != null)
                                {
                                    oldManifestFile = manifest.Files.SingleOrDefault(x => x.FileName == file.FileName);
                                }

                                if (oldManifestFile != null)
                                {
                                    neededChunks = new List<ProtoManifest.ChunkData>();
                                    if (!oldManifestFile.FileHash.SequenceEqual(file.FileHash))
                                    {
                                        var matchingChunks = new List<ChunkMatch>();
                                        foreach (var chunk in file.Chunks)
                                        {
                                            var oldChunk = oldManifestFile.Chunks.FirstOrDefault(x => x.ChunkID.SequenceEqual(chunk.ChunkID));
                                            if (oldChunk != null)
                                                matchingChunks.Add(new ChunkMatch(oldChunk, chunk));
                                            else
                                                neededChunks.Add(chunk);
                                        }

                                        File.Move(finalPath, stagingPath);
                                        fs = File.Open(finalPath, FileMode.Create);
                                        fs.SetLength((long)file.TotalSize);

                                        using (var oldFs = File.Open(stagingPath, FileMode.Open))
                                        {
                                            foreach (var match in matchingChunks)
                                            {
                                                oldFs.Seek((long)match.OldChunk.Offset, SeekOrigin.Begin);
                                                var tmp = new byte[match.OldChunk.UncompressedLength];
                                                oldFs.Read(tmp, 0, tmp.Length);

                                                var hash = AdlerHash(tmp);
                                                if (!hash.SequenceEqual(match.OldChunk.Checksum))
                                                    neededChunks.Add(match.NewChunk);
                                                else
                                                {
                                                    fs.Seek((long)match.NewChunk.Offset, SeekOrigin.Begin);
                                                    fs.Write(tmp, 0, tmp.Length);
                                                }
                                            }

                                            File.Delete(stagingPath);
                                        }
                                    }
                                }
                                else
                                {
                                    fs = File.Open(finalPath, FileMode.Open);
                                    if ((ulong)fileInfo.Length != file.TotalSize)
                                        fs.SetLength((long)file.TotalSize);

                                    neededChunks = ValidateFileChecksums(fs, file.Chunks.OrderBy(x => x.Offset).ToArray());
                                }

                                if (neededChunks.Count == 0)
                                {
                                    sizeDownloaded += file.TotalSize;
                                    Console.WriteLine("{0,6:#00.00}% {1}", ((float)sizeDownloaded / (float)completeDownloadSize) * 100.0f, filePath);
                                    fs?.Dispose();
                                    return;
                                }
                                else
                                {
                                    sizeDownloaded += (file.TotalSize - (ulong)neededChunks.Select(x => (long)x.UncompressedLength).Sum());
                                }
                            }

                            foreach (var chunk in neededChunks)
                            {
                                if (token.IsCancellationRequested)
                                    break;

                                var chunkId = EncodeHexString(chunk.ChunkID);
                                CDNClient.DepotChunk chunkData = null;

                                while (!token.IsCancellationRequested)
                                {
                                    CDNClient client;
                                    try
                                    {
                                        client = await _cdnClientPool.GetConnectionForDepotAsync(appId, depot.id,
                                            depot.depotKey, token);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        break;
                                    }

                                    var data = new DepotManifest.ChunkData()
                                    {
                                        ChunkID = chunk.ChunkID,
                                        Checksum = chunk.Checksum,
                                        Offset = chunk.Offset,
                                        CompressedLength = chunk.CompressedLength,
                                        UncompressedLength = chunk.UncompressedLength
                                    };

                                    try
                                    {
                                        chunkData = await client.DownloadDepotChunkAsync(depot.id, data);
                                        _cdnClientPool.ReturnConnection(client);
                                        break;
                                    }
                                    catch (WebException e)
                                    {
                                        _cdnClientPool.ReturnBrokenConnection(client);

                                        if (e.Status == WebExceptionStatus.ProtocolError)
                                        {
                                            var response = e.Response as HttpWebResponse;
                                            if (response?.StatusCode == HttpStatusCode.Unauthorized || response?.StatusCode == HttpStatusCode.Forbidden)
                                            {
                                                Console.WriteLine("Encountered 401 for chunk {0}. Aborting.", chunkId);
                                                cts.Cancel();
                                                break;
                                            }
                                            else
                                                Console.WriteLine("Encountered error downloading chunk {0}: {1}", chunkId, response?.StatusCode);
                                        }
                                        else
                                            Console.WriteLine("Encountered error downloading chunk {0}: {1}", chunkId, e.Status);
                                    }
                                    catch (Exception e)
                                    {
                                        _cdnClientPool.ReturnBrokenConnection(client);
                                        Console.WriteLine("Encountered unexpected error downloading chunk {0}: {1}", chunkId, e.Message);
                                    }
                                }

                                if (chunkData == null)
                                {
                                    Console.WriteLine("Failed to find any server with chunk {0} for depot {1}. Aborting.", chunkId, depot.id);
                                    cts.Cancel();
                                }

                                token.ThrowIfCancellationRequested();

                                fs.Seek((long)chunk.Offset, SeekOrigin.Begin);
                                fs.Write(chunkData.Data, 0, chunkData.Data.Length);
                                sizeDownloaded += chunk.UncompressedLength;
                            }

                            fs?.Dispose();
                            Console.WriteLine("{0,6:#00.00}% {1}", ((float)sizeDownloaded / (float)completeDownloadSize) * 100.0f, filePath);
                        }
                        finally
                        {
                            semaphone.Release();
                        }
                    });

                    taskList.Add(task);
                }

                await Task.WhenAll(taskList);

                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);

                if (completeDownloadSize > 0)
                    Console.WriteLine("Depot {0} - Downloaded {1} bytes", depot.id, completeDownloadSize);

            }
        }

    }
}
