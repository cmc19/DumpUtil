using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpUtil
{
    public class DumpHtmlWriter : HtmlWriter
    {

        public void WriteStyles()
        {
            DumpManager.WriteStyle(this);
        }

        public void WriteJavaScript()
        {
            DumpManager.WriteScript(this);
        }

        public void Dump(object obj)
        {
            DumpManager.Dump(obj, this);
            WriteRaw("<br />");
        }
    }
}
