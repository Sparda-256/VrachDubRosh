using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using ClosedXML.Excel;

namespace WebDubRosh.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly string _connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True";
        
        // Статический конструктор для установки лицензии EPPlus
        static ReportController()
        {
            // Пытаемся установить лицензию на EPPlus
            try 
            {
                // Проверяем, существует ли класс License в EPPlus 8+
                var epPlusAssembly = Assembly.GetAssembly(typeof(ExcelPackage));
                var licenseType = epPlusAssembly?.GetType("OfficeOpenXml.License");
                
                if (licenseType != null)
                {
                    // EPPlus 8+
                    var licenseContextProperty = licenseType.GetProperty("LicenseContext", 
                        BindingFlags.Public | BindingFlags.Static);
                    
                    if (licenseContextProperty != null)
                    {
                        var licenseContextType = typeof(LicenseContext);
                        var nonCommercialValue = Enum.Parse(licenseContextType, "NonCommercial");
                        licenseContextProperty.SetValue(null, nonCommercialValue);
                        Console.WriteLine("EPPlus 8+ license set successfully");
                    }
                }
                else
                {
                    // EPPlus 5-7
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    Console.WriteLine("EPPlus 5-7 license set successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting EPPlus license: {ex.Message}");
            }
        }

        // GET: api/report/doctors
        [HttpGet("doctors")]
        public IActionResult GetDoctors()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT DoctorID, FullName FROM Doctors ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/report/patients
        [HttpGet("patients")]
        public IActionResult GetPatients()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName FROM Patients ORDER BY FullName";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/report/procedures
        [HttpGet("procedures")]
        public IActionResult GetProcedures()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            p.ProcedureID, 
                            p.ProcedureName + ' - ' + d.FullName AS ProcedureDisplay
                        FROM Procedures p
                        INNER JOIN Doctors d ON p.DoctorID = d.DoctorID
                        ORDER BY p.ProcedureName";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/report/all-procedures
        [HttpGet("all-procedures")]
        public IActionResult GetAllProcedures([FromQuery] string startDate, [FromQuery] string endDate)
        {
            try
            {
                // Проверка дат
                if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                {
                    return BadRequest(new { message = "Необходимо указать даты начала и конца периода" });
                }

                DateTime start = DateTime.Parse(startDate);
                DateTime end = DateTime.Parse(endDate);
                
                // Захватываем весь последний день
                end = end.AddDays(1).AddSeconds(-1);

                // Обновляем статусы назначений перед формированием отчета
                UpdateAppointmentsStatus();

                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            d.FullName AS [Врач],
                            pr.ProcedureName AS [Процедура],
                            p.FullName AS [Пациент],
                            pa.AppointmentDateTime AS [Дата/время],
                            pa.Status AS [Статус],
                            pr.Duration AS [Длительность (мин)]
                        FROM ProcedureAppointments pa
                        INNER JOIN Doctors d ON pa.DoctorID = d.DoctorID
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        INNER JOIN Patients p ON pa.PatientID = p.PatientID
                        WHERE pa.AppointmentDateTime BETWEEN @StartDate AND @EndDate
                        ORDER BY pa.AppointmentDateTime";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        cmd.Parameters.AddWithValue("@EndDate", end);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/report/doctor-procedures
        [HttpGet("doctor-procedures")]
        public IActionResult GetDoctorProcedures([FromQuery] int doctorId, [FromQuery] string startDate, [FromQuery] string endDate)
        {
            try
            {
                // Проверка параметров
                if (doctorId <= 0)
                {
                    return BadRequest(new { message = "Необходимо указать ID врача" });
                }

                if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                {
                    return BadRequest(new { message = "Необходимо указать даты начала и конца периода" });
                }

                DateTime start = DateTime.Parse(startDate);
                DateTime end = DateTime.Parse(endDate);
                
                // Захватываем весь последний день
                end = end.AddDays(1).AddSeconds(-1);

                // Обновляем статусы назначений перед формированием отчета
                UpdateAppointmentsStatus();

                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            pr.ProcedureName AS [Процедура],
                            p.FullName AS [Пациент],
                            pa.AppointmentDateTime AS [Дата/время],
                            pa.Status AS [Статус],
                            pr.Duration AS [Длительность (мин)]
                        FROM ProcedureAppointments pa
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        INNER JOIN Patients p ON pa.PatientID = p.PatientID
                        WHERE pa.DoctorID = @DoctorID
                          AND pa.AppointmentDateTime BETWEEN @StartDate AND @EndDate
                        ORDER BY pa.AppointmentDateTime";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", doctorId);
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        cmd.Parameters.AddWithValue("@EndDate", end);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/report/patient-procedures
        [HttpGet("patient-procedures")]
        public IActionResult GetPatientProcedures([FromQuery] int patientId, [FromQuery] string startDate, [FromQuery] string endDate)
        {
            try
            {
                // Проверка параметров
                if (patientId <= 0)
                {
                    return BadRequest(new { message = "Необходимо указать ID пациента" });
                }

                if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                {
                    return BadRequest(new { message = "Необходимо указать даты начала и конца периода" });
                }

                DateTime start = DateTime.Parse(startDate);
                DateTime end = DateTime.Parse(endDate);
                
                // Захватываем весь последний день
                end = end.AddDays(1).AddSeconds(-1);

                // Обновляем статусы назначений перед формированием отчета
                UpdateAppointmentsStatus();

                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            d.FullName AS [Врач],
                            pr.ProcedureName AS [Процедура],
                            pa.AppointmentDateTime AS [Дата/время],
                            pa.Status AS [Статус],
                            pr.Duration AS [Длительность (мин)]
                        FROM ProcedureAppointments pa
                        INNER JOIN Doctors d ON pa.DoctorID = d.DoctorID
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        WHERE pa.PatientID = @PatientID
                          AND pa.AppointmentDateTime BETWEEN @StartDate AND @EndDate
                        ORDER BY pa.AppointmentDateTime";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientId);
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        cmd.Parameters.AddWithValue("@EndDate", end);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/report/procedure-appointments
        [HttpGet("procedure-appointments")]
        public IActionResult GetProcedureAppointments([FromQuery] int procedureId, [FromQuery] string startDate, [FromQuery] string endDate)
        {
            try
            {
                // Проверка параметров
                if (procedureId <= 0)
                {
                    return BadRequest(new { message = "Необходимо указать ID процедуры" });
                }

                if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                {
                    return BadRequest(new { message = "Необходимо указать даты начала и конца периода" });
                }

                DateTime start = DateTime.Parse(startDate);
                DateTime end = DateTime.Parse(endDate);
                
                // Захватываем весь последний день
                end = end.AddDays(1).AddSeconds(-1);

                // Обновляем статусы назначений перед формированием отчета
                UpdateAppointmentsStatus();

                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT
                            p.FullName AS [Пациент],
                            pa.AppointmentDateTime AS [Дата/время],
                            pa.Status AS [Статус],
                            pr.Duration AS [Длительность (мин)]
                        FROM ProcedureAppointments pa
                        INNER JOIN Patients p ON pa.PatientID = p.PatientID
                        INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                        WHERE pa.ProcedureID = @ProcedureID
                          AND pa.AppointmentDateTime BETWEEN @StartDate AND @EndDate
                        ORDER BY pa.AppointmentDateTime";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureId);
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        cmd.Parameters.AddWithValue("@EndDate", end);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
                
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/report/export
        [HttpPost("export")]
        public IActionResult ExportToExcel([FromBody] ReportExportModel model)
        {
            try
            {
                if (model == null || model.Data == null || model.Data.Count == 0)
                {
                    return BadRequest(new { message = "Нет данных для экспорта" });
                }

                try
                {
                    // Дополнительный резервный способ установки лицензии прямо перед созданием пакета
                    try
                    {
                        // Пытаемся напрямую установить свойство License.LicenseContext через reflection
                        var licenseType = Type.GetType("OfficeOpenXml.License, EPPlus");
                        if (licenseType != null)
                        {
                            var licenseContextProperty = licenseType.GetProperty("LicenseContext", 
                                BindingFlags.Public | BindingFlags.Static);
                            
                            if (licenseContextProperty != null)
                            {
                                licenseContextProperty.SetValue(null, LicenseContext.NonCommercial);
                                Console.WriteLine("ExportToExcel: Установлена лицензия EPPlus 8");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ExportToExcel: Не удалось установить лицензию через класс License: {ex.Message}");
                    }

                    // На всякий случай попытка использовать старый способ тоже
                    try
                    {
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    }
                    catch
                    {
                        // Игнорируем ошибки здесь
                    }

                    // Пытаемся экспортировать с помощью EPPlus
                    using (ExcelPackage package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add(model.ReportName);

                        // Добавляем заголовок отчёта
                        if (!string.IsNullOrEmpty(model.ReportHeader))
                        {
                            var headerRange = worksheet.Cells[1, 1, 1, model.Data[0].Count];
                            headerRange.Merge = true;
                            headerRange.Value = model.ReportHeader;
                            headerRange.Style.Font.Bold = true;
                            headerRange.Style.Font.Size = 14;
                            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        // Заголовки столбцов (строка 3)
                        int colIndex = 1;
                        foreach (var key in model.Data[0].Keys)
                        {
                            worksheet.Cells[3, colIndex].Value = key;
                            worksheet.Cells[3, colIndex].Style.Font.Bold = true;
                            colIndex++;
                        }

                        // Данные (начиная со строки 4)
                        for (int row = 0; row < model.Data.Count; row++)
                        {
                            colIndex = 1;
                            foreach (var key in model.Data[0].Keys)
                            {
                                var value = model.Data[row][key];
                                
                                if (key == "Дата/время" && value != null)
                                {
                                    if (DateTime.TryParse(value.ToString(), out DateTime dateTime))
                                    {
                                        var cell = worksheet.Cells[row + 4, colIndex];
                                        cell.Value = dateTime;
                                        cell.Style.Numberformat.Format = "dd.MM.yyyy HH:mm";
                                    }
                                    else
                                    {
                                        worksheet.Cells[row + 4, colIndex].Value = value;
                                    }
                                }
                                else
                                {
                                    worksheet.Cells[row + 4, colIndex].Value = value;
                                }
                                
                                colIndex++;
                            }
                        }

                        // Автоширина столбцов
                        worksheet.Cells[1, 1, model.Data.Count + 4, model.Data[0].Count].AutoFitColumns();

                        // Итоговые показатели
                        if (!string.IsNullOrEmpty(model.SummaryText))
                        {
                            int summaryStartRow = model.Data.Count + 6;
                            var lines = model.SummaryText.Split('\n');
                            for (int i = 0; i < lines.Length; i++)
                            {
                                worksheet.Cells[summaryStartRow + i, 1].Value = lines[i];
                            }
                        }

                        // Получаем данные Excel файла
                        byte[] fileBytes = package.GetAsByteArray();
                        
                        // Возвращаем файл клиенту
                        string fileName = $"{model.ReportName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(
                            fileBytes, 
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                            fileName
                        );
                    }
                }
                catch (Exception epPlusEx)
                {
                    // Если EPPlus не сработал, пробуем ClosedXML
                    Console.WriteLine($"EPPlus экспорт не удался: {epPlusEx.Message}. Пробуем ClosedXML.");
                    return ExportToExcelWithClosedXML(model);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка экспорта: {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                return StatusCode(500, new { message = "Ошибка при экспорте отчета: " + ex.Message });
            }
        }

        // Запасной метод экспорта с использованием ClosedXML вместо EPPlus
        private IActionResult ExportToExcelWithClosedXML(ReportExportModel model)
        {
            try
            {
                // Решение проблемы с доступом к шрифтам для ClosedXML
                // Устанавливаем временный каталог в пользовательский профиль вместо системных шрифтов
                string tempPath = Path.Combine(Path.GetTempPath(), "ClosedXML_Temp");
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }
                
                // Устанавливаем переменную окружения для XLWorkbook
                Environment.SetEnvironmentVariable("MONO_IOMAP", "all");
                Environment.SetEnvironmentVariable("TEMP", tempPath);
                Environment.SetEnvironmentVariable("TMP", tempPath);

                // Создаем параметры для XLWorkbook
                var saveOptions = new ClosedXML.Excel.SaveOptions { 
                    EvaluateFormulasBeforeSaving = false,
                    GenerateCalculationChain = false,
                    ValidatePackage = false
                };

                // Создаем временный файл для сохранения
                string fileName = $"{model.ReportName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string tempFile = Path.Combine(tempPath, fileName);

                // Добавляем необходимые using
                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(model.ReportName);

                    // Заголовок отчета
                    if (!string.IsNullOrEmpty(model.ReportHeader))
                    {
                        worksheet.Cell(1, 1).Value = model.ReportHeader;
                        worksheet.Range(1, 1, 1, model.Data[0].Count).Merge();
                        worksheet.Cell(1, 1).Style
                            .Font.SetBold(true)
                            .Font.SetFontSize(14)
                            .Alignment.SetHorizontal(ClosedXML.Excel.XLAlignmentHorizontalValues.Center);
                    }

                    // Заголовки столбцов
                    int columnIndex = 1;
                    foreach (var key in model.Data[0].Keys)
                    {
                        worksheet.Cell(3, columnIndex).Value = key?.ToString() ?? string.Empty;
                        worksheet.Cell(3, columnIndex).Style.Font.SetBold(true);
                        columnIndex++;
                    }

                    // Данные
                    for (int row = 0; row < model.Data.Count; row++)
                    {
                        columnIndex = 1;
                        foreach (var key in model.Data[0].Keys)
                        {
                            var value = model.Data[row][key];
                            var cell = worksheet.Cell(row + 4, columnIndex);

                            if (value == null)
                            {
                                cell.SetValue(string.Empty);
                            }
                            else if (key == "Дата/время" && value != null)
                            {
                                if (DateTime.TryParse(value.ToString(), out DateTime dateTime))
                                {
                                    cell.SetValue(dateTime);
                                    cell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                                }
                                else
                                {
                                    cell.SetValue(value.ToString());
                                }
                            }
                            else if (value is int || value is long || value is double || value is decimal)
                            {
                                cell.SetValue(Convert.ToDouble(value));
                            }
                            else if (value is bool)
                            {
                                cell.SetValue((bool)value);
                            }
                            else if (value is DateTime dt)
                            {
                                cell.SetValue(dt);
                                cell.Style.DateFormat.Format = "dd.MM.yyyy";
                            }
                            else
                            {
                                cell.SetValue(value.ToString());
                            }

                            columnIndex++;
                        }
                    }

                    // Устанавливаем фиксированную ширину столбцов
                    foreach (var column in worksheet.Columns())
                    {
                        column.Width = 15; // Фиксированная ширина для всех столбцов
                    }

                    // Итоги
                    if (!string.IsNullOrEmpty(model.SummaryText))
                    {
                        int summaryStartRow = model.Data.Count + 6;
                        var lines = model.SummaryText.Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            worksheet.Cell(summaryStartRow + i, 1).SetValue(lines[i]);
                        }
                    }

                    try
                    {
                        // Сохраняем в файл с опциями
                        workbook.SaveAs(tempFile, saveOptions);
                        
                        // Создаем и возвращаем FileStream
                        var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        
                        // Возвращаем файловый поток
                        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        
                        // Создание экземпляра ActionResult вручную
                        return new Microsoft.AspNetCore.Mvc.FileStreamResult(fileStream, contentType)
                        {
                            FileDownloadName = fileName
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при сохранении файла: {ex.Message}");
                        
                        // В случае ошибки просто возвращаем ошибку
                        return StatusCode(500, new { message = $"Ошибка при экспорте отчета: {ex.Message}" });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ClosedXML экспорт не удался: {ex.Message}");
                return StatusCode(500, new { message = $"Ошибка при экспорте отчета: {ex.Message}" });
            }
        }

        // Вспомогательный метод для обновления статусов назначений
        private void UpdateAppointmentsStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    // Сначала помечаем завершенные процедуры (независимо от текущего статуса)
                    string updateCompletedQuery = @"
                    UPDATE pa
                    SET pa.Status = 'Завершена'
                    FROM ProcedureAppointments pa
                    INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                    WHERE DATEADD(minute, pr.Duration, pa.AppointmentDateTime) <= GETDATE() 
                      AND pa.Status IN ('Назначена', 'Идёт')";

                    using (SqlCommand cmd = new SqlCommand(updateCompletedQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Затем помечаем процедуры, которые идут сейчас
                    string updateInProgressQuery = @"
                    UPDATE pa
                    SET pa.Status = 'Идёт'
                    FROM ProcedureAppointments pa
                    INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                    WHERE pa.AppointmentDateTime <= GETDATE() 
                      AND DATEADD(minute, pr.Duration, pa.AppointmentDateTime) > GETDATE() 
                      AND pa.Status = 'Назначена'";

                    using (SqlCommand cmd = new SqlCommand(updateInProgressQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но продолжаем выполнение
                Console.WriteLine("Ошибка при обновлении статусов: " + ex.Message);
            }
        }

        // Вспомогательный метод для преобразования DataTable в список словарей
        private List<Dictionary<string, object>> DataTableToList(DataTable dt)
        {
            var list = new List<Dictionary<string, object>>();
            
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
                
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                
                list.Add(dict);
            }
            
            return list;
        }
    }

    // Модель для экспорта отчета
    public class ReportExportModel
    {
        public string ReportType { get; set; }
        public string ReportName { get; set; }
        public string ReportHeader { get; set; }
        public string SummaryText { get; set; }
        public List<Dictionary<string, object>> Data { get; set; }
    }
} 