﻿using OfficeOpenXml;  // EPPlus
using OfficeOpenXml.Style;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VrachDubRosh
{
    public partial class ReportWindow : Window
    {
        private readonly string connectionString =
            "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework";

        // Для переключения темы
        private bool isDarkTheme = false;

        // Для фильтрации ComboBox
        private DataTable dtDoctors;
        private DataTable dtPatients;
        private DataTable dtProcedures;

        // Таблицы для экспорта и расчёта итогов
        private DataTable dtReportAll;
        private DataTable dtReportDoctor;
        private DataTable dtReportPatient;
        private DataTable dtReportProcedure;

        public ReportWindow()
        {
            UpdateAppointmentsStatus();
            // Разрешаем использование EPPlus в некоммерческом режиме
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            InitializeComponent();
            WindowState = WindowState.Maximized;
            Loaded += ReportWindow_Loaded;
        }

        private void ReportWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Инициализируем DatePicker'ы сегодняшней датой
            DateTime today = DateTime.Today;

            dpStart_All.SelectedDate = today;
            dpEnd_All.SelectedDate = today;

            dpStart_Doctor.SelectedDate = today;
            dpEnd_Doctor.SelectedDate = today;

            dpStart_Patient.SelectedDate = today;
            dpEnd_Patient.SelectedDate = today;

            dpStart_Procedure.SelectedDate = today;
            dpEnd_Procedure.SelectedDate = today;

            LoadDoctors();
            LoadPatients();
            LoadProcedures();
        }

        #region Переключение темы (светлая/тёмная)
        
        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            ChangeTheme(true);
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            ChangeTheme(false);
        }

        private void ChangeTheme(bool isDark)
        {
            isDarkTheme = isDark;
            ResourceDictionary resources = Resources;

            if (isDark)
            {
                // Тёмная тема
                resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                resources["SubtitleBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(70, 70, 70));
                resources["BorderHoverBrush"] = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                resources["InputBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                resources["AlternateRowBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
                resources["TabInactiveBrush"] = new SolidColorBrush(Color.FromRgb(60, 60, 60));
                resources["SelectionBrush"] = new SolidColorBrush(Color.FromRgb(45, 66, 85));
                resources["SelectionBorderBrush"] = new SolidColorBrush(Color.FromRgb(60, 90, 120));
                resources["SelectionForegroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }
            else
            {
                // Светлая тема
                resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 244, 248));
                resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
                resources["SubtitleBrush"] = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(208, 208, 208));
                resources["BorderHoverBrush"] = new SolidColorBrush(Color.FromRgb(187, 187, 187));
                resources["InputBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["AlternateRowBrush"] = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                resources["TabInactiveBrush"] = new SolidColorBrush(Color.FromRgb(238, 238, 238));
                resources["SelectionBrush"] = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                resources["SelectionBorderBrush"] = new SolidColorBrush(Color.FromRgb(144, 202, 249));
                resources["SelectionForegroundBrush"] = new SolidColorBrush(Colors.Black);
            }
        }

        #endregion

        private void UpdateAppointmentsStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    // 1. Сначала помечаем завершенные процедуры (независимо от текущего статуса)
                    string updateCompletedQuery = @"
                UPDATE pa
                SET pa.Status = 'Завершена'
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE DATEADD(minute, pr.Duration, pa.AppointmentDateTime) <= GETDATE() 
                  AND pa.Status IN ('Назначена', 'Идёт')"; // Обрабатываем оба статуса

                    using (SqlCommand cmd = new SqlCommand(updateCompletedQuery, con))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Затем помечаем процедуры, которые идут сейчас
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
                MessageBox.Show("Ошибка при обновлении статуса: " + ex.Message);
            }
        }
        #region Загрузка списков для ComboBox + поиск

        private void LoadDoctors()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT DoctorID, FullName FROM Doctors";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtDoctors = new DataTable();
                    da.Fill(dtDoctors);

                    cbDoctor.ItemsSource = dtDoctors.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки врачей: " + ex.Message);
            }
        }

        private void txtSearchDoctor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtDoctors == null) return;
            string filter = txtSearchDoctor.Text.Trim().Replace("'", "''");
            dtDoctors.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                ? ""
                : $"FullName LIKE '%{filter}%'";
        }

        private void LoadPatients()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName FROM Patients";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtPatients = new DataTable();
                    da.Fill(dtPatients);

                    cbPatient.ItemsSource = dtPatients.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пациентов: " + ex.Message);
            }
        }

        private void txtSearchPatient_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtPatients == null) return;
            string filter = txtSearchPatient.Text.Trim().Replace("'", "''");
            dtPatients.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                ? ""
                : $"FullName LIKE '%{filter}%'";
        }

        private void LoadProcedures()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Объединяем название процедуры и ФИО врача через INNER JOIN с таблицей Doctors
                    string query = @"
                SELECT 
                    pr.ProcedureID, 
                    (pr.ProcedureName + ' - ' + d.FullName) AS ProcedureDisplay
                FROM Procedures pr
                INNER JOIN Doctors d ON pr.DoctorID = d.DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    dtProcedures = new DataTable();
                    da.Fill(dtProcedures);

                    // Привязываем DataTable к ComboBox
                    cbProcedure.ItemsSource = dtProcedures.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки процедур: " + ex.Message);
            }
        }

        private void txtSearchProcedure_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dtProcedures == null) return;
            string filter = txtSearchProcedure.Text.Trim().Replace("'", "''");
            dtProcedures.DefaultView.RowFilter = string.IsNullOrEmpty(filter)
                ? ""
                : $"ProcedureName LIKE '%{filter}%'";
        }

        #endregion

        #region Вкладка 1: Все процедуры

        private void btnShowAll_Click(object sender, RoutedEventArgs e)
        {
            UpdateAppointmentsStatus();
            DateTime start = dpStart_All.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_All.SelectedDate ?? DateTime.Today;
            // Захватываем весь последний день
            end = end.AddDays(1).AddSeconds(-1);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Добавили Статус и Длительность (мин)
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
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@StartDate", start);
                    da.SelectCommand.Parameters.AddWithValue("@EndDate", end);

                    dtReportAll = new DataTable();
                    da.Fill(dtReportAll);
                    dgReportAll.ItemsSource = dtReportAll.DefaultView;
                }

                CalculateAllSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка формирования отчета: " + ex.Message);
            }
        }

        /// <summary>
        /// Итоговые показатели для вкладки "Все процедуры" с учётом того,
        /// что отменённые процедуры не считаются.
        /// </summary>
        private void CalculateAllSummary()
        {
            if (dtReportAll == null || dtReportAll.Rows.Count == 0)
            {
                txtAllSummary.Text = "Нет данных для отчета.";
                return;
            }

            // Фильтруем строки, исключая статус "Отменена"
            var validRows = dtReportAll.AsEnumerable()
                .Where(r => r.Field<string>("Статус") != "Отменена");

            if (!validRows.Any())
            {
                txtAllSummary.Text = "Все процедуры в выборке отменены.";
                return;
            }

            // Уникальные врачи, процедуры, пациенты
            int distinctDoctors = validRows.Select(r => r["Врач"]).Distinct().Count();
            int distinctProcedures = validRows.Select(r => r["Процедура"]).Distinct().Count();
            int distinctPatients = validRows.Select(r => r["Пациент"]).Distinct().Count();

            // Суммарная длительность
            int totalDuration = 0;
            foreach (var row in validRows)
            {
                if (row["Длительность (мин)"] != DBNull.Value)
                    totalDuration += Convert.ToInt32(row["Длительность (мин)"]);
            }

            txtAllSummary.Text =
                $"Общее количество врачей: {distinctDoctors}\n" +
                $"Общее количество процедур: {distinctProcedures}\n" +
                $"Общее количество пациентов: {distinctPatients}\n" +
                $"Общая длительность процедур: {totalDuration} мин";
        }

        private void btnExportAll_Click(object sender, RoutedEventArgs e)
        {
            if (dtReportAll == null || dtReportAll.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            DateTime start = dpStart_All.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_All.SelectedDate ?? DateTime.Today;
            string header = $"Все процедуры за период {start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            ExportToExcel(dtReportAll, "ВсеПроцедуры", txtAllSummary.Text, header);
        }

        #endregion

        #region Вкладка 2: Процедуры конкретного врача

        private void btnShowDoctor_Click(object sender, RoutedEventArgs e)
        {
            UpdateAppointmentsStatus();
            if (cbDoctor.SelectedValue == null)
            {
                MessageBox.Show("Выберите врача.");
                return;
            }
            int doctorID = Convert.ToInt32(cbDoctor.SelectedValue);
            DateTime start = dpStart_Doctor.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_Doctor.SelectedDate ?? DateTime.Today;
            end = end.AddDays(1).AddSeconds(-1);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Процедура, Пациент, Дата/время, Статус, Длительность
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
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", doctorID);
                    da.SelectCommand.Parameters.AddWithValue("@StartDate", start);
                    da.SelectCommand.Parameters.AddWithValue("@EndDate", end);

                    dtReportDoctor = new DataTable();
                    da.Fill(dtReportDoctor);
                    dgReportDoctor.ItemsSource = dtReportDoctor.DefaultView;
                }

                CalculateDoctorSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка формирования отчета для врача: " + ex.Message);
            }
        }

        private void CalculateDoctorSummary()
        {
            if (dtReportDoctor == null || dtReportDoctor.Rows.Count == 0)
            {
                txtDoctorSummary.Text = "Нет данных для отчета.";
                return;
            }

            // Исключаем отменённые процедуры
            var validRows = dtReportDoctor.AsEnumerable()
                .Where(r => r.Field<string>("Статус") != "Отменена");

            if (!validRows.Any())
            {
                txtDoctorSummary.Text = "Все процедуры в выборке отменены.";
                return;
            }

            // Уникальные процедуры, пациенты
            int distinctProcedures = validRows.Select(r => r["Процедура"]).Distinct().Count();
            int distinctPatients = validRows.Select(r => r["Пациент"]).Distinct().Count();

            // Суммарная длительность
            int totalDuration = 0;
            foreach (var row in validRows)
            {
                if (row["Длительность (мин)"] != DBNull.Value)
                    totalDuration += Convert.ToInt32(row["Длительность (мин)"]);
            }

            txtDoctorSummary.Text =
                $"Общее количество процедур: {distinctProcedures}\n" +
                $"Общее количество пациентов: {distinctPatients}\n" +
                $"Общая длительность процедур: {totalDuration} мин";
        }

        private void btnExportDoctor_Click(object sender, RoutedEventArgs e)
        {
            if (dtReportDoctor == null || dtReportDoctor.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            DataRowView doctor = cbDoctor.SelectedItem as DataRowView;
            string doctorName = doctor?["FullName"]?.ToString() ?? "Неизвестный врач";
            DateTime start = dpStart_Doctor.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_Doctor.SelectedDate ?? DateTime.Today;
            string header = $"Процедуры врача {doctorName} за период {start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            ExportToExcel(dtReportDoctor, "ПроцедурыВрача", txtDoctorSummary.Text, header);
        }

        #endregion

        #region Вкладка 3: Процедуры конкретного пациента

        private void btnShowPatient_Click(object sender, RoutedEventArgs e)
        {
            UpdateAppointmentsStatus();

            if (cbPatient.SelectedValue == null)
            {
                MessageBox.Show("Выберите пациента.");
                return;
            }
            int patientID = Convert.ToInt32(cbPatient.SelectedValue);
            DateTime start = dpStart_Patient.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_Patient.SelectedDate ?? DateTime.Today;
            end = end.AddDays(1).AddSeconds(-1);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Врач, Процедура, Дата/время, Статус, Длительность
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
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@PatientID", patientID);
                    da.SelectCommand.Parameters.AddWithValue("@StartDate", start);
                    da.SelectCommand.Parameters.AddWithValue("@EndDate", end);

                    dtReportPatient = new DataTable();
                    da.Fill(dtReportPatient);
                    dgReportPatient.ItemsSource = dtReportPatient.DefaultView;
                }

                CalculatePatientSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка формирования отчета для пациента: " + ex.Message);
            }
        }

        private void CalculatePatientSummary()
        {
            if (dtReportPatient == null || dtReportPatient.Rows.Count == 0)
            {
                txtPatientSummary.Text = "Нет данных для отчета.";
                return;
            }

            // Исключаем отменённые процедуры
            var validRows = dtReportPatient.AsEnumerable()
                .Where(r => r.Field<string>("Статус") != "Отменена");

            if (!validRows.Any())
            {
                txtPatientSummary.Text = "Все процедуры в выборке отменены.";
                return;
            }

            // Уникальные врачи, процедуры
            int distinctDoctors = validRows.Select(r => r["Врач"]).Distinct().Count();
            int distinctProcedures = validRows.Select(r => r["Процедура"]).Distinct().Count();

            // Суммарная длительность
            int totalDuration = 0;
            foreach (var row in validRows)
            {
                if (row["Длительность (мин)"] != DBNull.Value)
                    totalDuration += Convert.ToInt32(row["Длительность (мин)"]);
            }

            txtPatientSummary.Text =
                $"Общее количество врачей: {distinctDoctors}\n" +
                $"Общее количество процедур: {distinctProcedures}\n" +
                $"Общая длительность процедур: {totalDuration} мин";
        }

        private void btnExportPatient_Click(object sender, RoutedEventArgs e)
        {
            if (dtReportPatient == null || dtReportPatient.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            DataRowView patient = cbPatient.SelectedItem as DataRowView;
            string patientName = patient?["FullName"]?.ToString() ?? "Неизвестный пациент";
            DateTime start = dpStart_Patient.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_Patient.SelectedDate ?? DateTime.Today;
            string header = $"Процедуры пациента {patientName} за период {start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            ExportToExcel(dtReportPatient, "ПроцедурыПациента", txtPatientSummary.Text, header);
        }

        #endregion

        #region Вкладка 4: Назначения конкретной процедуры

        private void btnShowProcedure_Click(object sender, RoutedEventArgs e)
        {
            UpdateAppointmentsStatus();
            if (cbProcedure.SelectedValue == null)
            {
                MessageBox.Show("Выберите процедуру.");
                return;
            }
            int procedureID = Convert.ToInt32(cbProcedure.SelectedValue);
            DateTime start = dpStart_Procedure.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_Procedure.SelectedDate ?? DateTime.Today;
            end = end.AddDays(1).AddSeconds(-1);

            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    // Пациент, Дата/время, Статус, Длительность
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
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@ProcedureID", procedureID);
                    da.SelectCommand.Parameters.AddWithValue("@StartDate", start);
                    da.SelectCommand.Parameters.AddWithValue("@EndDate", end);

                    dtReportProcedure = new DataTable();
                    da.Fill(dtReportProcedure);
                    dgReportProcedure.ItemsSource = dtReportProcedure.DefaultView;
                }

                CalculateProcedureSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка формирования отчета для процедуры: " + ex.Message);
            }
        }

        private void CalculateProcedureSummary()
        {
            if (dtReportProcedure == null || dtReportProcedure.Rows.Count == 0)
            {
                txtProcedureSummary.Text = "Нет данных для отчета.";
                return;
            }

            // Исключаем отменённые процедуры
            var validRows = dtReportProcedure.AsEnumerable()
                .Where(r => r.Field<string>("Статус") != "Отменена");

            if (!validRows.Any())
            {
                txtProcedureSummary.Text = "Все процедуры в выборке отменены.";
                return;
            }

            // Уникальные пациенты
            int distinctPatients = validRows.Select(r => r["Пациент"]).Distinct().Count();

            // Суммарная длительность
            int totalDuration = 0;
            foreach (var row in validRows)
            {
                if (row["Длительность (мин)"] != DBNull.Value)
                    totalDuration += Convert.ToInt32(row["Длительность (мин)"]);
            }

            txtProcedureSummary.Text =
                $"Общее количество пациентов: {distinctPatients}\n" +
                $"Общая длительность процедуры: {totalDuration} мин";
        }

        private void btnExportProcedure_Click(object sender, RoutedEventArgs e)
        {
            UpdateAppointmentsStatus();
            if (dtReportProcedure == null || dtReportProcedure.Rows.Count == 0)
            {
                MessageBox.Show("Нет данных для экспорта.");
                return;
            }

            DataRowView procedure = cbProcedure.SelectedItem as DataRowView;
            string procedureName = procedure?["ProcedureName"]?.ToString() ?? "Неизвестная процедура";
            DateTime start = dpStart_Procedure.SelectedDate ?? DateTime.Today;
            DateTime end = dpEnd_Procedure.SelectedDate ?? DateTime.Today;
            string header = $"Назначения процедуры {procedureName} за период {start:dd.MM.yyyy} - {end:dd.MM.yyyy}";
            ExportToExcel(dtReportProcedure, "НазначенияПроцедуры", txtProcedureSummary.Text, header);
        }

        #endregion

        #region Экспорт в Excel (с итоговыми показателями)

        /// <summary>
        /// Экспортирует содержимое DataTable в Excel-файл (EPPlus).
        /// Даты/время форматируются в "dd.MM.yyyy HH:mm".
        /// Внизу добавляется текст итоговых показателей (summaryText).
        /// </summary>
        private void ExportToExcel(DataTable dt, string reportName, string summaryText, string reportHeader)
        {
            UpdateAppointmentsStatus();
            try
            {
                using (ExcelPackage package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(reportName);

                    // Добавляем заголовок отчёта
                    if (!string.IsNullOrEmpty(reportHeader))
                    {
                        var headerRange = worksheet.Cells[1, 1, 1, dt.Columns.Count];
                        headerRange.Merge = true;
                        headerRange.Value = reportHeader;
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Font.Size = 14;
                        headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // Заголовки столбцов (строка 3)
                    for (int col = 0; col < dt.Columns.Count; col++)
                    {
                        worksheet.Cells[3, col + 1].Value = dt.Columns[col].ColumnName;
                        worksheet.Cells[3, col + 1].Style.Font.Bold = true;
                    }

                    // Данные (начиная со строки 4)
                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        for (int col = 0; col < dt.Columns.Count; col++)
                        {
                            object value = dt.Rows[row][col];
                            if (dt.Columns[col].ColumnName == "Дата/время" && value != DBNull.Value)
                            {
                                DateTime dtValue = Convert.ToDateTime(value);
                                var cell = worksheet.Cells[row + 4, col + 1];
                                cell.Value = dtValue;
                                cell.Style.Numberformat.Format = "dd.MM.yyyy HH:mm";
                            }
                            else
                            {
                                worksheet.Cells[row + 4, col + 1].Value = value;
                            }
                        }
                    }

                    // Автоширина столбцов
                    worksheet.Cells[1, 1, dt.Rows.Count + 4, dt.Columns.Count].AutoFitColumns();

                    // Итоговые показатели
                    if (!string.IsNullOrEmpty(summaryText))
                    {
                        int summaryStartRow = dt.Rows.Count + 5;
                        var lines = summaryText.Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            worksheet.Cells[summaryStartRow + i, 1].Value = lines[i];
                        }
                    }

                    // Сохранение
                    var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "Excel Files (*.xlsx)|*.xlsx",
                        FileName = $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };

                    if (saveFileDialog.ShowDialog() == true)
                    {
                        File.WriteAllBytes(saveFileDialog.FileName, package.GetAsByteArray());
                        MessageBox.Show("Отчет успешно сохранен!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте в Excel: " + ex.Message);
            }
        }

        #endregion

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateAppointmentsStatus();
            this.Close();
        }
    }
}