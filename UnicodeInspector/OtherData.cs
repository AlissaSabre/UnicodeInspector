using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicodeInspector
{
    public static class OtherData
    {
        public static void Initialize()
        {
        }

        private static MultiDict<int, string> Classes;

        static OtherData()
        {
            var settings = Properties.Settings.Default;

            var classes = new MultiDict<int, string>();
            var reuser = new ObjectReuser<string>();

            var path = Path.Combine(Path.GetDirectoryName(typeof(OtherData).Assembly.Location), settings.UnicodeZipFilename);
            using (var zip = ZipFile.OpenRead(path))
            {
                using (var rd = new FieldReader(zip.GetEntry(settings.OtherAttribution).Open(), ';'))
                {
                    string[] fields;
                    while ((fields = rd.Read()) != null)
                    {
                        classes.Add(int.Parse(fields[0], System.Globalization.NumberStyles.HexNumber), reuser.Get(fields[1].Trim()));
                    }
                }
            }

            Classes = classes;
        }

        public static Unicode.IData GetData(int code)
        {
            return new Unicode.Data(Unicode.DataKey.OtherProperties, Classes.Get(code));
        }
    }
}
