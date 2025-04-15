using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class DoctorProceduresWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private int doctorID;
        private string doctorName;
        public bool isDarkTheme { get; private set; } = false;

        public DoctorProceduresWindow(int doctorID, string doctorName)
        {
            InitializeComponent();
            this.doctorID = doctorID;
            this.doctorName = doctorName;
            this.Title = $"Процедуры врача: {doctorName}";
            this.Loaded += DoctorProceduresWindow_Loaded;
            LoadDoctorProcedures();
        }

        private void DoctorProceduresWindow_Loaded(object sender, RoutedEventArgs e)
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

        private void LoadDoctorProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT ProcedureID, ProcedureName, Duration FROM Procedures WHERE DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", doctorID);
                    da.Fill(dt);
                    dgDoctorProcedures.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур врача: " + ex.Message);
            }
        }
    }
}
