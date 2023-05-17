using WebSocketSharp.Server;
using Server.Scripts;

WebSocketServer server = new WebSocketServer("ws://localhost:9999");
//server.SslConfiguration.ServerCertificate = Certificate2.GenerateSelfSignedCertificate();
server.AddWebSocketService<MassageService>("/MassageService");
server.WaitTime = TimeSpan.FromSeconds(10);
server.Start();

Console.ReadLine();

server.Stop();