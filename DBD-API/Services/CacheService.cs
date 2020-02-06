using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Checksum;
using Microsoft.Extensions.Caching.Distributed;
using RestSharp;

namespace DBD_API.Services
{
    public class CacheService
    {
        private readonly IDistributedCache _cache;


        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<IRestResponse> GetCachedRequest(RestRequest request, string branch)
        {
            var cacheHit = await _cache.GetAsync($"request:{HashRequest(request, branch)}");
            return cacheHit == null ? null : ReadCachedRequest(cacheHit);
        }

        public async Task CacheRequest(RestRequest request, IRestResponse response, string branch, int ttl = 1800)
        {
            if (response == null || !response.IsSuccessful)
                return;

            await _cache.SetAsync($"request:{HashRequest(request, branch)}",
                CreateCachedRequest(response), new DistributedCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttl)
                });
        }


        private static string HashRequest(RestRequest request, string branch)
        {
            var hasher = new Crc32();
            using (var rawStream = new MemoryStream())
            using (var byteStream = new BinaryWriter(rawStream))
            {
                byteStream.Write(branch);
                byteStream.Write((int)request.Method);
                byteStream.Write(request.Resource);
                byteStream.Write(string.Join("&", request.Parameters
                    .Where(x => x.Type == ParameterType.QueryString)
                    .Select(x => $"{x.Name}={x.Value}")));

                hasher.Update(rawStream.GetBuffer());

                var hash = hasher.Value.ToString("x");
                return hash;
            }

        }

        private static RestResponse ReadCachedRequest(byte[] content)
        {
            using (var stream = new MemoryStream(content))
            using (var reader = new BinaryReader(stream))
            {
                var byteCount = reader.ReadInt64();
                var response = new RestResponse()
                {
                    ResponseStatus = ResponseStatus.Completed,
                    StatusCode = (HttpStatusCode)reader.ReadUInt32(),
                    ContentLength = reader.ReadInt64(),
                    ContentType = reader.ReadString(),
                    RawBytes = reader.ReadBytes((int)byteCount),
                };

                response.Content = Encoding.Unicode.GetString(response.RawBytes);

                return response;
            }
        }

        private static byte[] CreateCachedRequest(IRestResponse response)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(response.RawBytes.LongLength);
                writer.Write((uint)response.StatusCode);
                writer.Write(response.ContentLength);
                writer.Write(response.ContentType);
                writer.Write(response.RawBytes);
                return stream.GetBuffer();
            }
        }
    }
}
