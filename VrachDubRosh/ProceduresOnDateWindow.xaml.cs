using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using OfficeOpenXml; // Для работы с Excel

namespace VrachDubRosh
{
    public partial class ProceduresOnDateWindow : Window
    {
        private readonly string connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";
        private string selectedDate;

        public ProceduresOnDateWindow(string selectedDate)
        {
            InitializeComponent();
            this.selectedDate = selectedDate;
            this.Title = $"Процедуры на {selectedDate}";
            LoadProceduresForSelectedDate();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private void LoadProceduresForSelectedDate()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT pa.AppointmentID, pr.ProcedureName, pa.AppointmentDateTime, pa.Status, 
                               p.FullName AS PatientName
                        FROM ProcedureAppointments pa
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        INNER JOIN Patients p ON pa.PatientID = p.PatientID
                        WHERE CONVERT(date, pa.AppointmentDateTime) = @SelectedDate
                        ORDER BY pa.AppointmentDateTime DESC";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    DataTable dt = new DataTable();
                    da.SelectCommand.Parameters.AddWithValue("@SelectedDate", selectedDate);
                    da.Fill(dt);

                    // Отображаем данные в DataGrid
                    dgProceduresOnDate.ItemsSource = dt.DefaultView;

                    // Если есть данные, показываем информацию о пациенте
                    if (dt.Rows.Count > 0)
                    {
                        string patientName = dt.Rows[0]["PatientName"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур на выбранную дату: " + ex.Message);
            }
        }

        // Метод для формирования отчета и сохранения его в Excel
        private void btnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Генерация Excel отчета
                using (ExcelPackage package = new ExcelPackage())
                {
                    // Создаем лист Excel
                    var worksheet = package.Workbook.Worksheets.Add("Процедуры");

                    // Заголовки столбцов
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Процедура";
                    worksheet.Cells[1, 3].Value = "Дата и время";
                    worksheet.Cells[1, 4].Value = "Статус";
                    worksheet.Cells[1, 5].Value = "Пациент";

                    // Заполнение данными из DataGrid
                    int row = 2;
                    foreach (DataRowView rowView in dgProceduresOnDate.ItemsSource)
                    {
                        worksheet.Cells[row, 1].Value = rowView["AppointmentID"];
                        worksheet.Cells[row, 2].Value = rowView["ProcedureName"];
                        worksheet.Cells[row, 3].Value = Convert.ToDateTime(rowView["AppointmentDateTime"]).ToString("dd.MM.yyyy HH:mm");
                        worksheet.Cells[row, 4].Value = rowView["Status"];
                        worksheet.Cells[row, 5].Value = rowView["PatientName"];
                        row++;
                    }

                    // Сохранение файла Excel
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel Files (*.xlsx)|*.xlsx",
                        FileName = $"Отчет_Процедуры_{selectedDate}.xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        System.IO.File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());
                        MessageBox.Show("Отчет успешно сохранен!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при формировании отчета: " + ex.Message);
            }
        }
    }
}