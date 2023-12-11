using System.Collections.Generic;

namespace Fix_LCU_Window.ErrorDict
{
    internal class FixLeagueClientWindowError
    {
        public static Dictionary<int, string> ErrorDict = new Dictionary<int, string>
        {
            { 1, "成功恢复客户端窗口大小" },
            { 0, "未发现客户端窗口出现异常" },
            { -1, "未检测到客户端窗口，可能客户端正处于最最下化状态" },
            { -2, "未能获取到LCUAPI必要参数" },
            { -3, "未能获取到客户端缩放比例" }
        };
    }
}
