using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace VrachDubRosh
{
    public partial class DiagnosesWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;
        private DataRowView _selectedDiagnosis;
        private List<DataRowView> _selectedDiagnoses = new List<DataRowView>();
        private bool _isSelectionMode = false;

        // Событие, которое будет вызываться при выборе диагноза
        public event Action<int, string> DiagnosisSelected;

        public DiagnosesWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            _isSelectionMode = false;
            tbPatientInfo.Text = $"Диагнозы пациента: {patientName}";
            
            LoadDiagnoses();
            
            this.Loaded += DiagnosesWindow_Loaded;
        }

        /// <summary>
        /// Конструктор с дополнительным параметром для режима выбора диагноза
        /// </summary>
        /// <param name="patientID">ID пациента</param>
        /// <param name="patientName">Имя пациента</param>
        /// <param name="isSelectionMode">Режим выбора диагноза</param>
        public DiagnosesWindow(int patientID, string patientName, bool isSelectionMode)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            _isSelectionMode = isSelectionMode;
            
            if (isSelectionMode)
            {
                tbPatientInfo.Text = $"Выберите диагноз для пациента: {patientName}";
                // Изменяем заголовок окна
                this.Title = "Выбор диагноза";
                
                // Скрываем стандартные кнопки управления, они не нужны в режиме выбора
                // Ищем кнопки по имени или через поиск в визуальном дереве
                UIElement addButton = FindButtonByContent("Добавить диагноз");
                UIElement editButton = btnEditDiagnosis; // У этой кнопки есть x:Name
                UIElement deleteButton = btnDeleteDiagnosis; // У этой кнопки есть x:Name
                
                if (addButton != null) addButton.Visibility = Visibility.Collapsed;
                if (editButton != null) editButton.Visibility = Visibility.Collapsed;
                if (deleteButton != null) deleteButton.Visibility = Visibility.Collapsed;
                
                // Добавляем кнопку выбора диагноза
                Button btnSelect = new Button
                {
                    Content = "Выбрать диагноз",
                    Margin = new Thickness(5),
                    Padding = new Thickness(10, 5, 10, 5),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Style = FindResource("BlueButtonStyle") as Style
                };
                btnSelect.Click += BtnSelect_Click;
                
                // Добавляем кнопку в контейнер кнопок
                // Находим родительский элемент (StackPanel) существующих кнопок
                StackPanel buttonPanel = FindButtonPanel();
                if (buttonPanel != null)
                {
                    buttonPanel.Children.Insert(0, btnSelect);
                }
            }
            else
            {
                tbPatientInfo.Text = $"Диагнозы пациента: {patientName}";
            }
            
            LoadDiagnoses();
            
            this.Loaded += DiagnosesWindow_Loaded;
        }

        /// <summary>
        /// Обработчик события загрузки окна
        /// </summary>
        private void DiagnosesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверяем тему родительского окна и применяем её
            if (this.Owner != null)
            {
                if (this.Owner is GlavDoctorWindow glavWindow && glavWindow.isDarkTheme)
                {
                    ApplyDarkTheme();
                }
                else if (this.Owner is DoctorWindow doctorWindow && doctorWindow.isDarkTheme)
                {
                    ApplyDarkTheme();
                }
            }
        }

        /// <summary>
        /// Применяет темную тему к окну
        /// </summary>
        private void ApplyDarkTheme()
        {
            // Применяем темную тему
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        /// <summary>
        /// Загружает список диагнозов пациента из базы данных
        /// </summary>
        private void LoadDiagnoses()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pd.PatientID, pd.DiagnosisID, d.DiagnosisName
                                    FROM PatientDiagnoses pd
                                    INNER JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                                    WHERE pd.PatientID = @PatientID";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgDiagnoses.ItemsSource = dt.DefaultView;
                    
                    // Очищаем детали диагноза
                    ClearDiagnosisDetails();
                    
                    // Отключаем кнопки, так как пока ничего не выбрано
                    btnEditDiagnosis.IsEnabled = false;
                    btnDeleteDiagnosis.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Очищает поля с деталями диагноза
        /// </summary>
        private void ClearDiagnosisDetails()
        {
            tbDiagnosisName.Text = "";
            
            // Отключаем кнопку редактирования
            btnEditDiagnosis.IsEnabled = false;
            // Кнопка удаления управляется отдельно в зависимости от выделенных строк
        }

        /// <summary>
        /// Обработчик события выбора диагноза в DataGrid
        /// </summary>
        private void dgDiagnoses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDiagnoses.Clear();
            
            // Собираем все выбранные диагнозы
            foreach (DataRowView item in dgDiagnoses.SelectedItems)
            {
                _selectedDiagnoses.Add(item);
            }
            
            // Активируем кнопку удаления, если есть выбранные диагнозы
            btnDeleteDiagnosis.IsEnabled = _selectedDiagnoses.Count > 0;
            
            // Для одиночного выбора - показываем детали и разрешаем редактирование
            if (dgDiagnoses.SelectedItems.Count == 1)
            {
                _selectedDiagnosis = dgDiagnoses.SelectedItem as DataRowView;
                if (_selectedDiagnosis != null)
                {
                    // Заполняем детали диагноза
                    tbDiagnosisName.Text = _selectedDiagnosis["DiagnosisName"].ToString();
                    
                    // Активируем кнопку редактирования
                    btnEditDiagnosis.IsEnabled = true;
                }
            }
            else
            {
                // Очищаем детали при множественном выборе
                tbDiagnosisName.Text = "";
                btnEditDiagnosis.IsEnabled = false;
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Добавить диагноз"
        /// </summary>
        private void btnAddDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            AddEditDiagnosisWindow addDiagnosisWindow = new AddEditDiagnosisWindow(_patientID, _patientName);
            addDiagnosisWindow.Owner = this;
            bool? result = addDiagnosisWindow.ShowDialog();
            
            if (result == true)
            {
                // Перезагружаем список диагнозов
                LoadDiagnoses();
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Редактировать"
        /// </summary>
        private void btnEditDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDiagnosis != null)
            {
                int diagnosisID = Convert.ToInt32(_selectedDiagnosis["DiagnosisID"]);
                AddEditDiagnosisWindow editDiagnosisWindow = new AddEditDiagnosisWindow(_patientID, _patientName, diagnosisID);
                editDiagnosisWindow.Owner = this;
                bool? result = editDiagnosisWindow.ShowDialog();
                
                if (result == true)
                {
                    // Перезагружаем список диагнозов
                    LoadDiagnoses();
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Удалить"
        /// </summary>
        private void btnDeleteDiagnosis_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDiagnoses.Count > 0)
            {
                string confirmMessage;
                if (_selectedDiagnoses.Count == 1)
                {
                    confirmMessage = $"Вы действительно хотите удалить диагноз \"{_selectedDiagnoses[0]["DiagnosisName"]}\" у пациента {_patientName}?";
                }
                else
                {
                    confirmMessage = $"Вы действительно хотите удалить {_selectedDiagnoses.Count} выбранных диагнозов у пациента {_patientName}?";
                }
                
                // Запрашиваем подтверждение на удаление
                MessageBoxResult result = MessageBox.Show(
                    confirmMessage,
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        int deletedCount = 0;
                        
                        using (SqlConnection con = new SqlConnection(connectionString))
                        {
                            con.Open();
                            
                            foreach (DataRowView diagnosis in _selectedDiagnoses)
                            {
                                int diagnosisID = Convert.ToInt32(diagnosis["DiagnosisID"]);
                                string query = "DELETE FROM PatientDiagnoses WHERE PatientID = @PatientID AND DiagnosisID = @DiagnosisID";
                                
                                using (SqlCommand cmd = new SqlCommand(query, con))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", _patientID);
                                    cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisID);
                                    
                                    deletedCount += cmd.ExecuteNonQuery();
                                }
                            }
                            
                            if (deletedCount > 0)
                            {
                                string successMessage = deletedCount == 1 
                                    ? "Диагноз успешно удален" 
                                    : $"Успешно удалено {deletedCount} диагнозов";
                                    
                                MessageBox.Show(successMessage, "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                // Перезагружаем список диагнозов
                                LoadDiagnoses();
                            }
                            else
                            {
                                MessageBox.Show("Не удалось удалить диагнозы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении диагнозов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Закрыть"
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Обработчик нажатия на кнопку "Выбрать диагноз"
        /// </summary>
        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDiagnosis != null)
            {
                // Получаем ID и название выбранного диагноза
                int diagnosisID = Convert.ToInt32(_selectedDiagnosis["DiagnosisID"]);
                string diagnosisName = _selectedDiagnosis["DiagnosisName"].ToString();
                
                // Вызываем событие с ID и названием диагноза
                DiagnosisSelected?.Invoke(diagnosisID, diagnosisName);
                
                // Закрываем окно
                this.Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите диагноз из списка", "Не выбран диагноз", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Находит StackPanel, который содержит кнопки
        /// </summary>
        private StackPanel FindButtonPanel()
        {
            // Ищем StackPanel в третьей строке грида
            Grid mainGrid = this.Content as Grid;
            if (mainGrid != null && mainGrid.Children.Count > 0)
            {
                var viewbox = mainGrid.Children[0] as Viewbox;
                if (viewbox != null && viewbox.Child != null)
                {
                    var grid = viewbox.Child as Grid;
                    if (grid != null)
                    {
                        foreach (UIElement child in grid.Children)
                        {
                            if (child is StackPanel panel && Grid.GetRow(child) == 3)
                            {
                                return panel;
                            }
                        }
                    }
                }
            }
            return null;
        }
        
        /// <summary>
        /// Находит кнопку по содержимому
        /// </summary>
        private UIElement FindButtonByContent(string content)
        {
            StackPanel buttonPanel = FindButtonPanel();
            if (buttonPanel != null)
            {
                foreach (UIElement element in buttonPanel.Children)
                {
                    if (element is Button button && button.Content.ToString() == content)
                    {
                        return button;
                    }
                }
            }
            return null;
        }
    }
} 