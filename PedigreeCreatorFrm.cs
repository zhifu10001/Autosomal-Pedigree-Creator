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
        bool dump_all = false;
        string dir_path = null;
        public PedigreeCreatorFrm()
        {
            InitializeComponent();
        }

        private void PedigreeCreatorFrm_Load(object sender, EventArgs e)
        {
            this.Text = "Autosomal Pedigree Creator v" + Application.ProductVersion;
            if(!Directory.Exists("data"))
                Directory.CreateDirectory("data");
            if (!Directory.Exists("ibd"))
                Directory.CreateDirectory("ibd");
            if (!Directory.Exists("tmp"))
                Directory.CreateDirectory("tmp");
        }

        private string sanatize(string file)
        {
            string new_file_name = Regex.Replace(file, "[^A-Za-z0-9]", "");
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
            backgroundWorker1.ReportProgress(0, "0% Initializing ...");
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

            backgroundWorker1.ReportProgress(10, "10% Complete.");

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

            backgroundWorker1.ReportProgress(15, "15% Complete.");
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
            backgroundWorker1.ReportProgress(75, "75% Complete.");
            copyFilesFromFolder("data", "ibd");
            deleteFilesFromFolder("data");

            // cleanup own file
            foreach (string file in files)
            {
                if (File.Exists("ibd\\" + sanatize(Path.GetFileNameWithoutExtension(file))))
                    File.Delete("ibd\\" + sanatize(Path.GetFileNameWithoutExtension(file)));
            }

            preparelist.PrepareList.doPrepareList();

            backgroundWorker1.ReportProgress(90, "90% Complete.");

            genxml.GenXML.doGenXML();

            xml2gv.Xml2GraphViz.doXml2GraphViz(dump_all);

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
            if (File.Exists("common_ancestors.csv"))
                File.Move("common_ancestors.csv", "tmp\\common_ancestors.csv");
            
            backgroundWorker1.ReportProgress(100, "100% Complete.");
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
            if (listBox1.Items.Count > 1000)
                listBox1.Items.Clear();
            listBox1.Items.Add(e.UserState.ToString());
            listBox1.SelectedIndex = listBox1.Items.Count - 1;
            if (e.ProgressPercentage != -1)
                progressBar1.Value = e.ProgressPercentage;
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
                button5.Enabled = true;
                dir_path = dlg.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Version "+Application.ProductVersion+"\r\n\r\nA tool to create pedigrees from a set of autosomal files.\r\n\r\nDeveloper: Felix Chandrakumar <i@fc.id.au>\r\nWebsite: y-str.org", "Autosomal Pedigree Creator", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.y-str.org/");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("1. Rename all kits to have some meaning filenames. No numbers please.\r\n2. Place all kits into one folder.\r\n3. Browse and select the folder.\r\n4. The final output will be an image file which will automatically open.\r\n\r\nTo get more indepth details, please visit website.", "Quick Help");
        }

        private void checkBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (MessageBox.Show("Dump all tries to connect every single matching segment which may make the pedigree clumsy. Use this option only of the set if the autosomal files are totally unrelated to each other. Are you sure you want to dump all?", "Question?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (!checkBox1.Checked)
                    checkBox1.Checked = true;
            }
            else
            {
                if (checkBox1.Checked)
                    checkBox1.Checked = false;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("The process of comparing several autosomal files to draw a tree can range from a few minutes to several hours or even days depending on the number of autosomal files. Click 'Yes' to proceed. Otherwise, click 'No'","Please Read ...",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)
            {
                button1.Enabled = false;
                dump_all = checkBox1.Checked;
                backgroundWorker1.RunWorkerAsync(dir_path);
            }
        }
    }
}
