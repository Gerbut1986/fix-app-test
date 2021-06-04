namespace MultiTerminal.Connections.Models
{
    public partial class OgmFixConnectionModel : FixConnectionModel
    {
        public OgmFixConnectionModel() : base()
        {
            UseSSL = true;
            HostQuote = "TS2LiveFix22.cfixtech.com";
            HostTrade = "TS2LiveFix22.cfixtech.com";
            TargetCompIdQuote = "LivePrices";
            TargetCompIdTrade = "LiveOrders";
        }
        public override string SaveConfig()
        {
            return SaveConfig(Name, OgmFixConfig, this);
        }
        private const string OgmFixConfig = @"[DEFAULT]
FileStorePath=.tmp
!LOGGING!
ConnectionType=initiator
HeartBtInt=20
StartTime=00:00:00
EndTime=23:59:59
DataDictionary=.cfg/.dict/ogmfix.xml
ValidateUserDefinedFields=N
ValidateFieldsHaveValues=N
ValidateFieldsOutOfOrder=N
ValidateUnorderedGroupFields=N
ValidateLengthAndChecksum=N
ReconnectInterval=5
ResetOnDisconnect=Y
ResetSeqNumFlag=Y
ResetOnLogout=Y
ResetOnLogon=Y
SendResetSeqNumFlag=Y
ContinueInitializationOnError=Y
PrintIncoming=Y
PrintOutgoing=Y
PrintEvents=Y
IgnorePossDupResendRequests=Y
!SSL!

[SESSION]
SocketConnectHost=!HOSTQUOTE!
SocketConnectPort=!PORTQUOTE!
BeginString=FIX.4.4
SenderCompID=!SENDERCOMPIDQUOTE!
TargetCompID=!TARGETCOMPIDQUOTE!
MDEntryType=Y

[SESSION]
SocketConnectHost=!HOSTTRADE!
SocketConnectPort=!PORTTRADE!
BeginString=FIX.4.4
SenderCompID=!SENDERCOMPIDTRADE!
TargetCompID=!TARGETCOMPIDTRADE!
MDEntryType=Y";

    }
}
