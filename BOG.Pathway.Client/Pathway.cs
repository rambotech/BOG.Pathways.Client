using System;
using System.Collections.Generic;
using System.Text;

namespace BOG.Pathway.Client
{
    public class Pathway
    {
        public string ID { get; set; }
        public string ReadToken { get; set; }
        public string WriteToken { get; set; }
        public int maxPayloads { get; set; }
        public int maxReferences { get; set; }
    }
}
