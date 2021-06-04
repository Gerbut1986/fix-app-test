using System.Reflection;
using System.Text;

namespace MultiTerminal.Connections.Models
{
    public class FixConnectionModel : ConnectionModel
    {
        public FixConnectionModel() : base()
        {
            TradeSession = true;
            UseLogging = true;
            UseSSL = true;
        }
        private bool _TradeSession;
        public bool TradeSession
        {
            get { return _TradeSession; }
            set { if (_TradeSession != value) { _TradeSession = value; OnPropertyChanged(); } }
        }
        private bool _UseLogging;
        public bool UseLogging
        {
            get { return _UseLogging; }
            set { if (_UseLogging != value) { _UseLogging = value; OnPropertyChanged(); } }
        }
        private bool _UseSSL;
        public bool UseSSL
        {
            get { return _UseSSL; }
            set { if (_UseSSL != value) { _UseSSL = value; OnPropertyChanged(); } }
        }
        private string _SenderCompIdQuote;
        public string SenderCompIdQuote
        {
            get { return _SenderCompIdQuote; }
            set { if (_SenderCompIdQuote != value) { _SenderCompIdQuote = value; OnPropertyChanged(); } }
        }
        private string _SenderCompIdTrade;
        public string SenderCompIdTrade
        {
            get { return _SenderCompIdTrade; }
            set { if (_SenderCompIdTrade != value) { _SenderCompIdTrade = value; OnPropertyChanged(); } }
        }
        private string _TargetCompIdQuote;
        public string TargetCompIdQuote
        {
            get { return _TargetCompIdQuote; }
            set { if (_TargetCompIdQuote != value) { _TargetCompIdQuote = value; OnPropertyChanged(); } }
        }
        private string _TargetCompIdTrade;
        public string TargetCompIdTrade
        {
            get { return _TargetCompIdTrade; }
            set { if (_TargetCompIdTrade != value) { _TargetCompIdTrade = value; OnPropertyChanged(); } }
        }

        private string _HostQuote;
        public string HostQuote
        {
            get { return _HostQuote; }
            set { if (_HostQuote != value) { _HostQuote = value; OnPropertyChanged(); } }
        }
        private string _HostTrade;
        public string HostTrade
        {
            get { return _HostTrade; }
            set { if (_HostTrade != value) { _HostTrade = value; OnPropertyChanged(); } }
        }

        private int _PortQuote;
        public int PortQuote
        {
            get { return _PortQuote; }
            set { if (_PortQuote != value) { _PortQuote = value; OnPropertyChanged(); } }
        }
        private int _PortTrade;
        public int PortTrade
        {
            get { return _PortTrade; }
            set { if (_PortTrade != value) { _PortTrade = value; OnPropertyChanged(); } }
        }
        public override void From(ConnectionModel other)
        {
            base.From(other);
            if (other is FixConnectionModel fm)
            {
                UseSSL = fm.UseSSL;
                UseLogging = fm.UseLogging;
                TradeSession = fm.TradeSession;
                SenderCompIdQuote = fm.SenderCompIdQuote;
                TargetCompIdQuote = fm.TargetCompIdQuote;
                HostQuote = fm.HostQuote;
                PortQuote = fm.PortQuote;
                SenderCompIdTrade = fm.SenderCompIdTrade;
                TargetCompIdTrade = fm.TargetCompIdTrade;
                HostTrade = fm.HostTrade;
                PortTrade = fm.PortTrade;
            }
        }
        
        protected static string SaveConfig(string connectionName,string fixCfgText,FixConnectionModel model)
        {
            if (model.UseSSL)
            {
                fixCfgText = fixCfgText.Replace("!SSL!", "SSLEnable=Y\r\nSSLValidateCertificates=N\r\nCertificateVerifyLevel=0\r\nSSLCertificate=.cfg/.ssl/QuickFixn-TestClient.pfx\r\nSSLCertificatePassword=QuickFixn-TestClient\r\nSSLCACertificate =.cfg/.ssl/QuickFixn-TestCA.cer\r\n");
            }
            else
            {
                fixCfgText = fixCfgText.Replace("!SSL!", "");
            }
            fixCfgText = fixCfgText.Replace("!HOSTTRADE!", model.HostTrade);
            fixCfgText = fixCfgText.Replace("!PORTTRADE!", model.PortTrade.ToString());
            fixCfgText = fixCfgText.Replace("!TARGETCOMPIDTRADE!", model.TargetCompIdTrade);
            fixCfgText = fixCfgText.Replace("!SENDERCOMPIDTRADE!", model.SenderCompIdTrade);
            fixCfgText = fixCfgText.Replace("!HOSTQUOTE!", model.HostQuote);
            fixCfgText = fixCfgText.Replace("!PORTQUOTE!", model.PortQuote.ToString());
            fixCfgText = fixCfgText.Replace("!TARGETCOMPIDQUOTE!", model.TargetCompIdQuote);
            fixCfgText = fixCfgText.Replace("!SENDERCOMPIDQUOTE!", model.SenderCompIdQuote);
            if (model.UseLogging)
            {
                fixCfgText = fixCfgText.Replace("!LOGGING!", "FileLogPath =.tmp");
            }
            else
            {
                fixCfgText = fixCfgText.Replace("!LOGGING!", "");
            }

            if (!model.TradeSession)
            {
                int n = fixCfgText.IndexOf("[SESSION]");
                if (n>0)
                {
                    n = fixCfgText.IndexOf("[SESSION]", n + 10);
                    if (n>0)
                    {
                        fixCfgText = fixCfgText.Substring(0,n);
                    }
                }
            }

            string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            path = System.IO.Path.Combine(path, ".cfg", ".fix");
            try
            {
                System.IO.Directory.CreateDirectory(path);
            }
            catch
            {
            }
            path = System.IO.Path.Combine(path, connectionName.ToLower() + ".cfg");
            try
            {
                System.IO.File.WriteAllBytes(path, Encoding.UTF8.GetBytes(fixCfgText));
            }
            catch
            {
            }
            return path;
        }
    }
}
