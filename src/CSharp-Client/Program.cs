using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using SocialWeather.Protobuf;

namespace SocialWeather
{
    public class Program
    {
        private const string Url = "ws://localhost:5000/weather?formatType=protobuf";

        public static async Task Main(string[] args)
        {
            await new Program().Run();
        }

        private Random rand = new Random();
        private string[] ZipCodes = { "98052", "98034", "98007", "98074" };

        private async Task Run()
        {
            var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri(Url), CancellationToken.None);
            await Console.Out.WriteLineAsync("Connected to Social Weather");

            await Task.WhenAll(Receive(ws), HandleInput(ws));
        }

        private async Task Receive(ClientWebSocket ws)
        {
            var buffer = new byte[2048];

            while (true)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
                else
                {
                    var inputStream = new CodedInputStream(buffer);
                    var weatherReport = new Protobuf.WeatherReport();
                    inputStream.ReadMessage(weatherReport);
                    await Console.Out.WriteLineAsync("Weather report received");
                    await Console.Out.WriteLineAsync($"Temperature: {weatherReport.Temperature}");
                    await Console.Out.WriteLineAsync($"Last updated: {weatherReport.ReportTime}");
                    await Console.Out.WriteLineAsync($"Weather: {weatherReport.Weather.ToString()}");
                    await Console.Out.WriteLineAsync($"ZipCode: {weatherReport.ZipCode}" + Environment.NewLine);
                }
            }
        }

        private async Task HandleInput(ClientWebSocket ws)
        {
            string line;
            while ((line = await Console.In.ReadLineAsync()) != ":q!")
            {
                await SendReport(ws);
            }

            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        private async Task SendReport(ClientWebSocket ws)
        {
            var protoWeatherReport = new WeatherReport
            {
                Temperature = 15 + (rand.Next() % 70),
                ReportTime = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Weather = WeatherReport.Types.WeatherKind.Sunny,
                ZipCode = ZipCodes[rand.Next() % 4]
            };

            using (var stream = new MemoryStream())
            {
                var outputStream = new CodedOutputStream(stream, leaveOpen: true);
                outputStream.WriteMessage(protoWeatherReport);
                outputStream.Flush();
                // var buffer = new ArraySegment<byte>(new byte[stream.Position]);
                stream.Position = 0;

                ArraySegment<byte> buffer;
                var t = stream.TryGetBuffer(out buffer);
                await ws.SendAsync(buffer, WebSocketMessageType.Binary, /*endOfMessage*/ true, CancellationToken.None);
            }
        }
    }
}
