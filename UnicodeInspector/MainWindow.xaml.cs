using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UnicodeInspector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            SummaryUpdater = new SummaryUpdater();
            SummaryUpdater.Updated += SummaryUpdater_Updated;

            DdetailsUpdater = new DetailsUpdater();
            DdetailsUpdater.GlyphUpdated += DetailsUpdater_GlyphUpdated;
            DdetailsUpdater.DetailsUpdated += DetailsUpdater_DetailsUpdated;

            InitializeComponent();
        }

        private readonly SummaryUpdater SummaryUpdater;

        private readonly DetailsUpdater DdetailsUpdater;

        private static readonly DataGridLength STAR = new DataGridLength(1, DataGridLengthUnitType.Star);

        //private static readonly object[] DUMMY_CONTENTS = new object[0];

        private static readonly UpdateMode[] UpdateModeMapper = 
        {
            UpdateMode.Text,
            UpdateMode.Code,
            UpdateMode.Name,
        };

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SummaryUpdater.Start();
            DdetailsUpdater.Start();
            Source.Focus();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            SummaryUpdater.Dispose();
            DdetailsUpdater.Dispose();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Source.Text = "";
        }

        private void Source_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSummary();
        }

        private void InspectMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSummary();
        }

        private void UpdateSummary()
        {
            if (Source != null && InspectMode != null)
            {
                SummaryUpdater.Update(Source.Text, UpdateModeMapper[InspectMode.SelectedIndex]);
            }
        }

        private void SummaryUpdater_Updated(object sender, SummaryUpdatedEventArgs e)
        {
            var items = e.Info;
            Dispatcher.Invoke(delegate
            {
                UpdateSummaries(items);
            });
        }

        private void UpdateSummaries(object[] items)
        {
            var s = Summaries;
            var n = s.SelectedIndex;

            // Oh what a mess!  http://stackoverflow.com/questions/5549099/datagrid-column-width-doesnt-auto-update
            //s.ItemsSource = DUMMY_CONTENTS;

            var columns = s.Columns;
            var count = columns.Count;
            for (int i = 0; i < count; i++) columns[i].Width = 0;
            s.UpdateLayout();
            for (int i = 0; i < count - 1; i++) columns[i].Width = DataGridLength.Auto;
            s.Columns[Summaries.Columns.Count - 1].Width = STAR;

            if (items.Length == 0)
            {
                s.ItemsSource = items;
            }
            else
            {
                SuppressUpdateGlyphs = true;
                s.ItemsSource = items;
                SuppressUpdateGlyphs = false;

                if (n < 0)
                {
                    s.SelectedIndex = 0;
                }
                else if (n < items.Length)
                {
                    s.SelectedIndex = n;
                }
                else
                {
                    s.SelectedIndex = items.Length - 1;
                }
            }
        }

        private bool SuppressUpdateGlyphs;

        private int? CurrentGlyph;

        private void Summaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SuppressUpdateGlyphs) return;

            var info = Summaries.SelectedItem as Unicode.Summary;
            if (info == null || !info.Code.HasValue)
            {
                CurrentGlyph = null;
                Identity.Clear();
                GlyphsInfo.Clear();
                GlyphArea.Text = GlyphText = "";
                FontList.SelectedIndex = -1;
                DetailedInfo.Clear();
            }
            else
            {
                CurrentGlyph = info.Code;
                Identity.Text = string.Format("{0:X04} {1}", (int)info.Code, info.Name);
                DdetailsUpdater.Update(info);
            }
        }

        private void DetailsUpdater_GlyphUpdated(object sender, GlyphUpdatedEventArgs e)
        {
            Dispatcher.Invoke(delegate
            {
                GlyphsInfo.Clear();

                GlyphText = e.Text;
                //GlyphArea.Text = e.Text;

                if (e.DefaultFaces.Length == 1)
                {
                    GlyphsInfo.AppendText(string.Format("Fallback Font: {0}\r\n", e.DefaultFaces[0].Face ?? "(none)"));
                }
                else
                {
                    GlyphsInfo.AppendText("Fallback Font:\r\n");
                    foreach (var p in e.DefaultFaces)
                    {
                        var cultures = p.Cultures == null ? "all other" : string.Join(", ", p.Cultures.Select(c => c.IetfLanguageTag));
                        GlyphsInfo.AppendText(string.Format("   {0} ({1})\r\n", p.Face ?? "(none)", cultures));
                    }
                }
                GlyphsInfo.AppendText(string.Format("Available in: {0}", 
                    e.CompatibleFaces.Length > 0 ? string.Join(", ", e.CompatibleFaces) : "(none)"));

                FontList.SelectedIndex = -1;
                FontList.ItemsSource = e.CompatibleFaces;
                if (e.DefaultFaces != null)
                {
                    FontList.SelectedItem = e.DefaultFaces.First(p => p.Cultures == null || p.Cultures.Contains(System.Globalization.CultureInfo.CurrentCulture)).Face;
                }
            });
        }

        private string GlyphText;

        private void FontList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var f = FontList.SelectedItem as string;
            if (f == null)
            {
                GlyphArea.Text = "";
            }
            else
            {
                GlyphArea.Text = GlyphText;
                GlyphArea.FontFamily = new FontFamily(f);
            }
        }

        private void GlyphArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var t = sender as TextBox;
            t.FontSize = Math.Min(e.NewSize.Width, e.NewSize.Height) / 2;
        }

        private void DetailsUpdater_DetailsUpdated(object sender, DetailsUpdatedEventArgs e)
        {
            Dispatcher.Invoke(delegate
            {
                DetailedInfo.Clear();

                foreach (var p in e.Details)
                {
                    var label = Localize.ResourceManager.GetString(p.Key.ToString()) ?? p.Key.ToString();
                    DetailedInfo.AppendText(string.Format("{0}: {1}\r\n", label, string.Join(", ", p.Value)));
                }
            });
        }
    }
}
