namespace MultiTerminal.Models
{
    using System;

    public class OrderModel : BaseModel
    {
        private string _Symbol;
        public string Symbol 
        {
            get { return _Symbol; }
            set { if (_Symbol != value) { _Symbol = value; OnPropertyChanged(); } }
        }

        private int _Ticket;
        public int Ticket
        {
            get { return _Ticket; }
            set { if (_Ticket != value) { _Ticket = value; OnPropertyChanged(); } }
        }

        private DateTime _Time;
        public DateTime Time
        {
            get { return _Time; }
            set { if (_Time != value) { _Time = value; OnPropertyChanged(); } }
        }

        private string _OrderTipe;
        public string OrderType
        {
            get { return _OrderTipe; }
            set { if (_OrderTipe != value) { _OrderTipe = value; OnPropertyChanged(); } }
        }

        private decimal _Lot;
        public decimal Lot
        {
            get { return _Lot; }
            set { if (_Lot != value) { _Lot = value; OnPropertyChanged(); } }
        }

        private decimal _PriceBid;
        public decimal PriceBid
        {
            get { return _PriceBid; }
            set { if (_PriceBid != value) { _PriceBid = value; OnPropertyChanged(); } }
        }

        private decimal _PriceAsk;
        public decimal PriceAsk
        {
            get { return _PriceAsk; }
            set { if (_PriceAsk != value) { _PriceAsk = value; OnPropertyChanged(); } }
        }

        private decimal _Profit;
        public decimal Profit
        {
            get { return _Profit; }
            set { if (_Profit != value) { _Profit = value; OnPropertyChanged(); } }
        }
    }
}
