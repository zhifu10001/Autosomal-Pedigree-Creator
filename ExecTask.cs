using Pedigree_Creator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ibdcsfast
{
    class ExecTask
    {
        string data_files_folder;
        string out_folder;
        int snp_threshold = 150;
        double base_pairs_threshold = 1000000;

        public ExecTask(string mdata_files_folder, string mout_folder, int msnp_threshold, double mbase_pairs_threshold)
        {
            this.data_files_folder = mdata_files_folder;
            this.out_folder = mout_folder;
            this.snp_threshold = msnp_threshold;
            this.base_pairs_threshold = mbase_pairs_threshold;
        }

        public void processFile(string input_file)
        {

            SortedDictionary<int, string>[][] chr_v4 = null;

            string[] files = Directory.GetFiles(data_files_folder);
            int total = files.Length;
            Program.addLog("Input file : " + input_file);

            DateTime start = DateTime.Now;
            int count = 0;
            start = DateTime.Now;
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = Environment.ProcessorCount;
            chr_v4 = new CompareUtil().NormalizeData(input_file);
            Object lockobj = new Object();
            Parallel.For(0, files.Length, options, i =>
            {
                count++;
                try
                {
                    lock (lockobj)
                    {
                        if (Path.GetFileNameWithoutExtension(files[i]) != Path.GetFileNameWithoutExtension(input_file))
                        {
                            if (!(File.Exists(out_folder + Path.GetFileNameWithoutExtension(input_file) + "-" + Path.GetFileNameWithoutExtension(files[i])) ||
                            File.Exists(out_folder + Path.GetFileNameWithoutExtension(files[i]) + "-" + Path.GetFileNameWithoutExtension(input_file))))
                            {
                                CompareTask task = new CompareTask(data_files_folder, out_folder, snp_threshold, base_pairs_threshold, files[i], chr_v4, input_file);
                                task.doCompare();
                            }
                        }
                    }
                }
                catch (Exception) { }
                Program.addLog(count + " / " + total + ", Remaining: " + (int)(((DateTime.Now.Subtract(start).TotalSeconds / count) * (total - count)) / 60) + " mins.    ");
            });
        }        
    }
}
