using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace VrachDubRosh
{
    public partial class AccompanyingDocumentsWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private readonly int accompanyingPersonID;
        private readonly string accompanyingPersonName;
        private readonly int patientID;
        private readonly string patientName;
        private int selectedDocumentTypeID = -1;
        private int selectedDocumentID = -1;

        public AccompanyingDocumentsWindow(int accompanyingPersonID, string accompanyingPersonName, int patientID, string patientName)
        {
            InitializeComponent();
            this.accompanyingPersonID = accompanyingPersonID;
            this.accompanyingPersonName = accompanyingPersonName;
            this.patientID = patientID;
            this.patientName = patientName;
            
            // Устанавливаем заголовки
            txtAccompanyingInfo.Text = $"Документы сопровождающего: {accompanyingPersonName}";
            txtPatientInfo.Text = $"Пациент: {patientName}";
            
            // Загружаем документы
            LoadDocuments();
        }

        private void LoadDocuments()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Создаем запрос для получения списка документов для сопровождающих лиц
                    string query = @"
                    SELECT 
                        dt.DocumentTypeID,
                        dt.DocumentName,
                        dt.Description,
                        dt.IsRequired,
                        CASE 
                            WHEN apd.DocumentID IS NOT NULL THEN 'Загружен'
                            WHEN dt.IsRequired = 1 THEN 'Требуется'
                            ELSE 'Опционально'
                        END AS Status,
                        apd.DocumentID,
                        apd.UploadDate,
                        apd.ExpiryDate,
                        apd.DocumentPath,
                        apd.IsVerified
                    FROM DocumentTypes dt
                    LEFT JOIN AccompanyingPersonDocuments apd ON dt.DocumentTypeID = apd.DocumentTypeID AND apd.AccompanyingPersonID = @AccompanyingPersonID
                    WHERE dt.ForAccompanyingPerson = 1
                    ORDER BY dt.IsRequired DESC, dt.DocumentName";
                    
                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                    
                    DataTable documentsTable = new DataTable();
                    adapter.Fill(documentsTable);
                    
                    dgDocuments.ItemsSource = documentsTable.DefaultView;
                    
                    // Обновляем статус документов
                    UpdateDocumentStatus(documentsTable);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке документов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDocumentStatus(DataTable documentsTable)
        {
            int requiredCount = 0;
            int uploadedCount = 0;
            int expiredCount = 0;
            
            foreach (DataRow row in documentsTable.Rows)
            {
                bool isRequired = Convert.ToBoolean(row["IsRequired"]);
                if (isRequired)
                {
                    requiredCount++;
                    
                    if (row["DocumentID"] != DBNull.Value)
                    {
                        // Проверка на истекший срок действия
                        if (row["ExpiryDate"] != DBNull.Value)
                        {
                            DateTime expiryDate = Convert.ToDateTime(row["ExpiryDate"]);
                            if (expiryDate < DateTime.Today)
                            {
                                expiredCount++;
                                continue;
                            }
                        }
                        
                        uploadedCount++;
                    }
                }
            }
            
            // Обновляем статус в интерфейсе
            if (requiredCount == 0)
            {
                txtDocumentStatus.Text = "Не требуются документы";
                txtDocumentStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
            else if (expiredCount > 0)
            {
                txtDocumentStatus.Text = $"Истек срок: {expiredCount} документ(ов)";
                txtDocumentStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (uploadedCount < requiredCount)
            {
                txtDocumentStatus.Text = $"Загружено {uploadedCount} из {requiredCount}";
                txtDocumentStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                txtDocumentStatus.Text = "Все документы загружены";
                txtDocumentStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void dgDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDocuments.SelectedItem != null && dgDocuments.SelectedItem is DataRowView row)
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
                MessageBox.Show("Пожалуйста, выберите тип документа для загрузки.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем информацию о типе документа
            string documentName = "";
            bool isRequired = false;
            int validityDays = 0;
            
            if (dgDocuments.SelectedItem is DataRowView row)
            {
                documentName = row["DocumentName"].ToString();
                isRequired = Convert.ToBoolean(row["IsRequired"]);
                
                // Проверяем, есть ли уже загруженный документ
                if (selectedDocumentID > 0)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Документ уже загружен. Хотите заменить его новой версией?",
                        "Подтверждение",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }
            
            // Открываем диалог выбора файла
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Файлы документов (*.pdf;*.jpg;*.jpeg;*.png;*.doc;*.docx)|*.pdf;*.jpg;*.jpeg;*.png;*.doc;*.docx|Все файлы (*.*)|*.*",
                Title = $"Выберите файл для документа '{documentName}'",
                CheckFileExists = true,
                Multiselect = false
            };
            
            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(selectedFilePath);
                
                // Создаем путь для сохранения копии файла в папке приложения
                string documentsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "Accompanying");
                
                // Создаем папку, если она не существует
                if (!Directory.Exists(documentsFolder))
                {
                    Directory.CreateDirectory(documentsFolder);
                }
                
                // Создаем уникальное имя файла
                string uniqueFileName = $"{accompanyingPersonID}_{selectedDocumentTypeID}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";
                string destinationPath = Path.Combine(documentsFolder, uniqueFileName);
                
                try
                {
                    // Копируем файл
                    File.Copy(selectedFilePath, destinationPath, true);
                    
                    // Получаем срок действия документа из базы данных
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        
                        string validityQuery = "SELECT ValidityDays FROM DocumentTypes WHERE DocumentTypeID = @DocumentTypeID";
                        using (SqlCommand cmd = new SqlCommand(validityQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@DocumentTypeID", selectedDocumentTypeID);
                            object result = cmd.ExecuteScalar();
                            if (result != DBNull.Value && result != null)
                            {
                                validityDays = Convert.ToInt32(result);
                            }
                        }
                        
                        // Рассчитываем дату окончания срока действия
                        DateTime? expiryDate = null;
                        if (validityDays > 0)
                        {
                            expiryDate = DateTime.Today.AddDays(validityDays);
                        }
                        
                        // Сохраняем информацию о документе в базу данных
                        if (selectedDocumentID > 0)
                        {
                            // Обновляем существующий документ
                            string updateQuery = @"
                                UPDATE AccompanyingPersonDocuments SET 
                                DocumentPath = @DocumentPath, 
                                UploadDate = @UploadDate, 
                                ExpiryDate = @ExpiryDate,
                                IsVerified = 0
                                WHERE DocumentID = @DocumentID";
                            
                            using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.Parameters.AddWithValue("@UploadDate", DateTime.Today);
                                
                                if (expiryDate.HasValue)
                                    cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate.Value);
                                else
                                    cmd.Parameters.AddWithValue("@ExpiryDate", DBNull.Value);
                                
                                cmd.Parameters.AddWithValue("@DocumentID", selectedDocumentID);
                                
                                cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            // Добавляем новый документ
                            string insertQuery = @"
                                INSERT INTO AccompanyingPersonDocuments (
                                    AccompanyingPersonID, 
                                    DocumentTypeID, 
                                    DocumentPath, 
                                    UploadDate, 
                                    ExpiryDate, 
                                    IsVerified
                                ) VALUES (
                                    @AccompanyingPersonID, 
                                    @DocumentTypeID, 
                                    @DocumentPath, 
                                    @UploadDate, 
                                    @ExpiryDate, 
                                    0
                                )";
                            
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                                cmd.Parameters.AddWithValue("@DocumentTypeID", selectedDocumentTypeID);
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.Parameters.AddWithValue("@UploadDate", DateTime.Today);
                                
                                if (expiryDate.HasValue)
                                    cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate.Value);
                                else
                                    cmd.Parameters.AddWithValue("@ExpiryDate", DBNull.Value);
                                
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    
                    MessageBox.Show($"Документ '{documentName}' успешно загружен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    
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
            
            try
            {
                string documentPath = "";
                
                // Получаем путь к файлу из базы данных
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    string query = "SELECT DocumentPath FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", selectedDocumentID);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            documentPath = result.ToString();
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(documentPath) && File.Exists(documentPath))
                {
                    // Открываем файл в программе по умолчанию
                    Process.Start(documentPath);
                }
                else
                {
                    MessageBox.Show("Файл документа не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
            
            // Запрос подтверждения
            MessageBoxResult result = MessageBox.Show(
                "Вы действительно хотите удалить выбранный документ?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            
            try
            {
                string documentPath = "";
                
                // Получаем путь к файлу и удаляем запись из базы данных
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Получаем путь к файлу
                    string pathQuery = "SELECT DocumentPath FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(pathQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", selectedDocumentID);
                        object result2 = cmd.ExecuteScalar();
                        if (result2 != null)
                        {
                            documentPath = result2.ToString();
                        }
                    }
                    
                    // Удаляем запись из базы данных
                    string deleteQuery = "DELETE FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", selectedDocumentID);
                        cmd.ExecuteNonQuery();
                    }
                }
                
                // Удаляем файл, если он существует
                if (!string.IsNullOrEmpty(documentPath) && File.Exists(documentPath))
                {
                    File.Delete(documentPath);
                }
                
                MessageBox.Show("Документ успешно удален.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Сбрасываем выбранный ID и обновляем список
                selectedDocumentID = -1;
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