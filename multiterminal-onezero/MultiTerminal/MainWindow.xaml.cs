namespace MultiTerminal
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Threading;
    using Arbitrage.Api.Enums;
    using MultiTerminal.Models;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.ComponentModel;
    using Arbitrage.Api.Security;
    using System.Threading.Tasks;
    using System.Windows.Documents;
    using MultiTerminal.Connections;
    using System.Collections.Concurrent;
    using MultiTerminal.Connections.Models;
    using System.Collections.ObjectModel;
    using System.Collections.Generic;

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Quotes,
        Logout
    }

    public partial class MainWindow : Window, IConnectorLogger
    {
        List<OrderInformation> ListOrders { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            sub_btn.Visibility = unsub_btn.Visibility = orders_lbl.Visibility = Visibility.Hidden;
            buClose.IsEnabled = false;
            #region Symbls init:
            symbls.Items.Add("EURUSD");
            symbls.Items.Add("AUDUSD");
            symbls.Items.Add("GBPUSD");
            symbls.Items.Add("USDCAD");
            symbls.Items.Add("USDCHF");
            symbls.Items.Add("USDJPY");
            symbls.Items.Add("AUDNZD");
            symbls.Items.Add("AUDCHF");
            symbls.Items.Add("AUDSGD");
            #endregion
        }

        #region (WndLoaded) Connect to OneZero using credentials:
        ConfigModel model;
        string logPath;
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            orders_lbl.Visibility = Visibility.Hidden;
            App.GetTmpFolder();
            App.ShowTitle();
            tbLogin.Text = "test@tm.com";
            var connections = ConnectionsModel.Load();

            if (connections.Connections.FirstOrDefault(x => x.Name.Contains("OneZero-BALAYOGESH")) == null)
            {
                //Target IP 147.160.248.73
                //Target Port 23444
                //QuoteSenderCompID BALAYOGESH_Q
                //QuoteTargetCompID ONEGMOZ_Q
                //OrderSenderCompID BALAYOGESH_T
                //OrderTargetCompID ONEGMOZ_T
                //Password eFnDAKyr79Ak

                connections.Connections.Add(new OneZeroFixConnectionModel()
                {
                    //Account = "6204067",
                    //HostQuote = "147.160.248.73",
                    //HostTrade = "147.160.248.73",
                    //Login = "BALAYOGESH",
                    //Password = "eFnDAKyr79Ak",
                    //Name = "OneZero-BALAYOGESH",
                    //PortQuote = 23444,
                    //PortTrade = 23444,
                    //SenderCompIdQuote = "BALAYOGESH_Q",
                    //SenderCompIdTrade = "BALAYOGESH_T",
                    //TargetCompIdQuote = "ONEGMOZ_Q",
                    //TargetCompIdTrade = "ONEGMOZ_T",
                    //TradeSession = true,
                    //UseLogging = true,
                    //UseSSL = false
                    // Default credentials:
                    Account = "6204067",
                    HostQuote = "216.93.241.29",
                    HostTrade = "216.93.241.29",
                    Login = "FRYTRADE",
                    Password = "T34w2qmoDS",
                    Name = "OneZeroFix - FRYTRADE",
                    PortQuote = 23985,
                    PortTrade = 23985,
                    SenderCompIdQuote = "FRYTRADE_Q",
                    SenderCompIdTrade = "FRYTRADE_T",
                    TargetCompIdQuote = "OZ_Q",
                    TargetCompIdTrade = "OZ_T",
                    TradeSession = true,
                    UseLogging = true,
                    UseSSL = true
                });
                connections.Save();
            }

            model = ConfigModel.Load();
            model.Connections = connections.Connections;

            DataContext = model;
        }
        #endregion

        #region (WndClosing) Dissconnect:
        void Window_Closing(object sender, CancelEventArgs e)
        {
            model.Closing = true;
            model.Save();
            Disconnect(false);
        }
        #endregion

        #region Log: clear, showing Info, warning, quotes, error msgs:
        public void ClearLog()
        {
            logPath = App.GetLogFolder();
            logPath = System.IO.Path.Combine(logPath, DateTime.Now.ToString("yyyy.MM.ddTHH_mm_ss") + ".log");
            rich.Text = "";
        }

        void LogMessage(LogLevel level, string message, SolidColorBrush color = null)
        {
            message += "\n";
            Run r = new Run(message)
            {
                Tag = DateTime.UtcNow,
                Foreground = Brushes.Black
            };
            if (level == LogLevel.Logout)
            {
                r.Foreground = Brushes.Red;
            }
            if (level == LogLevel.Error)
            {
                r.Foreground = Brushes.DarkOrange;
                SubLog(main_log, message); // Main Log
                return;
            }
            else if (level.Equals(LogLevel.Quotes))
            {
                qts.Foreground = color;
                //main_log.Foreground = Brushes.DarkKhaki;
                //SubLog(main_log, $" - Bid: {model.ViewBid} Ask: {model.ViewAsk}"); // for listbox
                SubLog(qts,  message, null, 0);
                return;
            }
            else if (level == LogLevel.Warning) r.Foreground = Brushes.Blue;
            else 
            {
                r.Foreground = Brushes.LightGreen;
            }
            SubLog(rich, message, r);
        }

        #region ListBox log (notusing):
        static ObservableCollection<string> instrs = new ObservableCollection<string>();
        void SubLog(System.Windows.Controls.ListBox log_name, string message)
        {
            Run r = new Run(message)
                {
                    Tag = symbls.Text
                };
            log_name.ItemsSource = instrs;
            try
            {
                if (subChk)
                {
                    instrs.Add(symbls.Text + message);
                    subChk = false;
                }
                while (instrs.Count > subQty)
                {
                    log_name.Items.RemoveAt(subQty);
                }
            }
            catch { }            
            try
            {
                System.IO.File.AppendAllText(logPath, message);
            }
            catch { }
        }
        #endregion

        void SubLog(System.Windows.Controls.TextBlock log_name, string message, Run rr = null, int symb_qty = 200)
        {
            Run r;
            if (rr == null)
            {
                r = new Run(message)
                {
                    Tag = DateTime.UtcNow,
                };
            }
            else r = rr;
            try
            {
                while (log_name.Inlines.Count > symb_qty)
                {
                    log_name.Inlines.Remove(log_name.Inlines.LastInline);
                }
            }
            catch
            {

            }
            int count = log_name.Inlines.Count;
            if (count == 0) log_name.Inlines.Add(r);
            else
            {
                log_name.Inlines.InsertBefore(log_name.Inlines.FirstInline, r);
            }
            try
            {
                System.IO.File.AppendAllText(logPath, message);
            }
            catch { }
        }

        Random rand = new Random();
        void LogMessageBeginInvoke(LogLevel level, string message)
        {
            string richMsg = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") + "> " + message + "\r\n";
            SafeInvoke(() =>
            {
                LogMessage(level, richMsg, GetColor()[rand.Next(0,9)]);
            });
        }

        SolidColorBrush[] GetColor()
        {
            return new SolidColorBrush[] // 10 -1
            {
                Brushes.Coral,
                Brushes.Green,
                Brushes.Red,
                Brushes.DarkRed,
                Brushes.Blue,
                Brushes.Aqua,
                Brushes.Aquamarine,
                Brushes.Coral,
                Brushes.BlueViolet,
                Brushes.BurlyWood
            };
        }

        public void LogError(string message)
        {
            LogMessageBeginInvoke(LogLevel.Error, message);
        }

        public void LogInfo(string message)
        {
            LogMessageBeginInvoke(LogLevel.Info, message);
        }

        public void LogWarning(string message)
        {
            LogMessageBeginInvoke(LogLevel.Warning, message);
        }

        public void LogLogout(string message)
        {
            LogMessageBeginInvoke(LogLevel.Logout, message);
        }

        public void LogQuotes(string message)
        {
            LogMessageBeginInvoke(LogLevel.Quotes, message);
        }
        #endregion

        #region Save Invoke func:
        void SafeInvoke(Action action)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                if (!model.Closing)
                {
                    action();
                }
            }));
        }
        #endregion

        #region Conn/Dissconn func:
        readonly ManualResetEvent threadStop = new ManualResetEvent(false);
        readonly ManualResetEvent threadStopped = new ManualResetEvent(false);
        void Connect()
        {
            if (model.Started) return;
            model.Started = true;
            symbls.IsEnabled = true;
            sub_btn.Visibility = unsub_btn.Visibility = Visibility.Visible;
            while (commands.Count > 0)
            {
                commands.TryDequeue(out int res);
            }
            threadStop.Reset();
            threadStopped.Reset();
            new Thread(ProcessingThread).Start();
        }

        void Disconnect(bool wait)
        {
            if (!model.Started) return;
            threadStop.Set();
            if (wait) threadStopped.WaitOne();
            LogLogout("Logout (Disconnect).");
            sub_btn.Visibility = unsub_btn.Visibility = orders_lbl.Visibility = Visibility.Hidden;
            model.Started = false;
        }
        #endregion

        #region Conn.thread, Subsc., Unsubs. func:
        IConnector connector;
        readonly ConcurrentQueue<int> commands = new ConcurrentQueue<int>();
        void ProcessingThread()
        {
            var connectorModel = model.Connections[model.SelectedConnectionIndex];
            connector = App.CreateConnector(connectorModel, this, threadStop, 1, Dispatcher);
            //connector.LoggedIn += Connector_LoggedIn;
            //connector.Tick += Connector_Tick;

            LogInfo("Logging in...");
            while (!threadStop.WaitOne(100))
            {
                if (connector.IsLoggedIn)
                {
                    LogInfo("Logged in.");
                    break;
                }
            }
            if (!threadStop.WaitOne(0))
            {
                if (connector.IsLoggedIn)
                {
                    //OnLogin();
                }
            }

            while (!threadStop.WaitOne(1))
            {
                int cmd = 0;
                while (commands.Count > 0)
                {
                    commands.TryDequeue(out cmd);
                }
                if (model.ViewBid > 0 && model.ViewAsk > 0)
                {
                    var order = connector.GetOrders(model.Symbol, 0, 0).FirstOrDefault();
                    if (order != null)
                    {
                        model.ViewVolume = order.Side == OrderSide.Buy ? order.Volume : -order.Volume;
                        if (cmd == 3)
                        {
                            connector.Close(order.Symbol, order.Id, order.Side == OrderSide.Buy ? model.ViewBid : model.ViewAsk, order.Volume, order.Side, 0, OrderType.Market, 0);
                        }
                    }
                    else
                    {
                        model.ViewVolume = 0;
                        if (cmd == 1) connector.Open(model.Symbol, model.ViewAsk, model.Volume, OrderSide.Buy, 0, 0, 0, OrderType.Market, 0);
                        if (cmd == 2) connector.Open(model.Symbol, model.ViewBid, model.Volume, OrderSide.Sell, 0, 0, 0, OrderType.Market, 0);
                    }
                }
            }
            connector.Tick -= Connector_Tick;
            connector.LoggedIn -= Connector_LoggedIn;
            ConnectorsFactory.Current.CloseConnector(connectorModel.Name, true);
            threadStopped.Set();
        }

        void OnLogin() => connector.Subscribe(model.Symbol, model.SymbolId);

        void UnSubscribe() => connector.Unsubscribe(model.Symbol, model.SymbolId);
        #endregion

        #region Buttons (EventHandlers):
        void BuConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        void BuDisconnect_Click(object sender, RoutedEventArgs e) => Disconnect(true);

        void Connector_Tick(object sender, TickEventArgs e)
        {
            model.ViewBid = e.Bid;
            model.ViewAsk = e.Ask;
            LogQuotes($"Bid: {model.ViewBid} Ask: {model.ViewAsk}");
        }      

        void Connector_LoggedIn(object sender, EventArgs e) => OnLogin();

        static bool subChk = false;
        static int subQty = -1;
        void sub_btn_Click(object sender, RoutedEventArgs e)
        {
            OnLogin(); 
            // subQty++;
            // subChk = true;
            symbls.IsEnabled = true;
            orders_lbl.Visibility = Visibility.Visible;
            connector.LoggedIn += Connector_LoggedIn;
            connector.Tick += Connector_Tick;
        }

        void unsub_btn_Click(object sender, RoutedEventArgs e)
        {
            UnSubscribe();
            orders_lbl.Visibility = Visibility.Hidden;
            connector.LoggedIn -= Connector_LoggedIn;
            connector.Tick -= Connector_Tick;
        }

        void BuBuy_Click(object sender, RoutedEventArgs e)
        {
             //var orderResult = connector.OpenOrd(symbls.Text, decimal.Parse(lot_txt.Text), OrderSide.Buy, OrderType.Market,0);
            var orderResult = connector.Open(symbls.Text, 0, decimal.Parse(lot_txt.Text), OrderSide.Buy, 0, 0, 0, OrderType.Market, 0);
            ordTbl.ItemsSource = ListOrders = connector.GetOrders(symbls.Text,0,0);
        }

        void BuSell_Click(object sender, RoutedEventArgs e)
        {
            //var orderResult = connector.OpenOrd(symbls.Text, decimal.Parse(lot_txt.Text), OrderSide.Sell, OrderType.Market, 0);
            var orderResult = connector.Open(symbls.Text, 0, decimal.Parse(lot_txt.Text), OrderSide.Sell, 0, 0, 0, OrderType.Market, 0);
            ordTbl.ItemsSource = ListOrders = connector.GetOrders(symbls.Text, 0, 0);
        }

        void BuClose_Click(object sender, RoutedEventArgs e)
        {
            if (selOrd != null)
                connector.Close(selOrd.Symbol, selOrd.Id, selOrd.OpenPrice, selOrd.Volume, selOrd.Side, 0, OrderType.Market, 0);
        }

        async void BuSendApplication_Click(object sender, RoutedEventArgs e)
        {
            buSendApplication.IsEnabled = false;
            buLogin.IsEnabled = false;
            Cursor old = Cursor;
            Cursor = Cursors.Wait;
            var sendApplicationResult = await SendApplicationTask(tbLogin.Text, false);
            SendApplicationResponse(sendApplicationResult);
            Cursor = old;
            buSendApplication.IsEnabled = true;
            buLogin.IsEnabled = true;
        }
        #endregion

        #region Login click:
        async void BuLogin_Click(object sender, RoutedEventArgs e)
        {
            buSendApplication.IsEnabled = false;
            buLogin.IsEnabled = false;
            Cursor old = Cursor;
            Cursor = Cursors.Wait;
            var loginResult = await LoginTask(tbLogin.Text);
            if (loginResult.Status == ResponseStatus.Ok)
            {
                LogInfo("Login ok from " + App.Subscription.Subscription.IpAddress);
                if (App.Subscription != null && App.Subscription.SubscriptionFeatures != null)
                {
                    foreach (var feature in App.Subscription.SubscriptionFeatures)
                    {
                        LogInfo("Subscription feature " + feature.Feature.Code + "=" + (feature.SubscriptionFeature.Value ?? ""));
                    }
                }
                if (App.Subscription != null && App.Subscription.Brokers != null)
                {
                    foreach (var broker in App.Subscription.Brokers)
                    {
                        int featuresCount = broker.BrokerFeatures != null ? broker.BrokerFeatures.Count : 0;
                        int instrumentsCount = broker.Instruments != null ? broker.Instruments.Count : 0;
                        LogInfo("Subscription broker " + broker.Broker.DisplayName + "(" + broker.Broker.DisplayName + ")" + " has " + featuresCount + " feature(s) and " + instrumentsCount + " instrument(s)");
                    }
                }
            }
            else
            {
                string error = "Login error";
                if (loginResult.Status == ResponseStatus.NotActive)
                {
                    error = "Login not active";
                }
                if (loginResult.Status == ResponseStatus.SoftwareLocationChanged)
                {
                    if (MessageBox.Show("Software Location Changed\nResend application", "Login", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var sendApplicationResult = await SendApplicationTask(tbLogin.Text, true);
                        SendApplicationResponse(sendApplicationResult);
                    }
                }
                else
                {
                    MessageBox.Show(error);
                }
            }
            Cursor = old;
            buSendApplication.IsEnabled = true;
            buLogin.IsEnabled = true;
        }
        #endregion

        #region Send App, Create Cli, Login Task..:
        void SendApplicationResponse(SendApplicationResult sendApplicationResult)
        {
            if (sendApplicationResult.Status == ResponseStatus.Ok || sendApplicationResult.Status == ResponseStatus.SoftwareLocationChanged)
            {
                MessageBox.Show("Application sent");
            }
            else
            {
                string error = "Send Application Error";
                if (sendApplicationResult.Status == ResponseStatus.AlreadyExists)
                {
                    error = "Application Already Sent";
                }
                MessageBox.Show(error);
            }
        }

        void CreateClient(string username)
        {
            if (string.IsNullOrEmpty(App.HostId))
            {
                App.HostId = ComputerId.Get();
            }
            App.Login = username;
            App.Client = new Arbitrage.Api.Clients.Client(App.ServerAddress, new Arbitrage.Api.Json.Net.ClientJsonConverter(), "TradeMonitor", username.Encrypt(App.ClientCryptoKey), App.HostId.Encrypt(App.ClientCryptoKey), "0000-0000-TEST", 1);
        }

        class LoginResult
        {
            public ResponseStatus Status { get; set; }
        }

        async Task<LoginResult> LoginTask(string username)
        {
            CreateClient(username);
            var response = await App.Client.SubscriptionLogin(true);
            if (response != null && response.Status == ResponseStatus.Ok)
            {
                App.Subscription = response;
                return new LoginResult()
                {
                    Status = response.Status
                };
            }
            else
            {
                return response != null ? new LoginResult() { Status = response.Status } : new LoginResult() { Status = ResponseStatus.Unknown };
            }
        }

        class SendApplicationResult
        {
            public ResponseStatus Status { get; set; }
        }

        async Task<SendApplicationResult> SendApplicationTask(string username, bool resetSoftwareLocation)
        {
            CreateClient(username);
            var response = await App.Client.SubscriptionRegister(resetSoftwareLocation);
            if (response != null && response.Status == ResponseStatus.Ok)
            {
                return new SendApplicationResult()
                {
                    Status = response.Status
                };
            }
            else
            {
                return response != null ? new SendApplicationResult() { Status = response.Status } : new SendApplicationResult() { Status = ResponseStatus.Unknown };
            }
        }
        #endregion

        OrderInformation selOrd = null;
        void ordTbl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            selOrd = ordTbl.SelectedItem as OrderInformation;
            if (selOrd != null) buClose.IsEnabled = true;
        }
    }
}
