using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GreenwayPlaces
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        private TimeSpan FAST = new TimeSpan(1);
        private TimeSpan MODERATE = new TimeSpan(10000);
        private TimeSpan SLOW = new TimeSpan(50000);
        private TimeSpan DAMNSLOW = new TimeSpan(500000);

        // status variables:
        private bool m_b_Running = false;
        private bool m_b_CityQueryShowing = false;
        private bool m_b_FadingIn = false;
        private int m_CityNum_QueryShowing = -1;
        private int m_CityNum_QueryShowingWhenClicked = -1;

        int m_NumCities = -1;
        int m_NumCitiesShown = 0;
        int m_NumCitiesDrawn = 0;
        List<string> m_CityStates = new List<string>();
        List<string> m_CityTitles = new List<string>();
        List<int> m_CityXCoords = new List<int>();
        List<int> m_CityYCoords = new List<int>();
        List<bool> m_b_CityQueryWasShown = new List<bool>();
        List<bool> m_b_CityDrawn = new List<bool>();

        int m_TickCount = 0;
        int m_TickCount_WhenQueryStarted = -1;

        int m_Score = 0;

        Random rnd = new Random();


        public Window1()
        {
            InitializeComponent();

            // Set up the timer:
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);

            /* Here user can change the speed of the snake. 
             * Possible speeds are FAST, MODERATE, SLOW and DAMNSLOW */
            timer.Interval = MODERATE;
            timer.Start();

            lbl_CityQuery.Opacity = 0;

            // Maximize this window:
            this.WindowState = System.Windows.WindowState.Maximized;
        }


        #region Events

        private void oCanvas_01_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            int i_LocationClicked_Y = (int)e.GetPosition(oCanvas_01).Y;
            int i_LocationClicked_X = (int)e.GetPosition(oCanvas_01).X;

            // DEBUG //
            //MessageBox.Show("You clicked " + i_LocationClicked_Y + ", " + i_LocationClicked_X);

            if (m_b_CityQueryShowing)
            {
                m_CityNum_QueryShowingWhenClicked = m_CityNum_QueryShowing;

                int i_LocationAnswer_Y = m_CityYCoords[m_CityNum_QueryShowing - 1];
                int i_LocationAnswer_X = m_CityXCoords[m_CityNum_QueryShowing - 1];

                int ErrorDistance = (int)Math.Sqrt(Math.Pow((i_LocationAnswer_Y - i_LocationClicked_Y), 2) + Math.Pow((i_LocationAnswer_X - i_LocationClicked_X), 2));
                int QueryDuration = m_TickCount - m_TickCount_WhenQueryStarted;
                
                // Calculate the points for this guess:
                int PointsAdded = 350 - ErrorDistance * 2 - QueryDuration;
                if (PointsAdded < 10) PointsAdded = 10;

                // Update the labels:
                m_Score += PointsAdded;
                lbl_Score.Content = "Score " + m_Score;

                string str_CityName = m_CityTitles[m_CityNum_QueryShowingWhenClicked - 1] + ", " + m_CityStates[m_CityNum_QueryShowingWhenClicked - 1];

                // Add to the recap:
                list_Recap.Items.Add(str_CityName + ", " + PointsAdded + " points");

                // DEBUG //
                //MessageBox.Show("You clicked " + i_LocationClicked_Y + ", " + i_LocationClicked_X + "\nError = " + ErrorDistance + ", Duration = " + QueryDuration + "\n" + PointsAdded + " points added");

                DrawCityOnMap(true);
                m_b_CityDrawn[m_CityNum_QueryShowingWhenClicked - 1] = true;
                m_NumCitiesDrawn += 1;

                // DEBUG //
                //lbl_Info.Content = m_NumCitiesDrawn + " cities drawn.";

                if (m_NumCitiesShown > 0 && m_NumCitiesShown >= m_NumCities)
                { // If ALL cities have been shown to the player ...
                    lbl_CityQuery.Text = "GAME OVER";
                    m_b_Running = false;
                    btn_Go.IsChecked = false;
                }

                m_b_CityQueryShowing = false;
                m_CityNum_QueryShowing = -1;
                m_b_FadingIn = false;
                lbl_CityQuery.Opacity = 0;
            }
        }

        private void btn_Go_Click(object sender, RoutedEventArgs e)
        {
            if (btn_Go.IsChecked == true)
            {
                m_b_Running = true;
                m_b_CityQueryShowing = false;
                m_b_FadingIn = false;
                lbl_CityQuery.Opacity = 0;
                lbl_Score.Visibility = System.Windows.Visibility.Visible;

                ReadCities();
            }
            else
            {
                m_b_Running = false;
                m_b_CityQueryShowing = false;
                m_b_FadingIn = false;
                lbl_CityQuery.Opacity = 0;
                lbl_Score.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        /// <summary>
        /// Timer tick event, which occurs several times per second
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            m_TickCount++;

            // DEBUG //
            //lbl_Info.Content = m_NumCitiesDrawn + " cities drawn";

            if (m_NumCitiesShown > 0 && m_NumCitiesShown >= m_NumCities)
            { // If ALL cities have been shown to the player ...
                lbl_CityQuery.Text = "GAME OVER";
                lbl_CityQuery.Foreground = new SolidColorBrush(Colors.OrangeRed);
                lbl_CityQuery.Opacity = 1;
                m_b_Running = false;
                btn_Go.IsChecked = false;
            }
            else
            { // If the game is not complete ...
                if (m_b_Running)
                {
                    // Update the caslon:
                    lbl_Caslon.Content += "|";
                    if (lbl_Caslon.Content.ToString().Length > 100) lbl_Caslon.Content = "";

                    // Continue fading in the instructions, if applicable:
                    if (m_b_FadingIn)
                    {
                        // TEST //
                        if (lbl_CityQuery.Opacity < .2) lbl_CityQuery.Opacity = .2;

                        lbl_CityQuery.Opacity += .01;
                        if (lbl_CityQuery.Opacity == 1)
                        {
                            m_b_FadingIn = false;
                        }
                    }

                    if (m_TickCount % 300 == 250)
                    { // If this is a special tick event ...
                        lbl_Caslon.Content += "=";

                        // Pick a city at random, excluding cities already queried:
                        do
                        {
                            m_CityNum_QueryShowing = rnd.Next(1, m_NumCities + 1);
                        }
                        while (m_b_CityQueryWasShown[m_CityNum_QueryShowing - 1] == true);

                        // DEBUG //
                        lbl_Info.Content += m_CityNum_QueryShowing + ", ";

                        lbl_CityQuery.Text = "Click on " + m_CityTitles[m_CityNum_QueryShowing - 1] + ", " + m_CityStates[m_CityNum_QueryShowing - 1] + ".";

                        m_b_CityQueryShowing = true;
                        m_b_FadingIn = true;
                        m_TickCount_WhenQueryStarted = m_TickCount;
                        m_b_CityQueryWasShown[m_CityNum_QueryShowing - 1] = true;
                        m_NumCitiesShown += 1;
                    } // ... if this is a special tick event.
                }
            } // ... if the game is not complete.
        }

        #endregion Events


        #region Private Methods

        private void ReadCities()
        {
            var t_01 = Properties.Settings.Default;
            string str_PathedFileName_DataFile_Cities = t_01.PathedFileName_DataFile_Cities;


            // DEBUG //
            //lbl_Info.Content = str_PathedFileName_DataFile_Cities;

            XmlDocument doc = new XmlDocument();
            doc.Load(str_PathedFileName_DataFile_Cities);
            XmlNode node_Cities = doc.FirstChild;

            m_NumCities = node_Cities.ChildNodes.Count;

            // Read the city data:
            for (int i = 0; i < m_NumCities; i++)
            {
                XmlNode node_City = node_Cities.ChildNodes[i];

                XmlNode node_CityState = node_City.ChildNodes[0];
                XmlNode node_CityTitle = node_City.ChildNodes[1];
                XmlNode node_CityYCoord = node_City.ChildNodes[2];
                XmlNode node_CityXCoord = node_City.ChildNodes[3];

                m_CityStates.Add(node_CityState.InnerText);
                m_CityTitles.Add(node_CityTitle.InnerText);
                m_CityYCoords.Add(Convert.ToInt32(node_CityYCoord.InnerText));
                m_CityXCoords.Add(Convert.ToInt32(node_CityXCoord.InnerText));
                m_b_CityDrawn.Add(false);
                m_b_CityQueryWasShown.Add(false);
            }
        }

        private void DrawCityOnMap(bool Found)
        {
            string CityState = m_CityStates[m_CityNum_QueryShowingWhenClicked - 1];
            string CityTitle = m_CityTitles[m_CityNum_QueryShowingWhenClicked - 1];
            int CityYCoord = m_CityYCoords[m_CityNum_QueryShowingWhenClicked - 1];
            int CityXCoord = m_CityXCoords[m_CityNum_QueryShowingWhenClicked - 1];

            Color color_OuterMarker;

            // Create the marker:
            Color color_InnerMarker = Colors.Yellow;
            if (Found)
            {
                color_OuterMarker = Colors.Red;
            }
            else
            {
                color_OuterMarker = Colors.Black;
            }
            Ellipse oEllipse_01 = new Ellipse();
            oEllipse_01.Height = 14;
            oEllipse_01.Width = 14;
            RadialGradientBrush rgb = new RadialGradientBrush();
            GradientStop gsa = new GradientStop();
            oEllipse_01.StrokeThickness = 3;
            oEllipse_01.StrokeDashArray = new DoubleCollection() { 6, 6 };
            gsa.Color = color_InnerMarker;
            gsa.Offset = 0;
            rgb.GradientStops.Add(gsa);
            GradientStop gsb = new GradientStop();
            gsb.Color = color_OuterMarker;
            gsb.Offset = 1;
            rgb.GradientStops.Add(gsb);
            oEllipse_01.Fill = rgb;

            // Place the marker:
            Canvas.SetTop(oEllipse_01, CityYCoord - 7);
            Canvas.SetLeft(oEllipse_01, CityXCoord - 7);

            // Draw the marker:
            oCanvas_01.Children.Add(oEllipse_01);

            // Draw the text:
            DrawText(CityYCoord - 10, CityXCoord + 15, CityTitle + ", " + CityState, Colors.Goldenrod);
        }

        private void DrawText(double y, double x, string text, Color color)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.Foreground = new SolidColorBrush(color);
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.FontSize = 11;

            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            oCanvas_01.Children.Add(textBlock);
        }

        #endregion Private Methods
    }
}
