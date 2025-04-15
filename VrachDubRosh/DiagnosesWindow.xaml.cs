using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace VrachDubRosh
{
    public partial class DiagnosesWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;
        private DataRowView _selectedDiagnosis;

        public DiagnosesWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            tbPatientInfo.Text = $"Диагнозы пациента: {patientName}";
            
            LoadDiagnoses();
            
            this.Loaded += DiagnosesWindow_Loaded;
        }

        /// <summary>
        /// Обработчик события загрузки окна
        /// </summary>
        private void DiagnosesWindow_Loaded(object sender, RoutedEventArgs e)
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
        /// Загружает список диагнозов пациента из базы данных
        /// </summary>
        private void LoadDiagnoses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pd.PatientID, pd.DiagnosisID, d.DiagnosisName, 
                                    pd.PercentageOfDiagnosis,
                                    doc.FullName as DoctorName
                                    FROM PatientDiagnoses pd
                                    INNER JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                                    LEFT JOIN DoctorDiagnoses dd ON d.DiagnosisID = dd.DiagnosisID
                                    LEFT JOIN Doctors doc ON dd.DoctorID = doc.DoctorID
                                    WHERE pd.PatientID = @PatientID
                                    ORDER BY pd.PercentageOfDiagnosis DESC";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgDiagnoses.ItemsSource = dt.DefaultView;
                    
                    // Очищаем детали диагноза
                    ClearDiagnosisDetails();
                    
                    // Отключаем кнопки редактирования и удаления
                    btnEditDiagnosis.IsEnabled = false;
                    btnDeleteDiagnosis.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Очищает поля с деталями диагноза
        /// </summary>
        private void ClearDiagnosisDetails()
        {
            tbDiagnosisName.Text = "";
            tbDoctorName.Text = "";
            tbPercentage.Text = "";
            
            // Отключаем кнопки редактирования и удаления
            btnEditDiagnosis.IsEnabled = false;
            btnDeleteDiagnosis.IsEnabled = false;
        }

        /// <summary>
        /// Обработчик события выбора диагноза в DataGrid
        /// </summary>
        private void dgDiagnoses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDiagnoses.SelectedItem != null)
            {
                _selectedDiagnosis = dgDiagnoses.SelectedItem as DataRowView;
                if (_selectedDiagnosis != null)
                {
                    // Заполняем детали диагноза
                    tbDiagnosisName.Text = _selectedDiagnosis["DiagnosisName"].ToString();
                    
                    tbDoctorName.Text = _selectedDiagnosis["DoctorName"] != DBNull.Value 
                        ? _selectedDiagnosis["DoctorName"].ToString() 
                        : "Не определен";
                    
                    var percentage = _selectedDiagnosis["PercentageOfDiagnosis"];
                    tbPercentage.Text = percentage != DBNull.Value 
                        ? percentage.ToString() + "%" 
                        : "Не указан";
                    
                    // Активируем кнопки редактирования и удаления
                    btnEditDiagnosis.IsEnabled = true;
                    btnDeleteDiagnosis.IsEnabled = true;
                }
            }
            else
            {
                ClearDiagnosisDetails();
                btnEditDiagnosis.IsEnabled = false;
                btnDeleteDiagnosis.IsEnabled = false;
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Добавить диагноз"
        /// </summary>
        private void btnAddDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            AddEditDiagnosisWindow addDiagnosisWindow = new AddEditDiagnosisWindow(_patientID, _patientName);
            addDiagnosisWindow.Owner = this;
            bool? result = addDiagnosisWindow.ShowDialog();
            
            if (result == true)
            {
                // Перезагружаем список диагнозов
                LoadDiagnoses();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Редактировать"
        /// </summary>
        private void btnEditDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDiagnosis != null)
            {
                int diagnosisID = Convert.ToInt32(_selectedDiagnosis["DiagnosisID"]);
                AddEditDiagnosisWindow editDiagnosisWindow = new AddEditDiagnosisWindow(_patientID, _patientName, diagnosisID);
                editDiagnosisWindow.Owner = this;
                bool? result = editDiagnosisWindow.ShowDialog();
                
                if (result == true)
                {
                    // Перезагружаем список диагнозов
                    LoadDiagnoses();
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Удалить"
        /// </summary>
        private void btnDeleteDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDiagnosis != null)
            {
                // Запрашиваем подтверждение на удаление
                MessageBoxResult result = MessageBox.Show(
                    $"Вы действительно хотите удалить диагноз \"{_selectedDiagnosis["DiagnosisName"]}\" у пациента {_patientName}?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        int diagnosisID = Convert.ToInt32(_selectedDiagnosis["DiagnosisID"]);
                        
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = "DELETE FROM PatientDiagnoses WHERE PatientID = @PatientID AND DiagnosisID = @DiagnosisID";
                            
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", _patientID);
                                cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                                
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show("Диагноз успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    // Перезагружаем список диагнозов
                                    LoadDiagnoses();
                                }
                                else
                                {
                                    MessageBox.Show("Не удалось удалить диагноз", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Закрыть"
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 