using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;

namespace WPFFrontendChatClient.ViewModel
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<MainViewModel>();
            // SimpleIoc.Default.Register<NetworkService>();
        }

        public MainViewModel Main
        {
            get { return ServiceLocator.Current.GetInstance<MainViewModel>(); }
        }

        /*public NetworkService Network
        {
            get { return ServiceLocator.Current.GetInstance<NetworkService>(); }
        }*/

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}