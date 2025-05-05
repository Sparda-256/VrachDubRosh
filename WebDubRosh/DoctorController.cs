using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;

namespace WebDubRosh.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorController : ControllerBase
    {
        // Строка подключения с TrustServerCertificate=True для работы через localtunnel
        private readonly string _connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True";

        // GET: api/doctor/{id}/patients
        [HttpGet("{id}/patients")]
        public IActionResult GetDoctorPatients(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"SELECT p.PatientID, p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate 
                               FROM Patients p
                               INNER JOIN PatientDoctorAssignments pda ON p.PatientID = pda.PatientID
                               WHERE pda.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", id);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/doctor/{id}/procedures
        [HttpGet("{id}/procedures")]
        public IActionResult GetDoctorProcedures(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT ProcedureID, ProcedureName, Duration FROM Procedures WHERE DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", id);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/doctor/{id}/appointments
        [HttpGet("{id}/appointments")]
        public IActionResult GetDoctorAppointments(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"SELECT pa.AppointmentID, p.FullName AS PatientName, p.PatientID,
                                   pr.ProcedureName, pr.ProcedureID, pa.AppointmentDateTime, 
                                   pr.Duration, pa.Status
                             FROM ProcedureAppointments pa
                             INNER JOIN Patients p ON pa.PatientID = p.PatientID
                             INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                             WHERE pa.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", id);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/doctor/{id}/patientsforappointment
        [HttpGet("{id}/patientsforappointment")]
        public IActionResult GetPatientsForAppointment(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"SELECT p.PatientID, p.FullName 
                               FROM Patients p
                               INNER JOIN PatientDoctorAssignments pda ON p.PatientID = pda.PatientID
                               WHERE pda.DoctorID = @DoctorID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", id);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/doctor/{id}/proceduresforappointment
        [HttpGet("{id}/proceduresforappointment")]
        public IActionResult GetProceduresForAppointment(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT ProcedureID, 
                               ProcedureName,
                               Duration,
                               CONCAT(ProcedureName, ' - ', Duration, ' мин.') AS DisplayText
                        FROM Procedures 
                        WHERE DoctorID = @DoctorID";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@DoctorID", id);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/doctor/assignprocedure
        [HttpPost("assignprocedure")]
        public IActionResult AssignProcedure([FromBody] AssignProcedureRequest request)
        {
            if (request == null || request.PatientID <= 0 || request.DoctorID <= 0 || 
                request.ProcedureID <= 0 || request.AppointmentDateTime == default)
            {
                return BadRequest(new { success = false, message = "Некорректные данные." });
            }

            try
            {
                // 1. Проверка на выписку пациента
                if (IsPatientDischarged(request.PatientID, request.AppointmentDateTime))
                {
                    return BadRequest(new { success = false, message = "Пациент не может быть записан на эту процедуру, так как он уже будет выписан к указанной дате." });
                }

                // 2. Проверка занятости врача
                if (IsDoctorOccupied(request.DoctorID, request.AppointmentDateTime, request.ProcedureID))
                {
                    return BadRequest(new { success = false, message = "Пересечение с другим назначением." });
                }

                // 3. Проверка занятости пациента
                if (IsPatientOccupied(request.PatientID, request.AppointmentDateTime, request.ProcedureID))
                {
                    return BadRequest(new { success = false, message = "Пациент уже записан на другую процедуру в это время." });
                }

                if (request.AppointmentDateTime <= DateTime.Now)
                {
                    return BadRequest(new { success = false, message = "Нельзя назначать процедуры на прошедшие дату и время." });
                }

                // Если все проверки прошли, записываем пациента на процедуру
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string insertQuery = @"INSERT INTO ProcedureAppointments (PatientID, DoctorID, ProcedureID, AppointmentDateTime, Status)
                                   VALUES (@PatientID, @DoctorID, @ProcedureID, @AppointmentDateTime, 'Назначена')";
                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", request.PatientID);
                        cmd.Parameters.AddWithValue("@DoctorID", request.DoctorID);
                        cmd.Parameters.AddWithValue("@ProcedureID", request.ProcedureID);
                        cmd.Parameters.AddWithValue("@AppointmentDateTime", request.AppointmentDateTime);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Обновляем статусы назначений
                UpdateAppointmentsStatus();

                return Ok(new { success = true, message = "Процедура успешно назначена." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/doctor/cancelappointment
        [HttpPost("cancelappointment")]
        public IActionResult CancelAppointment([FromBody] CancelAppointmentRequest request)
        {
            if (request == null || request.AppointmentID <= 0)
            {
                return BadRequest(new { success = false, message = "Некорректные данные." });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "UPDATE ProcedureAppointments SET Status = 'Отменена' WHERE AppointmentID = @AppointmentID AND Status != 'Завершена'";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AppointmentID", request.AppointmentID);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return BadRequest(new { success = false, message = "Не удалось отменить назначение. Возможно, оно уже завершено." });
                        }
                    }
                }

                return Ok(new { success = true, message = "Назначение успешно отменено." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/doctor/updateappointmentsstatus
        [HttpPost("updateappointmentsstatus")]
        public IActionResult UpdateAppointmentsStatusEndpoint()
        {
            try
            {
                UpdateAppointmentsStatus();
                return Ok(new { success = true, message = "Статусы назначений обновлены." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/doctor/addprocedure
        [HttpPost("addprocedure")]
        public IActionResult AddProcedure([FromBody] AddProcedureRequest request)
        {
            if (request == null || request.DoctorID <= 0 || string.IsNullOrEmpty(request.ProcedureName) || request.Duration <= 0)
            {
                return BadRequest(new { success = false, message = "Некорректные данные." });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"INSERT INTO Procedures (DoctorID, ProcedureName, Duration)
                                   VALUES (@DoctorID, @ProcedureName, @Duration);
                                   SELECT SCOPE_IDENTITY();";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", request.DoctorID);
                        cmd.Parameters.AddWithValue("@ProcedureName", request.ProcedureName);
                        cmd.Parameters.AddWithValue("@Duration", request.Duration);
                        
                        int newProcedureId = Convert.ToInt32(cmd.ExecuteScalar());
                        return Ok(new { success = true, procedureId = newProcedureId, message = "Процедура успешно добавлена." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/doctor/procedure/{id}
        [HttpDelete("procedure/{id}")]
        public IActionResult DeleteProcedure(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // 1. Удаляем все записи из ProcedureAppointments, где ProcedureID = id
                            string deleteAppointmentsQuery = "DELETE FROM ProcedureAppointments WHERE ProcedureID = @ProcedureID";
                            using (SqlCommand cmdAppointments = new SqlCommand(deleteAppointmentsQuery, con, tran))
                            {
                                cmdAppointments.Parameters.AddWithValue("@ProcedureID", id);
                                cmdAppointments.ExecuteNonQuery();
                            }

                            // 2. Удаляем саму процедуру из Procedures
                            string deleteProcedureQuery = "DELETE FROM Procedures WHERE ProcedureID = @ProcedureID";
                            using (SqlCommand cmdProcedure = new SqlCommand(deleteProcedureQuery, con, tran))
                            {
                                cmdProcedure.Parameters.AddWithValue("@ProcedureID", id);
                                int rowsAffected = cmdProcedure.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    tran.Rollback();
                                    return NotFound(new { success = false, message = "Процедура не найдена." });
                                }
                            }

                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }

                return Ok(new { success = true, message = "Процедура успешно удалена." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/doctor/adddescription
        [HttpPost("adddescription")]
        public IActionResult AddPatientDescription([FromBody] AddDescriptionRequest request)
        {
            if (request == null || request.PatientID <= 0 || request.DoctorID <= 0 || string.IsNullOrEmpty(request.Description))
            {
                return BadRequest(new { success = false, message = "Некорректные данные." });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"INSERT INTO PatientDescriptions (PatientID, DoctorID, Description)
                                   VALUES (@PatientID, @DoctorID, @Description)";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", request.PatientID);
                        cmd.Parameters.AddWithValue("@DoctorID", request.DoctorID);
                        cmd.Parameters.AddWithValue("@Description", request.Description);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { success = true, message = "Описание успешно добавлено." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #region Helper Methods
        private bool IsPatientDischarged(int patientID, DateTime appointmentDateTime)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT DischargeDate FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        object result = cmd.ExecuteScalar();

                        // Если дата выписки не установлена - пациент не выписан
                        if (result == DBNull.Value || result == null) return false;

                        DateTime dischargeDate = Convert.ToDateTime(result);
                        return dischargeDate <= appointmentDateTime.Date;
                    }
                }
            }
            catch
            {
                return true; // В случае ошибки считаем, что пациент выписан (для безопасности)
            }
        }

        private bool IsDoctorOccupied(int doctorID, DateTime appointmentDateTime, int procedureID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    // Получаем длительность процедуры
                    int duration = 0;
                    string durationQuery = "SELECT Duration FROM Procedures WHERE ProcedureID = @ProcedureID";
                    using (SqlCommand cmd = new SqlCommand(durationQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                        duration = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    DateTime endTime = appointmentDateTime.AddMinutes(duration);

                    string query = @"
                SELECT COUNT(*) 
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE 
                    pa.DoctorID = @DoctorID AND
                    pa.Status != 'Отменена' AND
                    (
                        (pa.AppointmentDateTime < @EndTime AND 
                        DATEADD(MINUTE, pr.Duration, pa.AppointmentDateTime) > @StartTime)
                    )";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", doctorID);
                        cmd.Parameters.AddWithValue("@StartTime", appointmentDateTime);
                        cmd.Parameters.AddWithValue("@EndTime", endTime);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return true; // В случае ошибки считаем, что врач занят (для безопасности)
            }
        }

        private bool IsPatientOccupied(int patientID, DateTime appointmentDateTime, int procedureID)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    int duration = 0;
                    string durationQuery = "SELECT Duration FROM Procedures WHERE ProcedureID = @ProcedureID";
                    using (SqlCommand cmd = new SqlCommand(durationQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@ProcedureID", procedureID);
                        duration = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    DateTime endTime = appointmentDateTime.AddMinutes(duration);

                    string query = @"
                SELECT COUNT(*) 
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE 
                    pa.PatientID = @PatientID AND
                    pa.Status != 'Отменена' AND
                    (
                        (pa.AppointmentDateTime < @EndTime AND 
                        DATEADD(MINUTE, pr.Duration, pa.AppointmentDateTime) > @StartTime)
                    )";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientID);
                        cmd.Parameters.AddWithValue("@StartTime", appointmentDateTime);
                        cmd.Parameters.AddWithValue("@EndTime", endTime);
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch
            {
                return true; // В случае ошибки считаем, что пациент занят (для безопасности)
            }
        }

        private void UpdateAppointmentsStatus()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
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
            catch
            {
                // Логирование ошибки
            }
        }

        private object DataTableToList(DataTable dt)
        {
            var list = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new System.Collections.Generic.Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                list.Add(dict);
            }
            return list;
        }
        #endregion
    }

    public class AssignProcedureRequest
    {
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public int ProcedureID { get; set; }
        public DateTime AppointmentDateTime { get; set; }
    }

    public class CancelAppointmentRequest
    {
        public int AppointmentID { get; set; }
    }

    public class AddProcedureRequest
    {
        public int DoctorID { get; set; }
        public string ProcedureName { get; set; }
        public int Duration { get; set; }
    }

    public class AddDescriptionRequest
    {
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public string Description { get; set; }
    }
} 