using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace VrachDubRosh
{
    // Класс для хранения информации о пациенте
    public class PatientInfo
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }

        public override string ToString()
        {
            return FullName;
        }
    }

    public partial class AddEditAccompanyingWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private readonly int accompaningPersonID;
        private readonly bool isEditMode;
        private readonly Window owner;

        // Конструктор для добавления нового сопровождающего
        public AddEditAccompanyingWindow(Window owner)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = false;
            accompaningPersonID = -1;
            Title = "Добавление сопровождающего";
            
            // По умолчанию выбираем отношение "Родитель"
            cbRelationship.SelectedIndex = 0;
            
            // Загружаем список пациентов
            LoadPatients();
        }

        // Конструктор для редактирования существующего сопровождающего
        public AddEditAccompanyingWindow(Window owner, int accompaningPersonID)
        {
            InitializeComponent();
            this.owner = owner;
            this.Owner = owner;
            isEditMode = true;
            this.accompaningPersonID = accompaningPersonID;
            Title = "Редактирование сопровождающего";
            
            // Загружаем список пациентов
            LoadPatients();
            
            // Загружаем данные сопровождающего
            LoadAccompanyingPersonData();
        }
        
        private void LoadPatients()
        {
            try
            {
                List<PatientInfo> patients = new List<PatientInfo>();
                
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName FROM Patients ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                patients.Add(new PatientInfo
                                {
                                    PatientID = Convert.ToInt32(reader["PatientID"]),
                                    FullName = reader["FullName"].ToString()
                                });
                            }
                        }
                    }
                }
                
                // Настраиваем комбобокс
                cbPatients.ItemsSource = patients;
                cbPatients.DisplayMemberPath = "FullName";
                cbPatients.SelectedValuePath = "PatientID";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке списка пациентов: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadAccompanyingPersonData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName, DateOfBirth, Relationship, HasPowerOfAttorney FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompaningPersonID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int patientID = Convert.ToInt32(reader["PatientID"]);
                                cbPatients.SelectedValue = patientID;
                                txtFullName.Text = reader["FullName"].ToString();
                                
                                if (reader["DateOfBirth"] != DBNull.Value)
                                    dpDateOfBirth.SelectedDate = Convert.ToDateTime(reader["DateOfBirth"]);
                                
                                string relationship = reader["Relationship"].ToString();
                                SelectRelationship(relationship);
                                
                                chkHasPowerOfAttorney.IsChecked = Convert.ToBoolean(reader["HasPowerOfAttorney"]);
                            }
                            else
                            {
                                MessageBox.Show("Сопровождающий не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                this.Close();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных сопровождающего: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        
        private void SelectRelationship(string relationship)
        {
            for (int i = 0; i < cbRelationship.Items.Count; i++)
            {
                ComboBoxItem item = (ComboBoxItem)cbRelationship.Items[i];
                if (item.Content.ToString() == relationship)
                {
                    cbRelationship.SelectedIndex = i;
                    return;
                }
            }
            
            // Если не нашли точное совпадение, выбираем "Иное лицо"
            cbRelationship.SelectedIndex = cbRelationship.Items.Count - 1;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения обязательных полей
            if (cbPatients.SelectedValue == null)
            {
                MessageBox.Show("Пожалуйста, выберите пациента.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                cbPatients.Focus();
                return;
            }
            
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Пожалуйста, введите ФИО сопровождающего.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                txtFullName.Focus();
                return;
            }
            
            // Проверка валидности даты
            if (dpDateOfBirth.SelectedDate.HasValue && dpDateOfBirth.SelectedDate.Value > DateTime.Today)
            {
                MessageBox.Show("Дата рождения не может быть в будущем.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                dpDateOfBirth.Focus();
                return;
            }

            // Получение данных из формы
            int patientID = Convert.ToInt32(cbPatients.SelectedValue);
            string fullName = txtFullName.Text.Trim();
            DateTime? dateOfBirth = dpDateOfBirth.SelectedDate;
            string relationship = ((ComboBoxItem)cbRelationship.SelectedItem).Content.ToString();
            bool hasPowerOfAttorney = chkHasPowerOfAttorney.IsChecked ?? false;

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    
                    if (isEditMode)
                    {
                        // Обновление существующего сопровождающего
                        string updateQuery = @"
                            UPDATE AccompanyingPersons SET 
                            PatientID = @PatientID, 
                            FullName = @FullName, 
                            DateOfBirth = @DateOfBirth, 
                            Relationship = @Relationship, 
                            HasPowerOfAttorney = @HasPowerOfAttorney 
                            WHERE AccompanyingPersonID = @AccompanyingPersonID";
                        
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", patientID);
                            cmd.Parameters.AddWithValue("@FullName", fullName);
                            
                            if (dateOfBirth.HasValue)
                                cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.Value);
                            else
                                cmd.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);
                            
                            cmd.Parameters.AddWithValue("@Relationship", relationship);
                            cmd.Parameters.AddWithValue("@HasPowerOfAttorney", hasPowerOfAttorney);
                            cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompaningPersonID);
                            
                            cmd.ExecuteNonQuery();
                        }
                        
                        MessageBox.Show("Сопровождающий успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        // Добавление нового сопровождающего
                        string insertQuery = @"
                            INSERT INTO AccompanyingPersons (PatientID, FullName, DateOfBirth, Relationship, HasPowerOfAttorney)
                            VALUES (@PatientID, @FullName, @DateOfBirth, @Relationship, @HasPowerOfAttorney);
                            SELECT SCOPE_IDENTITY();";
                        
                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", patientID);
                            cmd.Parameters.AddWithValue("@FullName", fullName);
                            
                            if (dateOfBirth.HasValue)
                                cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth.Value);
                            else
                                cmd.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);
                            
                            cmd.Parameters.AddWithValue("@Relationship", relationship);
                            cmd.Parameters.AddWithValue("@HasPowerOfAttorney", hasPowerOfAttorney);
                            
                            int newAccompanyingID = Convert.ToInt32(cmd.ExecuteScalar());
                            
                            MessageBox.Show("Сопровождающий успешно добавлен.\n\nТеперь вы можете добавить документы сопровождающего в разделе 'Документы'.", 
                                           "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении сопровождающего: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 