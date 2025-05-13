using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Gra2D.MainWindow;

namespace Gra2D
{
    public partial class MainWindow : Window
    {
        // Stałe określające typy terenu
        public const int LAS = 1; //kombinacja modyfikatorów dostępu
        public const int LAKA = 2;
        public const int SKALA = 3;
        public const int ILE_TERENOW = 4;

        // Zmienne przechowujące stan gry
        private int[,] mapa = null!;            // Dwuwymiarowa tablica reprezentująca mapę
        private int szerokoscMapy;             // Szerokość mapy w segmentach
        private int wysokoscMapy;              // Wysokość mapy w segmentach
        private Image[,] tablicaTerenu = null!; // Tablica obrazków reprezentujących teren
        private const int RozmiarSegmentu = 32; // Rozmiar pojedynczego segmentu mapy w pikselach
        private int calkowiteDrzewa = 0;       // Licznik wszystkich drzew na mapie

        // Zasoby graficzne
        private BitmapImage[] obrazyTerenu = new BitmapImage[ILE_TERENOW]; // Obrazy różnych typów terenu
        private List<Wilk> wilki = new List<Wilk>(); // Lista wilków na mapie
        private Gracz gracz;                        // Obiekt gracza
        private Random random = new Random();       // Generator liczb losowych

        // Zmienne związane z mechaniką gry
        private DispatcherTimer timerWilkow = null!; // Timer sterujący ruchami wilków
        private double predkoscWilkow = 5;          // Prędkość wilków (im mniejsza wartość, tym szybsze wilki)
        private int wybranaMapaSzerokosc = 10;     // Domyślna szerokość mapy
        private int wybranaMapaWysokosc = 10;      // Domyślna wysokość mapy
        private int poziomTrudnosci = 1;           // Aktualny poziom trudności
        private bool ruchWTrakcie = false;         // Flaga zapobiegająca równoczesnym ruchom

        public MainWindow()
        {
            InitializeComponent();
            WczytajObrazyTerenu();  // Ładowanie obrazów terenu
            PokazMenuGlowne();       // Wyświetlenie menu głównego
            this.WindowState = WindowState.Maximized; // tworzy efekt pełnoekranowy bez pasków tytułu i obramowań
            this.WindowStyle = WindowStyle.None;      // Brak paska tytułowego
        }

        // Metody zarządzające stanem okna
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (GamePanel.Visibility == Visibility.Visible)
            {
                this.KeyDown += OknoGlowne_KeyDown; // Aktywacja obsługi klawiatury gdy okno jest aktywne
            }
        }

        protected override void OnDeactivated(EventArgs e)
        {
            if (GamePanel.Visibility == Visibility.Visible)
            {
                this.KeyDown -= OknoGlowne_KeyDown; // Dezaktywacja obsługi klawiatury gdy okno jest nieaktywne
            }
            base.OnDeactivated(e);
        }

        // Wyświetlanie menu głównego i ukrywanie innych paneli
        private void PokazMenuGlowne()
        {
            MainMenuPanel.Visibility = Visibility.Visible;
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            MapaPanel.Visibility = Visibility.Collapsed;
            InstrukcjePanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            WinPanel.Visibility = Visibility.Collapsed;
            GameOverPanel.Visibility = Visibility.Collapsed;//Ten blok kodu odpowiada za przełączanie widoczności paneli

            MainMenuPanel.Focus();
            this.KeyDown -= OknoGlowne_KeyDown;
        }

        // Klasa reprezentująca gracza
        public class Gracz
        {
            public int X { get; set; }      // Pozycja X na mapie
            public int Y { get; set; }      // Pozycja Y na mapie
            public Image Obraz { get; set; } // Obrazek gracza
            public int Zycia { get; set; }   // Liczba żyć
            public int Drewno { get; set; }  // Zebrane drewno

            public Gracz()
            {
                Obraz = new Image
                {
                    Width = 32,
                    Height = 32,
                };
                Zycia = 3;    // Początkowa liczba żyć
                Drewno = 0;     // Początkowa ilość drewna
            }
        }

        // Obsługa przycisków menu
        private void NowaGra_Click(object sender, RoutedEventArgs e)
        {
            MainMenuPanel.Visibility = Visibility.Collapsed;
            PoziomTrudnosciPanel.Visibility = Visibility.Visible;
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
            Application.Current.Shutdown(); // Zamknięcie aplikacji
        }

        private void WrocDoMenuGlownego_Click(object sender, RoutedEventArgs e)
        {
            // Resetowanie gry i powrót do menu głównego
            WinPanel.Visibility = Visibility.Collapsed;
            GameOverPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;

            if (timerWilkow != null)
            {
                timerWilkow.Stop(); // Zatrzymanie timeru wilków
            }

            PokazMenuGlowne();
            this.KeyDown -= OknoGlowne_KeyDown;
        }

        private void WrocDoMenuGlownegoZGry_Click(object sender, RoutedEventArgs e)
        {
            // Zatrzymanie gry i powrót do menu
            if (timerWilkow != null && timerWilkow.IsEnabled)
            {
                timerWilkow.Stop();
            }

            GamePanel.Visibility = Visibility.Collapsed;
            WinPanel.Visibility = Visibility.Collapsed;
            GameOverPanel.Visibility = Visibility.Collapsed;
            MainMenuPanel.Visibility = Visibility.Visible;//Te linie kodu zarządzają widocznością głównych paneli interfejsu użytkownika

            this.IsEnabled = true;
            this.Focus();
        }

        // Wybór poziomu trudności
        private void LatwyPoziom_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci = 1;
            predkoscWilkow = 6;  // Wilki wolniejsze
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            WybierzMape_Click(sender, e);
        }

        private void SredniPoziom_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci = 2;
            predkoscWilkow = 4;  // Średnia prędkość
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            WybierzMape_Click(sender, e);
        }

        private void TrudnyPoziom_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci = 3;
            predkoscWilkow = 2;  // Wilki szybsze
            PoziomTrudnosciPanel.Visibility = Visibility.Collapsed;
            WybierzMape_Click(sender, e);
        }

        // Wybór rozmiaru mapy
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
            MapaPanel.Visibility = Visibility.Collapsed;//ukrywania panelu wyboru mapy
            StartNowaGra();
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            // Powrót do menu z poziomu gry
            GamePanel.Visibility = Visibility.Collapsed;
            MainMenuPanel.Visibility = Visibility.Visible;

            if (timerWilkow != null)
            {
                timerWilkow.Stop();
            }
        }

        // Rozpoczęcie nowej gry
        private void StartNowaGra()
        {
            GamePanel.Visibility = Visibility.Visible;
            SiatkaMapy.Children.Clear(); // Czyszczenie poprzedniej mapy
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();
            wilki.Clear(); // Usunięcie wilków z poprzedniej gry

            // Inicjalizacja gracza
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

            // Ustawienie rozmiarów mapy
            wysokoscMapy = wybranaMapaWysokosc;
            szerokoscMapy = wybranaMapaSzerokosc;
            mapa = new int[wysokoscMapy, szerokoscMapy];
            calkowiteDrzewa = 0;

            // Generowanie nowej gry
            WygenerujMape();
            InicjalizujWilki();
            UruchomTimerWilkow();
            AktualizujInterfejsGry();
            GamePanel.Focus();
        }

        // Ładowanie obrazów terenu z plików
        private void WczytajObrazyTerenu()
        {
            obrazyTerenu[LAS] = new BitmapImage(new Uri("las.png", UriKind.Relative));
            obrazyTerenu[LAKA] = new BitmapImage(new Uri("laka.png", UriKind.Relative));
            obrazyTerenu[SKALA] = new BitmapImage(new Uri("skala.png", UriKind.Relative));
        }

        // Generowanie losowej mapy
        private void WygenerujMape()
        {
            for (int y = 0; y < wysokoscMapy; y++)
            {
                for (int x = 0; x < szerokoscMapy; x++)
                {
                    double los = random.NextDouble();
                    mapa[y, x] = los switch
                    {
                        < 0.6 => LAKA,  // 60% szans na łąkę
                        < 0.8 => LAS,   // 20% szans na las
                        _ => SKALA      // 20% szans na skałę
                    };
                    if (mapa[y, x] == LAS) calkowiteDrzewa++;
                }
            }

            InicjalizujMapeGUI();
            // Ustawienie gracza na środku mapy
            gracz.X = szerokoscMapy / 2;
            gracz.Y = wysokoscMapy / 2;
            AktualizujPozycjeGracza();
        }

        // Inicjalizacja interfejsu graficznego mapy
        private void InicjalizujMapeGUI()
        {
            SiatkaMapy.Children.Clear();
            SiatkaMapy.RowDefinitions.Clear();
            SiatkaMapy.ColumnDefinitions.Clear();

            // Tworzenie wierszy i kolumn siatki
            for (int y = 0; y < wysokoscMapy; y++)
                SiatkaMapy.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(RozmiarSegmentu) });

            for (int x = 0; x < szerokoscMapy; x++)
                SiatkaMapy.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(RozmiarSegmentu) });

            // Wypełnianie mapy obrazkami terenu
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

            // Dodanie gracza na mapę
            Grid.SetRow(gracz.Obraz, gracz.Y);
            Grid.SetColumn(gracz.Obraz, gracz.X);
            SiatkaMapy.Children.Add(gracz.Obraz);
            Panel.SetZIndex(gracz.Obraz, 1); // Gracz na wierzchu
        }

        // Inicjalizacja wilków na mapie
        private void InicjalizujWilki()
        {
            wilki.Clear();
            // Liczba wilków zależna od poziomu trudności
            int liczbaWilkow = poziomTrudnosci switch
            {
                1 => 2,  // Łatwy - 2 wilki
                2 => 5,  // Średni - 5 wilków
                3 => 8,  // Trudny - 8 wilków
                _ => 2   // Domyślnie 2 wilki
            };

            List<(int x, int y)> dostepnePola = new List<(int, int)>();

            // Znajdowanie dostępnych pól (łąki z dala od gracza)
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

            // Ograniczenie liczby wilków do dostępnych pól
            liczbaWilkow = Math.Min(liczbaWilkow, dostepnePola.Count);

            // Umieszczanie wilków na mapie
            for (int i = 0; i < liczbaWilkow; i++)
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
        }//Ten fragment kodu odpowiada za generowanie i umieszczanie wilków na mapie gry

        // Uruchomienie timera sterującego wilkami
        private void UruchomTimerWilkow()
        {
            timerWilkow = new DispatcherTimer();//Tworzenie nowego timera
            timerWilkow.Interval = TimeSpan.FromSeconds(predkoscWilkow);//Ustawienie interwału
            timerWilkow.Tick += TimerWilkow_Tick;//Podpięcie zdarzenia Tick
            timerWilkow.Start();//Uruchomienie timera
        }

        // Obsługa ruchu wilków
        private void TimerWilkow_Tick(object sender, EventArgs e)
        {
            foreach (var wilk in wilki.ToList())
            {
                RuszWilka(wilk);
                // Sprawdzenie kolizji z graczem
                if (gracz.X == wilk.X && gracz.Y == wilk.Y)
                {
                    gracz.Zycia--;
                    OdswiezZycia();
                    SiatkaMapy.Children.Remove(wilk.Obraz);
                    wilki.Remove(wilk);
                    break;
                }
            }
        }//Ta metoda jest funkcją wywoływaną cyklicznie przez timer

        // Logika ruchu wilka w kierunku gracza
        private void RuszWilka(Wilk wilk)
        {
            int roznicaX = gracz.X - wilk.X;
            int roznicaY = gracz.Y - wilk.Y;

            int nowyX = wilk.X;
            int nowyY = wilk.Y;

            // Wybór kierunku ruchu (w stronę gracza)
            if (Math.Abs(roznicaX) > Math.Abs(roznicaY))
            {
                nowyX += Math.Sign(roznicaX); // Ruch w poziomie
            }
            else
            {
                nowyY += Math.Sign(roznicaY); // Ruch w pionie
            }

            // Sprawdzenie czy ruch jest możliwy (nie wychodzi poza mapę i nie jest na skałę)
            if (nowyX >= 0 && nowyX < szerokoscMapy && nowyY >= 0 && nowyY < wysokoscMapy && mapa[nowyY, nowyX] != SKALA)
            {
                Grid.SetColumn(wilk.Obraz, nowyX);//Ustawia kolumnę (współrzędną X) w której znajduje się obraz wilka
                Grid.SetRow(wilk.Obraz, nowyY);//Ustawia wiersz (współrzędną Y) w którym znajduje się obraz wilka
                wilk.X = nowyX;
                wilk.Y = nowyY;
            }
        }

       
        private void AktualizujPozycjeGracza()
        {
            Grid.SetRow(gracz.Obraz, gracz.Y);//// Ustawia wiersz (Y)
            Grid.SetColumn(gracz.Obraz, gracz.X);//// Ustawia kolumnę (X)
        }

        // Obsługa klawiatury
        private void OknoGlowne_KeyDown(object sender, KeyEventArgs e)
        {
           
            if (GamePanel.Visibility != Visibility.Visible ||
                WinPanel.Visibility == Visibility.Visible ||
                GameOverPanel.Visibility == Visibility.Visible ||
                ruchWTrakcie)
            {
                return;
            }//Sprawdza czy gra jest w odpowiednim stanie do obsługi inputu

            ruchWTrakcie = true; //Ustawia flagę blokującą kolejne wejścia podczas przetwarzania bieżącego

            try
            {
                int nowyX = gracz.X;
                int nowyY = gracz.Y;//Pobiera aktualną pozycję gracza jako punkt wyjścia

                // Obsługa klawiszy kierunkowych
                switch (e.Key)
                {
                    case Key.Up: nowyY--; break;
                    case Key.Down: nowyY++; break;
                    case Key.Left: nowyX--; break;
                    case Key.Right: nowyX++; break;//Modyfikuje współrzędne docelowe w zależności od wciśniętego klawisza
                    case Key.E: // Zbieranie drewna
                        if (mapa[gracz.Y, gracz.X] == LAS)
                        {
                            mapa[gracz.Y, gracz.X] = LAKA;
                            tablicaTerenu[gracz.Y, gracz.X].Source = obrazyTerenu[LAKA];
                            gracz.Drewno++;
                            AktualizujLicznikiDrewna();
                            SprawdzCzyWygrana();
                        }//Sprawdza czy gracz stoi na polu z lasem
                        return;
                    default://Ignoruje wszystkie klawisze inne niż kierunkowe i E
                        return;
                }

                // Sprawdzenie czy nowa pozycja jest prawidłowa
                if (nowyX >= 0 && nowyX < szerokoscMapy &&
                    nowyY >= 0 && nowyY < wysokoscMapy &&
                    mapa[nowyY, nowyX] != SKALA)
                {
                    gracz.X = nowyX;//Aktualizuje współrzędne gracza      
                    gracz.Y = nowyY;// Wywołuje metodę aktualizującą pozycję gracza na ekranie
                    AktualizujPozycjeGracza();

                    // Sprawdzenie kolizji z wilkami
                    foreach (var wilk in wilki.ToList())//Użycie .ToList() tworzy kopię listy, co pozwala bezpiecznie modyfikować oryginalną listę wilki podczas iteracji
                    {
                        if (gracz.X == wilk.X && gracz.Y == wilk.Y)//Porównuje współrzędne (X,Y) gracza i wilka
                        {
                            gracz.Zycia--;
                            OdswiezZycia();
                            SiatkaMapy.Children.Remove(wilk.Obraz);
                            wilki.Remove(wilk);
                            break;
                        }
                    }
                }
            }
            finally
            {
                ruchWTrakcie = false;
            }
        }

        // Sprawdzenie warunków wygranej (zebranie wszystkich drzew)
        private void SprawdzCzyWygrana()
        {
            if (gracz.Drewno >= calkowiteDrzewa) //Porównuje ilość zebranego drewna
            {
                WinPanel.Visibility = Visibility.Visible; //Ustawia widoczność panelu wygranej (WinPanel) na widoczną
                GamePanel.IsHitTestVisible = false; //Blokuje możliwość interakcji z głównym panelem gry
                if (timerWilkow != null)
                {
                    timerWilkow.Stop();
                }
            }//Ta metoda odpowiada za sprawdzenie warunków wygranej 
        }

        // Aktualizacja wyświetlania żyć gracza
        private void OdswiezZycia()
        {
            Serce1.Visibility = gracz.Zycia >= 1 ? Visibility.Visible : Visibility.Collapsed;
            Serce2.Visibility = gracz.Zycia >= 2 ? Visibility.Visible : Visibility.Collapsed;
            Serce3.Visibility = gracz.Zycia >= 3 ? Visibility.Visible : Visibility.Collapsed;//fragment kodu odpowiada za wizualizację liczby żyć gracza za pomocą ikon serc w interfejsie gry

            // Sprawdzenie przegranej (brak żyć)
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

        // Aktualizacja liczników drewna
        private void AktualizujLicznikiDrewna()
        {
            EtykietaZebraneDrewno.Text = gracz.Drewno.ToString();//Wyświetla ilość drewna zebranego przez gracza
            EtykietaPozostaleDrewno.Text = (calkowiteDrzewa - gracz.Drewno).ToString(); //Wyświetla ilość drewna pozostałego do zebrania
        }

        // Aktualizacja interfejsu gry
        private void AktualizujInterfejsGry()
        {
            EtykietaPoziomu.Content = $"Poziom: {poziomTrudnosci}";
            AktualizujLicznikiDrewna();
            OdswiezZycia();
        }

        // Przyciski w panelach gry
        private void Restart_Click(object sender, RoutedEventArgs e)
        {
            StartNowaGra(); // Restart gry
        }

        private void Kontynuuj_Click(object sender, RoutedEventArgs e)
        {
            poziomTrudnosci++; // Przejście na wyższy poziom
            StartNowaGra();//to funkcja inicjalizująca nową rozgrywkę 
            WinPanel.Visibility = Visibility.Collapsed; //W efekcie ukrywa panel wygranej w interfejsie użytkownika
            this.IsEnabled = true;
        }

        // Klasa reprezentująca wilka
        public class Wilk
        {
            public int X { get; set; }      // Pozycja X na mapie, przechowuje pozycje wilka
            public int Y { get; set; }      // Pozycja Y na mapie
            public Image Obraz { get; set; } // Obrazek wilka

            public Wilk(int x, int y)
            {
                X = x;
                Y = y;//ustawia pozycje wilka
                Obraz = new Image
                {
                    Width = 32,//szerokość
                    Height = 32,//wysokość obrazka
                    Source = new BitmapImage(new Uri("wilk.png", UriKind.Relative))//// Ładowanie tekstury
                };//Ten fragment kodu to konstruktor klasy Wilk który inicjalizuje nowego wilka
            }
        }
    }
}