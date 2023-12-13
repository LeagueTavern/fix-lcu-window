using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fix_LCU_Window.Util;
using Fix_LCU_Window.ErrorDict;
using static Fix_LCU_Window.Util.ProcessControl;
using static Fix_LCU_Window.Util.LeagueClientAPI;

namespace Fix_LCU_Window
{
    class Program
    {
        static LeagueClientAPI GetLCU()
        {
            var LeagueClientId = GetProcessId("LeagueClientUx");
            var LeagueClientUxCommandLine = GetCommandLineByProcessId(LeagueClientId);
            var LeagueClientUxArgs = CommandLineParser(LeagueClientUxCommandLine);

            if (!LeagueClientUxArgs.Available)
            {
                return null;
            }

            // Console.WriteLine(LeagueClientUxArgs.ToString());
            // Return a new instance of LeagueClientAPI
            return new LeagueClientAPI(LeagueClientUxArgs.Port, LeagueClientUxArgs.Token);
        }

        static (IntPtr LeagueClientWindowHWnd, IntPtr LeagueClientWindowCefHWnd) GetLeagueClientWindowHandle()
        {
            IntPtr LeagueClientWindowHWnd = FindWindow("RCLIENT", "League of Legends");
            IntPtr LeagueClientWindowCefHWnd = FindWindowEx(LeagueClientWindowHWnd, IntPtr.Zero, "CefBrowserWindow", null);

            // Return a tuple of the two window handles
            return (LeagueClientWindowHWnd, LeagueClientWindowCefHWnd);
        }

        static bool needResize(RECT Rect)
        {
            return (Rect.Bottom - Rect.Top) /
                (double)(Rect.Right - Rect.Left) != 0.5625;
        }

        static async Task<int> FixLeagueClientWindow(bool forced = false)
        {
            var LeagueClientWindow = GetLeagueClientWindowHandle();
            var LeagueClientWindowRect = new RECT();
            var LeagueClientWindowCefRect = new RECT();

            if (
                LeagueClientWindow.LeagueClientWindowHWnd == IntPtr.Zero ||
                LeagueClientWindow.LeagueClientWindowCefHWnd == IntPtr.Zero
                )
            {
                return -1; // Failed to get leagueclient window handle
            }

            if (IsMinimalized(LeagueClientWindow.LeagueClientWindowHWnd))
            {
                return 0;
            }

            GetWindowRect(LeagueClientWindow.LeagueClientWindowHWnd, ref LeagueClientWindowRect);
            GetWindowRect(LeagueClientWindow.LeagueClientWindowCefHWnd, ref LeagueClientWindowCefRect);

            if (!needResize(LeagueClientWindowRect) && !needResize(LeagueClientWindowCefRect) && !forced)
            {
                return 0;
            }

            var LeagueClientAPIClient = GetLCU();

            if (LeagueClientAPIClient == null)
            {
                return -2; // Failed to get leagueclient api instance
            }

            var LeagueClientZoom = await LeagueClientAPIClient.GetClientZoom();
            var PrimaryScreenWidth = Screen.PrimaryScreen.Bounds.Width;
            var PrimaryScreenHeight = Screen.PrimaryScreen.Bounds.Height;
            var PrimaryScreenDpi = GetDpiForWindow(LeagueClientWindow.LeagueClientWindowHWnd) / (double)GetDpiForSystem();

            if (LeagueClientZoom == -1)
            {
                return -3; // Failed to get leagueclient zoom
            }

            var TargetLeagueClientWindowWidth = (int)(1280 * LeagueClientZoom);
            var TargetLeagueClientWindowHeight = (int)(720 * LeagueClientZoom);

            PatchDpiChangedMessage(LeagueClientWindow.LeagueClientWindowHWnd);
            PatchDpiChangedMessage(LeagueClientWindow.LeagueClientWindowCefHWnd);

            SetWindowPos(
                LeagueClientWindow.LeagueClientWindowHWnd,
                0,
                (PrimaryScreenWidth - TargetLeagueClientWindowWidth) / 2,
                (PrimaryScreenHeight - TargetLeagueClientWindowHeight) / 2,
                TargetLeagueClientWindowWidth, TargetLeagueClientWindowHeight,
                0x0040
            );

            SetWindowPos(
                LeagueClientWindow.LeagueClientWindowCefHWnd,
                0,
                0,
                0,
                TargetLeagueClientWindowWidth,
                TargetLeagueClientWindowHeight,
                0x0040
            );

            return 1;
        }

        static void Main(string[] args)
        {

            if (args.Length >= 2 && args[0] == "--mode" && int.TryParse(args[1], out int mode))
            {
                Run(true, mode).Wait();
            }
            else
            {
                Run(false).Wait();
                Console.WriteLine("> 按任意键退出...");
                Console.ReadKey();
            }
        }

        static void PrintMenu()
        {
            Console.WriteLine("------");
            Console.WriteLine("> 功能菜单: ");
            Console.WriteLine("| [ 0 ]: 退出");
            Console.WriteLine("| [ 1 ]: 立即恢复客户端窗口到正常大小 ");
            Console.WriteLine("| [ 2 ]: 自动恢复客户端窗口到正常大小 (常驻)");
            Console.WriteLine("| [ 3 ]: 直接跳过结算页面");
            Console.WriteLine("| [ 4 ]: 热重载客户端");
            Console.WriteLine("> ");
            Console.Write("> 请输入功能序号并回车以执行 (1): ");
        }

        static void PrintCopyright()
        {
            Console.WriteLine("------");
            Console.WriteLine("> [ 英雄联盟客户端疑难杂症解决方案 ]");
            Console.WriteLine("> Fix LCU Window -- " + GetVersion());
            Console.WriteLine("------");
            Console.WriteLine("> Github: https://github.com/LeagueTavern/fix-lcu-window");
            Console.WriteLine("> Bilibili: Butter_Cookies");
            Console.WriteLine("> Code by LeagueTavern");
        }

        static string GetVersion()
        {
            return Application.ProductVersion.Substring(0, Application.ProductVersion.LastIndexOf('.'));
        }

        static int GetUserChoice()
        {
            var UserInput = 0;
            var UserOriginalInput = Console.ReadLine();
            if (String.IsNullOrWhiteSpace(UserOriginalInput) || !int.TryParse(UserOriginalInput, out UserInput))
            {
                UserInput = 1; // Default to 1
            }

            return UserInput;
        }

        static async Task<string> Plan1()
        {
            var Result = await FixLeagueClientWindow(true);
            var ErrorMessage = FixLeagueClientWindowError.ErrorDict[Result];
            return ErrorMessage;
        }

        static async Task Plan2()
        {
            int CurrentTriggerCount = 0;

            Console.WriteLine("> 已进入自动检测模式，您现在可以放心的去玩游戏了");
            Console.WriteLine("> 当客户端尺寸出现异常时，程序将会自动修复");
            Console.WriteLine("> 按下 [Ctrl] + [C] 或 直接关闭本窗口 即可关闭检测并结束本程序");
            Console.WriteLine("> ------");

            while (true)
            {
                var CurrentResult = await FixLeagueClientWindow();
                if (CurrentResult == 1)
                {
                    CurrentTriggerCount++;
                    Console.WriteLine("> 检测到窗口尺寸异常，已自动处理 (" + CurrentTriggerCount + ")");
                }
                await Task.Delay(1500);
            };
        }

        static async Task<string> Plan3()
        {
            var LeagueClientAPIClient = GetLCU();
            var Result = await LeagueClientAPIClient.LobbyPlayAgain();
            return Result ? "成功向客户端发送指令" : "向客户端发送指令时出现问题";
        }

        static async Task<string> Plan4()
        {
            var LeagueClientAPIClient = GetLCU();
            var Result = await LeagueClientAPIClient.RestartClientUx();
            return Result ? "成功向客户端发送指令" : "向客户端发送指令时出现问题";
        }

        static async Task Run(bool withArgs, int mode = 0)
        {
            if (!withArgs)
            {
                PrintCopyright();
                PrintMenu();
            }

            var UserChoice = withArgs ? mode : GetUserChoice();

            if (withArgs)
            {
                Console.WriteLine("> 正在执行功能: " + mode);
            }
            else
            {
                Console.WriteLine("> ------");
            }


            switch (UserChoice)
            {
                case 0: // Exit
                    return;
                case 1: // Fix League Client Window
                    Console.WriteLine("> " + await Plan1());
                    break;
                case 2: // Fix League Client Window (Auto)
                    await Plan2();
                    break;
                case 3: // Skip Post Game
                    Console.WriteLine("> " + await Plan3());
                    break;
                case 4: // Restart Client
                    Console.WriteLine("> " + await Plan4());
                    break;
                default: // Unknown Choice
                    Console.WriteLine("> 未知的功能序号");
                    break;
            }

            return;
        }
    }
}
