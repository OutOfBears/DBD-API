using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DBD_API.Services
{
    public class SteamEventService : IHostedService
    {
        private Task _eventLoop;
        private CancellationTokenSource _cancelEventLoop;
        private readonly SteamService _steamService;
        private readonly ILogger<SteamEventService> _logger;

        public SteamEventService(
            SteamService steamService,
            ILogger<SteamEventService> logger
        )
        {
            _steamService = steamService;
            _logger = logger;
        }

        // utter cancer lol
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancelEventLoop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _steamService.Client.Connect();
            //eventLoop = RunEventLoop(cancelEventLoop.Token);
            _eventLoop = Task.Factory.StartNew(() =>
            {
                _logger.LogInformation("event-loop start!");
                while (!_cancelEventLoop.Token.IsCancellationRequested && _steamService.KeepAlive)
                    _steamService.Manager.RunWaitCallbacks();
            }, _cancelEventLoop.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return _eventLoop.IsCompleted ? _eventLoop : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping service...");
            if (_steamService.KeepAlive && _eventLoop != null)
            {
                _steamService.KeepAlive = false;
                _steamService.Connected = false;

                _cancelEventLoop.Cancel();
                await Task.WhenAny(_eventLoop, Task.Delay(-1, cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
