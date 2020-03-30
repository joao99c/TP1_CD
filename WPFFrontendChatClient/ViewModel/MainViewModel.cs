using System;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using CommonServiceLocator;
using GalaSoft.MvvmLight;

namespace WPFFrontendChatClient.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            
            /*UserObservable = new ObservableCollection<User>();
            UserObservable.Add(
                new User() { 
                    Username = "michael", 
                    Password = "123456" 
                }
            );
            AddUserCommand = new RelayCommand(AddUserAction);
            ConnectCommand = new RelayCommand(ConnectAction);
            IpAddress = "192.168.1.8";
            Port = "15000";*/
        }
        
        public Dispatcher MainDispatcher { get; set; }
        // public string Username { get; set; }
        // public string Password { get; set; }
        // public string IpAddress { get; set; }
        // public string Port { get; set; }
        // public ObservableCollection<User> UserObservable { get; set; }
        
        // public ICommand AddUserCommand { get; set; }
        // public ICommand ConnectCommand { get; set; }

        /*private void AddUserAction()
        {
            User user = new User()
            {
                Username = Username,
                Password = Password
            };
            UserObservable.Add(user);
        }*/

        /*private void ConnectAction()
        {
            NetworkService networkService = ServiceLocator.Current.GetInstance<NetworkService>();
            networkService.IpAddress = IpAddress;
            networkService.Port = int.Parse(Port);
            Thread networkServiceThread = new Thread(networkService.Start);
            networkServiceThread.Start();
        }*/

        /*public void AddUser(User user)
        {
            MainDispatcher.Invoke(new Action(() => {
                UserObservable.Add(user);
            }));
        }*/
    }
}