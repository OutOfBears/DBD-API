using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

using DBD_API.Modules.Steam;
using DBD_API.Modules.Steam.ContentManagement;

namespace DBD_API.Services
{
    public class SteamDepotService : IHostedService
    {
        private CancellationTokenSource _cancellation;
        private ContentDownloader _contentDownloader;
        private CDNClientPool _cdnClientPool;

        private readonly SteamService _steamService;
        

        public SteamDepotService(SteamService steamService)
        {
            _steamService = steamService;
        }

        public async Task MainEvent()
        {
            if (!_steamService.LicenseCompletionSource.Task.IsCompleted)
                await _steamService.LicenseCompletionSource.Task;

            Console.WriteLine("[Steam] Got {0} licenses", _steamService.Licenses.Count);


        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _cdnClientPool = new CDNClientPool(_steamService.Client, _cancellation);
            _contentDownloader = new ContentDownloader(_steamService.Client, _cdnClientPool, _steamService.Licenses);

            var task = Task.Factory.StartNew(MainEvent, _cancellation.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            return task.IsCompleted ? task : Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cancellation?.Cancel();
            return Task.CompletedTask;
        }
    }
}
