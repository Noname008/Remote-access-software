using System;
using System.IO;
using Windows.UI.Core;
using Newtonsoft.Json;

namespace Client.Scripts
{
    internal class Controler
    {
        private KeyHandled keyHandled;
        private static CaptureScreen Capture;

        public Controler()
        {
            keyHandled = new KeyHandled();
        }

        public static void SetCaptureScreen(CaptureScreen capture)
        {
            Capture = capture;
        }

        [Method(Commands.Stop)]
        public void Stop(string data)
        {
            Capture.StopCapture();
        }

        [Method(Commands.InfoOnPC)]
        public void InfoPC(string data)
        {
            InvokeToPage(() => MainPage.main.AddPc(JsonConvert.DeserializeObject<InfoOnPC>(data)));
        }

        [Method(Commands.VideoData)]
        public void Video(string data)
        {
            if(data != null)
            {
                Capture.Buffer = new MemoryStream(Convert.FromBase64String(data));
            }
        }

        [Method(Commands.StartVideo)]
        public void StartVideoAsync(string data)
        {
            InvokeToPage(() => Capture.StartCapture(Int32.Parse(data)));
        }

        [Method(Commands.TryAuthorization)]
        public void TryAuthorization(string id)
        {
            if (id != "")
            {
                InvokeToPage(() => MainPage.main.SetActivContent(id));
            }
            else
            {
                InvokeToPage(() => MainPage.main.Exception("Неправильный логин или пароль", false));
            }
        }

        [Method(Commands.TryRegistr)]
        public void TryRegistr(string data)
        {
            if (Boolean.Parse(data))
            {
                InvokeToPage(() => 
                {
                    MainPage.main.Swap(null, null);
                    MainPage.main.Exception("Вы успешно зарегистрировались", true);
                });
            }
            else
            {
                InvokeToPage(() => MainPage.main.Exception("Такой пользователь уже существует", false));
            }
        }

        [Method(Commands.DeletePC)]
        public void DeletePC(string id)
        {
            InvokeToPage(() => MainPage.main.RemovePC(id));
        }

        private void InvokeToPage(Action action)
        {
            _ = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                action();
            });
        }
    }
}
