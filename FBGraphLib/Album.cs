using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBGraphLib
{
    public class Album
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<Upload> Uploads { get; set; }
    }
}
