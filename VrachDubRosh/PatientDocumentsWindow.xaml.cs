using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Linq;

namespace VrachDubRosh
{
    public partial class PatientDocumentsWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private readonly int patientID;
        private readonly string patientName;
        private readonly DateTime? dateOfBirth;
        private int patientAge;
        private int selectedDocumentTypeID = -1;
        private int selectedDocumentID = -1;
        
        public PatientDocumentsWindow(int patientID, string patientName, DateTime? dateOfBirth)
        {
            InitializeComponent();
            this.patientID = patientID;
            this.patientName = patientName;
            this.dateOfBirth = dateOfBirth;
            
            // Устанавливаем заголовок
            txtPatientInfo.Text = $"Документы пациента: {patientName}";
            
            // Вычисляем возраст пациента
            if (dateOfBirth.HasValue)
            {
                DateTime today = DateTime.Today;
                patientAge = today.Year - dateOfBirth.Value.Year;
                if (dateOfBirth.Value.Date > today.AddYears(-patientAge)) patientAge--;
                txtAgeInfo.Text = $"Возраст: {patientAge} лет";
            }
            else
            {
                patientAge = 0;
                txtAgeInfo.Text = "Возраст: не указан";
            }
            
            // Загружаем список документов
            LoadDocuments();
        }
        
        private void LoadDocuments()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Получаем список типов документов, соответствующих возрасту пациента
                    string query = @"
                        SELECT dt.DocumentTypeID, dt.DocumentName, dt.Description, dt.IsRequired, dt.ValidityDays,
                               pd.DocumentID, pd.DocumentPath, pd.UploadDate,
                               CASE 
                                   WHEN pd.DocumentID IS NULL THEN 'Не загружен'
                                   WHEN pd.IsVerified = 1 THEN 'Проверен'
                                   ELSE 'Загружен'
                               END AS Status
                        FROM DocumentTypes dt
                        LEFT JOIN PatientDocuments pd ON dt.DocumentTypeID = pd.DocumentTypeID AND pd.PatientID = @PatientID
                        WHERE dt.ForAccompanyingPerson = 0
                          AND @PatientAge BETWEEN dt.MinimumAge AND dt.MaximumAge
                        ORDER BY dt.IsRequired DESC, dt.DocumentName";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        cmd.Parameters.AddWithValue("@PatientAge", patientAge);
                        
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        
                        dgDocuments.ItemsSource = dt.DefaultView;
                        
                        // Обновляем статус комплекта документов
                        UpdateDocumentStatus(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке документов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void UpdateDocumentStatus(DataTable documentsTable)
        {
            int totalRequired = 0;
            int uploadedRequired = 0;
            
            foreach (DataRow row in documentsTable.Rows)
            {
                bool isRequired = Convert.ToBoolean(row["IsRequired"]);
                string status = row["Status"].ToString();
                
                if (isRequired)
                {
                    totalRequired++;
                    if (status == "Загружен" || status == "Проверен")
                    {
                        uploadedRequired++;
                    }
                }
            }
            
            if (totalRequired == 0)
            {
                txtDocumentStatus.Text = "Нет обязательных документов";
                txtDocumentStatus.Foreground = System.Windows.Media.Brushes.Gray;
            }
            else if (uploadedRequired == totalRequired)
            {
                txtDocumentStatus.Text = "Полный комплект";
                txtDocumentStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else
            {
                txtDocumentStatus.Text = $"Неполный комплект ({uploadedRequired}/{totalRequired})";
                txtDocumentStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }
        
        private void dgDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDocuments.SelectedItem is DataRowView row)
            {
                selectedDocumentTypeID = Convert.ToInt32(row["DocumentTypeID"]);
                
                if (row["DocumentID"] != DBNull.Value)
                {
                    selectedDocumentID = Convert.ToInt32(row["DocumentID"]);
                }
                else
                {
                    selectedDocumentID = -1;
                }
            }
            else
            {
                selectedDocumentTypeID = -1;
                selectedDocumentID = -1;
            }
        }
        
        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDocumentTypeID <= 0)
            {
                MessageBox.Show("Выберите документ для загрузки.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем текущую строку
            DataRowView row = dgDocuments.SelectedItem as DataRowView;
            if (row == null) return;
            
            // Получаем название типа документа
            string documentTypeName = row["DocumentName"].ToString();
            
            // Проверяем, есть ли уже загруженный документ
            bool documentExists = row["DocumentID"] != DBNull.Value;
            
            if (documentExists)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Документ уже загружен. Хотите загрузить новую версию?", 
                    "Подтверждение", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result != MessageBoxResult.Yes)
                    return;
            }
            
            // Открываем диалог выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения (*.jpeg;*.jpg;*.png;*.bmp)|*.jpeg;*.jpg;*.png;*.bmp|PDF файлы (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
                Title = "Выберите файл документа"
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                string sourceFilePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(sourceFilePath);
                string fileExtension = Path.GetExtension(sourceFilePath);
                
                // Создаем имя для нового файла, включая тип документа
                string safeDocumentName = documentTypeName.Replace('/', '_').Replace('\\', '_');
                string newFileName = $"{safeDocumentName}{fileExtension}";
                
                // Базовая директория для документов пациентов
                string baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "Пациенты");
                
                // Создаем директорию для пациента (используем только имя)
                string patientFolder = Path.Combine(baseDirectory, patientName.Replace(' ', '_'));
                Directory.CreateDirectory(patientFolder);
                
                // Полный путь к новому файлу
                string destinationPath = Path.Combine(patientFolder, newFileName);
                
                // Проверяем, существует ли уже файл с таким именем
                if (File.Exists(destinationPath))
                {
                    // Если файл существует, добавляем порядковый номер
                    int counter = 1;
                    string fileNameWithoutExt = safeDocumentName;
                    
                    while (File.Exists(destinationPath))
                    {
                        newFileName = $"{fileNameWithoutExt}_{counter}{fileExtension}";
                        destinationPath = Path.Combine(patientFolder, newFileName);
                        counter++;
                    }
                }
                
                try
                {
                    // Копируем файл в папку назначения
                    File.Copy(sourceFilePath, destinationPath);
                    
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        
                        if (documentExists)
                        {
                            // Удаляем старый файл, если он существует
                            string oldFilePath = row["DocumentPath"].ToString();
                            if (File.Exists(oldFilePath))
                            {
                                try
                                {
                                    File.Delete(oldFilePath);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Не удалось удалить старый файл: {ex.Message}");
                                }
                            }
                            
                            // Обновляем существующий документ
                            string updateQuery = @"
                                UPDATE PatientDocuments SET 
                                DocumentPath = @DocumentPath, 
                                UploadDate = GETDATE(), 
                                IsVerified = 0
                                WHERE DocumentID = @DocumentID";
                            
                            using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.Parameters.AddWithValue("@DocumentID", selectedDocumentID);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Добавляем новый документ
                            string insertQuery = @"
                                INSERT INTO PatientDocuments (PatientID, DocumentTypeID, DocumentPath, UploadDate, IsVerified)
                                VALUES (@PatientID, @DocumentTypeID, @DocumentPath, GETDATE(), 0)";
                            
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", patientID);
                                cmd.Parameters.AddWithValue("@DocumentTypeID", selectedDocumentTypeID);
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.ExecuteNonQuery();
                            }
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
        
        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDocumentID <= 0)
            {
                MessageBox.Show("Выберите загруженный документ для просмотра.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            DataRowView row = dgDocuments.SelectedItem as DataRowView;
            if (row == null || row["DocumentPath"] == DBNull.Value) return;
            
            string documentPath = row["DocumentPath"].ToString();
            
            if (!File.Exists(documentPath))
            {
                MessageBox.Show("Файл документа не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                // Открываем файл с помощью программы по умолчанию
                Process.Start(new ProcessStartInfo(documentPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии документа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDocumentID <= 0)
            {
                MessageBox.Show("Выберите загруженный документ для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем информацию о документе
            DataRowView row = dgDocuments.SelectedItem as DataRowView;
            if (row == null) return;
            
            string documentName = row["DocumentName"].ToString();
            string documentPath = row["DocumentPath"].ToString();
            
            // Подтверждение удаления
            if (MessageBox.Show($"Вы действительно хотите удалить документ \"{documentName}\"?", 
                               "Подтверждение удаления", 
                               MessageBoxButton.YesNo, 
                               MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                // Удаляем файл, если он существует
                if (File.Exists(documentPath))
                {
                    File.Delete(documentPath);
                    
                    // Проверяем, стала ли директория пациента пустой, и удаляем её если да
                    string patientDir = Path.GetDirectoryName(documentPath);
                    if (Directory.Exists(patientDir) && !Directory.EnumerateFileSystemEntries(patientDir).Any())
                    {
                        try
                        {
                            Directory.Delete(patientDir);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Не удалось удалить пустую директорию пациента: {ex.Message}");
                        }
                    }
                }
                
                // Удаляем запись из базы данных
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    string query = "DELETE FROM PatientDocuments WHERE DocumentID = @DocumentID";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", selectedDocumentID);
                        cmd.ExecuteNonQuery();
                    }
                }
                
                MessageBox.Show("Документ успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Обновляем список документов
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении документа: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 