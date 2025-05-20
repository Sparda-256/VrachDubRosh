using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Generic;

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
        private List<DataRowView> selectedRows = new List<DataRowView>();

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
            
            // Добавляем обработчик события закрытия окна
            this.Closed += AccompanyingDocumentsWindow_Closed;
        }
        
        private void AccompanyingDocumentsWindow_Closed(object sender, EventArgs e)
        {
            // Обновляем список сопровождающих в родительском окне
            if (Owner is ManagerWindow managerWindow)
            {
                managerWindow.LoadAccompanyingPersons();
            }
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
            selectedRows.Clear();
            
            if (dgDocuments.SelectedItems.Count == 1)
            {
                DataRowView row = dgDocuments.SelectedItem as DataRowView;
                if (row != null)
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
                    
                    selectedRows.Add(row);
                }
            }
            else if (dgDocuments.SelectedItems.Count > 1)
            {
                selectedDocumentTypeID = -1;
                selectedDocumentID = -1;
                
                foreach (DataRowView row in dgDocuments.SelectedItems)
                {
                    selectedRows.Add(row);
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
                            
                            // Сначала проверяем существование сопровождающего лица с данным ID
                            using (SqlCommand checkCmd = new SqlCommand("SELECT COUNT(1) FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID", con))
                            {
                                checkCmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                                int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                
                                if (count == 0)
                                {
                                    throw new Exception($"Сопровождающий с ID {accompanyingPersonID} не найден. Возможно, запись была удалена другим пользователем.");
                                }
                            }
                            
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
            if (dgDocuments.SelectedItems.Count != 1)
            {
                MessageBox.Show("Выберите один документ для просмотра.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            DataRowView row = dgDocuments.SelectedItem as DataRowView;
            if (row == null || row["DocumentPath"] == DBNull.Value) return;
            
            if (row["DocumentID"] == DBNull.Value)
            {
                MessageBox.Show("Документ не загружен. Его невозможно просмотреть.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
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
            if (dgDocuments.SelectedItems.Count == 0)
            {
                MessageBox.Show("Пожалуйста, выберите документы для удаления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Проверяем, все ли выбранные документы загружены
            List<DataRowView> rowsToDelete = new List<DataRowView>();
            foreach (DataRowView row in dgDocuments.SelectedItems)
            {
                if (row["DocumentID"] != DBNull.Value)
                {
                    rowsToDelete.Add(row);
                }
            }
            
            if (rowsToDelete.Count == 0)
            {
                MessageBox.Show("Среди выбранных документов нет загруженных.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Подтверждение удаления
            string confirmMessage = rowsToDelete.Count == 1 
                ? $"Вы действительно хотите удалить документ '{rowsToDelete[0]["DocumentName"]}'?"
                : $"Вы действительно хотите удалить {rowsToDelete.Count} выбранных документов?";
            
            MessageBoxResult result = MessageBox.Show(
                confirmMessage,
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return;
            
            int successCount = 0;
            
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    foreach (DataRowView row in rowsToDelete)
                    {
                        int documentID = Convert.ToInt32(row["DocumentID"]);
                        string documentPath = row["DocumentPath"].ToString();
                        
                        // Удаляем файл, если он существует
                        if (File.Exists(documentPath))
                        {
                            try
                            {
                                File.Delete(documentPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Не удалось удалить файл {documentPath}: {ex.Message}");
                            }
                        }
                        
                        // Удаляем запись из базы данных
                        string query = "DELETE FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@DocumentID", documentID);
                            cmd.ExecuteNonQuery();
                            successCount++;
                        }
                    }
                }
                
                // Проверяем, стала ли директория сопровождающего пустой, и удаляем её если да
                string accompanyingDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "Documents", 
                    "Сопровождающие", 
                    accompanyingPersonName.Replace(' ', '_'));
                
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
                
                string message = successCount == 1 
                    ? "Документ успешно удален." 
                    : $"{successCount} документов успешно удалено.";
                
                MessageBox.Show(message, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Обновляем список документов
                LoadDocuments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении документов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
} 