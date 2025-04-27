using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace VrachDubRosh
{
    // Класс для хранения информации о пациенте
    public class PatientInfo
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }
        public string StayType { get; set; }

        public override string ToString()
        {
            return FullName;
        }
    }

    public partial class AddEditAccompanyingWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private readonly int accompanyingID;
        private readonly bool isEditMode;
        private readonly Window owner;
        private string powerOfAttorneyPath; // Путь к файлу доверенности
        private bool isPowerOfAttorneyRequired; // Флаг, указывающий, требуется ли доверенность
        private List<PatientInfo> allPatients; // Полный список пациентов для поиска
        
        // Поля для размещения
        private List<Building> buildings;
        private List<Room> rooms;
        private int selectedBuildingID = -1;
        private int selectedRoomID = -1;
        private int selectedBedNumber = 1; // По умолчанию кровать 1
        private bool needAccommodation = false; // Требуется ли размещение

        // Конструктор для добавления нового сопровождающего
        public AddEditAccompanyingWindow(Window owner)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = false;
            accompanyingID = -1;
            Title = "Добавление сопровождающего";
            
            // По умолчанию выбираем отношение "Родитель"
            cbRelationship.SelectedIndex = 0;
            
            // Загружаем список пациентов
            LoadPatients();
            
            // Загружаем список корпусов
            LoadBuildings();
            
            // Подписываемся на событие изменения выбора отношения
            cbRelationship.SelectionChanged += CbRelationship_SelectionChanged;

            // По умолчанию скрываем панель размещения
            lblAccommodation.Visibility = Visibility.Collapsed;
            accommodationPanel.Visibility = Visibility.Collapsed;
        }

        // Конструктор для редактирования существующего сопровождающего
        public AddEditAccompanyingWindow(Window owner, int accompanyingID)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = true;
            this.accompanyingID = accompanyingID;
            Title = "Редактирование сопровождающего";
            
            // Загружаем список пациентов
            LoadPatients();
            
            // Загружаем список корпусов
            LoadBuildings();
            
            // Загружаем данные сопровождающего
            LoadAccompanyingPersonData();
            
            // Подписываемся на событие изменения выбора отношения
            cbRelationship.SelectionChanged += CbRelationship_SelectionChanged;
        }
        
        private void LoadPatients()
        {
            try
            {
                allPatients = new List<PatientInfo>();
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName, StayType FROM Patients ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                allPatients.Add(new PatientInfo
                                {
                                    PatientID = Convert.ToInt32(reader["PatientID"]),
                                    FullName = reader["FullName"].ToString(),
                                    StayType = reader["StayType"].ToString()
                                });
                            }
                        }
                    }
                }
                
                // Настраиваем комбобокс для отображения пациентов
                cbPatients.ItemsSource = allPatients;
                cbPatients.DisplayMemberPath = "FullName";
                cbPatients.SelectedValuePath = "PatientID";
                
                // Подписываемся на событие изменения выбора пациента
                cbPatients.SelectionChanged += CbPatients_SelectionChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка пациентов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CbPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbPatients.SelectedItem != null)
            {
                PatientInfo selectedPatient = (PatientInfo)cbPatients.SelectedItem;
                
                // Определяем, требуется ли размещение для сопровождающего
                needAccommodation = selectedPatient.StayType == "Круглосуточный";
                
                // Показываем или скрываем панель размещения
                lblAccommodation.Visibility = needAccommodation ? Visibility.Visible : Visibility.Collapsed;
                accommodationPanel.Visibility = needAccommodation ? Visibility.Visible : Visibility.Collapsed;
                
                // Если требуется размещение, загружаем доступные комнаты
                if (needAccommodation && cbBuilding.SelectedItem != null)
                {
                    int buildingID = (int)cbBuilding.SelectedValue;
                    LoadRooms(buildingID);
                }
            }
        }
        
        private void LoadAccompanyingPersonData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName, DateOfBirth, Relationship, HasPowerOfAttorney FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int patientID = Convert.ToInt32(reader["PatientID"]);
                                cbPatients.SelectedValue = patientID;
                                txtFullName.Text = reader["FullName"].ToString();
                                
                                if (reader["DateOfBirth"] != DBNull.Value)
                                    dpDateOfBirth.SelectedDate = Convert.ToDateTime(reader["DateOfBirth"]);
                                
                                // Временно отключаем обработчик события, чтобы избежать преждевременного вызова
                                cbRelationship.SelectionChanged -= CbRelationship_SelectionChanged;
                                
                                string relationship = reader["Relationship"].ToString();
                                SelectRelationship(relationship);
                                
                                // Загружаем информацию о доверенности
                                bool hasPowerOfAttorney = Convert.ToBoolean(reader["HasPowerOfAttorney"]);
                                
                                // Определяем, требуется ли доверенность, на основе отношения
                                isPowerOfAttorneyRequired = relationship != "Родитель" && relationship != "Опекун";
                                
                                // Загружаем документ доверенности, если он существует
                                LoadPowerOfAttorneyDocument();
                                
                                // Снова подключаем обработчик события
                                cbRelationship.SelectionChanged += CbRelationship_SelectionChanged;
                                
                                // Обновляем UI с учетом загруженных данных
                                UpdatePowerOfAttorneyUI();
                                
                                // Загружаем размещение сопровождающего, если пациент на круглосуточном стационаре
                                PatientInfo selectedPatient = cbPatients.SelectedItem as PatientInfo;
                                if (selectedPatient != null && selectedPatient.StayType == "Круглосуточный")
                                {
                                    LoadAccompanyingAccommodation();
                                }
                            }
                            else
                            {
                                MessageBox.Show("Сопровождающий не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных сопровождающего: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        
        private void LoadAccompanyingAccommodation()
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
                        WHERE a.AccompanyingPersonID = @AccompanyingPersonID AND a.CheckOutDate IS NULL";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
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
                MessageBox.Show("Ошибка при загрузке размещения сопровождающего: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SelectRelationship(string relationship)
        {
            for (int i = 0; i < cbRelationship.Items.Count; i++)
            {
                ComboBoxItem item = (ComboBoxItem)cbRelationship.Items[i];
                if (item.Content.ToString() == relationship)
                {
                    cbRelationship.SelectedIndex = i;
                    return;
                }
            }
            
            // Если не нашли точное совпадение, выбираем "Иное лицо"
            cbRelationship.SelectedIndex = cbRelationship.Items.Count - 1;
        }
        
        // Обработчик события изменения выбора отношения
        private void CbRelationship_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbRelationship.SelectedItem != null)
            {
                string relationship = ((ComboBoxItem)cbRelationship.SelectedItem).Content.ToString();
                
                // Определяем, требуется ли загрузка доверенности
                isPowerOfAttorneyRequired = relationship != "Родитель" && relationship != "Опекун";
                
                // Обновляем интерфейс в зависимости от требования
                UpdatePowerOfAttorneyUI();
            }
        }
        
        // Обновление интерфейса для работы с доверенностью
        private void UpdatePowerOfAttorneyUI()
        {
            if (isPowerOfAttorneyRequired)
            {
                btnUploadPowerOfAttorney.Visibility = Visibility.Visible;
                
                if (string.IsNullOrEmpty(powerOfAttorneyPath))
                {
                    txtPowerOfAttorneyStatus.Text = "Необходимо загрузить доверенность";
                    txtPowerOfAttorneyStatus.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    txtPowerOfAttorneyStatus.Text = "Доверенность загружена";
                    txtPowerOfAttorneyStatus.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            else
            {
                btnUploadPowerOfAttorney.Visibility = Visibility.Collapsed;
                txtPowerOfAttorneyStatus.Text = "Не требуется";
                txtPowerOfAttorneyStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }
        
        // Загрузка информации о доверенности из базы данных
        private void LoadPowerOfAttorneyDocument()
        {
            try
            {
                // Ищем документ типа "Доверенность от законных представителей"
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT apd.DocumentPath
                        FROM AccompanyingPersonDocuments apd
                        INNER JOIN DocumentTypes dt ON apd.DocumentTypeID = dt.DocumentTypeID
                        WHERE apd.AccompanyingPersonID = @AccompanyingPersonID
                        AND dt.DocumentName = 'Доверенность от законных представителей'";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
                        object result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            powerOfAttorneyPath = result.ToString();
                            // Не вызываем UpdatePowerOfAttorneyUI() здесь,
                            // так как этот метод может быть вызван как часть большего процесса
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке информации о доверенности: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Обработчик нажатия на кнопку загрузки доверенности
        private void btnUploadPowerOfAttorney_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Файлы документов (*.pdf;*.jpg;*.jpeg;*.png;*.doc;*.docx)|*.pdf;*.jpg;*.jpeg;*.png;*.doc;*.docx|Все файлы (*.*)|*.*",
                Title = "Выберите файл доверенности",
                CheckFileExists = true
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(selectedFilePath);
                
                // Создаем путь для сохранения копии файла в папке приложения
                string documentsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "PowerOfAttorney");
                
                // Создаем папку, если она не существует
                if (!Directory.Exists(documentsFolder))
                {
                    Directory.CreateDirectory(documentsFolder);
                }
                
                // Создаем уникальное имя файла
                string uniqueFileName = $"poa{fileExtension}";
                powerOfAttorneyPath = Path.Combine(documentsFolder, uniqueFileName);
                
                // Проверяем, существует ли уже файл с таким именем
                if (File.Exists(powerOfAttorneyPath))
                {
                    // Если файл существует, добавляем порядковый номер
                    int counter = 1;
                    
                    while (File.Exists(powerOfAttorneyPath))
                    {
                        uniqueFileName = $"poa_{counter}{fileExtension}";
                        powerOfAttorneyPath = Path.Combine(documentsFolder, uniqueFileName);
                        counter++;
                    }
                }
                
                try
                {
                    // Копируем файл
                    File.Copy(selectedFilePath, powerOfAttorneyPath, true);
                    
                    // Обновляем интерфейс
                    UpdatePowerOfAttorneyUI();
                    
                    MessageBox.Show("Доверенность успешно загружена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при загрузке доверенности: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    powerOfAttorneyPath = null;
                    UpdatePowerOfAttorneyUI();
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения обязательных полей
            if (cbPatients.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                cbPatients.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Пожалуйста, введите ФИО сопровождающего.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                txtFullName.Focus();
                return;
            }
            
            // Проверка валидности даты
            if (dpDateOfBirth.SelectedDate.HasValue && dpDateOfBirth.SelectedDate.Value > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть в будущем.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDateOfBirth.Focus();
                return;
            }
            
            // Проверка наличия доверенности, если она требуется
            if (isPowerOfAttorneyRequired && string.IsNullOrEmpty(powerOfAttorneyPath))
            {
                MessageBox.Show("Для выбранного отношения к пациенту необходимо загрузить доверенность.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                btnUploadPowerOfAttorney.Focus();
                return;
            }
            
            // Проверка выбора размещения для пациентов на круглосуточном стационаре
            PatientInfo selectedPatient = cbPatients.SelectedItem as PatientInfo;
            bool isInpatient = selectedPatient != null && selectedPatient.StayType == "Круглосуточный";
            
            if (isInpatient)
            {
                if (cbBuilding.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите корпус для размещения сопровождающего.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbBuilding.Focus();
                    return;
                }
                
                if (cbRoom.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите комнату для размещения сопровождающего.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbRoom.Focus();
                    return;
                }
                
                if (cbBed.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите кровать для размещения сопровождающего.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    cbBed.Focus();
                    return;
                }
            }

            // Получение данных из формы
            int patientID = Convert.ToInt32(cbPatients.SelectedValue);
            string fullName = txtFullName.Text.Trim();
            DateTime? dateOfBirth = dpDateOfBirth.SelectedDate;
            string relationship = ((ComboBoxItem)cbRelationship.SelectedItem).Content.ToString();
            bool hasPowerOfAttorney = isPowerOfAttorneyRequired && !string.IsNullOrEmpty(powerOfAttorneyPath);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Начинаем транзакцию
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            int newAccompanyingID = -1;
                            
                            if (isEditMode)
                            {
                                // Обновление существующего сопровождающего
                                string updateQuery = @"
                                    UPDATE AccompanyingPersons SET 
                                    PatientID = @PatientID, 
                                    FullName = @FullName, 
                                    DateOfBirth = @DateOfBirth, 
                                    Relationship = @Relationship, 
                                    HasPowerOfAttorney = @HasPowerOfAttorney 
                                    WHERE AccompanyingPersonID = @AccompanyingPersonID";
                                
                                using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.Parameters.AddWithValue("@FullName", fullName);
                                    
                                    if (dateOfBirth.HasValue)
                                        cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.Value);
                                    else
                                        cmd.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);
                                    
                                    cmd.Parameters.AddWithValue("@Relationship", relationship);
                                    cmd.Parameters.AddWithValue("@HasPowerOfAttorney", hasPowerOfAttorney);
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
                                    
                                    cmd.ExecuteNonQuery();
                                }
                                
                                newAccompanyingID = accompanyingID;
                            }
                            else
                            {
                                // Добавление нового сопровождающего
                                string insertQuery = @"
                                    INSERT INTO AccompanyingPersons (PatientID, FullName, DateOfBirth, Relationship, HasPowerOfAttorney)
                                    VALUES (@PatientID, @FullName, @DateOfBirth, @Relationship, @HasPowerOfAttorney);
                                    SELECT SCOPE_IDENTITY();";
                                
                                using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", patientID);
                                    cmd.Parameters.AddWithValue("@FullName", fullName);
                                    
                                    if (dateOfBirth.HasValue)
                                        cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.Value);
                                    else
                                        cmd.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);
                                    
                                    cmd.Parameters.AddWithValue("@Relationship", relationship);
                                    cmd.Parameters.AddWithValue("@HasPowerOfAttorney", hasPowerOfAttorney);
                                    
                                    newAccompanyingID = Convert.ToInt32(cmd.ExecuteScalar());
                                }
                            }
                            
                            // Обновляем размещение сопровождающего, если пациент на круглосуточном стационаре
                            if (isInpatient)
                            {
                                // Получаем выбранную кровать
                                ComboBoxItem selectedBedItem = (ComboBoxItem)cbBed.SelectedItem;
                                string bedContent = selectedBedItem.Content.ToString();
                                int bedNumber = int.Parse(bedContent.Replace("Кровать ", ""));
                                
                                if (isEditMode)
                                {
                                    // Обновляем существующее размещение
                                    // Сначала помечаем все текущие размещения сопровождающего как выселенные
                                    string updateQuery = @"
                                        UPDATE Accommodations 
                                        SET CheckOutDate = @CheckOutDate 
                                        WHERE AccompanyingPersonID = @AccompanyingPersonID AND CheckOutDate IS NULL";
                                        
                                    using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@CheckOutDate", DateTime.Now);
                                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                
                                // Добавляем новое размещение
                                string insertAccommodationQuery = @"
                                    INSERT INTO Accommodations (RoomID, AccompanyingPersonID, BedNumber, CheckInDate)
                                    VALUES (@RoomID, @AccompanyingPersonID, @BedNumber, @CheckInDate)";
                                    
                                using (SqlCommand cmd = new SqlCommand(insertAccommodationQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@RoomID", selectedRoomID);
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", newAccompanyingID);
                                    cmd.Parameters.AddWithValue("@BedNumber", bedNumber);
                                    cmd.Parameters.AddWithValue("@CheckInDate", DateTime.Now);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else if (isEditMode)
                            {
                                // Если пациент переведен с круглосуточного на дневной стационар,
                                // необходимо выселить сопровождающего из комнаты
                                string updateQuery = @"
                                    UPDATE Accommodations 
                                    SET CheckOutDate = @CheckOutDate 
                                    WHERE AccompanyingPersonID = @AccompanyingPersonID AND CheckOutDate IS NULL";
                                    
                                using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@CheckOutDate", DateTime.Now);
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            
                            // Если есть доверенность, сохраняем её в базу как документ
                            if (isPowerOfAttorneyRequired && !string.IsNullOrEmpty(powerOfAttorneyPath))
                            {
                                // Получаем ID типа документа "Доверенность от законных представителей"
                                int powerOfAttorneyDocTypeID = -1;
                                string docTypeQuery = "SELECT DocumentTypeID FROM DocumentTypes WHERE DocumentName = 'Доверенность от законных представителей'";
                                
                                using (SqlCommand cmd = new SqlCommand(docTypeQuery, con, transaction))
                                {
                                    object result = cmd.ExecuteScalar();
                                    if (result != null)
                                    {
                                        powerOfAttorneyDocTypeID = Convert.ToInt32(result);
                                    }
                                    else
                                    {
                                        throw new Exception("Не найден тип документа 'Доверенность от законных представителей'");
                                    }
                                }
                                
                                // Проверяем, есть ли уже загруженная доверенность
                                int existingDocID = -1;
                                string checkDocQuery = @"
                                    SELECT DocumentID FROM AccompanyingPersonDocuments 
                                    WHERE AccompanyingPersonID = @AccompanyingPersonID 
                                    AND DocumentTypeID = @DocumentTypeID";
                                    
                                using (SqlCommand cmd = new SqlCommand(checkDocQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", newAccompanyingID);
                                    cmd.Parameters.AddWithValue("@DocumentTypeID", powerOfAttorneyDocTypeID);
                                    
                                    object result = cmd.ExecuteScalar();
                                    if (result != null && result != DBNull.Value)
                                    {
                                        existingDocID = Convert.ToInt32(result);
                                    }
                                }
                                
                                if (existingDocID > 0)
                                {
                                    // Обновляем существующий документ
                                    string updateDocQuery = @"
                                        UPDATE AccompanyingPersonDocuments 
                                        SET DocumentPath = @DocumentPath, UploadDate = @UploadDate 
                                        WHERE DocumentID = @DocumentID";
                                        
                                    using (SqlCommand cmd = new SqlCommand(updateDocQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@DocumentPath", powerOfAttorneyPath);
                                        cmd.Parameters.AddWithValue("@UploadDate", DateTime.Now);
                                        cmd.Parameters.AddWithValue("@DocumentID", existingDocID);
                                        
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    // Добавляем новый документ
                                    string insertDocQuery = @"
                                        INSERT INTO AccompanyingPersonDocuments 
                                        (AccompanyingPersonID, DocumentTypeID, DocumentPath, UploadDate, IsVerified) 
                                        VALUES (@AccompanyingPersonID, @DocumentTypeID, @DocumentPath, @UploadDate, 0)";
                                        
                                    using (SqlCommand cmd = new SqlCommand(insertDocQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", newAccompanyingID);
                                        cmd.Parameters.AddWithValue("@DocumentTypeID", powerOfAttorneyDocTypeID);
                                        cmd.Parameters.AddWithValue("@DocumentPath", powerOfAttorneyPath);
                                        cmd.Parameters.AddWithValue("@UploadDate", DateTime.Now);
                                        
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            
                            // Подтверждаем транзакцию
                            transaction.Commit();
                            
                            if (isEditMode)
                            {
                                MessageBox.Show("Сопровождающий успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBox.Show("Сопровождающий успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                
                                // Автоматически открываем окно документов только для нового сопровождающего
                                PatientInfo selectedPatientInfo = (PatientInfo)cbPatients.SelectedItem;
                                AccompanyingDocumentsWindow documentsWindow = new AccompanyingDocumentsWindow(
                                    selectedPatientInfo.PatientID,
                                    selectedPatientInfo.FullName,
                                    newAccompanyingID,
                                    txtFullName.Text.Trim());
                                documentsWindow.ShowDialog();
                            }
                            
                            DialogResult = true;
                        }
                        catch (Exception ex)
                        {
                            // Отменяем транзакцию в случае ошибки
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении сопровождающего: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Обработчик события изменения текста в поле поиска
        private void txtPatientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = txtPatientSearch.Text.ToLower();
            
            // Если строка поиска пуста, отображаем весь список
            if (string.IsNullOrWhiteSpace(searchText))
            {
                cbPatients.ItemsSource = allPatients;
                return;
            }
            
            // Фильтруем список пациентов по введенному тексту
            var filteredPatients = allPatients
                .Where(p => p.FullName.ToLower().Contains(searchText))
                .ToList();
            
            // Обновляем комбобокс отфильтрованным списком
            cbPatients.ItemsSource = filteredPatients;
            
            // Открываем выпадающий список, если найдены совпадения
            if (filteredPatients.Count > 0)
            {
                cbPatients.IsDropDownOpen = true;
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
                
                // Запоминаем текущую комнату сопровождающего (если он уже размещен)
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
                            WHERE a.AccompanyingPersonID = @AccompanyingPersonID AND a.CheckOutDate IS NULL";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
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
                                
                                // Проверка, является ли комната текущей комнатой сопровождающего
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
                                                
                                                // Если кровать занята не этим сопровождающим, то считаем её занятой
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
                                        
                                        // Если это текущая комната сопровождающего, добавляем его кровать в список доступных
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
                                // Или если это текущая комната сопровождающего
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
                
                // По умолчанию выбираем первую комнату или текущую комнату сопровождающего
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
    }
} 