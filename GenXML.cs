using Pedigree_Creator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace genxml
{
    public class GenXML
    {
       static Dictionary<string, string> kitmap = new Dictionary<string, string>();
       public static void doGenXML()
       {
           Program.addLog("Loading kit names ...");
           string curr_dir = "";

           

           //debug
           //curr_dir = @"D:\Genetics\Ancient-DNA\";

           if (!File.Exists(curr_dir + "atree.txt"))
           {
               Program.addLog("atree.txt does not exist!");
               return;
           }

           string[] lines = null;
           string[] data = null;           
           //


           lines = File.ReadAllLines(curr_dir+"atree.txt");
           data = null;

           List<CommonAncestor> tree = new List<CommonAncestor>();

           foreach (string line in lines)
           {
               data = line.Split(new char[] { ',' });
               CommonAncestor ancestor = new CommonAncestor();
               ancestor.name = data[0];
               ancestor.id = data[1];
               ancestor.kits = new List<string>(data[1].Split(new char[] { '-' }));
               ancestor.segments = new List<Segment>();
               foreach (string segstr in data[2].Split(new char[] { '_' }))
               {
                   ancestor.segments.Add(new Segment(segstr.Split(new char[] { ':' })));
               }
               ancestor.descendents = new List<CommonAncestor>();

               addToTree(tree, ancestor);
           }
           string ca_xml = "<CA NAME='ADAM-EVE'>" + drawTree(tree) + "\r\n</CA>";

           File.WriteAllText("atree.xml", ca_xml);
           Program.addLog("atree.xml successfully written.");
       }



        private static string drawTree(List<CommonAncestor> descendents) {
		 string xml="";
		foreach(CommonAncestor ancestor in descendents)
		{
			xml=xml+"\r\n<CA NAME='"+ancestor.name+"' ID='"+ancestor.id+"'>\r\n";
			xml=xml+ getKits(ancestor.kits);
			xml=xml+ getSegments(ancestor.segments);
			xml=xml+ drawTree(ancestor.descendents);
			xml=xml+"\r\n</CA>";
		}
		return xml;
	}

        private static string getSegments(List<Segment> segments) {
		string tag2="<SEGMENTS>";
		foreach(Segment seg in segments)
			tag2+="<SEGMENT CHR='"+seg.chromosome+"' START='"+seg.start+"'  END='"+seg.end+"'/>";
		tag2+="</SEGMENTS>";
		return tag2;
	}

        private static string getKits(List<string> kits) {
		string kits2="<KITS>";
        foreach (string kit in kits)
        {
            if(kitmap.ContainsKey(kit))
                kits2 += "<KIT ID='" + kit + "' NAME='" + kitmap[kit] + "'/>";
            else
                kits2 += "<KIT ID='" + kit + "' NAME='" + kit + "'/>";
        }
		kits2+="</KITS>";
		return kits2;
	}

        private static void addToTree(List<CommonAncestor> tree, CommonAncestor ancestor) {
		foreach(CommonAncestor par in tree)
			if(AncestorKitsPresent(par,ancestor))
			{
				addToTree(par.descendents, ancestor);
				return;
			}
		tree.Add(ancestor);
	}

        private static bool AncestorKitsPresent(CommonAncestor par,
                CommonAncestor ancestor) {
		List<string> pkits=par.kits;
		List<string> akits=ancestor.kits;
		foreach(string kit in akits)
			if(!pkits.Contains(kit))
				return false;
		return true;
	}
    }
}
