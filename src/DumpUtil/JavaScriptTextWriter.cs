

namespace DumpUtil
{


    using System;
    using System.IO;
    using System.Text;


    public sealed class JavaScriptTextWriter : TextWriter
    {
        private TextWriter _writer;

        public override Encoding Encoding
        {
            get { return _writer.Encoding; }
        }

        public JavaScriptTextWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public override void Write(string value)
        {
            _writer.Write(value);
        }

        public override void Write(char value)
        {
            _writer.Write(value);
        }


        public void WriteJson(Action<JsonTextWriter> act)
        {
            JsonTextWriter jtw = new JsonTextWriter(_writer);

            act(jtw);
        }

        public void Semi() { WriteLine(";"); }
    }
}