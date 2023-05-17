using Client.Scripts;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;
using System.Collections.Generic;
using Windows.Graphics.Capture;
using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.Graphics;
using Newtonsoft.Json;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Threading;
using Windows.UI.Core;

namespace Client
{
    public sealed partial class MainPage : Page
    {
        public static MainPage main;
        private WebContext context;
        private string ID;

        private CaptureScreen CaptureScreen;
        private ClientTCP ClientTCP;
        private KeyHandled KeyHandled;
        private Audio Audio;

        private InfoOnPC activPC;
        private Dictionary<InfoOnPC, Button> pcs;
        private Dictionary<Button, SizeInt32> dispays;

        public MainPage()
        {
            InitializeComponent();
            ContentPlace.Visibility = Visibility.Collapsed;
            AutorizePlace.Visibility = Visibility.Visible;
            RegistrPlace.Visibility = Visibility.Collapsed;

            main = this;

            pcs = new Dictionary<InfoOnPC, Button>();
            dispays = new Dictionary<Button, SizeInt32>();

            KeyHandled = new KeyHandled();
            context = new WebContext(typeof(Controler));
            ClientTCP = new ClientTCP(new Uri("ws://localhost:9999" + "/MassageService"), context);
            CaptureScreen = new CaptureScreen(this.box, ClientTCP);
            Controler.SetCaptureScreen(CaptureScreen);

            ClientTCP.Start();
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var accessResult = await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Programmatic);
            if (accessResult != Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus.Allowed)
            {
                Environment.Exit(0);
            }
        }

        private void Box_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            KeyHandled.KeyDown(e, ClientTCP.SendMassage);
        }

        private void Box_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            KeyHandled.KeyUp(e, ClientTCP.SendMassage);
        }

        private void Autorize(object sender, RoutedEventArgs e)
        {
            ClientTCP.TryAutorizeOrRegistr(Login.Text, Pass.Password, Commands.TryAuthorization);
        }

        private void Registr(object sender, RoutedEventArgs e)
        {
            bool status = true;

            if (LoginR.Text.Length > 20 || LoginR.Text.Length < 8)
            {
                Exception("Длинна логина должна быть\n в диапозоне от 8 до 20", false);
                status = false;
            }

            if (PassR1.Password != PassR2.Password)
            {
                Exception("Пароли должны совпадать", false);
                status = false;
            }

            if(PassR1.Password.Length > 20 || PassR1.Password.Length < 8)
            {
                Exception("Длинна пароля должна быть\n в диапозоне от 8 до 20", false);
                status = false;
            }

            if(status)
                ClientTCP.TryAutorizeOrRegistr(LoginR.Text, PassR1.Password, Commands.TryRegistr);
        }

        public void SetActivContent(string ID)
        {
            this.ID = ID;
            AutorizePlace.Visibility = Visibility.Collapsed;
            ContentPlace.Visibility = Visibility.Visible;
            ClientTCP.SendMassage(JsonConvert.SerializeObject(InfoOnPC.Create(ID)), Commands.InfoOnPC);
        }

        public void AddPc(InfoOnPC pc)
        {
            Button button = new Button();
            pcs.Add(pc, button);
            button.Click += ButtonConnectToPc;
            button.Style = new Style(typeof(Button));
            button.Content = "Компьютер:\n" + pc.Name;
            button.Margin = new Thickness(10);
            if(pc.ID == ID)
            {
                button.IsEnabled = false;
                button.Content += "\n(Текущий)";
            }    
            PCsContent.Children.Add(button);
        }

        private void ButtonConnectToPc(object sender, RoutedEventArgs e)
        {
            MonitorsContent.Children.Clear();
            dispays.Clear();
            int i = 0;
            activPC = pcs.First(x => x.Value == (Button)sender).Key;
            foreach (var a in activPC.Displays)
            {
                Button button = new Button();
                button.Name = i + "";
                button.Click += ButtonConnectToMonitor;
                button.Style = new Style(typeof(Button));
                button.Content = "Монитор " + ++i + " :\n" + a.Width + ":" + a.Height;
                button.Margin = new Thickness(10);
                dispays.Add(button, a);
                MonitorsContent.Children.Add(button);
            }
        }

        private void ButtonConnectToMonitor(object sender, RoutedEventArgs e)
        {
            CaptureScreen.SetSize(dispays[(Button)sender]);
            ClientTCP.SendMassage(activPC.ID, Commands.Connect);
            ClientTCP.SendMassage(((Button)sender).Name, Commands.StartVideo);
        }

        public void RemovePC(string ID)
        {
            if (activPC != null && activPC.ID == ID)
                MonitorsContent.Children.Clear();
            pcs.Remove(pcs.Keys.First(x => x.ID == ID), out Button button);
            PCsContent.Children.Remove(button);
        }

        private void ListView_PointerEntered(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ((UIElement)sender).Opacity = 1;
        }

        private void ListView_PointerExited(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            ((UIElement)sender).Opacity = 0;
        }

        private void full_Click(object sender, RoutedEventArgs e)
        {
            var view = ApplicationView.GetForCurrentView();
            if (view.IsFullScreenMode)
            {
                view.ExitFullScreenMode();
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto;
                grid.RowDefinitions[0].Height = new GridLength(100, GridUnitType.Pixel);
                grid.ColumnDefinitions[1].Width = new GridLength(200, GridUnitType.Pixel);
                box.Margin = new Thickness(10);
                ((Button)sender).Content = "Переключить в полноэкранный режим";
            }
            else
            {
                if (view.TryEnterFullScreenMode())
                {
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.FullScreen;
                    grid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
                    grid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                    box.Margin = new Thickness(0);
                    ((Button)sender).Content = "Выйти из полноэкранного режима";
                }
            }
        }

        public void Swap(object sender, RoutedEventArgs e)
        {
            if(AutorizePlace.Visibility == Visibility.Visible)
            {
                AutorizePlace.Visibility = Visibility.Collapsed;
                RegistrPlace.Visibility = Visibility.Visible;
            }
            else if(ContentPlace.Visibility == Visibility.Visible)
            {
                ClientTCP.SendMassage(ID, Commands.DeletePC);
                pcs.Clear();
                dispays.Clear();
                PCsContent.Children.Clear();
                MonitorsContent.Children.Clear();
                box = new Windows.UI.Xaml.Shapes.Rectangle();
                AutorizePlace.Visibility = Visibility.Visible;
                RegistrPlace.Visibility = Visibility.Collapsed;
                ContentPlace.Visibility = Visibility.Collapsed;
            }
            else if (RegistrPlace.Visibility == Visibility.Visible)
            {
                AutorizePlace.Visibility = Visibility.Visible;
                RegistrPlace.Visibility = Visibility.Collapsed;
            }
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            ClientTCP.SendMassage("", Commands.Stop);
            box = new Windows.UI.Xaml.Shapes.Rectangle();
        }

        public void Exception(string data, bool status)
        {
            Border border = new Border();
            if(status)
                border.Background = new SolidColorBrush(Colors.Green);
            else
                border.Background = new SolidColorBrush(Colors.Red);
            TextBlock message = new TextBlock();
            message.Text = data;
            message.Width = 270;
            message.Margin = new Thickness(10);
            border.Margin = new Thickness(10);
            border.Child = message;
            border.Width = 270;
            border.Opacity = 0.7;
            border.BorderThickness = new Thickness(2);
            border.CornerRadius = new CornerRadius(10);
            Exceptions.Children.Add(border);
            Task.Run(() =>
            {
                Thread.Sleep(5000);
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Exceptions.Children.Remove(border));
            });
        }
    }
}

