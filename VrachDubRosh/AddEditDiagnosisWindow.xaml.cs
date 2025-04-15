using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VrachDubRosh
{
    public partial class AddEditDiagnosisWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;
        private int? _diagnosisID;
        private bool _isEditMode;
        private DataTable _diagnosesDataTable;

        /// <summary>
        /// Конструктор для добавления нового диагноза
        /// </summary>
        public AddEditDiagnosisWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            _diagnosisID = null;
            _isEditMode = false;
            
            tbPatientInfo.Text = $"Добавление диагноза для пациента: {patientName}";
            Title = "Добавление диагноза";
            
            // Установка значения по умолчанию для процента
            txtPercentage.Text = "50";
            
            // Загружаем список диагнозов
            LoadDiagnoses();
            
            this.Loaded += AddEditDiagnosisWindow_Loaded;
        }

        /// <summary>
        /// Конструктор для редактирования существующего диагноза
        /// </summary>
        public AddEditDiagnosisWindow(int patientID, string patientName, int diagnosisID)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            _diagnosisID = diagnosisID;
            _isEditMode = true;
            
            tbPatientInfo.Text = $"Редактирование диагноза для пациента: {patientName}";
            Title = "Редактирование диагноза";
            
            // Загружаем список диагнозов
            LoadDiagnoses();
            
            // Загружаем данные диагноза
            LoadDiagnosisData();
            
            this.Loaded += AddEditDiagnosisWindow_Loaded;
        }

        /// <summary>
        /// Обработчик события загрузки окна
        /// </summary>
        private void AddEditDiagnosisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем тему родительского окна и применяем её
            if (this.Owner != null)
            {
                if (this.Owner is GlavDoctorWindow glavWindow && glavWindow.isDarkTheme)
                {
                    ApplyDarkTheme();
                }
                else if (this.Owner is DoctorWindow doctorWindow && doctorWindow.isDarkTheme)
                {
                    ApplyDarkTheme();
                }
            }
        }

        /// <summary>
        /// Применяет темную тему к окну
        /// </summary>
        private void ApplyDarkTheme()
        {
            // Применяем темную тему
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        /// <summary>
        /// Проверяет, что в поле вероятности вводятся только числа
        /// </summary>
        private void txtPercentage_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Только цифры
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Обработчик изменения текста поиска
        /// </summary>
        private void txtSearchDiagnosis_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterDiagnoses();
        }

        /// <summary>
        /// Фильтрует список диагнозов по введённому тексту
        /// </summary>
        private void FilterDiagnoses()
        {
            if (_diagnosesDataTable == null || cbDiagnoses == null)
                return;

            string searchText = txtSearchDiagnosis.Text.ToLower().Trim();
            
            if (string.IsNullOrEmpty(searchText))
            {
                cbDiagnoses.ItemsSource = _diagnosesDataTable.DefaultView;
            }
            else
            {
                DataView dv = _diagnosesDataTable.DefaultView;
                dv.RowFilter = $"DiagnosisName LIKE '%{searchText.Replace("'", "''")}%'";
                cbDiagnoses.ItemsSource = dv;
            }
        }

        /// <summary>
        /// Загружает список диагнозов из базы данных
        /// </summary>
        private void LoadDiagnoses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DiagnosisID, DiagnosisName FROM Diagnoses ORDER BY DiagnosisName";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    _diagnosesDataTable = new DataTable();
                    da.Fill(_diagnosesDataTable);
                    cbDiagnoses.ItemsSource = _diagnosesDataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает данные диагноза для редактирования
        /// </summary>
        private void LoadDiagnosisData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pd.DiagnosisID, pd.PercentageOfDiagnosis
                                     FROM PatientDiagnoses pd
                                     WHERE pd.PatientID = @PatientID AND pd.DiagnosisID = @DiagnosisID";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        cmd.Parameters.AddWithValue("@DiagnosisID", _diagnosisID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Выбираем диагноз в комбобоксе
                                foreach (DataRowView item in cbDiagnoses.Items)
                                {
                                    if (Convert.ToInt32(item["DiagnosisID"]) == Convert.ToInt32(reader["DiagnosisID"]))
                                    {
                                        cbDiagnoses.SelectedItem = item;
                                        break;
                                    }
                                }
                                
                                // Устанавливаем процент
                                int percentage = reader["PercentageOfDiagnosis"] != DBNull.Value 
                                    ? Convert.ToInt32(reader["PercentageOfDiagnosis"]) 
                                    : 50;
                                txtPercentage.Text = percentage.ToString();
                            }
                            else
                            {
                                MessageBox.Show("Диагноз не найден", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохраняет диагноз в базу данных
        /// </summary>
        private void SaveDiagnosis()
        {
            // Проверяем обязательные поля
            if (cbDiagnoses.SelectedItem == null)
            {
                MessageBox.Show("Выберите диагноз из списка", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtPercentage.Text))
            {
                MessageBox.Show("Укажите значение вероятности", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Проверяем корректность процента
            if (!int.TryParse(txtPercentage.Text, out int percentage) || percentage < 0 || percentage > 100)
            {
                MessageBox.Show("Вероятность должна быть числом от 0 до 100", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlCommand cmd;
                    
                    // Получаем ID выбранного диагноза
                    int diagnosisID = Convert.ToInt32(((DataRowView)cbDiagnoses.SelectedItem)["DiagnosisID"]);
                    
                    if (_isEditMode && _diagnosisID.HasValue)
                    {
                        // Обновляем существующий диагноз
                        string query = @"UPDATE PatientDiagnoses 
                                         SET DiagnosisID = @DiagnosisID, 
                                             PercentageOfDiagnosis = @PercentageOfDiagnosis 
                                         WHERE PatientID = @PatientID AND DiagnosisID = @OldDiagnosisID";
                        
                        cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@OldDiagnosisID", _diagnosisID.Value);
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                        cmd.Parameters.AddWithValue("@PercentageOfDiagnosis", percentage);
                    }
                    else
                    {
                        // Проверяем, не существует ли уже такой диагноз у пациента
                        string checkQuery = @"SELECT COUNT(*) FROM PatientDiagnoses 
                                             WHERE PatientID = @PatientID AND DiagnosisID = @DiagnosisID";
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                        {
                            checkCmd.Parameters.AddWithValue("@PatientID", _patientID);
                            checkCmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            
                            if (count > 0)
                            {
                                MessageBox.Show("Этот диагноз уже существует у данного пациента.", 
                                              "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        
                        // Добавляем новый диагноз
                        string query = @"INSERT INTO PatientDiagnoses 
                                         (PatientID, DiagnosisID, PercentageOfDiagnosis) 
                                         VALUES 
                                         (@PatientID, @DiagnosisID, @PercentageOfDiagnosis)";
                        
                        cmd = new SqlCommand(query, con);
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                        cmd.Parameters.AddWithValue("@PercentageOfDiagnosis", percentage);
                    }
                    
                    cmd.ExecuteNonQuery();
                    
                    MessageBox.Show(_isEditMode ? "Диагноз успешно обновлен" : "Диагноз успешно добавлен", 
                                   "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении диагноза: " + ex.Message, 
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик клика по кнопке "Сохранить"
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveDiagnosis();
        }

        /// <summary>
        /// Обработчик клика по кнопке "Отмена"
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
} 