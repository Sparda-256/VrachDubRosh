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
        private DataTable dtNewPatients;
        private DataTable dtPatients;
        private DataTable dtDoctors;

        // Таймер для обновления списка новых пациентов
        private DispatcherTimer newPatientsTimer;

        public GlavDoctorWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            Loaded += GlavDoctorWindow_Loaded;
        }

        private void GlavDoctorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Первоначальная загрузка данных
            LoadNewPatients();
            LoadPatients();
            LoadDoctors();
            LoadDoctorsForComboBox();
            dpRecordDate.SelectedDate = DateTime.Today;

            // Настраиваем таймер для обновления списка новых пациентов каждые 3 секунды
            newPatientsTimer = new DispatcherTimer();
            newPatientsTimer.Interval = TimeSpan.FromSeconds(10);
            newPatientsTimer.Tick += (s, args) => LoadNewPatients();
            newPatientsTimer.Start();
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

        private void txtSearchNewPatients_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtNewPatients != null)
            {
                string filter = txtSearchNewPatients.Text.Trim().Replace("'", "''");
                dtNewPatients.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
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

        private void dgNewPatients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Можно добавить логику двойного клика для новых пациентов при необходимости
        }

        private void dgNewPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgNewPatients.SelectedItem is DataRowView row)
            {
                if (row["PredictedDoctorID"] != DBNull.Value)
                {
                    int predictedDoctorId = Convert.ToInt32(row["PredictedDoctorID"]);
                    cbDoctors.SelectedValue = predictedDoctorId;
                }
                else
                {
                    cbDoctors.SelectedIndex = -1;
                }
            }
        }

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

        private void LoadNewPatients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT NewPatientID, FullName, DateOfBirth, Gender, PredictedDoctorID FROM NewPatients";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtNewPatients = new DataTable();
                    da.Fill(dtNewPatients);
                    dgNewPatients.ItemsSource = dtNewPatients.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки новых пациентов: " + ex.Message);
            }
        }

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

        private void LoadDoctorsForComboBox()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DoctorID, FullName FROM Doctors";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cbDoctors.ItemsSource = dt.DefaultView;
                    cbDoctors.SelectedValuePath = "DoctorID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки списка врачей: " + ex.Message);
            }
        }

        #endregion

        #region Новые пациенты -> Перенос в Patients

        private void btnAssignPatient_Click(object sender, RoutedEventArgs e)
        {
            if (dgNewPatients.SelectedItem == null)
            {
                MessageBox.Show("Выберите пациента из списка новых пациентов.");
                return;
            }

            if (cbDoctors.SelectedValue == null)
            {
                MessageBox.Show("Выберите врача для назначения.");
                return;
            }

            // Проверяем обязательную дату записи
            if (dpRecordDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату записи.");
                return;
            }

            DateTime recordDate = dpRecordDate.SelectedDate.Value;
            DateTime? dischargeDate = dpDischargeDate.SelectedDate; // Может быть null

            // Если дата выписки задана, проверяем, что она не раньше даты записи
            if (dischargeDate.HasValue && dischargeDate.Value < recordDate)
            {
                MessageBox.Show("Дата выписки не может быть раньше даты записи.");
                return;
            }

            DataRowView row = dgNewPatients.SelectedItem as DataRowView;
            int newPatientID = Convert.ToInt32(row["NewPatientID"]);
            string fullName = row["FullName"].ToString();
            DateTime dateOfBirth = Convert.ToDateTime(row["DateOfBirth"]);
            string gender = row["Gender"].ToString();
            int doctorID = Convert.ToInt32(cbDoctors.SelectedValue);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        int newPatientInsertedID;
                        // Если дата выписки задана, выполняем запрос с параметром для даты выписки
                        if (dischargeDate.HasValue)
                        {
                            string insertQuery = @"
                                INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, DischargeDate)
                                VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @DischargeDate);
                                SELECT SCOPE_IDENTITY();";
                            using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con, tran))
                            {
                                cmdInsert.Parameters.AddWithValue("@FullName", fullName);
                                cmdInsert.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                                cmdInsert.Parameters.AddWithValue("@Gender", gender);
                                cmdInsert.Parameters.AddWithValue("@RecordDate", recordDate);
                                cmdInsert.Parameters.AddWithValue("@DischargeDate", dischargeDate.Value);
                                newPatientInsertedID = Convert.ToInt32(cmdInsert.ExecuteScalar());
                            }
                        }
                        else // Иначе – без даты выписки
                        {
                            string insertQuery = @"
                                INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate)
                                VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate);
                                SELECT SCOPE_IDENTITY();";
                            using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con, tran))
                            {
                                cmdInsert.Parameters.AddWithValue("@FullName", fullName);
                                cmdInsert.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                                cmdInsert.Parameters.AddWithValue("@Gender", gender);
                                cmdInsert.Parameters.AddWithValue("@RecordDate", recordDate);
                                newPatientInsertedID = Convert.ToInt32(cmdInsert.ExecuteScalar());
                            }
                        }

                        // Связываем пациента с выбранным врачом (PatientDoctorAssignments)
                        string assignQuery = "INSERT INTO PatientDoctorAssignments (PatientID, DoctorID) VALUES (@PatientID, @DoctorID)";
                        using (SqlCommand cmdAssign = new SqlCommand(assignQuery, con, tran))
                        {
                            cmdAssign.Parameters.AddWithValue("@PatientID", newPatientInsertedID);
                            cmdAssign.Parameters.AddWithValue("@DoctorID", doctorID);
                            cmdAssign.ExecuteNonQuery();
                        }

                        // Переносим диагнозы из NewPatientDiagnoses в PatientDiagnoses
                        string copyDiagnosesQuery = @"
                            INSERT INTO PatientDiagnoses (PatientID, DiagnosisID, PercentageOfDiagnosis)
                            SELECT @PatientID, DiagnosisID, PercentageOfDiagnosis
                            FROM NewPatientDiagnoses
                            WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdCopyDiagnoses = new SqlCommand(copyDiagnosesQuery, con, tran))
                        {
                            cmdCopyDiagnoses.Parameters.AddWithValue("@PatientID", newPatientInsertedID);
                            cmdCopyDiagnoses.Parameters.AddWithValue("@NewPatientID", newPatientID);
                            cmdCopyDiagnoses.ExecuteNonQuery();
                        }

                        // Удаляем связанные записи из вспомогательных таблиц
                        string deleteSymptomsQuery = "DELETE FROM NewPatientSymptoms WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelSymptoms = new SqlCommand(deleteSymptomsQuery, con, tran))
                        {
                            cmdDelSymptoms.Parameters.AddWithValue("@NewPatientID", newPatientID);
                            cmdDelSymptoms.ExecuteNonQuery();
                        }
                        string deleteAnswersQuery = "DELETE FROM NewPatientAnswers WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelAnswers = new SqlCommand(deleteAnswersQuery, con, tran))
                        {
                            cmdDelAnswers.Parameters.AddWithValue("@NewPatientID", newPatientID);
                            cmdDelAnswers.ExecuteNonQuery();
                        }
                        string deleteDiagnosesQuery = "DELETE FROM NewPatientDiagnoses WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelDiagnoses = new SqlCommand(deleteDiagnosesQuery, con, tran))
                        {
                            cmdDelDiagnoses.Parameters.AddWithValue("@NewPatientID", newPatientID);
                            cmdDelDiagnoses.ExecuteNonQuery();
                        }
                        // Удаляем запись из NewPatients
                        string deleteQuery = "DELETE FROM NewPatients WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelete = new SqlCommand(deleteQuery, con, tran))
                        {
                            cmdDelete.Parameters.AddWithValue("@NewPatientID", newPatientID);
                            cmdDelete.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                }

                MessageBox.Show("Пациент успешно назначен.");
                LoadNewPatients();
                LoadPatients();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при назначении пациента: " + ex.Message);
            }
        }

        #endregion

        #region Управление пациентами

        private void btnAddPatient_Click(object sender, RoutedEventArgs e)
        {
            AddEditPatientWindow addPatientWindow = new AddEditPatientWindow();
            addPatientWindow.Owner = this;
            if (addPatientWindow.ShowDialog() == true)
            {
                LoadPatients();
            }
        }

        private void btnEditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (dgPatients.SelectedItem == null)
            {
                MessageBox.Show("Выберите пациента для редактирования.");
                return;
            }
            DataRowView row = dgPatients.SelectedItem as DataRowView;
            int patientID = Convert.ToInt32(row["PatientID"]);
            AddEditPatientWindow editPatientWindow = new AddEditPatientWindow(patientID);
            editPatientWindow.Owner = this;
            if (editPatientWindow.ShowDialog() == true)
            {
                LoadPatients();
            }
        }

        private void btnDeletePatient_Click(object sender, RoutedEventArgs e)
        {
            if (dgPatients.SelectedItems == null || dgPatients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы одного пациента для удаления.");
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить выбранных пациентов?",
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
                        foreach (var selectedItem in dgPatients.SelectedItems)
                        {
                            if (selectedItem is DataRowView row)
                            {
                                int patientID = Convert.ToInt32(row["PatientID"]);

                                string deleteAppointments = @"DELETE FROM ProcedureAppointments WHERE PatientID = @PatientID";
                                using (SqlCommand cmdApp = new SqlCommand(deleteAppointments, con, tran))
                                {
                                    cmdApp.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdApp.ExecuteNonQuery();
                                }

                                string deleteDescriptions = @"DELETE FROM PatientDescriptions WHERE PatientID = @PatientID";
                                using (SqlCommand cmdDesc = new SqlCommand(deleteDescriptions, con, tran))
                                {
                                    cmdDesc.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdDesc.ExecuteNonQuery();
                                }

                                string deleteAssignments = @"DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                                using (SqlCommand cmdAssign = new SqlCommand(deleteAssignments, con, tran))
                                {
                                    cmdAssign.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdAssign.ExecuteNonQuery();
                                }

                                string deletePatient = @"DELETE FROM Patients WHERE PatientID = @PatientID";
                                using (SqlCommand cmdPat = new SqlCommand(deletePatient, con, tran))
                                {
                                    cmdPat.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdPat.ExecuteNonQuery();
                                }
                            }
                        }
                        tran.Commit();
                    }
                }
                MessageBox.Show("Выбранные пациенты удалены.");
                LoadPatients();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении пациентов: " + ex.Message);
            }
        }

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

        private void btnOpenReports_Click(object sender, RoutedEventArgs e)
        {
            ReportWindow reportWindow = new ReportWindow();
            reportWindow.Owner = this;
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
                LoadDoctorsForComboBox();
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
                LoadDoctorsForComboBox();
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

                                string deleteDoctorDiagnoses = @"DELETE FROM DoctorDiagnoses WHERE DoctorID = @DoctorID";
                                using (SqlCommand cmdDocDiag = new SqlCommand(deleteDoctorDiagnoses, con, tran))
                                {
                                    cmdDocDiag.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdDocDiag.ExecuteNonQuery();
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
                LoadDoctorsForComboBox();
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
