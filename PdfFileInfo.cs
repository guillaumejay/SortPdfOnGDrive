using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortPDFOnGDrive
{
    public class PdfFileInfo
    {
        public PdfFileInfo(string file)
        {
            File = file;
        }

        public string File { get; set; }
        public string Author { get; set; }
        public string Producer { get; set; }
        public string Creator { get; set; }

       
    }
}
