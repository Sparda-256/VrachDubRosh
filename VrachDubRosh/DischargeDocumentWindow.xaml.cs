using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;
using Microsoft.Office.Interop.Word;
using Word = Microsoft.Office.Interop.Word;
using WinParagraph = System.Windows.Documents.Paragraph;
using WordParagraph = Microsoft.Office.Interop.Word.Paragraph;

namespace VrachDubRosh
{
    public partial class DischargeDocumentWindow : System.Windows.Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int patientID;
        private string patientName;
        private string chiefDoctorName;
        
        // Constructor for creating/editing a discharge document
        public DischargeDocumentWindow(int patientID, string patientName, string chiefDoctorName)
        {
            InitializeComponent();
            this.patientID = patientID;
            this.patientName = patientName;
            this.chiefDoctorName = chiefDoctorName;
            
            // Set the chief doctor name
            txtChiefDoctor.Text = $"Главный врач: {this.chiefDoctorName}";
            
            // Load patient information
            LoadPatientInfo();
            
            // Load existing discharge document if it exists
            LoadDischargeDocument();
            
            // Set default selection for goal achieved
            if (cmbGoalAchieved.SelectedIndex == -1)
                cmbGoalAchieved.SelectedIndex = 0;
        }
        
        private void LoadPatientInfo()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    // Get patient basic information
                    string patientQuery = @"
                        SELECT FullName, DateOfBirth, Gender, RecordDate, DischargeDate, StayType
                        FROM Patients 
                        WHERE PatientID = @PatientID";
                    
                    using (SqlCommand command = new SqlCommand(patientQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientID);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string fullName = reader["FullName"].ToString();
                                DateTime dateOfBirth = Convert.ToDateTime(reader["DateOfBirth"]);
                                DateTime recordDate = Convert.ToDateTime(reader["RecordDate"]);
                                object dischargeDateObj = reader["DischargeDate"];
                                DateTime? dischargeDate = dischargeDateObj != DBNull.Value 
                                                        ? (DateTime?)Convert.ToDateTime(dischargeDateObj) 
                                                        : null;
                                
                                // Format the patient information
                                txtPatientInfo.Text = $"{fullName}, {dateOfBirth:dd.MM.yyyy}";
                                
                                // Format the stay information
                                string stayDateInfo = $"находился на лечении в отделении реалилитации с {recordDate:dd.MM.yyyy}";
                                if (dischargeDate.HasValue)
                                {
                                    stayDateInfo += $" по {dischargeDate.Value:dd.MM.yyyy}";
                                }
                                txtStayInfo.Text = stayDateInfo;
                            }
                        }
                    }
                    
                    // Get patient diagnoses
                    string diagnosesQuery = @"
                        SELECT d.DiagnosisName
                        FROM PatientDiagnoses pd
                        JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                        WHERE pd.PatientID = @PatientID";
                    
                    List<string> diagnoses = new List<string>();
                    
                    using (SqlCommand command = new SqlCommand(diagnosesQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientID);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnoses.Add(reader["DiagnosisName"].ToString());
                            }
                        }
                    }
                    
                    // Format diagnoses
                    if (diagnoses.Count > 0)
                    {
                        txtDiagnoses.Text = $"с: {string.Join(", ", diagnoses)}";
                    }
                    else
                    {
                        txtDiagnoses.Text = "с: Нет установленных диагнозов";
                    }
                    
                    // Get doctors assigned to the patient
                    string doctorsQuery = @"
                        SELECT d.FullName, d.Specialty
                        FROM PatientDoctorAssignments pda
                        JOIN Doctors d ON pda.DoctorID = d.DoctorID
                        WHERE pda.PatientID = @PatientID";
                    
                    List<string> doctors = new List<string>();
                    
                    using (SqlCommand command = new SqlCommand(doctorsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientID);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string doctorName = reader["FullName"].ToString();
                                string specialty = reader["Specialty"].ToString();
                                doctors.Add($"{doctorName} ({specialty})");
                            }
                        }
                    }
                    
                    // Format doctors
                    if (doctors.Count > 0)
                    {
                        txtDoctors.Text = string.Join(", ", doctors);
                    }
                    else
                    {
                        txtDoctors.Text = "Не назначены";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о пациенте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadDischargeDocument()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT Complaints, DiseaseHistory, InitialCondition, 
                               RehabilitationGoal, GoalAchieved, Recommendations
                        FROM DischargeDocuments
                        WHERE PatientID = @PatientID";
                    
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientID);
                        
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Fill form fields with database values
                                txtComplaints.Text = reader["Complaints"] as string;
                                txtDiseaseHistory.Text = reader["DiseaseHistory"] as string;
                                txtInitialCondition.Text = reader["InitialCondition"] as string;
                                txtRehabilitationGoal.Text = reader["RehabilitationGoal"] as string;
                                txtRecommendations.Text = reader["Recommendations"] as string;
                                
                                // Set goal achieved combobox
                                bool? goalAchieved = reader["GoalAchieved"] as bool?;
                                cmbGoalAchieved.SelectedIndex = goalAchieved.HasValue && goalAchieved.Value ? 0 : 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке выписного эпикриза: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SaveDischargeDocument()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    
                    bool goalAchieved = cmbGoalAchieved.SelectedIndex == 0;
                    
                    // Check if a discharge document already exists for this patient
                    string checkQuery = "SELECT COUNT(*) FROM DischargeDocuments WHERE PatientID = @PatientID";
                    bool documentExists = false;
                    
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@PatientID", patientID);
                        documentExists = (int)checkCommand.ExecuteScalar() > 0;
                    }
                    
                    string query = documentExists
                        ? @"
                            UPDATE DischargeDocuments
                            SET Complaints = @Complaints,
                                DiseaseHistory = @DiseaseHistory,
                                InitialCondition = @InitialCondition,
                                RehabilitationGoal = @RehabilitationGoal,
                                GoalAchieved = @GoalAchieved,
                                Recommendations = @Recommendations,
                                LastUpdated = GETDATE()
                            WHERE PatientID = @PatientID"
                        : @"
                            INSERT INTO DischargeDocuments
                            (PatientID, Complaints, DiseaseHistory, InitialCondition, 
                             RehabilitationGoal, GoalAchieved, Recommendations, LastUpdated)
                            VALUES
                            (@PatientID, @Complaints, @DiseaseHistory, @InitialCondition, 
                             @RehabilitationGoal, @GoalAchieved, @Recommendations, GETDATE())";
                    
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientID);
                        command.Parameters.AddWithValue("@Complaints", txtComplaints.Text ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@DiseaseHistory", txtDiseaseHistory.Text ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@InitialCondition", txtInitialCondition.Text ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@RehabilitationGoal", txtRehabilitationGoal.Text ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@GoalAchieved", goalAchieved);
                        command.Parameters.AddWithValue("@Recommendations", txtRecommendations.Text ?? (object)DBNull.Value);
                        
                        command.ExecuteNonQuery();
                    }
                }
                
                MessageBox.Show("Выписной эпикриз успешно сохранен.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении выписного эпикриза: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveDischargeDocument();
        }
        
        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            SaveDischargeDocument();
            try 
            {
                // Создаем документ без использования COM-объектов Word
                string filePath = GenerateDocumentWithoutWord();
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    MessageBox.Show($"Документ успешно создан: {filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Пытаемся открыть документ стандартным приложением для .docx файлов
                    try
                    {
                        System.Diagnostics.Process.Start(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Документ создан, но не удалось открыть его автоматически: {ex.Message}. " +
                                       $"Документ сохранен здесь: {filePath}", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    if (MessageBox.Show("Не удалось создать Word-документ. Хотите открыть простой предпросмотр?", 
                                      "Ошибка создания документа", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        ShowSimplePreview();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Метод для создания документа без использования COM-объектов Word
        private string GenerateDocumentWithoutWord()
        {
            try
            {
                // Создаем имя файла и путь 
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"Выписной_эпикриз_{patientName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.rtf";
                string filePath = System.IO.Path.Combine(desktopPath, fileName);
                
                // Создаем RTF документ
                StringBuilder rtf = new StringBuilder();
                
                // RTF заголовок
                rtf.Append(@"{\rtf1\ansi\ansicpg1251\deff0\deflang1049");
                
                // Определяем шрифты
                rtf.Append(@"{\fonttbl{\f0\froman\fprq2\fcharset204 Times New Roman;}}");
                
                // Начало документа - убираем \sa200 (отступ снизу) и уменьшаем размер заголовка до 28 (14pt)
                rtf.Append(@"\viewkind4\uc1\pard\sl276\slmult1\qc\b\f0\fs28 ВЫПИСНОЙ ЭПИКРИЗ\par");
                
                // Добавляем пустую строку после заголовка
                rtf.Append(@"\par");
                
                // Основной текст - размер 24 (12pt)
                // Информация о пациенте (жирный шрифт, выравнивание по левому краю)
                rtf.Append(@"\ql\b\fs24 ");
                rtf.Append(EscapeRtf(txtPatientInfo.Text));
                rtf.Append(@"\par");
                
                // Сведения о пребывании (обычный шрифт)
                rtf.Append(@"\b0 ");
                rtf.Append(EscapeRtf(txtStayInfo.Text));
                rtf.Append(@"\par");
                
                // Диагнозы
                rtf.Append(EscapeRtf(txtDiagnoses.Text));
                rtf.Append(@"\par");
                
                // Жалобы
                rtf.Append(@"\b Поступил с жалобами на:\b0\par ");
                rtf.Append(EscapeRtf(txtComplaints.Text));
                rtf.Append(@"\par");
                
                // Анамнез
                rtf.Append(@"\b Из анамнеза заболевания:\b0\par ");
                rtf.Append(EscapeRtf(txtDiseaseHistory.Text));
                rtf.Append(@"\par");
                
                // Состояние при поступлении
                rtf.Append(@"\b Общее состояние при поступлении:\b0\par ");
                rtf.Append(EscapeRtf(txtInitialCondition.Text));
                rtf.Append(@"\par");
                
                // Врачи
                rtf.Append(@"\b В составе мультидисциплинарной команды консультирован специалистами:\b0\par ");
                rtf.Append(EscapeRtf(txtDoctors.Text));
                rtf.Append(@"\par");
                
                // Цель реабилитации
                rtf.Append(@"\b Цель реабилитации:\b0\par ");
                rtf.Append(EscapeRtf(txtRehabilitationGoal.Text));
                rtf.Append(@"\par");
                
                // Достижение цели
                string goalAchievedText = cmbGoalAchieved.SelectedIndex == 0 
                    ? "Цель реабилитации достигнута. Лечение закончено"
                    : "Цель реабилитации не достигнута";
                rtf.Append(@"\b ");
                rtf.Append(goalAchievedText);
                rtf.Append(@"\b0\par");
                
                // Рекомендации
                rtf.Append(@"\b Рекомендации:\b0\par ");
                rtf.Append(EscapeRtf(txtRecommendations.Text));
                rtf.Append(@"\par");
                
                // Пустая строка перед подписью
                rtf.Append(@"\par");
                
                // Подпись (выравнивание по правому краю, жирный шрифт)
                rtf.Append(@"\qr\b ");
                rtf.Append(EscapeRtf(txtChiefDoctor.Text.Replace("Главный врач: ", "")));
                rtf.Append(@"\par");
                
                // Дополнительная пустая строка после ФИО главврача
                rtf.Append(@"\par");
                
                // М.П. (курсив)
                rtf.Append(@"\i М.П.\i0\par");
                
                // Закрытие RTF документа
                rtf.Append("}");
                
                // Записываем RTF-файл
                File.WriteAllText(filePath, rtf.ToString(), Encoding.GetEncoding(1251));
                
                MessageBox.Show($"RTF-документ создан и сохранен на рабочем столе: {fileName}\n\n" +
                              "Вы можете открыть его в Word и сохранить как .docx файл при необходимости.", 
                              "Документ создан", MessageBoxButton.OK, MessageBoxImage.Information);
                
                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании RTF-документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
        
        // Вспомогательный метод для экранирования символов RTF
        private string EscapeRtf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            return text
                .Replace("\\", "\\\\")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace("\r\n", "\\par ")
                .Replace("\n", "\\par ")
                .Replace("\r", "\\par ");
        }
        
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        private void ShowSimplePreview()
        {
            try
            {
                // Создаем простое окно предпросмотра
                System.Windows.Window previewWindow = new System.Windows.Window
                {
                    Title = "Предпросмотр выписного эпикриза",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this
                };

                // Создаем скроллвьювер для контента
                ScrollViewer scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Margin = new Thickness(20)
                };

                // Создаем флоудокумент для отображения содержимого
                FlowDocument document = new FlowDocument
                {
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                    FontSize = 12
                };

                // Добавляем заголовок
                WinParagraph titlePara = new WinParagraph(new Run("ВЫПИСНОЙ ЭПИКРИЗ"))
                {
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center
                };
                document.Blocks.Add(titlePara);

                // Добавляем информацию о пациенте
                WinParagraph patientPara = new WinParagraph(new Run(txtPatientInfo.Text))
                {
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(patientPara);

                // Добавляем сведения о пребывании
                document.Blocks.Add(new WinParagraph(new Run(txtStayInfo.Text)));
                document.Blocks.Add(new WinParagraph(new Run(txtDiagnoses.Text)));

                // Добавляем жалобы
                WinParagraph complaintsTitlePara = new WinParagraph(new Run("Поступил с жалобами на:"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(complaintsTitlePara);
                document.Blocks.Add(new WinParagraph(new Run(txtComplaints.Text)));

                // Добавляем анамнез
                WinParagraph historyTitlePara = new WinParagraph(new Run("Из анамнеза заболевания:"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(historyTitlePara);
                document.Blocks.Add(new WinParagraph(new Run(txtDiseaseHistory.Text)));

                // Добавляем состояние при поступлении
                WinParagraph conditionTitlePara = new WinParagraph(new Run("Общее состояние при поступлении:"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(conditionTitlePara);
                document.Blocks.Add(new WinParagraph(new Run(txtInitialCondition.Text)));

                // Добавляем информацию о врачах
                WinParagraph doctorsTitlePara = new WinParagraph(new Run("В составе мультидисциплинарной команды консультирован специалистами:"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(doctorsTitlePara);
                document.Blocks.Add(new WinParagraph(new Run(txtDoctors.Text)));

                // Добавляем цель реабилитации
                WinParagraph goalTitlePara = new WinParagraph(new Run("Цель реабилитации:"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(goalTitlePara);
                document.Blocks.Add(new WinParagraph(new Run(txtRehabilitationGoal.Text)));

                // Добавляем информацию о достижении цели
                string goalAchievedText = cmbGoalAchieved.SelectedIndex == 0 
                    ? "Цель реабилитации достигнута. Лечение закончено"
                    : "Цель реабилитации не достигнута";
                WinParagraph goalAchievedPara = new WinParagraph(new Run(goalAchievedText))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(goalAchievedPara);

                // Добавляем рекомендации
                WinParagraph recommendationsTitlePara = new WinParagraph(new Run("Рекомендации:"))
                {
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                document.Blocks.Add(recommendationsTitlePara);
                document.Blocks.Add(new WinParagraph(new Run(txtRecommendations.Text)));

                // Добавляем подпись
                WinParagraph chiefDoctorPara = new WinParagraph(new Run(txtChiefDoctor.Text.Replace("Главный врач: ", "")))
                {
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 30, 0, 0)
                };
                document.Blocks.Add(chiefDoctorPara);

                WinParagraph stampPara = new WinParagraph(new Run("М.П."))
                {
                    FontStyle = FontStyles.Italic,
                    TextAlignment = TextAlignment.Right
                };
                document.Blocks.Add(stampPara);

                // Добавляем кнопку печати
                Button printButton = new Button
                {
                    Content = "Печать",
                    Padding = new Thickness(15, 8, 15, 8),
                    Margin = new Thickness(0, 10, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                printButton.Click += (s, e) =>
                {
                    PrintDialog printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        // Создаем копию документа для печати
                        FlowDocument printDoc = new FlowDocument();
                        
                        // Копируем содержимое, используя корректный способ копирования блоков
                        foreach (var block in document.Blocks)
                        {
                            if (block is WinParagraph paragraph)
                            {
                                WinParagraph newParagraph = new WinParagraph();
                                TextRange originalRange = new TextRange(paragraph.ContentStart, paragraph.ContentEnd);
                                TextRange newRange = new TextRange(newParagraph.ContentStart, newParagraph.ContentEnd);
                                
                                newRange.Text = originalRange.Text;
                                newParagraph.FontWeight = paragraph.FontWeight;
                                newParagraph.FontSize = paragraph.FontSize;
                                newParagraph.FontStyle = paragraph.FontStyle;
                                newParagraph.TextAlignment = paragraph.TextAlignment;
                                newParagraph.Margin = paragraph.Margin;
                                
                                printDoc.Blocks.Add(newParagraph);
                            }
                        }
                        
                        // Настраиваем размер страницы для печати
                        printDoc.PagePadding = new Thickness(50);
                        printDoc.ColumnWidth = printDialog.PrintableAreaWidth;
                        IDocumentPaginatorSource paginatorSource = printDoc;
                        printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Выписной эпикриз");
                    }
                };

                // Создаем компоновку для содержимого и кнопки
                DockPanel dockPanel = new DockPanel();
                
                // Создаем DocumentViewer для просмотра документа
                FlowDocumentReader reader = new FlowDocumentReader
                {
                    Document = document,
                    ViewingMode = FlowDocumentReaderViewingMode.Scroll
                };
                
                DockPanel.SetDock(printButton, Dock.Bottom);
                dockPanel.Children.Add(printButton);
                dockPanel.Children.Add(reader);

                previewWindow.Content = dockPanel;
                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отображении предпросмотра: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 