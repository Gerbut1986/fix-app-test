namespace MultiTerminal.Connections
{
    using System;
    using QuickFix;
    using System.Linq;
    using QuickFix.Fields;
    using System.Threading;
    using System.Collections.Generic;

    internal class OneZeroFixClient : IApplication, IConnector
    {
        readonly string login;
        readonly string password;
        readonly string config;
        QuickFix.Transport.SocketInitiator initiator;
        readonly Dictionary<string, TickEventArgs> smbToQuote = new Dictionary<string, TickEventArgs>();
        SessionID lastLoggedSessionIdMD;
        SessionID lastLoggedSessionIdTR;
        readonly object tradeLock = new object();
        readonly ManualResetEvent tradeResultReceived = new ManualResetEvent(false);
        OrderInformation tradeResult;
        readonly List<OrderInformation> Positions = new List<OrderInformation>();
        readonly ManualResetEvent cancelToken;
        readonly IConnectorLogger logger;
        public string ViewId => "ONEZERO FIX" + login;
        public FillPolicy Fill { get; set; }
        public decimal? Balance => null;
        public decimal? Equity => null;

        public OneZeroFixClient(IConnectorLogger logger, ManualResetEvent cancelToken, string login, string password, string config)
        {
            this.logger = logger;
            this.cancelToken = cancelToken;
            this.login = login;
            this.password = password;
            this.config = config;
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
                    return lastLoggedSessionIdMD != null && lastLoggedSessionIdTR != null;
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
                        QuickFix.FIX44.MarketDataRequest message = new QuickFix.FIX44.MarketDataRequest();
                        QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup symbolGroup = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();
                        symbolGroup.Set(new Symbol(symbol));
                        symbolGroup.SetField(new MDReqID(quote.SubscriptionId));
                        symbolGroup.SetField(new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES));
                        symbolGroup.SetField(new MarketDepth(1));
                        symbolGroup.SetField(new MDUpdateType(MDUpdateType.FULL_REFRESH));
                        message.AddGroup(symbolGroup);
                        QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup marketDataEntryGroup = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
                        marketDataEntryGroup.Set(new MDEntryType(MDEntryType.BID));
                        message.AddGroup(marketDataEntryGroup);
                        marketDataEntryGroup.Set(new MDEntryType(MDEntryType.OFFER));
                        message.AddGroup(marketDataEntryGroup);

                        try
                        {
                            Session.SendToTarget(message, lastLoggedSessionIdMD);
                        }
                        catch (Exception e)
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
                        QuickFix.FIX44.MarketDataRequest message = new QuickFix.FIX44.MarketDataRequest();
                        QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup symbolGroup = new QuickFix.FIX44.MarketDataRequest.NoRelatedSymGroup();
                        symbolGroup.Set(new Symbol(symbol));
                        symbolGroup.SetField(new MDReqID(quote.SubscriptionId));
                        symbolGroup.SetField(new SubscriptionRequestType(SubscriptionRequestType.DISABLE_PREVIOUS_SNAPSHOT_PLUS_UPDATE_REQUEST));
                        symbolGroup.SetField(new MarketDepth(1));
                        symbolGroup.SetField(new MDUpdateType(MDUpdateType.FULL_REFRESH));
                        message.AddGroup(symbolGroup);
                        QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup marketDataEntryGroup = new QuickFix.FIX44.MarketDataRequest.NoMDEntryTypesGroup();
                        marketDataEntryGroup.Set(new MDEntryType(MDEntryType.BID));
                        message.AddGroup(marketDataEntryGroup);
                        marketDataEntryGroup.Set(new MDEntryType(MDEntryType.OFFER));
                        message.AddGroup(marketDataEntryGroup);

                        try
                        {
                            Session.SendToTarget(message, lastLoggedSessionIdMD);
                        }
                        catch (Exception e)
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
                catch (Exception e)
                {
                    logger.LogError(ViewId + " " + e.Message);
                }
            }
        }

        public event EventHandler LoggedIn;
        public event EventHandler LoggedOut;
        public event EventHandler<TickEventArgs> Tick;

        string GenerateClOrdId()
        {
            string result = DateTime.UtcNow.ToString("yyyyMMddHHmmss" + uniqueOrderCounter.ToString("00"));
            uniqueOrderCounter++;
            if (uniqueOrderCounter >= 100) uniqueOrderCounter = 0;
            return result;
        }
        int uniqueOrderCounter;

        #region IApplication
        public void OnCreate(SessionID sessionID)
        {
        }
        public void OnLogon(SessionID sessionID)
        {
            bool loggedIn = false;
            lock (smbToQuote)
            {
                if (sessionID.TargetCompID.EndsWith("_Q")) lastLoggedSessionIdMD = sessionID;
                else lastLoggedSessionIdTR = sessionID;
                loggedIn = lastLoggedSessionIdMD != null && lastLoggedSessionIdTR != null;
            }
            if (loggedIn)
            {
                LoggedIn?.Invoke(this, EventArgs.Empty);
            }
        }
        public void OnLogout(SessionID sessionID)
        {
            lock (smbToQuote)
            {
                if (sessionID.TargetCompID.EndsWith("_Q"))
                {
                    lastLoggedSessionIdMD = null;
                }
                else
                {
                    lastLoggedSessionIdTR = null;
                }
                smbToQuote.Clear();
                Positions.Clear();
            }
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            Crack(message, sessionID);
        }

        public void ToAdmin(Message message, SessionID sessionID)
        {
            string value = message.Header.GetField(Tags.MsgType);
            if (value == "A")
            {
                message.SetField(new Password(password));
                message.SetField(new ResetSeqNumFlag(true));
            }
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

            else if (message is QuickFix.FIX44.ExecutionReport)
            {
                OnExecutionReport(message as QuickFix.FIX44.ExecutionReport, sessionID);
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
            catch (Exception e)
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

        // exper.
        public Message CreateOrderStatusRequest()
        {
            QuickFix.FIX44.OrderMassStatusRequest orderMassStatusRequest = new QuickFix.FIX44.OrderMassStatusRequest();
            orderMassStatusRequest.Set(new MassStatusReqID("2123413")); // 584 tag
            orderMassStatusRequest.Set(new MassStatusReqType(MassStatusReqType.STATUS_FOR_ALL_ORDERS)); // 585 tag
            return orderMassStatusRequest;
        }

        public OrderOpenResult OpenOrd(string symbol, decimal lot, OrderSide side, OrderType type, int price = 0)
        {
            DateTime begin = DateTime.UtcNow;
            // hard-coded fields:           
            QuickFix.Fields.HandlInst dHandInst =
                new QuickFix.Fields.HandlInst(QuickFix.Fields.HandlInst.AUTOMATED_EXECUTION_ORDER_PRIVATE);
            // from params:
            QuickFix.Fields.OrdType fOrdType = new OrdType(OrdType.MARKET); //FixEnumTranslator(new OrdType(type));
            QuickFix.Fields.Side fSide = new Side(side == OrderSide.Buy ? Side.BUY : Side.SELL); //FixEnumTranslator()
            QuickFix.Fields.Symbol fSymbol = new QuickFix.Fields.Symbol(symbol);
            QuickFix.Fields.TransactTime fTransactTime = new QuickFix.Fields.TransactTime(DateTime.Now);
            QuickFix.Fields.ClOrdID fClOrdID = new QuickFix.Fields.ClOrdID(GenerateClOrdId());

            QuickFix.FIX44.NewOrderSingle nos =
                new QuickFix.FIX44.NewOrderSingle(fClOrdID, fSymbol, fSide, fTransactTime, fOrdType);

            nos.HandlInst = dHandInst;
            nos.OrderQty = new QuickFix.Fields.OrderQty(lot);
            nos.TimeInForce = new TimeInForce(TimeInForce.GOOD_TILL_CANCEL);

            if (type == OrderType.Limit)
            {
                nos.Price = new QuickFix.Fields.Price(price);
            }

            try
            {
                bool res = Session.SendToTarget(nos, lastLoggedSessionIdTR);
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
            catch (Exception e)
            {
                return new OrderOpenResult() { Error = e.Message };
            }

            QuickFix.FIX44.ExecutionReport ex =
                new QuickFix.FIX44.ExecutionReport(
                    new OrderID("385251"),
                    new ExecID(GenerateClOrdId()),
                    new ExecType(ExecType.ORDER_STATUS),
                    new OrdStatus(OrdStatus.NEW),//(OrdStatus.FILLED),
                    new Symbol(symbol),
                    new Side(Side.BUY),
                    new LeavesQty(lot),
                    new CumQty(lot),
                    new AvgPx());
            ex.SetField(fClOrdID);
            OnExecutionReport(ex, lastLoggedSessionIdTR);

            return new OrderOpenResult() { Error = "Timeout error." };
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
                request.SetField(new OrderQty(lot)); // 38 tag
                request.SetField(new TimeInForce(TimeInForce.GOOD_TILL_CANCEL)); // 59 tag
                request.SetField(new NoPartyIDs(1)); // 453 tag
                QuickFix.FIX44.NewOrderSingle.NoPartyIDsGroup parties_group = new QuickFix.FIX44.NewOrderSingle.NoPartyIDsGroup();
                parties_group.SetField(new PartyID(login));     // 448 tag (login - FRYTRADE)
                parties_group.SetField(new PartyIDSource('D')); // 447 tag
                parties_group.SetField(new PartyRole(3));
                request.AddGroup(parties_group);

                tradeResult = null;
                tradeResultReceived.Reset();
                try
                {
                    bool res = Session.SendToTarget(request, lastLoggedSessionIdTR);
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
                catch (Exception e)
                {
                    return new OrderOpenResult() { Error = e.Message };
                }
            }

            return new OrderOpenResult() { Error = "Timeout error." };
        }

        //exper.
        //public void TransmitNewOrderSingle()
        //{
        //    QuickFix.FIX44.NewOrderSingle msg = new QuickFix.FIX44.NewOrderSingle(
        //        new ClOrdID("woooot"),
        //        new Symbol("IBM"),
        //        new Side(Side.BUY),
        //        new TransactTime(DateTime.Now),
        //        new OrdType(OrdType.MARKET));

        //    msg.Set(new OrderQty(99));

        //   // SendMessage(msg);
        //}
        // exper.
        //private void SendMessage(Message m)
        //{
        //    if (session != null)
        //        _session.Send(m);
        //    else
        //        Puts("Can't send message: session not created.");  // This probably won't ever happen.
        //}

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
                    catch (Exception e)
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


    }
}
