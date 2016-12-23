
namespace DumpUtil.Html
{

    using System;
    public class HtmlTable : HtmlTag
    {
        public HtmlTable(HtmlWriter writer) : base(writer, "table")
        {

        }

        public void Row(Action<HtmlRow> act)
        {
            TagSpecial(act);
        }
    }
}