using System;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class AddPatientDescriptionWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private int _doctorID;
        // Если описание уже существует, сохраняем его идентификатор
        private int? _patientDescriptionID = null;

        public AddPatientDescriptionWindow(int patientID, int doctorID)
        {
            InitializeComponent();
            _patientID = patientID;
            _doctorID = doctorID;
            LoadDescription();
        }

        private void LoadDescription()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Предполагаем, что у врача может быть одно описание для данного пациента.
                    string query = @"SELECT TOP 1 PatientDescriptionID, Description 
                                     FROM PatientDescriptions 
                                     WHERE PatientID = @PatientID AND DoctorID = @DoctorID 
                                     ORDER BY DescriptionDate DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                _patientDescriptionID = Convert.ToInt32(reader["PatientDescriptionID"]);
                                txtDescription.Text = reader["Description"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки описания: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Пожалуйста, введите описание.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    if (_patientDescriptionID.HasValue)
                    {
                        // Обновляем существующее описание
                        string updateQuery = @"UPDATE PatientDescriptions 
                                               SET Description = @Description, DescriptionDate = GETDATE()
                                               WHERE PatientDescriptionID = @PatientDescriptionID";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@Description", txtDescription.Text.Trim());
                            cmd.Parameters.AddWithValue("@PatientDescriptionID", _patientDescriptionID.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Вставляем новое описание
                        string insertQuery = @"INSERT INTO PatientDescriptions (PatientID, DoctorID, Description, DescriptionDate)
                                               VALUES (@PatientID, @DoctorID, @Description, GETDATE())";
                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", _patientID);
                            cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                            cmd.Parameters.AddWithValue("@Description", txtDescription.Text.Trim());
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                MessageBox.Show("Описание успешно сохранено.");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении описания: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
