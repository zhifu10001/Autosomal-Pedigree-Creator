using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace genxml
{
    class CommonAncestor
    {
        public String name = "";
        public List<String> kits = new List<String>();
        public String id = "";
        public List<Segment> segments = new List<Segment>();

        public List<CommonAncestor> descendents = new List<CommonAncestor>();
    }
}
