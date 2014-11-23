using Pedigree_Creator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace renkits
{
    class RenKits
    {
        public static void doRenKits()
        {
            if(!Directory.Exists("ibd"))
            {
                Program.addLog("'ibd' folder does not exist!");
                return;
            }
            string[] files = Directory.GetFiles("data");
            foreach(string file in files)
            {
                string[] kits = Path.GetFileNameWithoutExtension(file).Split(new char[] { '-' });
                List<string> k = new List<string>();
                foreach(string kit in kits)
                    if(!k.Contains(kit))
                        k.Add(kit);
                StringBuilder sb = new StringBuilder();
                k.Sort();
                foreach (string kk in k)
                    sb.Append(" "+kk);
                string new_name = sb.ToString().Trim().Replace(" ", "-");
                new_name=Path.GetDirectoryName(file)+"\\"+new_name;
                Program.addLog(file + " -> " + new_name);
                if (file != new_name)
                {
                    if (File.Exists(new_name))
                        File.Delete(file);
                    else
                        File.Move(file, new_name);
                }               
            }
        }
    }
}
