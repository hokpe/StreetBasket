using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.System.Display;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace StreetBasket
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static int RoundLenght = 15; // 15 min
        static int TimeoutLenght = 30; // 30 sec
        static int MAIN_TIMER = 60 * RoundLenght; // 15 min
        static int TIMEOUT_TIMER = TimeoutLenght;  // 30 sec
        private int[] DifferentTimeouts = { 20, 30, 60 };
        private enum TIMEOUT_TYPE { none, home, away };
        private Color[] colors; // declare numbers as an int array of any size

        private DispatcherTimer mainTimer;
        private DispatcherTimer timeoutTimer;
        private DispatcherTimer RandomAnimationTimer;

        int timesTicked = 1;
        int timesToTick = MAIN_TIMER;
        int timeoutTicked = 1;
        int timeoutToTick = TIMEOUT_TIMER;
        int randomCounter = 50;
        int HomeColor = 0;
        int AwayColor = 1;
        Boolean running = false;
        private Audio audio = new Audio();
        string rules = null;
        private bool PageEnabled;
        private bool ConfigMode;
        DisplayRequest g_DisplayRequest;

        private TIMEOUT_TYPE timeout { get; set; }

        public MainPage()
        {
            this.InitializeComponent();
            colors = new Color[] { Colors.White, Colors.Black, Colors.Red, Colors.Green, Colors.Blue, Colors.DarkOrchid };
            mainTimer = new DispatcherTimer();
            mainTimer.Tick += mainTimer_Tick;
            mainTimer.Interval = new TimeSpan(0, 0, 1);
            timeoutTimer = new DispatcherTimer();
            timeoutTimer.Tick += timeoutTimer_Tick;
            timeoutTimer.Interval = new TimeSpan(0, 0, 1);
            RandomAnimationTimer = new DispatcherTimer();
            RandomAnimationTimer.Tick += RandomTimer_Tick;
            RandomAnimationTimer.Interval = TimeSpan.FromMilliseconds(50);
            audio.AudioCreate();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            MainPage rootPage = e.Parameter as MainPage;
            ResetValues();
            ReadRulesFromFile();
            //setLogo();
            g_DisplayRequest = new DisplayRequest();
        }
        private async void setLogo()
        {
            bool CustomLogoUsed = false;
            try
            {
                string s = "logo.jpg";
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.GetFileAsync(s);
                if (file != null)
                {
                    var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var image = new BitmapImage();
                    image.SetSource(stream);
                    Assets_StreetBasket_jpg.Source = image;
                    CustomLogoUsed = true;
                }
            }
            catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.InvalidOperationException) { }

            if (!CustomLogoUsed)
            {
                Assets_StreetBasket_jpg.Source = new BitmapImage(new Uri("ms-appx:///Assets/StreetBasket.jpg"));
            }
        }

        async void mainTimer_Tick(object sender, object e)
        {
            int t = timesToTick - timesTicked;

            textBlock_Time.Text = getTimeString(t);
            if (timesTicked >= timesToTick)
            {
                mainTimer.Stop();
                audio.PlayAudio();
                await Task.Delay(TimeSpan.FromSeconds(2));
                audio.StopAudio();
                TextBlock_StartStop.Text = "Reset";
                try
                {
                    g_DisplayRequest.RequestRelease();
                }
                catch (Exception ex)
                {
                    Task task = new MessageDialog("DisplayRequest.RequestRelease failed " + ex.Message).ShowAsync().AsTask();
                }
            }
            timesTicked++;
        }

        async void timeoutTimer_Tick(object sender, object e)
        {
            int t = timeoutToTick - timeoutTicked;

            if (timeout == TIMEOUT_TYPE.away)
                textBlock_AwayTimeoutTime.Text = getTimeString(t);
            else if (timeout == TIMEOUT_TYPE.home)
                textBlock_HomeTimeoutTime.Text = getTimeString(t);
            if (timeoutTicked >= timeoutToTick)
            {
                audio.PlayAudio();
                await Task.Delay(TimeSpan.FromSeconds(2));
                audio.StopAudio();
                timeoutTimer.Stop();
                timeout = TIMEOUT_TYPE.none;
                timeoutTicked = 1;
                // Make a sound
            }
            timeoutTicked++;
        }


        private void RandomTimer_Tick(object sender, object e)
        {
            if (randomCounter > 0) randomCounter--;
            if (BlackArrow.Visibility == Visibility.Visible || WhiteArrow.Visibility == Visibility.Visible)
            {
                BlackArrow.Visibility = Visibility.Collapsed;
                WhiteArrow.Visibility = Visibility.Collapsed;
            }
            else
            {
                uint random = CryptographicBuffer.GenerateRandomNumber();
                if (random % 2 == 0)
                {
                    BlackArrow.Visibility = Visibility.Collapsed;
                    WhiteArrow.Visibility = Visibility.Visible;
                }
                else
                {
                    BlackArrow.Visibility = Visibility.Visible;
                    WhiteArrow.Visibility = Visibility.Collapsed;
                }
                if (randomCounter == 0)
                {
                    RandomAnimationTimer.Stop();
                }
            }
        }

        private void ResetValues()
        {
            timesTicked = 1;
            timesToTick = MAIN_TIMER; // 900 = 15 min
            timeoutTicked = 1;
            timeoutToTick = TIMEOUT_TIMER; // 30 = 30 sec
            textBlock_Time.Text = getTimeString(timesToTick);
            textBlock_AwayScore.Text = "0";
            textBlock_HomeScore.Text = "0";
            textBlock_AwayTimeoutTime.Text = getTimeString(timeoutToTick);
            textBlock_HomeTimeoutTime.Text = getTimeString(timeoutToTick);
            BlackArrow.Visibility = Visibility.Collapsed;
            WhiteArrow.Visibility = Visibility.Collapsed;
            HideEditor();
            TextBlock_StartStop.Text = "Start";
            running = false;
        }

        private string getTimeString(int Time)
        {
            string s;

            int min, sec;
            min = Time / 60;
            sec = Time - (min * 60);

            s = min.ToString() + ":" + (sec < 10 ? "0" : "") + sec.ToString();

            return s;
        }

        private void AwayPlusClick(object sender, RoutedEventArgs e)
        {
            if (timeout == TIMEOUT_TYPE.none)
            {
                int i = int.Parse(textBlock_AwayScore.Text);
                i++;
                textBlock_AwayScore.Text = i.ToString();
            }
        }

        private void AwayMinusClick(object sender, RoutedEventArgs e)
        {
            if (timeout == TIMEOUT_TYPE.none)
            {
                int i = int.Parse(textBlock_AwayScore.Text);
                if (i >= 1) i--;
                textBlock_AwayScore.Text = i.ToString();
            }
        }

        private void HomePlusClick(object sender, RoutedEventArgs e)
        {
            if (timeout == TIMEOUT_TYPE.none)
            {
                int i = int.Parse(textBlock_HomeScore.Text);
                i++;
                textBlock_HomeScore.Text = i.ToString();
            }
        }

        private void HomeMinusClick(object sender, RoutedEventArgs e)
        {
            if (timeout == TIMEOUT_TYPE.none)
            {
                int i = int.Parse(textBlock_HomeScore.Text);
                if (i >= 1) i--;
                textBlock_HomeScore.Text = i.ToString();
            }
        }

        private void ChangeTimeoutLength()
        {
            for (int i = 0; i < DifferentTimeouts.Length; i++)
            {
                if (TimeoutLenght == DifferentTimeouts[i])
                {
                    if (i == DifferentTimeouts.Length - 1)
                    {
                        TimeoutLenght = DifferentTimeouts[0];
                    }
                    else
                    {
                        TimeoutLenght = DifferentTimeouts[i + 1];
                    }
                    break;
                }
            }
            TIMEOUT_TIMER = TimeoutLenght;
            timeoutToTick = TIMEOUT_TIMER;
            textBlock_AwayTimeoutTime.Text = getTimeString(timeoutToTick);
            textBlock_HomeTimeoutTime.Text = getTimeString(timeoutToTick);
        }
    
        private void AwayTimeoutTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ConfigMode)
            {
                ChangeTimeoutLength();
            }
            else if (!running && timeout != TIMEOUT_TYPE.home && PageEnabled)
            {
                if (timeout == TIMEOUT_TYPE.none && textBlock_AwayTimeoutTime.Text != "0:00")
                {
                    timeout = TIMEOUT_TYPE.away;
                    timeoutTimer.Start();
                }
                else
                {
                    if (textBlock_AwayTimeoutTime.Text != "0:00")
                    {
                        timeoutTimer.Stop();
                    }
                    timeoutTicked = 1;
                    textBlock_AwayTimeoutTime.Text = getTimeString(timeoutToTick);
                    timeout = TIMEOUT_TYPE.none;
                }
            }
        }

        private void HomeTimeoutTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ConfigMode)
            {
                ChangeTimeoutLength();
            }
            else if (!running && timeout != TIMEOUT_TYPE.away && PageEnabled)
            {
                if (timeout == TIMEOUT_TYPE.none && textBlock_HomeTimeoutTime.Text != "0:00")
                {
                    timeout = TIMEOUT_TYPE.home;
                    timeoutTimer.Start();
                }
                else
                {
                    if (textBlock_HomeTimeoutTime.Text != "0:00")
                    {
                        timeoutTimer.Stop();
                    }
                    timeoutTicked = 1;
                    textBlock_HomeTimeoutTime.Text = getTimeString(timeoutToTick);
                    timeout = TIMEOUT_TYPE.none;
                }
            }
        }

        private void StartStopHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (PageEnabled)
            {
                if (!running && timeout == TIMEOUT_TYPE.none)
                {
                    ResetValues();
                }
            }
        }

        private void StartStopHolding(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (PageEnabled)
            {
                if (!running && timeout == TIMEOUT_TYPE.none)
                {
                    ResetValues();
                }
            }
        }

        private void StartStopClick(object sender, TappedRoutedEventArgs e)
        {
            if (PageEnabled)
            {
                BlackArrow.Visibility = Visibility.Collapsed;
                WhiteArrow.Visibility = Visibility.Collapsed;
                if (timeout == TIMEOUT_TYPE.none)
                {
                    if (TextBlock_StartStop.Text == "Reset")
                    {
                        ResetValues();
                    }
                    else
                    {
                        running = !running;
                        if (running)
                        {
                            TextBlock_StartStop.Text = "Stop";
                            mainTimer.Start();
                            try
                            {
                                g_DisplayRequest.RequestActive();
                            }
                            catch (Exception ex)
                            {
                                Task task = new MessageDialog("DisplayRequest.RequestActive failed " + ex.Message).ShowAsync().AsTask();
                            }
                        }
                        else
                        {
                            TextBlock_StartStop.Text = "Start";
                            mainTimer.Stop();
                            try
                            {
                                g_DisplayRequest.RequestRelease();
                            }
                            catch (Exception ex)
                            {
                                Task task = new MessageDialog("DisplayRequest.RequestRelease failed " + ex.Message).ShowAsync().AsTask();
                            }
                        }
                    }
                }
            }
        }

        private void RulesTapped(object sender, TappedRoutedEventArgs e)
        {
            if (rules != null && PageEnabled)
            {
                /* Asynkroninen operaatio ilman varoituksia. */
                Task task = new MessageDialog(rules).ShowAsync().AsTask();
            }
        }

        private void TimeTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ConfigMode)
            {
                RoundLenght++; if (RoundLenght >= 21) RoundLenght = 5; 
                MAIN_TIMER = 60 * RoundLenght;
                timesToTick = MAIN_TIMER;
                textBlock_Time.Text = getTimeString(timesToTick);
            }
            else if (PageEnabled)
            {
                if (BlackArrow.Visibility == Visibility.Visible || WhiteArrow.Visibility == Visibility.Visible)
                {
                    BlackArrow.Visibility = Visibility.Collapsed;
                    WhiteArrow.Visibility = Visibility.Collapsed;
                }
                else if (timeout == TIMEOUT_TYPE.none && running == false)
                {
                    randomCounter = 50;
                    RandomAnimationTimer.Start();
                }
            }
        }

        private void HomeScoreTapped(object sender, TappedRoutedEventArgs e)
        {
            if (timeout == TIMEOUT_TYPE.none && running == false && PageEnabled)
            {
                HomeColor++; if (HomeColor >= colors.Length) HomeColor = 0;
                textBlock_HomeScore.Foreground = new SolidColorBrush(colors[HomeColor]);
            }
        }

        private void AwayScoreTapped(object sender, TappedRoutedEventArgs e)
        {
            if (ConfigMode)
            {
                for(int i = 0; i < DifferentTimeouts.Length; i++)
                {
                    if(TimeoutLenght == DifferentTimeouts[i])
                    {
                        if(i == DifferentTimeouts.Length - 1)
                        {
                            TimeoutLenght = DifferentTimeouts[0];
                        }
                        else
                        {
                            TimeoutLenght = DifferentTimeouts[i+1];
                        }
                    }
                }
                TIMEOUT_TIMER = TimeoutLenght;
                timeoutToTick = TIMEOUT_TIMER;
                textBlock_AwayTimeoutTime.Text = getTimeString(timeoutToTick);
                textBlock_HomeTimeoutTime.Text = getTimeString(timeoutToTick);
            }
            else if (timeout == TIMEOUT_TYPE.none && running == false && PageEnabled)
            {
                AwayColor++; if (AwayColor >= colors.Length) AwayColor = 0;
                textBlock_AwayScore.Foreground = new SolidColorBrush(colors[AwayColor]);
            }
        }

        private void EditRules_Click(object sender, RoutedEventArgs e)
        {
            if (!running && timeoutTicked == 1 && timesTicked == 1)
            {
                PopUpQueryBorder.Visibility = Visibility.Visible;
                PopUpQueryBox.Visibility = Visibility.Visible;
                PopUpQueryInfo.Visibility = Visibility.Visible;
                PopUpQueryOkButton.Visibility = Visibility.Visible;
                PopUpQueryClearButton.Visibility = Visibility.Visible;
                if (rules != null)
                {
                    PopUpQueryBox.Text = rules;
                }
                EnableApp(false);
            }
            else
            {
                string s = "Reset to the initial state first.";
                Task task = new MessageDialog(s).ShowAsync().AsTask();
            }
        }

        private async void SelectIcon_Click(object sender, RoutedEventArgs e)
        {
            if (!running && timeoutTicked == 1 && timesTicked == 1)
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                openPicker.FileTypeFilter.Add(".jpg");
                openPicker.FileTypeFilter.Add(".png");
                StorageFile file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    string renamedFileName = "logo.jpg";
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    StorageFile copiedFile = await file.CopyAsync(folder, renamedFileName, NameCollisionOption.ReplaceExisting);
                    var stream = await copiedFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                    var image = new BitmapImage();
                    image.SetSource(stream);
                    Assets_StreetBasket_jpg.Source = image;
                }
                else
                {
                    Assets_StreetBasket_jpg.Source = new BitmapImage(new Uri("ms-appx:///Assets/StreetBasket.jpg"));
                    string s = "logo.jpg";
                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    StorageFile file2 = await folder.GetFileAsync(s);
                    if (file2 != null)
                    {
                        await file2.DeleteAsync();
                    }
                }
            }
            else
            {
                string s = "Reset to the initial state first.";
                Task task = new MessageDialog(s).ShowAsync().AsTask();
            }
        }

        private async void StoreRulesToFile(string r)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            StorageFile File = await folder.CreateFileAsync("rules.txt", CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(File, r);
        }

        private async void ReadRulesFromFile()
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;
            try
            {
                StorageFile File = await folder.GetFileAsync("rules.txt");
                rules = await FileIO.ReadTextAsync(File);
            }
            catch (Exception ex) when (ex is System.IO.FileNotFoundException || ex is System.InvalidOperationException)
            {
                rules = null;
            }
        }

        private void HideEditor()
        {
            PopUpQueryBorder.Visibility = Visibility.Collapsed;
            PopUpQueryBox.Visibility = Visibility.Collapsed;
            PopUpQueryInfo.Visibility = Visibility.Collapsed;
            PopUpQueryOkButton.Visibility = Visibility.Collapsed;
            PopUpQueryClearButton.Visibility = Visibility.Collapsed;
            EnableApp(true);
        }

        private void EnableApp(bool enable)
        {
            button_AwayMinus.IsEnabled = enable;
            button_AwayPlus.IsEnabled = enable;
            button_HomeMinus.IsEnabled = enable;
            button_HomePlus.IsEnabled = enable;
            button_StartStop.IsEnabled = enable;
            PageEnabled = enable;
        }

        private void PopUpQueryOkClick(object sender, RoutedEventArgs e)
        {
            string s = PopUpQueryBox.Text;
            string ss = null;

            do
            {
                ss = replaseLineFeed(s);
                if (ss != null) s = ss;
            } while (ss != null);

            rules = s;
            StoreRulesToFile(rules);
            HideEditor();
        }

        private string replaseLineFeed(string s_in)
        {
            int i;
            string s_out = null;

            i = s_in.IndexOf("\r\n");
            if (i != -1) // Found
            {
                s_out = s_in.Substring(0, i);
                s_out += s_in.Substring(i + 1);
            }

            return s_out;
        }

        private void PopUpQueryCancelClick(object sender, RoutedEventArgs e)
        {
            PopUpQueryBox.Text = "";
        }

        private void ConfigMode_Click(object sender, RoutedEventArgs e)
        {
            if (!running && timeoutTicked == 1 && timesTicked == 1)
            {
                ConfigMode = (ConfigMode ? false : true);
                textBlock_Time.Foreground = new SolidColorBrush((ConfigMode ? Colors.Gray : Colors.Yellow));
                textBlock_AwayTimeoutTime.Foreground = new SolidColorBrush((ConfigMode ? Colors.Gray : Colors.Yellow));
                textBlock_HomeTimeoutTime.Foreground = new SolidColorBrush((ConfigMode ? Colors.Gray : Colors.Yellow));
                ConfigModeAppBarButton.Label = (ConfigMode ? "Normal mode" : "Config mode");
            }
            else
            {
                string s = "Reset to the initial state first.";
                Task task = new MessageDialog(s).ShowAsync().AsTask();
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string s = "Street Basket Scoreboard\n\nVersion:"+ GetAppVersion() + "\n";
            s += "\u00A9 2016 Hokpe Software. All rights reserved.\n";
            s += "Application is made as study project for universal windows applications.\n";
            s += "If you have change proposals or improvement ideas, don't hesitate to contact.\n\nhokpesoftware@outlook.com.\n";
            Task task = new MessageDialog(s).ShowAsync().AsTask();
        }

        private string GetAppVersion()
        {
            /* Function copied from: http://stackoverflow.com/questions/28635208/retrieve-the-current-app-version-from-package */
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        private void StartStopRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (PageEnabled)
            {
                if (!running && timeout == TIMEOUT_TYPE.none)
                {
                    ResetValues();
                }
            }
        }
    }
}
