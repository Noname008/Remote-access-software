using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Server.Scripts
{
    internal class MassageService : WebSocketBehavior
    {
        public static Dictionary<string, string> Connections = new();

        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.IsText)
            {
                JSData data = JsonConvert.DeserializeObject<JSData>(e.Data)!;

                if (PC.PCs.ContainsKey(ID))
                {
                    switch (data.Command)
                    {
                        case Commands.VideoData or Commands.AudioData or Commands.StartVideo or Commands.StartAudio or Commands.Key or Commands.Stop:
                            Sessions.SendTo(e.Data, Connections[ID]);
                            break;
                        case Commands.InfoOnPC:
                            PC.PCs[ID].SetInfo(e.Data);
                            foreach(PC p in User.Users[PC.PCs[ID].GetUser()])
                            {
                                Sessions.SendTo(e.Data, p.ID);
                                if (ID != p.ID) Sessions.SendTo(p.GetInfo(), ID);
                            }
                            break;
                        case Commands.Connect:
                            Disconect(ID);
                            Connections.Add(ID, data.Data);
                            Connections.Add(data.Data, ID);
                            break;
                        case Commands.DeletePC:
                            DeletePC(ID);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (data.Command)
                    {
                        case Commands.TryAuthorization:
                            Send(Commands.TryAuthorization, ID, CheckAuthorization(data.Data) ? ID : "");
                            break;
                        case Commands.TryRegistr:
                            Send(Commands.TryRegistr, ID, CheckDataBase(data.Data).ToString());
                            break;
                    }
                }
                
            }
            base.OnMessage(e);
        }

        private void DeletePC(string ID)
        {
            Disconect(ID);
            foreach (PC p in from p in User.Users[PC.PCs[ID].GetUser()] 
                             where p.ID != ID
                             select p)
            {
                Send(Commands.DeletePC, p.ID, ID);
            }
            PC.Dispose(ID);
        }

        private void Send(Commands command, string ID, string Data)
        {
            Sessions.SendTo(
                JsonConvert.SerializeObject(
                    new JSData(
                        command,
                        Data)),
                ID);
        }

        private void Disconect(string ID)
        {
            try
            {
                var s = Connections.Where(x => x.Key == ID || x.Value == ID);
                if (s.Any())
                    foreach (var c in s)
                    {
                        Sessions.SendTo(
                        JsonConvert.SerializeObject(
                        new JSData(Commands.Stop, "")), c.Key == ID ? c.Value : c.Key);
                        Connections.Remove(c.Key);
                    }
            }
            finally
            {
                Console.WriteLine("deleted " + ID);
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
        }

        private bool CheckAuthorization(string data)
        {
            string[] logData = JsonConvert.DeserializeObject<string[]>(data)!;
            return User.CreateUser(logData[0], logData[1], ID);
        }

        private bool CheckDataBase(string data)
        {
            string[] logData = JsonConvert.DeserializeObject<string[]>(data)!;
            return User.CreateUserToDatabase(logData[0], logData[1]);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("Close con:" + e.ToString());
            DeletePC(ID);
            base.OnClose(e);
        }
    }
}
