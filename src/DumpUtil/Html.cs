namespace DumpUtil
{
    #region (using)
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Xml;
    #endregion

    #region HTML

    #region HtmlWriter

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

    #endregion

    #region HtmlTag

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

    #endregion

    #region HtmlHead



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


    #endregion

    #region JavaScriptTextWriter


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

    #endregion

    #region JsonTextWriter

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

    #endregion

    #region IndentedTextWriter

    /// <summary>
    ///     The same as <see cref="System.CodeDom.Compiler.IndentedTextWriter" /> but works in partial trust.
    /// </summary>
    public class IndentedTextWriter : TextWriter
    {
        /// <summary>
        ///     Specifies the default tab string. This field is constant.
        /// </summary>
        public const string DefaultTabString = "    ";

        private readonly TextWriter _writer;
        private int _indentLevel;
        private bool _tabsPending;
        private readonly string _tabString;


        /// <summary>
        ///     Gets the encoding for the text writer to use.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Text.Encoding" /> that indicates the encoding for the text writer to use.
        /// </returns>
        public override Encoding Encoding
        {
            get { return _writer.Encoding; }
        }

        /// <summary>
        ///     Gets or sets the new line character to use.
        /// </summary>
        /// <returns> The new line character to use. </returns>
        public override string NewLine
        {
            get { return _writer.NewLine; }
            set { _writer.NewLine = value; }
        }

        /// <summary>
        ///     Gets or sets the number of spaces to indent.
        /// </summary>
        /// <returns> The number of spaces to indent. </returns>
        public int Indent
        {
            get { return _indentLevel; }
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                _indentLevel = value;
            }
        }

        /// <summary>
        ///     Gets the <see cref="T:System.IO.TextWriter" /> to use.
        /// </summary>
        /// <returns>
        ///     The <see cref="T:System.IO.TextWriter" /> to use.
        /// </returns>
        public TextWriter InnerWriter
        {
            get { return _writer; }
        }

        /// <summary>
        ///     Initializes a new instance of the IndentedTextWriter class using the specified text writer and default tab string.
        /// </summary>
        /// <param name="writer">
        ///     The <see cref="T:System.IO.TextWriter" /> to use for output.
        /// </param>
        public IndentedTextWriter(TextWriter writer)
            : this(writer, DefaultTabString, false)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the IndentedTextWriter class using the specified text writer and tab string.
        /// </summary>
        /// <param name="writer">
        ///     The <see cref="T:System.IO.TextWriter" /> to use for output.
        /// </param>
        /// <param name="tabString"> The tab string to use for indentation. </param>
        public IndentedTextWriter(TextWriter writer, string tabString, bool disableNewLine)
            : base(CultureInfo.InvariantCulture)
        {
            _writer = writer;
            _tabString = tabString;
            _indentLevel = 0;
            _tabsPending = false;
            if (disableNewLine) _writer.NewLine = " ";
        }

        /// <summary>
        ///     Closes the document being written to.
        /// </summary>
        public override void Close()
        {
            _writer.Close();
        }

        /// <summary>
        ///     Flushes the stream.
        /// </summary>
        public override void Flush()
        {
            _writer.Flush();
        }

        /// <summary>
        ///     Outputs the tab string once for each level of indentation according to the
        ///     <see
        ///         cref="P:System.CodeDom.Compiler.IndentedTextWriter.Indent" />
        ///     property.
        /// </summary>
        protected virtual void OutputTabs()
        {
            if (_tabString == "") return;

            if (!_tabsPending)
            {
                return;
            }
            for (var index = 0; index < _indentLevel; ++index)
            {
                _writer.Write(_tabString);
            }
            _tabsPending = false;
        }

        /// <summary>
        ///     Writes the specified string to the text stream.
        /// </summary>
        /// <param name="value"> The string to write. </param>
        public override void Write(string value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of a Boolean value to the text stream.
        /// </summary>
        /// <param name="value"> The Boolean value to write. </param>
        public override void Write(bool value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a character to the text stream.
        /// </summary>
        /// <param name="value"> The character to write. </param>
        public override void Write(char value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes a character array to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write. </param>
        public override void Write(char[] buffer)
        {
            OutputTabs();
            _writer.Write(buffer);
        }

        /// <summary>
        ///     Writes a subarray of characters to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write data from. </param>
        /// <param name="index"> Starting index in the buffer. </param>
        /// <param name="count"> The number of characters to write. </param>
        public override void Write(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.Write(buffer, index, count);
        }

        /// <summary>
        ///     Writes the text representation of a Double to the text stream.
        /// </summary>
        /// <param name="value"> The double to write. </param>
        public override void Write(double value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of a Single to the text stream.
        /// </summary>
        /// <param name="value"> The single to write. </param>
        public override void Write(float value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of an integer to the text stream.
        /// </summary>
        /// <param name="value"> The integer to write. </param>
        public override void Write(int value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of an 8-byte integer to the text stream.
        /// </summary>
        /// <param name="value"> The 8-byte integer to write. </param>
        public override void Write(long value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes the text representation of an object to the text stream.
        /// </summary>
        /// <param name="value"> The object to write. </param>
        public override void Write(object value)
        {
            OutputTabs();
            _writer.Write(value);
        }

        /// <summary>
        ///     Writes out a formatted string, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string. </param>
        /// <param name="arg0"> The object to write into the formatted string. </param>
        public override void Write(string format, object arg0)
        {
            OutputTabs();
            _writer.Write(format, arg0);
        }

        /// <summary>
        ///     Writes out a formatted string, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg0"> The first object to write into the formatted string. </param>
        /// <param name="arg1"> The second object to write into the formatted string. </param>
        public override void Write(string format, object arg0, object arg1)
        {
            OutputTabs();
            _writer.Write(format, arg0, arg1);
        }

        /// <summary>
        ///     Writes out a formatted string, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg"> The argument array to output. </param>
        public override void Write(string format, params object[] arg)
        {
            OutputTabs();
            _writer.Write(format, arg);
        }

        /// <summary>
        ///     Writes the specified string to a line without tabs.
        /// </summary>
        /// <param name="value"> The string to write. </param>
        public void WriteLineNoTabs(string value)
        {
            _writer.WriteLine(value);
        }

        /// <summary>
        ///     Writes the specified string, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The string to write. </param>
        public override void WriteLine(string value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a line terminator.
        /// </summary>
        public override void WriteLine()
        {
            OutputTabs();
            _writer.WriteLine();
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a Boolean, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The Boolean to write. </param>
        public override void WriteLine(bool value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a character, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The character to write. </param>
        public override void WriteLine(char value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a character array, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write. </param>
        public override void WriteLine(char[] buffer)
        {
            OutputTabs();
            _writer.WriteLine(buffer);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes a subarray of characters, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="buffer"> The character array to write data from. </param>
        /// <param name="index"> Starting index in the buffer. </param>
        /// <param name="count"> The number of characters to write. </param>
        public override void WriteLine(char[] buffer, int index, int count)
        {
            OutputTabs();
            _writer.WriteLine(buffer, index, count);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a Double, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The double to write. </param>
        public override void WriteLine(double value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a Single, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The single to write. </param>
        public override void WriteLine(float value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of an integer, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The integer to write. </param>
        public override void WriteLine(int value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of an 8-byte integer, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The 8-byte integer to write. </param>
        public override void WriteLine(long value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of an object, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> The object to write. </param>
        public override void WriteLine(object value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes out a formatted string, followed by a line terminator, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string. </param>
        /// <param name="arg0"> The object to write into the formatted string. </param>
        public override void WriteLine(string format, object arg0)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes out a formatted string, followed by a line terminator, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg0"> The first object to write into the formatted string. </param>
        /// <param name="arg1"> The second object to write into the formatted string. </param>
        public override void WriteLine(string format, object arg0, object arg1)
        {
            OutputTabs();
            _writer.WriteLine(format, arg0, arg1);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes out a formatted string, followed by a line terminator, using the same semantics as specified.
        /// </summary>
        /// <param name="format"> The formatting string to use. </param>
        /// <param name="arg"> The argument array to output. </param>
        public override void WriteLine(string format, params object[] arg)
        {
            OutputTabs();
            _writer.WriteLine(format, arg);
            _tabsPending = true;
        }

        /// <summary>
        ///     Writes the text representation of a UInt32, followed by a line terminator, to the text stream.
        /// </summary>
        /// <param name="value"> A UInt32 to output. </param>
        public override void WriteLine(uint value)
        {
            OutputTabs();
            _writer.WriteLine(value);
            _tabsPending = true;
        }
    }


    #endregion

    #endregion
}
