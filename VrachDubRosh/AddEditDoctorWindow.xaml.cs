using System;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class AddEditDoctorWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int? doctorID = null;

        public AddEditDoctorWindow()
        {
            InitializeComponent();
            this.Title = "Добавить врача";
            this.Loaded += AddEditDoctorWindow_Loaded;
        }

        public AddEditDoctorWindow(int doctorID) : this()
        {
            this.doctorID = doctorID;
            this.Title = "Редактировать врача";
            LoadDoctorData();
        }

        private void AddEditDoctorWindow_Loaded(object sender, RoutedEventArgs e)
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
            ResourceDictionary resourceDict = new ResourceDictionary();
            resourceDict.Source = new Uri("/Themes/DarkTheme.xaml", UriKind.Relative);
            Application.Current.Resources.MergedDictionaries[0] = resourceDict;
        }

        private void LoadDoctorData()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT FullName, Specialty, OfficeNumber, Password, WorkExperience FROM Doctors WHERE DoctorID = @DoctorID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", doctorID.Value);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtFullName.Text = reader["FullName"].ToString();
                                txtSpecialty.Text = reader["Specialty"].ToString();
                                txtOfficeNumber.Text = reader["OfficeNumber"].ToString();
                                txtPassword.Password = reader["Password"].ToString();
                                txtWorkExperience.Text = reader["WorkExperience"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных врача: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) ||
                string.IsNullOrWhiteSpace(txtSpecialty.Text) ||
                string.IsNullOrWhiteSpace(txtOfficeNumber.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Password) ||
                string.IsNullOrWhiteSpace(txtWorkExperience.Text))
            {
                MessageBox.Show("Пожалуйста, заполните все поля.");
                return;
            }

            if (!int.TryParse(txtWorkExperience.Text.Trim(), out int workExp))
            {
                MessageBox.Show("Стаж должен быть числом.");
                return;
            }

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    if (doctorID == null)
                    {
                        // Добавление врача
                        string insertQuery = @"INSERT INTO Doctors (FullName, Specialty, OfficeNumber, Password, WorkExperience)
                                               VALUES (@FullName, @Specialty, @OfficeNumber, @Password, @WorkExperience)";
                        using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("@Specialty", txtSpecialty.Text.Trim());
                            cmd.Parameters.AddWithValue("@OfficeNumber", txtOfficeNumber.Text.Trim());
                            cmd.Parameters.AddWithValue("@Password", txtPassword.Password.Trim());
                            cmd.Parameters.AddWithValue("@WorkExperience", workExp);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Редактирование врача
                        string updateQuery = @"UPDATE Doctors 
                                               SET FullName = @FullName, Specialty = @Specialty, OfficeNumber = @OfficeNumber, 
                                                   Password = @Password, WorkExperience = @WorkExperience
                                               WHERE DoctorID = @DoctorID";
                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("@Specialty", txtSpecialty.Text.Trim());
                            cmd.Parameters.AddWithValue("@OfficeNumber", txtOfficeNumber.Text.Trim());
                            cmd.Parameters.AddWithValue("@Password", txtPassword.Password.Trim());
                            cmd.Parameters.AddWithValue("@WorkExperience", workExp);
                            cmd.Parameters.AddWithValue("@DoctorID", doctorID.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения данных врача: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
