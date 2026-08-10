using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace imageViewer
{
    /// <summary>
    /// Interaktionslogik für SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // This property bridges your UI TextBox directly to your Application Resource
        public double CurrentFontSize
        {
            get => (double)Application.Current.Resources["AppFontSize"];
            set
            {
                Application.Current.Resources["AppFontSize"] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentFontSize)));
            }
        }

        public SettingsWindow()
        {
            DataContext = this; // Set the DataContext to this instance for data binding
            InitializeComponent();
        }

        private void SetLightTheme_Click(object sender, RoutedEventArgs e)
        {
            SwitchTheme(false);
        }

        private void SetDarkTheme_Click(object sender, RoutedEventArgs e)
        {
            SwitchTheme(true);
        }

        private void SwitchTheme(bool isDark)
        {
            var dict = new ResourceDictionary();
            dict.Source = isDark
                ? new Uri("DarkTheme.xaml", UriKind.Relative)
                : new Uri("LightTheme.xaml", UriKind.Relative);

            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }

        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            CurrentFontSize = Math.Max(8.0, CurrentFontSize - 2.0); // Keeps it from going below 8
        }

        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            CurrentFontSize += 2.0;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SetMidnightTheme_Click(object sender, RoutedEventArgs e)
        {
            var dict = new ResourceDictionary();
            dict.Source = new Uri("MidnightTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }


    }
}
