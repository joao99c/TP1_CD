using System.Windows;
using WPFFrontendChatClient.ViewModel;

namespace WPFFrontendChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            CommonServiceLocator.ServiceLocator.Current.GetInstance<MainViewModel>().MainDispatcher = Application.Current.Dispatcher;
        }
    }
}