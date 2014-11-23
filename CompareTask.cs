using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ibdcsfast
{
    class CompareTask
    {

        string data_files_folder;
        string out_folder;
        int snp_threshold = 150;
        double base_pairs_threshold = 1000000;
        SortedDictionary<int, string>[][] chr_v4;

        string fv3;
        string input_file;

        public CompareTask(string mdata_files_folder, string mout_folder, int msnp_threshold, double mbase_pairs_threshold,
            string mfv3, SortedDictionary<int, string>[][] mchr_v4, string minput_file)
        {
            this.data_files_folder = mdata_files_folder;
            this.out_folder = mout_folder;
            this.snp_threshold = msnp_threshold;
            this.base_pairs_threshold = mbase_pairs_threshold;

            this.fv3 = mfv3;
            this.chr_v4 = mchr_v4;
            this.input_file = minput_file;
        }

        public void doCompare()
        {
            //Program.addLog("Comparing .." + Path.GetFileName(fv3));

            ArrayList output_text = new ArrayList();

            StringBuilder sb_result = new StringBuilder();
            ArrayList chr_rm = new ArrayList();
            output_text.Clear();
            SortedDictionary<int, string>[][] chr_v3 = new CompareUtil().NormalizeData(fv3);
            chr_rm.Clear();
            for (int i = 0; i < 23; i++)
            {
                if (chr_v3[0][i] == null || chr_v4[0][i] == null)
                    continue;
                chr_rm.Clear();
                foreach (int position in chr_v3[0][i].Keys)
                {
                    if (!chr_v4[0][i].ContainsKey(position)) //these SNPs are not tested.. on the other match
                        chr_rm.Add(position);
                }
                //foreach (int position in chr_rm)
                //    chr_v4[0][i].Add(position, chr_v3[0][i][position]);
                foreach (int position in chr_rm)
                    chr_v4[0][i].Add(position, "--");
            }
            //

            int segment_start = 0;
            int segment_end = 0;
            double total_mb = 0;
            int snp_count = 0;
            string genotype_v3 = null;
            string genotype_v4 = null;
            List<int> keys = null;
            string rsid = null;
            List<string> shared = new List<string>();
            int last_success_position = 0;
            string file = null;
            int no_call_count_threshold = 25;
            int no_call_count = 0;
            for (int i = 0; i < 23; i++)
            {
                if (chr_v3[0][i] == null || chr_v4[0][i] == null)
                    continue;
                //
                snp_count = 0;
                no_call_count = 0;
                keys = chr_v3[0][i].Keys.ToList();
                segment_start = -1;
                last_success_position = 0;
                shared.Clear();
                string shared_gt = null;
                int prev_pos = 0;
                foreach (int position in keys)
                {
                    if (chr_v3[0][i] == null || chr_v4[0][i] == null) // doesn't exist in file.
                        continue;
                    genotype_v3 = chr_v3[0][i][position];
                    genotype_v4 = chr_v4[0][i][position];
                    rsid = chr_v3[1][i][position];

                    if ((shared_gt = new CompareUtil().getShared(genotype_v3, genotype_v4)) != null && position - prev_pos < 50000 && no_call_count < no_call_count_threshold)
                    {
                        // each position recorded is a snp

                        if (segment_start == -1)
                        {
                            segment_start = position;
                            shared.Clear();
                        }
                        snp_count++;
                        if (i + 1 == 23)
                            shared.Add(rsid + "\tX\t" + position + "\t" + shared_gt);
                        else
                            shared.Add(rsid + "\t" + (i + 1).ToString() + "\t" + position + "\t" + shared_gt);
                        last_success_position = position;
                        if (shared_gt.IndexOf('-') != -1 || shared_gt.IndexOf('0') != -1)
                            no_call_count++;
                    }
                    else
                    {
                        if (segment_start != -1)
                        {
                            segment_end = last_success_position;
                            total_mb = segment_end - segment_start;
                            if (snp_count > snp_threshold && total_mb > base_pairs_threshold)
                            {
                                file = Path.GetFileNameWithoutExtension(input_file) + "-" + Path.GetFileNameWithoutExtension(fv3);
                                string file_alt = Path.GetFileNameWithoutExtension(fv3) + "-" + Path.GetFileNameWithoutExtension(input_file);
                                File.AppendAllLines(out_folder + file, shared);
                                shared.Clear();
                                no_call_count = 0;
                            }
                        }
                        segment_start = -1;
                        snp_count = 0;
                    }
                    prev_pos = position;
                }
                ///
                if (segment_start != -1)
                {
                    segment_end = last_success_position;
                    total_mb = keys[keys.Count - 1] - segment_start;
                    if (snp_count > snp_threshold && total_mb > base_pairs_threshold)
                    {
                        file = Path.GetFileNameWithoutExtension(input_file) + "-" + Path.GetFileNameWithoutExtension(fv3);
                        string file_alt = Path.GetFileNameWithoutExtension(fv3) + "-" + Path.GetFileNameWithoutExtension(input_file);
                        File.AppendAllLines(out_folder + file, shared);
                        shared.Clear();
                        no_call_count = 0;
                    }
                }
                snp_count = 0;

                ///
            }
        }
    }
}
