using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class PatientProceduresWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int patientID;
        private string patientName;

        public PatientProceduresWindow(int patientID, string patientName)
        {
            InitializeComponent();
            this.patientID = patientID;
            this.patientName = patientName;
            this.Title = $"Процедуры пациента: {patientName}";
            LoadPatientProcedures();
        }

        private void LoadPatientProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT pa.AppointmentID, pr.ProcedureName, pa.AppointmentDateTime, pa.Status
                        FROM ProcedureAppointments pa
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        WHERE pa.PatientID = @PatientID
                        ORDER BY pa.AppointmentDateTime DESC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", patientID);
                    da.Fill(dt);
                    dgPatientProcedures.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур пациента: " + ex.Message);
            }
        }
    }
}
