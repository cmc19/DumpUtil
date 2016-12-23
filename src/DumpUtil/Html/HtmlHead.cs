namespace DumpUtil.Html
{


    public class HtmlHead : HtmlTag
    {

        public HtmlHead(HtmlWriter writer) : base(writer, "head")
        {

        }

        public void Title(string str)
        {
            Tag("title", t =>
            {
                t.NewLineAfterSTag = false;
                t.WriteRaw(str);
            });
        }
    }
}