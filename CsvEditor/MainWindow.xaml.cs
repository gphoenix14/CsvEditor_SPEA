using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Threading;
namespace CsvEditor
{
    public partial class MainWindow : Window
    {
        DataTable dataTable = new();
        List<long> chunkOffsets = new();
        string filePath;
        int totalLines;
        int chunkSize = 1000;
        int currentChunk = -1;
        public MainWindow()
        {
            InitializeComponent();
            txtChunk.Text = $"Chunk: {chunkSize}";
        }
        async void LoadCsv_Click(object s, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new() { Filter = "CSV (*.csv)|*.csv|Tutti i file (*.*)|*.*", Title = "Seleziona CSV" };
            if (dlg.ShowDialog() != true) return;
            ResetUI();
            filePath = dlg.FileName;
            chunkSize = (int)sliderChunk.Value;
            txtChunk.Text = $"Chunk: {chunkSize}";
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;
            await Task.Run(BuildIndex);
            progressBar.IsIndeterminate = false;
            progressBar.Visibility = Visibility.Collapsed;
            btnExport.IsEnabled = true;
            sliderChunk.IsEnabled = false;
            await LoadChunkAsync(0);
        }
        void BuildIndex()
        {
            chunkOffsets.Clear();
            using FileStream fs = File.OpenRead(filePath);
            using StreamReader sr = new(fs, Encoding.UTF8);
            string header = sr.ReadLine();
            if (header == null) return;
            Dispatcher.Invoke(() => {
                dataTable.Clear();
                dataTable.Columns.Clear();
                foreach (string h in ParseCsvLine(header)) dataTable.Columns.Add(h);
                dataGrid.Columns.Clear();
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = dataTable.DefaultView;
            });
            long start = fs.Position;
            chunkOffsets.Add(start);
            int lc = 0;
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                lc++;
                if (lc % chunkSize == 0) chunkOffsets.Add(fs.Position);
            }
            totalLines = lc + 1;
        }
        async Task LoadChunkAsync(int idx)
        {
            if (idx < 0 || idx >= chunkOffsets.Count) return;
            if (idx == currentChunk) return;
            currentChunk = idx;
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;
            List<string[]> rows = new();
            await Task.Run(() => {
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.Seek(chunkOffsets[idx], SeekOrigin.Begin);
                using StreamReader sr = new(fs, Encoding.UTF8, false, 1024, true);
                for (int i = 0; i < chunkSize; i++)
                {
                    string l = sr.ReadLine();
                    if (l == null) break;
                    rows.Add(ParseCsvLine(l));
                }
            });
            await Dispatcher.InvokeAsync(() => {
                dataTable.Clear();
                foreach (string[] r in rows) dataTable.Rows.Add(r);
                progressBar.Visibility = Visibility.Collapsed;
            }, DispatcherPriority.Background);
        }
        void DataGrid_Loaded(object s, RoutedEventArgs e)
        {
            ScrollViewer sv = GetSV(dataGrid);
            if (sv != null) sv.ScrollChanged += ScrollChanged;
        }
        void ScrollChanged(object s, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = s as ScrollViewer;
            if (sv == null || sv.ScrollableHeight == 0) return;
            double pos = sv.VerticalOffset / sv.ScrollableHeight;
            int approxRow = (int)(pos * (totalLines - 1));
            int targetChunk = approxRow / chunkSize;
            if (targetChunk >= chunkOffsets.Count) targetChunk = chunkOffsets.Count - 1;
            _ = LoadChunkAsync(targetChunk);
        }
        void SliderChunk_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e)
        {
            chunkSize = (int)e.NewValue;
            if (txtChunk == null) return;
            if (!sliderChunk.IsEnabled) return;
            txtChunk.Text = $"Chunk: {chunkSize}";
        }
        ScrollViewer GetSV(DependencyObject d)
        {
            if (d is ScrollViewer) return (ScrollViewer)d;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                ScrollViewer sv = GetSV(VisualTreeHelper.GetChild(d, i));
                if (sv != null) return sv;
            }
            return null;
        }
        void ExportCsv_Click(object s, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new() { Filter = "CSV (*.csv)|*.csv|Tutti i file (*.*)|*.*", Title = "Salva CSV" };
            if (dlg.ShowDialog() != true) return;
            try
            {
                using StreamWriter w = new(dlg.FileName, false, Encoding.UTF8);
                IEnumerable<string> cols = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                w.WriteLine(string.Join(",", cols.Select(Esc)));
                foreach (DataRow r in dataTable.Rows) w.WriteLine(string.Join(",", r.ItemArray.Select(x => Esc(x.ToString()))));
                MessageBox.Show("Esportazione completata");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore: {ex.Message}");
            }
        }
        void ResetUI()
        {
            dataGrid.ItemsSource = null;
            dataTable.Clear();
            dataTable.Columns.Clear();
            progressBar.Visibility = Visibility.Collapsed;
            currentChunk = -1;
            sliderChunk.IsEnabled = true;
        }
        string[] ParseCsvLine(string line)
        {
            List<string> f = new();
            bool q = false;
            StringBuilder sb = new();
            foreach (char c in line)
            {
                if (c == '\"')
                {
                    q = !q;
                    continue;
                }
                if (c == ',' && !q)
                {
                    f.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }
                sb.Append(c);
            }
            f.Add(sb.ToString());
            return f.ToArray();
        }
        static string Esc(string s)
        {
            if (s.IndexOfAny(['\"', ',', '\n', '\r']) != -1)
            {
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }
            return s;
        }
    }
}
