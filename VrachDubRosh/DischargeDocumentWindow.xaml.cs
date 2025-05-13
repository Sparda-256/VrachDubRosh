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
        private List<string> doctorsList;

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
                                string stayDateInfo = $"Находился на лечении в отделении реабилитации с {recordDate:dd.MM.yyyy}";
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

                    doctorsList = new List<string>();
                    // Get doctors assigned to the patient
                    string doctorsQuery = @"
                        SELECT d.FullName, d.Specialty
                        FROM PatientDoctorAssignments pda
                        JOIN Doctors d ON pda.DoctorID = d.DoctorID
                        WHERE pda.PatientID = @PatientID";
                    using (SqlCommand command = new SqlCommand(doctorsQuery, connection))
                    {
                        command.Parameters.AddWithValue("@PatientID", patientID);
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string doctorName = reader["FullName"].ToString();
                                string specialty = reader["Specialty"].ToString();
                                doctorsList.Add($"{doctorName} ({specialty})");
                            }
                        }
                    }

                    // Привязка списка к ItemsControl
                    if (doctorsList.Count > 0)
                    {
                        lstDoctors.ItemsSource = doctorsList;
                    }
                    else
                    {
                        lstDoctors.ItemsSource = new List<string> { "Не назначены" };
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
                rtf.Append(@"\b В составе мультидисциплинарной команды консультирован специалистами:\b0\par");
                if (doctorsList.Count > 0)
                {
                    foreach (string doctor in doctorsList)
                    {
                        rtf.Append(@"\li300 "); // Левый отступ 300 твипов
                        rtf.Append(EscapeRtf(doctor));
                        rtf.Append(@"\par");
                    }
                    // Сброс отступа после списка
                    rtf.Append(@"\li0 ");
                }
                else
                {
                    rtf.Append(@"\li300 Не назначены\par");
                    rtf.Append(@"\li0 ");
                }

                // После списка специалистов добавляем:
                rtf.Append(@"\pard"); // Сбрасываем все форматирования абзаца

                // Затем продолжаем с обычным форматированием:
                rtf.Append(@"\ql\b Цель реабилитации:\b0\par ");
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
    }
} 