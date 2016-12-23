namespace DumpUtil
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

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
}