using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace MultiTerminal.Connections.Models
{
    public partial class ConnectionModel : INotifyPropertyChanged
    {
        private string _Login;
        public string Login
        {
            get { return _Login; }
            set { if (_Login != value) { _Login = value; FillName();  OnPropertyChanged(); } }
        }
        private string _Password;
        public string Password
        {
            get { return _Password; }
            set { if (_Password != value) { _Password = value; OnPropertyChanged(); } }
        }
        private string _Account;
        public string Account
        {
            get { return _Account; }
            set { if (_Account != value) { _Account = value; FillName();  OnPropertyChanged(); } }
        }
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { if (_Name != value) { _Name = value; OnPropertyChanged(); } }
        }
        public virtual string UIShortName()
        {
            return Login;
        }
        public void FillName()
        {
            string suffix = Login;
            if (string.IsNullOrEmpty(suffix)) suffix = Account;
            string className = GetType().Name;
            int pos = className.IndexOf("ConnectionModel");
            string name = pos > 0 ? className.Substring(0, pos) : className;
            Name = string.IsNullOrEmpty(suffix) ? name : name + " - " + suffix;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected virtual void OnPropertyChanged<TProperty>(Expression<Func<TProperty>> projection)
        {
            var memberExpression = (MemberExpression)projection.Body;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberExpression.Member.Name));
        }
        public virtual void From(ConnectionModel other)
        {
            Login = other.Login;
            Password = other.Password;
            Account = other.Account;
            Name = other.Name;
        }
        public virtual string SaveConfig()
        {
            return null;
        }
    }
}