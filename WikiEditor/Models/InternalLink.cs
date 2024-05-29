using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WikiEditor.Models
{
    public class InternalLink
    {
        public string HeaderName { get; set; }
        public int Level { get; set; }

        public InternalLink() { }
    }
}
