using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Pedigree_Creator
{
    public partial class PedigreeCreatorFrm : Form
    {
        public PedigreeCreatorFrm()
        {
            InitializeComponent();
        }

        private void PedigreeCreatorFrm_Load(object sender, EventArgs e)
        {
            if(!Directory.Exists("data"))
                Directory.CreateDirectory("data");
            if (!Directory.Exists("ibd"))
                Directory.CreateDirectory("ibd");
            if (!Directory.Exists("tmp"))
                Directory.CreateDirectory("tmp");
        }

        private string sanatize(string file)
        {
            file = file.Replace(" ", "_");
            string new_file_name = Regex.Replace(file, "[^A-Za-z_]", "");
            return new_file_name;
        }

        public static byte[] Zip(byte[] bytes)
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

        public static byte[] Unzip(byte[] bytes)
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

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            
            deleteFilesFromFolder("data");
            deleteFilesFromFolder("ibd");
            deleteFilesFromFolder("tmp");   

            string[] files = Directory.GetFiles(e.Argument.ToString());
            foreach (string file in files)
            {
                if (file.EndsWith(".gz"))
                {
                    StringReader reader = new StringReader(Encoding.UTF8.GetString(Unzip(File.ReadAllBytes(file))));
                    string text = reader.ReadToEnd();
                    reader.Close();
                    File.WriteAllText("data\\" + sanatize(Path.GetFileNameWithoutExtension(file)), text);

                }
                else if (file.EndsWith(".zip"))
                {
                    string text = "";
                    using (var fs = new MemoryStream(File.ReadAllBytes(file)))
                    using (var zf = new ZipFile(fs))
                    {
                        var ze = zf[0];
                        if (ze == null)
                        {
                            throw new ArgumentException("file not found in Zip");
                        }
                        using (var s = zf.GetInputStream(ze))
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                               text = sr.ReadToEnd();
                            }
                        }
                    }
                    File.WriteAllText("data\\" + sanatize(Path.GetFileNameWithoutExtension(file)), text);
                }
                else
                    File.Copy(file, "data\\" + sanatize(Path.GetFileNameWithoutExtension(file)));
               
            }
            

            // initial pass
            ibdcsfast.IBDCSFast.doIBDCSFast();
            // cleanup own file
            foreach (string file in files)
            {
                if (File.Exists("ibd\\" + sanatize(Path.GetFileNameWithoutExtension(file))))
                    File.Delete("ibd\\" + sanatize(Path.GetFileNameWithoutExtension(file)));
            }
            copyFilesFromFolder("ibd", "data");
            renkits.RenKits.doRenKits();
            deleteFilesFromFolder("ibd");


            // subsequent pass

            int count = 0;
            int prev_count = 0;
            for(int i=0;i<int.MaxValue;i++)
            {
                
                ibdcsfast.IBDCSFast.doIBDCSFast();                
                copyFilesFromFolder("ibd", "data");
                renkits.RenKits.doRenKits();
                deleteFilesFromFolder("ibd");

                count = Directory.GetFiles("ibd").Length;
                if (count == prev_count)
                    break;
                prev_count = Directory.GetFiles("ibd").Length;
            }

            copyFilesFromFolder("data", "ibd");
            deleteFilesFromFolder("data");            
            
            preparelist.PrepareList.doPrepareList();
            genxml.GenXML.doGenXML();

            xml2gv.Xml2GraphViz.doXml2GraphViz();

            Process p = new Process();
            ProcessStartInfo psinfo=new ProcessStartInfo("bin\\dot.exe","-Tpng tree.gv -o pedigree.png");
            psinfo.WindowStyle=ProcessWindowStyle.Hidden;
            p.StartInfo = psinfo;
            p.Start();
            p.WaitForExit(5000);

            deleteFilesFromFolder("tmp");
            if (File.Exists("atree.txt"))
                File.Move("atree.txt", "tmp\\atree.txt");
            if (File.Exists("atree.xml")) 
                File.Move("atree.xml", "tmp\\atree.xml");
            if (File.Exists("tree.gv")) 
                File.Move("tree.gv", "tmp\\tree.gv");
            Process.Start("pedigree.png");
        }

        private void deleteFilesFromFolder(string folder)
        {
            string[] files = Directory.GetFiles(folder);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }

        private void copyFilesFromFolder(string source,string target)
        {
            string[] files = Directory.GetFiles(source);
            foreach (string file in files)
            {
                if (!File.Exists(target + "\\" + Path.GetFileName(file)))
                    File.Copy(file, target + "\\" + Path.GetFileName(file));
            }
        }

        public void addLog(String log)
        {
            backgroundWorker1.ReportProgress(-1, log);
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            listBox1.Items.Add(e.UserState.ToString());
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void PedigreeCreatorFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backgroundWorker1.IsBusy)
                backgroundWorker1.CancelAsync();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                button1.Enabled = false;
                backgroundWorker1.RunWorkerAsync(dlg.SelectedPath);                
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("A tool to create pedigrees from a set of autosomal files.\r\n\r\nDeveloper: Felix Chandrakumar <i@fc.id.au>", "Autosomal Pedigree Creator", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.y-str.org/");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("1. Rename all kits to have some meaning filenames.\r\n2. Place all kits into one folder.\r\n3. Browse and select the folder.\r\n4. The final output will be an image file which will automatically open.\r\n\r\nTo get more indepth details, please visit website.", "Quick Help");
        }
    }
}
