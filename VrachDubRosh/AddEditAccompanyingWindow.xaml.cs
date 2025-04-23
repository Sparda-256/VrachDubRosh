using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
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

        public override string ToString()
        {
            return FullName;
        }
    }

    public partial class AddEditAccompanyingWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private readonly int accompaningPersonID;
        private readonly bool isEditMode;
        private readonly Window owner;
        private string powerOfAttorneyPath; // Путь к файлу доверенности
        private bool isPowerOfAttorneyRequired; // Флаг, указывающий, требуется ли доверенность

        // Конструктор для добавления нового сопровождающего
        public AddEditAccompanyingWindow(Window owner)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = false;
            accompaningPersonID = -1;
            Title = "Добавление сопровождающего";
            
            // По умолчанию выбираем отношение "Родитель"
            cbRelationship.SelectedIndex = 0;
            
            // Загружаем список пациентов
            LoadPatients();
            
            // Подписываемся на событие изменения выбора отношения
            cbRelationship.SelectionChanged += CbRelationship_SelectionChanged;
        }

        // Конструктор для редактирования существующего сопровождающего
        public AddEditAccompanyingWindow(Window owner, int accompaningPersonID)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = true;
            this.accompaningPersonID = accompaningPersonID;
            Title = "Редактирование сопровождающего";
            
            // Загружаем список пациентов
            LoadPatients();
            
            // Загружаем данные сопровождающего
            LoadAccompanyingPersonData();
            
            // Подписываемся на событие изменения выбора отношения
            cbRelationship.SelectionChanged += CbRelationship_SelectionChanged;
        }
        
        private void LoadPatients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName FROM Patients ORDER BY FullName";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    
                    // Убедимся, что ComboBox показывает имя пациента, а не DataRowView.ToString()
                    cbPatients.DisplayMemberPath = "FullName";
                    cbPatients.SelectedValuePath = "PatientID";
                    cbPatients.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка пациентов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompaningPersonID);
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
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompaningPersonID);
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
                string uniqueFileName = $"poa_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                powerOfAttorneyPath = Path.Combine(documentsFolder, uniqueFileName);
                
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
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompaningPersonID);
                                    
                                    cmd.ExecuteNonQuery();
                                }
                                
                                newAccompanyingID = accompaningPersonID;
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
                                MessageBox.Show("Сопровождающий успешно добавлен.\n\nТеперь вы можете добавить документы сопровождающего в разделе 'Документы'.", 
                                               "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }
} 