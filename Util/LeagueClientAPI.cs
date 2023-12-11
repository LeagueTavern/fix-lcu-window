using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Fix_LCU_Window.Util
{
    public class LeagueClientAPI
    {
        private WebRequestHandler RequestHandler = new WebRequestHandler();
        private HttpClient Client;

        public LeagueClientAPI(int Port, string Token)
        {
            // ignore certificate errors
            RequestHandler.ServerCertificateValidationCallback = delegate { return true; };

            Client = new HttpClient(RequestHandler);
            Client.BaseAddress = new Uri("https://127.0.0.1:" + Port);
            Client.DefaultRequestHeaders.Add("Accept", "*/*");
            Client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("riot:" + Token)));
        }

        public async Task<double> GetClientZoom()
        {
            try
            {
                HttpResponseMessage Response = await Client.GetAsync("/riotclient/zoom-scale");
                Response.EnsureSuccessStatusCode();
                return double.Parse(await Response.Content.ReadAsStringAsync());
            }
            catch
            {
                return -1;
            }
        }

        public async Task<bool> RestartClientUx()
        {
            try
            {
                HttpResponseMessage response = await Client.PostAsync("/riotclient/kill-and-restart-ux", new StringContent(""));
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LobbyPlayAgain()
        {
            try
            {
                HttpResponseMessage response = await Client.PostAsync("/lol-lobby/v2/play-again", new StringContent(""));
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static (bool Available, int Port, string Token, string Protocol) CommandLineParser(string command)
        {
            Regex installAuthToken = new Regex(@"""--remoting-auth-token=(.*?)""");
            Regex installAppPort = new Regex(@"""--app-port=(.*?)""");

            var portMatch = installAppPort.Match(command);
            var tokenMatch = installAuthToken.Match(command);

            return (portMatch.Success && tokenMatch.Success)
                ? (true, int.Parse(portMatch.Groups[1].Value), tokenMatch.Groups[1].Value, "https")
                : (false, 0, null, null);
        }
    }
}
