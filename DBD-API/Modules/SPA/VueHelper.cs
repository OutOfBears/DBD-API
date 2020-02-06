using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DBD_API.Modules.SPA
{
    public static class VueHelper
    {

        private static int Port { get; } = 8080;
        private static Uri DevelopmentServerEndpoint { get; } = new Uri($"http://localhost:{Port}");
        private static TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);

        private static string DoneMessage { get; } = "DONE  Compiled successfully in";

        public static void UseVueDevelopmentServer(this ISpaBuilder spa)
        {
            spa.UseProxyToSpaDevelopmentServer(async () =>
            {
                var loggerFactory = spa.ApplicationBuilder.ApplicationServices.GetService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("Vue");

                if (IsRunning())
                {
                    return DevelopmentServerEndpoint;
                }

                // launch vue.js development server
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                var processInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "cmd" : "npm",
                    Arguments = $"{(isWindows ? "/c npm " : "")}run serve",
                    WorkingDirectory = "ClientApp",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };
                var process = Process.Start(processInfo);
                var tcs = new TaskCompletionSource<int>();
                _ = Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            logger.LogInformation(line);
                            if (!tcs.Task.IsCompleted && line.Contains(DoneMessage))
                            {
                                tcs.SetResult(1);
                            }
                        }
                    }
                    catch (EndOfStreamException ex)
                    {
                        logger.LogError(ex.ToString());
                        tcs.SetException(new InvalidOperationException("'npm run serve' failed.", ex));
                    }
                });
                _ = Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while ((line = process.StandardError.ReadLine()) != null)
                        {
                            logger.LogError(line);
                        }
                    }
                    catch (EndOfStreamException ex)
                    {
                        logger.LogError(ex.ToString());
                        tcs.SetException(new InvalidOperationException("'npm run serve' failed.", ex));
                    }
                });

                var timeout = Task.Delay(Timeout);
                if (await Task.WhenAny(timeout, tcs.Task) == timeout)
                {
                    throw new TimeoutException();
                }

                return DevelopmentServerEndpoint;
            });

        }

        private static bool IsRunning() => IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Select(x => x.Port)
                .Contains(Port);
    }
}
