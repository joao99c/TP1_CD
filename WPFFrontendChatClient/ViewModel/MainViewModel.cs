using System.Threading;
using System.Windows.Threading;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using WPFFrontendChatClient.Service;

namespace WPFFrontendChatClient.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
        }

        /// <summary>
        /// Ação de conexão de utilizador
        /// </summary>
        /// <param name="utilizador">Utilizador a conectar</param>
        /// <typeparam name="T">Tipo de Utilizador</typeparam>
        public void ConnectAction<T>(T utilizador)
        {
            ServerConnectService networkService = ServiceLocator.Current.GetInstance<ServerConnectService>();
            
            //Router Helder
            // networkService.IpAddress = "tp1cd.ddns.net";
            
            //Local
            networkService.IpAddress = "192.168.1.4";
            networkService.Port = int.Parse("1000");
            Thread networkServiceThread = new Thread(() => networkService.Start(utilizador));
            networkServiceThread.Start();
        }

        public Dispatcher MainDispatcher { get; set; }
    }
}