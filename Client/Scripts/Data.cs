using System;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.Display;

namespace Client.Scripts
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

    public class InfoOnPC
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public SizeInt32[] Displays { get; set; }

        public InfoOnPC(string ID, string Name, SizeInt32[] Displays)
        {
            this.ID = ID;
            this.Name = Name;
            this.Displays = Displays;
        }

        public static InfoOnPC Create(string ID)
        {
            DisplayId[] iddisplays = DisplayServices.FindAll();
            SizeInt32[] displays = new SizeInt32[iddisplays.Length];
            for (int i = 0; i < iddisplays.Length; i++)
            {
                displays[i] = GraphicsCaptureItem.TryCreateFromDisplayId(iddisplays[i]).Size;
            }
            return new InfoOnPC(ID, Environment.MachineName, displays);
        }
    }
}
