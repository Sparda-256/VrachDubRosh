using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VrachDubRosh
{
    public partial class MedCardWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;
        private DataRowView _selectedProcedure;
        private DataTable _patientDescriptions;
        private bool _isInitialLoad = true;

        public MedCardWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            tbPatientName.Text = $"Медицинская карточка пациента: {patientName}";
            
            LoadPatientInfo();
            LoadAllMedicalNotes();
            LoadProcedures();
            
            _isInitialLoad = false;
        }

        /// <summary>
        /// Загружает информацию о пациенте и отображает её в интерфейсе.
        /// </summary>
        private void LoadPatientInfo()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT DateOfBirth, Gender, RecordDate, DischargeDate 
                                     FROM Patients 
                                     WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tbDateOfBirth.Text = reader["DateOfBirth"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["DateOfBirth"]).ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                tbGender.Text = reader["Gender"] != DBNull.Value 
                                    ? reader["Gender"].ToString() 
                                    : "Не указан";
                                
                                tbRecordDate.Text = reader["RecordDate"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["RecordDate"]).ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                tbDischargeDate.Text = reader["DischargeDate"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["DischargeDate"]).ToString("dd.MM.yyyy") 
                                    : "Не указана";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки информации о пациенте: " + ex.Message);
            }
        }

        /// <summary>
        /// Загружает все медицинские заметки для данного пациента от всех врачей
        /// </summary>
        private void LoadAllMedicalNotes()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pd.PatientDescriptionID, pd.Description, pd.DescriptionDate, 
                                    d.DoctorID, d.FullName as DoctorName
                                    FROM PatientDescriptions pd
                                    LEFT JOIN Doctors d ON pd.DoctorID = d.DoctorID
                                    WHERE pd.PatientID = @PatientID
                                    ORDER BY pd.DescriptionDate DESC";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    _patientDescriptions = new DataTable();
                    da.Fill(_patientDescriptions);

                    if (_patientDescriptions.Rows.Count > 0)
                    {
                        // Заполняем комбобокс со списком врачей
                        LoadDoctorsComboBox();
                        
                        // Заполняем комбобокс с датами для выбранного врача
                        LoadDatesComboBox();
                        
                        // Показываем последнюю запись
                        DisplaySelectedNote();
                    }
                    else
                    {
                        txtMedicalNotes.Text = "";
                        cbDoctors.ItemsSource = null;
                        cbDates.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки медицинских заметок: " + ex.Message);
            }
        }

        /// <summary>
        /// Заполняет комбобокс со списком врачей, оставивших заметки
        /// </summary>
        private void LoadDoctorsComboBox()
        {
            var uniqueDoctors = new List<DoctorItem>();
            var doctorsSet = new HashSet<int?>();
            
            foreach (DataRow row in _patientDescriptions.Rows)
            {
                int? doctorID = row["DoctorID"] as int?;
                if (!doctorsSet.Contains(doctorID))
                {
                    string doctorName = row["DoctorName"] != DBNull.Value 
                        ? row["DoctorName"].ToString() 
                        : "Не указан";
                    
                    uniqueDoctors.Add(new DoctorItem { 
                        DoctorID = doctorID, 
                        DoctorName = doctorName 
                    });
                    
                    doctorsSet.Add(doctorID);
                }
            }
            
            // Добавляем опцию "Все врачи"
            uniqueDoctors.Insert(0, new DoctorItem { DoctorID = null, DoctorName = "Все врачи" });
            
            cbDoctors.DisplayMemberPath = "DoctorName";
            cbDoctors.SelectedValuePath = "DoctorID";
            cbDoctors.ItemsSource = uniqueDoctors;
            cbDoctors.SelectedIndex = 0; // Выбираем "Все врачи" по умолчанию
        }

        /// <summary>
        /// Заполняет комбобокс с датами для выбранного врача
        /// </summary>
        private void LoadDatesComboBox()
        {
            var uniqueDates = new List<DateItem>();
            var datesSet = new HashSet<DateTime>();
            
            // Получаем выбранного врача
            DoctorItem selectedDoctor = cbDoctors.SelectedItem as DoctorItem;
            
            if (selectedDoctor != null)
            {
                foreach (DataRow row in _patientDescriptions.Rows)
                {
                    // Если выбран конкретный врач, фильтруем по нему
                    if (selectedDoctor.DoctorID.HasValue && 
                        row["DoctorID"] != DBNull.Value && 
                        Convert.ToInt32(row["DoctorID"]) != selectedDoctor.DoctorID.Value)
                    {
                        continue;
                    }
                    
                    DateTime date = Convert.ToDateTime(row["DescriptionDate"]);
                    if (!datesSet.Contains(date))
                    {
                        uniqueDates.Add(new DateItem { 
                            Date = date,
                            DisplayText = date.ToString("dd.MM.yyyy HH:mm") 
                        });
                        datesSet.Add(date);
                    }
                }
                
                // Сортируем даты по убыванию (новые сверху)
                uniqueDates.Sort((a, b) => b.Date.CompareTo(a.Date));
                
                // Добавляем опцию "Все даты"
                uniqueDates.Insert(0, new DateItem { Date = DateTime.MinValue, DisplayText = "Все даты" });
                
                cbDates.DisplayMemberPath = "DisplayText";
                cbDates.SelectedValuePath = "Date";
                cbDates.ItemsSource = uniqueDates;
                cbDates.SelectedIndex = 0; // Выбираем "Все даты" по умолчанию
            }
        }

        /// <summary>
        /// Отображает выбранную или все заметки
        /// </summary>
        private void DisplaySelectedNote()
        {
            // Получаем выбранного врача и дату
            DoctorItem selectedDoctor = cbDoctors.SelectedItem as DoctorItem;
            DateItem selectedDate = cbDates.SelectedItem as DateItem;
            
            if (selectedDoctor == null || selectedDate == null)
            {
                txtMedicalNotes.Text = "";
                return;
            }
            
            // Формируем текст для отображения
            var noteText = new System.Text.StringBuilder();
            
            foreach (DataRow row in _patientDescriptions.Rows)
            {
                bool doctorMatch = !selectedDoctor.DoctorID.HasValue || 
                    (row["DoctorID"] != DBNull.Value && 
                     Convert.ToInt32(row["DoctorID"]) == selectedDoctor.DoctorID.Value);
                
                bool dateMatch = selectedDate.Date == DateTime.MinValue || 
                    Convert.ToDateTime(row["DescriptionDate"]).Date == selectedDate.Date.Date;
                
                if (doctorMatch && dateMatch)
                {
                    string doctorName = row["DoctorName"] != DBNull.Value 
                        ? row["DoctorName"].ToString() 
                        : "Не указан";
                    
                    DateTime date = Convert.ToDateTime(row["DescriptionDate"]);
                    string description = row["Description"].ToString();
                    
                    // Если выбраны все врачи или все даты, добавляем информацию о враче и дате
                    if (!selectedDoctor.DoctorID.HasValue || selectedDate.Date == DateTime.MinValue)
                    {
                        noteText.AppendLine($"[Врач: {doctorName}, Дата: {date:dd.MM.yyyy HH:mm}]");
                        noteText.AppendLine(description);
                        noteText.AppendLine(new string('-', 40));
                    }
                    else
                    {
                        // Для конкретного врача и даты показываем только текст заметки
                        noteText.AppendLine(description);
                    }
                }
            }
            
            txtMedicalNotes.Text = noteText.ToString().TrimEnd('-');
        }

        /// <summary>
        /// Обработчик события выбора врача
        /// </summary>
        private void cbDoctors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialLoad)
            {
                LoadDatesComboBox();
                DisplaySelectedNote();
            }
        }

        /// <summary>
        /// Обработчик события выбора даты
        /// </summary>
        private void cbDates_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialLoad)
            {
                DisplaySelectedNote();
            }
        }

        /// <summary>
        /// Загружает список проведённых процедур для данного пациента.
        /// </summary>
        private void LoadProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pa.AppointmentID, pr.ProcedureName, pa.AppointmentDateTime, pa.Status, 
                                     d.FullName as DoctorName, pa.Description
                                     FROM ProcedureAppointments pa
                                     INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                                     LEFT JOIN Doctors d ON pa.DoctorID = d.DoctorID
                                     WHERE pa.PatientID = @PatientID
                                     ORDER BY pa.AppointmentDateTime DESC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
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

        /// <summary>
        /// Обработчик события выбора процедуры в DataGrid
        /// </summary>
        private void dgProcedures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProcedures.SelectedItem != null)
            {
                _selectedProcedure = dgProcedures.SelectedItem as DataRowView;
                if (_selectedProcedure != null)
                {
                    // Загружаем описание процедуры
                    var description = _selectedProcedure["Description"];
                    txtProcedureDescription.Text = (description != null && description != DBNull.Value)
                        ? description.ToString()
                        : "Описание не указано";
                    
                    // Автоматически открываем панель деталей
                    expanderProcedureDetails.IsExpanded = true;
                }
            }
            else
            {
                txtProcedureDescription.Text = "";
                expanderProcedureDetails.IsExpanded = false;
            }
        }

        /// <summary>
        /// Обработчик двойного клика по строке процедуры
        /// </summary>
        private void dgProcedures_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProcedures.SelectedItem != null)
            {
                DataRowView row = dgProcedures.SelectedItem as DataRowView;
                if (row != null)
                {
                    int appointmentID = Convert.ToInt32(row["AppointmentID"]);
                    // Открываем диалог просмотра детальной информации о процедуре
                    ShowProcedureDetailsDialog(appointmentID);
                }
            }
        }

        /// <summary>
        /// Показывает диалог с подробной информацией о процедуре
        /// </summary>
        private void ShowProcedureDetailsDialog(int appointmentID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pa.AppointmentID, pa.Description, 
                                    pr.ProcedureName, pa.AppointmentDateTime, pa.Status, 
                                    d.FullName as DoctorName, pr.Duration
                                    FROM ProcedureAppointments pa
                                    INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                                    LEFT JOIN Doctors d ON pa.DoctorID = d.DoctorID
                                    WHERE pa.AppointmentID = @AppointmentID";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AppointmentID", appointmentID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string procedureName = reader["ProcedureName"].ToString();
                                string doctorName = reader["DoctorName"] != DBNull.Value 
                                    ? reader["DoctorName"].ToString() 
                                    : "Не указан";
                                string dateTime = Convert.ToDateTime(reader["AppointmentDateTime"]).ToString("dd.MM.yyyy HH:mm");
                                string status = reader["Status"].ToString();
                                string duration = reader["Duration"] != DBNull.Value 
                                    ? reader["Duration"].ToString() + " мин." 
                                    : "Не указана";
                                string description = reader["Description"] != DBNull.Value 
                                    ? reader["Description"].ToString() 
                                    : "Описание отсутствует";

                                string message = $"Процедура: {procedureName}\n" +
                                                $"Врач: {doctorName}\n" +
                                                $"Дата и время: {dateTime}\n" +
                                                $"Статус: {status}\n" +
                                                $"Длительность: {duration}\n\n" +
                                                $"Описание:\n{description}";

                                MessageBox.Show(message, "Детали процедуры", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке деталей процедуры: " + ex.Message);
            }
        }

        /// <summary>
        /// Показывает историю медицинских записей пациента
        /// </summary>
        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            ShowPatientHistoryWindow();
        }

        /// <summary>
        /// Открывает окно с историей медицинских записей пациента
        /// </summary>
        private void ShowPatientHistoryWindow()
        {
            try
            {
                PatientHistoryWindow historyWindow = new PatientHistoryWindow(_patientID, _patientName);
                historyWindow.Owner = this;
                historyWindow.ShowDialog();
                
                // После закрытия окна истории, обновляем данные
                LoadAllMedicalNotes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии истории пациента: " + ex.Message);
            }
        }

        /// <summary>
        /// Сохраняет изменения в медицинских заметках.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Для добавления новой медицинской заметки воспользуйтесь кнопкой 'История'.");
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// Класс для хранения информации о враче для комбобокса
    /// </summary>
    public class DoctorItem
    {
        public int? DoctorID { get; set; }
        public string DoctorName { get; set; }
    }

    /// <summary>
    /// Класс для хранения информации о дате для комбобокса
    /// </summary>
    public class DateItem
    {
        public DateTime Date { get; set; }
        public string DisplayText { get; set; }
    }
}