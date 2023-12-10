using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Management;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;

namespace fix_lcu_window_bin
{
    internal class App
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow([In] IntPtr hmonitor);

        // 取commandline
        private static string GetCommandLineByProcessId(int processId)
        {
            try
            {
                return GetCommandLineArgsCore();
            }
            catch (Win32Exception ex) when ((uint)ex.ErrorCode == 0x80004005)
            {
                return string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }

            string GetCommandLineArgsCore()
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + processId))
                using (var objects = searcher.Get())
                {
                    var @object = objects.Cast<ManagementBaseObject>().SingleOrDefault();
                    return @object?["CommandLine"]?.ToString() ?? "";
                }
            }
        }

        private static int GetProcessId(String processName)
        {
            Process[] processes = Process.GetProcesses();
            int iProcessId = 0;

            foreach (Process p in processes)
            {
                if (p.ProcessName == processName)
                {
                    iProcessId = p.Id;
                    break;
                }
            }

            return iProcessId;
        }

        private static (bool Available, int Port, string Token, string Protocol) CommandLineParser(string command)
        {
            Regex installAuthToken = new Regex(@"""--remoting-auth-token=(.*?)""");
            Regex installAppPort = new Regex(@"""--app-port=(.*?)""");

            var portMatch = installAppPort.Match(command);
            var tokenMatch = installAuthToken.Match(command);

            if (portMatch.Success && tokenMatch.Success)
            {
                return (true, int.Parse(portMatch.Groups[1].Value), tokenMatch.Groups[1].Value, "https");
            }

            return (false, 0, null, null);
        }

        private static async Task<double> GetLeagueClientZoom(int Port, String Token)
        {

            String address = "https://127.0.0.1:" + Port + "/riotclient/zoom-scale";

            WebRequestHandler handler = new WebRequestHandler();
            HttpClient client = new HttpClient(handler);

            handler.ServerCertificateValidationCallback = delegate { return true; };
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("riot:" + Token)));
            try
            {
                HttpResponseMessage response = await client.GetAsync(address);
                response.EnsureSuccessStatusCode();

                return double.Parse(await response.Content.ReadAsStringAsync());
            }
            catch
            {
                return -1;
            }
        }

        private static async Task<bool> RestartClientUx(int Port, String Token)
        {

            string address = "https://127.0.0.1:" + Port + "/riotclient/kill-and-restart-ux";

            WebRequestHandler handler = new WebRequestHandler();
            HttpClient client = new HttpClient(handler);

            handler.ServerCertificateValidationCallback = delegate { return true; };
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("riot:" + Token)));
            try
            {
                HttpResponseMessage response = await client.PostAsync(address, new StringContent(""));
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void emitExit()
        {
            Console.WriteLine("按任意键退出...");
            Console.ReadKey();
        }

        private static async Task MethodPlanA()
        {
            var LeagueClientUxCommandLine = GetCommandLineByProcessId(GetProcessId("LeagueClientUx"));
            var LeagueClientUxArgs = CommandLineParser(LeagueClientUxCommandLine);

            if (!LeagueClientUxArgs.Available)
            {
                Console.WriteLine("Can't find LeagueClientUx process.");
                emitExit();
                return;
            }

            IntPtr pLeagueClientWindowHWnd = FindWindow("RCLIENT", "League of Legends");
            IntPtr pLeagueClientWindowCefHWnd = FindWindowEx(pLeagueClientWindowHWnd, IntPtr.Zero, "CefBrowserWindow", null);

            if (pLeagueClientWindowHWnd == IntPtr.Zero || pLeagueClientWindowCefHWnd == IntPtr.Zero)
            {
                Console.WriteLine("Can't find LeagueClient window.");
                emitExit();
                return;
            }

            double fLeagueClientZoom = await GetLeagueClientZoom(LeagueClientUxArgs.Port, LeagueClientUxArgs.Token);
            double fScreenDpi = GetDpiForWindow(pLeagueClientWindowHWnd) / 96.0;
            int iScreenWidth = Screen.PrimaryScreen.Bounds.Width;
            int iScreenHeight = Screen.PrimaryScreen.Bounds.Height;

            if (fLeagueClientZoom == -1)
            {
                Console.WriteLine("Can't get original zoom of LeagueClientUx");
                emitExit();
                return;
            }

            int iTargetWindowWidth = (int)(1280 * fLeagueClientZoom);
            int iTargetWindowHeight = (int)(720 * fLeagueClientZoom);

            Console.WriteLine("LeagueClientPort: " + LeagueClientUxArgs.Port);
            Console.WriteLine("LeagueClientAuthtoken: " + LeagueClientUxArgs.Token);
            Console.WriteLine("LeagueClientOriginZoom: " + fLeagueClientZoom);
            Console.WriteLine("LeagueClientWindowHWnd: " + pLeagueClientWindowHWnd);
            Console.WriteLine("LeagueClientWindowCefHWnd: " + pLeagueClientWindowCefHWnd);
            Console.WriteLine("ScreenWidth: " + iScreenWidth * fScreenDpi);
            Console.WriteLine("ScreenHeight: " + iScreenHeight * fScreenDpi);
            Console.WriteLine("ScreenDpi: " + fScreenDpi);
            Console.WriteLine("TargetWindowWidth: " + iTargetWindowWidth);
            Console.WriteLine("TargetWindowHeight: " + iTargetWindowHeight);

            Console.WriteLine("-----------------------------");
            Console.WriteLine("客户端修复成功!");
            Console.WriteLine("-----------------------------");

            SetWindowPos(pLeagueClientWindowHWnd, 0, (iScreenWidth - iTargetWindowWidth) / 2, (iScreenHeight - iTargetWindowHeight) / 2, iTargetWindowWidth, iTargetWindowHeight, 0x0040);
            SetWindowPos(pLeagueClientWindowCefHWnd, 0, 0, 0, iTargetWindowWidth, iTargetWindowHeight, 0x0040);
        }

        private static async Task MethodPlanB()
        {
            var LeagueClientUxCommandLine = GetCommandLineByProcessId(GetProcessId("LeagueClientUx"));
            var LeagueClientUxArgs = CommandLineParser(LeagueClientUxCommandLine);

            if (!LeagueClientUxArgs.Available)
            {
                Console.WriteLine("Can't find LeagueClientUx process.");
                emitExit();
                return;
            }

            if (!await RestartClientUx(LeagueClientUxArgs.Port, LeagueClientUxArgs.Token))
            {
                Console.WriteLine("Failed to reload LeagueClientUx.");
                emitExit();
                return;
            }

            Console.WriteLine("LeagueClientPort: " + LeagueClientUxArgs.Port);
            Console.WriteLine("LeagueClientAuthtoken: " + LeagueClientUxArgs.Token);

            Console.WriteLine("-----------------------------");
            Console.WriteLine("客户端修复成功!");
            Console.WriteLine("-----------------------------");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("-----------------------------");
            Console.WriteLine("Bilibili: Butter_Cookies");
            Console.WriteLine("Github: https://github.com/LeagueTavern/fix-lcu-window");
            Console.WriteLine("Code by LeagueTavern");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("选择修复模式:");
            Console.WriteLine("[1]: 通过 窗口句柄 修复客户端");
            Console.WriteLine("[2]: 通过 LCUAPI 热重载客户端");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("输入您想选择的修复模式 [1]:");

            String strUserInput = Console.ReadLine();
            int iUserChoice;

            Console.WriteLine("-----------------------------");


            if (String.IsNullOrWhiteSpace(strUserInput) || !int.TryParse(strUserInput, out iUserChoice))
            {
                iUserChoice = 1;
            }

            switch (iUserChoice)
            {
                case 1:
                    MethodPlanA().Wait();
                    break;
                case 2:
                    MethodPlanB().Wait();
                    break;
            }
            emitExit();
        }
    }
}