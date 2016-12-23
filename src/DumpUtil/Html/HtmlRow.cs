
namespace DumpUtil.Html
{

    public class HtmlRow : HtmlTag
    {
        public HtmlRow(HtmlWriter writer) : base(writer, "tr")
        {

        }


        public void Column(string text)
        {
            Tag("td", td =>
            {
                td.NewLineAfterSTag = false;
                if (string.IsNullOrWhiteSpace(text))
                {
                    td.WriteRaw(" ");
                    return;
                }
                td.WriteRaw(text);
            });
        }
    }



}
