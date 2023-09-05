using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using CMS.Core;
using Kentico.Xperience.UMT.Auxiliary;
using Kentico.Xperience.UMT.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.UMT;

public static class ServiceCollectionExtensions
{
    private const string SOURCE = "UMT.Example";

    public static void AddUmtSample(this IServiceCollection services) => services.AddUniversalMigrationToolkit();


    public static void UseUmtSample(this IApplicationBuilder app)
    {
        app.UseWebSockets();

        app.Use(async (HttpContext context, RequestDelegate next) =>
        {
            if (context.Request.Path == "/umtsample/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var importService = context.RequestServices.GetRequiredService<IImportService>();
                    var eventLogService = context.RequestServices.GetRequiredService<IEventLogService>();

                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

                    await DownloadAndImport(webSocket, importService, eventLogService);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            else
            {
                await next(context);
            }
        });
    }

    private sealed record Message(string Type, HeaderPayload? Payload);

    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed record HeaderPayload();

    private static async Task DownloadAndImport(WebSocket webSocket, IImportService importService, IEventLogService logService)
    {
        async Task SendProgressReport(string message)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "msg", payload = $"{message}" }));
                await webSocket.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
        
        async Task SendStats(IDictionary<string, int> stats)
        {
            if (webSocket.State == WebSocketState.Open)
            {
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "stats", payload = System.Text.Json.JsonSerializer.Serialize(stats) }));
                await webSocket.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        async Task SendTooFastReport()
        {
            if (webSocket.State == WebSocketState.Open)
            {
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "toofast", payload = $"" }));
                await webSocket.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        async Task SendProgressFinished()
        {
            if (webSocket.State == WebSocketState.Open)
            {
                byte[] payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "finished", payload = $"" }));
                await webSocket.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        async Task SendConfirmHeader()
        {
            if (webSocket.State == WebSocketState.Open)
            {
                byte[] msg = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "headerConfirmed", payload = "" }));
                await webSocket.SendAsync(new ArraySegment<byte>(msg, 0, msg.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        // indicates import start
        _ = (await ReceiveHeader(webSocket))?.ToObject<Message>()?.Payload;

        await SendConfirmHeader();

        bool consumerIsRunning = true;
        var ms = new AsynchronousStream(1024 * 32 * 500);
        var consumerTask = Task.Run(async () =>
        {
            try
            {
                var data = importService.FromJsonStream(ms);
                var observer = new ImportStateObserver();

                var stats = new ConcurrentDictionary<string, int>();
                
                observer = await importService.StartImportAsync(data!, new ImporterContext("Boilerplate", "en-US"), observer);
                observer.ImportedInfo += async info =>
                {
                    await SendProgressReport($"Processed: {info.TypeInfo.ObjectType} {info.GetValue(info.TypeInfo.GUIDColumn)}");
                    stats.AddOrUpdate(info.TypeInfo.ObjectType, s => 1, (s, i) => i + 1);
                };
                observer.ValidationError += async (model, id, validationResults) =>
                {
                    string validationMessage = string.Join("\r\n", validationResults.Select(x => $"{string.Join(", ", x.MemberNames)}: {x.ErrorMessage}"));
                    await SendProgressReport($"Validation error: {id} {validationMessage}");
                };
                observer.Exception += async (model, id, exception) =>
                {
                    await SendProgressReport($"Error: {id} {exception}");
                };

                await observer.ImportCompletedTask;

                await SendStats(stats);
                await SendProgressReport($"...finished");
            }
            finally
            {
                consumerIsRunning = false;
            }
        });

        var producerTask = Task.Run(async () =>
        {
            WebSocketReceiveResult? receiveResult = null;
            int bufferSize = 1024 * 32;
            while (true)
            {
                if (!consumerIsRunning)
                {
                    break;
                }

                byte[] buffer = new byte[bufferSize];
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (!receiveResult.CloseStatus.HasValue)
                {
                    var data = new ArraySegment<byte>(buffer);

                    if (buffer[0] == 0x2D && buffer.Take(5).All(x => x.Equals(0X2D)))
                    {
                        ms.Flush();
                        break;    
                    }
                    
                    ms.Write(data.Array!, data.Offset, receiveResult.Count);
                    ms.Flush();

                    int count = receiveResult.Count;
                    byte[] response = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { type = "progress", payload = count }));
                    await webSocket.SendAsync(new ArraySegment<byte>(response, 0, response.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                    if (ms.CachedBlocks > 3500)
                    {
                        await SendTooFastReport();
                        await SendProgressReport($"Too fast, waiting 3s CachedBlocks: {ms.CachedBlocks}");
                        await Task.Delay(3000);
                    }
                }
                else
                {
                    break;
                }
            }
        });

        bool socketAvailable = true;
        try
        {
            await producerTask;
        }
        catch (SocketException se)
        {
            socketAvailable = false;
            logService.LogException(SOURCE, "PRODUCER", se);
        }
        catch (Exception e)
        {
            logService.LogException(SOURCE, "PRODUCER", e);
            await SendProgressReport($"{e}");
        }
        finally
        {
            ms.TryCompleteWriting();
        }

        try
        {
            await consumerTask;
        }
        catch (SocketException se)
        {
            logService.LogException(SOURCE, "CONSUMER", se);
            socketAvailable = false;
        }
        catch (Exception e)
        {
            logService.LogException(SOURCE, "CONSUMER", e);
            await SendProgressReport($"{e}");
        }

        if (socketAvailable)
        {
            await SendProgressFinished();
            await Task.Delay(1000);
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Standard closing",
                CancellationToken.None);
        }
    }

    private static async Task<JObject?> ReceiveHeader(WebSocket webSocket)
    {
        var ms = new MemoryStream();
        const int bufferSize = 1024 * 32;
        while (true)
        {
            byte[] buffer = new byte[bufferSize];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (!receiveResult.CloseStatus.HasValue)
            {
                var data = new ArraySegment<byte>(buffer);

                ms.Write(data.Array!, data.Offset, receiveResult.Count);
                ms.Flush();
            }
            else
            {
                break;
            }

            if (receiveResult.EndOfMessage)
            {
                break;
            }
        }

        ms.Seek(0, SeekOrigin.Begin);

        using var sr = new StreamReader(ms);
        string msg = await sr.ReadToEndAsync();
        var deserialized = JObject.Parse(msg);
        return deserialized;
    }
}
