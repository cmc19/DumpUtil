

namespace DumpUtil.Html
{
    using System;
    using System.IO;
    using System.Web;

    public class HtmlWriter
    {
        IndentedTextWriter _writer;

        public virtual TextWriter Writer => _writer.InnerWriter;

        internal IndentedTextWriter InnerWriter => _writer;

        internal HtmlWriter(IndentedTextWriter itw)
        {
            _writer = itw;
        }
        public HtmlWriter(bool noTabs = false, bool disableNewLines = false) : this(new StringWriter(), noTabs: noTabs, disableNewLines: disableNewLines) { }

        public HtmlWriter(TextWriter writer, bool noTabs = false, bool disableNewLines = false)
        {
            if (noTabs) _writer = new IndentedTextWriter(writer, "", disableNewLines);
            else _writer = new IndentedTextWriter(writer, tabString: "\t", disableNewLine: disableNewLines);
        }

        public override string ToString()
        {
            return _writer.InnerWriter.ToString();
        }

        public virtual void Tag(string name, Action<HtmlTag> work)
        {

            var tag = new HtmlTag(this, name);
            work(tag);
            tag.WriteEndTag();
        }

        public virtual void Head(Action<HtmlHead> work)
        {

            var tag = new HtmlHead(this);
            work(tag);
            tag.WriteEndTag();
        }

        protected virtual void TagSpecial<T>(Action<T> work)
                where T : HtmlTag
        {

            var tag = (T)Activator.CreateInstance(typeof(T), new object[] { this });
            work(tag);
            tag.WriteEndTag();
        }

        public void Table(Action<HtmlTable> table)
        {
            TagSpecial(table);
        }


        public virtual void WriteRaw(string rawText)
        {
            _writer.Write(rawText);
        }

        public virtual void WriteLineRaw(string rawText)
        {
            _writer.WriteLine(rawText);
        }

        public void WriteString(string str, bool asPre = false)
        {
            if (asPre) WriteRaw("<pre>");
            WriteRaw(HttpUtility.HtmlEncode(str));
            if (asPre) WriteRaw("</pre>");
        }

        public void WriteDiv(Action<HtmlTag> act) => Tag("div", act);
        public void WriteSpan(Action<HtmlTag> act) => Tag("span", act);


        void STag(string name, bool withIndent = true)
        {
            //this forces the indenter to not auto indent the actual tag
            _writer.Write("");
            if (withIndent)
            {
                _writer.Indent++;
            }
            _writer.WriteLine($"<{name}>");
        }

        void ETag(string name, bool withIndent = true)
        {
            if (withIndent)
            {
                _writer.Indent--;
            }
            _writer.WriteLine($"</{name}>");
        }

        public void HtmlOpen() => STag("html", false);
        public void HtmlClose() => ETag("html", false);


        public void BodyOpen() => STag("body");
        public void BodyClose() => ETag("body");


        public void WriteJavascript(Action<JavaScriptTextWriter> act)
        {
            Tag("script", s =>
            {
                s.NewLineAfterSTag = false;
                s.WriteSTagEnd();
                var js = new JavaScriptTextWriter(s.Writer);
                js.WriteLine();
                act(js);

                s.WriteLineRaw("");
            });
        }

    }
}