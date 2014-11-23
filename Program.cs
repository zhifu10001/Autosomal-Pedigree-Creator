using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Pedigree_Creator
{
    static class Program
    {

        static PedigreeCreatorFrm frm = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            frm = new PedigreeCreatorFrm();
            Application.Run(frm);
        }

        public static void addLog(String log)
        {
            frm.addLog(log);
        }
    }
}
