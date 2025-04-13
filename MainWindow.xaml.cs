using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        public const int LAS = 1;
        public const int LAKA = 2;
        public const int SKALA = 3;
        public const int ILE_TERENOW = 4;

        private int[,] mapa;
        private int szerokoscMapy;
        private int wysokoscMapy;
        private Image[,] tablicaTerenu;
        private const int RozmiarSegmentu = 32;
        private int calkowiteDrzewa = 0;

        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW];
        private List<Wilk> wilki = new List<Wilk>();
        private Gracz gracz;
        private Random random = new Random();
        private int poziomTrudnosci = 1;
        private const int BazowaWielkoscMapy = 10;

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();
            NowaGra();
        }

        private void NowaGra()
        {
            poziomTrudnosci = 1;
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
            WygenerujMape();
        }

        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
        }

        private void WygenerujMape()
        {
            wysokoscMapy = BazowaWielkoscMapy + (poziomTrudnosci * 2);
            szerokoscMapy = BazowaWielkoscMapy + (poziomTrudnosci * 2);
            mapa = new int[wysokoscMapy, szerokoscMapy];
            calkowiteDrzewa = 0;

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

            InicjalizujMape();
            gracz.X = szerokoscMapy / 2;
            gracz.Y = wysokoscMapy / 2;
            AktualizujPozycjeGracza();

            int liczbaWilkow = 3 + (poziomTrudnosci * 2);
            InicjalizujWilki(liczbaWilkow);

            EtykietaPoziomu.Content = $"Poziom: {poziomTrudnosci}";
            AktualizujLicznikiDrewna();
            OdswiezZycia();
        }

        private void AktualizujLicznikiDrewna()
        {
            EtykietaZebraneDrewno.Text = gracz.Drewno.ToString();
            EtykietaPozostaleDrewno.Text = (calkowiteDrzewa - gracz.Drewno).ToString();
        }

        private void InicjalizujMape()
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

            SiatkaMapy.Children.Add(gracz.Obraz);
            Panel.SetZIndex(gracz.Obraz, 1);
        }

        private void InicjalizujWilki(int liczbaWilkow)
        {
            wilki.Clear();
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

        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(gracz.Obraz, gracz.Y);
            Grid.SetColumn(gracz.Obraz, gracz.X);
        }

        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
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

        private void SprawdzCzyWygrana()
        {
            if (gracz.Drewno >= calkowiteDrzewa)
            {
                WinPanel.Visibility = Visibility.Visible;
                this.IsEnabled = false;
            }
        }

        private void OdswiezZycia()
        {
            Serce1.Visibility = gracz.Zycia >= 1 ? Visibility.Visible : Visibility.Collapsed;
            Serce2.Visibility = gracz.Zycia >= 2 ? Visibility.Visible : Visibility.Collapsed;
            Serce3.Visibility = gracz.Zycia >= 3 ? Visibility.Visible : Visibility.Collapsed;

            if (gracz.Zycia <= 0)
            {
                MessageBox.Show("Game Over!");
                NowaGra();
            }
        }

        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            NowaGra();
        }

        private void Kontynuj_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Kontynuuj clicked!"); // Debug w Output
            WinPanel.Visibility = Visibility.Collapsed;
            LevelCompletePanel.Visibility = Visibility.Visible;
            LevelCompletePanel.IsEnabled = true;
            FocusManager.SetFocusedElement(this, NextLevelButton);
        }

        private void NextLevel_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci++;
            WygenerujMape();
            LevelCompletePanel.Visibility = Visibility.Collapsed;
            this.IsEnabled = true;
        }

        private void ExitGame_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
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

    public class Gracz
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Zycia { get; set; }
        public int Drewno { get; set; }
        public Image Obraz { get; set; }
    }
    
}