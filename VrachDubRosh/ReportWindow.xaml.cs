using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace VrachDubRosh
{
    public partial class ReportWindow : Window
    {
        // Строка подключения (при необходимости замените на актуальную)
        private readonly string connectionString = "Data Source=.;Initial Catalog=PomoshnikPolicliniki2;Integrated Security=True";

        public ReportWindow()
        {
            InitializeComponent();
            LoadProceduresReport();
            LoadDoctorsReport();
        }

        // Загрузка отчета по процедурам: группировка по дате, подсчет количества и суммарной длительности
        private void LoadProceduresReport()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT CONVERT(date, pa.AppointmentDateTime) AS [Дата],
                               COUNT(*) AS [Количество_процедур],
                               SUM(pr.Duration) AS [Общая_длительность]
                        FROM ProcedureAppointments pa
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        GROUP BY CONVERT(date, pa.AppointmentDateTime)
                        ORDER BY [Дата]";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgProceduresReport.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки отчета по процедурам: " + ex.Message);
            }
        }

        // Загрузка отчета по врачам: для каждого врача количество назначенных процедур и суммарная длительность
        private void LoadDoctorsReport()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT d.DoctorID,
                               d.FullName AS [Врач],
                               COUNT(*) AS [Количество_процедур],
                               SUM(pr.Duration) AS [Общая_длительность]
                        FROM ProcedureAppointments pa
                        INNER JOIN Doctors d ON pa.DoctorID = d.DoctorID
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        GROUP BY d.DoctorID, d.FullName
                        ORDER BY [Врач]";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgDoctorsReport.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки отчета по врачам: " + ex.Message);
            }
        }

        // Обработчик кнопки "Обновить" для отчета по процедурам
        private void btnRefreshProcedures_Click(object sender, RoutedEventArgs e)
        {
            LoadProceduresReport();
        }

        // Обработчик кнопки "Обновить" для отчета по врачам
        private void btnRefreshDoctors_Click(object sender, RoutedEventArgs e)
        {
            LoadDoctorsReport();
        }
    }
}