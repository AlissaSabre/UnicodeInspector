using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UnicodeInspector
{
    public enum UpdateMode
    {
        Text,
        Code,
        Name,
    }

    public class UpdateData
    {
        public string Text;
        public UpdateMode Mode;
    }

    public class SummaryUpdatedEventArgs : EventArgs
    {
        public Unicode.Summary[] Info { get; set; }
    }

    public delegate void SummaryUpdatedEvent(object sender, SummaryUpdatedEventArgs e);

    public class SummaryUpdater : IncrementalUpdater<UpdateData>
    {
        public event SummaryUpdatedEvent Updated;

        public void Update(string text, UpdateMode mode)
        {
            Update(new UpdateData() { Text = text, Mode = mode });
        }

        protected override void DoWork(UpdateData input)
        {
            switch (input.Mode)
            {
                case UpdateMode.Text:
                    DoText(input.Text);
                    break;
                case UpdateMode.Code:
                    DoCode(input.Text);
                    break;
                case UpdateMode.Name:
                    DoName(input.Text);
                    break;
            }
        }

        private void DoText(string text)
        {
            var list = new List<Unicode.Summary>(text.Length);
            bool after_high_surrogate = false;

            foreach (var c in text)
            {
                if (IsCancelled()) return;
                int code = c;

                if (after_high_surrogate && Unicode.IsLowSurrogate(code))
                {
                    var high = list[list.Count - 1];
                    code = Unicode.DecodeSurrogatePair((int)high.Code, code);
                    list[list.Count - 1] = Unicode.GetSummary(code);
                }
                else
                {
                    list.Add(Unicode.GetSummary(c));
                }

                after_high_surrogate = Unicode.IsHighSurroate(code);
            }

            OnUpdated(list.ToArray());
        }

        private class UnicodeSummaryEx : Unicode.Summary
        {
            public string Orig { get { return _Orig; } }

            private string _Orig;

            public UnicodeSummaryEx(Unicode.Summary summary, string orig)
                : base(summary.Code, summary.Text, summary.Name)
            {
                _Orig = orig;
            }
        }

        private void DoCode(string text)
        {
            var list = new List<Unicode.Summary>(text.Length);
            bool after_high_surrogate = false;

            var a = text
                .Replace(";", "; ").Replace("\\", " \\").Replace("&#", " &#")
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in a)
            {
                if (IsCancelled()) return;
                int code = ParseCodepointNotation(s);

                if (after_high_surrogate && Unicode.IsLowSurrogate(code) && !s.StartsWith("&#"))
                {
                    var high = list[list.Count - 1] as UnicodeSummaryEx;
                    code = Unicode.DecodeSurrogatePair((int)high.Code, code);
                    list[list.Count - 1] = new UnicodeSummaryEx(Unicode.GetSummary(code), high.Orig + " " + s);
                }
                else
                {
                    list.Add(new UnicodeSummaryEx(Unicode.GetSummary(code), s));
                }

                after_high_surrogate = Unicode.IsHighSurroate(code) && !s.StartsWith("&#");
            }
            OnUpdated(list.ToArray());
        }

        private static int ParseCodepointNotation(string text)
        {
            int code;

            if (text.StartsWith("\\u") || text.StartsWith("\\U") ||
                text.StartsWith("\\x") || text.StartsWith("\\X") || 
                text.StartsWith("U+") || text.StartsWith("u+") ||
                text.StartsWith("U-") || text.StartsWith("u-") ||
                text.StartsWith("0x") || text.StartsWith("0X") ||
                text.StartsWith("&H") || text.StartsWith("&h"))
            {
                return Int32.TryParse(text.Substring(2), NumberStyles.HexNumber, null, out code) ? code : -1;
            }

            if (text.StartsWith("u") || text.StartsWith("U"))
            {
                return Int32.TryParse(text.Substring(1), NumberStyles.HexNumber, null, out code) ? code : -1;
            }

            if (text.EndsWith(";") && (text.StartsWith("&#x") || text.StartsWith("&#X")))
            {
                return Int32.TryParse(text.Substring(3, text.Length - 4), NumberStyles.HexNumber, null, out code) ? code : -1;
            }

            if (text.EndsWith(";") && text.StartsWith("&#"))
            {
                return Int32.TryParse(text.Substring(2, text.Length - 3), out code) ? code : -1;
            }

            return Int32.TryParse(text, NumberStyles.HexNumber, null, out code) ? code : -1;
        }

        private void DoName(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                OnUpdated(new Unicode.Summary[0]);
                return;
            }

            var max = Properties.Settings.Default.MaxNameHists;
            var u = new List<Unicode.Summary>();
            var r = string.Join(".* .*", text.Split().Select(s => Regex.Escape(s)));
            var re = new Regex(r, RegexOptions.IgnoreCase);
            foreach (var name in Unicode.AllNames.Where(n => re.IsMatch(n)))
            {
                u.Add(new Unicode.Summary(null, "", name));
                //if (u.Count >= max) break;
            }
            OnUpdated(u.ToArray());
        }

        protected void OnUpdated(Unicode.Summary[] info)
        {
            var handler = Updated;
            if (handler != null) handler(this, new SummaryUpdatedEventArgs() { Info = info });
        }
    }
}
