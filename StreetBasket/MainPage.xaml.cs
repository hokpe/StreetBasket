using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Security.Cryptography;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace StreetBasket
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static int MAIN_TIMER = 60 * 15; // 15 min
        static int TiMEOUT_TIMER = 30;   // 30 sec
        private enum TIMEOUT_TYPE { none, home, away };
        private Color[] colors; // declare numbers as an int array of any size
        
        private DispatcherTimer mainTimer;
        private DispatcherTimer timeoutTimer;
        private DispatcherTimer RandomAnimationTimer;
        int timesTicked = 1;
        int timesToTick = MAIN_TIMER;
        int timeoutTicked = 1;
        int timeoutToTick = TiMEOUT_TIMER;
        int randomCounter = 50;
        int HomeColor = 0;
        int AwayColor = 1;
        Boolean running = false;
        private Audio audio = new Audio();

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
            ResetValues();
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
                if(randomCounter == 0)
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
            timeoutToTick = TiMEOUT_TIMER; // 30 = 30 sec
            textBlock_Time.Text = getTimeString(timesToTick);
            textBlock_AwayScore.Text = "0";
            textBlock_HomeScore.Text = "0";
            textBlock_AwayTimeoutTime.Text = getTimeString(timeoutToTick);
            textBlock_HomeTimeoutTime.Text = getTimeString(timeoutToTick);
            BlackArrow.Visibility = Visibility.Collapsed;
            WhiteArrow.Visibility = Visibility.Collapsed;
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

        private void AwayTimeoutTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!running && timeout != TIMEOUT_TYPE.home)
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
            if (!running && timeout != TIMEOUT_TYPE.away)
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
            if (!running && timeout == TIMEOUT_TYPE.none)
            {
                ResetValues();
            }
        }

        private void StartStopHolding(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (!running && timeout == TIMEOUT_TYPE.none)
            {
                ResetValues();
            }
        }

        private void StartStopClick(object sender, TappedRoutedEventArgs e)
        {
            BlackArrow.Visibility = Visibility.Collapsed;
            WhiteArrow.Visibility = Visibility.Collapsed;
            if (timeout == TIMEOUT_TYPE.none)
            {
                running = !running;
                if (running)
                {
                    TextBlock_StartStop.Text = "Stop";
                    mainTimer.Start();
                }
                else
                {
                    TextBlock_StartStop.Text = "Start";
                    mainTimer.Stop();
                }
            }
        }

        private void RulesTapped(object sender, TappedRoutedEventArgs e)
        {
            /* Asynkroninen operaatio ilman varoituksia. */
            Task task = new MessageDialog("Salo StreetBasket Säännöt:\n\n1. Peli\nPeliä pelaa yhteen koriin kaksi joukkuetta, joilla kummallakin on kolme pelaajaa kerrallaan kentällä.Pelissä noudatetaan virallisia koripallosääntöjä näissä säännöissä mainituin poikkeuksin.\n\n2.Pelikenttä\nPelikentän koko on noin puolet normaalista koripallokentästä.Pääty - ja sivurajat merkitään olosuhteisiin parhaiten soveltuvalla tavalla.Kenttään tulee merkitä myös aloituspiste / -viiva ja vapaaheittoviiva sekä kolmen pisteen heittoviiva.\n\n3.Toimitsijat\nPelissä ei ole erotuomaria, vaan ns.valvoja laskee pisteet.Pelaajat tuomitsevat itse ottelunsa, jolloin hyökkääjä tuomitsee puolustajan virheet ja rikkomukset ja puolustaja vastaavasti hyökkääjän.Pelaajien omista tuomioista ei seuraa vapaaheittoja vaan peliä jatketaan aloituspisteestä kohdan 7.Rikkomukset ja virheet kohdan mukaisesti.\n\n4.Pelaajat\nJoukkueessa saa olla enintään viisi pelaajaa, joista kentällä yhtä aikaa kolme.Firma - sarjassa ja Hupi - sarjassa ei rajoitteita pelaajamäärä / joukkue.\n\n5. Pelin pituus\nOttelun max.kestoaika on 15min. Ottelun voittaa ja päättää ensimmäisenä 15 pistettä tehnyt joukkue.Mikäli eroa on vain yksi piste, jatketaan kunnes syntyy kahden pisteen ero, tai kunnes toinen joukkue saavuttaa 20 pistettä.Joukkueilla on oikeus 30 sekunnin aikalisään.Mikäli peli on tasan kun 15min täyttyy, pelataan jatkoaika ns.kultainen kori - säännöllä.Tällöin aloittaja arvotaan.\n\n6.Pelimääräykset\nAloittava joukkue arvotaan.Peli alkaa aloittavan joukkueen syötöllä aloituspisteestä(vapaaheittoympyrän kaaren takaa, n. 6, 5 m korirenkaasta).Hyökkäysaika on rajoittamaton.Hyökkäysvuoro vaihtuu jokaisen hyväksytyn pelikorin jälkeen.Peli jatkuu, kun puolustukseen ryhmittynyt joukkue antaa pallon aloituspisteessä olevalle hyökkäävän joukkueen pelaajalle.Kiistapallotilanteissa aloitus annetaan aina puolustavalle joukkueelle.Korista saa yhden pisteen, kolmen pisteen viivan takaa tehdystä korista saa kaksi pistettä.Pelaajavaihtoja voidaan suorittaa vapaasti kaikkien pelikatkojen aikana.\n\n7.Rikkomukset ja virheet\nKolmen sekunnin sääntö ei ole voimassa.Virheistä(pelaajien henkilökohtaiset virheet), rikkomuksista(esim.askelrike, kaksoiskuljetus) ja pallon joutuessa ulos kentältä rikkeen aiheuttaneen joukkueen vastustaja saa aloituksen aloituspisteestä.Ottelun valvoja voi omasta aloitteestaan puuttua tarvittaessa peliin.Hän voi tuomita epäurheilijamaisen tai teknillisen virheen, josta seuraa rikotulle joukkueelle yksi vapaaheitto ja sen jälkeen myös aloitus samalle joukkueelle aloituspisteestä.").ShowAsync().AsTask();
        }

        private void TimeTapped(object sender, TappedRoutedEventArgs e)
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

        private void HomeScoreTapped(object sender, TappedRoutedEventArgs e)
        {
            if (timeout == TIMEOUT_TYPE.none && running == false)
            {
                HomeColor++; if (HomeColor >= colors.Length) HomeColor = 0;
                textBlock_HomeScore.Foreground = new SolidColorBrush(colors[HomeColor]);
            }
        }

        private void AwayScoreTapped(object sender, TappedRoutedEventArgs e)
        {
            if (timeout == TIMEOUT_TYPE.none && running == false)
            {
                AwayColor++; if (AwayColor >= colors.Length) AwayColor = 0;
                textBlock_AwayScore.Foreground = new SolidColorBrush(colors[AwayColor]);
            }
        }
    }
}
