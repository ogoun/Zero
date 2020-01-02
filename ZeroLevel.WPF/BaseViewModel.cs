using System;
using System.ComponentModel;

namespace ZeroLevel.WPF
{
    public abstract class BaseViewModel 
        : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = this.PropertyChanged;
            if (null != handler)
            {
                this.VerifyPropertyName(propertyName);
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        public void VerifyPropertyName(string propertyName)
        {
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                throw new ArgumentException("Invalid property name", propertyName);
            }
        }
    }
}
