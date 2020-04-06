using System.Windows;
using CommonServiceLocator;
using WPFFrontendChatClient.ViewModel;

namespace WPFFrontendChatClient.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher =
                Application.Current.Dispatcher;
        }
    }
}