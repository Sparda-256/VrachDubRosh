using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace VrachDubRosh
{
    public partial class MedCardWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;
        private bool _isDarkTheme = false;

        // Данные пациента
        private DateTime? _dateOfBirth;
        private DateTime? _recordDate;
        private DateTime? _dischargeDate;
        private string _stayType;
        private string _accommodation;

        // Данные для отображения
        private DataTable _diagnoses;
        private DataTable _measurements;
        private DataTable _medications;
        private Dictionary<string, DataTable> _doctorProcedures = new Dictionary<string, DataTable>();

        public MedCardWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            
            this.Title = $"Санаторная книжка - {patientName}";
            txtPatientName.Text = patientName;
            
            this.Loaded += MedCardWindow_Loaded;
        }

        private void MedCardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Определяем, применена ли темная тема в родительском окне
            if (this.Owner != null)
            {
                if (this.Owner is GlavDoctorWindow glavWindow && glavWindow.isDarkTheme)
                {
                    _isDarkTheme = true;
                    ApplyDarkTheme();
                }
                else if (this.Owner is DoctorWindow doctorWindow && doctorWindow.isDarkTheme)
                {
                    _isDarkTheme = true;
                    ApplyDarkTheme();
                }
            }

            // Загружаем данные пациента
            LoadPatientInfo();
            
            // Загружаем диагнозы
            LoadDiagnoses();
            
            // Загружаем антропометрические измерения
            LoadMeasurements();
            
            // Загружаем медикаменты
            LoadMedications();
            
            // Загружаем процедуры для каждого врача
            LoadProceduresByDoctor();
        }

        private void ApplyDarkTheme()
        {
            // Применяем темную тему
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        private void LoadPatientInfo()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate, p.StayType,
                               CONCAT('Корпус ', b.BuildingNumber, ', комната ', r.RoomNumber, ', кровать ', a.BedNumber) AS Accommodation
                        FROM Patients p
                        LEFT JOIN Accommodations a ON p.PatientID = a.PatientID
                        LEFT JOIN Rooms r ON a.RoomID = r.RoomID
                        LEFT JOIN Buildings b ON r.BuildingID = b.BuildingID
                        WHERE p.PatientID = @PatientID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Сохраняем данные
                                _dateOfBirth = reader["DateOfBirth"] as DateTime?;
                                _recordDate = reader["RecordDate"] as DateTime?;
                                _dischargeDate = reader["DischargeDate"] as DateTime?;
                                _stayType = reader["StayType"].ToString();
                                _accommodation = reader["Accommodation"].ToString();

                                // Отображаем данные
                                txtPatientFullName.Text = reader["FullName"].ToString();
                                txtPatientDateOfBirth.Text = _dateOfBirth.HasValue 
                                    ? _dateOfBirth.Value.ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                txtPatientRecordDate.Text = _recordDate.HasValue 
                                    ? _recordDate.Value.ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                txtPatientDischargeDate.Text = _dischargeDate.HasValue 
                                    ? _dischargeDate.Value.ToString("dd.MM.yyyy") 
                                    : "Не определена";
                                
                                txtStayType.Text = _stayType;
                                txtAccommodation.Text = String.IsNullOrEmpty(_accommodation) 
                                    ? "Не определено" 
                                    : _accommodation;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке информации о пациенте: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDiagnoses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Загружаем все диагнозы
                    string query = @"
                        SELECT d.DiagnosisName
                        FROM PatientDiagnoses pd
                        JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                        WHERE pd.PatientID = @PatientID";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    _diagnoses = new DataTable();
                    adapter.Fill(_diagnoses);
                    dgDiagnoses.ItemsSource = _diagnoses.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            // Напрямую открываем окно добавления диагноза
            AddEditDiagnosisWindow addDiagnosisWindow = new AddEditDiagnosisWindow(_patientID, _patientName);
            addDiagnosisWindow.Owner = this;
            bool? result = addDiagnosisWindow.ShowDialog();
            
            if (result == true)
            {
                // Перезагружаем список диагнозов
                LoadDiagnoses();
            }
        }

        private void btnRemoveDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (dgDiagnoses.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите диагноз(ы) для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<string> selectedDiagnoses = new List<string>();
            foreach (DataRowView row in dgDiagnoses.SelectedItems)
            {
                selectedDiagnoses.Add(row["DiagnosisName"].ToString());
            }

            string diagnosesStr = selectedDiagnoses.Count == 1 
                ? $"диагноз '{selectedDiagnoses[0]}'" 
                : $"{selectedDiagnoses.Count} диагнозов";

            if (MessageBox.Show($"Вы уверены, что хотите удалить {diagnosesStr}?", 
                               "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                                foreach (string diagnosisName in selectedDiagnoses)
                                {
                                    int diagnosisID = GetDiagnosisID(diagnosisName);
                                    if (diagnosisID > 0)
                                    {
                                        string query = @"
                                            DELETE FROM PatientDiagnoses 
                                            WHERE PatientID = @PatientID AND DiagnosisID = @DiagnosisID";
                                        
                                        using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                                            cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                                
                                transaction.Commit();
                                
                                // Обновляем отображение диагнозов
                                LoadDiagnoses();
                                
                                string successMessage = selectedDiagnoses.Count == 1
                                    ? "Диагноз удален"
                                    : "Диагнозы удалены";
                                MessageBox.Show(successMessage, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadMeasurements()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            MeasurementType,
                            Height,
                            Weight,
                            BloodPressure,
                            MeasurementDate
                        FROM PatientMeasurements
                        WHERE PatientID = @PatientID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Сбрасываем текущие значения
                            txtAdmissionHeight.Text = "";
                            txtAdmissionWeight.Text = "";
                            txtAdmissionBP.Text = "";
                            dpAdmissionDate.SelectedDate = null;
                            
                            txtProcessHeight.Text = "";
                            txtProcessWeight.Text = "";
                            txtProcessBP.Text = "";
                            dpProcessDate.SelectedDate = null;
                            
                            txtDischargeHeight.Text = "";
                            txtDischargeWeight.Text = "";
                            txtDischargeBP.Text = "";
                            dpDischargeDate.SelectedDate = null;

                            // Загружаем данные для каждого типа измерений
                            while (reader.Read())
                            {
                                string measurementType = reader["MeasurementType"].ToString();
                                decimal? height = reader["Height"] != DBNull.Value ? (decimal?)reader["Height"] : null;
                                decimal? weight = reader["Weight"] != DBNull.Value ? (decimal?)reader["Weight"] : null;
                                string bloodPressure = reader["BloodPressure"] != DBNull.Value ? reader["BloodPressure"].ToString() : "";
                                DateTime? measurementDate = reader["MeasurementDate"] != DBNull.Value ? (DateTime?)reader["MeasurementDate"] : null;
                                
                                switch (measurementType)
                                {
                                    case "При поступлении":
                                        txtAdmissionHeight.Text = height?.ToString() ?? "";
                                        txtAdmissionWeight.Text = weight?.ToString() ?? "";
                                        txtAdmissionBP.Text = bloodPressure;
                                        dpAdmissionDate.SelectedDate = measurementDate;
                                        break;
                                        
                                    case "В процессе лечения":
                                        txtProcessHeight.Text = height?.ToString() ?? "";
                                        txtProcessWeight.Text = weight?.ToString() ?? "";
                                        txtProcessBP.Text = bloodPressure;
                                        dpProcessDate.SelectedDate = measurementDate;
                                        break;
                                        
                                    case "При выписке":
                                        txtDischargeHeight.Text = height?.ToString() ?? "";
                                        txtDischargeWeight.Text = weight?.ToString() ?? "";
                                        txtDischargeBP.Text = bloodPressure;
                                        dpDischargeDate.SelectedDate = measurementDate;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке антропометрических данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMeasurements()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Массив типов измерений для обработки
                    string[] measurementTypes = { "При поступлении", "В процессе лечения", "При выписке" };
                    
                    foreach (string measurementType in measurementTypes)
                    {
                        // Получаем значения из соответствующих полей в зависимости от типа
                        string heightText = "";
                        string weightText = "";
                        string bloodPressure = "";
                        DateTime? measurementDate = null;
                        
                        switch (measurementType)
                        {
                            case "При поступлении":
                                heightText = txtAdmissionHeight.Text.Trim();
                                weightText = txtAdmissionWeight.Text.Trim();
                                bloodPressure = txtAdmissionBP.Text.Trim();
                                measurementDate = dpAdmissionDate.SelectedDate;
                                break;
                                
                            case "В процессе лечения":
                                heightText = txtProcessHeight.Text.Trim();
                                weightText = txtProcessWeight.Text.Trim();
                                bloodPressure = txtProcessBP.Text.Trim();
                                measurementDate = dpProcessDate.SelectedDate;
                                break;
                                
                            case "При выписке":
                                heightText = txtDischargeHeight.Text.Trim();
                                weightText = txtDischargeWeight.Text.Trim();
                                bloodPressure = txtDischargeBP.Text.Trim();
                                measurementDate = dpDischargeDate.SelectedDate;
                                break;
                        }
                        
                        // Проверяем, есть ли данные для сохранения
                        if (string.IsNullOrWhiteSpace(heightText) && 
                            string.IsNullOrWhiteSpace(weightText) && 
                            string.IsNullOrWhiteSpace(bloodPressure) &&
                            !measurementDate.HasValue)
                        {
                            // Если все поля пустые, удаляем существующую запись для этого типа
                            string deleteQuery = @"
                                DELETE FROM PatientMeasurements 
                                WHERE PatientID = @PatientID AND MeasurementType = @MeasurementType";
                            
                            using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con))
                            {
                                deleteCmd.Parameters.AddWithValue("@PatientID", _patientID);
                                deleteCmd.Parameters.AddWithValue("@MeasurementType", measurementType);
                                deleteCmd.ExecuteNonQuery();
                            }
                            
                            continue;
                        }
                        
                        // Если дата не выбрана, но есть другие данные, устанавливаем текущую дату
                        if (!measurementDate.HasValue)
                        {
                            measurementDate = DateTime.Now;
                        }
                        
                        // Преобразуем строковые значения в числовые
                        decimal? height = null;
                        decimal? weight = null;
                        
                        if (!string.IsNullOrWhiteSpace(heightText) && decimal.TryParse(heightText, out decimal parsedHeight))
                        {
                            height = parsedHeight;
                        }
                        
                        if (!string.IsNullOrWhiteSpace(weightText) && decimal.TryParse(weightText, out decimal parsedWeight))
                        {
                            weight = parsedWeight;
                        }
                        
                        // Проверяем, существует ли уже запись для этого типа
                        string checkQuery = @"
                            SELECT COUNT(*) 
                            FROM PatientMeasurements 
                            WHERE PatientID = @PatientID AND MeasurementType = @MeasurementType";
                        
                        bool recordExists = false;
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                        {
                            checkCmd.Parameters.AddWithValue("@PatientID", _patientID);
                            checkCmd.Parameters.AddWithValue("@MeasurementType", measurementType);
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            recordExists = count > 0;
                        }
                        
                        if (recordExists)
                        {
                            // Обновляем существующую запись
                            string updateQuery = @"
                                UPDATE PatientMeasurements 
                                SET Height = @Height,
                                    Weight = @Weight,
                                    BloodPressure = @BloodPressure,
                                    MeasurementDate = @MeasurementDate
                                WHERE PatientID = @PatientID AND MeasurementType = @MeasurementType";
                            
                            using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                            {
                                updateCmd.Parameters.AddWithValue("@PatientID", _patientID);
                                updateCmd.Parameters.AddWithValue("@MeasurementType", measurementType);
                                updateCmd.Parameters.AddWithValue("@Height", height.HasValue ? (object)height.Value : DBNull.Value);
                                updateCmd.Parameters.AddWithValue("@Weight", weight.HasValue ? (object)weight.Value : DBNull.Value);
                                updateCmd.Parameters.AddWithValue("@BloodPressure", string.IsNullOrWhiteSpace(bloodPressure) ? DBNull.Value : (object)bloodPressure);
                                updateCmd.Parameters.AddWithValue("@MeasurementDate", measurementDate.HasValue ? (object)measurementDate.Value : DBNull.Value);
                                
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Создаем новую запись
                            string insertQuery = @"
                                INSERT INTO PatientMeasurements 
                                    (PatientID, MeasurementType, Height, Weight, BloodPressure, MeasurementDate, MeasuredBy) 
                                VALUES 
                                    (@PatientID, @MeasurementType, @Height, @Weight, @BloodPressure, @MeasurementDate, @MeasuredBy)";
                            
                            using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                            {
                                insertCmd.Parameters.AddWithValue("@PatientID", _patientID);
                                insertCmd.Parameters.AddWithValue("@MeasurementType", measurementType);
                                insertCmd.Parameters.AddWithValue("@Height", height.HasValue ? (object)height.Value : DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@Weight", weight.HasValue ? (object)weight.Value : DBNull.Value);
                                insertCmd.Parameters.AddWithValue("@BloodPressure", string.IsNullOrWhiteSpace(bloodPressure) ? DBNull.Value : (object)bloodPressure);
                                insertCmd.Parameters.AddWithValue("@MeasurementDate", measurementDate.HasValue ? (object)measurementDate.Value : DBNull.Value);
                                
                                // Получаем ID главврача (если пользователь - главврач)
                                if (this.Owner is GlavDoctorWindow glavDoctorWindow && glavDoctorWindow.ChiefDoctorID > 0)
                                {
                                    insertCmd.Parameters.AddWithValue("@MeasuredBy", glavDoctorWindow.ChiefDoctorID);
                                }
                                else
                                {
                                    insertCmd.Parameters.AddWithValue("@MeasuredBy", DBNull.Value);
                                }
                                
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                
                // Перезагружаем данные для отображения обновленных дат
                LoadMeasurements();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении антропометрических данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSaveAll_Click(object sender, RoutedEventArgs e)
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
                            // Сохраняем диагнозы
                            SaveDiagnoses();
                            
                            // Сохраняем антропометрические данные
                            SaveMeasurements();
                            
                            // Сохраняем медикаменты (они уже сохраняются в базе при добавлении/редактировании)
                            
                            // Фиксируем транзакцию
                            transaction.Commit();
                            
                            MessageBox.Show("Все изменения успешно сохранены", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMedications()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            MedicationName,
                            Dosage,
                            Instructions,
                            PrescribedDate
                        FROM PatientMedications
                        WHERE PatientID = @PatientID
                        ORDER BY PrescribedDate DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    _medications = new DataTable();
                    adapter.Fill(_medications);
                    dgMedications.ItemsSource = _medications.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке медикаментов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProceduresByDoctor()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Сначала получаем список врачей, назначенных данному пациенту
                    string doctorsQuery = @"
                        SELECT DISTINCT 
                            d.DoctorID,
                            d.FullName,
                            d.Specialty,
                            d.GeneralName,
                            d.OfficeNumber
                        FROM Doctors d
                        JOIN PatientDoctorAssignments pda ON d.DoctorID = pda.DoctorID
                        WHERE pda.PatientID = @PatientID
                        ORDER BY d.GeneralName";

                    using (SqlCommand cmd = new SqlCommand(doctorsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int doctorID = Convert.ToInt32(reader["DoctorID"]);
                                string doctorName = reader["FullName"].ToString();
                                string specialty = reader["Specialty"].ToString();
                                string generalName = reader["GeneralName"].ToString();
                                string officeNumber = reader["OfficeNumber"].ToString();
                                
                                // Если общее наименование пустое, используем специальность
                                string tabTitle = !string.IsNullOrEmpty(generalName) 
                                    ? generalName 
                                    : specialty;
                                
                                // Создаем ключ для словаря и названия вкладки
                                string key = tabTitle;
                                
                                // Добавляем новую вкладку для этого врача/специальности
                                TabItem newTab = new TabItem
                                {
                                    Header = tabTitle
                                };
                                
                                // Будем запрашивать процедуры для этого врача и пациента
                                LoadProceduresForDoctor(doctorID, key);
                                
                                // Создаем контент для вкладки
                                Grid grid = new Grid { Margin = new Thickness(15) };
                                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                                
                                // Заголовок вкладки
                                TextBlock titleBlock = new TextBlock
                                {
                                    Text = $"{tabTitle.ToUpper()}, кабинет {officeNumber}",
                                    FontSize = 18,
                                    FontWeight = FontWeights.Bold,
                                    Foreground = (Brush)FindResource("AccentBrush"),
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 0, 0, 15)
                                };
                                Grid.SetRow(titleBlock, 0);
                                
                                // Подзаголовок
                                TextBlock subtitleBlock = new TextBlock
                                {
                                    Text = $"НАЗНАЧЕННЫЕ ПРОЦЕДУРЫ",
                                    FontSize = 16,
                                    FontWeight = FontWeights.SemiBold,
                                    Foreground = (Brush)FindResource("ForegroundBrush"),
                                    Margin = new Thickness(0, 0, 0, 10)
                                };
                                Grid.SetRow(subtitleBlock, 1);
                                
                                // DataGrid для процедур
                                DataGrid proceduresGrid = new DataGrid
                                {
                                    Style = (Style)FindResource("ModernDataGridStyle"),
                                    AutoGenerateColumns = false,
                                    IsReadOnly = true
                                };
                                
                                // Настраиваем столбцы для DataGrid
                                DataGridTextColumn nameColumn = new DataGridTextColumn
                                {
                                    Header = "Наименование",
                                    Binding = new System.Windows.Data.Binding("ProcedureName"),
                                    Width = new DataGridLength(2, DataGridLengthUnitType.Star)
                                };
                                
                                DataGridTextColumn dateColumn = new DataGridTextColumn
                                {
                                    Header = "Дата и время",
                                    Binding = new System.Windows.Data.Binding("AppointmentDateTime") 
                                    { 
                                        StringFormat = "{0:dd.MM.yyyy HH:mm}" 
                                    },
                                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                                };
                                
                                DataGridTextColumn statusColumn = new DataGridTextColumn
                                {
                                    Header = "Статус",
                                    Binding = new System.Windows.Data.Binding("Status"),
                                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                                };
                                
                                proceduresGrid.Columns.Add(nameColumn);
                                proceduresGrid.Columns.Add(dateColumn);
                                proceduresGrid.Columns.Add(statusColumn);
                                
                                // Привязываем данные, если они уже загружены
                                if (_doctorProcedures.ContainsKey(key))
                                {
                                    proceduresGrid.ItemsSource = _doctorProcedures[key].DefaultView;
                                }
                                
                                Grid.SetRow(proceduresGrid, 2);
                                
                                // Добавляем элементы в Grid
                                grid.Children.Add(titleBlock);
                                grid.Children.Add(subtitleBlock);
                                grid.Children.Add(proceduresGrid);
                                
                                // Устанавливаем содержимое вкладки
                                newTab.Content = grid;
                                
                                // Добавляем вкладку в TabControl
                                tabControlMedCard.Items.Add(newTab);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке вкладок процедур: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProceduresForDoctor(int doctorID, string key)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            pa.AppointmentID,
                            pr.ProcedureName,
                            pa.AppointmentDateTime,
                            pa.Status,
                            pa.Description
                        FROM ProcedureAppointments pa
                        JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        WHERE pa.PatientID = @PatientID AND pa.DoctorID = @DoctorID
                        ORDER BY pa.AppointmentDateTime DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    adapter.SelectCommand.Parameters.AddWithValue("@DoctorID", doctorID);
                    
                    DataTable dtProcedures = new DataTable();
                    adapter.Fill(dtProcedures);
                    
                    // Сохраняем результат в словаре
                    _doctorProcedures[key] = dtProcedures;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке процедур для врача: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintMedicalCard();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PrintMedicalCard()
        {
            try
            {
                // Создаем документ для печати
                FlowDocument document = CreatePrintDocument();

                // Настраиваем параметры печати
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Создаем пагинатор для нашего документа
                    IDocumentPaginatorSource paginatorSource = document;
                    
                    // Печатаем документ
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Санаторная книжка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при печати медицинской карточки: " + ex.Message, "Ошибка печати", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrintDocument()
        {
            FlowDocument doc = new FlowDocument();
            doc.PagePadding = new Thickness(50);
            doc.FontFamily = new FontFamily("Arial");
            doc.FontSize = 12;
            
            // Стиль для заголовков
            Style headerStyle = new Style(typeof(Paragraph));
            headerStyle.Setters.Add(new Setter(Block.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(Block.FontSizeProperty, 16.0));
            headerStyle.Setters.Add(new Setter(Block.TextAlignmentProperty, TextAlignment.Center));
            headerStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 10, 0, 10)));
            
            // Стиль для подзаголовков
            Style subheaderStyle = new Style(typeof(Paragraph));
            subheaderStyle.Setters.Add(new Setter(Block.FontWeightProperty, FontWeights.Bold));
            subheaderStyle.Setters.Add(new Setter(Block.FontSizeProperty, 14.0));
            subheaderStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 10, 0, 5)));
            
            // Стиль для обычного текста
            Style textStyle = new Style(typeof(Paragraph));
            textStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 5)));
            
            // ===== Титульный лист =====
            Paragraph titleHeader = new Paragraph(new Run("САНАТОРНАЯ КНИЖКА"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            doc.Blocks.Add(titleHeader);
            
            Paragraph patientNamePara = new Paragraph(new Run(_patientName))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 20)
            };
            doc.Blocks.Add(patientNamePara);
            
            // Основная информация о пациенте
            doc.Blocks.Add(new Paragraph(new Run($"Дата рождения: {(_dateOfBirth.HasValue ? _dateOfBirth.Value.ToString("dd.MM.yyyy") : "Не указана")}")));
            
            Run treatmentPeriod = new Run($"Срок лечения: с {(_recordDate.HasValue ? _recordDate.Value.ToString("dd.MM.yyyy") : "не указана")} по {(_dischargeDate.HasValue ? _dischargeDate.Value.ToString("dd.MM.yyyy") : "не определена")}");
            doc.Blocks.Add(new Paragraph(treatmentPeriod));
            
            doc.Blocks.Add(new Paragraph(new Run($"Тип размещения: {_stayType}")));
            doc.Blocks.Add(new Paragraph(new Run($"Проживание: {_accommodation}")));
            
            // Диагнозы (добавлены на титульный лист)
            Paragraph diagnosesHeader = new Paragraph(new Run("ДИАГНОЗЫ"))
            {
                Style = headerStyle,
                Margin = new Thickness(0, 20, 0, 10)
            };
            doc.Blocks.Add(diagnosesHeader);
            
            // Получаем данные из источника DataGrid
            DataView diagnosesView = dgDiagnoses.ItemsSource as DataView;
            
            if (diagnosesView != null && diagnosesView.Count > 0)
            {
                foreach (DataRowView row in diagnosesView)
                {
                    doc.Blocks.Add(new Paragraph(new Run($"• {row["DiagnosisName"]}")) { Style = textStyle });
                }
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("Диагнозы не указаны")) { Style = textStyle });
            }
            
            // Добавляем разрыв страницы
            doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Border 
            { 
                Height = 20 
            }));
            doc.Blocks.Add(new Paragraph(new Run(" ")) { BreakPageBefore = true });
            
            // ===== Процедуры по врачам =====
            foreach (string key in _doctorProcedures.Keys)
            {
                DataTable dtProcs = _doctorProcedures[key];
                if (dtProcs.Rows.Count == 0) continue;
                
                // Заголовок с названием специальности
                Paragraph specialtyHeader = new Paragraph(new Run(key.ToUpper()))
                {
                    Style = headerStyle
                };
                doc.Blocks.Add(specialtyHeader);
                
                // Подзаголовок для процедур
                Paragraph procsSubheader = new Paragraph(new Run("НАЗНАЧЕННЫЕ ПРОЦЕДУРЫ:"))
                {
                    Style = subheaderStyle
                };
                doc.Blocks.Add(procsSubheader);
                
                // Создаем таблицу с процедурами
                Table procTable = new Table();
                procTable.CellSpacing = 0;
                procTable.BorderThickness = new Thickness(1);
                procTable.BorderBrush = Brushes.Black;
                
                // Добавляем столбцы
                procTable.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) });
                procTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
                procTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                
                // Создаем заголовок таблицы
                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.Background = Brushes.LightGray;
                headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Наименование")))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дата и время")))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Статус")))));
                headerGroup.Rows.Add(headerRow);
                procTable.RowGroups.Add(headerGroup);
                
                // Добавляем строки с данными
                TableRowGroup dataGroup = new TableRowGroup();
                foreach (DataRow row in dtProcs.Rows)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["ProcedureName"].ToString()))));
                    
                    DateTime dt = Convert.ToDateTime(row["AppointmentDateTime"]);
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(dt.ToString("dd.MM.yyyy HH:mm")))));
                    
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Status"].ToString()))));
                    dataGroup.Rows.Add(dataRow);
                }
                procTable.RowGroups.Add(dataGroup);
                
                doc.Blocks.Add(procTable);
                
                // Добавляем разрыв страницы после каждой специальности
                doc.Blocks.Add(new Paragraph(new Run(" ")) { BreakPageBefore = true });
            }
            
            // Добавляем раздел с измерениями
            Paragraph measurementsHeader = new Paragraph(new Run("АНТРОПОМЕТРИЧЕСКИЕ ИЗМЕРЕНИЯ"))
            {
                Style = headerStyle,
                Margin = new Thickness(0, 20, 0, 10)
            };
            doc.Blocks.Add(measurementsHeader);

            // При поступлении
            if (!string.IsNullOrWhiteSpace(txtAdmissionHeight.Text) || 
                !string.IsNullOrWhiteSpace(txtAdmissionWeight.Text) || 
                !string.IsNullOrWhiteSpace(txtAdmissionBP.Text) ||
                dpAdmissionDate.SelectedDate.HasValue)
            {
                Paragraph admissionHeader = new Paragraph(new Run("При поступлении:"))
                {
                    Style = subheaderStyle,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                doc.Blocks.Add(admissionHeader);

                if (!string.IsNullOrWhiteSpace(txtAdmissionHeight.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Рост: {txtAdmissionHeight.Text} см")) { Style = textStyle });
                }
                
                if (!string.IsNullOrWhiteSpace(txtAdmissionWeight.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Вес: {txtAdmissionWeight.Text} кг")) { Style = textStyle });
                }
                
                if (!string.IsNullOrWhiteSpace(txtAdmissionBP.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Давление: {txtAdmissionBP.Text}")) { Style = textStyle });
                }
                
                if (dpAdmissionDate.SelectedDate.HasValue)
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Дата: {dpAdmissionDate.SelectedDate.Value.ToString("dd.MM.yyyy")}")) { Style = textStyle, Margin = new Thickness(0, 0, 0, 10) });
                }
            }

            // В процессе лечения
            if (!string.IsNullOrWhiteSpace(txtProcessHeight.Text) || 
                !string.IsNullOrWhiteSpace(txtProcessWeight.Text) || 
                !string.IsNullOrWhiteSpace(txtProcessBP.Text) ||
                dpProcessDate.SelectedDate.HasValue)
            {
                Paragraph processHeader = new Paragraph(new Run("В процессе лечения:"))
                {
                    Style = subheaderStyle,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                doc.Blocks.Add(processHeader);

                if (!string.IsNullOrWhiteSpace(txtProcessHeight.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Рост: {txtProcessHeight.Text} см")) { Style = textStyle });
                }
                
                if (!string.IsNullOrWhiteSpace(txtProcessWeight.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Вес: {txtProcessWeight.Text} кг")) { Style = textStyle });
                }
                
                if (!string.IsNullOrWhiteSpace(txtProcessBP.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Давление: {txtProcessBP.Text}")) { Style = textStyle });
                }
                
                if (dpProcessDate.SelectedDate.HasValue)
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Дата: {dpProcessDate.SelectedDate.Value.ToString("dd.MM.yyyy")}")) { Style = textStyle, Margin = new Thickness(0, 0, 0, 10) });
                }
            }

            // При выписке
            if (!string.IsNullOrWhiteSpace(txtDischargeHeight.Text) || 
                !string.IsNullOrWhiteSpace(txtDischargeWeight.Text) || 
                !string.IsNullOrWhiteSpace(txtDischargeBP.Text) ||
                dpDischargeDate.SelectedDate.HasValue)
            {
                Paragraph dischargeHeader = new Paragraph(new Run("При выписке:"))
                {
                    Style = subheaderStyle,
                    Margin = new Thickness(0, 10, 0, 5)
                };
                doc.Blocks.Add(dischargeHeader);

                if (!string.IsNullOrWhiteSpace(txtDischargeHeight.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Рост: {txtDischargeHeight.Text} см")) { Style = textStyle });
                }
                
                if (!string.IsNullOrWhiteSpace(txtDischargeWeight.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Вес: {txtDischargeWeight.Text} кг")) { Style = textStyle });
                }
                
                if (!string.IsNullOrWhiteSpace(txtDischargeBP.Text))
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Давление: {txtDischargeBP.Text}")) { Style = textStyle });
                }
                
                if (dpDischargeDate.SelectedDate.HasValue)
                {
                    doc.Blocks.Add(new Paragraph(new Run($"Дата: {dpDischargeDate.SelectedDate.Value.ToString("dd.MM.yyyy")}")) { Style = textStyle, Margin = new Thickness(0, 0, 0, 10) });
                }
            }
            
            // ===== Медикаменты =====
            Paragraph medHeader = new Paragraph(new Run("МЕДИКАМЕНТЫ"))
            {
                Style = headerStyle
            };
            doc.Blocks.Add(medHeader);
            
            if (_medications != null && _medications.Rows.Count > 0)
            {
                // Создаем таблицу с медикаментами
                Table medTable = new Table();
                medTable.CellSpacing = 0;
                medTable.BorderThickness = new Thickness(1);
                medTable.BorderBrush = Brushes.Black;
                
                // Добавляем столбцы
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                
                // Создаем заголовок таблицы
                TableRowGroup medHeaderGroup = new TableRowGroup();
                TableRow medHeaderRow = new TableRow();
                medHeaderRow.Background = Brushes.LightGray;
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Наименование")))));
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дозировка")))));
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Инструкции")))));
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дата назначения")))));
                medHeaderGroup.Rows.Add(medHeaderRow);
                medTable.RowGroups.Add(medHeaderGroup);
                
                // Добавляем строки с данными
                TableRowGroup medDataGroup = new TableRowGroup();
                foreach (DataRow row in _medications.Rows)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["MedicationName"].ToString()))));
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Dosage"].ToString()))));
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Instructions"].ToString()))));
                    
                    DateTime dt = Convert.ToDateTime(row["PrescribedDate"]);
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(dt.ToString("dd.MM.yyyy")))));
                    
                    medDataGroup.Rows.Add(dataRow);
                }
                medTable.RowGroups.Add(medDataGroup);
                
                doc.Blocks.Add(medTable);
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("Нет назначенных медикаментов")) { Style = textStyle });
            }
            
            return doc;
        }

        private int GetDiagnosisID(string diagnosisName)
        {
            int diagnosisID = -1;
            
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DiagnosisID FROM Diagnoses WHERE DiagnosisName = @DiagnosisName";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DiagnosisName", diagnosisName);
                        object result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            diagnosisID = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении ID диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return diagnosisID;
        }

        private void SaveDiagnoses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Получаем текущий список диагнозов из DataGrid
                    DataView diagnosesView = dgDiagnoses.ItemsSource as DataView;
                    
                    if (diagnosesView != null)
                    {
                        // Сначала удаляем все существующие диагнозы пациента
                        string deleteQuery = @"
                            DELETE FROM PatientDiagnoses
                            WHERE PatientID = @PatientID";
                            
                        using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con))
                        {
                            deleteCmd.Parameters.AddWithValue("@PatientID", _patientID);
                            deleteCmd.ExecuteNonQuery();
                        }
                        
                        // Затем добавляем текущие диагнозы
                        foreach (DataRowView row in diagnosesView)
                        {
                            string diagnosisName = row["DiagnosisName"].ToString();
                            int diagnosisID = GetDiagnosisID(diagnosisName);
                            
                            if (diagnosisID > 0)
                            {
                                string insertQuery = @"
                                    INSERT INTO PatientDiagnoses (PatientID, DiagnosisID)
                                    VALUES (@PatientID, @DiagnosisID)";
                                    
                                using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                                {
                                    insertCmd.Parameters.AddWithValue("@PatientID", _patientID);
                                    insertCmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddMedication_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем и настраиваем окно для добавления медикаментов
                Window medicationWindow = new Window
                {
                    Title = "Добавление медикамента",
                    Width = 450,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем Grid для размещения элементов
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Заголовок
                TextBlock titleBlock = new TextBlock
                {
                    Text = "Добавление медикамента",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(titleBlock, 0);
                Grid.SetColumnSpan(titleBlock, 2);

                // Наименование
                TextBlock nameLabel = new TextBlock
                {
                    Text = "Наименование:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(nameLabel, 1);
                Grid.SetColumn(nameLabel, 0);

                TextBox nameTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                Grid.SetRow(nameTextBox, 1);
                Grid.SetColumn(nameTextBox, 1);

                // Дозировка
                TextBlock dosageLabel = new TextBlock
                {
                    Text = "Дозировка:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dosageLabel, 2);
                Grid.SetColumn(dosageLabel, 0);

                TextBox dosageTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                Grid.SetRow(dosageTextBox, 2);
                Grid.SetColumn(dosageTextBox, 1);

                // Инструкции
                TextBlock instructionsLabel = new TextBlock
                {
                    Text = "Инструкции:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetRow(instructionsLabel, 3);
                Grid.SetColumn(instructionsLabel, 0);

                TextBox instructionsTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 100,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Grid.SetRow(instructionsTextBox, 3);
                Grid.SetColumn(instructionsTextBox, 1);

                // Дата назначения
                TextBlock dateLabel = new TextBlock
                {
                    Text = "Дата назначения:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dateLabel, 4);
                Grid.SetColumn(dateLabel, 0);

                DatePicker datePicker = new DatePicker
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    SelectedDate = DateTime.Today,
                    Style = (Style)FindResource("RoundedDatePickerStyle")
                };
                Grid.SetRow(datePicker, 4);
                Grid.SetColumn(datePicker, 1);

                // Кнопки
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Button saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                saveButton.Click += (s, args) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                        {
                            MessageBox.Show("Пожалуйста, укажите наименование медикамента", "Предупреждение");
                            return;
                        }

                        // Получаем данные из формы
                        string medicationName = nameTextBox.Text.Trim();
                        string dosage = dosageTextBox.Text.Trim();
                        string instructions = instructionsTextBox.Text.Trim();

                        // Сохраняем данные в базу
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = @"
                                INSERT INTO PatientMedications 
                                    (PatientID, MedicationName, Dosage, Instructions, PrescribedDate, PrescribedBy) 
                                VALUES 
                                    (@PatientID, @MedicationName, @Dosage, @Instructions, @PrescribedDate, @PrescribedBy)";

                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", _patientID);
                                cmd.Parameters.AddWithValue("@MedicationName", medicationName);
                                cmd.Parameters.AddWithValue("@Dosage", string.IsNullOrEmpty(dosage) ? DBNull.Value : (object)dosage);
                                cmd.Parameters.AddWithValue("@Instructions", string.IsNullOrEmpty(instructions) ? DBNull.Value : (object)instructions);
                                cmd.Parameters.AddWithValue("@PrescribedDate", datePicker.SelectedDate ?? DateTime.Now);
                                
                                // Получаем ID главврача (если пользователь - главврач)
                                if (this.Owner is GlavDoctorWindow glavDoctorWindow && glavDoctorWindow.ChiefDoctorID > 0)
                                {
                                    cmd.Parameters.AddWithValue("@PrescribedBy", glavDoctorWindow.ChiefDoctorID);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@PrescribedBy", DBNull.Value);
                                }

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Обновляем отображение медикаментов
                        LoadMedications();
                        MessageBox.Show("Медикамент успешно добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        medicationWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при добавлении медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Button cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 30,
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                cancelButton.Click += (s, args) => medicationWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 5);
                Grid.SetColumnSpan(buttonPanel, 2);

                // Добавляем элементы на форму
                grid.Children.Add(titleBlock);
                grid.Children.Add(nameLabel);
                grid.Children.Add(nameTextBox);
                grid.Children.Add(dosageLabel);
                grid.Children.Add(dosageTextBox);
                grid.Children.Add(instructionsLabel);
                grid.Children.Add(instructionsTextBox);
                grid.Children.Add(dateLabel);
                grid.Children.Add(datePicker);
                grid.Children.Add(buttonPanel);

                medicationWindow.Content = grid;
                medicationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна добавления медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEditMedication_Click(object sender, RoutedEventArgs e)
        {
            if (dgMedications.SelectedItem == null)
            {
                MessageBox.Show("Выберите медикамент для редактирования", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Получаем данные выбранного медикамента
                DataRowView selectedRow = dgMedications.SelectedItem as DataRowView;
                if (selectedRow == null) return;

                // Получаем ID медикамента
                int medicationID = -1;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT MedicationID 
                        FROM PatientMedications 
                        WHERE PatientID = @PatientID 
                        AND MedicationName = @MedicationName 
                        AND PrescribedDate = @PrescribedDate";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        cmd.Parameters.AddWithValue("@MedicationName", selectedRow["MedicationName"].ToString());
                        cmd.Parameters.AddWithValue("@PrescribedDate", Convert.ToDateTime(selectedRow["PrescribedDate"]));
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            medicationID = Convert.ToInt32(result);
                        }
                    }
                }

                if (medicationID == -1)
                {
                    MessageBox.Show("Не удалось найти медикамент в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем окно для редактирования
                Window medicationWindow = new Window
                {
                    Title = "Редактирование медикамента",
                    Width = 450,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем Grid для размещения элементов
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Заголовок
                TextBlock titleBlock = new TextBlock
                {
                    Text = "Редактирование медикамента",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(titleBlock, 0);
                Grid.SetColumnSpan(titleBlock, 2);

                // Наименование
                TextBlock nameLabel = new TextBlock
                {
                    Text = "Наименование:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(nameLabel, 1);
                Grid.SetColumn(nameLabel, 0);

                TextBox nameTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = selectedRow["MedicationName"].ToString()
                };
                Grid.SetRow(nameTextBox, 1);
                Grid.SetColumn(nameTextBox, 1);

                // Дозировка
                TextBlock dosageLabel = new TextBlock
                {
                    Text = "Дозировка:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dosageLabel, 2);
                Grid.SetColumn(dosageLabel, 0);

                TextBox dosageTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = selectedRow["Dosage"] != DBNull.Value ? selectedRow["Dosage"].ToString() : ""
                };
                Grid.SetRow(dosageTextBox, 2);
                Grid.SetColumn(dosageTextBox, 1);

                // Инструкции
                TextBlock instructionsLabel = new TextBlock
                {
                    Text = "Инструкции:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetRow(instructionsLabel, 3);
                Grid.SetColumn(instructionsLabel, 0);

                TextBox instructionsTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 100,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Text = selectedRow["Instructions"] != DBNull.Value ? selectedRow["Instructions"].ToString() : ""
                };
                Grid.SetRow(instructionsTextBox, 3);
                Grid.SetColumn(instructionsTextBox, 1);

                // Дата назначения
                TextBlock dateLabel = new TextBlock
                {
                    Text = "Дата назначения:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dateLabel, 4);
                Grid.SetColumn(dateLabel, 0);

                DatePicker datePicker = new DatePicker
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    SelectedDate = selectedRow["PrescribedDate"] != DBNull.Value ? 
                        (DateTime?)selectedRow["PrescribedDate"] : DateTime.Today,
                    Style = (Style)FindResource("RoundedDatePickerStyle")
                };
                Grid.SetRow(datePicker, 4);
                Grid.SetColumn(datePicker, 1);

                // Кнопки
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Button saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                saveButton.Click += (s, args) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                        {
                            MessageBox.Show("Пожалуйста, укажите наименование медикамента", "Предупреждение");
                            return;
                        }

                        // Получаем данные из формы
                        string medicationName = nameTextBox.Text.Trim();
                        string dosage = dosageTextBox.Text.Trim();
                        string instructions = instructionsTextBox.Text.Trim();

                        // Обновляем данные в базе
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = @"
                                UPDATE PatientMedications 
                                SET MedicationName = @MedicationName,
                                    Dosage = @Dosage,
                                    Instructions = @Instructions,
                                    PrescribedDate = @PrescribedDate
                                WHERE MedicationID = @MedicationID";

                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@MedicationID", medicationID);
                                cmd.Parameters.AddWithValue("@MedicationName", medicationName);
                                cmd.Parameters.AddWithValue("@Dosage", string.IsNullOrEmpty(dosage) ? DBNull.Value : (object)dosage);
                                cmd.Parameters.AddWithValue("@Instructions", string.IsNullOrEmpty(instructions) ? DBNull.Value : (object)instructions);
                                cmd.Parameters.AddWithValue("@PrescribedDate", datePicker.SelectedDate ?? DateTime.Now);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Обновляем отображение медикаментов
                        LoadMedications();
                        MessageBox.Show("Медикамент успешно обновлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        medicationWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при обновлении медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Button cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 30,
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                cancelButton.Click += (s, args) => medicationWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 5);
                Grid.SetColumnSpan(buttonPanel, 2);

                // Добавляем элементы на форму
                grid.Children.Add(titleBlock);
                grid.Children.Add(nameLabel);
                grid.Children.Add(nameTextBox);
                grid.Children.Add(dosageLabel);
                grid.Children.Add(dosageTextBox);
                grid.Children.Add(instructionsLabel);
                grid.Children.Add(instructionsTextBox);
                grid.Children.Add(dateLabel);
                grid.Children.Add(datePicker);
                grid.Children.Add(buttonPanel);

                medicationWindow.Content = grid;
                medicationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при редактировании медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoveMedication_Click(object sender, RoutedEventArgs e)
        {
            if (dgMedications.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите медикамент(ы) для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<Tuple<string, DateTime>> selectedMedications = new List<Tuple<string, DateTime>>();
            foreach (DataRowView row in dgMedications.SelectedItems)
            {
                string medicationName = row["MedicationName"].ToString();
                DateTime prescribedDate = Convert.ToDateTime(row["PrescribedDate"]);
                selectedMedications.Add(new Tuple<string, DateTime>(medicationName, prescribedDate));
            }

            string medicationsStr = selectedMedications.Count == 1 
                ? $"медикамент '{selectedMedications[0].Item1}'" 
                : $"{selectedMedications.Count} медикаментов";

            if (MessageBox.Show($"Вы уверены, что хотите удалить {medicationsStr}?", 
                               "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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
                                foreach (var medication in selectedMedications)
                                {
                                    string medicationName = medication.Item1;
                                    DateTime prescribedDate = medication.Item2;
                                    
                                    string query = @"
                                        DELETE FROM PatientMedications 
                                        WHERE PatientID = @PatientID 
                                        AND MedicationName = @MedicationName 
                                        AND PrescribedDate = @PrescribedDate";
                                    
                                    using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                                        cmd.Parameters.AddWithValue("@MedicationName", medicationName);
                                        cmd.Parameters.AddWithValue("@PrescribedDate", prescribedDate);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                
                                transaction.Commit();
                                
                                // Обновляем отображение медикаментов
                                LoadMedications();
                                
                                string successMessage = selectedMedications.Count == 1
                                    ? "Медикамент успешно удален"
                                    : "Медикаменты успешно удалены";
                                MessageBox.Show(successMessage, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                throw ex;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении медикаментов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}