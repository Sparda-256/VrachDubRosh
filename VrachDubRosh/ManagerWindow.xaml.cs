using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace VrachDubRosh
{
    // Класс для работы с корпусами
    public class BuildingInfo
    {
        public int BuildingID { get; set; }
        public int BuildingNumber { get; set; }
        public string Description { get; set; }
        public int TotalRooms { get; set; }
        
        public override string ToString()
        {
            return BuildingNumber == 0 ? "Все корпуса" : $"Корпус {BuildingNumber}";
        }
    }

    // Класс для представления информации о размещении
    public class AccommodationInfo
    {
        public int AccommodationID { get; set; }
        public int RoomID { get; set; }
        public string RoomNumber { get; set; }
        public int BuildingID { get; set; }
        public int BuildingNumber { get; set; }
        public int BedNumber { get; set; }
        public string Status { get; set; } // "Занято", "Свободно"
        public int? PatientID { get; set; }
        public int? AccompanyingPersonID { get; set; }
        public string PersonName { get; set; } // ФИО проживающего (пациент или сопровождающий)
        public string PersonType { get; set; } // "Пациент" или "Сопровождающий"
        public DateTime? CheckInDate { get; set; }
    }

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
        private int selectedAccommodationID = -1;

        // Данные для размещения
        private List<BuildingInfo> buildings;
        private List<AccommodationInfo> accommodations;

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
            LoadBuildings();
            LoadAccommodations();
            
            // Устанавливаем значения по умолчанию для фильтров
            cbRoomStatus.SelectedIndex = 0; // "Все комнаты"
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
        
        private void LoadBuildings()
        {
            try
            {
                buildings = new List<BuildingInfo>();
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT BuildingID, BuildingNumber, Description, TotalRooms FROM Buildings ORDER BY BuildingNumber";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                buildings.Add(new BuildingInfo
                                {
                                    BuildingID = Convert.ToInt32(reader["BuildingID"]),
                                    BuildingNumber = Convert.ToInt32(reader["BuildingNumber"]),
                                    Description = reader["Description"] as string,
                                    TotalRooms = Convert.ToInt32(reader["TotalRooms"])
                                });
                            }
                        }
                    }
                }
                
                // Заполняем комбобокс для фильтрации по корпусам
                List<BuildingInfo> filterBuildings = new List<BuildingInfo>(buildings);
                // Добавляем пункт "Все корпуса"
                filterBuildings.Insert(0, new BuildingInfo { BuildingID = 0, BuildingNumber = 0, Description = "Все корпуса" });
                
                cbBuildingFilter.ItemsSource = filterBuildings;
                
                // По умолчанию выбираем "Все корпуса"
                cbBuildingFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка корпусов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadAccommodations()
        {
            try
            {
                accommodations = new List<AccommodationInfo>();
                
                int? buildingFilter = null;
                string statusFilter = "Все";
                
                if (cbBuildingFilter != null && cbBuildingFilter.SelectedItem != null)
                {
                    BuildingInfo selectedBuilding = cbBuildingFilter.SelectedItem as BuildingInfo;
                    if (selectedBuilding != null && selectedBuilding.BuildingID > 0)
                    {
                        buildingFilter = selectedBuilding.BuildingID;
                    }
                }
                
                if (cbRoomStatus != null && cbRoomStatus.SelectedItem != null)
                {
                    ComboBoxItem selectedStatus = cbRoomStatus.SelectedItem as ComboBoxItem;
                    if (selectedStatus != null)
                    {
                        statusFilter = selectedStatus.Content.ToString();
                    }
                }
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Получаем информацию о всех комнатах
                    string roomsQuery = @"
                        SELECT r.RoomID, r.RoomNumber, r.BuildingID, b.BuildingNumber
                        FROM Rooms r
                        JOIN Buildings b ON r.BuildingID = b.BuildingID
                        WHERE (@BuildingID IS NULL OR r.BuildingID = @BuildingID)
                        ORDER BY b.BuildingNumber, r.RoomNumber";
                    
                    using (SqlCommand cmd = new SqlCommand(roomsQuery, con))
                    {
                        if (buildingFilter != null)
                        {
                            cmd.Parameters.AddWithValue("@BuildingID", buildingFilter);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@BuildingID", DBNull.Value);
                        }
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int roomID = Convert.ToInt32(reader["RoomID"]);
                                string roomNumber = reader["RoomNumber"].ToString();
                                int buildingID = Convert.ToInt32(reader["BuildingID"]);
                                int buildingNumber = Convert.ToInt32(reader["BuildingNumber"]);
                                
                                // Добавляем две кровати для каждой комнаты (заполним информацию позже)
                                for (int bed = 1; bed <= 2; bed++)
                                {
                                    accommodations.Add(new AccommodationInfo
                                    {
                                        RoomID = roomID,
                                        RoomNumber = roomNumber,
                                        BuildingID = buildingID,
                                        BuildingNumber = buildingNumber,
                                        BedNumber = bed,
                                        Status = "Свободно", // По умолчанию свободно, уточним позже
                                        PersonName = "-",
                                        PersonType = "-"
                                    });
                                }
                            }
                        }
                    }
                    
                    // Получаем информацию о занятых местах (пациенты)
                    string patientAccommodationsQuery = @"
                        SELECT a.AccommodationID, a.RoomID, a.PatientID, a.BedNumber, a.CheckInDate,
                               r.RoomNumber, b.BuildingID, b.BuildingNumber, p.FullName
                        FROM Accommodations a
                        JOIN Rooms r ON a.RoomID = r.RoomID
                        JOIN Buildings b ON r.BuildingID = b.BuildingID
                        JOIN Patients p ON a.PatientID = p.PatientID
                        WHERE a.CheckOutDate IS NULL
                            AND (@BuildingID IS NULL OR b.BuildingID = @BuildingID)";
                    
                    using (SqlCommand cmd = new SqlCommand(patientAccommodationsQuery, con))
                    {
                        if (buildingFilter != null)
                        {
                            cmd.Parameters.AddWithValue("@BuildingID", buildingFilter);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@BuildingID", DBNull.Value);
                        }
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int accommodationID = Convert.ToInt32(reader["AccommodationID"]);
                                int roomID = Convert.ToInt32(reader["RoomID"]);
                                int patientID = Convert.ToInt32(reader["PatientID"]);
                                int bedNumber = Convert.ToInt32(reader["BedNumber"]);
                                DateTime checkInDate = Convert.ToDateTime(reader["CheckInDate"]);
                                string roomNumber = reader["RoomNumber"].ToString();
                                int buildingID = Convert.ToInt32(reader["BuildingID"]);
                                int buildingNumber = Convert.ToInt32(reader["BuildingNumber"]);
                                string fullName = reader["FullName"].ToString();
                                
                                // Находим соответствующее место в списке и обновляем его
                                foreach (var accommodation in accommodations)
                                {
                                    if (accommodation.RoomID == roomID && accommodation.BedNumber == bedNumber)
                                    {
                                        accommodation.AccommodationID = accommodationID;
                                        accommodation.PatientID = patientID;
                                        accommodation.Status = "Занято";
                                        accommodation.PersonName = fullName;
                                        accommodation.PersonType = "Пациент";
                                        accommodation.CheckInDate = checkInDate;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    
                    // Получаем информацию о занятых местах (сопровождающие)
                    string accompanyingAccommodationsQuery = @"
                        SELECT a.AccommodationID, a.RoomID, a.AccompanyingPersonID, a.BedNumber, a.CheckInDate,
                               r.RoomNumber, b.BuildingID, b.BuildingNumber, ap.FullName
                        FROM Accommodations a
                        JOIN Rooms r ON a.RoomID = r.RoomID
                        JOIN Buildings b ON r.BuildingID = b.BuildingID
                        JOIN AccompanyingPersons ap ON a.AccompanyingPersonID = ap.AccompanyingPersonID
                        WHERE a.CheckOutDate IS NULL
                            AND (@BuildingID IS NULL OR b.BuildingID = @BuildingID)";
                    
                    using (SqlCommand cmd = new SqlCommand(accompanyingAccommodationsQuery, con))
                    {
                        if (buildingFilter != null)
                        {
                            cmd.Parameters.AddWithValue("@BuildingID", buildingFilter);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@BuildingID", DBNull.Value);
                        }
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int accommodationID = Convert.ToInt32(reader["AccommodationID"]);
                                int roomID = Convert.ToInt32(reader["RoomID"]);
                                int accompanyingPersonID = Convert.ToInt32(reader["AccompanyingPersonID"]);
                                int bedNumber = Convert.ToInt32(reader["BedNumber"]);
                                DateTime checkInDate = Convert.ToDateTime(reader["CheckInDate"]);
                                string roomNumber = reader["RoomNumber"].ToString();
                                int buildingID = Convert.ToInt32(reader["BuildingID"]);
                                int buildingNumber = Convert.ToInt32(reader["BuildingNumber"]);
                                string fullName = reader["FullName"].ToString();
                                
                                // Находим соответствующее место в списке и обновляем его
                                foreach (var accommodation in accommodations)
                                {
                                    if (accommodation.RoomID == roomID && accommodation.BedNumber == bedNumber)
                                    {
                                        accommodation.AccommodationID = accommodationID;
                                        accommodation.AccompanyingPersonID = accompanyingPersonID;
                                        accommodation.Status = "Занято";
                                        accommodation.PersonName = fullName;
                                        accommodation.PersonType = "Сопровождающий";
                                        accommodation.CheckInDate = checkInDate;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Применяем фильтр по статусу
                List<AccommodationInfo> filteredAccommodations = new List<AccommodationInfo>();
                
                foreach (var accommodation in accommodations)
                {
                    bool includeItem = false;
                    
                    switch (statusFilter)
                    {
                        case "Все комнаты":
                            includeItem = true;
                            break;
                        case "Свободные":
                            includeItem = accommodation.Status == "Свободно";
                            break;
                        case "Заполненные":
                            // Сначала получаем все комнаты с тем же ID и проверяем, что все кровати заняты
                            includeItem = accommodation.Status == "Занято";
                            if (includeItem)
                            {
                                // Проверяем, что все кровати в этой комнате заняты
                                bool allOccupied = true;
                                foreach (var acc in accommodations)
                                {
                                    if (acc.RoomID == accommodation.RoomID && acc.Status == "Свободно")
                                    {
                                        allOccupied = false;
                                        break;
                                    }
                                }
                                includeItem = allOccupied;
                            }
                            break;
                        case "Частично заполненные":
                            // Находим комнаты, где есть хотя бы одно занятое место и хотя бы одно свободное
                            bool hasOccupied = false;
                            bool hasFree = false;
                            
                            foreach (var acc in accommodations)
                            {
                                if (acc.RoomID == accommodation.RoomID)
                                {
                                    if (acc.Status == "Занято")
                                        hasOccupied = true;
                                    else
                                        hasFree = true;
                                    
                                    if (hasOccupied && hasFree)
                                        break;
                                }
                            }
                            
                            includeItem = hasOccupied && hasFree;
                            break;
                    }
                    
                    if (includeItem)
                    {
                        filteredAccommodations.Add(accommodation);
                    }
                }
                
                // Обновляем интерфейс
                dgAccommodation.ItemsSource = filteredAccommodations;
                
                // Скрываем или показываем колонку с корпусом в зависимости от фильтра
                if (dgAccommodation.Columns.Count > 0)
                {
                    // Первая колонка - это колонка с корпусом
                    dgAccommodation.Columns[0].Visibility = buildingFilter.HasValue 
                        ? Visibility.Collapsed 
                        : Visibility.Visible;
                }
                
                // Подсчитываем статистику
                int totalRooms = accommodations.Count / 2; // Две кровати в комнате
                int occupiedBeds = accommodations.Where(a => a.Status == "Занято").Count();
                int availableBeds = accommodations.Count - occupiedBeds;
                
                txtTotalRooms.Text = totalRooms.ToString();
                txtOccupiedBeds.Text = occupiedBeds.ToString();
                txtAvailableBeds.Text = availableBeds.ToString();
                
                // Сбрасываем выбранное размещение
                selectedAccommodationID = -1;
                
                // Конфигурируем кнопки по умолчанию
                btnCheckOut.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке информации о размещении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void cbBuildingFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAccommodations();
        }
        
        private void cbRoomStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAccommodations();
        }
        
        private void dgAccommodation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Очищаем предыдущий выбор
            selectedAccommodationID = -1;
            
            // Проверяем выбранные элементы
            bool hasSelectedFree = false;
            bool hasSelectedOccupied = false;
            
            foreach (var item in dgAccommodation.SelectedItems)
            {
                if (item is AccommodationInfo info)
                {
                    // Если выбран один элемент, запоминаем его ID
                    if (dgAccommodation.SelectedItems.Count == 1)
                    {
                        selectedAccommodationID = info.AccommodationID;
                    }
                    
                    // Проверяем статус для включения/выключения кнопок
                    if (info.Status == "Свободно")
                    {
                        hasSelectedFree = true;
                    }
                    else if (info.Status == "Занято")
                    {
                        hasSelectedOccupied = true;
                    }
                }
            }
            
            // Включаем/выключаем кнопки в зависимости от выбора
            
            // Если среди выбранных элементов есть хотя бы одно занятое место, разрешаем выселение
            btnCheckOut.IsEnabled = hasSelectedOccupied;
        }
                
        private void btnCheckOut_Click(object sender, RoutedEventArgs e)
        {
            if (dgAccommodation.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите занятые места для выселения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Проверяем, что все выбранные места заняты
            bool allOccupied = true;
            foreach (var item in dgAccommodation.SelectedItems)
            {
                if (item is AccommodationInfo info && info.Status != "Занято")
                {
                    allOccupied = false;
                    break;
                }
            }
            
            if (!allOccupied)
            {
                MessageBox.Show("Для выселения можно выбрать только занятые места.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Формируем сообщение в зависимости от количества выбранных мест
            string message = dgAccommodation.SelectedItems.Count == 1
                ? "Вы уверены, что хотите выселить проживающего из выбранного места?"
                : $"Вы уверены, что хотите выселить проживающих из {dgAccommodation.SelectedItems.Count} выбранных мест?";
            
            if (MessageBox.Show(message, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                // Счетчик выселенных мест
                int checkoutCount = 0;
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    foreach (AccommodationInfo info in dgAccommodation.SelectedItems)
                    {
                        if (info.Status == "Занято" && info.AccommodationID > 0)
                        {
                            string query = @"
                                UPDATE Accommodations 
                                SET CheckOutDate = @CheckOutDate 
                                WHERE AccommodationID = @AccommodationID";
                            
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@CheckOutDate", DateTime.Now);
                                cmd.Parameters.AddWithValue("@AccommodationID", info.AccommodationID);
                                cmd.ExecuteNonQuery();
                                checkoutCount++;
                            }
                        }
                    }
                }
                
                string resultMessage = checkoutCount == 1 
                    ? "Выселение выполнено успешно."
                    : $"Успешно выселено мест: {checkoutCount}";
                
                MessageBox.Show(resultMessage, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Обновляем список размещений
                LoadAccommodations();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выселении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
               
        private void btnRefreshAccommodation_Click(object sender, RoutedEventArgs e)
        {
            LoadAccommodations();
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
            // Очищаем предыдущий выбор
            selectedPatientID = -1;
            
            // Проверяем, есть ли выбранные элементы
            if (dgPatients.SelectedItems.Count > 0)
            {
                // Если выбран один элемент, запоминаем его ID
                if (dgPatients.SelectedItems.Count == 1 && dgPatients.SelectedItem is DataRowView row)
                {
                    selectedPatientID = Convert.ToInt32(row["PatientID"]);
                }
            }
        }
        
        private void dgAccompanying_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Очищаем предыдущий выбор
            selectedAccompanyingPersonID = -1;
            
            // Проверяем, есть ли выбранные элементы
            if (dgAccompanying.SelectedItems.Count > 0)
            {
                // Если выбран один элемент, запоминаем его ID
                if (dgAccompanying.SelectedItems.Count == 1 && dgAccompanying.SelectedItem is DataRowView row)
                {
                    selectedAccompanyingPersonID = Convert.ToInt32(row["AccompanyingPersonID"]);
                }
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
            // Проверяем, выбран ли хотя бы один пациент
            if (dgPatients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите пациента для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Редактирование работает только с одним пациентом за раз
            if (dgPatients.SelectedItems.Count > 1)
            {
                MessageBox.Show("Пожалуйста, выберите только одного пациента для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
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
            // Проверяем, выбран ли хотя бы один пациент
            if (dgPatients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите пациентов для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Если выбрано несколько пациентов, подтверждаем удаление
            string message = dgPatients.SelectedItems.Count == 1 
                ? "Вы уверены, что хотите удалить выбранного пациента?\nТакже будут удалены все документы и связанные сопровождающие лица."
                : $"Вы уверены, что хотите удалить {dgPatients.SelectedItems.Count} выбранных пациентов?\nТакже будут удалены все документы и связанные сопровождающие лица.";
            
            if (MessageBox.Show(message, 
                               "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                // Счетчик удаленных пациентов
                int deletedCount = 0;
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    foreach (DataRowView row in dgPatients.SelectedItems)
                    {
                        int patientID = Convert.ToInt32(row["PatientID"]);
                        
                        using (SqlTransaction tran = con.BeginTransaction())
                        {
                            try
                            {
                                // Удаление записей о размещении пациента
                                string deleteAccommodationsQuery = "DELETE FROM Accommodations WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deleteAccommodationsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }

                                // Удаление документов сопровождающих лиц
                                string deleteAccompanyingDocsQuery = @"
                                    DELETE FROM AccompanyingPersonDocuments
                                    WHERE AccompanyingPersonID IN (
                                        SELECT AccompanyingPersonID FROM AccompanyingPersons
                                        WHERE PatientID = @PatientID
                                    )";
                                using (SqlCommand cmd = new SqlCommand(deleteAccompanyingDocsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                // Удаление записей о размещении сопровождающих
                                string deleteAccompanyingAccommodationsQuery = @"
                                    DELETE FROM Accommodations 
                                    WHERE AccompanyingPersonID IN (
                                        SELECT AccompanyingPersonID FROM AccompanyingPersons
                                        WHERE PatientID = @PatientID
                                    )";
                                using (SqlCommand cmd = new SqlCommand(deleteAccompanyingAccommodationsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                // Удаление сопровождающих лиц
                                string deleteAccompanyingQuery = "DELETE FROM AccompanyingPersons WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deleteAccompanyingQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                // Удаление документов пациента
                                string deleteDocsQuery = "DELETE FROM PatientDocuments WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deleteDocsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                // Удаление пациента из связанных таблиц
                                string deleteAssignmentsQuery = "DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deleteAssignmentsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                string deletePatientDiagnosesQuery = "DELETE FROM PatientDiagnoses WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deletePatientDiagnosesQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                string deletePatientDescriptionsQuery = "DELETE FROM PatientDescriptions WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deletePatientDescriptionsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                string deleteProcedureAppointmentsQuery = "DELETE FROM ProcedureAppointments WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deleteProcedureAppointmentsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                // Удаление самого пациента
                                string deletePatientQuery = "DELETE FROM Patients WHERE PatientID = @PatientID";
                                using (SqlCommand cmd = new SqlCommand(deletePatientQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                tran.Commit();
                                deletedCount++;
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                throw new Exception($"Ошибка при удалении пациента (ID: {patientID}): {ex.Message}");
                            }
                        }
                    }
                }
                
                string resultMessage = deletedCount == 1 
                    ? "Пациент успешно удален."
                    : $"Успешно удалено пациентов: {deletedCount}";
                
                MessageBox.Show(resultMessage, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoadPatients();
                LoadAccompanyingPersons();
                LoadAccommodations();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении пациентов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnManageDocuments_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, выбран ли хотя бы один пациент
            if (dgPatients.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите пациента для управления документами.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Управление документами работает только с одним пациентом за раз
            if (dgPatients.SelectedItems.Count > 1)
            {
                MessageBox.Show("Пожалуйста, выберите только одного пациента для управления документами.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем имя пациента
            string patientName = "";
            DateTime? dateOfBirth = null;
            int patientID = -1;
            
            if (dgPatients.SelectedItem is DataRowView row)
            {
                patientName = row["FullName"].ToString();
                dateOfBirth = row["DateOfBirth"] as DateTime?;
                patientID = Convert.ToInt32(row["PatientID"]);
            }
            
            PatientDocumentsWindow documentsWindow = new PatientDocumentsWindow(patientID, patientName, dateOfBirth);
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
            // Проверяем, выбран ли хотя бы один сопровождающий
            if (dgAccompanying.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите сопровождающего для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Редактирование работает только с одним сопровождающим за раз
            if (dgAccompanying.SelectedItems.Count > 1)
            {
                MessageBox.Show("Пожалуйста, выберите только одного сопровождающего для редактирования.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
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
            // Проверяем, выбран ли хотя бы один сопровождающий
            if (dgAccompanying.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите сопровождающих для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Если выбрано несколько сопровождающих, подтверждаем удаление
            string message = dgAccompanying.SelectedItems.Count == 1 
                ? "Вы уверены, что хотите удалить выбранного сопровождающего?\nТакже будут удалены все связанные документы."
                : $"Вы уверены, что хотите удалить {dgAccompanying.SelectedItems.Count} выбранных сопровождающих?\nТакже будут удалены все связанные документы.";
            
            if (MessageBox.Show(message, 
                               "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                // Счетчик удаленных сопровождающих
                int deletedCount = 0;
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    foreach (DataRowView row in dgAccompanying.SelectedItems)
                    {
                        int accompanyingPersonID = Convert.ToInt32(row["AccompanyingPersonID"]);
                        
                        using (SqlTransaction tran = con.BeginTransaction())
                        {
                            try
                            {
                                // Удаление записей о размещении сопровождающего
                                string deleteAccommodationsQuery = "DELETE FROM Accommodations WHERE AccompanyingPersonID = @AccompanyingPersonID";
                                using (SqlCommand cmd = new SqlCommand(deleteAccommodationsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                                    cmd.ExecuteNonQuery();
                                }

                                // Удаление документов сопровождающего лица
                                string deleteDocsQuery = "DELETE FROM AccompanyingPersonDocuments WHERE AccompanyingPersonID = @AccompanyingPersonID";
                                using (SqlCommand cmd = new SqlCommand(deleteDocsQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                // Удаление самого сопровождающего
                                string deleteQuery = "DELETE FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID";
                                using (SqlCommand cmd = new SqlCommand(deleteQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                                    cmd.ExecuteNonQuery();
                                }
                                
                                tran.Commit();
                                deletedCount++;
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                throw new Exception($"Ошибка при удалении сопровождающего (ID: {accompanyingPersonID}): {ex.Message}");
                            }
                        }
                    }
                }
                
                string resultMessage = deletedCount == 1 
                    ? "Сопровождающий успешно удален."
                    : $"Успешно удалено сопровождающих: {deletedCount}";
                
                MessageBox.Show(resultMessage, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoadAccompanyingPersons();
                LoadAccommodations();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении сопровождающих: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnManageAccompanyingDocuments_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, выбран ли хотя бы один сопровождающий
            if (dgAccompanying.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите сопровождающего для управления документами.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Управление документами работает только с одним сопровождающим за раз
            if (dgAccompanying.SelectedItems.Count > 1)
            {
                MessageBox.Show("Пожалуйста, выберите только одного сопровождающего для управления документами.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем данные о сопровождающем
            string accompaningName = "";
            int patientID = -1;
            string patientName = "";
            int accompanyingID = -1;
            
            if (dgAccompanying.SelectedItem is DataRowView row)
            {
                accompaningName = row["FullName"].ToString();
                patientID = Convert.ToInt32(row["PatientID"]);
                patientName = row["PatientName"].ToString();
                accompanyingID = Convert.ToInt32(row["AccompanyingPersonID"]);
            }
            
            AccompanyingDocumentsWindow documentsWindow = new AccompanyingDocumentsWindow(accompanyingID, accompaningName, patientID, patientName);
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