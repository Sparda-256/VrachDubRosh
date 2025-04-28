using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using VrachDubRosh;

namespace VrachDubRosh
{
    public partial class GlavDoctorWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";

        // Кэшированные таблицы для фильтрации
        private DataTable dtPatients;
        private DataTable dtDoctors;
        
        // Флаг для отслеживания текущей темы
        public bool isDarkTheme = false;

        public GlavDoctorWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            Loaded += GlavDoctorWindow_Loaded;
        }

        private void GlavDoctorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Первоначальная загрузка данных
            LoadPatients();
            LoadDoctors();

            // Проверяем текущую тему приложения
            ResourceDictionary currentDict = Application.Current.Resources.MergedDictionaries[0];
            if (currentDict.Source.ToString().Contains("DarkTheme"))
            {
                isDarkTheme = true;
                themeToggle.IsChecked = true;
                this.Title = "Врач ДубРощ - Главврач (Темная тема)";
            }
        }

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
                this.Title = "Врач ДубРощ - Главврач (Темная тема)";
            }
            else
            {
                resourceDict.Source = new Uri("/Themes/LightTheme.xaml", UriKind.Relative);
                this.Title = "Врач ДубРощ - Главврач";
            }
            
            // Обновляем ресурсы приложения
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        #region Обработчики поиска
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

        private void txtSearchDoctors_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtDoctors != null)
            {
                string filter = txtSearchDoctors.Text.Trim().Replace("'", "''");
                dtDoctors.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                    ? ""
                    : $"FullName LIKE '%{filter}%'";
            }
        }
        #endregion

        #region Обработчики кликов
        private void dgPatients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgPatients.SelectedItem is DataRowView row)
            {
                int patientID = Convert.ToInt32(row["PatientID"]);
                string patientName = row["FullName"].ToString();
                MedCardWindow medCardWindow = new MedCardWindow(patientID, patientName);
                medCardWindow.Owner = this;
                medCardWindow.ShowDialog();
            }
        }

        private void dgDoctors_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgDoctors.SelectedItem is DataRowView row)
            {
                int doctorID = Convert.ToInt32(row["DoctorID"]);
                string doctorName = row["FullName"].ToString();
                DoctorProceduresWindow dpw = new DoctorProceduresWindow(doctorID, doctorName);
                dpw.Owner = this;
                dpw.ShowDialog();
            }
        }
        #endregion

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
                MessageBox.Show("Ошибка загрузки пациентов: " + ex.Message);
            }
        }

        private void LoadDoctors()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DoctorID, FullName, Specialty, OfficeNumber, WorkExperience FROM Doctors";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtDoctors = new DataTable();
                    da.Fill(dtDoctors);
                    dgDoctors.ItemsSource = dtDoctors.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки врачей: " + ex.Message);
            }
        }
        #endregion

        #region Управление пациентами

        private void btnAssignDoctors_Click(object sender, RoutedEventArgs e)
        {
            if (dgPatients.SelectedItem == null)
            {
                MessageBox.Show("Выберите пациента для назначения врача.");
                return;
            }
            DataRowView row = dgPatients.SelectedItem as DataRowView;
            int patientID = Convert.ToInt32(row["PatientID"]);
            string patientName = row["FullName"].ToString();

            PatientAssignmentWindow assignmentWindow = new PatientAssignmentWindow(patientID, patientName);
            assignmentWindow.Owner = this;
            assignmentWindow.ShowDialog();
        }

        private void btnCreateDischargeDocument_Click(object sender, RoutedEventArgs e)
        {
            if (dgPatients.SelectedItem == null)
            {
                MessageBox.Show("Выберите пациента для создания выписного эпикриза.", "Выбор пациента", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DataRowView row = dgPatients.SelectedItem as DataRowView;
            int patientID = Convert.ToInt32(row["PatientID"]);
            string patientName = row["FullName"].ToString();

            // Получаем имя главврача (пользователя) из настроек или из текущей сессии
            // В данном примере используем фиксированное значение
            string chiefDoctorName = "Администратор";
            
            // Открываем окно создания выписного эпикриза
            DischargeDocumentWindow dischargeWindow = new DischargeDocumentWindow(patientID, patientName, chiefDoctorName);
            dischargeWindow.Owner = this;
            dischargeWindow.ShowDialog();
        }

        private void btnOpenReports_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow reportWindow = new ReportWindow();
            reportWindow.Owner = this;
            
            // Передаем информацию о текущей теме
            if (isDarkTheme)
            {
                reportWindow.Loaded += (s, args) =>
                {
                    // Устанавливаем темную тему в окне отчетов
                    if (reportWindow.themeToggle != null)
                    {
                        reportWindow.themeToggle.IsChecked = true;
                    }
                };
            }
            
            reportWindow.ShowDialog();
        }
        #endregion

        #region Управление врачами

        private void btnAddDoctor_Click(object sender, RoutedEventArgs e)
        {
            AddEditDoctorWindow addDoctorWindow = new AddEditDoctorWindow();
            addDoctorWindow.Owner = this;
            if (addDoctorWindow.ShowDialog() == true)
            {
                LoadDoctors();
            }
        }

        private void btnEditDoctor_Click(object sender, RoutedEventArgs e)
        {
            if (dgDoctors.SelectedItem == null)
            {
                MessageBox.Show("Выберите врача для редактирования.");
                return;
            }
            DataRowView row = dgDoctors.SelectedItem as DataRowView;
            int doctorID = Convert.ToInt32(row["DoctorID"]);
            AddEditDoctorWindow editDoctorWindow = new AddEditDoctorWindow(doctorID);
            editDoctorWindow.Owner = this;
            if (editDoctorWindow.ShowDialog() == true)
            {
                LoadDoctors();
            }
        }

        private void btnDeleteDoctor_Click(object sender, RoutedEventArgs e)
        {
            if (dgDoctors.SelectedItems == null || dgDoctors.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного врача для удаления.");
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить выбранных врачей?",
                                "Подтверждение", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
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
                        foreach (var selectedItem in dgDoctors.SelectedItems)
                        {
                            if (selectedItem is DataRowView row)
                            {
                                int doctorID = Convert.ToInt32(row["DoctorID"]);

                                string deleteAppointments = @"DELETE FROM ProcedureAppointments WHERE DoctorID = @DoctorID";
                                using (SqlCommand cmdApp = new SqlCommand(deleteAppointments, con, tran))
                                {
                                    cmdApp.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdApp.ExecuteNonQuery();
                                }

                                string deleteDescriptions = @"DELETE FROM PatientDescriptions WHERE DoctorID = @DoctorID";
                                using (SqlCommand cmdDesc = new SqlCommand(deleteDescriptions, con, tran))
                                {
                                    cmdDesc.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdDesc.ExecuteNonQuery();
                                }

                                string deleteAssignments = @"DELETE FROM PatientDoctorAssignments WHERE DoctorID = @DoctorID";
                                using (SqlCommand cmdAssign = new SqlCommand(deleteAssignments, con, tran))
                                {
                                    cmdAssign.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdAssign.ExecuteNonQuery();
                                }

                                string deleteProcedures = @"DELETE FROM Procedures WHERE DoctorID = @DoctorID";
                                using (SqlCommand cmdProcs = new SqlCommand(deleteProcedures, con, tran))
                                {
                                    cmdProcs.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdProcs.ExecuteNonQuery();
                                }

                                string deleteDoctor = @"DELETE FROM Doctors WHERE DoctorID = @DoctorID";
                                using (SqlCommand cmdDoc = new SqlCommand(deleteDoctor, con, tran))
                                {
                                    cmdDoc.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdDoc.ExecuteNonQuery();
                                }
                            }
                        }
                        tran.Commit();
                    }
                }
                MessageBox.Show("Выбранные врачи удалены.");
                LoadDoctors();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении врачей: " + ex.Message);
            }
        }
        #endregion

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}
