using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiEditor
{
    internal static class Helper
    {
        internal static string GetUser()
        {
            var systemUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            if (systemUser.Contains("leoni"))
                return "Leonie";
            else if (systemUser.Contains("andre"))
                return "Andreas";
            else return "Unbekannt";
        }

        internal static string GetTabs(int number)
        {
            var tabs = "";
            for (int i = 0; i < number; i++)
                tabs += "\t";

            return tabs;
        }
    }
}
