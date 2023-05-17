using System;
using System.Security.Cryptography;
using WebSocketSharp;
using Newtonsoft.Json;
using System.Text;

namespace Client.Scripts
{
    internal class ClientTCP
    {
        private WebSocket WebSocket;
        private readonly Uri _uri;
        private static WebContext context;

        public ClientTCP(Uri uri, WebContext context)
        {
            ClientTCP.context = context;
            _uri = uri;
        }

        public void TryAutorizeOrRegistr(string log, string pass, Commands command)
        {
            HashAlgorithm sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(pass));
            string hash = BitConverter
                .ToString(hashBytes)
                .Replace("-", String.Empty);

            SendMassage(JsonConvert.SerializeObject(new string[] { log, hash }), command);
        }

        public void Start()
        {
            OnNavigatedTo();
        }

        private bool OnNavigatedTo()
        {
            try
            {
                WebSocket = new WebSocket(Convert.ToString(_uri));
                WebSocket.OnMessage += (s, e) => { WebSocket_MessageReceivedAsync(s, e); };
                WebSocket.OnClose += (s, e) => { WebSocket_Close(s, e); };
                WebSocket.Connect();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public void SendMassage(JSData data)
        {
            try
            {
                WebSocket.Send(JsonConvert.SerializeObject(data));
            }
            catch (Exception ex)
            {
                Start();
                System.Diagnostics.Debug.WriteLine("Error send massage:" + ex.Message);
            }
        }

        public void SendMassage(string message, Commands command)
        {
            SendMassage(new JSData(command, message));
        }

        private void WebSocket_MessageReceivedAsync(Object sender, MessageEventArgs args)
        {
            JSData data = JsonConvert.DeserializeObject<JSData>(args.Data);
            ClientTCP.context.Invoke(data.Command, data.Data);
        }

        public void WebSocket_Close(Object sender, CloseEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine(args.Code);
        }
    }
}
