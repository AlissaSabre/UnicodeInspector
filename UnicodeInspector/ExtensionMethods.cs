using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnicodeInspector
{
    public static class ExtensionMethods
    {
        private class WrappedEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> Source;

            public WrappedEnumerable(IEnumerable<T> source)
            {
                Source = source;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return Source.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return ((System.Collections.IEnumerable)Source).GetEnumerator();
            }
        }

        public static IEnumerable<T> ToEnumerable<T>(this IEnumerable<T> source)
        {
            return new WrappedEnumerable<T>(source);
        }

    }
}
