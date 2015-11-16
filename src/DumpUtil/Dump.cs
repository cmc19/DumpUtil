namespace DumpUtil
{
    #region (using)
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;
    #endregion

    #region Dump


    #region RefDictionary

    sealed class RefDictionary
    {
        readonly HashSet<long> _set = new HashSet<long>();
        readonly ObjectIDGenerator _gen = new ObjectIDGenerator();

        public bool Add(object o)
        {
            if (o == null) return true;
            if (o.GetType().IsClass == false) return true;

            bool firstTime;
            var x = _gen.GetId(o, out firstTime);
            return _set.Add(x);

        }

        public long? GetID(object o)
        {
            if (o == null) return null;
            if (o.GetType().IsClass == false) return null;

            bool firstTime;
            return _gen.GetId(o, out firstTime);
        }
    }

    #endregion


    #region DumpState

    sealed class DumpState
    {
        public bool InList { get; }
        public int Level { get; }
        public DumpState() { }
        private DumpState(int level, bool inList)
        {
            InList = inList;
            Level = level;
        }
        public DumpState BumpLevel()
        {
            return new DumpState(level: Level + 1, inList: InList);
        }
        public DumpState AsList(bool list = true)
        {
            return new DumpState(level: Level, inList: list);
        }
    }

    #endregion

    public static class DumpManager
    {

        static readonly List<DumpType> Types;

        static DumpManager()
        {
            Types = GetEnumerableOfType<DumpType>().OrderByDescending(x => x.Priority).ToList();
        }

        static IEnumerable<T> GetEnumerableOfType<T>(params object[] constructorArgs)
            where T : class
        {
            var q = Assembly.GetAssembly(typeof(T))
                            .GetTypes()
                            .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)));
            return q.Select(type => (T)Activator.CreateInstance(type, constructorArgs))
                    .ToList();
        }

        public static void WriteStyle(HtmlWriter writer)
        {

            writer.Tag("style", s =>
            {
                s.NewLineAfterSTag = false;
                s.WriteRaw(Res.Style);
            });
        }

        public static void WriteScript(HtmlWriter writer)
        {
            writer.WriteRaw("<script>");

         //   writer.WriteRaw(System.Text.Encoding.Default.GetString(Res.Javascript));
            writer.WriteRaw(Res.Script);

            writer.WriteRaw("</script>");
        }

        internal static void Dump(object o, Type oType, HtmlWriter writer, DumpState state, RefDictionary refDict)
        {
            if (state == null) { state = new DumpState(); }
            else
            {
                state = state.BumpLevel();
            }



            if (refDict == null) refDict = new RefDictionary();

            if (refDict.Add(o) == false)
            {
                writer.Tag("span", t =>
                {
                    t.Attribute("class", "goto");
                    t.Attribute("data-goto", refDict.GetID(o).ToString());
                    t.WriteString("<>");
                });
                return;
            }

            if (o == null)
            {
                writer.WriteString("null");
                return;
            }



            var dt = GetDumpType(oType);
            if (dt == null)
            {
                writer.WriteSpan(s => s.WriteString($"{oType.FullName} NOT HANDLED"));
                return;
            }


            if (dt.IgnoreDeep == false)
            {
                if (state.Level >= 4)
                {
                    writer.WriteString("TOO DEEP");
                    return;
                }
            }
            dt.Write(o, writer, state, refDict);
        }

        public static void Dump(object o, HtmlWriter writer)
        {
            var oType = o.GetType();
            Dump(o, oType, writer, null, null);
        }

        public static string DumpToHtml(object o, string title = "Dump")
        {
            HtmlWriter writer = new HtmlWriter(false, false);
            writer.HtmlOpen();
            writer.Head(h =>
            {
                h.Title(title);
                h.WriteRaw("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\" />");
                WriteStyle(h);
            });

            writer.BodyOpen();


            Dump(o, writer);

            WriteScript(writer);

            writer.BodyClose();
            writer.HtmlClose();

            var html = writer.Writer.ToString();

            return html;
        }

        static DumpType GetDumpType(Type t)
        {


            foreach (var dt in Types)
            {
                if (dt.ValidTypes.Contains(t))
                {
                    return dt;
                }

                if (dt.AcceptsType(t))
                {
                    return dt;
                }
            }

            return null;
        }
    }

    #region DumpType

    abstract class DumpType
    {
        public virtual int Priority { get { return 1; } }

        public virtual bool IgnoreDeep { get { return false; } }

        public virtual Type[] ValidTypes
        {
            get
            {
                return new Type[] { };
            }
        }

        public virtual bool AcceptsType(Type t)
        {
            return false;
        }

        public abstract void Write(object o, HtmlWriter tag, DumpState state, RefDictionary refDict);


        protected void SetID(RefDictionary dict, HtmlWriter wri, object o)
        {
            (wri as HtmlTag)?.Attribute("data-id", dict.GetID(o).ToString());
        }
    }

    #endregion

    #region SingleDumpType`1
    abstract class SingleDumpType<T> : DumpType
    {
        public sealed override int Priority { get { return 5; } }

        public sealed override Type[] ValidTypes
        {
            get { return new[] { typeof(T) }; }
        }

        public sealed override bool AcceptsType(Type t)
        {
            return base.AcceptsType(t);
        }

        public sealed override void Write(object o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            Write((T)o, tag, state, refDict);
        }

        protected abstract void Write(T o, HtmlWriter tag, DumpState state, RefDictionary refDict);

    }
    #endregion

    #region string
    sealed class StringDump : SingleDumpType<string>
    {
        protected override void Write(string o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            if (o.Contains(Environment.NewLine))
            {
                tag.Tag("pre", p =>
                {
                    p.Attribute("class", "str tg");
                    p.NewLineAfterSTag = false;
                    p.WriteString(o);
                });
            }
            else
            {
                tag.WriteSpan(s =>
                {
                    s.NewLineAfterSTag = false;
                    s.WriteString(o);
                });
            }
        }

        public override bool IgnoreDeep
        {
            get
            {
                return true;
            }
        }
    }

    #endregion

    #region ToStringDump
    sealed class ToStringDump : DumpType
    {
        static Type[] Types = new[] {   typeof(byte),typeof(char),typeof(decimal),typeof(double),typeof(float),typeof(int),typeof(long),
                                        typeof(sbyte),typeof(short),typeof(string),typeof(uint),typeof(ulong)};
        public override bool IgnoreDeep { get { return true; } }

        public override Type[] ValidTypes { get { return Types; } }

        public override void Write(object o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            tag.WriteSpan(span =>
            {
                span.Attribute("title", o.GetType().FullName);
                span.WriteString(o.ToString());
            });
        }
    }

    #endregion

    #region bool
    sealed class BoolDump : SingleDumpType<bool>
    {
        protected override void Write(bool o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            var str = (bool)o ? "true" : "false";
            tag.WriteSpan(s =>
            {
                s.NewLineAfterSTag = false;
                s.Attribute("class", "b");
                //s.Attribute("style", "color:blue;");
                s.WriteString(str);
            });
        }

        public override bool IgnoreDeep { get { return true; } }
    }
    #endregion

    #region IEnumerable (ListDump)
    sealed class ListDump : DumpType
    {
        public override int Priority
        {
            get
            {
                return 2;
            }
        }

        public override bool AcceptsType(Type t)
        {
            if (t.GetInterfaces().Select(x => x.IsGenericType ? x.GetGenericTypeDefinition() : x).Contains(typeof(System.Collections.IEnumerable)))
            {
                return true;
            }

            return false;
        }

        public override void Write(object o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            state = state.AsList();

            var e = (o as IEnumerable).GetEnumerator();

            tag.Table(ul =>
            {
                int idx = 0;
                while (e.MoveNext())
                {
                    ul.Row(r =>
                    {
                        r.Tag("th", th => th.WriteString(idx.ToString()));
                        r.Tag("td", td =>
                        {
                            DumpManager.Dump(e.Current, e.Current.GetType(), td, state, refDict);

                        });
                    });
                    idx++;
                    if (idx > 100)
                    {
                        ul.Row(r =>
                        {
                            r.Tag("td", td =>
                            {
                                td.Attribute("colspan", "2");
                                td.WriteString("...");
                            });
                        });
                        return;
                    }
                }
            });


        }
    }
    #endregion

    #region Default (ObjectDump)
    sealed class ObjectDump : DumpType
    {
        public override bool AcceptsType(Type t)
        {
            return true;
        }

        public override void Write(object o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            //if (tag is HtmlTag && o != null)
            //{
            //    (tag as HtmlTag).Attribute("data-id", refDict.GetID(o).ToString());
            //}

            var type = o.GetType();

            WriteSingleObject(o, tag, type, state, refDict);

        }

        private void WriteSingleObject(object o, HtmlWriter tag, Type type, DumpState state, RefDictionary refDict)
        {
            tag.Table(table =>
            {
                SetID(refDict, table, o);
                table.Row(r =>
                {
                    r.Tag("th", th =>
                    {
                        th.Attribute("colspan", "2");
                        th.Attribute("class", "o");
                        th.Tag("span", s => s.WriteString(type.Namespace));
                        th.Tag("span", s =>
                        {
                            s.NewLineAfterSTag = false;
                            s.Attribute("class", "type");
                            s.WriteString(type.Name);
                        });
                        //    th.WriteString(type.FullName);
                    });
                });
                table.Row(r =>
                {
                    r.Tag("td", th =>
                    {
                        th.Attribute("colspan", "2");
                        th.WriteString(o.ToString());
                    });
                });

                foreach (var p in type.GetProperties())
                {
                    NewMethod(o, state, refDict, table, p);
                }

                foreach (var p in type.GetFields())
                {
                    NewMethod2(o, state, refDict, table, p);
                }
            });
        }

        private static void NewMethod2(object o, DumpState state, RefDictionary refDict, HtmlTable table, FieldInfo p)
        {
            table.Row(r =>
            {
                r.Tag("th", th =>
                {
                    th.NewLineAfterSTag = false;
                    th.Attribute("title", p.FieldType.FullName.ToString());
                    th.WriteString(p.Name);
                });

                r.Tag("td", td =>
                {
                    object val = null;
                    try
                    {
                        val = p.GetValue(o);
                    }
                    catch (Exception ex)
                    {
                        td.Attribute("style", "background:pink");
                        td.Tag("pre", pr =>
                        {
                            pr.NewLineAfterSTag = false;
                            pr.Attribute("class", "tg");
                            pr.WriteString(ex.ToString());
                        });
                        return;
                    }

                    DumpManager.Dump(val, p.FieldType, td, state, refDict);


                });
            });
        }

        private static void NewMethod(object o, DumpState state, RefDictionary refDict, HtmlTable table, PropertyInfo p)
        {
            table.Row(r =>
            {
                r.Tag("th", th =>
                {
                    th.NewLineAfterSTag = false;
                    th.Attribute("title", p.PropertyType.FullName.ToString());
                    th.WriteString(p.Name);
                });

                r.Tag("td", td =>
                {
                    object val = null;
                    try
                    {
                        val = p.GetValue(o);
                    }
                    catch (Exception ex)
                    {
                        td.Attribute("style", "background:pink");
                        td.Tag("pre", pr =>
                        {
                            pr.NewLineAfterSTag = false;
                            pr.Attribute("class", "tg");
                            pr.WriteString(ex.ToString());
                        });
                        return;
                    }

                    DumpManager.Dump(val, p.PropertyType, td, state, refDict);


                });
            });
        }
    }
    #endregion

    #endregion

    #region MiscUtil
    public static class DumpUtilites
    {
        /// <summary>
        /// Compares to strings char by char. Useful for linqpad
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string StringCompareHtml(string a, string b)
        {
            if (a == null && b == null) return "";
            var max = new[] { a, b }.Where(x => x != null).Max(x => x.Length);
            bool bIsNull = false;
            if (a == null) a = new string('\0', max);
            if (b == null)
            {
                bIsNull = true;
                b = new string('\0', max);
            }

            a = GetFullLengthString(a, max);

            HtmlWriter hw = new HtmlWriter();

            hw.Table(t =>
            {
                t.Row(r =>
                {
                    for (int i = 0; i < max; i++)
                        r.Tag("th", h => h.WriteRaw(i.ToString()));
                });

                t.Row(r =>
                {
                    foreach (var c in a)
                    {
                        r.Tag("td", td =>
                        {
                            StringCompareHtml_td(td, c);
                        });
                    }
                });

                if (bIsNull) return;
                t.Row(r =>
                {
                    var idx = 0;
                    foreach (var c in b)
                    {
                        r.Tag("td", td =>
                        {
                            td.NewLineAfterSTag = false;
                            try
                            {
                                if (a[idx] != c)
                                {
                                    td.Attribute("style", "background:pink;");
                                }
                            }
                            catch
                            {

                            }
                            StringCompareHtml_td(td, c);

                        });

                        idx++;

                    }

                });


            });

            return hw.Writer.ToString();
        }

        private static void StringCompareHtml_td(HtmlTag td, char c)
        {
            if (c == (char)0)
            {
                td.Tag("span", span =>
                {
                    span.Attribute("style", "color:gray");
                    span.WriteString("\\0");

                });
            }
            else
            {
                td.WriteString(c.ToString());
            }
        }

        /// <summary>
        /// pads '\0' at the end
        /// </summary>
        /// <param name="a"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        private static string GetFullLengthString(string a, int max)
        {
            if (a.Length != max)
            {
                var chars = new char[max];
                for (int i = 0; i < chars.Length; i++)
                {
                    if (a.Length > i) chars[i] = a[i];
                    else chars[i] = (char)0;
                }

                a = new string(chars);
            }

            return a;
        }
    }
    #endregion
}
