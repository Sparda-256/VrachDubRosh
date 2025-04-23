using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VrachDubRosh
{
    public partial class ManagerWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private bool isDarkTheme = false;
        
        // Кэшированные таблицы для фильтрации
        private DataTable dtPatients;
        private DataTable dtAccompanying;
        
        // Выбранные ID
        private int selectedPatientID = -1;
        private int selectedAccompanyingPersonID = -1;

        public ManagerWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            
            // Проверяем текущую тему при запуске
            ResourceDictionary currentDict = Application.Current.Resources.MergedDictionaries[0];
            if (currentDict.Source.ToString().Contains("DarkTheme"))
            {
                isDarkTheme = true;
                themeToggle.IsChecked = true;
                this.Title = "Врач ДубРощ - Менеджер (Темная тема)";
            }
            
            // Загружаем данные при инициализации окна
            Loaded += ManagerWindow_Loaded;
        }
        
        private void ManagerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPatients();
            LoadAccompanyingPersons();
        }

        #region Загрузка данных
        
        private void LoadPatients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName, DateOfBirth, Gender, RecordDate, DischargeDate FROM Patients";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtPatients = new DataTable();
                    da.Fill(dtPatients);
                    dgPatients.ItemsSource = dtPatients.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пациентов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadAccompanyingPersons()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT ap.AccompanyingPersonID, ap.FullName, ap.DateOfBirth, 
                               p.FullName AS PatientName, ap.Relationship, ap.HasPowerOfAttorney, 
                               ap.PatientID
                        FROM AccompanyingPersons ap
                        JOIN Patients p ON ap.PatientID = p.PatientID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtAccompanying = new DataTable();
                    da.Fill(dtAccompanying);
                    dgAccompanying.ItemsSource = dtAccompanying.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки сопровождающих лиц: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        #endregion
        
        #region Поиск
        
        private void txtSearchPatients_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtPatients != null)
            {
                string filter = txtSearchPatients.Text.Trim().Replace("'", "''");
                dtPatients.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                    ? ""
                    : $"FullName LIKE '%{filter}%'";
            }
        }
        
        private void txtSearchAccompanying_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtAccompanying != null)
            {
                string filter = txtSearchAccompanying.Text.Trim().Replace("'", "''");
                dtAccompanying.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                    ? ""
                    : $"FullName LIKE '%{filter}%' OR PatientName LIKE '%{filter}%'";
            }
        }
        
        #endregion
        
        #region Обработчики событий выбора
        
        private void dgPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgPatients.SelectedItem is DataRowView row)
            {
                selectedPatientID = Convert.ToInt32(row["PatientID"]);
            }
            else
            {
                selectedPatientID = -1;
            }
        }
        
        private void dgAccompanying_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAccompanying.SelectedItem is DataRowView row)
            {
                selectedAccompanyingPersonID = Convert.ToInt32(row["AccompanyingPersonID"]);
            }
            else
            {
                selectedAccompanyingPersonID = -1;
            }
        }
        
        private void dgPatients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedPatientID > 0)
            {
                btnEditPatient_Click(sender, e);
            }
        }
        
        private void dgAccompanying_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedAccompanyingPersonID > 0)
            {
                btnEditAccompanying_Click(sender, e);
            }
        }
        
        #endregion
        
        #region Обработчики кнопок для пациентов
        
        private void btnAddPatient_Click(object sender, RoutedEventArgs e)
        {
            AddEditPatientWindow addPatientWindow = new AddEditPatientWindow(this);
            if (addPatientWindow.ShowDialog() == true)
            {
                LoadPatients();
            }
        }
        
        private void btnEditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPatientID <= 0)
            {
                MessageBox.Show("Выберите пациента для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            AddEditPatientWindow editPatientWindow = new AddEditPatientWindow(this, selectedPatientID);
            if (editPatientWindow.ShowDialog() == true)
            {
                LoadPatients();
            }
        }
        
        private void btnDeletePatient_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPatientID <= 0)
            {
                MessageBox.Show("Выберите пациента для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (MessageBox.Show("Вы уверены, что хотите удалить выбранного пациента?\nТакже будут удалены все документы и связанные сопровождающие лица.", 
                               "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // Удаление документов сопровождающих лиц
                            string deleteAccompanyingDocsQuery = @"
                                DELETE FROM AccompanyingPersonDocuments
                                WHERE AccompanyingPersonID IN (
                                    SELECT AccompanyingPersonID FROM AccompanyingPersons
                                    WHERE PatientID = @PatientID
                                )";
                            using (SqlCommand cmd = new SqlCommand(deleteAccompanyingDocsQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаление сопровождающих лиц
                            string deleteAccompanyingQuery = "DELETE FROM AccompanyingPersons WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deleteAccompanyingQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаление документов пациента
                            string deleteDocsQuery = "DELETE FROM PatientDocuments WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deleteDocsQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаление пациента из связанных таблиц
                            string deleteAssignmentsQuery = "DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deleteAssignmentsQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            string deletePatientDiagnosesQuery = "DELETE FROM PatientDiagnoses WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deletePatientDiagnosesQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            string deletePatientDescriptionsQuery = "DELETE FROM PatientDescriptions WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deletePatientDescriptionsQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            string deleteProcedureAppointmentsQuery = "DELETE FROM ProcedureAppointments WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deleteProcedureAppointmentsQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаление самого пациента
                            string deletePatientQuery = "DELETE FROM Patients WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deletePatientQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", selectedPatientID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            tran.Commit();
                            MessageBox.Show("Пациент успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            throw new Exception("Ошибка при удалении: " + ex.Message);
                        }
                    }
                }
                
                LoadPatients();
                LoadAccompanyingPersons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении пациента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnManageDocuments_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPatientID <= 0)
            {
                MessageBox.Show("Выберите пациента для управления документами.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем имя пациента
            string patientName = "";
            DateTime? dateOfBirth = null;
            if (dgPatients.SelectedItem is DataRowView row)
            {
                patientName = row["FullName"].ToString();
                dateOfBirth = row["DateOfBirth"] as DateTime?;
            }
            
            PatientDocumentsWindow documentsWindow = new PatientDocumentsWindow(selectedPatientID, patientName, dateOfBirth);
            documentsWindow.Owner = this;
            documentsWindow.ShowDialog();
        }
        
        #endregion
        
        #region Обработчики кнопок для сопровождающих лиц
        
        private void btnAddAccompanying_Click(object sender, RoutedEventArgs e)
        {
            AddEditAccompanyingWindow addWindow = new AddEditAccompanyingWindow(this);
            if (addWindow.ShowDialog() == true)
            {
                LoadAccompanyingPersons();
            }
        }
        
        private void btnEditAccompanying_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAccompanyingPersonID <= 0)
            {
                MessageBox.Show("Выберите сопровождающего для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            AddEditAccompanyingWindow editWindow = new AddEditAccompanyingWindow(this, selectedAccompanyingPersonID);
            if (editWindow.ShowDialog() == true)
            {
                LoadAccompanyingPersons();
            }
        }
        
        private void btnDeleteAccompanying_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAccompanyingPersonID <= 0)
            {
                MessageBox.Show("Выберите сопровождающего для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (MessageBox.Show("Вы уверены, что хотите удалить выбранного сопровождающего?\nТакже будут удалены все связанные документы.", 
                               "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // Удаление документов сопровождающего лица
                            string deleteDocsQuery = "DELETE FROM AccompanyingPersonDocuments WHERE AccompanyingPersonID = @AccompanyingPersonID";
                            using (SqlCommand cmd = new SqlCommand(deleteDocsQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@AccompanyingPersonID", selectedAccompanyingPersonID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаление самого сопровождающего
                            string deleteQuery = "DELETE FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID";
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@AccompanyingPersonID", selectedAccompanyingPersonID);
                                cmd.ExecuteNonQuery();
                            }
                            
                            tran.Commit();
                            MessageBox.Show("Сопровождающий успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            throw new Exception("Ошибка при удалении: " + ex.Message);
                        }
                    }
                }
                
                LoadAccompanyingPersons();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении сопровождающего: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnManageAccompanyingDocuments_Click(object sender, RoutedEventArgs e)
        {
            if (selectedAccompanyingPersonID <= 0)
            {
                MessageBox.Show("Выберите сопровождающего для управления документами.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем данные о сопровождающем
            string accompaningName = "";
            int patientID = -1;
            string patientName = "";
            
            if (dgAccompanying.SelectedItem is DataRowView row)
            {
                accompaningName = row["FullName"].ToString();
                patientID = Convert.ToInt32(row["PatientID"]);
                patientName = row["PatientName"].ToString();
            }
            
            AccompanyingDocumentsWindow documentsWindow = new AccompanyingDocumentsWindow(selectedAccompanyingPersonID, accompaningName, patientID, patientName);
            documentsWindow.Owner = this;
            documentsWindow.ShowDialog();
        }
        
        #endregion

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ChangeTheme(true);
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ChangeTheme(false);
        }

        private void ChangeTheme(bool isDark)
        {
            isDarkTheme = isDark;
            // Получаем доступ к ресурсам приложения
            ResourceDictionary resourceDict = new ResourceDictionary();
            
            // Меняем источник ресурсов в зависимости от выбранной темы
            if (isDark)
            {
                resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
                this.Title = "Врач ДубРощ - Менеджер (Темная тема)";
            }
            else
            {
                resourceDict.Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative);
                this.Title = "Врач ДубРощ - Менеджер";
            }

            // Заменяем текущие ресурсы приложения на новые
            var appResources = Application.Current.Resources.MergedDictionaries;
            if (appResources.Count > 0)
            {
                appResources[0] = resourceDict;
            }
            else
            {
                appResources.Add(resourceDict);
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
} 