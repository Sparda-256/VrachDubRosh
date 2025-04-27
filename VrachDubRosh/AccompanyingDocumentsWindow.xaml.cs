using System;
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
            
            foreach (DataRow row in documentsTable.Rows)
            {
                bool isRequired = Convert.ToBoolean(row["IsRequired"]);
                if (isRequired)
                {
                    requiredCount++;
                    
                    if (row["DocumentID"] != DBNull.Value)
                    {
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
                
                // Создаем имя для нового файла, включая тип документа
                string safeDocumentName = documentName.Replace('/', '_').Replace('\\', '_');
                string newFileName = $"{safeDocumentName}{fileExtension}";
                
                // Базовая директория для документов сопровождающих
                string baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "Сопровождающие");
                
                // Создаем директорию для сопровождающего
                string accompanyingFolder = Path.Combine(baseDirectory, accompanyingPersonName.Replace(' ', '_'));
                Directory.CreateDirectory(accompanyingFolder);
                
                // Полный путь к новому файлу
                string destinationPath = Path.Combine(accompanyingFolder, newFileName);
                
                // Проверяем, существует ли уже файл с таким именем
                if (File.Exists(destinationPath))
                {
                    // Если файл существует, добавляем порядковый номер
                    int counter = 1;
                    string fileNameWithoutExt = safeDocumentName;
                    
                    while (File.Exists(destinationPath))
                    {
                        newFileName = $"{fileNameWithoutExt}_{counter}{fileExtension}";
                        destinationPath = Path.Combine(accompanyingFolder, newFileName);
                        counter++;
                    }
                }
                
                try
                {
                    // Копируем файл
                    File.Copy(selectedFilePath, destinationPath, true);
                    
                    // Удаляем старый файл, если существует
                    if (selectedDocumentID > 0)
                    {
                        string oldPath = "";
                        
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = "SELECT DocumentPath FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID";
                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@DocumentID", selectedDocumentID);
                                oldPath = cmd.ExecuteScalar() as string;
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(oldPath) && File.Exists(oldPath))
                        {
                            try
                            {
                                File.Delete(oldPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Не удалось удалить старый файл: {ex.Message}");
                            }
                        }
                    }
                    
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        
                        // Сохраняем информацию о документе в базу данных
                        if (selectedDocumentID > 0)
                        {
                            // Обновляем существующий документ
                            string updateQuery = @"
                                UPDATE AccompanyingPersonDocuments SET 
                                DocumentPath = @DocumentPath, 
                                UploadDate = @UploadDate, 
                                IsVerified = 0
                                WHERE DocumentID = @DocumentID";
                            
                            using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.Parameters.AddWithValue("@UploadDate", DateTime.Today);
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
                                    IsVerified
                                ) VALUES (
                                    @AccompanyingPersonID, 
                                    @DocumentTypeID, 
                                    @DocumentPath, 
                                    @UploadDate, 
                                    0
                                )";
                            
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                                cmd.Parameters.AddWithValue("@DocumentTypeID", selectedDocumentTypeID);
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.Parameters.AddWithValue("@UploadDate", DateTime.Today);
                                
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
                    MessageBox.Show($"Ошибка при загрузке документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Пожалуйста, выберите загруженный документ для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Получаем информацию о документе
            DataRowView row = dgDocuments.SelectedItem as DataRowView;
            if (row == null) return;
            
            string documentName = row["DocumentName"].ToString();
            string documentPath = row["DocumentPath"].ToString();
            
            // Подтверждение удаления
            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите удалить документ '{documentName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
            
            try
            {
                // Удаляем файл, если он существует
                if (File.Exists(documentPath))
                {
                    File.Delete(documentPath);
                    
                    // Проверяем, стала ли директория сопровождающего пустой, и удаляем её если да
                    string accompanyingDir = Path.GetDirectoryName(documentPath);
                    if (Directory.Exists(accompanyingDir) && !Directory.EnumerateFileSystemEntries(accompanyingDir).Any())
                    {
                        try
                        {
                            Directory.Delete(accompanyingDir);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Не удалось удалить пустую директорию сопровождающего: {ex.Message}");
                        }
                    }
                }
                
                // Удаляем запись из базы данных
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    string query = "DELETE FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID";
                    
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
                MessageBox.Show($"Ошибка при удалении документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 