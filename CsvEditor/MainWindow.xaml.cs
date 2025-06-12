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

namespace CsvEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private DataTable dataTable = new DataTable();
        private BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            SetupBackgroundWorker();
        }

        private void SetupBackgroundWorker() {
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }
        private async void LoadCsv_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv) | *.csv |All files (*.*)|*.*",
                Title = "Seleziona un file CSV"
            };

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
                    rows.Add(line.Split(","));
                    currentLine++;

                    //Aggiornamento ProgressBar
                    int progress = (int)((double)currentLine / totalLines * 100));
                    worker.ReportProgress(progress);
                }
            }
            e.Result = rows;
        }
        private void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                progressBar.Value = e.ProgressPercentage;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


    }
}