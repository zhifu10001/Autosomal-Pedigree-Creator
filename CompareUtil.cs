using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ibdcsfast
{
    class CompareUtil
    {
        //public const long TOTAL_BP = 2877000000L;

        public string getShared(string genotype1, string genotype2)
        {

            if (Regex.Replace(genotype1, "[^ATGC]", "") != genotype1 || Regex.Replace(genotype2, "[^ATGC]", "") != genotype2)
            {
                foreach (char c1 in genotype1)
                    foreach (char c2 in genotype2)
                    {
                        if (c1 == c2)
                            return c1 + "" + c1;
                        else if (c1 == '-' || c1 == '0')
                            return c2 + "-";
                        else if (c2 == '-' || c2 == '0')
                            return c1 + "-";
                    }

                return null;
            }

            if (genotype1 == genotype2)
                return String.Concat(genotype1.OrderBy(c => c));

            if (genotype1 == Reverse(genotype2))
                return String.Concat(genotype1.OrderBy(c => c));

            foreach(char c1 in genotype1)
                foreach (char c2 in genotype2)
                {
                    if (c1 == c2)
                        return c1 + "" + c1;
                    else if (c1 == '-' || c1 == '0')
                        return c2+"-";
                    else if (c2 == '-' || c2 == '0')
                        return c1+"-";
                }
            return null;
        }

        public byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public byte[] Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
        }

        public void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        public SortedDictionary<int, string>[][] NormalizeData(string file)
        {
            string[] data = null;
            string line1 = null;
            List<string> list1 = new List<string>();
            if(Path.GetExtension(file).EndsWith(".gz"))
            {
                StringReader reader = new StringReader( Encoding.UTF8.GetString(Unzip(File.ReadAllBytes(file))));
                while((line1 = reader.ReadLine())!=null)
                {
                    list1.Add(line1);
                }
                reader.Close();
                data = list1.ToArray();
            }
            else
                data = File.ReadAllLines(file);
            bool ftdna = false;
            SortedDictionary<int, string>[][] chr = {
                                                           new SortedDictionary<int, string>[23],
                                                           new SortedDictionary<int, string>[23]
                                                       };
            if (data[0].Trim() == "RSID,CHROMOSOME,POSITION,RESULT")
                ftdna = true;

            string line = null;
            string[] ldata = null;
            int chromosome = -1;
            int position = -1;
            foreach (string d in data)
            {
                if (ftdna)
                {
                    if (d.StartsWith("RSID"))
                        continue;
                    line = d.Replace("\"", "");
                    line = line.Replace(" ", ",");
                }
                else
                {
                    if (d.StartsWith("#"))
                        continue;
                    //
                    line = d.Replace("\t", ",");
                }
                ldata = line.Split(",".ToCharArray());
                if (ldata[1] == "Y" || ldata[1] == "XY" || ldata[1] == "MT")
                    continue;

                if (ldata[1] == "X")
                    chromosome=23;
                else
                    chromosome = Int32.Parse(ldata[1]);
                if (chromosome == 0)
                    continue;
                position = Int32.Parse(ldata[2]);

                if (chr[0][chromosome - 1] == null)
                    chr[0][chromosome - 1] = new SortedDictionary<int, string>();
                if (chr[1][chromosome - 1] == null)
                    chr[1][chromosome - 1] = new SortedDictionary<int, string>();
                if (!chr[0][chromosome - 1].ContainsKey(position))
                    chr[0][chromosome - 1].Add(position, ldata[3]);

                // rsid - pos map 
                if (!chr[1][chromosome - 1].ContainsKey(position))
                    chr[1][chromosome - 1].Add(position, ldata[0]);        
            }
            return chr;
        }
    }
}
