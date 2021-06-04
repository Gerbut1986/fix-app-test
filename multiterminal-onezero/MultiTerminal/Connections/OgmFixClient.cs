using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuickFix;
using QuickFix.Fields;

namespace MultiTerminal.Connections
{
    internal class OgmFixClient : IApplication, IConnector
    {
        readonly string login;
        readonly string password;
        readonly string config;
        readonly string account;
        uint requestId;
        QuickFix.Transport.SocketInitiator initiator;
        readonly Dictionary<string, TickEventArgs> smbToQuote = new Dictionary<string, TickEventArgs>();
        SessionID lastLoggedSessionIdMD;
        SessionID lastLoggedSessionIdTR;
        readonly object tradeLock = new object();
        readonly ManualResetEvent tradeResultReceived = new ManualResetEvent(false);
        OrderInformation tradeResult;
        readonly List<OrderInformation> Positions = new List<OrderInformation>();
        int initialPositionsCounter;
        int initialPositionsCount;
        readonly ManualResetEvent cancelToken;
        readonly IConnectorLogger logger;
        public string ViewId => "OGM FIX" + login;
        public FillPolicy Fill { get; set; }
        public decimal? Balance => null;
        public decimal? Equity => null;

        public OgmFixClient(IConnectorLogger logger, ManualResetEvent cancelToken, string login, string password, string account, string config)
        {
            this.logger = logger;
            this.cancelToken = cancelToken;
            this.login = login;
            this.password = password;
            this.config = config;
            this.account = account;
            requestId = 1;
            initialPositionsCount = -1;
            initialPositionsCounter = 0;
        }

        public void Start()
        {
            if (initiator == null)
            {
                try
                {
                    SessionSettings settings = new SessionSettings(config);
                    IMessageStoreFactory storeFactory = new FileStoreFactory(settings);
                    ILogFactory logFactory = new FileLogFactory(settings);
                    initiator = new QuickFix.Transport.SocketInitiator(this, storeFactory, settings, logFactory);
                    initiator.Start();
                }
                catch (Exception e)
                {
                    logger.LogError(ViewId + " " + e.Message);
                }
            }
        }
        public bool IsLoggedIn
        {
            get
            {
                lock (smbToQuote)
                {
                    return lastLoggedSessionIdMD != null && lastLoggedSessionIdTR != null && initialPositionsCount == initialPositionsCounter;
                }
            }
        }
        public DateTime CurrentTime => DateTime.UtcNow;

        public void Subscribe(string symbol, string id)
        {
            lock (smbToQuote)
            {
                if (lastLoggedSessionIdMD != null)
                {
                    if (!smbToQuote.ContainsKey(symbol))
                    {
                        var quote = new TickEventArgs
                        {
                            Symbol = symbol,
                            SymbolId = id,
                            SubscriptionId = GenerateClOrdId()
                        };
                        smbToQuote[symbol] = quote;

                        QuickFix.FIX44.MarketDataRequest message = new QuickFix.FIX44.MarketDataRequest(
                            new MDReqID(quote.SubscriptionId),
                            new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
                            new MarketDepth(1)
                            );
                        message.SetField(new MDUpdateType(MDUpdateType.FULL_REFRESH));
                        QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group1 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
                        group1.Set(new MDEntryType(MDEntryType.BID));
                        message.AddGroup(group1);
                        group1.Set(new MDEntryType(MDEntryType.OFFER));
                        message.AddGroup(group1);

                        QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup group2 = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();
                        group2.Set(new Symbol(symbol));
                        message.AddGroup(group2);

                        try
                        {
                            Session.SendToTarget(message, lastLoggedSessionIdMD);
                        }
                        catch(Exception e)
                        {
                            logger.LogError(ViewId + " " + e.Message);
                        }
                    }
                }
            }
        }
        public void Unsubscribe(string symbol, string id)
        {
            lock (smbToQuote)
            {
                if (lastLoggedSessionIdMD != null)
                {
                    if (smbToQuote.ContainsKey(symbol))
                    {
                        var quote = smbToQuote[symbol];
                        QuickFix.FIX44.MarketDataRequest message = new QuickFix.FIX44.MarketDataRequest(
                            new MDReqID(quote.SubscriptionId),
                            new SubscriptionRequestType(SubscriptionRequestType.DISABLE_PREVIOUS_SNAPSHOT_PLUS_UPDATE_REQUEST),
                            new MarketDepth(1)
                            );
                        QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup group1 = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
                        group1.Set(new MDEntryType(MDEntryType.BID));
                        message.AddGroup(group1);
                        group1.Set(new MDEntryType(MDEntryType.OFFER));
                        message.AddGroup(group1);

                        QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup group2 = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();
                        group2.Set(new Symbol(symbol));
                        message.AddGroup(group2);

                        try
                        {
                            Session.SendToTarget(message, lastLoggedSessionIdMD);
                        }
                        catch(Exception e)
                        {
                            logger.LogError(ViewId + " " + e.Message);
                        }
                    }
                }
            }
        }
        public void Stop(bool wait)
        {
            if (initiator != null)
            {
                try
                {
                    initiator.Stop(!wait);
                    initiator.Dispose();
                    initiator = null;
                    lastLoggedSessionIdMD = null;
                    lastLoggedSessionIdTR = null;
                }
                catch(Exception e)
                {
                    logger.LogError(ViewId + " " + e.Message);
                }
            }
        }

        public event EventHandler LoggedIn;
        public event EventHandler<TickEventArgs> Tick;
        public event EventHandler LoggedOut;

        string NextRequestID()
        {
            if (requestId == 65535) requestId = 1;
            requestId++;
            return requestId.ToString();
        }
        string GenerateClOrdId()
        {
            string result = DateTime.UtcNow.ToString("yyyyMMddHHmmss" + uniqueOrderCounter.ToString("00"));
            uniqueOrderCounter++;
            if (uniqueOrderCounter >= 100) uniqueOrderCounter = 0;
            return result;
        }
        int uniqueOrderCounter;

        void GetPositionsRequest()
        {
            QuickFix.FIX44.RequestForPositions request = new QuickFix.FIX44.RequestForPositions(
                new PosReqID(NextRequestID()),
                new PosReqType(PosReqType.POSITIONS),
                new Account(account),
                new AccountType(AccountType.ACCOUNT_IS_CARRIED_ON_CUSTOMER_SIDE_OF_BOOKS),
                new ClearingBusinessDate(DateTime.Now.ToString("yyyyMMdd-HH:mm:ss")),
                new TransactTime(DateTime.UtcNow, true));
            request.SetField(new NoPartyIDs(0));
            //request.SetField(new IntField(5003, -1));
            try
            {
                Session.SendToTarget(request, lastLoggedSessionIdTR);
            }
            catch(Exception e)
            {
                logger.LogError(ViewId + " " + e.Message);
            }
        }

        #region IApplication
        public void OnCreate(SessionID sessionID)
        {
        }
        public void OnLogon(SessionID sessionID)
        {
            bool loggedIn = false;
            lock (smbToQuote)
            {
                if (sessionID.TargetCompID.EndsWith("Prices")) lastLoggedSessionIdMD = sessionID;
                else lastLoggedSessionIdTR = sessionID;
                loggedIn = lastLoggedSessionIdMD != null && lastLoggedSessionIdTR != null;
            }
            if (loggedIn)
            {
                GetPositionsRequest();
            }
        }
        public void OnLogout(SessionID sessionID)
        {
            lock (smbToQuote)
            {
                if (sessionID.TargetCompID.EndsWith("Prices"))
                {
                    lastLoggedSessionIdMD = null;
                }
                else
                {
                    lastLoggedSessionIdTR = null;
                }
                smbToQuote.Clear();
                initialPositionsCount = -1;
                initialPositionsCounter = 0;
                Positions.Clear();
            }
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }
        public void FromAdmin(Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
        }
        public void ToAdmin(Message message, SessionID sessionID)
        {
            string value = message.Header.GetField(Tags.MsgType);
            if (value == "A")
            {
                message.SetField(new Password(password));
                message.SetField(new Username(login));
                message.SetField(new ResetSeqNumFlag(true));
            }
        }
        public void FromApp(Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
        }
        public void ToApp(Message message, SessionID sessionID)
        {
        }
        #endregion

        void Crack(Message message, SessionID sessionID)
        {
            if (message is QuickFix.FIX44.MarketDataSnapshotFullRefresh)
            {
                OnQuoteReceive(message as QuickFix.FIX44.MarketDataSnapshotFullRefresh, sessionID);
            }
            else if (message is QuickFix.FIX44.PositionReport)
            {
                OnPositionReport(message as QuickFix.FIX44.PositionReport, sessionID);
            }
            else if (message is QuickFix.FIX44.RequestForPositionsAck)
            {
                OnRequestForPositionAck(message as QuickFix.FIX44.RequestForPositionsAck, sessionID);
            }
            else if (message is QuickFix.FIX44.ExecutionReport)
            {
                OnExecutionReport(message as QuickFix.FIX44.ExecutionReport, sessionID);
            }
        }
        void OnRequestForPositionAck(QuickFix.FIX44.RequestForPositionsAck ack, SessionID _)
        {
            bool sendLoggedIn = false;
            lock (smbToQuote)
            {
                initialPositionsCounter = 0;
                Positions.Clear();
                initialPositionsCount = ack.TotalNumPosReports.getValue();
                sendLoggedIn = initialPositionsCount == 0 && lastLoggedSessionIdMD != null && lastLoggedSessionIdTR != null;
            }
            if (sendLoggedIn)
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
        }
        void OnPositionReport(QuickFix.FIX44.PositionReport pr, SessionID _)
        {
            bool sendLoggedIn = false;
            string symbol = pr.GetField(Tags.Symbol);

            lock (smbToQuote)
            {
                OrderInformation info = new OrderInformation
                {
                    Symbol = symbol,
                    Id = symbol,
                    OpenPrice = pr.SettlPrice.getValue()
                };
                string qty = pr.IsSetField(Tags.ShortQty) ? pr.GetField(Tags.ShortQty) : pr.GetField(Tags.LongQty);
                info.Volume = long.Parse(qty);
                info.Side = pr.IsSetField(Tags.ShortQty) ? OrderSide.Sell : OrderSide.Buy;

                //int track = pr.IsSetField(5003) ? pr.GetInt(5003) : 0;
                if (info.Volume != 0)
                {
                    Positions.Add(info);
                }
                initialPositionsCounter++;
                sendLoggedIn = initialPositionsCount == initialPositionsCounter && lastLoggedSessionIdMD != null && lastLoggedSessionIdTR != null;
            }
            if (sendLoggedIn)
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
        }

        readonly QuickFix.FIX44.MarketDataSnapshotFullRefresh.NoMDEntriesGroup quotesGroup = new QuickFix.FIX44.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();
        void OnQuoteReceive(QuickFix.FIX44.MarketDataSnapshotFullRefresh message, SessionID _)
        {
            try
            {
                string smb = message.GetString(Tags.Symbol);
                TickEventArgs quote = null;
                lock (smbToQuote)
                {
                    if (smbToQuote.ContainsKey(smb))
                    {
                        quote = smbToQuote[smb];
                    }
                }

                if (quote != null)
                {
                    int noValues = message.GetInt(Tags.NoMDEntries);
                    for (int i = 1; i <= noValues; i++)
                    {
                        message.GetGroup(i, quotesGroup);
                        char quoteType = quotesGroup.GetChar(Tags.MDEntryType);
                        decimal quotePrice = quotesGroup.GetDecimal(Tags.MDEntryPx);
                        if (quoteType == MDEntryType.BID)
                        {
                            quote.Bid = quotePrice;
                        }
                        else if (quoteType == MDEntryType.OFFER)
                        {
                            quote.Ask = quotePrice;
                        }

                        //if (i == 1)
                        //{
                        //    quote.Time = DateTime.UtcNow;
                        //}
                    }
                    Tick?.Invoke(this, quote);
                }
            }
            catch(Exception e)
            {
                logger.LogError(ViewId + " " + e.Message);
            }
        }
        void OnExecutionReport(QuickFix.FIX44.ExecutionReport ex, SessionID _)
        {
            char status = ex.GetChar(Tags.OrdStatus);
            string id = ex.GetString(Tags.OrderID);
            string clid = ex.GetString(Tags.ClOrdID);
            string pid = ex.Symbol.getValue();
            OrderSide oside = ex.Side.getValue() == Side.BUY ? OrderSide.Buy : OrderSide.Sell;
            lock (smbToQuote)
            {
                if (status == OrdStatus.FILLED)
                {
                    OrderInformation order = Positions.FirstOrDefault(x => x.Id == pid);
                    if (order == null)
                    {
                        order = new OrderInformation
                        {
                            Id = pid,
                            Symbol = ex.Symbol.getValue(),
                            Side = oside,
                            OpenTime = DateTime.UtcNow
                        };
                        Positions.Add(order);
                    }
                    if (order.Side != oside)
                    {
                        Positions.Remove(order);
                    }
                    order.OpenPrice = ex.GetDecimal(Tags.AvgPx);
                    order.Volume = ex.CumQty.getValue();
                    tradeResult = order;
                    tradeResultReceived.Set();
                }
                if (status == OrdStatus.CANCELED || status == OrdStatus.REJECTED)
                {
                    var order = Positions.FirstOrDefault(x => x.Id == pid);
                    if (order != null)
                    {
                        Positions.Remove(order);
                    }
                    tradeResult = null;
                    tradeResultReceived.Set();
                }
            }
        }
        public List<OrderInformation> GetOrders(string symbol, int magic, int track)
        {
            lock (smbToQuote)
            {
                return Positions.ToList();
            }
        }
        public OrderOpenResult Open(string symbol, decimal price, decimal lot, OrderSide side, int magic, int slippage, int track, OrderType type, int lifetimeMs)
        {
            DateTime begin = DateTime.UtcNow;
            lock (tradeLock)
            {
                string clientOrderId = GenerateClOrdId();
                QuickFix.FIX44.NewOrderSingle request = new QuickFix.FIX44.NewOrderSingle(
                    new ClOrdID(clientOrderId),
                    new Symbol(symbol),
                    new Side(side == OrderSide.Buy ? Side.BUY : Side.SELL),
                    new TransactTime(DateTime.UtcNow, true),
                    new OrdType(OrdType.MARKET));
                request.SetField(new OrderQty(lot));
                request.SetField(new TimeInForce(TimeInForce.GOOD_TILL_CANCEL));
                request.SetField(new NoPartyIDs(1));
                QuickFix.FIX44.NewOrderSingle.NoPartyIDsGroup parties_group = new QuickFix.FIX44.NewOrderSingle.NoPartyIDsGroup();
                parties_group.SetField(new PartyID(login));
                parties_group.SetField(new PartyIDSource('D'));
                parties_group.SetField(new PartyRole(3));
                request.AddGroup(parties_group);

                tradeResult = null;
                tradeResultReceived.Reset();
                try
                {
                    Session.SendToTarget(request, lastLoggedSessionIdTR);
                    while (!tradeResultReceived.WaitOne(250))
                    {
                        if (!IsLoggedIn) break;
                        if (cancelToken.WaitOne(0)) break;
                    }
                    if (tradeResultReceived.WaitOne(0))
                    {
                        if (tradeResult != null)
                        {
                            return new OrderOpenResult()
                            {
                                Id = tradeResult.Id,
                                ExecutionTime = DateTime.UtcNow - begin,
                                OpenPrice = tradeResult.OpenPrice
                            };
                        }
                    }
                }
                catch(Exception e)
                {
                    return new OrderOpenResult() { Error = e.Message };
                }
            }
            return new OrderOpenResult() { Error = "Timeout error." };
        }
        public OrderModifyResult Modify(string symbol, string orderId, OrderSide side, decimal slPrice, decimal tpPrice)
        {

            return new OrderModifyResult() { Error = "Not implemented" };
        }
        public OrderCloseResult Close(string symbol, string orderId, decimal price, decimal volume, OrderSide side, int slippage, OrderType type, int lifetimeMs)
        {
            DateTime begin = DateTime.UtcNow;

            lock (tradeLock)
            {
                OrderInformation order = null;
                lock (smbToQuote)
                {
                    order = Positions.FirstOrDefault(x => x.Id == orderId);
                }
                if (order != null)
                {
                    string clientOrderId = GenerateClOrdId();
                    QuickFix.FIX44.NewOrderSingle request = new QuickFix.FIX44.NewOrderSingle(
                        new ClOrdID(clientOrderId),
                        new Symbol(symbol),
                        new Side(order.Side == OrderSide.Buy ? Side.SELL : Side.BUY),
                         new TransactTime(DateTime.UtcNow, true),
                        new OrdType(OrdType.MARKET));
                    request.SetField(new OrderQty(volume));
                    request.SetField(new TimeInForce(TimeInForce.GOOD_TILL_CANCEL));
                    request.SetField(new NoPartyIDs(1));
                    request.SetField(new PositionEffect('C'));
                    QuickFix.FIX44.NewOrderSingle.NoPartyIDsGroup parties_group = new QuickFix.FIX44.NewOrderSingle.NoPartyIDsGroup();
                    parties_group.SetField(new PartyID(login));
                    parties_group.SetField(new PartyIDSource('D'));
                    parties_group.SetField(new PartyRole(3));
                    request.AddGroup(parties_group);


                    tradeResult = null;
                    tradeResultReceived.Reset();
                    try
                    {
                        Session.SendToTarget(request, lastLoggedSessionIdTR);
                        while (!tradeResultReceived.WaitOne(250))
                        {
                            if (!IsLoggedIn) break;
                            if (cancelToken.WaitOne(0)) break;
                        }
                        if (tradeResultReceived.WaitOne(0))
                        {
                            if (tradeResult != null)
                            {
                                return new OrderCloseResult()
                                {
                                    ClosePrice = tradeResult.OpenPrice,
                                    ExecutionTime = DateTime.UtcNow - begin,
                                };
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        return new OrderCloseResult() { Error = e.Message };
                    }
                }
            }
            return new OrderCloseResult() { Error = "Timeout error." };
        }
        public bool OrderDelete(string id, string symbol, OrderType type, OrderSide side, decimal lot, decimal price)
        {
            return false;
        }

        public Message CreateOrderStatusRequest()
        {
            throw new NotImplementedException();
        }

        public OrderOpenResult OpenMarketOrd(string symbol, decimal lot, OrderSide side, OrderType type)
        {
            throw new NotImplementedException();
        }

        public OrderOpenResult OpenOrd(string symbol, decimal lot, OrderSide side, OrderType type, int price)
        {
            throw new NotImplementedException();
        }
    }
}
