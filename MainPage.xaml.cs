using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace StylusTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = new MainPageVM();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Windows.UI.Popups.MessageDialog(BluetoothDevices.SelectedItem.ToString());
            dialog.ShowAsync();
        }
    }
}
