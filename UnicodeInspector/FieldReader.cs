using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicodeInspector
{
    public class FieldReader : IDisposable
    {
        private readonly StreamReader Reader;

        private readonly char[] Separators;

        public FieldReader(Stream stream, params char[] separators)
        {
            Reader = new StreamReader(stream, Encoding.UTF8);
            Separators = separators.Clone() as char[];
        }

        public string[] Read()
        {
            for (; ; )
            {
                var line = Reader.ReadLine();
                if (line == null) return null;

                int pound;
                if ((pound = line.IndexOf('#')) >= 0) line = line.Substring(0, pound);

                if (!string.IsNullOrWhiteSpace(line))
                {
                    return line.Split(Separators);
                }
            }
        }

        public void Dispose()
        {
            Reader.Dispose();
        }
    }
}
