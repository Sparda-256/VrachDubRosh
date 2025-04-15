using System;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class AddProcedureDescriptionWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int _appointmentID;
        private int _doctorID;
        public bool isDarkTheme { get; private set; } = false;

        public AddProcedureDescriptionWindow(int appointmentID, int doctorID)
        {
            InitializeComponent();
            _appointmentID = appointmentID;
            _doctorID = doctorID;
            LoadDescription();
            this.Loaded += AddProcedureDescriptionWindow_Loaded;
        }

        private void AddProcedureDescriptionWindow_Loaded(object sender, RoutedEventArgs e)
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

        private void ApplyDarkTheme()
        {
            // Применяем темную тему
            isDarkTheme = true;
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        private void LoadDescription()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT Description FROM ProcedureAppointments WHERE AppointmentID = @AppointmentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AppointmentID", _appointmentID);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            txtDescription.Text = result.ToString();
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
                    // Обновляем поле Description в таблице ProcedureAppointments
                    string updateQuery = @"UPDATE ProcedureAppointments 
                                           SET Description = @Description 
                                           WHERE AppointmentID = @AppointmentID";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Description", txtDescription.Text.Trim());
                        cmd.Parameters.AddWithValue("@AppointmentID", _appointmentID);
                        cmd.ExecuteNonQuery();
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