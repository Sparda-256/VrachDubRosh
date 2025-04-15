using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;

namespace VrachDubRosh
{
    public partial class MedCardWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;
        private DataRowView _selectedProcedure;
        private DataTable _patientDescriptions;
        private DataTable _proceduresData;
        private bool _isInitialLoad = true;
        private ObservableCollection<MedicalNote> _medicalNotes = new ObservableCollection<MedicalNote>();

        public MedCardWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            tbPatientName.Text = $"Медицинская карточка пациента: {patientName}";
            
            // Установка источника данных для ItemsControl заметок
            icMedicalNotes.ItemsSource = _medicalNotes;
            
            // Инициализация DatePicker
            dpStartDate.SelectedDate = DateTime.Today;
            dpEndDate.SelectedDate = DateTime.Today;
            
            LoadPatientInfo();
            LoadAllMedicalNotes();
            LoadProcedures();
            
            _isInitialLoad = false;
        }

        /// <summary>
        /// Загружает информацию о пациенте и отображает её в интерфейсе.
        /// </summary>
        private void LoadPatientInfo()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT DateOfBirth, Gender, RecordDate, DischargeDate 
                                     FROM Patients 
                                     WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                tbDateOfBirth.Text = reader["DateOfBirth"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["DateOfBirth"]).ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                tbGender.Text = reader["Gender"] != DBNull.Value 
                                    ? reader["Gender"].ToString() 
                                    : "Не указан";
                                
                                tbRecordDate.Text = reader["RecordDate"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["RecordDate"]).ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                tbDischargeDate.Text = reader["DischargeDate"] != DBNull.Value 
                                    ? Convert.ToDateTime(reader["DischargeDate"]).ToString("dd.MM.yyyy") 
                                    : "Не указана";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки информации о пациенте: " + ex.Message);
            }
        }

        /// <summary>
        /// Загружает все медицинские заметки для данного пациента от всех врачей
        /// </summary>
        private void LoadAllMedicalNotes()
        {
            try
            {
                // Получаем выбранные даты
                DateTime? startDate = dpStartDate.SelectedDate;
                DateTime? endDate = dpEndDate.SelectedDate;
                
                // Проверяем валидность дат
                if (startDate == null)
                    startDate = DateTime.MinValue;
                if (endDate == null)
                    endDate = DateTime.MaxValue;
                else
                    endDate = endDate.Value.AddDays(1).AddSeconds(-1); // До конца выбранного дня
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pd.PatientDescriptionID, pd.Description, pd.DescriptionDate, 
                                    pd.DoctorID, d.FullName as DoctorName
                                    FROM PatientDescriptions pd
                                    LEFT JOIN Doctors d ON pd.DoctorID = d.DoctorID
                                    WHERE pd.PatientID = @PatientID
                                    AND pd.DescriptionDate BETWEEN @StartDate AND @EndDate
                                    ORDER BY CONVERT(date, pd.DescriptionDate) DESC, pd.DescriptionDate DESC";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    da.SelectCommand.Parameters.AddWithValue("@StartDate", startDate);
                    da.SelectCommand.Parameters.AddWithValue("@EndDate", endDate);
                    _patientDescriptions = new DataTable();
                    da.Fill(_patientDescriptions);

                    if (_patientDescriptions.Rows.Count > 0)
                    {
                        // Заполняем комбобокс со списком врачей
                        LoadDoctorsComboBox();
                        
                        // Показываем заметки
                        DisplaySelectedNotes();
                    }
                    else
                    {
                        _medicalNotes.Clear();
                        cbDoctors.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки медицинских заметок: " + ex.Message);
            }
        }

        /// <summary>
        /// Заполняет комбобокс со списком врачей, оставивших заметки
        /// </summary>
        private void LoadDoctorsComboBox()
        {
            var uniqueDoctors = new List<DoctorItem>();
            var doctorsSet = new HashSet<int?>();
            
            foreach (DataRow row in _patientDescriptions.Rows)
            {
                int? doctorID = row["DoctorID"] != DBNull.Value ? Convert.ToInt32(row["DoctorID"]) : (int?)null;
                if (!doctorsSet.Contains(doctorID))
                {
                    string doctorName = row["DoctorName"] != DBNull.Value 
                        ? row["DoctorName"].ToString() 
                        : "Не указан";
                    
                    uniqueDoctors.Add(new DoctorItem { 
                        DoctorID = doctorID, 
                        DoctorName = doctorName 
                    });
                    
                    doctorsSet.Add(doctorID);
                }
            }
            
            // Добавляем опцию "Все врачи"
            uniqueDoctors.Insert(0, new DoctorItem { DoctorID = null, DoctorName = "Все врачи" });
            
            cbDoctors.DisplayMemberPath = "DoctorName";
            cbDoctors.SelectedValuePath = "DoctorID";
            cbDoctors.ItemsSource = uniqueDoctors;
            cbDoctors.SelectedIndex = 0; // Выбираем "Все врачи" по умолчанию
        }

        /// <summary>
        /// Отображает выбранные заметки
        /// </summary>
        private void DisplaySelectedNotes()
        {
            // Получаем выбранного врача
            DoctorItem selectedDoctor = cbDoctors.SelectedItem as DoctorItem;
            
            if (selectedDoctor == null)
            {
                _medicalNotes.Clear();
                return;
            }
            
            // Очищаем предыдущие заметки
            _medicalNotes.Clear();
            
            foreach (DataRow row in _patientDescriptions.Rows)
            {
                bool doctorMatch = !selectedDoctor.DoctorID.HasValue || 
                    (row["DoctorID"] != DBNull.Value && 
                     Convert.ToInt32(row["DoctorID"]) == selectedDoctor.DoctorID.Value);
                
                if (doctorMatch)
                {
                    string doctorName = row["DoctorName"] != DBNull.Value 
                        ? row["DoctorName"].ToString() 
                        : "Не указан";
                    
                    DateTime date = Convert.ToDateTime(row["DescriptionDate"]);
                    string description = row["Description"].ToString();
                    
                    // Добавляем заметку в коллекцию
                    _medicalNotes.Add(new MedicalNote
                    {
                        Date = date.ToString("dd.MM.yyyy HH:mm"),
                        Doctor = doctorName,
                        Description = description
                    });
                }
            }
        }

        /// <summary>
        /// Обработчик события выбора врача
        /// </summary>
        private void cbDoctors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialLoad)
            {
                DisplaySelectedNotes();
            }
        }

        /// <summary>
        /// Загружает список проведённых процедур для данного пациента.
        /// </summary>
        private void LoadProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pa.AppointmentID, pr.ProcedureName, pa.AppointmentDateTime, pa.Status, 
                                     d.FullName as DoctorName, pa.Description
                                     FROM ProcedureAppointments pa
                                     INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                                     LEFT JOIN Doctors d ON pa.DoctorID = d.DoctorID
                                     WHERE pa.PatientID = @PatientID
                                     ORDER BY pa.AppointmentDateTime DESC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgProcedures.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур: " + ex.Message);
            }
        }

        /// <summary>
        /// Обработчик события выбора процедуры в DataGrid
        /// </summary>
        private void dgProcedures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProcedures.SelectedItem != null)
            {
                _selectedProcedure = dgProcedures.SelectedItem as DataRowView;
                if (_selectedProcedure != null)
                {
                    // Загружаем описание процедуры
                    var description = _selectedProcedure["Description"];
                    txtProcedureDescription.Text = (description != null && description != DBNull.Value)
                        ? description.ToString()
                        : "Описание не указано";
                    
                    // Удаляем автоматическое открытие панели деталей
                }
            }
            else
            {
                txtProcedureDescription.Text = "";
            }
        }

        /// <summary>
        /// Обработчик двойного клика по строке процедуры
        /// </summary>
        private void dgProcedures_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgProcedures.SelectedItem != null)
            {
                DataRowView row = dgProcedures.SelectedItem as DataRowView;
                if (row != null)
                {
                    int appointmentID = Convert.ToInt32(row["AppointmentID"]);
                    // Открываем диалог просмотра детальной информации о процедуре
                    ShowProcedureDetailsDialog(appointmentID);
                }
            }
        }

        /// <summary>
        /// Показывает диалог с подробной информацией о процедуре
        /// </summary>
        private void ShowProcedureDetailsDialog(int appointmentID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pa.AppointmentID, pa.Description, 
                                    pr.ProcedureName, pa.AppointmentDateTime, pa.Status, 
                                    d.FullName as DoctorName, pr.Duration
                                    FROM ProcedureAppointments pa
                                    INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                                    LEFT JOIN Doctors d ON pa.DoctorID = d.DoctorID
                                    WHERE pa.AppointmentID = @AppointmentID";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AppointmentID", appointmentID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string procedureName = reader["ProcedureName"].ToString();
                                string doctorName = reader["DoctorName"] != DBNull.Value 
                                    ? reader["DoctorName"].ToString() 
                                    : "Не указан";
                                string dateTime = Convert.ToDateTime(reader["AppointmentDateTime"]).ToString("dd.MM.yyyy HH:mm");
                                string status = reader["Status"].ToString();
                                string duration = reader["Duration"] != DBNull.Value 
                                    ? reader["Duration"].ToString() + " мин." 
                                    : "Не указана";
                                string description = reader["Description"] != DBNull.Value 
                                    ? reader["Description"].ToString() 
                                    : "Описание отсутствует";

                                string message = $"Процедура: {procedureName}\n" +
                                                $"Врач: {doctorName}\n" +
                                                $"Дата и время: {dateTime}\n" +
                                                $"Статус: {status}\n" +
                                                $"Длительность: {duration}\n\n" +
                                                $"Описание:\n{description}";

                                MessageBox.Show(message, "Детали процедуры", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке деталей процедуры: " + ex.Message);
            }
        }
       
        /// <summary>
        /// Получает список всех врачей из базы данных
        /// </summary>
        private DataTable GetDoctorsList()
        {
            DataTable dtDoctors = new DataTable();
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DoctorID, FullName FROM Doctors ORDER BY FullName";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.Fill(dtDoctors);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении списка врачей: " + ex.Message);
                return null;
            }
            return dtDoctors;
        }

        /// <summary>
        /// Сохраняет новую медицинскую заметку в базу данных
        /// </summary>
        private void SaveNewMedicalNote(int? doctorID, string description)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"INSERT INTO PatientDescriptions (PatientID, DoctorID, Description, DescriptionDate) 
                                     VALUES (@PatientID, @DoctorID, @Description, @DescriptionDate)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        
                        if (doctorID.HasValue)
                            cmd.Parameters.AddWithValue("@DoctorID", doctorID.Value);
                        else
                            cmd.Parameters.AddWithValue("@DoctorID", DBNull.Value);
                        
                        cmd.Parameters.AddWithValue("@Description", description);
                        cmd.Parameters.AddWithValue("@DescriptionDate", DateTime.Now);
                        
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Медицинская заметка успешно добавлена.", "Информация", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении заметки: " + ex.Message);
            }
        }

        /// <summary>
        /// Открывает окно с диагнозами пациента
        /// </summary>
        private void btnDiagnoses_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DiagnosesWindow diagnosesWindow = new DiagnosesWindow(_patientID, _patientName);
                diagnosesWindow.Owner = this;
                diagnosesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии диагнозов пациента: " + ex.Message);
            }
        }

        /// <summary>
        /// Сохраняет изменения в медицинских заметках.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Изменения сохранены.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку печати
        /// </summary>
        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintMedicalCard();
        }

        /// <summary>
        /// Создаёт и печатает медицинскую карточку пациента
        /// </summary>
        private void PrintMedicalCard()
        {
            try
            {
                FlowDocument document = CreatePrintDocument();
                
                System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Настраиваем размер страницы
                    document.PageHeight = printDialog.PrintableAreaHeight;
                    document.PageWidth = printDialog.PrintableAreaWidth;
                    document.ColumnWidth = printDialog.PrintableAreaWidth;
                    
                    // Печать через DocumentWriter
                    IDocumentPaginatorSource paginatorSource = document;
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Медицинская карта - " + _patientName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при печати: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Создаёт документ FlowDocument для печати медицинской карты
        /// </summary>
        private FlowDocument CreatePrintDocument()
        {
            // Создание документа
            FlowDocument document = new FlowDocument();
            document.PagePadding = new Thickness(50);
            document.FontFamily = new FontFamily("Segoe UI");
            document.FontSize = 12;
            document.ColumnWidth = 700;

            // Стили для документа
            Style titleStyle = new Style(typeof(Paragraph));
            titleStyle.Setters.Add(new Setter(Block.FontSizeProperty, 20.0));
            titleStyle.Setters.Add(new Setter(Block.FontWeightProperty, FontWeights.Bold));
            titleStyle.Setters.Add(new Setter(Block.TextAlignmentProperty, TextAlignment.Center));
            titleStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 20)));

            Style sectionTitleStyle = new Style(typeof(Paragraph));
            sectionTitleStyle.Setters.Add(new Setter(Block.FontSizeProperty, 16.0));
            sectionTitleStyle.Setters.Add(new Setter(Block.FontWeightProperty, FontWeights.Bold));
            sectionTitleStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 15, 0, 5)));
            
            Style subtitleStyle = new Style(typeof(Paragraph));
            subtitleStyle.Setters.Add(new Setter(Block.FontSizeProperty, 14.0));
            subtitleStyle.Setters.Add(new Setter(Block.FontWeightProperty, FontWeights.SemiBold));
            subtitleStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 10, 0, 5)));

            // Заголовок документа
            Paragraph title = new Paragraph(new Run("Медицинская карта пациента"));
            title.Style = titleStyle;
            document.Blocks.Add(title);

            // Информация о пациенте
            Paragraph patientInfoTitle = new Paragraph(new Run("Информация о пациенте"));
            patientInfoTitle.Style = sectionTitleStyle;
            document.Blocks.Add(patientInfoTitle);

            Table patientInfoTable = new Table();
            patientInfoTable.CellSpacing = 0;
            patientInfoTable.BorderThickness = new Thickness(1);
            patientInfoTable.BorderBrush = Brushes.Black;

            // Определение колонок таблицы
            patientInfoTable.Columns.Add(new TableColumn() { Width = new GridLength(150) });
            patientInfoTable.Columns.Add(new TableColumn() { Width = new GridLength(400) });

            // Добавление строк таблицы
            TableRowGroup patientRowGroup = new TableRowGroup();
            patientInfoTable.RowGroups.Add(patientRowGroup);

            // ФИО пациента
            TableRow nameRow = new TableRow();
            nameRow.Cells.Add(new TableCell(new Paragraph(new Run("ФИО:"))));
            nameRow.Cells.Add(new TableCell(new Paragraph(new Run(_patientName))));
            patientRowGroup.Rows.Add(nameRow);

            // Дата рождения
            TableRow birthRow = new TableRow();
            birthRow.Cells.Add(new TableCell(new Paragraph(new Run("Дата рождения:"))));
            birthRow.Cells.Add(new TableCell(new Paragraph(new Run(tbDateOfBirth.Text))));
            patientRowGroup.Rows.Add(birthRow);

            // Пол
            TableRow genderRow = new TableRow();
            genderRow.Cells.Add(new TableCell(new Paragraph(new Run("Пол:"))));
            genderRow.Cells.Add(new TableCell(new Paragraph(new Run(tbGender.Text))));
            patientRowGroup.Rows.Add(genderRow);

            // Дата записи
            TableRow recordRow = new TableRow();
            recordRow.Cells.Add(new TableCell(new Paragraph(new Run("Дата записи:"))));
            recordRow.Cells.Add(new TableCell(new Paragraph(new Run(tbRecordDate.Text))));
            patientRowGroup.Rows.Add(recordRow);

            // Дата выписки
            TableRow dischargeRow = new TableRow();
            dischargeRow.Cells.Add(new TableCell(new Paragraph(new Run("Дата выписки:"))));
            dischargeRow.Cells.Add(new TableCell(new Paragraph(new Run(tbDischargeDate.Text))));
            patientRowGroup.Rows.Add(dischargeRow);

            document.Blocks.Add(patientInfoTable);

            // Раздел медицинских заметок
            Paragraph notesTitle = new Paragraph(new Run("Медицинские заметки"));
            notesTitle.Style = sectionTitleStyle;
            document.Blocks.Add(notesTitle);

            if (_medicalNotes.Count > 0)
            {
                foreach (var note in _medicalNotes)
                {
                    // Заголовок заметки с датой и врачом
                    Paragraph noteHeader = new Paragraph();
                    noteHeader.Style = subtitleStyle;
                    noteHeader.Inlines.Add(new Run(note.Date + " | " + note.Doctor));
                    document.Blocks.Add(noteHeader);

                    // Текст заметки
                    Paragraph noteText = new Paragraph(new Run(note.Description));
                    noteText.Margin = new Thickness(10, 0, 10, 15);
                    document.Blocks.Add(noteText);
                }
            }
            else
            {
                document.Blocks.Add(new Paragraph(new Run("Медицинские заметки отсутствуют.")));
            }

            // Процедуры пациента
            Paragraph proceduresTitle = new Paragraph(new Run("Проведённые процедуры"));
            proceduresTitle.Style = sectionTitleStyle;
            document.Blocks.Add(proceduresTitle);

            // Создание таблицы для процедур
            Table proceduresTable = new Table();
            proceduresTable.CellSpacing = 0;
            proceduresTable.BorderThickness = new Thickness(1);
            proceduresTable.BorderBrush = Brushes.Black;

            // Определение колонок таблицы
            proceduresTable.Columns.Add(new TableColumn() { Width = new GridLength(25) });
            proceduresTable.Columns.Add(new TableColumn() { Width = new GridLength(180) });
            proceduresTable.Columns.Add(new TableColumn() { Width = new GridLength(140) });
            proceduresTable.Columns.Add(new TableColumn() { Width = new GridLength(75) });
            proceduresTable.Columns.Add(new TableColumn() { Width = new GridLength(130) });

            // Добавление строк таблицы
            TableRowGroup proceduresRowGroup = new TableRowGroup();
            proceduresTable.RowGroups.Add(proceduresRowGroup);

            // Заголовок таблицы
            TableRow headerRow = new TableRow();
            headerRow.Background = Brushes.LightGray;
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("ID")))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Процедура")))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дата и время")))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Статус")))));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Врач")))));
            proceduresRowGroup.Rows.Add(headerRow);

            // Получение данных о процедурах
            DataView proceduresView = dgProcedures.ItemsSource as DataView;
            if (proceduresView != null && proceduresView.Count > 0)
            {
                foreach (DataRowView rowView in proceduresView)
                {
                    TableRow row = new TableRow();
                    row.Cells.Add(new TableCell(new Paragraph(new Run(rowView["AppointmentID"].ToString()))));
                    row.Cells.Add(new TableCell(new Paragraph(new Run(rowView["ProcedureName"].ToString()))));
                    
                    string dateTime = Convert.ToDateTime(rowView["AppointmentDateTime"]).ToString("dd.MM.yyyy HH:mm");
                    row.Cells.Add(new TableCell(new Paragraph(new Run(dateTime))));
                    
                    row.Cells.Add(new TableCell(new Paragraph(new Run(rowView["Status"].ToString()))));
                    
                    string doctorName = rowView["DoctorName"] != DBNull.Value 
                        ? rowView["DoctorName"].ToString() 
                        : "Не указан";
                    row.Cells.Add(new TableCell(new Paragraph(new Run(doctorName))));
                    
                    proceduresRowGroup.Rows.Add(row);
                }
            }
            else
            {
                TableRow row = new TableRow();
                TableCell cell = new TableCell(new Paragraph(new Run("Нет данных о процедурах")));
                cell.ColumnSpan = 5;
                row.Cells.Add(cell);
                proceduresRowGroup.Rows.Add(row);
            }

            document.Blocks.Add(proceduresTable);

            // Дата и время печати
            Paragraph footer = new Paragraph(new Run("Дата печати: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm")));
            footer.Margin = new Thickness(0, 30, 0, 0);
            footer.TextAlignment = TextAlignment.Right;
            document.Blocks.Add(footer);

            return document;
        }

        /// <summary>
        /// Обработчик изменения дат в DatePicker
        /// </summary>
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialLoad)
            {
                LoadAllMedicalNotes();
            }
        }
    }

    /// <summary>
    /// Класс для хранения информации о враче для комбобокса
    /// </summary>
    public class DoctorItem
    {
        public int? DoctorID { get; set; }
        public string DoctorName { get; set; }
    }

    /// <summary>
    /// Класс для хранения информации о медицинской заметке
    /// </summary>
    public class MedicalNote
    {
        public string Date { get; set; }
        public string Doctor { get; set; }
        public string Description { get; set; }
    }
}