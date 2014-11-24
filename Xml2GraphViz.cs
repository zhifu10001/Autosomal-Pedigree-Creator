using Pedigree_Creator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

namespace xml2gv
{
    class Xml2GraphViz
    {
        static StringBuilder sb = null;

        static Dictionary<string, List<string>> kvp = new Dictionary<string, List<string>>();
        static Dictionary<string, string> namesdb = new Dictionary<string, string>();
        static Random r = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
        static XmlDocument doc = null;
        static bool dump_all = false;
        public static void doXml2GraphViz(bool m_dump_all)
        {
            dump_all = m_dump_all;
            if(!File.Exists("atree.xml"))
            {
                Program.addLog("atree.xml does not exist!");
                return;
            }
            doc = new XmlDocument();
            doc.Load("atree.xml");
            sb = new StringBuilder();
            sb.Append("digraph {\r\n");
            foreach (XmlNode node in doc.SelectNodes("CA"))
            {
                parseXml("Adam_Eve",node);
            }

            int total_ca = kvp.Keys.Count;
            int prev_total_ca = -1;

            if (!dump_all)
            {
                while (true)
                {
                    removeTerminalCAs();
                    total_ca = kvp.Keys.Count;
                    if (total_ca == prev_total_ca)
                        break;
                    prev_total_ca = total_ca;
                }
            }


            string key1 = null;
            string value1 = null;
            foreach(string key in kvp.Keys)
            {
                foreach(string value in kvp[key])
                {
                    if (key.StartsWith("CA_"))
                        key1 = key.Substring(3);
                    else
                        key1 = key;
                    if (value.StartsWith("CA_"))
                        value1 = value.Substring(3);
                    else
                        value1 = value;
                    sb.Append("\""+key1 + "\" -> \"" + value1 + "\";\r\n");
                }
            }


            sb.Append("}\r\n");
            File.WriteAllText("tree.gv", sb.ToString());

            StringBuilder sb1 = new StringBuilder();
            sb1.Append("\"COMMON_ANCESTOR_ID\",\"DESCENDENTS\"\r\n");
            foreach (string name in namesdb.Keys)
                sb1.Append("\"" + name + "\",\"" + namesdb[name] + "\"\r\n");
            File.WriteAllText("common_ancestors.csv ", sb1.ToString());
            
            Program.addLog("xml -> gv done.");
        }

        private static void removeTerminalCAs()
        {
            Dictionary<string, List<string>> kvp2 = new Dictionary<string,List<string>>();

            foreach(string ca in kvp.Keys)
            {
                if (kvp2.ContainsKey(ca))
                    continue;
                List<string> new_children=new List<string>();
                foreach(string child in kvp[ca])
                {
                    if (!child.StartsWith("CA_")) // terminal
                        new_children.Add(child);
                    else if (kvp.ContainsKey(child))
                    {
                            new_children.Add(child);
                    }
                }
                kvp2.Remove(ca);
                if (new_children.Count>0)
                    kvp2.Add(ca, new_children);
            }
            kvp = kvp2;
        }

        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (T)formatter.Deserialize(ms);
            }
        }

        private static void parseXml(string parent, XmlNode node)
        {
            if (node.Name != "CA")
                return;
            if (parent != "" && parent != "Adam_Eve")
            {
                //sb.Append(parent + " -> " + getName(node.Attributes["NAME"].Value) + ";\r\n");
                List<string> children = null;
                if (kvp.ContainsKey(parent))
                {
                    children = kvp[parent];
                    kvp.Remove(parent);
                }
                else
                    children = new List<string>();
                children.Add(getName(node.Attributes["NAME"].Value));
                kvp.Add(parent, children);
            }
            if (node.SelectNodes("CA").Count > 0)
            {
                foreach (XmlNode n in node.SelectNodes("CA"))
                {
                    parseXml(getName(node.Attributes["NAME"].Value), n);
                }
            }
            else
            {
                // terminal node...
                List<string> children = null;
                string p = getName(node.Attributes["NAME"].Value);
                if (kvp.ContainsKey(p))
                {
                    children = kvp[p];
                    kvp.Remove(p);
                }
                else
                    children = new List<string>();


                    foreach (XmlNode n in node.SelectSingleNode("KITS").ChildNodes)
                    {
                        children.Add(n.Attributes["NAME"].Value);
                    }

                    if (!dump_all)
                    {
                        if (isParentChildLine(node))
                            kvp.Add(p, children);
                    }
                    else
                    {
                        kvp.Add(p, children);
                    }
            }
        }


        private static bool isChild(string child_kit)
        {
            XmlNodeList nodes= doc.SelectNodes("//CA");
            foreach(XmlNode node in nodes)
            {
                if (node.Attributes["NAME"].Value.IndexOf(child_kit) != -1 && node.Attributes["NAME"].Value.Split(new char[] { '/' }).Length == 2 && isParentChildLine(node))
                    return true;
            }
            return false;
        }


        private static bool isParentChildLine(XmlNode node)
        {
            uint total = 0;
            uint end = 0;
            uint start = 0;
            foreach (XmlNode n in node.SelectSingleNode("SEGMENTS").ChildNodes)
            {
                start = uint.Parse(n.Attributes["START"].Value);
                end = uint.Parse(n.Attributes["END"].Value);
                total = total + (end - start);
            }
            if (total > 1400000000)
                return true;
            else
                return false;
        }

        private static string parentLine(XmlNode node)
        {
            uint total = 0;
            uint end = 0;
            uint start = 0;
            foreach (XmlNode n in node.SelectSingleNode("SEGMENTS").ChildNodes)
            {
                start = uint.Parse(n.Attributes["START"].Value);
                end = uint.Parse(n.Attributes["END"].Value);
                total = total + (end - start);
            }
            if (total > 1400000000)            
                return "";
            else
                return " [style=\"dotted\", arrowsize=\"0\"]";
        }

        private static string getName(string p)
        {
            if (namesdb.ContainsKey(p))
                return namesdb[p];
            else
            {
                string new_name = "CA_"+RandomString(4);
                if(!namesdb.ContainsValue(new_name))
                {
                    namesdb.Add(p, new_name);
                    return new_name;
                }
                else
                    return getName(p);
            }
        }

        private static string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * r.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
