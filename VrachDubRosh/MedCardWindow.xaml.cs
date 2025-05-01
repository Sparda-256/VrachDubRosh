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

namespace VrachDubRosh
{
    public partial class MedCardWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;
        private bool _isDarkTheme = false;

        // Данные пациента
        private DateTime? _dateOfBirth;
        private DateTime? _recordDate;
        private DateTime? _dischargeDate;
        private string _stayType;
        private string _accommodation;

        // Данные для отображения
        private DataTable _mainDiagnoses;
        private DataTable _secondaryDiagnoses;
        private DataTable _measurements;
        private DataTable _medications;
        private Dictionary<string, DataTable> _doctorProcedures = new Dictionary<string, DataTable>();

        public MedCardWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            
            this.Title = $"Санаторная книжка - {patientName}";
            txtPatientName.Text = patientName;
            
            this.Loaded += MedCardWindow_Loaded;
        }

        private void MedCardWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Определяем, применена ли темная тема в родительском окне
            if (this.Owner != null)
            {
                if (this.Owner is GlavDoctorWindow glavWindow && glavWindow.isDarkTheme)
                {
                    _isDarkTheme = true;
                    ApplyDarkTheme();
                }
                else if (this.Owner is DoctorWindow doctorWindow && doctorWindow.isDarkTheme)
                {
                    _isDarkTheme = true;
                    ApplyDarkTheme();
                }
            }

            // Загружаем данные пациента
            LoadPatientInfo();
            
            // Загружаем диагнозы
            LoadDiagnoses();
            
            // Загружаем антропометрические измерения
            LoadMeasurements();
            
            // Загружаем медикаменты
            LoadMedications();
            
            // Загружаем процедуры для каждого врача
            LoadProceduresByDoctor();
        }

        private void ApplyDarkTheme()
        {
            // Применяем темную тему
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        private void LoadPatientInfo()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate, p.StayType,
                               CONCAT('Корпус ', b.BuildingNumber, ', комната ', r.RoomNumber, ', кровать ', a.BedNumber) AS Accommodation
                        FROM Patients p
                        LEFT JOIN Accommodations a ON p.PatientID = a.PatientID
                        LEFT JOIN Rooms r ON a.RoomID = r.RoomID
                        LEFT JOIN Buildings b ON r.BuildingID = b.BuildingID
                        WHERE p.PatientID = @PatientID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Сохраняем данные
                                _dateOfBirth = reader["DateOfBirth"] as DateTime?;
                                _recordDate = reader["RecordDate"] as DateTime?;
                                _dischargeDate = reader["DischargeDate"] as DateTime?;
                                _stayType = reader["StayType"].ToString();
                                _accommodation = reader["Accommodation"].ToString();

                                // Отображаем данные
                                txtPatientFullName.Text = reader["FullName"].ToString();
                                txtPatientDateOfBirth.Text = _dateOfBirth.HasValue 
                                    ? _dateOfBirth.Value.ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                txtPatientRecordDate.Text = _recordDate.HasValue 
                                    ? _recordDate.Value.ToString("dd.MM.yyyy") 
                                    : "Не указана";
                                
                                txtPatientDischargeDate.Text = _dischargeDate.HasValue 
                                    ? _dischargeDate.Value.ToString("dd.MM.yyyy") 
                                    : "Не определена";
                                
                                txtStayType.Text = _stayType;
                                txtAccommodation.Text = String.IsNullOrEmpty(_accommodation) 
                                    ? "Не определено" 
                                    : _accommodation;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке информации о пациенте: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDiagnoses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Загружаем основные диагнозы
                    string mainQuery = @"
                        SELECT d.DiagnosisName
                        FROM PatientDiagnoses pd
                        JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                        WHERE pd.PatientID = @PatientID AND pd.DiagnosisType = 'Основной'";

                    SqlDataAdapter mainAdapter = new SqlDataAdapter(mainQuery, con);
                    mainAdapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    _mainDiagnoses = new DataTable();
                    mainAdapter.Fill(_mainDiagnoses);
                    dgMainDiagnoses.ItemsSource = _mainDiagnoses.DefaultView;

                    // Загружаем сопутствующие диагнозы
                    string secQuery = @"
                        SELECT d.DiagnosisName
                        FROM PatientDiagnoses pd
                        JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                        WHERE pd.PatientID = @PatientID AND (pd.DiagnosisType = 'Сопутствующий' OR pd.DiagnosisType IS NULL)";

                    SqlDataAdapter secAdapter = new SqlDataAdapter(secQuery, con);
                    secAdapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    _secondaryDiagnoses = new DataTable();
                    secAdapter.Fill(_secondaryDiagnoses);
                    dgSecondaryDiagnoses.ItemsSource = _secondaryDiagnoses.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMeasurements()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            MeasurementType,
                            Height,
                            Weight,
                            BloodPressure,
                            MeasurementDate
                        FROM PatientMeasurements
                        WHERE PatientID = @PatientID
                        ORDER BY MeasurementDate DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    _measurements = new DataTable();
                    adapter.Fill(_measurements);
                    dgMeasurements.ItemsSource = _measurements.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке антропометрических данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMedications()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            MedicationName,
                            Dosage,
                            Instructions,
                            PrescribedDate
                        FROM PatientMedications
                        WHERE PatientID = @PatientID
                        ORDER BY PrescribedDate DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    _medications = new DataTable();
                    adapter.Fill(_medications);
                    dgMedications.ItemsSource = _medications.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке медикаментов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProceduresByDoctor()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Сначала получаем список врачей, назначенных данному пациенту
                    string doctorsQuery = @"
                        SELECT DISTINCT 
                            d.DoctorID,
                            d.FullName,
                            d.Specialty,
                            d.GeneralName,
                            d.OfficeNumber
                        FROM Doctors d
                        JOIN PatientDoctorAssignments pda ON d.DoctorID = pda.DoctorID
                        WHERE pda.PatientID = @PatientID
                        ORDER BY d.GeneralName";

                    using (SqlCommand cmd = new SqlCommand(doctorsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int doctorID = Convert.ToInt32(reader["DoctorID"]);
                                string doctorName = reader["FullName"].ToString();
                                string specialty = reader["Specialty"].ToString();
                                string generalName = reader["GeneralName"].ToString();
                                string officeNumber = reader["OfficeNumber"].ToString();
                                
                                // Если общее наименование пустое, используем специальность
                                string tabTitle = !string.IsNullOrEmpty(generalName) 
                                    ? generalName 
                                    : specialty;
                                
                                // Создаем ключ для словаря и названия вкладки
                                string key = tabTitle;
                                
                                // Добавляем новую вкладку для этого врача/специальности
                                TabItem newTab = new TabItem
                                {
                                    Header = tabTitle
                                };
                                
                                // Будем запрашивать процедуры для этого врача и пациента
                                LoadProceduresForDoctor(doctorID, key);
                                
                                // Создаем контент для вкладки
                                Grid grid = new Grid { Margin = new Thickness(15) };
                                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                                
                                // Заголовок вкладки
                                TextBlock titleBlock = new TextBlock
                                {
                                    Text = $"{tabTitle.ToUpper()}, кабинет {officeNumber}",
                                    FontSize = 18,
                                    FontWeight = FontWeights.Bold,
                                    Foreground = (Brush)FindResource("AccentBrush"),
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 0, 0, 15)
                                };
                                Grid.SetRow(titleBlock, 0);
                                
                                // Подзаголовок
                                TextBlock subtitleBlock = new TextBlock
                                {
                                    Text = $"НАЗНАЧЕННЫЕ ПРОЦЕДУРЫ",
                                    FontSize = 16,
                                    FontWeight = FontWeights.SemiBold,
                                    Foreground = (Brush)FindResource("ForegroundBrush"),
                                    Margin = new Thickness(0, 0, 0, 10)
                                };
                                Grid.SetRow(subtitleBlock, 1);
                                
                                // DataGrid для процедур
                                DataGrid proceduresGrid = new DataGrid
                                {
                                    Style = (Style)FindResource("ModernDataGridStyle"),
                                    AutoGenerateColumns = false,
                                    IsReadOnly = true
                                };
                                
                                // Настраиваем столбцы для DataGrid
                                DataGridTextColumn nameColumn = new DataGridTextColumn
                                {
                                    Header = "Наименование",
                                    Binding = new System.Windows.Data.Binding("ProcedureName"),
                                    Width = new DataGridLength(2, DataGridLengthUnitType.Star)
                                };
                                
                                DataGridTextColumn dateColumn = new DataGridTextColumn
                                {
                                    Header = "Дата и время",
                                    Binding = new System.Windows.Data.Binding("AppointmentDateTime") 
                                    { 
                                        StringFormat = "{0:dd.MM.yyyy HH:mm}" 
                                    },
                                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                                };
                                
                                DataGridTextColumn statusColumn = new DataGridTextColumn
                                {
                                    Header = "Статус",
                                    Binding = new System.Windows.Data.Binding("Status"),
                                    Width = new DataGridLength(1, DataGridLengthUnitType.Star)
                                };
                                
                                proceduresGrid.Columns.Add(nameColumn);
                                proceduresGrid.Columns.Add(dateColumn);
                                proceduresGrid.Columns.Add(statusColumn);
                                
                                // Привязываем данные, если они уже загружены
                                if (_doctorProcedures.ContainsKey(key))
                                {
                                    proceduresGrid.ItemsSource = _doctorProcedures[key].DefaultView;
                                }
                                
                                Grid.SetRow(proceduresGrid, 2);
                                
                                // Добавляем элементы в Grid
                                grid.Children.Add(titleBlock);
                                grid.Children.Add(subtitleBlock);
                                grid.Children.Add(proceduresGrid);
                                
                                // Устанавливаем содержимое вкладки
                                newTab.Content = grid;
                                
                                // Добавляем вкладку в TabControl
                                tabControlMedCard.Items.Add(newTab);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке вкладок процедур: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProceduresForDoctor(int doctorID, string key)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            pa.AppointmentID,
                            pr.ProcedureName,
                            pa.AppointmentDateTime,
                            pa.Status,
                            pa.Description
                        FROM ProcedureAppointments pa
                        JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        WHERE pa.PatientID = @PatientID AND pa.DoctorID = @DoctorID
                        ORDER BY pa.AppointmentDateTime DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    adapter.SelectCommand.Parameters.AddWithValue("@DoctorID", doctorID);
                    
                    DataTable dtProcedures = new DataTable();
                    adapter.Fill(dtProcedures);
                    
                    // Сохраняем результат в словаре
                    _doctorProcedures[key] = dtProcedures;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке процедур для врача: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintMedicalCard();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PrintMedicalCard()
        {
            try
            {
                // Создаем документ для печати
                FlowDocument document = CreatePrintDocument();

                // Настраиваем параметры печати
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Создаем пагинатор для нашего документа
                    IDocumentPaginatorSource paginatorSource = document;
                    
                    // Печатаем документ
                    printDialog.PrintDocument(paginatorSource.DocumentPaginator, "Санаторная книжка");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при печати медицинской карточки: " + ex.Message, "Ошибка печати", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreatePrintDocument()
        {
            FlowDocument doc = new FlowDocument();
            doc.PagePadding = new Thickness(50);
            doc.FontFamily = new FontFamily("Arial");
            doc.FontSize = 12;
            
            // Стиль для заголовков
            Style headerStyle = new Style(typeof(Paragraph));
            headerStyle.Setters.Add(new Setter(Block.FontWeightProperty, FontWeights.Bold));
            headerStyle.Setters.Add(new Setter(Block.FontSizeProperty, 16.0));
            headerStyle.Setters.Add(new Setter(Block.TextAlignmentProperty, TextAlignment.Center));
            headerStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 10, 0, 10)));
            
            // Стиль для подзаголовков
            Style subheaderStyle = new Style(typeof(Paragraph));
            subheaderStyle.Setters.Add(new Setter(Block.FontWeightProperty, FontWeights.Bold));
            subheaderStyle.Setters.Add(new Setter(Block.FontSizeProperty, 14.0));
            subheaderStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 10, 0, 5)));
            
            // Стиль для обычного текста
            Style textStyle = new Style(typeof(Paragraph));
            textStyle.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0, 0, 0, 5)));
            
            // ===== Титульный лист =====
            Paragraph titleHeader = new Paragraph(new Run("САНАТОРНАЯ КНИЖКА"))
            {
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center
            };
            doc.Blocks.Add(titleHeader);
            
            Paragraph patientNamePara = new Paragraph(new Run(_patientName))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 20)
            };
            doc.Blocks.Add(patientNamePara);
            
            // Основная информация о пациенте
            doc.Blocks.Add(new Paragraph(new Run($"Дата рождения: {(_dateOfBirth.HasValue ? _dateOfBirth.Value.ToString("dd.MM.yyyy") : "Не указана")}")));
            
            Run treatmentPeriod = new Run($"Срок лечения: с {(_recordDate.HasValue ? _recordDate.Value.ToString("dd.MM.yyyy") : "не указана")} по {(_dischargeDate.HasValue ? _dischargeDate.Value.ToString("dd.MM.yyyy") : "не определена")}");
            doc.Blocks.Add(new Paragraph(treatmentPeriod));
            
            doc.Blocks.Add(new Paragraph(new Run($"Тип размещения: {_stayType}")));
            doc.Blocks.Add(new Paragraph(new Run($"Проживание: {_accommodation}")));
            
            // Добавляем разрыв страницы
            doc.Blocks.Add(new BlockUIContainer(new System.Windows.Controls.Border 
            { 
                Height = 20 
            }));
            doc.Blocks.Add(new Paragraph(new Run(" ")) { BreakPageBefore = true });
            
            // ===== Диагнозы =====
            Paragraph diagnosesHeader = new Paragraph(new Run("ДИАГНОЗЫ"))
            {
                Style = headerStyle
            };
            doc.Blocks.Add(diagnosesHeader);
            
            // Основной диагноз
            Paragraph mainDiagnosesSubheader = new Paragraph(new Run("а) Основной:"))
            {
                Style = subheaderStyle
            };
            doc.Blocks.Add(mainDiagnosesSubheader);
            
            if (_mainDiagnoses != null && _mainDiagnoses.Rows.Count > 0)
            {
                foreach (DataRow row in _mainDiagnoses.Rows)
                {
                    doc.Blocks.Add(new Paragraph(new Run($"• {row["DiagnosisName"]}")) { Style = textStyle });
                }
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("Не указан")) { Style = textStyle });
            }
            
            // Сопутствующий диагноз
            Paragraph secondaryDiagnosesSubheader = new Paragraph(new Run("б) Сопутствующий:"))
            {
                Style = subheaderStyle
            };
            doc.Blocks.Add(secondaryDiagnosesSubheader);
            
            if (_secondaryDiagnoses != null && _secondaryDiagnoses.Rows.Count > 0)
            {
                foreach (DataRow row in _secondaryDiagnoses.Rows)
                {
                    doc.Blocks.Add(new Paragraph(new Run($"• {row["DiagnosisName"]}")) { Style = textStyle });
                }
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("Не указан")) { Style = textStyle });
            }
            
            // Добавляем разрыв страницы
            doc.Blocks.Add(new Paragraph(new Run(" ")) { BreakPageBefore = true });
            
            // ===== Процедуры по врачам =====
            foreach (string key in _doctorProcedures.Keys)
            {
                DataTable dtProcs = _doctorProcedures[key];
                if (dtProcs.Rows.Count == 0) continue;
                
                // Заголовок с названием специальности
                Paragraph specialtyHeader = new Paragraph(new Run(key.ToUpper()))
                {
                    Style = headerStyle
                };
                doc.Blocks.Add(specialtyHeader);
                
                // Подзаголовок для процедур
                Paragraph procsSubheader = new Paragraph(new Run("НАЗНАЧЕННЫЕ ПРОЦЕДУРЫ:"))
                {
                    Style = subheaderStyle
                };
                doc.Blocks.Add(procsSubheader);
                
                // Создаем таблицу с процедурами
                Table procTable = new Table();
                procTable.CellSpacing = 0;
                procTable.BorderThickness = new Thickness(1);
                procTable.BorderBrush = Brushes.Black;
                
                // Добавляем столбцы
                procTable.Columns.Add(new TableColumn() { Width = new GridLength(3, GridUnitType.Star) });
                procTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
                procTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                
                // Создаем заголовок таблицы
                TableRowGroup headerGroup = new TableRowGroup();
                TableRow headerRow = new TableRow();
                headerRow.Background = Brushes.LightGray;
                headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Наименование")))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дата и время")))));
                headerRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Статус")))));
                headerGroup.Rows.Add(headerRow);
                procTable.RowGroups.Add(headerGroup);
                
                // Добавляем строки с данными
                TableRowGroup dataGroup = new TableRowGroup();
                foreach (DataRow row in dtProcs.Rows)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["ProcedureName"].ToString()))));
                    
                    DateTime dt = Convert.ToDateTime(row["AppointmentDateTime"]);
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(dt.ToString("dd.MM.yyyy HH:mm")))));
                    
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Status"].ToString()))));
                    dataGroup.Rows.Add(dataRow);
                }
                procTable.RowGroups.Add(dataGroup);
                
                doc.Blocks.Add(procTable);
                
                // Добавляем разрыв страницы после каждой специальности
                doc.Blocks.Add(new Paragraph(new Run(" ")) { BreakPageBefore = true });
            }
            
            // ===== Антропометрические измерения =====
            Paragraph measHeader = new Paragraph(new Run("АНТРОПОМЕТРИЧЕСКИЕ ИЗМЕРЕНИЯ"))
            {
                Style = headerStyle
            };
            doc.Blocks.Add(measHeader);
            
            if (_measurements != null && _measurements.Rows.Count > 0)
            {
                // Создаем таблицу с измерениями
                Table measTable = new Table();
                measTable.CellSpacing = 0;
                measTable.BorderThickness = new Thickness(1);
                measTable.BorderBrush = Brushes.Black;
                
                // Добавляем столбцы
                measTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
                measTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                measTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                measTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                measTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                
                // Создаем заголовок таблицы
                TableRowGroup mHeaderGroup = new TableRowGroup();
                TableRow mHeaderRow = new TableRow();
                mHeaderRow.Background = Brushes.LightGray;
                mHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Тип измерения")))));
                mHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Рост (см)")))));
                mHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Вес (кг)")))));
                mHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Давление")))));
                mHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дата")))));
                mHeaderGroup.Rows.Add(mHeaderRow);
                measTable.RowGroups.Add(mHeaderGroup);
                
                // Добавляем строки с данными
                TableRowGroup mDataGroup = new TableRowGroup();
                foreach (DataRow row in _measurements.Rows)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["MeasurementType"].ToString()))));
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Height"].ToString()))));
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Weight"].ToString()))));
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["BloodPressure"].ToString()))));
                    
                    DateTime dt = Convert.ToDateTime(row["MeasurementDate"]);
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(dt.ToString("dd.MM.yyyy")))));
                    
                    mDataGroup.Rows.Add(dataRow);
                }
                measTable.RowGroups.Add(mDataGroup);
                
                doc.Blocks.Add(measTable);
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("Нет данных измерений")) { Style = textStyle });
            }
            
            // ===== Медикаменты =====
            Paragraph medHeader = new Paragraph(new Run("МЕДИКАМЕНТЫ"))
            {
                Style = headerStyle
            };
            doc.Blocks.Add(medHeader);
            
            if (_medications != null && _medications.Rows.Count > 0)
            {
                // Создаем таблицу с медикаментами
                Table medTable = new Table();
                medTable.CellSpacing = 0;
                medTable.BorderThickness = new Thickness(1);
                medTable.BorderBrush = Brushes.Black;
                
                // Добавляем столбцы
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
                medTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                
                // Создаем заголовок таблицы
                TableRowGroup medHeaderGroup = new TableRowGroup();
                TableRow medHeaderRow = new TableRow();
                medHeaderRow.Background = Brushes.LightGray;
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Наименование")))));
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дозировка")))));
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Инструкции")))));
                medHeaderRow.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("Дата назначения")))));
                medHeaderGroup.Rows.Add(medHeaderRow);
                medTable.RowGroups.Add(medHeaderGroup);
                
                // Добавляем строки с данными
                TableRowGroup medDataGroup = new TableRowGroup();
                foreach (DataRow row in _medications.Rows)
                {
                    TableRow dataRow = new TableRow();
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["MedicationName"].ToString()))));
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Dosage"].ToString()))));
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(row["Instructions"].ToString()))));
                    
                    DateTime dt = Convert.ToDateTime(row["PrescribedDate"]);
                    dataRow.Cells.Add(new TableCell(new Paragraph(new Run(dt.ToString("dd.MM.yyyy")))));
                    
                    medDataGroup.Rows.Add(dataRow);
                }
                medTable.RowGroups.Add(medDataGroup);
                
                doc.Blocks.Add(medTable);
            }
            else
            {
                doc.Blocks.Add(new Paragraph(new Run("Нет назначенных медикаментов")) { Style = textStyle });
            }
            
            return doc;
        }

        private void btnAddMainDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалог выбора диагноза из справочника
            DiagnosesWindow diagnosesWindow = new DiagnosesWindow(_patientID, _patientName, true);
            diagnosesWindow.Owner = this;
            diagnosesWindow.DiagnosisSelected += (diagnosisID, diagnosisName) => 
            {
                try
                {
                    // Добавляем основной диагноз пациенту
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"
                            INSERT INTO PatientDiagnoses (PatientID, DiagnosisID, DiagnosisType)
                            VALUES (@PatientID, @DiagnosisID, 'Основной')";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                            cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    
                    // Обновляем отображение основных диагнозов
                    LoadDiagnoses();
                    MessageBox.Show("Основной диагноз добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при добавлении диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            diagnosesWindow.ShowDialog();
        }

        private void btnRemoveMainDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (dgMainDiagnoses.SelectedItem == null)
            {
                MessageBox.Show("Выберите диагноз для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDiagnosis = dgMainDiagnoses.SelectedItem as DataRowView;
            string diagnosisName = selectedDiagnosis["DiagnosisName"].ToString();

            if (MessageBox.Show($"Вы уверены, что хотите удалить основной диагноз '{diagnosisName}'?", 
                               "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Получаем ID диагноза
                    int diagnosisID = GetDiagnosisID(diagnosisName);
                    
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"
                            DELETE FROM PatientDiagnoses 
                            WHERE PatientID = @PatientID 
                            AND DiagnosisID = @DiagnosisID
                            AND DiagnosisType = 'Основной'";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                            cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    
                    // Обновляем отображение основных диагнозов
                    LoadDiagnoses();
                    MessageBox.Show("Основной диагноз удален", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnAddSecondaryDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалог выбора диагноза из справочника
            DiagnosesWindow diagnosesWindow = new DiagnosesWindow(_patientID, _patientName, true);
            diagnosesWindow.Owner = this;
            diagnosesWindow.DiagnosisSelected += (diagnosisID, diagnosisName) => 
            {
                try
                {
                    // Добавляем сопутствующий диагноз пациенту
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"
                            INSERT INTO PatientDiagnoses (PatientID, DiagnosisID, DiagnosisType)
                            VALUES (@PatientID, @DiagnosisID, 'Сопутствующий')";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                            cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    
                    // Обновляем отображение сопутствующих диагнозов
                    LoadDiagnoses();
                    MessageBox.Show("Сопутствующий диагноз добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при добавлении диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            diagnosesWindow.ShowDialog();
        }

        private void btnRemoveSecondaryDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (dgSecondaryDiagnoses.SelectedItem == null)
            {
                MessageBox.Show("Выберите диагноз для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDiagnosis = dgSecondaryDiagnoses.SelectedItem as DataRowView;
            string diagnosisName = selectedDiagnosis["DiagnosisName"].ToString();

            if (MessageBox.Show($"Вы уверены, что хотите удалить сопутствующий диагноз '{diagnosisName}'?", 
                               "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // Получаем ID диагноза
                    int diagnosisID = GetDiagnosisID(diagnosisName);
                    
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"
                            DELETE FROM PatientDiagnoses 
                            WHERE PatientID = @PatientID 
                            AND DiagnosisID = @DiagnosisID
                            AND (DiagnosisType = 'Сопутствующий' OR DiagnosisType IS NULL)";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                            cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    
                    // Обновляем отображение сопутствующих диагнозов
                    LoadDiagnoses();
                    MessageBox.Show("Сопутствующий диагноз удален", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private int GetDiagnosisID(string diagnosisName)
        {
            int diagnosisID = -1;
            
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DiagnosisID FROM Diagnoses WHERE DiagnosisName = @DiagnosisName";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DiagnosisName", diagnosisName);
                        object result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            diagnosisID = Convert.ToInt32(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при получении ID диагноза: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return diagnosisID;
        }

        private void btnSaveDiagnoses_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Изменения диагнозов сохранены", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAddMeasurement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем и настраиваем окно для добавления антропометрических измерений
                Window measurementWindow = new Window
                {
                    Title = "Добавление измерения",
                    Width = 400,
                    Height = 370,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем Grid для размещения элементов
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Заголовок
                TextBlock titleBlock = new TextBlock
                {
                    Text = "Добавление антропометрического измерения",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(titleBlock, 0);
                Grid.SetColumnSpan(titleBlock, 2);

                // Тип измерения
                TextBlock typeLabel = new TextBlock
                {
                    Text = "Тип измерения:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(typeLabel, 1);
                Grid.SetColumn(typeLabel, 0);

                ComboBox typeComboBox = new ComboBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                typeComboBox.Items.Add("При поступлении");
                typeComboBox.Items.Add("В процессе лечения");
                typeComboBox.Items.Add("При выписке");
                typeComboBox.SelectedIndex = 0;
                Grid.SetRow(typeComboBox, 1);
                Grid.SetColumn(typeComboBox, 1);

                // Рост
                TextBlock heightLabel = new TextBlock
                {
                    Text = "Рост (см):",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(heightLabel, 2);
                Grid.SetColumn(heightLabel, 0);

                TextBox heightTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                Grid.SetRow(heightTextBox, 2);
                Grid.SetColumn(heightTextBox, 1);

                // Вес
                TextBlock weightLabel = new TextBlock
                {
                    Text = "Вес (кг):",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(weightLabel, 3);
                Grid.SetColumn(weightLabel, 0);

                TextBox weightTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                Grid.SetRow(weightTextBox, 3);
                Grid.SetColumn(weightTextBox, 1);

                // Кровяное давление
                TextBlock bpLabel = new TextBlock
                {
                    Text = "Кровяное давление:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(bpLabel, 4);
                Grid.SetColumn(bpLabel, 0);

                TextBox bpTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = "120/80"
                };
                Grid.SetRow(bpTextBox, 4);
                Grid.SetColumn(bpTextBox, 1);

                // Кнопки
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Button saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                saveButton.Click += (s, args) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(heightTextBox.Text) ||
                            string.IsNullOrWhiteSpace(weightTextBox.Text) ||
                            string.IsNullOrWhiteSpace(bpTextBox.Text))
                        {
                            MessageBox.Show("Пожалуйста, заполните все поля", "Предупреждение");
                            return;
                        }

                        // Проверка ввода на корректность числовых значений
                        if (!decimal.TryParse(heightTextBox.Text, out decimal height) ||
                            !decimal.TryParse(weightTextBox.Text, out decimal weight))
                        {
                            MessageBox.Show("Пожалуйста, введите корректные числовые значения для роста и веса", "Ошибка");
                            return;
                        }

                        // Получаем данные из формы
                        string measurementType = typeComboBox.SelectedItem.ToString();
                        string bloodPressure = bpTextBox.Text;

                        // Сохраняем данные в базу
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = @"
                                INSERT INTO PatientMeasurements 
                                    (PatientID, MeasurementType, Height, Weight, BloodPressure, MeasurementDate, MeasuredBy) 
                                VALUES 
                                    (@PatientID, @MeasurementType, @Height, @Weight, @BloodPressure, @MeasurementDate, @MeasuredBy)";

                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", _patientID);
                                cmd.Parameters.AddWithValue("@MeasurementType", measurementType);
                                cmd.Parameters.AddWithValue("@Height", height);
                                cmd.Parameters.AddWithValue("@Weight", weight);
                                cmd.Parameters.AddWithValue("@BloodPressure", bloodPressure);
                                cmd.Parameters.AddWithValue("@MeasurementDate", DateTime.Now);
                                
                                // Получаем ID главврача (если пользователь - главврач)
                                if (this.Owner is GlavDoctorWindow glavDoctorWindow && glavDoctorWindow.ChiefDoctorID > 0)
                                {
                                    cmd.Parameters.AddWithValue("@MeasuredBy", glavDoctorWindow.ChiefDoctorID);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@MeasuredBy", DBNull.Value);
                                }

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Обновляем отображение измерений
                        LoadMeasurements();
                        MessageBox.Show("Измерение успешно добавлено", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        measurementWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при добавлении измерения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Button cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 30,
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                cancelButton.Click += (s, args) => measurementWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 5);
                Grid.SetColumnSpan(buttonPanel, 2);

                // Добавляем элементы на форму
                grid.Children.Add(titleBlock);
                grid.Children.Add(typeLabel);
                grid.Children.Add(typeComboBox);
                grid.Children.Add(heightLabel);
                grid.Children.Add(heightTextBox);
                grid.Children.Add(weightLabel);
                grid.Children.Add(weightTextBox);
                grid.Children.Add(bpLabel);
                grid.Children.Add(bpTextBox);
                grid.Children.Add(buttonPanel);

                measurementWindow.Content = grid;
                measurementWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна измерений: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEditMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (dgMeasurements.SelectedItem == null)
            {
                MessageBox.Show("Выберите измерение для редактирования", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Получаем данные выбранного измерения
                DataRowView selectedRow = dgMeasurements.SelectedItem as DataRowView;
                if (selectedRow == null) return;

                // Получаем ID измерения
                int measurementID = -1;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT MeasurementID 
                        FROM PatientMeasurements 
                        WHERE PatientID = @PatientID 
                        AND MeasurementType = @MeasurementType 
                        AND MeasurementDate = @MeasurementDate";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        cmd.Parameters.AddWithValue("@MeasurementType", selectedRow["MeasurementType"].ToString());
                        cmd.Parameters.AddWithValue("@MeasurementDate", Convert.ToDateTime(selectedRow["MeasurementDate"]));
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            measurementID = Convert.ToInt32(result);
                        }
                    }
                }

                if (measurementID == -1)
                {
                    MessageBox.Show("Не удалось найти измерение в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем окно для редактирования
                Window measurementWindow = new Window
                {
                    Title = "Редактирование измерения",
                    Width = 400,
                    Height = 370,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем Grid для размещения элементов
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Заголовок
                TextBlock titleBlock = new TextBlock
                {
                    Text = "Редактирование антропометрического измерения",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(titleBlock, 0);
                Grid.SetColumnSpan(titleBlock, 2);

                // Тип измерения
                TextBlock typeLabel = new TextBlock
                {
                    Text = "Тип измерения:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(typeLabel, 1);
                Grid.SetColumn(typeLabel, 0);

                ComboBox typeComboBox = new ComboBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                typeComboBox.Items.Add("При поступлении");
                typeComboBox.Items.Add("В процессе лечения");
                typeComboBox.Items.Add("При выписке");
                // Устанавливаем текущее значение
                string currentType = selectedRow["MeasurementType"].ToString();
                typeComboBox.SelectedIndex = typeComboBox.Items.IndexOf(currentType);
                if (typeComboBox.SelectedIndex == -1) typeComboBox.SelectedIndex = 0;
                
                Grid.SetRow(typeComboBox, 1);
                Grid.SetColumn(typeComboBox, 1);

                // Рост
                TextBlock heightLabel = new TextBlock
                {
                    Text = "Рост (см):",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(heightLabel, 2);
                Grid.SetColumn(heightLabel, 0);

                TextBox heightTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = selectedRow["Height"].ToString()
                };
                Grid.SetRow(heightTextBox, 2);
                Grid.SetColumn(heightTextBox, 1);

                // Вес
                TextBlock weightLabel = new TextBlock
                {
                    Text = "Вес (кг):",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(weightLabel, 3);
                Grid.SetColumn(weightLabel, 0);

                TextBox weightTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = selectedRow["Weight"].ToString()
                };
                Grid.SetRow(weightTextBox, 3);
                Grid.SetColumn(weightTextBox, 1);

                // Кровяное давление
                TextBlock bpLabel = new TextBlock
                {
                    Text = "Кровяное давление:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(bpLabel, 4);
                Grid.SetColumn(bpLabel, 0);

                TextBox bpTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = selectedRow["BloodPressure"].ToString()
                };
                Grid.SetRow(bpTextBox, 4);
                Grid.SetColumn(bpTextBox, 1);

                // Кнопки
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Button saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                saveButton.Click += (s, args) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(heightTextBox.Text) ||
                            string.IsNullOrWhiteSpace(weightTextBox.Text) ||
                            string.IsNullOrWhiteSpace(bpTextBox.Text))
                        {
                            MessageBox.Show("Пожалуйста, заполните все поля", "Предупреждение");
                            return;
                        }

                        // Проверка ввода на корректность числовых значений
                        if (!decimal.TryParse(heightTextBox.Text, out decimal height) ||
                            !decimal.TryParse(weightTextBox.Text, out decimal weight))
                        {
                            MessageBox.Show("Пожалуйста, введите корректные числовые значения для роста и веса", "Ошибка");
                            return;
                        }

                        // Получаем данные из формы
                        string measurementType = typeComboBox.SelectedItem.ToString();
                        string bloodPressure = bpTextBox.Text;

                        // Обновляем данные в базе
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = @"
                                UPDATE PatientMeasurements 
                                SET MeasurementType = @MeasurementType,
                                    Height = @Height,
                                    Weight = @Weight,
                                    BloodPressure = @BloodPressure
                                WHERE MeasurementID = @MeasurementID";

                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@MeasurementID", measurementID);
                                cmd.Parameters.AddWithValue("@MeasurementType", measurementType);
                                cmd.Parameters.AddWithValue("@Height", height);
                                cmd.Parameters.AddWithValue("@Weight", weight);
                                cmd.Parameters.AddWithValue("@BloodPressure", bloodPressure);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Обновляем отображение измерений
                        LoadMeasurements();
                        MessageBox.Show("Измерение успешно обновлено", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        measurementWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при обновлении измерения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Button cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 30,
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                cancelButton.Click += (s, args) => measurementWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 5);
                Grid.SetColumnSpan(buttonPanel, 2);

                // Добавляем элементы на форму
                grid.Children.Add(titleBlock);
                grid.Children.Add(typeLabel);
                grid.Children.Add(typeComboBox);
                grid.Children.Add(heightLabel);
                grid.Children.Add(heightTextBox);
                grid.Children.Add(weightLabel);
                grid.Children.Add(weightTextBox);
                grid.Children.Add(bpLabel);
                grid.Children.Add(bpTextBox);
                grid.Children.Add(buttonPanel);

                measurementWindow.Content = grid;
                measurementWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при редактировании измерения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoveMeasurement_Click(object sender, RoutedEventArgs e)
        {
            if (dgMeasurements.SelectedItem == null)
            {
                MessageBox.Show("Выберите измерение для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView selectedRow = dgMeasurements.SelectedItem as DataRowView;
            string measurementType = selectedRow["MeasurementType"].ToString();
            DateTime measurementDate = Convert.ToDateTime(selectedRow["MeasurementDate"]);

            if (MessageBox.Show($"Вы уверены, что хотите удалить измерение типа '{measurementType}' от {measurementDate.ToString("dd.MM.yyyy")}?", 
                               "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"
                            DELETE FROM PatientMeasurements 
                            WHERE PatientID = @PatientID 
                            AND MeasurementType = @MeasurementType 
                            AND MeasurementDate = @MeasurementDate";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                            cmd.Parameters.AddWithValue("@MeasurementType", measurementType);
                            cmd.Parameters.AddWithValue("@MeasurementDate", measurementDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    
                    // Обновляем отображение измерений
                    LoadMeasurements();
                    MessageBox.Show("Измерение успешно удалено", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении измерения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnSaveMeasurements_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Измерения сохранены", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnAddMedication_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем и настраиваем окно для добавления медикаментов
                Window medicationWindow = new Window
                {
                    Title = "Добавление медикамента",
                    Width = 450,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем Grid для размещения элементов
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Заголовок
                TextBlock titleBlock = new TextBlock
                {
                    Text = "Добавление медикамента",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(titleBlock, 0);
                Grid.SetColumnSpan(titleBlock, 2);

                // Наименование
                TextBlock nameLabel = new TextBlock
                {
                    Text = "Наименование:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(nameLabel, 1);
                Grid.SetColumn(nameLabel, 0);

                TextBox nameTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                Grid.SetRow(nameTextBox, 1);
                Grid.SetColumn(nameTextBox, 1);

                // Дозировка
                TextBlock dosageLabel = new TextBlock
                {
                    Text = "Дозировка:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dosageLabel, 2);
                Grid.SetColumn(dosageLabel, 0);

                TextBox dosageTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30
                };
                Grid.SetRow(dosageTextBox, 2);
                Grid.SetColumn(dosageTextBox, 1);

                // Инструкции
                TextBlock instructionsLabel = new TextBlock
                {
                    Text = "Инструкции:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetRow(instructionsLabel, 3);
                Grid.SetColumn(instructionsLabel, 0);

                TextBox instructionsTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 100,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Grid.SetRow(instructionsTextBox, 3);
                Grid.SetColumn(instructionsTextBox, 1);

                // Кнопки
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Button saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                saveButton.Click += (s, args) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                        {
                            MessageBox.Show("Пожалуйста, укажите наименование медикамента", "Предупреждение");
                            return;
                        }

                        // Получаем данные из формы
                        string medicationName = nameTextBox.Text.Trim();
                        string dosage = dosageTextBox.Text.Trim();
                        string instructions = instructionsTextBox.Text.Trim();

                        // Сохраняем данные в базу
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = @"
                                INSERT INTO PatientMedications 
                                    (PatientID, MedicationName, Dosage, Instructions, PrescribedDate, PrescribedBy) 
                                VALUES 
                                    (@PatientID, @MedicationName, @Dosage, @Instructions, @PrescribedDate, @PrescribedBy)";

                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", _patientID);
                                cmd.Parameters.AddWithValue("@MedicationName", medicationName);
                                cmd.Parameters.AddWithValue("@Dosage", string.IsNullOrEmpty(dosage) ? DBNull.Value : (object)dosage);
                                cmd.Parameters.AddWithValue("@Instructions", string.IsNullOrEmpty(instructions) ? DBNull.Value : (object)instructions);
                                cmd.Parameters.AddWithValue("@PrescribedDate", DateTime.Now);
                                
                                // Получаем ID главврача (если пользователь - главврач)
                                if (this.Owner is GlavDoctorWindow glavDoctorWindow && glavDoctorWindow.ChiefDoctorID > 0)
                                {
                                    cmd.Parameters.AddWithValue("@PrescribedBy", glavDoctorWindow.ChiefDoctorID);
                                }
                                else
                                {
                                    cmd.Parameters.AddWithValue("@PrescribedBy", DBNull.Value);
                                }

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Обновляем отображение медикаментов
                        LoadMedications();
                        MessageBox.Show("Медикамент успешно добавлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        medicationWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при добавлении медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Button cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 30,
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                cancelButton.Click += (s, args) => medicationWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 4);
                Grid.SetColumnSpan(buttonPanel, 2);

                // Добавляем элементы на форму
                grid.Children.Add(titleBlock);
                grid.Children.Add(nameLabel);
                grid.Children.Add(nameTextBox);
                grid.Children.Add(dosageLabel);
                grid.Children.Add(dosageTextBox);
                grid.Children.Add(instructionsLabel);
                grid.Children.Add(instructionsTextBox);
                grid.Children.Add(buttonPanel);

                medicationWindow.Content = grid;
                medicationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при открытии окна добавления медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEditMedication_Click(object sender, RoutedEventArgs e)
        {
            if (dgMedications.SelectedItem == null)
            {
                MessageBox.Show("Выберите медикамент для редактирования", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Получаем данные выбранного медикамента
                DataRowView selectedRow = dgMedications.SelectedItem as DataRowView;
                if (selectedRow == null) return;

                // Получаем ID медикамента
                int medicationID = -1;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT MedicationID 
                        FROM PatientMedications 
                        WHERE PatientID = @PatientID 
                        AND MedicationName = @MedicationName 
                        AND PrescribedDate = @PrescribedDate";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        cmd.Parameters.AddWithValue("@MedicationName", selectedRow["MedicationName"].ToString());
                        cmd.Parameters.AddWithValue("@PrescribedDate", Convert.ToDateTime(selectedRow["PrescribedDate"]));
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            medicationID = Convert.ToInt32(result);
                        }
                    }
                }

                if (medicationID == -1)
                {
                    MessageBox.Show("Не удалось найти медикамент в базе данных", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем окно для редактирования
                Window medicationWindow = new Window
                {
                    Title = "Редактирование медикамента",
                    Width = 450,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                // Создаем Grid для размещения элементов
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Заголовок
                TextBlock titleBlock = new TextBlock
                {
                    Text = "Редактирование медикамента",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 20),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetRow(titleBlock, 0);
                Grid.SetColumnSpan(titleBlock, 2);

                // Наименование
                TextBlock nameLabel = new TextBlock
                {
                    Text = "Наименование:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(nameLabel, 1);
                Grid.SetColumn(nameLabel, 0);

                TextBox nameTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = selectedRow["MedicationName"].ToString()
                };
                Grid.SetRow(nameTextBox, 1);
                Grid.SetColumn(nameTextBox, 1);

                // Дозировка
                TextBlock dosageLabel = new TextBlock
                {
                    Text = "Дозировка:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(dosageLabel, 2);
                Grid.SetColumn(dosageLabel, 0);

                TextBox dosageTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 30,
                    Text = selectedRow["Dosage"] != DBNull.Value ? selectedRow["Dosage"].ToString() : ""
                };
                Grid.SetRow(dosageTextBox, 2);
                Grid.SetColumn(dosageTextBox, 1);

                // Инструкции
                TextBlock instructionsLabel = new TextBlock
                {
                    Text = "Инструкции:",
                    Margin = new Thickness(0, 0, 10, 10),
                    VerticalAlignment = VerticalAlignment.Top
                };
                Grid.SetRow(instructionsLabel, 3);
                Grid.SetColumn(instructionsLabel, 0);

                TextBox instructionsTextBox = new TextBox
                {
                    Margin = new Thickness(0, 0, 0, 10),
                    Height = 100,
                    TextWrapping = TextWrapping.Wrap,
                    AcceptsReturn = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Text = selectedRow["Instructions"] != DBNull.Value ? selectedRow["Instructions"].ToString() : ""
                };
                Grid.SetRow(instructionsTextBox, 3);
                Grid.SetColumn(instructionsTextBox, 1);

                // Кнопки
                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                Button saveButton = new Button
                {
                    Content = "Сохранить",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(0, 0, 10, 0),
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                saveButton.Click += (s, args) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                        {
                            MessageBox.Show("Пожалуйста, укажите наименование медикамента", "Предупреждение");
                            return;
                        }

                        // Получаем данные из формы
                        string medicationName = nameTextBox.Text.Trim();
                        string dosage = dosageTextBox.Text.Trim();
                        string instructions = instructionsTextBox.Text.Trim();

                        // Обновляем данные в базе
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            string query = @"
                                UPDATE PatientMedications 
                                SET MedicationName = @MedicationName,
                                    Dosage = @Dosage,
                                    Instructions = @Instructions
                                WHERE MedicationID = @MedicationID";

                            using (SqlCommand cmd = new SqlCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@MedicationID", medicationID);
                                cmd.Parameters.AddWithValue("@MedicationName", medicationName);
                                cmd.Parameters.AddWithValue("@Dosage", string.IsNullOrEmpty(dosage) ? DBNull.Value : (object)dosage);
                                cmd.Parameters.AddWithValue("@Instructions", string.IsNullOrEmpty(instructions) ? DBNull.Value : (object)instructions);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Обновляем отображение медикаментов
                        LoadMedications();
                        MessageBox.Show("Медикамент успешно обновлен", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                        medicationWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при обновлении медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                Button cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 100,
                    Height = 30,
                    Style = (Style)FindResource("RoundedButtonStyle")
                };
                cancelButton.Click += (s, args) => medicationWindow.Close();

                buttonPanel.Children.Add(saveButton);
                buttonPanel.Children.Add(cancelButton);
                Grid.SetRow(buttonPanel, 4);
                Grid.SetColumnSpan(buttonPanel, 2);

                // Добавляем элементы на форму
                grid.Children.Add(titleBlock);
                grid.Children.Add(nameLabel);
                grid.Children.Add(nameTextBox);
                grid.Children.Add(dosageLabel);
                grid.Children.Add(dosageTextBox);
                grid.Children.Add(instructionsLabel);
                grid.Children.Add(instructionsTextBox);
                grid.Children.Add(buttonPanel);

                medicationWindow.Content = grid;
                medicationWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при редактировании медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoveMedication_Click(object sender, RoutedEventArgs e)
        {
            if (dgMedications.SelectedItem == null)
            {
                MessageBox.Show("Выберите медикамент для удаления", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView selectedRow = dgMedications.SelectedItem as DataRowView;
            string medicationName = selectedRow["MedicationName"].ToString();
            DateTime prescribedDate = Convert.ToDateTime(selectedRow["PrescribedDate"]);

            if (MessageBox.Show($"Вы уверены, что хотите удалить медикамент '{medicationName}'?", 
                               "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();
                        string query = @"
                            DELETE FROM PatientMedications 
                            WHERE PatientID = @PatientID 
                            AND MedicationName = @MedicationName 
                            AND PrescribedDate = @PrescribedDate";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                            cmd.Parameters.AddWithValue("@MedicationName", medicationName);
                            cmd.Parameters.AddWithValue("@PrescribedDate", prescribedDate);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    
                    // Обновляем отображение медикаментов
                    LoadMedications();
                    MessageBox.Show("Медикамент успешно удален", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении медикамента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnSaveMedications_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Медикаменты сохранены", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnSaveAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Все изменения успешно сохранены", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}