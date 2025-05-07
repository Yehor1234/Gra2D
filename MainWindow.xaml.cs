using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        public const int LAS = 1;
        public const int LAKA = 2;
        public const int SKALA = 3;
        public const int ILE_TERENOW = 4;

        private int[,] mapa = null!;
        private int szerokoscMapy;
        private int wysokoscMapy;
        private Image[,] tablicaTerenu = null!;
        private const int RozmiarSegmentu = 32;
        private int calkowiteDrzewa = 0;

        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];
        private List<Wilk> wilki = new List<Wilk>();
        private Gracz gracz;
        private Random random = new Random();
        private DispatcherTimer timerWilkow = null!;
        private double predkoscWilkow = 5;
        private int wybranaMapaSzerokosc = 10;
        private int wybranaMapaWysokosc = 10;
        private int poziomTrudnosci = 1;


        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();
            PokazMenuGlowne();
            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
        }

        private void PokazMenuGlowne()
        {
            MainMenuPanel.Visibility = Visibility.Visible;
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            MapaPanel.Visibility = Visibility.Collapsed;
            InstrukcjePanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            WinPanel.Visibility = Visibility.Collapsed;
            GameOverPanel.Visibility = Visibility.Collapsed;

            // Reset focus
            MainMenuPanel.Focus();
        }
        public class Gracz
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Image Obraz { get; set; }
            public int Zycia { get; set; }
            public int Drewno { get; set; }

            public Gracz()
            {
                Obraz = new Image
                {
                    Width = 32,
                    Height = 32,
                };
                Zycia = 3;
                Drewno = 0;
            }
        }

        private void NowaGra_Click(object sender, RoutedEventArgs e)
        {
            WybierzPoziom_Click(sender, e);
        }

        private void WybierzPoziom_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            PoziomTrudnosciPanel.Visibility = Visibility.Visible;
        }

        private void WybierzMape_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            MapaPanel.Visibility = Visibility.Visible;
        }

        private void Instrukcje_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            InstrukcjePanel.Visibility = Visibility.Visible;
        }

        private void Wyjdz_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void WrocDoMenuGlownego_Click(object sender, RoutedEventArgs e)
        {
            WinPanel.Visibility = Visibility.Collapsed;
            GameOverPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;

            if (timerWilkow != null)
            {
                timerWilkow.Stop();
            }

            PokazMenuGlowne();
        }


        private void WrocDoMenuGlownegoZGry_Click(object sender, RoutedEventArgs e)
        {
            if (timerWilkow != null && timerWilkow.IsEnabled)
            {
                timerWilkow.Stop();
            }
            PokazMenuGlowne();
        }

        private void LatwyPoziom_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci = 1;
            predkoscWilkow = 6;
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            WybierzMape_Click(sender, e);
        }

        private void SredniPoziom_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci = 2;
            predkoscWilkow = 3.5;
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            WybierzMape_Click(sender, e);
        }

        private void TrudnyPoziom_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci = 3;
            predkoscWilkow = 2;
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            WybierzMape_Click(sender, e);
        }

        private void MapaLatwa_Click(object sender, RoutedEventArgs e)
        {
            wybranaMapaSzerokosc = 5;
            wybranaMapaWysokosc = 5;
            MapaPanel.Visibility = Visibility.Collapsed;
            StartNowaGra();
        }

        private void MapaSrednia_Click(object sender, RoutedEventArgs e)
        {
            wybranaMapaSzerokosc = 10;
            wybranaMapaWysokosc = 10;
            MapaPanel.Visibility = Visibility.Collapsed;
            StartNowaGra();
        }

        private void MapaTrudna_Click(object sender, RoutedEventArgs e)
        {
            wybranaMapaSzerokosc = 15;
            wybranaMapaWysokosc = 15;
            MapaPanel.Visibility = Visibility.Collapsed;
            StartNowaGra();
        }

        private void StartNowaGra()
        {
            GamePanel.Visibility = Visibility.Visible;
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();
            wilki.Clear();

            gracz = new Gracz
            {
                Obraz = new Image
                {
                    Width = RozmiarSegmentu,
                    Height = RozmiarSegmentu,
                    Source = new BitmapImage(new Uri("gracz.png", UriKind.Relative))
                },
                Zycia = 3,
                Drewno = 0
            };

            wysokoscMapy = wybranaMapaWysokosc;
            szerokoscMapy = wybranaMapaSzerokosc;
            mapa = new int[wysokoscMapy, szerokoscMapy];
            calkowiteDrzewa = 0;

            WygenerujMape();
            InicjalizujWilki();
            UruchomTimerWilkow();
            AktualizujInterfejsGry();
            GamePanel.Focus(); // Dodana linia ustawiająca fokus na GamePanel
                              
        }

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
        }

        private void WygenerujMape()
        {
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    double los = random.NextDouble();
                    mapa[y, x] = los switch
                    {
                        < 0.6 => LAKA,
                        < 0.8 => LAS,
                        _ => SKALA
                    };
                    if (mapa[y, x] == LAS) calkowiteDrzewa++;
                }
            }

            InicjalizujMapeGUI();
            gracz.X = szerokoscMapy / 2;
            gracz.Y = wysokoscMapy / 2;
            AktualizujPozycjeGracza();
        }

        private void InicjalizujMapeGUI()
        {
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            tablicaTerenu = new Image[wysokoscMapy, szerokoscMapy];
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    Image obraz = new Image
                    {
                        Width = RozmiarSegmentu,
                        Height = RozmiarSegmentu,
                        Source = obrazyTerenu[mapa[y, x]]
                    };
                    Grid.SetRow(obraz, y);
                    Grid.SetColumn(obraz, x);
                    SiatkaMapy.Children.Add(obraz);
                    tablicaTerenu[y, x] = obraz;
                }
            }

            Grid.SetRow(gracz.Obraz, gracz.Y);
            Grid.SetColumn(gracz.Obraz, gracz.X);
            SiatkaMapy.Children.Add(gracz.Obraz);
            Panel.SetZIndex(gracz.Obraz, 1);
        }

        private void InicjalizujWilki()
        {
            wilki.Clear();
            int liczbaWilkow = 3 + (poziomTrudnosci * 2);
            List<(int x, int y)> dostepnePola = new List<(int, int)>();

            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    if (mapa[y, x] == LAKA &&
                        Math.Abs(x - gracz.X) > 3 &&
                        Math.Abs(y - gracz.Y) > 3)
                    {
                        dostepnePola.Add((x, y));
                    }
                }
            }

            for (int i = 0; i < Math.Min(liczbaWilkow, dostepnePola.Count); i++)
            {
                int index = random.Next(0, dostepnePola.Count);
                var (x, y) = dostepnePola[index];
                dostepnePola.RemoveAt(index);

                Wilk wilk = new Wilk(x, y);
                wilki.Add(wilk);
                Grid.SetRow(wilk.Obraz, y);
                Grid.SetColumn(wilk.Obraz, x);
                SiatkaMapy.Children.Add(wilk.Obraz);
            }
        }

        private void UruchomTimerWilkow()
        {
            timerWilkow = new DispatcherTimer();
            timerWilkow.Interval = TimeSpan.FromSeconds(predkoscWilkow);
            timerWilkow.Tick += TimerWilkow_Tick;
            timerWilkow.Start();
        }

        private void TimerWilkow_Tick(object sender, EventArgs e)
        {
            foreach (var wilk in wilki.ToList())
            {
                RuszWilka(wilk);
                if (gracz.X == wilk.X && gracz.Y == wilk.Y)
                {
                    gracz.Zycia--;
                    OdswiezZycia();
                    SiatkaMapy.Children.Remove(wilk.Obraz);
                    wilki.Remove(wilk);
                    break;
                }
            }
        }

        private void RuszWilka(Wilk wilk)
        {
            int roznicaX = gracz.X - wilk.X;
            int roznicaY = gracz.Y - wilk.Y;

            int nowyX = wilk.X;
            int nowyY = wilk.Y;

            if (Math.Abs(roznicaX) > Math.Abs(roznicaY))
            {
                nowyX += Math.Sign(roznicaX);
            }
            else
            {
                nowyY += Math.Sign(roznicaY);
            }

            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy && mapa[nowyY, nowyX] != SKALA)
            {
                Grid.SetColumn(wilk.Obraz, nowyX);
                Grid.SetRow(wilk.Obraz, nowyY);
                wilk.X = nowyX;
                wilk.Y = nowyY;
            }
        }

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(gracz.Obraz, gracz.Y);
            Grid.SetColumn(gracz.Obraz, gracz.X);
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
            if (GamePanel.Visibility == Visibility.Visible && WinPanel.Visibility != Visibility.Visible && GameOverPanel.Visibility != Visibility.Visible)
            {
                int nowyX = gracz.X;
                int nowyY = gracz.Y;

                if (e.Key == Key.Up) nowyY--;
                else if (e.Key == Key.Down) nowyY++;
                else if (e.Key == Key.Left) nowyX--;
                else if (e.Key == Key.Right) nowyX++;

                if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy)
                {
                    if (mapa[nowyY, nowyX] != SKALA)
                    {
                        gracz.X = nowyX;
                        gracz.Y = nowyY;
                        AktualizujPozycjeGracza();
                    }
                }

                foreach (var wilk in wilki.ToList())
                {
                    if (gracz.X == wilk.X && gracz.Y == wilk.Y)
                    {
                        gracz.Zycia--;
                        OdswiezZycia();
                        SiatkaMapy.Children.Remove(wilk.Obraz);
                        wilki.Remove(wilk);
                        break;
                    }
                }

                // Ścinanie drzewa (klawisz E)
                if (e.Key == Key.E && mapa[gracz.Y, gracz.X] == LAS)
                {
                    mapa[gracz.Y, gracz.X] = LAKA;
                    tablicaTerenu[gracz.Y, gracz.X].Source = obrazyTerenu[LAKA];
                    gracz.Drewno++;
                    AktualizujLicznikiDrewna();
                    SprawdzCzyWygrana();
                }
            }
        }

        private void SprawdzCzyWygrana()
        {
            if (gracz.Drewno >= calkowiteDrzewa)
            {
                WinPanel.Visibility = Visibility.Visible;
                GamePanel.IsHitTestVisible = false;
                if (timerWilkow != null)
                {
                    timerWilkow.Stop();
                }
            }
        }

        private void OdswiezZycia()
        {
            Serce1.Visibility = gracz.Zycia >= 1 ? Visibility.Visible : Visibility.Collapsed;
            Serce2.Visibility = gracz.Zycia >= 2 ? Visibility.Visible : Visibility.Collapsed;
            Serce3.Visibility = gracz.Zycia >= 3 ? Visibility.Visible : Visibility.Collapsed;

            if (gracz.Zycia <= 0)
            {
                GameOverPanel.Visibility = Visibility.Visible;
                GamePanel.IsHitTestVisible = false;
                if (timerWilkow != null)
                {
                    timerWilkow.Stop();
                }
            }
        }

        private void AktualizujLicznikiDrewna()
        {
            EtykietaZebraneDrewno.Text = gracz.Drewno.ToString();
            EtykietaPozostaleDrewno.Text = (calkowiteDrzewa - gracz.Drewno).ToString();
        }

        private void AktualizujInterfejsGry()
        {
            EtykietaPoziomu.Content = $"Poziom: {poziomTrudnosci}";
            AktualizujLicznikiDrewna();
            OdswiezZycia();
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            StartNowaGra();
        }

        private void Kontynuuj_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci++;
            StartNowaGra();
            WinPanel.Visibility = Visibility.Collapsed;
            this.IsEnabled = true;
        }
        public class Wilk
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Image Obraz { get; set; }

            public Wilk(int x, int y)
            {
                X = x;
                Y = y;
                Obraz = new Image
                {
                    Width = 32,
                    Height = 32,
                    Source = new BitmapImage(new Uri("wilk.png", UriKind.Relative))
                };
            }

        }
    }
}