using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace DBD_API.Services
{
    public class SteamEventService : IHostedService
    {
        private Task _eventLoop;
        private CancellationTokenSource _cancelEventLoop;
        private readonly SteamService _steamService;

        public SteamEventService(
            SteamService steamService
        )
        {
            _steamService = steamService;
        }

        // utter cancer lol
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancelEventLoop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _steamService.Client.Connect();
            //eventLoop = RunEventLoop(cancelEventLoop.Token);
            _eventLoop = Task.Factory.StartNew(() =>
            {
                Console.WriteLine("[Steam] event-loop start!");
                while (!_cancelEventLoop.Token.IsCancellationRequested && _steamService.KeepAlive)
                    _steamService.Manager.RunWaitCallbacks();
            }, _cancelEventLoop.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return _eventLoop.IsCompleted ? _eventLoop : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Stopping service...");
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
