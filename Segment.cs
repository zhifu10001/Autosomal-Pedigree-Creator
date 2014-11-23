using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace genxml
{
    class Segment
    {
       public int chromosome;
       public int start;
       public int end;
	
	public Segment(String[] seg_str) {
		this.chromosome=int.Parse(seg_str[0]);
		this.start=int.Parse(seg_str[1]);
		this.end=int.Parse(seg_str[2]);
	}
	
	public Segment(Segment s1,Segment s2) {
		this.chromosome=s1.chromosome;
		this.start=s1.start<s2.start?s1.start:s2.start;
		this.end=s1.end>s2.end?s1.end:s2.end;
	}
	
	public bool isOverlap(Segment seg)
	{
		bool flag = false;
		if(this.chromosome==seg.chromosome)
		{
			if(this.start < seg.start && (this.end-seg.start > (this.end-this.start)*3/4 || this.end-seg.start > (seg.end-seg.start)*3/4))
				flag = true;
			else if(seg.start < this.start && (seg.end-this.start > (seg.end-seg.start)*3/4 || seg.end-this.start > (this.end-this.start)*3/4))
				flag = true;
		}
		return flag;
	}
	
	public Segment inList(List<Segment> list)
	{
		foreach(Segment s in list)
		{
			if(isOverlap(s))
				return s;
		}
		return null;
	}

	public bool equals(Object s_obj) {
		Segment s = ((Segment)s_obj);
		if(this.chromosome==s.chromosome && this.start==s.start && this.end==s.end)
			return true;
		else
			return false;
	}
	
	public String toString() {
		String str= "chr"+chromosome+":"+start+"-"+end;
		return str;
	}
    }
}
