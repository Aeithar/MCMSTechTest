using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MCMSTechTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _sqliteFilename;
        private int _page = 0;
        private int _totalPages = 0;
        private string _whereClause;

        public MainWindow()
        {
            InitializeComponent();
            SetControlStatus(false);
        }

        private void SetControlStatus(bool enabled)
        {
            btnFirst.IsEnabled = enabled;
            btnPrevious.IsEnabled = enabled;
            btnNext.IsEnabled = enabled;
            btnLast.IsEnabled = enabled;
            txtPageNo.IsEnabled = enabled;
            txtSearch.IsEnabled = enabled;
        }

        public void SelectSQLiteFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var extn = Path.GetExtension(openFileDialog.FileName);
                if (extn.ToLower() == ".sqlite" && File.Exists(openFileDialog.FileName))
                {
                    _sqliteFilename = openFileDialog.FileName;
                    _page = 1;
                    txtPageNo.Text = _page.ToString();
                    GetTotalPages();
                    ReadData();
                    SetControlStatus(true);
                }
                else
                {
                    MessageBox.Show("That is not a valid file, please select another.");
                }
            }
        }

        private void GetTotalPages()
        {
            var connectionString = string.Format("Data Source={0}; Version = 3; New = True; Compress = True;", _sqliteFilename);
            using (var sqlite_conn = new SQLiteConnection(connectionString))
            {
                sqlite_conn.Open();
                SQLiteCommand sqlite_cmd;
                sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = "SELECT COUNT(*) FROM safe_hashes " + _whereClause;
                var count = Convert.ToDouble((long)sqlite_cmd.ExecuteScalar());
                sqlite_conn.Close();
                _totalPages = (int)Math.Ceiling(count / 10);
                lblOf.Content = string.Format("of {0}", _totalPages);
                if (_totalPages == 0)
                {
                    _page = 0;
                    txtPageNo.Text = _page.ToString();
                }
            }
        }

        private void ReadData()
        {
            var connectionString = string.Format("Data Source={0}; Version = 3; New = True; Compress = True;", _sqliteFilename);
            using (var sqlite_conn = new SQLiteConnection(connectionString))
            {
                SQLiteCommand sqlite_cmd;
                sqlite_cmd = sqlite_conn.CreateCommand();
                sqlite_cmd.CommandText = string.Format("SELECT * FROM safe_hashes {1} ORDER BY hash_id LIMIT 10 OFFSET {0}", (_page - 1) * 10, _whereClause);

                using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sqlite_cmd.CommandText, sqlite_conn))
                {
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    dgBrowser.ItemsSource = dataTable.AsDataView();
                }
            }
        }

        private void FirstPage_Click(object sender, RoutedEventArgs e)
        {
            if (_page > 1)
            {
                _page = 1;
                ReadData();
                txtPageNo.Text = _page.ToString();
            }
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_page > 1)
            {
                _page -= 1;
                ReadData();
                txtPageNo.Text = _page.ToString();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_page < _totalPages)
            {
                _page += 1;
                ReadData();
                txtPageNo.Text = _page.ToString();
            }
        }

        private void LastPage_Click(object sender, RoutedEventArgs e)
        {
            if (_page < _totalPages)
            {
                _page = _totalPages;
                ReadData();
                txtPageNo.Text = _page.ToString();
            }
        }

        private void txtPageNo_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var pageText = txtPageNo.Text;
                int pageNo;
                if (int.TryParse(pageText, out pageNo))
                {
                    if (pageNo > 0 && pageNo <= _totalPages)
                    {
                        _page = pageNo;
                        ReadData();
                        return;
                    }
                }
                txtPageNo.Text = _page.ToString();
            }
        }

        private void txtSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var searchText = txtSearch.Text;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var items = searchText.Split(',');
                    var firstItem = true;
                    if (items.Length > 0)
                    {
                        _whereClause = "WHERE sha1 IN (";
                        foreach(var item in items)
                        {
                            if (!string.IsNullOrWhiteSpace(item.Trim()))
                            if (!firstItem)
                            {
                                _whereClause += ",";
                            }
                            firstItem = false;
                            _whereClause += string.Format("\"{0}\"", item.Trim());
                        }
                        _whereClause += ")";
                    }
                }
                else
                {
                    _whereClause = "";
                }
            }
            _page = 1;
            txtPageNo.Text = _page.ToString();
            GetTotalPages();
            ReadData();
        }

    }
}
