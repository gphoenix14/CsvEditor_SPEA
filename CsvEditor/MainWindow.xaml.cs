// MainWindow.xaml.cs
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Data;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Linq;

namespace CsvEditor
{
    public partial class MainWindow : Window
    {
        private DataTable dataTable = new DataTable();
        private BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            SetupBackgroundWorker();
        }

        private void SetupBackgroundWorker()
        {
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private async void LoadCsv_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Title = "Seleziona un file CSV" };
            if (openFileDialog.ShowDialog() == true)
            {
                progressBar.Visibility = Visibility.Visible;
                progressBar.Value = 0;
                dataGrid.Visibility = Visibility.Collapsed;
                btnExport.IsEnabled = false;
                try
                {
                    await Task.Run(() => worker.RunWorkerAsync(openFileDialog.FileName));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante il caricamento: {ex.Message}");
                    ResetUI();
                }
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string filePath = (string)e.Argument;
            List<string[]> rows = new List<string[]>();
            int totalLines = File.ReadAllLines(filePath).Length;
            int currentLine = 0;
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                string[] headers = reader.ReadLine()?.Split(',');
                if (headers == null) return;
                rows.Add(headers);
                currentLine++;
                while ((line = reader.ReadLine()) != null)
                {
                    rows.Add(line.Split(','));
                    currentLine++;
                    int progress = (int)((double)currentLine / totalLines * 100);
                    worker.ReportProgress(progress);
                }
            }
            e.Result = rows;
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { progressBar.Value = e.ProgressPercentage; }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                progressBar.Visibility = Visibility.Collapsed;
                progressBar.Value = 0;
                dataTable.Clear();
                dataTable.Columns.Clear();
                if (e.Error != null)
                {
                    MessageBox.Show($"Errore: {e.Error.Message}");
                    ResetUI();
                    return;
                }
                List<string[]> rows = e.Result as List<string[]>;
                if (rows == null || rows.Count == 0)
                {
                    MessageBox.Show("Nessun dato.");
                    ResetUI();
                    return;
                }
                string[] headers = rows[0];
                foreach (string header in headers) dataTable.Columns.Add(header);
                for (int i = 1; i < rows.Count; i++) dataTable.Rows.Add(rows[i]);
                dataGrid.ItemsSource = dataTable.DefaultView;
                dataGrid.Visibility = Visibility.Visible;
                btnExport.IsEnabled = true;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", Title = "Salva CSV" };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName, false, Encoding.UTF8))
                    {
                        IEnumerable<string> columnNames = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                        writer.WriteLine(string.Join(",", columnNames.Select(Escape)));
                        foreach (DataRow row in dataTable.Rows) writer.WriteLine(string.Join(",", row.ItemArray.Select(field => Escape(field.ToString()))));
                    }
                    MessageBox.Show("Esportazione completata!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Errore durante l'esportazione: {ex.Message}");
                }
            }
        }

        private void ResetUI()
        {
            progressBar.Visibility = Visibility.Collapsed;
            progressBar.Value = 0;
            dataGrid.Visibility = Visibility.Collapsed;
            btnExport.IsEnabled = false;
        }

        private static string Escape(string s)
        {
            if (s.Contains("\"") || s.Contains(",") || s.Contains("\n") || s.Contains("\r"))
            {
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }
            return s;
        }
    }
}
