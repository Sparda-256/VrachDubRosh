using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class MedCardWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _patientID;
        private string _patientName;

        public MedCardWindow(int patientID, string patientName)
        {
            InitializeComponent();
            _patientID = patientID;
            _patientName = patientName;
            tbPatientName.Text = $"Медицинская карточка пациента: {patientName}";
            LoadMedicalNotes();
            LoadProcedures();
        }

        /// <summary>
        /// Загружает последние медицинские заметки для данного пациента (если имеются).
        /// </summary>
        private void LoadMedicalNotes()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"SELECT TOP 1 Description 
                                     FROM PatientDescriptions 
                                     WHERE PatientID = @PatientID
                                     ORDER BY DescriptionDate DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", _patientID);
                        object result = cmd.ExecuteScalar();
                        txtMedicalNotes.Text = (result != null && result != DBNull.Value)
                                               ? result.ToString()
                                               : "";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки медицинских заметок: " + ex.Message);
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
                    string query = @"SELECT pa.AppointmentID, pr.ProcedureName, pa.AppointmentDateTime, pa.Status
                                     FROM ProcedureAppointments pa
                                     INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
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
        /// Сохраняет изменения в медицинских заметках.
        /// Если ранее заметки были записаны, обновляет их; иначе, вставляет новую запись.
        /// </summary>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Проверяем наличие существующей записи для данного пациента
                    string checkQuery = @"SELECT TOP 1 PatientDescriptionID FROM PatientDescriptions 
                                          WHERE PatientID = @PatientID 
                                          ORDER BY DescriptionDate DESC";
                    int? descriptionID = null;
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@PatientID", _patientID);
                        object result = checkCmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            descriptionID = Convert.ToInt32(result);
                        }
                    }
                    if (descriptionID.HasValue)
                    {
                        // Обновляем существующую запись
                        string updateQuery = @"UPDATE PatientDescriptions 
                                               SET Description = @Description, DescriptionDate = GETDATE()
                                               WHERE PatientDescriptionID = @ID";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, con))
                        {
                            updateCmd.Parameters.AddWithValue("@Description", txtMedicalNotes.Text.Trim());
                            updateCmd.Parameters.AddWithValue("@ID", descriptionID.Value);
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Вставляем новую запись. При необходимости можно передать идентификатор врача.
                        string insertQuery = @"INSERT INTO PatientDescriptions (PatientID, DoctorID, Description, DescriptionDate)
                                               VALUES (@PatientID, NULL, @Description, GETDATE())";
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, con))
                        {
                            insertCmd.Parameters.AddWithValue("@PatientID", _patientID);
                            insertCmd.Parameters.AddWithValue("@Description", txtMedicalNotes.Text.Trim());
                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
                MessageBox.Show("Медицинская карточка успешно сохранена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}