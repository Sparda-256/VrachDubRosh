using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;  // Для Geometry и цветов
using System.Windows.Shapes; // Для Path
using System.Windows.Media.Animation; // Для анимаций, если понадобятся

namespace VrachDubRosh
{
    public partial class WeeklyScheduleWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _doctorID; // Идентификатор врача
        private DataTable dtSchedules; // Таблица для хранения расписаний
        private int _scheduleID = 0; // ID расписания для редактирования, 0 = новое

        public WeeklyScheduleWindow(int doctorID)
        {
            InitializeComponent();
            _doctorID = doctorID;
            Loaded += WeeklyScheduleWindow_Loaded;
        }

        private void WeeklyScheduleWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Устанавливаем начальные значения для дат
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = DateTime.Today.AddMonths(1);

            // Загружаем пациентов и процедуры
            LoadPatients();
            LoadProcedures();
            
            // Загружаем существующие расписания
            LoadSchedules();

            // Устанавливаем подсказки для полей времени
            tbMondayTime.Tag = "Время для понедельника (ЧЧ:ММ)";
            tbTuesdayTime.Tag = "Время для вторника (ЧЧ:ММ)";
            tbWednesdayTime.Tag = "Время для среды (ЧЧ:ММ)";
            tbThursdayTime.Tag = "Время для четверга (ЧЧ:ММ)";
            tbFridayTime.Tag = "Время для пятницы (ЧЧ:ММ)";
            tbSaturdayTime.Tag = "Время для субботы (ЧЧ:ММ)";
            tbSundayTime.Tag = "Время для воскресенья (ЧЧ:ММ)";
        }

        private void LoadPatients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT p.PatientID, p.FullName 
                                     FROM Patients p
                                     INNER JOIN PatientDoctorAssignments pda ON p.PatientID = pda.PatientID
                                     WHERE pda.DoctorID = @DoctorID
                                     AND (p.DischargeDate IS NULL OR p.DischargeDate > GETDATE())";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cbPatients.ItemsSource = dt.DefaultView;
                    cbPatients.SelectedValuePath = "PatientID";
                    
                    if (dt.Rows.Count > 0)
                        cbPatients.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пациентов: " + ex.Message);
            }
        }

        private void LoadProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT ProcedureID, 
                               CONCAT(ProcedureName, ' - ', Duration, ' мин.') AS DisplayText
                        FROM Procedures 
                        WHERE DoctorID = @DoctorID";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    cbProcedures.ItemsSource = dt.DefaultView;
                    cbProcedures.SelectedValuePath = "ProcedureID";
                    
                    if (dt.Rows.Count > 0)
                        cbProcedures.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур: " + ex.Message);
            }
        }

        private void LoadSchedules()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT ws.ScheduleID, ws.PatientID, ws.ProcedureID, ws.DayOfWeek, 
                               ws.AppointmentTime, ws.StartDate, ws.EndDate, ws.IsActive,
                               p.FullName AS PatientName, pr.ProcedureName,
                               CASE 
                                   WHEN ws.DayOfWeek = 1 THEN 'Понедельник'
                                   WHEN ws.DayOfWeek = 2 THEN 'Вторник'
                                   WHEN ws.DayOfWeek = 3 THEN 'Среда'
                                   WHEN ws.DayOfWeek = 4 THEN 'Четверг'
                                   WHEN ws.DayOfWeek = 5 THEN 'Пятница'
                                   WHEN ws.DayOfWeek = 6 THEN 'Суббота'
                                   WHEN ws.DayOfWeek = 7 THEN 'Воскресенье'
                               END AS DayOfWeekName,
                               CONVERT(VARCHAR(5), ws.AppointmentTime, 108) AS AppointmentTimeStr
                        FROM WeeklyScheduleAppointments ws
                        INNER JOIN Patients p ON ws.PatientID = p.PatientID
                        INNER JOIN Procedures pr ON ws.ProcedureID = pr.ProcedureID
                        WHERE ws.DoctorID = @DoctorID
                        ORDER BY ws.IsActive DESC, p.FullName, ws.DayOfWeek";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", _doctorID);
                    dtSchedules = new DataTable();
                    da.Fill(dtSchedules);
                    dgSchedules.ItemsSource = dtSchedules.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки расписаний: " + ex.Message);
            }
        }

        private void btnAddSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    SqlTransaction transaction = con.BeginTransaction();

                    try
                    {
                        List<int> selectedDays = GetSelectedDays();
                        foreach (int dayOfWeek in selectedDays)
                        {
                            string timeValue = GetTimeForDay(dayOfWeek);
                            if (string.IsNullOrWhiteSpace(timeValue))
                                continue;

                            if (!TimeSpan.TryParse(timeValue, out TimeSpan appointmentTime))
                            {
                                MessageBox.Show($"Неверный формат времени для дня {GetDayName(dayOfWeek)}: {timeValue}. Используйте формат ЧЧ:ММ.");
                                transaction.Rollback();
                                return;
                            }

                            string insertQuery = @"
                                INSERT INTO WeeklyScheduleAppointments 
                                (PatientID, DoctorID, ProcedureID, DayOfWeek, AppointmentTime, StartDate, EndDate, IsActive)
                                VALUES 
                                (@PatientID, @DoctorID, @ProcedureID, @DayOfWeek, @AppointmentTime, @StartDate, @EndDate, 1)";

                            using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", cbPatients.SelectedValue);
                                cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                                cmd.Parameters.AddWithValue("@ProcedureID", cbProcedures.SelectedValue);
                                cmd.Parameters.AddWithValue("@DayOfWeek", dayOfWeek);
                                cmd.Parameters.AddWithValue("@AppointmentTime", appointmentTime);
                                cmd.Parameters.AddWithValue("@StartDate", dpStartDate.SelectedDate.Value);
                                cmd.Parameters.AddWithValue("@EndDate", dpEndDate.SelectedDate.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        MessageBox.Show("Недельный график создан успешно.");
                        ClearForm();
                        LoadSchedules();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании недельного графика: " + ex.Message);
            }
        }

        private List<int> GetSelectedDays()
        {
            List<int> selectedDays = new List<int>();
            if (chkMonday.IsChecked == true) selectedDays.Add(1);
            if (chkTuesday.IsChecked == true) selectedDays.Add(2);
            if (chkWednesday.IsChecked == true) selectedDays.Add(3);
            if (chkThursday.IsChecked == true) selectedDays.Add(4);
            if (chkFriday.IsChecked == true) selectedDays.Add(5);
            if (chkSaturday.IsChecked == true) selectedDays.Add(6);
            if (chkSunday.IsChecked == true) selectedDays.Add(7);
            return selectedDays;
        }

        private string GetTimeForDay(int dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case 1: return tbMondayTime.Text.Trim();
                case 2: return tbTuesdayTime.Text.Trim();
                case 3: return tbWednesdayTime.Text.Trim();
                case 4: return tbThursdayTime.Text.Trim();
                case 5: return tbFridayTime.Text.Trim();
                case 6: return tbSaturdayTime.Text.Trim();
                case 7: return tbSundayTime.Text.Trim();
                default: return string.Empty;
            }
        }

        private string GetDayName(int dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case 1: return "Понедельник";
                case 2: return "Вторник";
                case 3: return "Среда";
                case 4: return "Четверг";
                case 5: return "Пятница";
                case 6: return "Суббота";
                case 7: return "Воскресенье";
                default: return string.Empty;
            }
        }

        private void SetTimeForDay(int dayOfWeek, string time)
        {
            switch (dayOfWeek)
            {
                case 1: tbMondayTime.Text = time; break;
                case 2: tbTuesdayTime.Text = time; break;
                case 3: tbWednesdayTime.Text = time; break;
                case 4: tbThursdayTime.Text = time; break;
                case 5: tbFridayTime.Text = time; break;
                case 6: tbSaturdayTime.Text = time; break;
                case 7: tbSundayTime.Text = time; break;
            }
        }

        private void SetDayChecked(int dayOfWeek, bool isChecked)
        {
            switch (dayOfWeek)
            {
                case 1: chkMonday.IsChecked = isChecked; break;
                case 2: chkTuesday.IsChecked = isChecked; break;
                case 3: chkWednesday.IsChecked = isChecked; break;
                case 4: chkThursday.IsChecked = isChecked; break;
                case 5: chkFriday.IsChecked = isChecked; break;
                case 6: chkSaturday.IsChecked = isChecked; break;
                case 7: chkSunday.IsChecked = isChecked; break;
            }
        }

        private bool ValidateInput()
        {
            if (cbPatients.SelectedValue == null)
            {
                MessageBox.Show("Выберите пациента.");
                return false;
            }

            if (cbProcedures.SelectedValue == null)
            {
                MessageBox.Show("Выберите процедуру.");
                return false;
            }

            if (dpStartDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату начала графика.");
                return false;
            }

            if (dpEndDate.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату окончания графика.");
                return false;
            }

            if (dpStartDate.SelectedDate > dpEndDate.SelectedDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания.");
                return false;
            }

            List<int> selectedDays = GetSelectedDays();
            if (selectedDays.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один день недели.");
                return false;
            }

            // Проверяем, что для каждого выбранного дня указано время
            bool hasTimeForSelectedDays = false;
            foreach (int day in selectedDays)
            {
                string time = GetTimeForDay(day);
                if (!string.IsNullOrWhiteSpace(time))
                {
                    hasTimeForSelectedDays = true;
                    // Проверяем формат времени
                    if (!TimeSpan.TryParse(time, out _))
                    {
                        MessageBox.Show($"Неверный формат времени для дня {GetDayName(day)}: {time}. Используйте формат ЧЧ:ММ.");
                        return false;
                    }
                }
            }

            if (!hasTimeForSelectedDays)
            {
                MessageBox.Show("Укажите время хотя бы для одного выбранного дня недели.");
                return false;
            }

            return true;
        }

        private void ClearForm()
        {
            cbPatients.SelectedIndex = 0;
            cbProcedures.SelectedIndex = 0;
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = DateTime.Today.AddMonths(1);
            
            chkMonday.IsChecked = false;
            chkTuesday.IsChecked = false;
            chkWednesday.IsChecked = false;
            chkThursday.IsChecked = false;
            chkFriday.IsChecked = false;
            chkSaturday.IsChecked = false;
            chkSunday.IsChecked = false;
            
            tbMondayTime.Text = string.Empty;
            tbTuesdayTime.Text = string.Empty;
            tbWednesdayTime.Text = string.Empty;
            tbThursdayTime.Text = string.Empty;
            tbFridayTime.Text = string.Empty;
            tbSaturdayTime.Text = string.Empty;
            tbSundayTime.Text = string.Empty;

            _scheduleID = 0;
            
            // Создаем StackPanel с иконкой и текстом для кнопки создания
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            
            Path p = new Path();
            p.Data = Geometry.Parse("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z");
            p.Fill = System.Windows.Media.Brushes.White;
            p.Width = 18;
            p.Height = 18;
            p.Stretch = Stretch.Uniform;
            
            TextBlock tb = new TextBlock();
            tb.Text = "Создать график";
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Foreground = System.Windows.Media.Brushes.White;
            tb.Margin = new Thickness(5, 0, 0, 0);
            
            sp.Children.Add(p);
            sp.Children.Add(tb);
            
            btnAddSchedule.Content = sp;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSchedules();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnEditSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (dgSchedules.SelectedItem == null)
            {
                MessageBox.Show("Выберите расписание для редактирования.");
                return;
            }

            DataRowView row = dgSchedules.SelectedItem as DataRowView;
            LoadScheduleForEdit(row);
        }

        private void dgSchedules_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgSchedules.SelectedItem is DataRowView row)
            {
                LoadScheduleForEdit(row);
            }
        }

        private void LoadScheduleForEdit(DataRowView row)
        {
            _scheduleID = Convert.ToInt32(row["ScheduleID"]);
            int patientID = Convert.ToInt32(row["PatientID"]);
            int procedureID = Convert.ToInt32(row["ProcedureID"]);
            int dayOfWeek = Convert.ToInt32(row["DayOfWeek"]);
            DateTime startDate = Convert.ToDateTime(row["StartDate"]);
            DateTime endDate = Convert.ToDateTime(row["EndDate"]);
            TimeSpan appointmentTime = (TimeSpan)row["AppointmentTime"];
            string timeStr = appointmentTime.ToString(@"hh\:mm");

            // Устанавливаем значения в форме
            cbPatients.SelectedValue = patientID;
            cbProcedures.SelectedValue = procedureID;
            dpStartDate.SelectedDate = startDate;
            dpEndDate.SelectedDate = endDate;

            // Очищаем все чекбоксы и поля времени
            chkMonday.IsChecked = false;
            chkTuesday.IsChecked = false;
            chkWednesday.IsChecked = false;
            chkThursday.IsChecked = false;
            chkFriday.IsChecked = false;
            chkSaturday.IsChecked = false;
            chkSunday.IsChecked = false;
            
            tbMondayTime.Text = string.Empty;
            tbTuesdayTime.Text = string.Empty;
            tbWednesdayTime.Text = string.Empty;
            tbThursdayTime.Text = string.Empty;
            tbFridayTime.Text = string.Empty;
            tbSaturdayTime.Text = string.Empty;
            tbSundayTime.Text = string.Empty;

            // Устанавливаем день недели и время
            SetDayChecked(dayOfWeek, true);
            SetTimeForDay(dayOfWeek, timeStr);

            // Создаем StackPanel с иконкой и текстом для кнопки обновления
            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;
            
            Path p = new Path();
            p.Data = Geometry.Parse("M17.65,6.35C16.2,4.9 14.21,4 12,4A8,8 0 0,0 4,12A8,8 0 0,0 12,20C15.73,20 18.84,17.45 19.73,14H17.65C16.83,16.33 14.61,18 12,18A6,6 0 0,1 6,12A6,6 0 0,1 12,6C13.66,6 15.14,6.69 16.22,7.78L13,11H20V4L17.65,6.35Z");
            p.Fill = System.Windows.Media.Brushes.White;
            p.Width = 18;
            p.Height = 18;
            p.Stretch = Stretch.Uniform;
            
            TextBlock tb = new TextBlock();
            tb.Text = "Обновить график";
            tb.VerticalAlignment = VerticalAlignment.Center;
            tb.Foreground = System.Windows.Media.Brushes.White;
            tb.Margin = new Thickness(5, 0, 0, 0);
            
            sp.Children.Add(p);
            sp.Children.Add(tb);
            
            btnAddSchedule.Content = sp;
        }

        private void btnDeleteSchedule_Click(object sender, RoutedEventArgs e)
        {
            if (dgSchedules.SelectedItem == null)
            {
                MessageBox.Show("Выберите расписание для удаления.");
                return;
            }

            DataRowView row = dgSchedules.SelectedItem as DataRowView;
            int scheduleID = Convert.ToInt32(row["ScheduleID"]);
            string patientName = row["PatientName"].ToString();
            string dayOfWeek = row["DayOfWeekName"].ToString();

            if (MessageBox.Show($"Вы действительно хотите удалить расписание для пациента {patientName} на {dayOfWeek}?\n\nЭто также отменит все будущие назначения по этому расписанию.", 
                                "Подтверждение удаления", 
                                MessageBoxButton.YesNo, 
                                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        using (SqlTransaction transaction = con.BeginTransaction())
                        {
                            try
                            {
                                // 1. Сначала отменяем будущие назначения
                                string updateAppointmentsQuery = @"
                                    UPDATE pa
                                    SET pa.Status = 'Отменена'
                                    FROM ProcedureAppointments pa
                                    JOIN ScheduleGeneratedAppointments sga ON pa.AppointmentID = sga.AppointmentID
                                    WHERE sga.ScheduleID = @ScheduleID
                                    AND pa.AppointmentDateTime > GETDATE()
                                    AND pa.Status = 'Назначена'";

                                using (SqlCommand cmd = new SqlCommand(updateAppointmentsQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@ScheduleID", scheduleID);
                                    cmd.ExecuteNonQuery();
                                }

                                // 2. Удаляем записи из связанной таблицы ScheduleGeneratedAppointments
                                string deleteLinksQuery = "DELETE FROM ScheduleGeneratedAppointments WHERE ScheduleID = @ScheduleID";
                                using (SqlCommand cmd = new SqlCommand(deleteLinksQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@ScheduleID", scheduleID);
                                    cmd.ExecuteNonQuery();
                                }

                                // 3. Наконец, удаляем само расписание
                                string deleteScheduleQuery = "DELETE FROM WeeklyScheduleAppointments WHERE ScheduleID = @ScheduleID";
                                using (SqlCommand cmd = new SqlCommand(deleteScheduleQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@ScheduleID", scheduleID);
                                    cmd.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                MessageBox.Show("Расписание успешно удалено и будущие назначения отменены.");
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw new Exception("Ошибка при удалении расписания: " + ex.Message);
                            }
                        }
                        
                        // Обновляем отображение
                        LoadSchedules();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении расписания: " + ex.Message);
                }
            }
        }
    }
} 