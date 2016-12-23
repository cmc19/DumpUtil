namespace DumpUtil.Types
{

    using Html;

    sealed class BoolDump : SingleDumpType<bool>
    {
        protected override void Write(bool o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            var str = (bool)o ? "true" : "false";
            tag.WriteSpan(s =>
            {
                s.NewLineAfterSTag = false;
                s.Attribute("class", "b");
                s.WriteString(str);
            });
        }

        public override bool IgnoreDeep { get { return true; } }
    }

    sealed class NullBoolDump : SingleDumpType<bool?>
    {
        protected override void Write(bool? o, HtmlWriter tag, DumpState state, RefDictionary refDict)
        {
            var str = o.HasValue ? (o.Value ? "true" : "false") : "null";
            tag.WriteSpan(s =>
            {
                s.NewLineAfterSTag = false;
                s.Attribute("class", "b");
                s.WriteString(str);
            });
        }

        public override bool IgnoreDeep { get { return true; } }
    }
}