using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace UnicodeInspector
{
    public class DetailsUpdatedEventArgs : EventArgs
    {
        public IDictionary<Unicode.DataKey, IEnumerable<string>> Details;
    }

    public delegate void DetailsUpdatedEvent(object sender, DetailsUpdatedEventArgs e);

    public class GlyphUpdatedEventArgs : EventArgs
    {
        public int? Code;
        public string Text;
        public FontFamilyMapper.CultureFacePair[] DefaultFaces;
        public string[] CompatibleFaces;
    }

    public delegate void GlyphUpdatedEvent(object sender, GlyphUpdatedEventArgs e);

    public class DetailsUpdater : IncrementalUpdater<Unicode.Summary>
    {
        public event DetailsUpdatedEvent DetailsUpdated;

        public event GlyphUpdatedEvent GlyphUpdated;

        private FontFamilyMapper DefaultFontMapper;

        private Typeface[] SystemTypefaces;

        protected override void WorkerLoop()
        {
            DefaultFontMapper = new FontFamilyMapper(new FontFamily("Global User Interface").FamilyMaps);
            SystemTypefaces = Fonts.SystemTypefaces.Where(f => IsUsable(f)).OrderBy(f => f.FontFamily.Source).ToArray();
            base.WorkerLoop();
        }

        protected override void DoWork(Unicode.Summary input)
        {
            DoGlyphs(input);
            DoDetails(input);
        }

        private void DoGlyphs(Unicode.Summary input)
        {
            if (input.Code == null)
            {
                OnGlyphUpdated(null, null, null, new string[0]);
            }
            else
            {
                var code = (int)input.Code;
                var defaults = DefaultFontMapper.GetCultureFacePairs(code).ToArray();
                if (IsCancelled()) return;
                var compatibles = new HashSet<string>(SystemTypefaces.Where(f => HasGlyph(f, code)).Select(f => f.FontFamily.Source)).ToArray();
                if (IsCancelled()) return;
                OnGlyphUpdated(input.Code, input.Text, defaults, compatibles);
            }
        }

        private void DoDetails(Unicode.Summary input)
        {
            if (IsCancelled()) return;
            var dict = new SortedDictionary<Unicode.DataKey, IEnumerable<string>>();
            int code = (int)input.Code;

            if (IsCancelled()) return;
            var unihan_source = UniHanData.GetSources(code);
            if (unihan_source != null) dict.Add(Unicode.DataKey.UniHanSources, unihan_source);

            if (IsCancelled()) return;
            var unihan_mapping = UniHanData.GetMappings(code);
            if (unihan_mapping != null) dict.Add(Unicode.DataKey.UniHanMappings, unihan_mapping);

            if (IsCancelled()) return;
            var unihan_variant = UniHanData.GetVariants(code);
            if (unihan_variant != null) dict.Add(Unicode.DataKey.UniHanVariants, unihan_variant.Select(u => "U+" + u.ToString("X04")));

            if (IsCancelled()) return;
            var other_info = OtherData.GetData(code);
            if (other_info != null) dict.Add(Unicode.DataKey.OtherProperties, other_info);

            if (IsCancelled()) return;
            var handler = DetailsUpdated;
            if (handler != null)
            {
                var e = new DetailsUpdatedEventArgs();
                e.Details = dict;
                handler(this, e);
            }
        }

        protected void OnGlyphUpdated(int? code, string text, FontFamilyMapper.CultureFacePair[] default_faces, string[] compatible_faces)
        {
            var handler = GlyphUpdated;
            if (handler != null)
            {
                handler(this, new GlyphUpdatedEventArgs() { Code = code, Text = text, DefaultFaces = default_faces, CompatibleFaces = compatible_faces });
            }
        }

        private static bool HasGlyph(Typeface typeface, int code)
        {
            GlyphTypeface g;
            return typeface.TryGetGlyphTypeface(out g) && g.CharacterToGlyphMap.ContainsKey(code);
        }

        private static bool IsUsable(Typeface typeface)
        {
            GlyphTypeface g;
            return typeface.TryGetGlyphTypeface(out g) && !g.Symbol;
        }

    }
}
