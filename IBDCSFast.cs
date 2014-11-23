using Pedigree_Creator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ibdcsfast
{
    public static class IBDCSFast
    {
        static string out_folder = "ibd\\";
        static string data_files_folder = "data\\";

        static int snp_threshold = 150;

        static double base_pairs_threshold = 1000000;

        public static void doIBDCSFast()
        {
            Program.addLog("data: " + data_files_folder);
            Program.addLog("ibd: " + out_folder);

            if (!Directory.Exists(data_files_folder) || !Directory.Exists(out_folder))
            {
                Program.addLog("Required data and ibd directories doesn't exist!");
                return;
            }

            string[] files = Directory.GetFiles(data_files_folder);
            int total = files.Length;

            foreach(string file in files)
            {
                ExecTask task = new ExecTask(data_files_folder, out_folder, snp_threshold, base_pairs_threshold);
                task.processFile(file);
            }
        }

       
    }
}
