using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace VrachDubRosh
{
    // Classes for Room and Building moved to Models.cs

    public partial class AddEditPatientWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private readonly int patientID;
        private readonly bool isEditMode;
        private readonly Window owner;
        private List<Building> buildings;
        private List<Room> rooms;
        private int selectedBuildingID = -1;
        private int selectedRoomID = -1;
        private int selectedBedNumber = 1; // По умолчанию кровать 1

        // Конструктор для добавления нового пациента
        public AddEditPatientWindow(Window owner)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = false;
            patientID = -1;
            Title = "Добавление пациента";
            
            // Устанавливаем текущую дату
            dpRecordDate.SelectedDate = DateTime.Today;
            // По умолчанию выбираем мужской пол
            cbGender.SelectedIndex = 0;
            // По умолчанию выбираем дневной стационар
            cbStayType.SelectedIndex = 0;
            
            // Загружаем список корпусов
            LoadBuildings();
        }

        // Конструктор для редактирования существующего пациента
        public AddEditPatientWindow(Window owner, int patientID)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = true;
            this.patientID = patientID;
            Title = "Редактирование пациента";
            
            // Загружаем список корпусов
            LoadBuildings();
            
            // Загружаем данные пациента
            LoadPatientData();
        }

        private void LoadPatientData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT FullName, DateOfBirth, Gender, RecordDate, DischargeDate, StayType FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFullName.Text = reader["FullName"].ToString();
                                
                                if (reader["DateOfBirth"] != DBNull.Value)
                                    dpDateOfBirth.SelectedDate = Convert.ToDateTime(reader["DateOfBirth"]);
                                
                                string gender = reader["Gender"].ToString();
                                cbGender.SelectedIndex = gender.Equals("Мужской", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                                
                                string stayType = reader["StayType"].ToString();
                                cbStayType.SelectedIndex = stayType.Equals("Дневной", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                                
                                if (reader["RecordDate"] != DBNull.Value)
                                    dpRecordDate.SelectedDate = Convert.ToDateTime(reader["RecordDate"]);
                                
                                if (reader["DischargeDate"] != DBNull.Value)
                                    dpDischargeDate.SelectedDate = Convert.ToDateTime(reader["DischargeDate"]);
                            }
                            else
                            {
                                MessageBox.Show("Пациент не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                    
                    // Если пациент находится на круглосуточном стационаре, загружаем его размещение
                    if (cbStayType.SelectedIndex == 1)
                    {
                        LoadPatientAccommodation();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных пациента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        
        private void LoadPatientAccommodation()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT a.RoomID, a.BedNumber, r.RoomNumber, b.BuildingID, b.BuildingNumber 
                        FROM Accommodations a
                        JOIN Rooms r ON a.RoomID = r.RoomID
                        JOIN Buildings b ON r.BuildingID = b.BuildingID
                        WHERE a.PatientID = @PatientID AND a.CheckOutDate IS NULL";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int buildingID = Convert.ToInt32(reader["BuildingID"]);
                                int roomID = Convert.ToInt32(reader["RoomID"]);
                                int bedNumber = Convert.ToInt32(reader["BedNumber"]);
                                
                                // Выбираем корпус
                                foreach (var building in buildings)
                                {
                                    if (building.BuildingID == buildingID)
                                    {
                                        cbBuilding.SelectedValue = buildingID;
                                        break;
                                    }
                                }
                                
                                // Загружаем комнаты для выбранного корпуса
                                LoadRooms(buildingID);
                                
                                // Выбираем комнату
                                cbRoom.SelectedValue = roomID;
                                
                                // Выбираем кровать
                                cbBed.SelectedIndex = bedNumber - 1; // индексы начинаются с 0
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке размещения пациента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadBuildings()
        {
            try
            {
                buildings = new List<Building>();
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT BuildingID, BuildingNumber, Description FROM Buildings ORDER BY BuildingNumber";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                buildings.Add(new Building
                                {
                                    BuildingID = Convert.ToInt32(reader["BuildingID"]),
                                    BuildingNumber = Convert.ToInt32(reader["BuildingNumber"]),
                                    Description = reader["Description"] as string
                                });
                            }
                        }
                    }
                }
                
                cbBuilding.ItemsSource = buildings;
                cbBuilding.SelectedValuePath = "BuildingID";
                
                // По умолчанию выбираем первый корпус
                if (buildings.Count > 0)
                {
                    cbBuilding.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка корпусов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadRooms(int buildingID)
        {
            try
            {
                rooms = new List<Room>();
                selectedBuildingID = buildingID;
                
                // Запоминаем текущую комнату пациента (если он уже размещен)
                int currentRoomID = -1;
                int currentBedNumber = -1;
                
                if (isEditMode)
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"
                            SELECT a.RoomID, a.BedNumber
                            FROM Accommodations a
                            WHERE a.PatientID = @PatientID AND a.CheckOutDate IS NULL";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", patientID);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    currentRoomID = Convert.ToInt32(reader["RoomID"]);
                                    currentBedNumber = Convert.ToInt32(reader["BedNumber"]);
                                }
                            }
                        }
                    }
                }
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Запрос для получения комнат и информации о занятых местах
                    string query = @"
                        SELECT r.RoomID, r.RoomNumber, r.IsAvailable,
                               (SELECT COUNT(*) FROM Accommodations a WHERE a.RoomID = r.RoomID AND a.CheckOutDate IS NULL) AS OccupiedBeds
                        FROM Rooms r
                        WHERE r.BuildingID = @BuildingID
                        ORDER BY r.RoomNumber";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@BuildingID", buildingID);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int roomID = Convert.ToInt32(reader["RoomID"]);
                                string roomNumber = reader["RoomNumber"].ToString();
                                bool isAvailable = Convert.ToBoolean(reader["IsAvailable"]);
                                int occupiedBeds = Convert.ToInt32(reader["OccupiedBeds"]);
                                
                                Room room = new Room
                                {
                                    RoomID = roomID,
                                    RoomNumber = roomNumber,
                                    IsAvailable = isAvailable
                                };
                                
                                // Проверка, является ли комната текущей комнатой пациента
                                bool isCurrentRoom = (roomID == currentRoomID);
                                
                                // Определение доступных кроватей (всего 2 кровати в комнате)
                                if (occupiedBeds < 2 || isCurrentRoom)
                                {
                                    // Получаем информацию о том, какие кровати заняты
                                    string bedQuery = @"
                                        SELECT BedNumber, PatientID, AccompanyingPersonID
                                        FROM Accommodations
                                        WHERE RoomID = @RoomID AND CheckOutDate IS NULL";
                                    
                                    using (SqlCommand bedCmd = new SqlCommand(bedQuery, con))
                                    {
                                        bedCmd.Parameters.AddWithValue("@RoomID", roomID);
                                        
                                        List<int> occupiedBedNumbers = new List<int>();
                                        using (SqlDataReader bedReader = bedCmd.ExecuteReader())
                                        {
                                            while (bedReader.Read())
                                            {
                                                int bedNumber = Convert.ToInt32(bedReader["BedNumber"]);
                                                
                                                // Если кровать занята не этим пациентом, то считаем её занятой
                                                if (!isCurrentRoom || bedNumber != currentBedNumber)
                                                {
                                                    occupiedBedNumbers.Add(bedNumber);
                                                }
                                            }
                                        }
                                        
                                        // Добавляем свободные кровати
                                        for (int i = 1; i <= 2; i++)
                                        {
                                            if (!occupiedBedNumbers.Contains(i))
                                            {
                                                room.AvailableBeds.Add(i);
                                            }
                                        }
                                        
                                        // Если это текущая комната пациента, добавляем его кровать в список доступных
                                        if (isCurrentRoom)
                                        {
                                            if (!room.AvailableBeds.Contains(currentBedNumber))
                                            {
                                                room.AvailableBeds.Add(currentBedNumber);
                                            }
                                        }
                                    }
                                }
                                
                                // Добавляем комнату в список, если она доступна и есть свободные кровати
                                // Или если это текущая комната пациента
                                if ((isAvailable && room.AvailableBeds.Count > 0) || isCurrentRoom)
                                {
                                    rooms.Add(room);
                                }
                            }
                        }
                    }
                }
                
                cbRoom.ItemsSource = rooms;
                cbRoom.SelectedValuePath = "RoomID";
                
                // По умолчанию выбираем первую комнату или текущую комнату пациента
                if (currentRoomID > 0)
                {
                    // Находим текущую комнату в списке
                    foreach (var room in rooms)
                    {
                        if (room.RoomID == currentRoomID)
                        {
                            cbRoom.SelectedValue = currentRoomID;
                            break;
                        }
                    }
                }
                else if (rooms.Count > 0)
                {
                    cbRoom.SelectedIndex = 0;
                }
                else
                {
                    cbRoom.ItemsSource = null;
                    cbBed.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка комнат: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateAvailableBeds()
        {
            if (cbRoom.SelectedItem != null)
            {
                Room selectedRoom = (Room)cbRoom.SelectedItem;
                selectedRoomID = selectedRoom.RoomID;
                
                // Обновляем выпадающий список с кроватями
                cbBed.Items.Clear();
                
                foreach (int bedNumber in selectedRoom.AvailableBeds)
                {
                    cbBed.Items.Add(new ComboBoxItem { Content = $"Кровать {bedNumber}" });
                }
                
                // Выбираем первую доступную кровать
                if (cbBed.Items.Count > 0)
                {
                    cbBed.SelectedIndex = 0;
                    selectedBedNumber = selectedRoom.AvailableBeds[0];
                }
            }
        }

        private void cbStayType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isInpatient = cbStayType.SelectedIndex == 1; // 1 - Круглосуточный
            
            // Показываем или скрываем панель размещения
            lblAccommodation.Visibility = isInpatient ? Visibility.Visible : Visibility.Collapsed;
            accommodationPanel.Visibility = isInpatient ? Visibility.Visible : Visibility.Collapsed;
            
            // Если выбран круглосуточный стационар, загружаем доступные комнаты
            if (isInpatient && cbBuilding.SelectedItem != null)
            {
                int buildingID = (int)cbBuilding.SelectedValue;
                LoadRooms(buildingID);
            }
        }
        
        private void cbBuilding_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbBuilding.SelectedItem != null)
            {
                int buildingID = (int)cbBuilding.SelectedValue;
                LoadRooms(buildingID);
            }
        }
        
        private void cbRoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAvailableBeds();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения обязательных полей
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Пожалуйста, введите ФИО пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                txtFullName.Focus();
                return;
            }

            if (dpDateOfBirth.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату рождения пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDateOfBirth.Focus();
                return;
            }

            if (dpRecordDate.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату записи пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpRecordDate.Focus();
                return;
            }

            // Проверка выбора размещения для круглосуточного стационара
            bool isInpatient = cbStayType.SelectedIndex == 1; // 1 - Круглосуточный
            if (isInpatient)
            {
                if (cbBuilding.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите корпус для размещения пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbBuilding.Focus();
                    return;
                }
                
                if (cbRoom.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите комнату для размещения пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbRoom.Focus();
                    return;
                }
                
                if (cbBed.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите кровать для размещения пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbBed.Focus();
                    return;
                }
            }

            // Проверка валидности дат
            DateTime dateOfBirth = dpDateOfBirth.SelectedDate.Value;
            DateTime recordDate = dpRecordDate.SelectedDate.Value;
            DateTime? dischargeDate = dpDischargeDate.SelectedDate;

            if (dateOfBirth > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть в будущем.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDateOfBirth.Focus();
                return;
            }

            if (recordDate > DateTime.Today)
            {
                MessageBox.Show("Дата записи не может быть в будущем.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpRecordDate.Focus();
                return;
            }

            if (dischargeDate.HasValue && dischargeDate.Value < recordDate)
            {
                MessageBox.Show("Дата выписки не может быть раньше даты записи.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDischargeDate.Focus();
                return;
            }

            // Получение данных из формы
            string fullName = txtFullName.Text.Trim();
            string gender = ((ComboBoxItem)cbGender.SelectedItem).Content.ToString();
            string stayType = ((ComboBoxItem)cbStayType.SelectedItem).Content.ToString();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            int newPatientID = -1;
                            
                            if (isEditMode)
                            {
                                // Обновление существующего пациента
                                string updateQuery;
                                if (dischargeDate.HasValue)
                                {
                                    updateQuery = @"
                                        UPDATE Patients SET 
                                        FullName = @FullName, 
                                        DateOfBirth = @DateOfBirth, 
                                        Gender = @Gender, 
                                        RecordDate = @RecordDate, 
                                        DischargeDate = @DischargeDate,
                                        StayType = @StayType
                                        WHERE PatientID = @PatientID";
                                }
                                else
                                {
                                    updateQuery = @"
                                        UPDATE Patients SET 
                                        FullName = @FullName, 
                                        DateOfBirth = @DateOfBirth, 
                                        Gender = @Gender, 
                                        RecordDate = @RecordDate, 
                                        DischargeDate = NULL,
                                        StayType = @StayType
                                        WHERE PatientID = @PatientID";
                                }

                                using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@FullName", fullName);
                                    cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                                    cmd.Parameters.AddWithValue("@Gender", gender);
                                    cmd.Parameters.AddWithValue("@RecordDate", recordDate);
                                    cmd.Parameters.AddWithValue("@StayType", stayType);
                                    
                                    if (dischargeDate.HasValue)
                                        cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate.Value);
                                    
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    
                                    cmd.ExecuteNonQuery();
                                }
                                
                                newPatientID = patientID;
                            }
                            else
                            {
                                // Добавление нового пациента
                                string insertQuery;
                                if (dischargeDate.HasValue)
                                {
                                    insertQuery = @"
                                        INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, DischargeDate, StayType)
                                        VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @DischargeDate, @StayType);
                                        SELECT SCOPE_IDENTITY();";
                                }
                                else
                                {
                                    insertQuery = @"
                                        INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, StayType)
                                        VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @StayType);
                                        SELECT SCOPE_IDENTITY();";
                                }

                                using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@FullName", fullName);
                                    cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                                    cmd.Parameters.AddWithValue("@Gender", gender);
                                    cmd.Parameters.AddWithValue("@RecordDate", recordDate);
                                    cmd.Parameters.AddWithValue("@StayType", stayType);
                                    
                                    if (dischargeDate.HasValue)
                                        cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate.Value);
                                    
                                    newPatientID = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                            }
                            
                            // Обновляем размещение пациента, если он на круглосуточном стационаре
                            if (isInpatient)
                            {
                                // Получаем выбранную кровать
                                ComboBoxItem selectedBedItem = (ComboBoxItem)cbBed.SelectedItem;
                                string bedContent = selectedBedItem.Content.ToString();
                                int bedNumber = int.Parse(bedContent.Replace("Кровать ", ""));
                                
                                if (isEditMode)
                                {
                                    // Обновляем существующее размещение
                                    // Сначала помечаем все текущие размещения пациента как выселенные
                                    string updateQuery = @"
                                        UPDATE Accommodations 
                                        SET CheckOutDate = @CheckOutDate 
                                        WHERE PatientID = @PatientID AND CheckOutDate IS NULL";
                                        
                                    using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@CheckOutDate", DateTime.Now);
                                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                
                                // Добавляем новое размещение
                                string insertAccommodationQuery = @"
                                    INSERT INTO Accommodations (RoomID, PatientID, BedNumber, CheckInDate)
                                    VALUES (@RoomID, @PatientID, @BedNumber, @CheckInDate)";
                                    
                                using (SqlCommand cmd = new SqlCommand(insertAccommodationQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@RoomID", selectedRoomID);
                                    cmd.Parameters.AddWithValue("@PatientID", newPatientID);
                                    cmd.Parameters.AddWithValue("@BedNumber", bedNumber);
                                    cmd.Parameters.AddWithValue("@CheckInDate", DateTime.Now);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (isEditMode)
                            {
                                // Если пациент переведен с круглосуточного на дневной стационар,
                                // необходимо выселить его из комнаты
                                string updateQuery = @"
                                    UPDATE Accommodations 
                                    SET CheckOutDate = @CheckOutDate 
                                    WHERE PatientID = @PatientID AND CheckOutDate IS NULL";
                                    
                                using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@CheckOutDate", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            
                            // Фиксируем транзакцию
                            transaction.Commit();
                            
                            MessageBox.Show(isEditMode ? "Пациент успешно обновлен." : "Пациент успешно добавлен.", 
                                           "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Автоматически открываем окно документов только для нового пациента
                            if (!isEditMode)
                            {
                                PatientDocumentsWindow documentsWindow = new PatientDocumentsWindow(
                                    newPatientID, 
                                    txtFullName.Text.Trim(), 
                                    dpDateOfBirth.SelectedDate);
                                documentsWindow.ShowDialog();
                            }
                            
                            // Устанавливаем ID добавленного пациента для возможности открытия окна документов
                            DialogResult = true;
                        }
                        catch (Exception ex)
                        {
                            // Откатываем транзакцию в случае ошибки
                            transaction.Rollback();
                            throw new Exception("Ошибка при сохранении: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении пациента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
