using System.IO;
using System.Reflection;
using System.Collections.Generic;

static class Res 
{
    #region Private Static Methods
    static object _lock = new object();
    static Dictionary<string,string> _dict = new Dictionary<string,string>();
    private static string GetResource(string name)
    {
        lock(_lock)
        {
            if(!_dict.ContainsKey(name))
            {            
                var assembly = Assembly.GetExecutingAssembly();

                using (Stream stream = assembly.GetManifestResourceStream(name))
                {
                    if(stream == null) throw new FileNotFoundException("Resource not found");
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        _dict[name] = reader.ReadToEnd();
                    }		
                }
            
            }

            return _dict[name];
        }
    }
    #endregion

    // DumpUtil.Script.js
    public static string Script
    {
        get
        {
            return GetResource("DumpUtil.Script.js");
        }
    }

    // DumpUtil.Style.css
    public static string Style
    {
        get
        {
            return GetResource("DumpUtil.Style.css");
        }
    }

}