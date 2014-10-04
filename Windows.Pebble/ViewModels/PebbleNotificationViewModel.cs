
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;

namespace Windows.Pebble.ViewModels
{
    public class PebbleNotificationViewModel : PebbleViewModelBase
    {
        private readonly RelayCommand<NotificationTypes> _sendNotificationCommand;

        public PebbleNotificationViewModel()
        {
            _sendNotificationCommand = new RelayCommand<NotificationTypes>( OnSendNotification );
        }

        public ICommand SendNotificationCommand
        {
            get { return _sendNotificationCommand; }
        }

        private string _Sender;
        public string Sender
        {
            get { return _Sender; }
            set { Set( () => Sender, ref _Sender, value ); }
        }

        private string _Subject;
        public string Subject
        {
            get { return _Subject; }
            set { Set( () => Subject, ref _Subject, value ); }
        }

        private string _Body;
        public string Body
        {
            get { return _Body; }
            set { Set( () => Body, ref _Body, value ); }
        }

        private async void OnSendNotification( NotificationTypes notificationType )
        {
            switch ( notificationType )
            {
                case NotificationTypes.Email:
                    await _pebble.NotificationMailAsync( Sender, Subject, Body );
                    break;
                case NotificationTypes.SMS:
                    await _pebble.NotificationSMSAsync( Sender, Body );
                    break;
                case NotificationTypes.Facebook:
                    await _pebble.NotificationFacebookAsync( Sender, Body );
                    break;
                case NotificationTypes.Twitter:
                    await _pebble.NotificationTwitterAsync( Sender, Body );
                    break;
            }
        }
    }

    public enum NotificationTypes
    {
        Email,
        SMS,
        Facebook,
        Twitter
    }
}