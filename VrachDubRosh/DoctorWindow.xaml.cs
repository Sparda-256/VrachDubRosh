using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using VrachDubRosh;

namespace VrachDubRosh
{
    public partial class DoctorWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _doctorID; // Идентификатор врача, передается после авторизации

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
        private void LoadDoctorPatients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Выбираем пациентов, назначенных данному врачу
                    string query = @"SELECT p.PatientID, p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate 
                                     FROM Patients p
                                     INNER JOIN PatientDoctorAssignments pda ON p.PatientID = pda.PatientID
                                     WHERE pda.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgDoctorPatients.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пациентов: " + ex.Message);
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
        #endregion

        #region Вкладка "Назначение процедур"
        private void UpdateAppointmentsStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Обновляем статус на "Идёт", если процедура началась, но ещё не завершена
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

                    // Обновляем статус на "Завершена", если процедура уже завершена
                    string updateCompletedQuery = @"
                UPDATE pa
                SET pa.Status = 'Завершена'
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE DATEADD(minute, pr.Duration, pa.AppointmentDateTime) <= GETDATE() 
                  AND pa.Status = 'Идёт'";

                    using (SqlCommand cmd = new SqlCommand(updateCompletedQuery, con))
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

        private void btnAssignProcedure_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, что все необходимые поля заполнены
            if (cbPatients.SelectedValue == null || cbProcedures.SelectedValue == null || dpApptDate.SelectedDate == null || string.IsNullOrWhiteSpace(tbApptTime.Text))
            {
                MessageBox.Show("Пожалуйста, выберите пациента, процедуру, дату и введите время (ЧЧ:ММ).");
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

            // Проверка на назначение в прошлом
            if (appointmentDateTime < DateTime.Now)
            {
                MessageBox.Show("Нельзя назначить процедуру на прошедшую дату и время.");
                return;
            }

            // Получаем длительность процедуры (в минутах)
            int procedureDuration = 0;
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT Duration FROM Procedures WHERE ProcedureID = @ProcedureID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            procedureDuration = Convert.ToInt32(result);
                        }
                        else
                        {
                            MessageBox.Show("Не найдена процедура с таким идентификатором.");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка получения длительности процедуры: " + ex.Message);
                return;
            }

            // Проверка, чтобы время назначения плюс длительность не превышали текущий момент
            DateTime procedureEndTime = appointmentDateTime.AddMinutes(procedureDuration);
            if (procedureEndTime < DateTime.Now)
            {
                MessageBox.Show("Процедура не может быть назначена, так как её длительность выходит за пределы текущего времени.");
                return;
            }

            // Если все проверки пройдены, вставляем новое назначение
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string insertQuery = @"INSERT INTO ProcedureAppointments (PatientID, DoctorID, ProcedureID, AppointmentDateTime, Status)
                                   VALUES (@PatientID, @DoctorID, @ProcedureID, @ApptDateTime, 'Назначена')";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                        cmd.Parameters.AddWithValue("@ApptDateTime", appointmentDateTime);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Процедура успешно назначена.");
                UpdateAppointmentsStatus();
                LoadDoctorAppointments();
            }

            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при назначении процедуры: " + ex.Message);
            }
        }


        private void LoadDoctorAppointments()
        {
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
                        WHERE pa.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgAppointments.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки назначенных процедур: " + ex.Message);
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
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // Обновляем статус на "Идёт", если процедура началась, но ещё не завершена
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

                    // Обновляем статус на "Завершена", если процедура уже завершена
                    string updateCompletedQuery = @"
                UPDATE pa
                SET pa.Status = 'Завершена'
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE DATEADD(minute, pr.Duration, pa.AppointmentDateTime) <= GETDATE() 
                  AND pa.Status = 'Идёт'";

                    using (SqlCommand cmd = new SqlCommand(updateCompletedQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                UpdateAppointmentsStatus();  // Дополнительное обновление статусов после выполнения
                LoadDoctorAppointments();  // Обновляем список назначений
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обновлении статусов: " + ex.Message);
            }
        }
        #endregion

        #region Вкладка "Процедуры"

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
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgProcedures.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур: " + ex.Message);
            }
        }

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
        #endregion
    }
}
