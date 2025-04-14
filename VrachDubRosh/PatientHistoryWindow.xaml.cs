using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace VrachDubRosh
{
    public partial class PatientHistoryWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;

        public PatientHistoryWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            tbPatientName.Text = $"История болезни пациента: {patientName}";
            
            Loaded += PatientHistoryWindow_Loaded;
        }

        private void PatientHistoryWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPatientHistory();
        }

        /// <summary>
        /// Загружает историю медицинских записей пациента
        /// </summary>
        private void LoadPatientHistory()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    // Запрос для получения всех записей в истории болезни с информацией о враче
                    string query = @"SELECT pd.PatientDescriptionID, pd.Description, pd.DescriptionDate, 
                                    d.FullName as DoctorName,
                                    CASE 
                                        WHEN LEN(pd.Description) <= 50 THEN pd.Description 
                                        ELSE SUBSTRING(pd.Description, 1, 50) + '...' 
                                    END as ShortDescription
                                    FROM PatientDescriptions pd
                                    LEFT JOIN Doctors d ON pd.DoctorID = d.DoctorID
                                    WHERE pd.PatientID = @PatientID
                                    ORDER BY pd.DescriptionDate DESC";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    
                    dgHistory.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки истории болезни: " + ex.Message);
            }
        }

        /// <summary>
        /// Обрабатывает событие выбора записи в истории болезни
        /// </summary>
        private void dgHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgHistory.SelectedItem != null)
            {
                DataRowView selectedRow = dgHistory.SelectedItem as DataRowView;
                if (selectedRow != null)
                {
                    // Выводим полное описание выбранной записи
                    string fullDescription = selectedRow["Description"].ToString();
                    txtFullDescription.Text = fullDescription;
                }
            }
            else
            {
                txtFullDescription.Text = string.Empty;
            }
        }

        
        /// <summary>
        /// Обрабатывает нажатие на кнопку "Закрыть"
        /// </summary>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Загружает процедуры из истории пациента
        /// </summary>
        private void LoadPatientProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT pa.AppointmentID, pr.ProcedureName, pa.AppointmentDateTime, 
                                  pa.Status, d.FullName as DoctorName, pa.Description
                                  FROM ProcedureAppointments pa
                                  INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                                  LEFT JOIN Doctors d ON pa.DoctorID = d.DoctorID
                                  WHERE pa.PatientID = @PatientID
                                  ORDER BY pa.AppointmentDateTime DESC";
                    
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", _patientID);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    
                    // Здесь можно было бы добавить отображение процедур, 
                    // если бы для них был отдельный контрол в интерфейсе
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур пациента: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Окно для выбора врача при добавлении заметки
    /// </summary>
    public class SelectDoctorWindow : Window
    {
        private ComboBox cbDoctors;
        public int? SelectedDoctorID { get; private set; }

        public SelectDoctorWindow(DataTable dtDoctors)
        {
            // Настройка окна
            Title = "Выбор врача";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.WhiteSmoke;
            
            // Создаем элементы интерфейса
            Grid grid = new Grid();
            grid.Margin = new Thickness(20);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Заголовок
            TextBlock tbHeader = new TextBlock
            {
                Text = "Выберите врача для создания заметки",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(tbHeader, 0);
            
            // Выпадающий список врачей
            cbDoctors = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 20),
                Height = 30,
                DisplayMemberPath = "FullName",
                SelectedValuePath = "DoctorID"
            };
            
            // Заполняем список врачами
            foreach (DataRow row in dtDoctors.Rows)
            {
                cbDoctors.Items.Add(new 
                {
                    DoctorID = Convert.ToInt32(row["DoctorID"]),
                    FullName = row["FullName"].ToString()
                });
            }
            
            cbDoctors.SelectedIndex = 0;
            Grid.SetRow(cbDoctors, 1);
            
            // Панель с кнопками
            StackPanel buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            Button btnOk = new Button
            {
                Content = "OK",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            
            Button btnCancel = new Button
            {
                Content = "Отмена",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };
            
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += BtnCancel_Click;
            
            buttonsPanel.Children.Add(btnOk);
            buttonsPanel.Children.Add(btnCancel);
            Grid.SetRow(buttonsPanel, 2);
            
            // Добавляем элементы в грид
            grid.Children.Add(tbHeader);
            grid.Children.Add(cbDoctors);
            grid.Children.Add(buttonsPanel);
            
            // Устанавливаем грид как содержимое окна
            Content = grid;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (cbDoctors.SelectedItem != null)
            {
                dynamic selectedItem = cbDoctors.SelectedItem;
                SelectedDoctorID = selectedItem.DoctorID;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите врача.", "Предупреждение", 
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 