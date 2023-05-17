using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Scripts
{
    public enum Commands
    {
        TryAuthorization,
        TryRegistr,
        InfoOnPC,
        DeletePC,

        Connect,
        StartVideo,
        StartAudio,
        Stop,

        VideoData,
        AudioData,
        Key,
    }

    internal class JSData
    {
        public Commands Command { get; set; }
        public string Data { get; set; }

        public JSData(Commands command, string jSData)
        {
            Command = command;
            Data = jSData;
        }
    }
}
