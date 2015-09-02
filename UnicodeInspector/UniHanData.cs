using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicodeInspector
{
    public static class UniHanData
    {
        public static void Initialize()
        {
        }

        private static MultiDict<int, string> Sources;

        private static MultiDict<int, string> Mappings;

        private static MultiDict<int, int> Variants;

        static UniHanData()
        {
            var settings = Properties.Settings.Default;

            Sources = new MultiDict<int, string>();
            Mappings = new MultiDict<int, string>();
            Variants = new MultiDict<int, int>();

            var path = Path.Combine(Path.GetDirectoryName(typeof(UniHanData).Assembly.Location), settings.UnicodeZipFilename);
            using (var zip = ZipFile.OpenRead(path))
            {
                using (var rd = new FieldReader(zip.GetEntry(settings.Unihan_IRGSources).Open(), '\t'))
                {
                    string[] fields;
                    while ((fields = rd.Read()) != null)
                    {
                        if (fields[1].StartsWith("kIRG_") && fields[1].EndsWith("Source"))
                        {
                            var code = int.Parse(fields[0].Substring(2), NumberStyles.HexNumber);
                            Sources.Add(code, fields[2]);
                        }
                        else if (fields[1].EndsWith("Variant"))
                        {
                            var code = int.Parse(fields[0].Substring(2), NumberStyles.HexNumber);
                            var vant = int.Parse(fields[2].Substring(2), NumberStyles.HexNumber);
                            Variants.Add(code, vant);
                        }
                    }
                }

                using (var rd = new FieldReader(zip.GetEntry(settings.Unihon_OtherMappings).Open(), '\t'))
                {
                    string[] fields;
                    while ((fields = rd.Read()) != null)
                    {
                        var code = int.Parse(fields[0].Substring(2), NumberStyles.HexNumber);
                        var mapping = fields[1].Substring(1) + "-" + fields[2].Replace(',', '-').Replace(':','-');
                        Mappings.Add(code, mapping);
                    }
                }

                using (var rd = new FieldReader(zip.GetEntry(settings.Unihan_Variants).Open(), '\t'))
                {
                    string[] fields;
                    while ((fields = rd.Read()) != null)
                    {
                        var code = int.Parse(fields[0].Substring(2), NumberStyles.HexNumber);
                        foreach (var v in fields[2].Split('\t', ' ', '<').Where(s => s.StartsWith("U+")))
                        {
                            var vant = int.Parse(v.Substring(2), NumberStyles.HexNumber);
                            Variants.Add(code, vant);
                        }
                    }
                }
            }
        }

        public static bool IsUniHan(int code)
        {
            return Sources.ContainsKey(code);
        }

        public static IEnumerable<string> GetSources(int code)
        {
            return Sources.Get(code);
        }

        public static IEnumerable<string> GetMappings(int code)
        {
            return Mappings.Get(code);
        }

        public static IEnumerable<int> GetVariants(int code)
        {
            return Variants.Get(code);
        }
    }
}
