using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WikiEditor.Models
{
    public class Tag
    {
        public string Name { get; set; }
        public Color Color { get; set; }

        public Tag() { }

        public static Tag Unknown { get; set; } = new Tag() { Name= "Unbekannt", Color = Colors.Gray };
    }
}
