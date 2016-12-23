

namespace DumpUtil.Html
{

    using System;
    using System.IO;
    using System.Web;

    public class HtmlTag : HtmlWriter
    {
        public override TextWriter Writer
        {
            get
            {
                return InnerWriter.InnerWriter;
            }
        }

        public bool NewLineAfterSTag { get; set; } = true;

        string _name;
        public bool EndTagWritten { get; private set; }
        public bool STagEndWritten { get; private set; }

        internal HtmlTag(HtmlWriter writer, string name) : base(writer.InnerWriter)
        {
            _name = name;

            InnerWriter.Write($"<{name}");
        }

        public void WriteEndTag()
        {
            if (EndTagWritten) return;
            WriteSTagEnd();
            InnerWriter.Indent--;
            InnerWriter.WriteLine($"</{_name}>");
            EndTagWritten = true;
        }

        public void WriteSTagEnd()
        {
            if (STagEndWritten) return;
            InnerWriter.Write("");
            InnerWriter.Indent++;

            if (NewLineAfterSTag) InnerWriter.WriteLine(">");
            else InnerWriter.Write(">");

            STagEndWritten = true;
        }

        public override void Head(Action<HtmlHead> work)
        {
            throw new Exception("Head cannot exist in tag");
        }

        public override void Tag(string name, Action<HtmlTag> work)
        {
            WriteSTagEnd();
            base.Tag(name, work);
        }

        public override void WriteRaw(string rawText)
        {
            WriteSTagEnd();
            base.WriteRaw(rawText);
        }

        public override void WriteLineRaw(string rawText)
        {
            WriteSTagEnd();
            base.WriteLineRaw(rawText);
        }

        protected override void TagSpecial<T>(Action<T> work)
        {
            WriteSTagEnd();
            base.TagSpecial(work);
        }

        public void Attribute(string name, string value)
        {
            if (STagEndWritten) throw new Exception();

            Writer.Write($" {name}=\"{HttpUtility.HtmlAttributeEncode(value)}\"");
        }
    }
}