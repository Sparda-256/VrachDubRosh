using System;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class AddEditProcedureWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _doctorID;
        private int? _procedureID = null;

        // Конструктор для добавления
        public AddEditProcedureWindow(int doctorID)
        {
            InitializeComponent();
            _doctorID = doctorID;
            this.Title = "Добавить процедуру";
        }

        // Конструктор для редактирования
        public AddEditProcedureWindow(int doctorID, int procedureID) : this(doctorID)
        {
            _procedureID = procedureID;
            this.Title = "Редактировать процедуру";
            LoadProcedureData();
        }

        private void LoadProcedureData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ProcedureName, Duration FROM Procedures WHERE ProcedureID = @ProcedureID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", _procedureID.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtProcedureName.Text = reader["ProcedureName"].ToString();
                                txtDuration.Text = reader["Duration"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных процедуры: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProcedureName.Text) || string.IsNullOrWhiteSpace(txtDuration.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (!int.TryParse(txtDuration.Text.Trim(), out int duration))
            {
                MessageBox.Show("Длительность должна быть числом (в минутах).");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    if (_procedureID == null)
                    {
                        // Добавление процедуры
                        string insertQuery = @"INSERT INTO Procedures (ProcedureName, Duration, DoctorID)
                                               VALUES (@ProcedureName, @Duration, @DoctorID)";
                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@ProcedureName", txtProcedureName.Text.Trim());
                            cmd.Parameters.AddWithValue("@Duration", duration);
                            cmd.Parameters.AddWithValue("@DoctorID", _doctorID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Редактирование процедуры
                        string updateQuery = @"UPDATE Procedures 
                                               SET ProcedureName = @ProcedureName, Duration = @Duration
                                               WHERE ProcedureID = @ProcedureID";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@ProcedureName", txtProcedureName.Text.Trim());
                            cmd.Parameters.AddWithValue("@Duration", duration);
                            cmd.Parameters.AddWithValue("@ProcedureID", _procedureID.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения данных процедуры: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
