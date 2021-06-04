using Arbitrage.Api.Clients;
using Arbitrage.Api.Dto;
using MultiTerminal.Connections;
using MultiTerminal.Connections.Models;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MultiTerminal
{
    public partial class App : Application
    {
        internal static string ServerAddress = "https://westernpips.net:8443";
        internal static Client Client { get; set; }
        internal static string Login { get; set; }
        internal static string HostId { get; set; }
        internal static SubscriptionLoginResponseDto Subscription { get; set; }
        internal static string ClientCryptoKey { get; set; }
        public App()
        {
            BaseClient.InitializeServicePointManager();
            ClientCryptoKey = "J7Wdv0eoHTVOMAhGPaNbEi0l8kfgFQYDu4adbReR";
        }
        public static string GetTmpFolder()
        {
            string tmpFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ".tmp");
            try
            {
                System.IO.Directory.CreateDirectory(tmpFolder);
            }
            catch
            {

            }
            return tmpFolder;
        }
        public static string GetLogFolder()
        {
            string result = Assembly.GetExecutingAssembly().Location;
            result = System.IO.Path.GetDirectoryName(result);
            result = System.IO.Path.Combine(result, ".logs");
            try
            {
                System.IO.Directory.CreateDirectory(result);
            }
            catch
            {
            }
            return result;
        }

        public static void ShowTitle()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Current.MainWindow.Title = "MultiTerminal v" + version.Major + "." + version.Minor;
        }
        internal static IConnector CreateConnector(ConnectionModel connection, IConnectorLogger logger, ManualResetEvent cancelToken, int sleepMs, Dispatcher dispatcher)
        {
            if (connection is OgmFixConnectionModel)
            {
                return ConnectorsFactory.Current.CreateOgmFix(logger, cancelToken, connection as OgmFixConnectionModel);
            }
            if (connection is OneZeroFixConnectionModel)
            {
                return ConnectorsFactory.Current.CreateOneZeroFix(logger, cancelToken, connection as OneZeroFixConnectionModel);
            }
            return new ProxyConnector();
        }
    }
}
