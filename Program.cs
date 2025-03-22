using Ptxbuddy;
using System;
using System.Windows.Forms;

namespace Ptxbuddy
{
    internal class Program
    {
        [STAThread]
        static void Main()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new FrmChatBoxForm());

        }
    }
}
