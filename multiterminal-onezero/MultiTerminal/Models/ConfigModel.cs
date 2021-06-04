using MultiTerminal.Connections.Models;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Xml.Serialization;

namespace MultiTerminal.Models
{
    public class ConfigModel : BaseModel
    {
        public ConfigModel()
        {
            Symbol = "EURUSD";
            SymbolId = "4001";
            Volume = 1000;
            Lot = 1;
        }

        private int _SelectedConnectionIndex;
        [XmlIgnore]
        public int SelectedConnectionIndex
        {
            get { return _SelectedConnectionIndex; }
            set { if (_SelectedConnectionIndex != value) { _SelectedConnectionIndex = value; OnPropertyChanged(); } }
        }
        private ObservableCollection<ConnectionModel> _Connections;
        [XmlIgnore]
        public ObservableCollection<ConnectionModel> Connections
        {
            get { return _Connections; }
            set { if (_Connections != value) { _Connections = value; OnPropertyChanged(); } }
        }
        private bool _Started;
        [XmlIgnore]
        public bool Started
        {
            get { return _Started; }
            set { if (_Started != value) { _Started = value; OnPropertyChanged(); } }
        }
        private bool _Closing;
        [XmlIgnore]
        public bool Closing
        {
            get { return _Closing; }
            set { if (_Closing != value) { _Closing = value; OnPropertyChanged(); } }
        }
        private string _Symbol;
        public string Symbol
        {
            get { return _Symbol; }
            set { if (_Symbol != value) { _Symbol = value; OnPropertyChanged(); } }
        }
        private string _SymbolId;
        public string SymbolId
        {
            get { return _SymbolId; }
            set { if (_SymbolId != value) { _SymbolId = value; OnPropertyChanged(); } }
        }

        private decimal _Lot;
        public decimal Lot
        {
            get { return _Lot; }
            set { if (_Lot != value) { _Lot = value; OnPropertyChanged(); } }
        }

        private decimal _Volume;
        public decimal Volume
        {
            get { return _Volume; }
            set { if (_Volume != value) { _Volume = value; OnPropertyChanged(); } }
        }

        private decimal _ViewBid;
        [XmlIgnore]
        public decimal ViewBid
        {
            get { return _ViewBid; }
            set { if (_ViewBid != value) { _ViewBid = value; OnPropertyChanged(); } }
        }
        private decimal _ViewAsk;
        [XmlIgnore]
        public decimal ViewAsk
        {
            get { return _ViewAsk; }
            set { if (_ViewAsk != value) { _ViewAsk = value; OnPropertyChanged(); } }
        }
        private decimal _ViewVolume;
        [XmlIgnore]
        public decimal ViewVolume
        {
            get { return _ViewVolume; }
            set { if (_ViewVolume != value) { _ViewVolume = value; OnPropertyChanged(); } }
        }

        public static string ConfigPathname()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string folder = System.IO.Path.GetDirectoryName(location);
            return System.IO.Path.Combine(folder, ".cfg", "main.xml");
        }
        private static XmlSerializer CreateSerializer()
        {
            return new XmlSerializer(typeof(ConfigModel));
        }
        public static ConfigModel Load()
        {
            return Load(ConfigPathname());
        }
        public static ConfigModel Load(string filename)
        {
            ConfigModel res = null;
            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open))
                {
                    var xs = CreateSerializer();
                    res = xs.Deserialize(fs) as ConfigModel;
                }
            }
            catch
            {
            }
            if (res == null) res = new ConfigModel();
            return res;
        }
        public void Save()
        {
            Save(ConfigPathname());
        }
        public void Save(string filename)
        {
            try
            {
                using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create))
                {
                    var xs = CreateSerializer();
                    xs.Serialize(fs, this);
                }
            }
            catch
            {
            }
        }
    }
}
