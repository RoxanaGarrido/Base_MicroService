using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base_MicroService
{
    public class MemoryUpdateMessage
    {
        public long ID { get; set; }
        public string Text { get; set; }
        public int Gen1CollectionCount { get; set; }
        public int Gen2CollectionCount { get; set; }
        public float TimeSpentPercent { get; set; }
        public string MemoryBeforeCollection { get; set; }
        public string MemoryAfterCollection { get; set; }
        public DateTime Date { get; set; }
    }
}
