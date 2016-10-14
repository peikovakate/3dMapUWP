using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Map1
{
    public sealed partial class CardsPanel : UserControl
    {
        const int CardsLength = 17;

        public CardsPanel()
        {
            this.InitializeComponent();
            for(int i=0; i<CardsLength; i++)
            {
                var listViewItem = new ListViewItem();
                listViewItem.Height = 200;
                listViewItem.HorizontalContentAlignment = HorizontalAlignment.Center;

                Image card = new Image();
                card.Source = new BitmapImage(new Uri("ms-appx:///Cards/Card_" + (i + 1).ToString() + ".jpg"));
                card.Width = 200;
                card.Height = 180;

                listViewItem.Content = card;

                CardsList.Items.Add(listViewItem);
            }
        }

        public void SlideOutBegin()
        {
            SlideOut.Begin();
        }

        public void SlideInBegin()
        {
            CardsList.Visibility = Visibility.Collapsed;
            Text.Visibility = Visibility.Visible;
            SlideIn.Begin();
        }


        public delegate void OnChooseCard(object sender, EventArgs e);
        public event OnChooseCard ChooseCard;

        public delegate void OnCardClick(object sender, EventArgs e);
        public event OnCardClick CardClicked;

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            CardsList.Visibility = Visibility.Visible;
            Text.Visibility = Visibility.Collapsed;
            ChooseCard?.Invoke(null, new EventArgs());
        }

        public int cardNumber;



        private void CardsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cardNumber = CardsList.SelectedIndex;
            CardClicked?.Invoke(null, new EventArgs());
        }
    }
}
