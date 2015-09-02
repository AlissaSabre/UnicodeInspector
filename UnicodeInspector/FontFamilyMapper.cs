using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Markup;

namespace UnicodeInspector
{
    /// <summary>
    /// Provides methods to find an underlying FontFamily to use for a particular unicode codepoint.
    /// </summary>
    /// <remarks>
    /// WPF provides a mechanism to create a composite font.
    /// The composition is described by <see cref="FontFamily.FontFamilyMap"/> property.
    /// It is easy to specify, e.g., on XAML, but not easy to interpret.
    /// This class provides methods, <see cref="FontFamilyMapper.Map(int)"/> and 
    /// <see cref="FontFamilyMapper.Map(ICollection<FontFamilyMap>,int,CultureInfo)"/>
    /// to do the interpretation.
    /// </remarks>
    public class FontFamilyMapper
    {
        public class CultureFacePair
        {
            public IEnumerable<CultureInfo> Cultures;
            public string Face;
        }

        private static readonly char[] COMMA = new char[] { ',' };

        private static readonly string[] EMPTY_STRING_ARRAY = new string[0];

        private readonly ICollection<FontFamilyMap> Mapper;

        public FontFamilyMapper(ICollection<FontFamilyMap> map)
        {
            Mapper = map.ToArray();
        }

        public string GetFace(int code, CultureInfo culture = null)
        {
            if (culture == null) culture = CultureInfo.CurrentCulture;

            return Mapper
                .Where(m => Compatible(m.Language, culture) && Includes(code, m.Unicode))
                .SelectMany(m => m.Target.Split(COMMA).Select(f => f.Trim()).Where(f => HasGlyph(f, code)))
                .FirstOrDefault();
        }

        private static CultureInfo[] Differentiators;

        public ICollection<CultureFacePair> GetCultureFacePairs(int code)
        {
            if (Differentiators == null)
            {
                var d = Mapper.Select(m => m.Language).Where(new HashSet<XmlLanguage>(new XmlLanguage[] { null }).Add).ToList();
                Differentiators = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Where(c => d.Any(x => Compatible(x, c))).ToArray();
            }

            var default_face = GetFace(code, CultureInfo.InvariantCulture);
            //if (default_face == null) return new CultureFacePair[0];

            var list = Differentiators
                .GroupBy(culture => GetFace(code, culture))
                .Where(g => g.Key != default_face)
                .Select(g => new CultureFacePair() { Face = g.Key, Cultures = g }).ToList();
            list.Add(new CultureFacePair() { Face = default_face, Cultures = null });
            
            return list;
        }

        private static bool Compatible(XmlLanguage lang, CultureInfo culture)
        {
            if (lang == null || lang == XmlLanguage.Empty) return true;
            var ll = lang.IetfLanguageTag.ToLowerInvariant();
            while (!culture.Equals(CultureInfo.InvariantCulture))
            {
                if (ll == culture.IetfLanguageTag.ToLowerInvariant()) return true;
                culture = culture.Parent;
            }
            return false;
        }

        private static bool Includes(int c, string unicode)
        {
            foreach (var r in unicode.Split(COMMA).Select(s => s.Trim()))
            {
                var i = r.IndexOf('-');
                if (i < 0) throw new ArgumentException("Invalid format", "unicode");
                var lo = Int32.Parse(r.Substring(0, i), NumberStyles.HexNumber);
                var hi = Int32.Parse(r.Substring(i + 1), NumberStyles.HexNumber);
                if (c >= lo && c <= hi) return true;
            }
            return false;
        }

        private static bool HasGlyph(string face, int code)
        {
            GlyphTypeface g;
            return new Typeface(face).TryGetGlyphTypeface(out g) && g.CharacterToGlyphMap.ContainsKey(code);
        }
    }
}
