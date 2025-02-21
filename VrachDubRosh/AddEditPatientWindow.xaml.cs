using System;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class AddEditPatientWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int? patientID = null; // Если null – добавление, иначе редактирование

        public AddEditPatientWindow()
        {
            InitializeComponent();
            this.Title = "Добавить пациента";
        }

        public AddEditPatientWindow(int patientID) : this()
        {
            this.patientID = patientID;
            this.Title = "Редактировать пациента";
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
                        cmd.Parameters.AddWithValue("@PatientID", patientID.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFullName.Text = reader["FullName"].ToString();
                                dpDateOfBirth.SelectedDate = Convert.ToDateTime(reader["DateOfBirth"]);
                                // Определяем пол (если это "Мужской" или "Женский", выбираем соответствующий пункт)
                                string genderValue = reader["Gender"].ToString();
                                if (genderValue == "Мужской")
                                {
                                    cbGender.SelectedIndex = 0;
                                }
                                else if (genderValue == "Женский")
                                {
                                    cbGender.SelectedIndex = 1;
                                }
                                else
                                {
                                    // Если вдруг в БД другое значение, можно оставить ComboBox невыбранным
                                    cbGender.SelectedIndex = -1;
                                }

                                dpRecordDate.SelectedDate = reader["RecordDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["RecordDate"]);
                                dpDischargeDate.SelectedDate = reader["DischargeDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["DischargeDate"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных пациента: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || dpDateOfBirth.SelectedDate == null || cbGender.SelectedIndex < 0)
            {
                MessageBox.Show("Пожалуйста, заполните поля ФИО, Дата рождения и выберите Пол.");
                return;
            }

            // Получаем выбранный пол
            string gender = ((System.Windows.Controls.ComboBoxItem)cbGender.SelectedItem).Content.ToString();

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    if (patientID == null)
                    {
                        // Добавление нового пациента
                        string insertQuery = @"
                            INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, DischargeDate)
                            VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @DischargeDate)";
                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("@DateOfBirth", dpDateOfBirth.SelectedDate.Value);
                            cmd.Parameters.AddWithValue("@Gender", gender);
                            cmd.Parameters.AddWithValue("@RecordDate", dpRecordDate.SelectedDate.HasValue ? (object)dpRecordDate.SelectedDate.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@DischargeDate", dpDischargeDate.SelectedDate.HasValue ? (object)dpDischargeDate.SelectedDate.Value : DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Редактирование пациента
                        string updateQuery = @"
                            UPDATE Patients 
                            SET FullName = @FullName, 
                                DateOfBirth = @DateOfBirth, 
                                Gender = @Gender, 
                                RecordDate = @RecordDate, 
                                DischargeDate = @DischargeDate
                            WHERE PatientID = @PatientID";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("@DateOfBirth", dpDateOfBirth.SelectedDate.Value);
                            cmd.Parameters.AddWithValue("@Gender", gender);
                            cmd.Parameters.AddWithValue("@RecordDate", dpRecordDate.SelectedDate.HasValue ? (object)dpRecordDate.SelectedDate.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@DischargeDate", dpDischargeDate.SelectedDate.HasValue ? (object)dpDischargeDate.SelectedDate.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@PatientID", patientID.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения данных пациента: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
