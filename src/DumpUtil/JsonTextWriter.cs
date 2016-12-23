
namespace DumpUtil
{

    using System;
    using System.Globalization;
    using System.IO;
    using System.Xml;


    /// <summary>
    /// Represents a writer that provides a fast, non-cached, forward-only 
    /// way of generating streams or files containing JSON Text according
    /// to the grammar rules laid out in <a href="http://www.ietf.org/rfc/rfc4627.txt">RFC 4627</a>.
    /// </summary>
    /// <remarks>
    /// https://github.com/mikefrey/ELMAH/blob/master/src/Elmah/JsonTextWriter.cs
    /// </remarks>
    public sealed class JsonTextWriter
    {
        private readonly TextWriter _writer;
        private readonly int[] _counters;
        private readonly char[] _terminators;
        private int _depth;
        private string _memberName;

        public JsonTextWriter(TextWriter writer)
        {
            //Debug.Assert(writer != null);
            _writer = writer;
            const int levels = 10 + /* root */ 1;
            _counters = new int[levels];
            _terminators = new char[levels];
        }

        public int Depth
        {
            get { return _depth; }
        }

        private int ItemCount
        {
            get { return _counters[Depth]; }
            set { _counters[Depth] = value; }
        }

        private char Terminator
        {
            get { return _terminators[Depth]; }
            set { _terminators[Depth] = value; }
        }

        public JsonTextWriter Object()
        {
            return StartStructured("{", "}");
        }

        public JsonTextWriter EndObject()
        {
            return Pop();
        }

        public JsonTextWriter Array()
        {
            return StartStructured("[", "]");
        }

        public JsonTextWriter EndArray()
        {
            return Pop();
        }

        public JsonTextWriter Pop()
        {
            return EndStructured();
        }

        public JsonTextWriter Member(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name.Length == 0) throw new ArgumentException(null, "name");
            if (_memberName != null) throw new InvalidOperationException("Missing member value.");
            _memberName = name;
            return this;
        }

        private JsonTextWriter Write(string text)
        {
            return WriteImpl(text, /* raw */ false);
        }

        private JsonTextWriter WriteEnquoted(string text)
        {
            return WriteImpl(text, /* raw */ true);
        }

        private JsonTextWriter WriteImpl(string text, bool raw)
        {
            //Debug.Assert(raw || (text != null && text.Length > 0));

            if (Depth == 0 && (text.Length > 1 || (text[0] != '{' && text[0] != '[')))
                throw new InvalidOperationException();

            TextWriter writer = _writer;

            if (ItemCount > 0)
                writer.Write(',');

            string name = _memberName;
            _memberName = null;

            if (name != null)
            {
                writer.Write(' ');
                Enquote(name, writer);
                writer.Write(':');
            }

            if (Depth > 0)
                writer.Write(' ');

            if (raw)
                Enquote(text, writer);
            else
                writer.Write(text);

            ItemCount = ItemCount + 1;

            return this;
        }

        public JsonTextWriter Number(int value)
        {
            return Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public JsonTextWriter String(string str)
        {
            return str == null ? Null() : WriteEnquoted(str);
        }

        public JsonTextWriter Null()
        {
            return Write("null");
        }

        public JsonTextWriter Boolean(bool value)
        {
            return Write(value ? "true" : "false");
        }

        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);


        public JsonTextWriter Number(DateTime time)
        {
            double seconds = time.ToUniversalTime().Subtract(_epoch).TotalSeconds;
            return Write(seconds.ToString(CultureInfo.InvariantCulture));
        }

        public JsonTextWriter String(DateTime time)
        {
            string xmlTime;

            xmlTime = XmlConvert.ToString(time, XmlDateTimeSerializationMode.Utc);

            return String(xmlTime);
        }

        private JsonTextWriter StartStructured(string start, string end)
        {
            if (Depth + 1 == _counters.Length)
                throw new Exception();

            Write(start);
            _depth++;
            Terminator = end[0];
            return this;
        }

        private JsonTextWriter EndStructured()
        {
            if (Depth - 1 < 0)
                throw new Exception();

            _writer.Write(' ');
            _writer.Write(Terminator);
            ItemCount = 0;
            _depth--;
            return this;
        }

        static string NullString(string s)
        {
            return s == null ? string.Empty : s;
        }

        private static void Enquote(string s, TextWriter writer)
        {
            //Debug.Assert(writer != null);

            int length = NullString(s).Length;

            writer.Write('"');

            char last;
            char ch = '\0';

            for (int index = 0; index < length; index++)
            {
                last = ch;
                ch = s[index];

                switch (ch)
                {
                    case '\\':
                    case '"':
                        {
                            writer.Write('\\');
                            writer.Write(ch);
                            break;
                        }

                    case '/':
                        {
                            if (last == '<')
                                writer.Write('\\');
                            writer.Write(ch);
                            break;
                        }

                    case '\b': writer.Write("\\b"); break;
                    case '\t': writer.Write("\\t"); break;
                    case '\n': writer.Write("\\n"); break;
                    case '\f': writer.Write("\\f"); break;
                    case '\r': writer.Write("\\r"); break;

                    default:
                        {
                            if (ch < ' ')
                            {
                                writer.Write("\\u");
                                writer.Write(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                writer.Write(ch);
                            }

                            break;
                        }
                }
            }

            writer.Write('"');
        }
    }
}