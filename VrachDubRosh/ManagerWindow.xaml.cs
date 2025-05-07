using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Data;
using WinForms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using Application = System.Windows.Application;
using System.Globalization;
using System.Windows.Media;

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

    // Класс для представления информации о документах
    public class DocumentInfo
    {
        public int DocumentID { get; set; }
        public string DocumentName { get; set; }
        public string Category { get; set; }
        public string FileType { get; set; }
        public string DisplayFileType { get; set; }
        public string FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string UploadedBy { get; set; }
        public string FilePath { get; set; }
        public byte[] FileData { get; set; }
    }

    public class DocumentStatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            if (status == "Все документы загружены")
            {
                // Галочка
                return "M9,16.17L4.83,12l-1.42,1.41L9,19 21,7l-1.41,-1.41z";
            }
            else
            {
                // Крестик
                return "M19,6.41L17.59,5 12,10.59 6.41,5 5,6.41 10.59,12 5,17.59 6.41,19 12,13.41 17.59,19 19,17.59 13.41,12z";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DocumentStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            if (status == "Все документы загружены")
            {
                return new SolidColorBrush(Colors.Green);
            }
            else
            {
                return new SolidColorBrush(Colors.Red);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class ManagerWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private bool isDarkTheme = false;
        
        // Кэшированные таблицы для фильтрации
        private DataTable dtPatients;
        private DataTable dtAccompanying;
        private List<DocumentInfo> documents;
        
        // Выбранные ID
        private int selectedPatientID = -1;
        private int selectedAccompanyingPersonID = -1;
        private int selectedAccommodationID = -1;
        private int selectedDocumentID = -1;

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
            
            // Организуем существующие документы по категориям
            OrganizeExistingDocuments();
            
            // Загружаем данные при инициализации окна
            Loaded += ManagerWindow_Loaded;
        }
        
        private void ManagerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Убедимся, что все элементы UI проинициализированы
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                LoadPatients();
                LoadAccompanyingPersons();
                LoadBuildings();
                LoadAccommodations();
                LoadDocuments();
                
                // Устанавливаем значения по умолчанию для фильтров
                cbRoomStatus.SelectedIndex = 0; // "Все комнаты"
            }));
        }

        #region Загрузка данных
        
        public void LoadPatients()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT p.*, 
                            CASE 
                                WHEN EXISTS (
                                    SELECT 1 
                                    FROM DocumentTypes dt 
                                    WHERE dt.IsRequired = 1 
                                    AND dt.ForAccompanyingPerson = 0
                                    AND (dt.MinimumAge IS NULL OR DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) >= dt.MinimumAge)
                                    AND (dt.MaximumAge IS NULL OR DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) <= dt.MaximumAge)
                                    AND NOT EXISTS (
                                        SELECT 1 
                                        FROM PatientDocuments pd 
                                        WHERE pd.PatientID = p.PatientID 
                                        AND pd.DocumentTypeID = dt.DocumentTypeID
                                    )
                                ) THEN 'Не все документы загружены'
                                ELSE 'Все документы загружены'
                            END as DocumentsStatus
                        FROM Patients p";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    dtPatients = new DataTable();
                    adapter.Fill(dtPatients);
                    dgPatients.ItemsSource = dtPatients.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пациентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public void LoadAccompanyingPersons()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                        SELECT ap.*, p.FullName as PatientName,
                            CASE 
                                WHEN EXISTS (
                                    SELECT 1 
                                    FROM DocumentTypes dt 
                                    WHERE dt.IsRequired = 1 
                                    AND dt.ForAccompanyingPerson = 1
                                    AND NOT EXISTS (
                                        SELECT 1 
                                        FROM AccompanyingPersonDocuments apd 
                                        WHERE apd.AccompanyingPersonID = ap.AccompanyingPersonID 
                                        AND apd.DocumentTypeID = dt.DocumentTypeID
                                    )
                                ) THEN 'Не все документы загружены'
                                ELSE 'Все документы загружены'
                            END as DocumentsStatus
                        FROM AccompanyingPersons ap
                        JOIN Patients p ON ap.PatientID = p.PatientID";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    dtAccompanying = new DataTable();
                    adapter.Fill(dtAccompanying);
                    dgAccompanying.ItemsSource = dtAccompanying.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке сопровождающих: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        #region Документы
        
        private void LoadDocuments()
        {
            try
            {
                documents = new List<DocumentInfo>();
                
                string categoryFilter = "Все документы";
                if (cmbDocumentCategory != null && cmbDocumentCategory.SelectedItem != null)
                {
                    ComboBoxItem selectedCategory = cmbDocumentCategory.SelectedItem as ComboBoxItem;
                    if (selectedCategory != null)
                    {
                        categoryFilter = selectedCategory.Content.ToString();
                    }
                }
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    string query = @"
                        SELECT d.DocumentID, d.DocumentName, d.Category, d.FileType, 
                               d.FileSizeBytes, d.UploadDate, d.UploadedBy, d.FilePath
                        FROM ManagerDocuments d
                        WHERE (@Category = 'Все документы' OR d.Category = @Category)
                        ORDER BY d.UploadDate DESC";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Category", categoryFilter);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                long fileSizeBytes = Convert.ToInt64(reader["FileSizeBytes"]);
                                string fileSizeFormatted = FormatFileSize(fileSizeBytes);
                                
                                documents.Add(new DocumentInfo
                                {
                                    DocumentID = Convert.ToInt32(reader["DocumentID"]),
                                    DocumentName = reader["DocumentName"].ToString(),
                                    Category = reader["Category"].ToString(),
                                    FileType = reader["FileType"].ToString(),
                                    DisplayFileType = GetDisplayFileType(reader["FileType"].ToString()),
                                    FileSize = fileSizeFormatted,
                                    UploadDate = Convert.ToDateTime(reader["UploadDate"]),
                                    UploadedBy = reader["UploadedBy"].ToString(),
                                    FilePath = reader["FilePath"].ToString()
                                });
                            }
                        }
                    }
                }
                
                // Применяем фильтр поиска, если он есть
                if (txtSearchDocuments != null && !string.IsNullOrEmpty(txtSearchDocuments.Text))
                {
                    string searchText = txtSearchDocuments.Text.ToLower();
                    documents = documents.Where(d => 
                        d.DocumentName.ToLower().Contains(searchText) || 
                        d.Category.ToLower().Contains(searchText) ||
                        d.FileType.ToLower().Contains(searchText) ||
                        d.UploadedBy.ToLower().Contains(searchText)).ToList();
                }
                
                // Обновляем интерфейс
                if (dgDocuments != null)
                {
                    dgDocuments.ItemsSource = documents;
                }
                
                // Сбрасываем выбранный документ
                selectedDocumentID = -1;
                
                // Конфигурируем кнопки по умолчанию
                UpdateDocumentButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке документов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Форматирование размера файла в читаемый вид
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
        
        private string GetDisplayFileType(string fileType)
        {
            switch (fileType.ToLower())
            {
                case "docx":
                    return "Документ Word";
                case "xlsx":
                    return "Электронная таблица";
                case "pdf":
                    return "PDF документ";
                case "png":
                    return "Изображение";
                case "jpg":
                case "jpeg":
                    return "Изображение";
                case "gif":
                    return "Изображение";
                case "txt":
                    return "Текстовый документ";
                case "ppt":
                case "pptx":
                    return "Презентация";
                default:
                    return fileType.Replace(".", "").ToUpper();
            }
        }
        
        private void UpdateDocumentButtonStates()
        {
            if (dgDocuments == null) return;
            
            bool hasSelectedDocument = selectedDocumentID > 0;
            
            // В реальном окне настраиваем доступность кнопок в зависимости от состояния
            if (dgDocuments.SelectedItems.Count > 0)
            {
                // Доступ к кнопкам в зависимости от количества выбранных элементов
                bool isSingleSelection = dgDocuments.SelectedItems.Count == 1;
                
                // Кнопки доступны, если выбран хотя бы один документ
                var viewButton = this.FindName("btnViewDocument") as Button;
                var downloadButton = this.FindName("btnDownloadDocument") as Button;
                var printButton = this.FindName("btnPrintDocument") as Button;
                var deleteButton = this.FindName("btnDeleteDocument") as Button;
                
                if (viewButton != null) viewButton.IsEnabled = isSingleSelection;
                if (downloadButton != null) downloadButton.IsEnabled = true;
                if (printButton != null) printButton.IsEnabled = isSingleSelection;
                if (deleteButton != null) deleteButton.IsEnabled = true;
            }
            else
            {
                // Если ничего не выбрано, отключаем все кнопки
                var viewButton = this.FindName("btnViewDocument") as Button;
                var downloadButton = this.FindName("btnDownloadDocument") as Button;
                var printButton = this.FindName("btnPrintDocument") as Button;
                var deleteButton = this.FindName("btnDeleteDocument") as Button;
                
                if (viewButton != null) viewButton.IsEnabled = false;
                if (downloadButton != null) downloadButton.IsEnabled = false;
                if (printButton != null) printButton.IsEnabled = false;
                if (deleteButton != null) deleteButton.IsEnabled = false;
            }
        }
        
        private void txtSearchDocuments_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDocuments();
        }
        
        private void cmbDocumentCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDocuments();
        }
        
        private void dgDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Очищаем предыдущий выбор
            selectedDocumentID = -1;
            
            // Проверяем, есть ли выбранные элементы
            if (dgDocuments.SelectedItems.Count > 0)
            {
                // Если выбран один элемент, запоминаем его ID
                if (dgDocuments.SelectedItems.Count == 1 && dgDocuments.SelectedItem is DocumentInfo doc)
                {
                    selectedDocumentID = doc.DocumentID;
                }
            }
            
            UpdateDocumentButtonStates();
        }
        
        private void dgDocuments_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedDocumentID > 0)
            {
                btnViewDocument_Click(sender, e);
            }
        }
        
        private void btnUploadDocument_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалог выбора файла
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Все файлы (*.*)|*.*|Документы Word (*.docx;*.doc)|*.docx;*.doc|PDF файлы (*.pdf)|*.pdf|Изображения (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;
            
            if (openFileDialog.ShowDialog() == true)
            {
                // Получаем информацию о выбранном файле
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                string fileExtension = Path.GetExtension(filePath).ToLower();
                long fileSize = new FileInfo(filePath).Length;
                
                // Загружаем файл в память
                byte[] fileData = File.ReadAllBytes(filePath);
                
                // Определяем тип файла на основе расширения для хранения в БД (без точки)
                string fileExtensionWithoutDot = fileExtension.TrimStart('.');
                
                // Определяем отображаемый тип файла для пользовательского интерфейса
                string displayFileType = GetDisplayFileType(fileExtensionWithoutDot);
                
                // Открываем диалог для добавления информации о документе
                UploadDocumentWindow uploadWindow = new UploadDocumentWindow(fileName, displayFileType);
                
                if (uploadWindow.ShowDialog() == true)
                {
                    try
                    {
                        // Базовая директория для хранения документов
                        string baseDocumentsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
                        
                        // Создаем базовую директорию, если она не существует
                        if (!Directory.Exists(baseDocumentsDirectory))
                        {
                            Directory.CreateDirectory(baseDocumentsDirectory);
                        }
                        
                        // Создаем директорию для категории документа
                        string categoryDirectory = Path.Combine(baseDocumentsDirectory, uploadWindow.SelectedCategory);
                        if (!Directory.Exists(categoryDirectory))
                        {
                            Directory.CreateDirectory(categoryDirectory);
                        }
                        
                        // Создаем имя файла из названия документа и расширения исходного файла
                        string safeDocumentName = string.Join("_", uploadWindow.DocumentName.Split(Path.GetInvalidFileNameChars()));
                        string uniqueFileName = $"{safeDocumentName}{fileExtension}";
                        string destinationPath = Path.Combine(categoryDirectory, uniqueFileName);
                        
                        // Проверяем, существует ли уже файл с таким именем
                        if (File.Exists(destinationPath))
                        {
                            // Если файл существует, добавляем порядковый номер
                            int counter = 1;
                            string fileNameWithoutExt = safeDocumentName;
                            
                            while (File.Exists(destinationPath))
                            {
                                uniqueFileName = $"{fileNameWithoutExt}_{counter}{fileExtension}";
                                destinationPath = Path.Combine(categoryDirectory, uniqueFileName);
                                counter++;
                            }
                        }
                        
                        // Сохраняем файл в соответствующую категорию
                        File.WriteAllBytes(destinationPath, fileData);
                        
                        // Получаем имя пользователя (в реальном приложении следует использовать имя текущего пользователя)
                        string currentUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                        
                        // Сохраняем информацию о документе в базу данных
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            
                            string query = @"
                                INSERT INTO ManagerDocuments (DocumentName, Category, FileType, FileSizeBytes, UploadDate, UploadedBy, FilePath)
                                VALUES (@DocumentName, @Category, @FileType, @FileSizeBytes, @UploadDate, @UploadedBy, @FilePath);
                                SELECT SCOPE_IDENTITY();";
                            
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@DocumentName", uploadWindow.DocumentName);
                                cmd.Parameters.AddWithValue("@Category", uploadWindow.SelectedCategory);
                                cmd.Parameters.AddWithValue("@FileType", fileExtensionWithoutDot);
                                cmd.Parameters.AddWithValue("@FileSizeBytes", fileSize);
                                cmd.Parameters.AddWithValue("@UploadDate", DateTime.Now);
                                cmd.Parameters.AddWithValue("@UploadedBy", currentUser);
                                cmd.Parameters.AddWithValue("@FilePath", destinationPath);
                                
                                // Получаем ID добавленного документа
                                int newDocumentID = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }
                        
                        MessageBox.Show("Документ успешно загружен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Обновляем список документов
                        LoadDocuments();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при загрузке документа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private void btnViewDocument_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDocumentID <= 0)
            {
                MessageBox.Show("Выберите документ для просмотра.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем выбранный документ
            DocumentInfo selectedDocument = documents.FirstOrDefault(d => d.DocumentID == selectedDocumentID);
            
            if (selectedDocument == null)
            {
                MessageBox.Show("Документ не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                // Проверяем, существует ли файл
                if (!File.Exists(selectedDocument.FilePath))
                {
                    MessageBox.Show("Файл документа не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Открываем файл с помощью ассоциированной программы
                Process.Start(selectedDocument.FilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии документа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnDownloadDocument_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, выбран ли хотя бы один документ
            if (dgDocuments.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите документы для скачивания.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                // Если выбран один документ, предлагаем выбрать место сохранения
                if (dgDocuments.SelectedItems.Count == 1)
                {
                    DocumentInfo selectedDocument = dgDocuments.SelectedItem as DocumentInfo;
                    
                    // Проверяем, существует ли файл
                    if (!File.Exists(selectedDocument.FilePath))
                    {
                        MessageBox.Show("Файл документа не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Обеспечиваем, что у имени файла есть правильное расширение
                    string suggestedFileName = selectedDocument.DocumentName;
                    string fileExtension = selectedDocument.FileType.ToLower();
                    
                    if (!suggestedFileName.ToLower().EndsWith($".{fileExtension}"))
                    {
                        suggestedFileName = $"{suggestedFileName}.{fileExtension}";
                    }
                    
                    // Открываем диалог сохранения файла
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = suggestedFileName;
                    saveFileDialog.Filter = $"Файлы {fileExtension} (*.{fileExtension})|*.{fileExtension}|Все файлы (*.*)|*.*";
                    
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        // Копируем файл в выбранное место
                        File.Copy(selectedDocument.FilePath, saveFileDialog.FileName, true);
                        MessageBox.Show("Документ успешно сохранен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Если выбрано несколько документов, предлагаем выбрать папку
                    var folderDialog = new WinForms.FolderBrowserDialog();
                    folderDialog.Description = "Выберите папку для сохранения выбранных документов";
                    
                    if (folderDialog.ShowDialog() == WinForms.DialogResult.OK)
                    {
                        string destinationFolder = folderDialog.SelectedPath;
                        int savedCount = 0;
                        int errorCount = 0;
                        
                        foreach (DocumentInfo document in dgDocuments.SelectedItems)
                        {
                            try
                            {
                                // Проверяем, существует ли файл
                                if (File.Exists(document.FilePath))
                                {
                                    // Обеспечиваем, что у имени файла есть правильное расширение
                                    string fileName = document.DocumentName;
                                    string fileExtension = document.FileType.ToLower();
                                    
                                    if (!fileName.ToLower().EndsWith($".{fileExtension}"))
                                    {
                                        fileName = $"{fileName}.{fileExtension}";
                                    }
                                    
                                    // Создаем уникальное имя файла, если файл с таким именем уже существует
                                    string destinationPath = Path.Combine(destinationFolder, fileName);
                                    int counter = 1;
                                    
                                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                                    string extension = Path.GetExtension(fileName);
                                    
                                    while (File.Exists(destinationPath))
                                    {
                                        destinationPath = Path.Combine(destinationFolder, $"{fileNameWithoutExt}_{counter}{extension}");
                                        counter++;
                                    }
                                    
                                    // Копируем файл в выбранную папку
                                    File.Copy(document.FilePath, destinationPath);
                                    savedCount++;
                                }
                                else
                                {
                                    errorCount++;
                                }
                            }
                            catch
                            {
                                errorCount++;
                            }
                        }
                        
                        string resultMessage = $"Документов сохранено: {savedCount}";
                        if (errorCount > 0)
                        {
                            resultMessage += $"\nНе удалось сохранить документов: {errorCount}";
                        }
                        
                        MessageBox.Show(resultMessage, "Результат", MessageBoxButton.OK, 
                                       errorCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при скачивании документов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnPrintDocument_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDocumentID <= 0)
            {
                MessageBox.Show("Выберите документ для печати.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем выбранный документ
            DocumentInfo selectedDocument = documents.FirstOrDefault(d => d.DocumentID == selectedDocumentID);
            
            if (selectedDocument == null)
            {
                MessageBox.Show("Документ не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                // Проверяем, существует ли файл
                if (!File.Exists(selectedDocument.FilePath))
                {
                    MessageBox.Show("Файл документа не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Вызываем печать через ассоциированное приложение
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = $"printui.dll,PrintUIEntry /p /n \"{selectedDocument.FilePath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                
                Process.Start(psi);
                
                MessageBox.Show("Документ отправлен на печать.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при печати документа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnDeleteDocument_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, выбран ли хотя бы один документ
            if (dgDocuments.SelectedItems.Count == 0)
            {
                MessageBox.Show("Выберите документы для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Создаем сообщение с подтверждением в зависимости от количества выбранных документов
            string message = dgDocuments.SelectedItems.Count == 1
                ? "Вы уверены, что хотите удалить выбранный документ?"
                : $"Вы уверены, что хотите удалить {dgDocuments.SelectedItems.Count} выбранных документов?";
            
            if (MessageBox.Show(message, "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                int deletedCount = 0;
                // Словарь для отслеживания категорий, из которых были удалены документы
                Dictionary<string, bool> modifiedCategories = new Dictionary<string, bool>();
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    foreach (DocumentInfo document in dgDocuments.SelectedItems)
                    {
                        // Сначала удаляем физический файл, если он существует
                        if (File.Exists(document.FilePath))
                        {
                            try
                            {
                                // Получаем директорию, в которой находится файл (должна быть категория)
                                string documentDirectory = Path.GetDirectoryName(document.FilePath);
                                string category = Path.GetFileName(documentDirectory);
                                
                                // Добавляем категорию в список для проверки
                                if (!modifiedCategories.ContainsKey(category))
                                {
                                    modifiedCategories[category] = true;
                                }
                                
                                File.Delete(document.FilePath);
                            }
                            catch (Exception ex)
                            {
                                // Логируем ошибку, но продолжаем удаление записи из БД
                                Console.WriteLine($"Ошибка удаления файла: {ex.Message}");
                            }
                        }
                        
                        // Удаляем запись из базы данных
                        string query = "DELETE FROM ManagerDocuments WHERE DocumentID = @DocumentID";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@DocumentID", document.DocumentID);
                            cmd.ExecuteNonQuery();
                            deletedCount++;
                        }
                    }
                }
                
                // Проверяем и удаляем пустые директории категорий
                string baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
                foreach (var category in modifiedCategories.Keys)
                {
                    string categoryPath = Path.Combine(baseDirectory, category);
                    if (Directory.Exists(categoryPath) && !Directory.EnumerateFileSystemEntries(categoryPath).Any())
                    {
                        try
                        {
                            Directory.Delete(categoryPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Не удалось удалить пустую директорию {categoryPath}: {ex.Message}");
                        }
                    }
                }
                
                string resultMessage = deletedCount == 1
                    ? "Документ успешно удален."
                    : $"Успешно удалено документов: {deletedCount}";
                
                MessageBox.Show(resultMessage, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Обновляем список документов
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении документов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        // Метод для организации существующих документов по категориям
        private void OrganizeExistingDocuments()
        {
            try
            {
                string baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
                
                // Если директории не существует, просто выходим
                if (!Directory.Exists(baseDirectory))
                {
                    return;
                }
                
                // Получаем все файлы из корневой директории Documents (без файлов из поддиректорий)
                string[] files = Directory.GetFiles(baseDirectory, "*.*", SearchOption.TopDirectoryOnly);
                
                if (files.Length == 0)
                {
                    return; // Нет файлов для организации
                }
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    foreach (string filePath in files)
                    {
                        // Пытаемся получить категорию файла из базы данных
                        string query = "SELECT DocumentID, Category FROM ManagerDocuments WHERE FilePath = @FilePath";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@FilePath", filePath);
                            
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int documentId = Convert.ToInt32(reader["DocumentID"]);
                                    string category = reader["Category"].ToString();
                                    
                                    // Закрываем reader перед выполнением других операций
                                    reader.Close();
                                    
                                    // Создаем директорию категории, если она не существует
                                    string categoryDirectory = Path.Combine(baseDirectory, category);
                                    if (!Directory.Exists(categoryDirectory))
                                    {
                                        Directory.CreateDirectory(categoryDirectory);
                                    }
                                    
                                    // Создаем новый путь для файла
                                    string fileName = Path.GetFileName(filePath);
                                    string newFilePath = Path.Combine(categoryDirectory, fileName);
                                    
                                    // Перемещаем файл, если новый путь отличается от текущего
                                    if (filePath != newFilePath)
                                    {
                                        // Если файл с таким именем уже существует, генерируем уникальное имя
                                        if (File.Exists(newFilePath))
                                        {
                                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                                            string extension = Path.GetExtension(fileName);
                                            newFilePath = Path.Combine(categoryDirectory, $"{fileNameWithoutExt}_{Guid.NewGuid().ToString().Substring(0, 8)}{extension}");
                                        }
                                        
                                        // Перемещаем файл
                                        File.Move(filePath, newFilePath);
                                        
                                        // Обновляем путь в базе данных
                                        string updateQuery = "UPDATE ManagerDocuments SET FilePath = @NewFilePath WHERE DocumentID = @DocumentID";
                                        
                                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                                        {
                                            updateCmd.Parameters.AddWithValue("@NewFilePath", newFilePath);
                                            updateCmd.Parameters.AddWithValue("@DocumentID", documentId);
                                            updateCmd.ExecuteNonQuery();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Просто логируем ошибку и продолжаем работу
                Console.WriteLine("Ошибка при организации документов: " + ex.Message);
            }
        }
    }
} 