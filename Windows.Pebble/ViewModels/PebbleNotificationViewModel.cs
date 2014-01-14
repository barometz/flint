
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace Windows.Pebble.ViewModels
{
    public class PebbleNotificationViewModel : PebbleViewModelBase
    {
        private readonly RelayCommand _smsCommand;
        private readonly RelayCommand _emailCommand;

        public PebbleNotificationViewModel()
        {
            _smsCommand = new RelayCommand(OnSMSCommand);
            _emailCommand = new RelayCommand(OnEmailCommand);
        }

        public ICommand SMSCommand
        {
            get { return _smsCommand; }
        }

        public ICommand EMailCommand
        {
            get { return _emailCommand; }
        }

        private string _Sender;
        public string Sender
        {
            get { return _Sender; }
            set { Set(() => Sender, ref _Sender, value); }
        }

        private string _Subject;
        public string Subject
        {
            get { return _Subject; }
            set { Set(() => Subject, ref _Subject, value); }
        }

        private string _Body;
        public string Body
        {
            get { return _Body; }
            set { Set(() => Body, ref _Body, value); }
        }

        private void OnEmailCommand()
        {

        }

        private void OnSMSCommand()
        {

        }
    }
}