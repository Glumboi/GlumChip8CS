using GlumChip8.GUI.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace GlumChip8.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FluentWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
        }

        private void Play_Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;

            if (button?.DataContext is KeyValuePair<string, string> selectedRom)
            {
                string romPath = selectedRom.Value;

                var viewModel = (MainViewModel)this.DataContext;
                viewModel.CurrentRomPath = romPath;
                viewModel.StartEmulation();
            }
        }
    }
}