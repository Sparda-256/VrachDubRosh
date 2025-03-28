﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VrachDubRosh;

namespace VrachDubRosh
{
    public partial class GlavDoctorWindow : Window
    {
        // Замените строку подключения на актуальную для вашей базы данных
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";

        // Кэшированные таблицы для фильтрации
        private DataTable dtNewPatients;
        private DataTable dtPatients;
        private DataTable dtDoctors;

        public GlavDoctorWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            Loaded += GlavDoctorWindow_Loaded;
        }

        private void GlavDoctorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNewPatients();
            LoadPatients();
            LoadDoctors();
            LoadDoctorsForComboBox();
        }

        #region Обработчики поиска
        private void txtSearchPatients_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtPatients != null)
            {
                string filter = txtSearchPatients.Text.Trim().Replace("'", "''");
                // Если поле пустое — сбрасываем фильтр
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

        #region Обработчики двойного клика

        // Если понадобится, можно реализовать двойной клик и для новых пациентов
        private void dgNewPatients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Пока оставляем пустым или добавляем аналогичную логику при необходимости
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
                    string query = "SELECT NewPatientID, FullName, DateOfBirth, Gender FROM NewPatients";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtNewPatients = new DataTable(); // Сохраняем в поле класса
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
                    dtPatients = new DataTable(); // Сохраняем в поле класса
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
                    dtDoctors = new DataTable(); // Сохраняем в поле класса
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
                    cbDoctors.DisplayMemberPath = "FullName";
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

            if (dpRecordDate.SelectedDate == null || dpDischargeDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату записи и дату выписки.");
                return;
            }

            DataRowView row = dgNewPatients.SelectedItem as DataRowView;
            int newPatientID = Convert.ToInt32(row["NewPatientID"]);
            string fullName = row["FullName"].ToString();
            DateTime dateOfBirth = Convert.ToDateTime(row["DateOfBirth"]);
            string gender = row["Gender"].ToString();

            int doctorID = Convert.ToInt32(cbDoctors.SelectedValue);
            DateTime recordDate = dpRecordDate.SelectedDate.Value;
            DateTime dischargeDate = dpDischargeDate.SelectedDate.Value;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        // 1. Перенос пациента в таблицу Patients
                        string insertQuery = @"
                            INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, DischargeDate)
                            VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @DischargeDate);
                            SELECT SCOPE_IDENTITY();";
                        int newPatientInsertedID;
                        using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con, tran))
                        {
                            cmdInsert.Parameters.AddWithValue("@FullName", fullName);
                            cmdInsert.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                            cmdInsert.Parameters.AddWithValue("@Gender", gender);
                            cmdInsert.Parameters.AddWithValue("@RecordDate", recordDate);
                            cmdInsert.Parameters.AddWithValue("@DischargeDate", dischargeDate);
                            newPatientInsertedID = Convert.ToInt32(cmdInsert.ExecuteScalar());
                        }

                        // 2. Связываем пациента с выбранным врачом (PatientDoctorAssignments)
                        string assignQuery = "INSERT INTO PatientDoctorAssignments (PatientID, DoctorID) VALUES (@PatientID, @DoctorID)";
                        using (SqlCommand cmdAssign = new SqlCommand(assignQuery, con, tran))
                        {
                            cmdAssign.Parameters.AddWithValue("@PatientID", newPatientInsertedID);
                            cmdAssign.Parameters.AddWithValue("@DoctorID", doctorID);
                            cmdAssign.ExecuteNonQuery();
                        }

                        // 3. Удаляем связанные записи из "NewPatient*" таблиц (чтобы не было конфликта)
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

                        // 4. Удаляем запись из NewPatients
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
                    // Открываем транзакцию для группового удаления
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        foreach (var selectedItem in dgPatients.SelectedItems)
                        {
                            if (selectedItem is DataRowView row)
                            {
                                int patientID = Convert.ToInt32(row["PatientID"]);

                                // 1. Удаляем связанные записи из ProcedureAppointments
                                string deleteAppointments = @"
                            DELETE FROM ProcedureAppointments
                            WHERE PatientID = @PatientID
                        ";
                                using (SqlCommand cmdApp = new SqlCommand(deleteAppointments, con, tran))
                                {
                                    cmdApp.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdApp.ExecuteNonQuery();
                                }

                                // 2. Удаляем связанные записи из PatientDescriptions
                                string deleteDescriptions = @"
                            DELETE FROM PatientDescriptions
                            WHERE PatientID = @PatientID
                        ";
                                using (SqlCommand cmdDesc = new SqlCommand(deleteDescriptions, con, tran))
                                {
                                    cmdDesc.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdDesc.ExecuteNonQuery();
                                }

                                // 3. Удаляем связи в PatientDoctorAssignments
                                string deleteAssignments = @"
                            DELETE FROM PatientDoctorAssignments
                            WHERE PatientID = @PatientID
                        ";
                                using (SqlCommand cmdAssign = new SqlCommand(deleteAssignments, con, tran))
                                {
                                    cmdAssign.Parameters.AddWithValue("@PatientID", patientID);
                                    cmdAssign.ExecuteNonQuery();
                                }

                                // 4. Удаляем самого пациента из Patients
                                string deletePatient = @"
                            DELETE FROM Patients
                            WHERE PatientID = @PatientID
                        ";
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
                LoadPatients(); // Обновляем список пациентов
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении пациентов: " + ex.Message);
            }
        }

        // Кнопка для назначения (нескольких) врачей уже существующему пациенту
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
                    // Открываем транзакцию для группового удаления
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        foreach (var selectedItem in dgDoctors.SelectedItems)
                        {
                            if (selectedItem is DataRowView row)
                            {
                                int doctorID = Convert.ToInt32(row["DoctorID"]);

                                // 1. Удаляем связанные записи из ProcedureAppointments
                                string deleteAppointments = @"
                            DELETE FROM ProcedureAppointments
                            WHERE DoctorID = @DoctorID
                        ";
                                using (SqlCommand cmdApp = new SqlCommand(deleteAppointments, con, tran))
                                {
                                    cmdApp.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdApp.ExecuteNonQuery();
                                }

                                // 2. Удаляем связанные записи из PatientDescriptions
                                string deleteDescriptions = @"
                            DELETE FROM PatientDescriptions
                            WHERE DoctorID = @DoctorID
                        ";
                                using (SqlCommand cmdDesc = new SqlCommand(deleteDescriptions, con, tran))
                                {
                                    cmdDesc.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdDesc.ExecuteNonQuery();
                                }

                                // 3. Удаляем связи в PatientDoctorAssignments
                                string deleteAssignments = @"
                            DELETE FROM PatientDoctorAssignments
                            WHERE DoctorID = @DoctorID
                        ";
                                using (SqlCommand cmdAssign = new SqlCommand(deleteAssignments, con, tran))
                                {
                                    cmdAssign.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdAssign.ExecuteNonQuery();
                                }

                                // 4. Удаляем записи из DoctorDiagnoses (если используется)
                                string deleteDoctorDiagnoses = @"
                            DELETE FROM DoctorDiagnoses
                            WHERE DoctorID = @DoctorID
                        ";
                                using (SqlCommand cmdDocDiag = new SqlCommand(deleteDoctorDiagnoses, con, tran))
                                {
                                    cmdDocDiag.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdDocDiag.ExecuteNonQuery();
                                }

                                // 5. Удаляем связанные процедуры из Procedures
                                //    (если нужно полностью убрать все процедуры этого врача)
                                string deleteProcedures = @"
                            DELETE FROM Procedures
                            WHERE DoctorID = @DoctorID
                        ";
                                using (SqlCommand cmdProcs = new SqlCommand(deleteProcedures, con, tran))
                                {
                                    cmdProcs.Parameters.AddWithValue("@DoctorID", doctorID);
                                    cmdProcs.ExecuteNonQuery();
                                }

                                // 6. Удаляем врача из Doctors
                                string deleteDoctor = @"
                            DELETE FROM Doctors
                            WHERE DoctorID = @DoctorID
                        ";
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
                LoadDoctors(); // Обновляем список врачей
                LoadDoctorsForComboBox(); // Если нужно обновить ComboBox
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
