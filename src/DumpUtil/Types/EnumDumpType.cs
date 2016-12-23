
namespace DumpUtil
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Html;

    abstract class BaseEnumDumpType:DumpType
    {
        
    }

    class EnumDumpType : BaseEnumDumpType
    {

        public override int Priority
        {
            get
            {
                return 5;
            }
        }

        public override bool AcceptsType(Type t)
        {
            return t.IsEnum;
        }

        public override void Write(object o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
          var val =  o.ToString();
            tag.Tag("kbd", t =>
            {
                t.NewLineAfterSTag = false;
                t.WriteString(val);
            });
        }
    }


    //class FlagEnumDumpType: BaseEnumDumpType
    //{

    //}
}
