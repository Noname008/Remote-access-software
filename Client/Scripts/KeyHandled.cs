using System;
using System.Linq;
using Windows.UI.Input.Preview.Injection;

namespace Client.Scripts
{
    internal class KeyHandled
    {
        private static bool[] handlMap;

        public KeyHandled()
        {
            handlMap = new bool[ushort.MaxValue];
            handlMap.All(handlMap => handlMap = false);
        }

        public void KeyDown(Windows.UI.Xaml.Input.KeyRoutedEventArgs e, Action<string, Commands> delegat)
        {
            if (!handlMap[(ushort)e.Key])
            {
                handlMap[(ushort)e.Key] = true;
                delegat.Invoke(((ushort)e.Key).ToString(), Commands.Key);
            }
        }

        public void KeyUp(Windows.UI.Xaml.Input.KeyRoutedEventArgs e, Action<string, Commands> delegat)
        {
            if (handlMap[(ushort)e.Key])
            {
                handlMap[(ushort)e.Key] = false;
                delegat.Invoke(((ushort)e.Key).ToString(), Commands.Key);
            }
        }

        public void OutputKeyDown(ushort ScanCode)
        {
            InputInjector inputInjector = InputInjector.TryCreate();
            var info = new InjectedInputKeyboardInfo { VirtualKey = ScanCode };
            inputInjector.InjectKeyboardInput(new[] { info });
        }

        public void OutputKeyUp(Windows.UI.Xaml.Input.KeyRoutedEventArgs e, Action<string> delegat)
        {
            if (handlMap[e.KeyStatus.ScanCode])
            {
                handlMap[e.KeyStatus.ScanCode] = false;
                delegat.Invoke("test/" + e.KeyStatus.ScanCode.ToString());
            }
        }
    }
}