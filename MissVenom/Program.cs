using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MissVenom
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            //context class:
            //I've embedded some DLL's into the EXE,
            //and in order to resolve them during runtime
            //you need a context, which the static Program
            //class doesn't have
            Context c = new Context();
            c.Run();
        }
    }
}
