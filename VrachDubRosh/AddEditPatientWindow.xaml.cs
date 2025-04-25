using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace VrachDubRosh
{
    public partial class AddEditPatientWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private readonly int patientID;
        private readonly bool isEditMode;
        private readonly Window owner;

        // Конструктор для добавления нового пациента
        public AddEditPatientWindow(Window owner)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = false;
            patientID = -1;
            Title = "Добавление пациента";
            
            // Устанавливаем текущую дату
            dpRecordDate.SelectedDate = DateTime.Today;
            // По умолчанию выбираем мужской пол
            cbGender.SelectedIndex = 0;
        }

        // Конструктор для редактирования существующего пациента
        public AddEditPatientWindow(Window owner, int patientID)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = true;
            this.patientID = patientID;
            Title = "Редактирование пациента";
            
            // Загружаем данные пациента
            LoadPatientData();
        }

        private void LoadPatientData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT FullName, DateOfBirth, Gender, RecordDate, DischargeDate FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFullName.Text = reader["FullName"].ToString();
                                
                                if (reader["DateOfBirth"] != DBNull.Value)
                                    dpDateOfBirth.SelectedDate = Convert.ToDateTime(reader["DateOfBirth"]);
                                
                                string gender = reader["Gender"].ToString();
                                cbGender.SelectedIndex = gender.Equals("Мужской", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                                
                                if (reader["RecordDate"] != DBNull.Value)
                                    dpRecordDate.SelectedDate = Convert.ToDateTime(reader["RecordDate"]);
                                
                                if (reader["DischargeDate"] != DBNull.Value)
                                    dpDischargeDate.SelectedDate = Convert.ToDateTime(reader["DischargeDate"]);
                            }
                            else
                            {
                                MessageBox.Show("Пациент не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных пациента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения обязательных полей
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Пожалуйста, введите ФИО пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                txtFullName.Focus();
                return;
            }

            if (dpDateOfBirth.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату рождения пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDateOfBirth.Focus();
                return;
            }

            if (dpRecordDate.SelectedDate == null)
            {
                MessageBox.Show("Пожалуйста, выберите дату записи пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpRecordDate.Focus();
                return;
            }

            // Проверка валидности дат
            DateTime dateOfBirth = dpDateOfBirth.SelectedDate.Value;
            DateTime recordDate = dpRecordDate.SelectedDate.Value;
            DateTime? dischargeDate = dpDischargeDate.SelectedDate;

            if (dateOfBirth > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть в будущем.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDateOfBirth.Focus();
                return;
            }

            if (recordDate > DateTime.Today)
            {
                MessageBox.Show("Дата записи не может быть в будущем.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpRecordDate.Focus();
                return;
            }

            if (dischargeDate.HasValue && dischargeDate.Value < recordDate)
            {
                MessageBox.Show("Дата выписки не может быть раньше даты записи.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDischargeDate.Focus();
                return;
            }

            // Получение данных из формы
            string fullName = txtFullName.Text.Trim();
            string gender = ((ComboBoxItem)cbGender.SelectedItem).Content.ToString();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    if (isEditMode)
                    {
                        // Обновление существующего пациента
                        string updateQuery;
                        if (dischargeDate.HasValue)
                        {
                            updateQuery = @"
                                UPDATE Patients SET 
                                FullName = @FullName, 
                                DateOfBirth = @DateOfBirth, 
                                Gender = @Gender, 
                                RecordDate = @RecordDate, 
                                DischargeDate = @DischargeDate 
                                WHERE PatientID = @PatientID";
                        }
                        else
                        {
                            updateQuery = @"
                                UPDATE Patients SET 
                                FullName = @FullName, 
                                DateOfBirth = @DateOfBirth, 
                                Gender = @Gender, 
                                RecordDate = @RecordDate, 
                                DischargeDate = NULL 
                                WHERE PatientID = @PatientID";
                        }

                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@FullName", fullName);
                            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                            cmd.Parameters.AddWithValue("@Gender", gender);
                            cmd.Parameters.AddWithValue("@RecordDate", recordDate);
                            
                            if (dischargeDate.HasValue)
                                cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate.Value);
                            
                            cmd.Parameters.AddWithValue("@PatientID", patientID);
                            
                            cmd.ExecuteNonQuery();
                        }
                        
                        MessageBox.Show("Пациент успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Добавление нового пациента
                        string insertQuery;
                        if (dischargeDate.HasValue)
                        {
                            insertQuery = @"
                                INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, DischargeDate)
                                VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @DischargeDate);
                                SELECT SCOPE_IDENTITY();";
                        }
                        else
                        {
                            insertQuery = @"
                                INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate)
                                VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate);
                                SELECT SCOPE_IDENTITY();";
                        }

                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@FullName", fullName);
                            cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                            cmd.Parameters.AddWithValue("@Gender", gender);
                            cmd.Parameters.AddWithValue("@RecordDate", recordDate);
                            
                            if (dischargeDate.HasValue)
                                cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate.Value);
                            
                            int newPatientID = Convert.ToInt32(cmd.ExecuteScalar());
                            
                            MessageBox.Show("Пациент успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Автоматически открываем окно документов для нового пациента
                            PatientDocumentsWindow documentsWindow = new PatientDocumentsWindow(
                                newPatientID, 
                                txtFullName.Text.Trim(), 
                                dpDateOfBirth.SelectedDate);
                            documentsWindow.ShowDialog();
                            
                            // Устанавливаем ID добавленного пациента для возможности открытия окна документов
                            DialogResult = true;
                        }
                    }
                }
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении пациента: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
