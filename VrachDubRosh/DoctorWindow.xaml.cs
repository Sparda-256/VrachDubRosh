﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VrachDubRosh;

namespace VrachDubRosh
{
    public partial class DoctorWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _doctorID; // Идентификатор врача, передается после авторизации

        // Добавляем поля для кэширования данных
        private DataTable dtDoctorPatients;
        private DataTable dtAppointments;
        private DataTable dtProcedures;

        public DoctorWindow(int doctorID)
        {
            InitializeComponent();
            _doctorID = doctorID;
            Loaded += DoctorWindow_Loaded;
        }

        private void DoctorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAppointmentsStatus();
            LoadDoctorPatients();
            LoadDoctorProcedures();
            LoadDoctorAppointments();
            LoadPatientsForAssignment();
            LoadProceduresForAssignment();
            UpdateAppointmentsStatus();
        }

        #region Вкладка "Пациенты"
        // Обработчики поиска
        private void txtSearchPatients_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtDoctorPatients != null)
            {
                string filter = txtSearchPatients.Text.Trim().Replace("'", "''");
                dtDoctorPatients.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                    ? ""
                    : $"FullName LIKE '%{filter}%'";
            }
        }

        private void txtSearchAppointments_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtAppointments != null)
            {
                string filter = txtSearchAppointments.Text.Trim().Replace("'", "''");
                dtAppointments.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                    ? ""
                    : $"PatientName LIKE '%{filter}%' OR ProcedureName LIKE '%{filter}%'";
            }
        }

        private void txtSearchProcedures_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtProcedures != null)
            {
                string filter = txtSearchProcedures.Text.Trim().Replace("'", "''");
                dtProcedures.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                    ? ""
                    : $"ProcedureName LIKE '%{filter}%'";
            }
        }

        // Обновляем методы загрузки данных
        private void LoadDoctorPatients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT p.PatientID, p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate 
                               FROM Patients p
                               INNER JOIN PatientDoctorAssignments pda ON p.PatientID = pda.PatientID
                               WHERE pda.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    dtDoctorPatients = new DataTable(); // Используем поле класса
                    da.Fill(dtDoctorPatients);
                    dgDoctorPatients.ItemsSource = dtDoctorPatients.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пациентов: " + ex.Message);
            }
        }

        private void LoadDoctorAppointments()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pa.AppointmentID, p.FullName AS PatientName, 
                                        pr.ProcedureName, pa.AppointmentDateTime, 
                                        pr.Duration, pa.Status
                                 FROM ProcedureAppointments pa
                                 INNER JOIN Patients p ON pa.PatientID = p.PatientID
                                 INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                                 WHERE pa.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    dtAppointments = new DataTable(); // Используем поле класса
                    da.Fill(dtAppointments);
                    dgAppointments.ItemsSource = dtAppointments.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки назначенных процедур: " + ex.Message);
            }
        }

        private void LoadDoctorProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ProcedureID, ProcedureName, Duration FROM Procedures WHERE DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    dtProcedures = new DataTable(); // Используем поле класса
                    da.Fill(dtProcedures);
                    dgProcedures.ItemsSource = dtProcedures.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур: " + ex.Message);
            }
        }

        private void btnEditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (dgDoctorPatients.SelectedItem == null)
            {
                MessageBox.Show("Выберите пациента для редактирования.");
                return;
            }
            DataRowView row = dgDoctorPatients.SelectedItem as DataRowView;
            int patientID = Convert.ToInt32(row["PatientID"]);
            // Открываем окно редактирования пациента (AddEditPatientWindow)
            AddEditPatientWindow editPatientWindow = new AddEditPatientWindow(patientID);
            editPatientWindow.Owner = this;
            if (editPatientWindow.ShowDialog() == true)
            {
                LoadDoctorPatients();
            }
        }
        // Открытие окна редактирования пациента по двойному щелчку
        private void dgDoctorPatients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgDoctorPatients.SelectedItem is DataRowView row)
            {
                int patientID = Convert.ToInt32(row["PatientID"]);
                AddEditPatientWindow editPatientWindow = new AddEditPatientWindow(patientID);
                editPatientWindow.Owner = this;
                if (editPatientWindow.ShowDialog() == true)
                {
                    LoadDoctorPatients();
                }
            }
        }
        #endregion

        #region Вкладка "Назначение процедур"
        private void UpdateAppointmentsStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 1. Сначала помечаем завершенные процедуры (независимо от текущего статуса)
                    string updateCompletedQuery = @"
                UPDATE pa
                SET pa.Status = 'Завершена'
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE DATEADD(minute, pr.Duration, pa.AppointmentDateTime) <= GETDATE() 
                  AND pa.Status IN ('Назначена', 'Идёт')"; // Обрабатываем оба статуса

                    using (SqlCommand cmd = new SqlCommand(updateCompletedQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Затем помечаем процедуры, которые идут сейчас
                    string updateInProgressQuery = @"
                UPDATE pa
                SET pa.Status = 'Идёт'
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE pa.AppointmentDateTime <= GETDATE() 
                  AND DATEADD(minute, pr.Duration, pa.AppointmentDateTime) > GETDATE() 
                  AND pa.Status = 'Назначена'";

                    using (SqlCommand cmd = new SqlCommand(updateInProgressQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении статуса: " + ex.Message);
            }
        }


        private void LoadPatientsForAssignment()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT p.PatientID, p.FullName 
                                     FROM Patients p
                                     INNER JOIN PatientDoctorAssignments pda ON p.PatientID = pda.PatientID
                                     WHERE pda.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cbPatients.ItemsSource = dt.DefaultView;
                    cbPatients.DisplayMemberPath = "FullName";
                    cbPatients.SelectedValuePath = "PatientID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пациентов для назначения: " + ex.Message);
            }
        }

        private void LoadProceduresForAssignment()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ProcedureID, ProcedureName FROM Procedures WHERE DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cbProcedures.ItemsSource = dt.DefaultView;
                    cbProcedures.DisplayMemberPath = "ProcedureName";
                    cbProcedures.SelectedValuePath = "ProcedureID";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур для назначения: " + ex.Message);
            }
        }

        // Метод для добавления процедуры
        private void btnAssignProcedure_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что все необходимые данные были введены
            if (cbPatients.SelectedValue == null || cbProcedures.SelectedValue == null || dpApptDate.SelectedDate == null || string.IsNullOrWhiteSpace(tbApptTime.Text))
            {
                MessageBox.Show("Пожалуйста, выберите пациента, процедуру, дату и время.");
                return;
            }

            int patientID = Convert.ToInt32(cbPatients.SelectedValue);
            int procedureID = Convert.ToInt32(cbProcedures.SelectedValue);
            DateTime selectedDate = dpApptDate.SelectedDate.Value;

            // Парсим время из TextBox
            if (!TimeSpan.TryParse(tbApptTime.Text, out TimeSpan timeOfDay))
            {
                MessageBox.Show("Неверный формат времени. Используйте формат ЧЧ:ММ.");
                return;
            }

            DateTime appointmentDateTime = selectedDate.Date + timeOfDay;

            // 1. Проверка на выписку пациента
            if (IsPatientDischarged(patientID, appointmentDateTime))
            {
                MessageBox.Show("Пациент не может быть записан на эту процедуру, так как он уже будет выписан к указанной дате.");
                return;
            }

            // 2. Проверка занятости врача
            if (IsDoctorOccupied(appointmentDateTime, procedureID))
            {
                MessageBox.Show("Пересечение с другим назначением.");
                return;
            }

            // 3. Проверка занятости пациента
            if (IsPatientOccupied(patientID, appointmentDateTime, procedureID))
            {
                MessageBox.Show("Пациент уже записан на другую процедуру в это время.");
                return;
            }

            if (appointmentDateTime <= DateTime.Now)
            {
                MessageBox.Show("Нельзя назначать процедуры на прошедшие дату и время.");
                return;
            }

            // Если все проверки прошли, записываем пациента на процедуру
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string insertQuery = @"INSERT INTO ProcedureAppointments (PatientID, DoctorID, ProcedureID, AppointmentDateTime, Status)
                                   VALUES (@PatientID, @DoctorID, @ProcedureID, @AppointmentDateTime, 'Назначена')";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                        cmd.Parameters.AddWithValue("@AppointmentDateTime", appointmentDateTime);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Процедура успешно назначена.");
                LoadDoctorProcedures();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при назначении процедуры: " + ex.Message);
            }
        }

        private bool IsPatientDischarged(int patientID, DateTime appointmentDateTime)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DischargeDate FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        object result = cmd.ExecuteScalar();

                        // Если дата выписки не установлена - пациент не выписан
                        if (result == DBNull.Value || result == null) return false;

                        DateTime dischargeDate = Convert.ToDateTime(result);
                        return dischargeDate <= appointmentDateTime.Date;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при проверке выписки пациента: " + ex.Message);
                return true;
            }
        }

        private bool IsDoctorOccupied(DateTime appointmentDateTime, int procedureID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Получаем длительность процедуры
                    int duration = 0;
                    string durationQuery = "SELECT Duration FROM Procedures WHERE ProcedureID = @ProcedureID";
                    using (SqlCommand cmd = new SqlCommand(durationQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                        duration = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    DateTime endTime = appointmentDateTime.AddMinutes(duration);

                    string query = @"
                SELECT COUNT(*) 
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE 
                    pa.DoctorID = @DoctorID AND
                    pa.Status != 'Отменена' AND
                    (
                        (pa.AppointmentDateTime < @EndTime AND 
                        DATEADD(MINUTE, pr.Duration, pa.AppointmentDateTime) > @StartTime)
                    )";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                        cmd.Parameters.AddWithValue("@StartTime", appointmentDateTime);
                        cmd.Parameters.AddWithValue("@EndTime", endTime);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при проверке занятости врача: " + ex.Message);
                return true;
            }
        }

        private bool IsPatientOccupied(int patientID, DateTime appointmentDateTime, int procedureID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    int duration = 0;
                    string durationQuery = "SELECT Duration FROM Procedures WHERE ProcedureID = @ProcedureID";
                    using (SqlCommand cmd = new SqlCommand(durationQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                        duration = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    DateTime endTime = appointmentDateTime.AddMinutes(duration);

                    string query = @"
                SELECT COUNT(*) 
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE 
                    pa.PatientID = @PatientID AND
                    pa.Status != 'Отменена' AND
                    (
                        (pa.AppointmentDateTime < @EndTime AND 
                        DATEADD(MINUTE, pr.Duration, pa.AppointmentDateTime) > @StartTime)
                    )";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        cmd.Parameters.AddWithValue("@StartTime", appointmentDateTime);
                        cmd.Parameters.AddWithValue("@EndTime", endTime);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при проверке занятости пациента: " + ex.Message);
                return true;
            }
        }

        private void btnCancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (dgAppointments.SelectedItem == null)
            {
                MessageBox.Show("Выберите назначение для отмены.");
                return;
            }

            DataRowView row = dgAppointments.SelectedItem as DataRowView;
            int appointmentID = Convert.ToInt32(row["AppointmentID"]);
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string updateQuery = "UPDATE ProcedureAppointments SET Status = 'Отменена' WHERE AppointmentID = @AppointmentID";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@AppointmentID", appointmentID);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Назначение отменено.");
                UpdateAppointmentsStatus();
                LoadDoctorAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при отмене назначения: " + ex.Message);
            }
        }

        private void btnFilterAppointments_Click(object sender, RoutedEventArgs e)
        {
            // Фильтрация назначений по статусу
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT pa.AppointmentID, p.FullName AS PatientName, pr.ProcedureName, pa.AppointmentDateTime, pr.Duration, pa.Status
                        FROM ProcedureAppointments pa
                        INNER JOIN Patients p ON pa.PatientID = p.PatientID
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        WHERE pa.DoctorID = @DoctorID AND pa.Status NOT IN ('Завершена', 'Отменена')";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgAppointments.ItemsSource = dt.DefaultView;
                }
                UpdateAppointmentsStatus();
            }

            catch (Exception ex)
            {
                MessageBox.Show("Ошибка фильтрации назначений: " + ex.Message);
            }
        }
        private void btnUpdateAppointmentsStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateAppointmentsStatus();
                LoadDoctorAppointments(); // Перезагружаем данные для отображения изменений
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении статусов: " + ex.Message);
            }
        }
        #endregion

        #region Вкладка "Процедуры"


        private void btnAddProcedure_Click(object sender, RoutedEventArgs e)
        {
            AddEditProcedureWindow addProcedureWindow = new AddEditProcedureWindow(_doctorID);
            addProcedureWindow.Owner = this;
            if (addProcedureWindow.ShowDialog() == true)
            {
                LoadDoctorProcedures();
                LoadProceduresForAssignment();
            }
        }

        private void btnEditProcedure_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcedures.SelectedItem == null)
            {
                MessageBox.Show("Выберите процедуру для редактирования.");
                return;
            }
            DataRowView row = dgProcedures.SelectedItem as DataRowView;
            int procedureID = Convert.ToInt32(row["ProcedureID"]);
            AddEditProcedureWindow editProcedureWindow = new AddEditProcedureWindow(_doctorID, procedureID);
            editProcedureWindow.Owner = this;
            if (editProcedureWindow.ShowDialog() == true)
            {
                LoadDoctorProcedures();
                LoadProceduresForAssignment();
            }
        }

        private void btnDeleteProcedure_Click(object sender, RoutedEventArgs e)
        {
            if (dgProcedures.SelectedItem == null)
            {
                MessageBox.Show("Выберите процедуру для удаления.");
                return;
            }
            DataRowView row = dgProcedures.SelectedItem as DataRowView;
            int procedureID = Convert.ToInt32(row["ProcedureID"]);
            if (MessageBox.Show("Вы уверены, что хотите удалить эту процедуру?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string deleteQuery = "DELETE FROM Procedures WHERE ProcedureID = @ProcedureID";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Процедура удалена.");
                    LoadDoctorProcedures();
                    LoadProceduresForAssignment();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении процедуры: " + ex.Message);
                }
            }
        }
        // Открытие окна редактирования процедуры по двойному щелчку
        private void dgProcedures_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProcedures.SelectedItem is DataRowView row)
            {
                int procedureID = Convert.ToInt32(row["ProcedureID"]);
                AddEditProcedureWindow editProcedureWindow = new AddEditProcedureWindow(_doctorID, procedureID);
                editProcedureWindow.Owner = this;
                if (editProcedureWindow.ShowDialog() == true)
                {
                    LoadDoctorProcedures();
                    LoadProceduresForAssignment();
                }
            }
        }
        #endregion
    }
}