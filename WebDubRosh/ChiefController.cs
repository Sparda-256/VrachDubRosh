using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;

namespace WebDubRosh.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChiefController : ControllerBase
    {
        // Обновлённая строка подключения с TrustServerCertificate=True для работы через localtunnel
        private readonly string _connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True";

        // GET: api/chief/patients
        [HttpGet("patients")]
        public IActionResult GetPatients()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName, DateOfBirth, Gender, RecordDate, DischargeDate FROM Patients";
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
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/chief/doctors
        [HttpGet("doctors")]
        public IActionResult GetDoctors()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT DoctorID, FullName, Specialty, GeneralName, OfficeNumber, WorkExperience FROM Doctors";
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
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/chief/patient/{id}/medcard
        [HttpGet("patient/{id}/medcard")]
        public IActionResult GetPatientMedCard(int id)
        {
            try
            {
                // Основная информация о пациенте
                var patientInfo = new System.Collections.Generic.Dictionary<string, object>();
                // Процедуры пациента
                var patientProcedures = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>();
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // Получаем информацию о пациенте
                    string patientQuery = @"
                        SELECT P.PatientID, P.FullName, P.DateOfBirth, P.Gender, P.RecordDate, P.DischargeDate
                        FROM Patients P
                        WHERE P.PatientID = @PatientID";
                    
                    using (SqlCommand cmd = new SqlCommand(patientQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                patientInfo["PatientID"] = reader["PatientID"];
                                patientInfo["FullName"] = reader["FullName"];
                                patientInfo["DateOfBirth"] = Convert.ToDateTime(reader["DateOfBirth"]).ToString("yyyy-MM-dd");
                                patientInfo["Gender"] = reader["Gender"];
                                patientInfo["RecordDate"] = Convert.ToDateTime(reader["RecordDate"]).ToString("yyyy-MM-dd");
                                patientInfo["DischargeDate"] = Convert.ToDateTime(reader["DischargeDate"]).ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Пациент не найден" });
                            }
                        }
                    }
                }
                
                // Возвращаем всю информацию о пациенте
                return Ok(new { 
                    success = true, 
                    patient = patientInfo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/chief/doctor/{id}/procedures
        [HttpGet("doctor/{id}/procedures")]
        public IActionResult GetDoctorProcedures(int id)
        {
            try
            {
                // Информация о процедурах врача
                DataTable dt = new DataTable();
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT p.ProcedureID, p.ProcedureName, p.Duration, p.Description
                        FROM Procedures p
                        WHERE p.DoctorID = @DoctorID";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", id);
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
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/chief/patient/{id}
        [HttpDelete("patient/{id}")]
        public IActionResult DeletePatient(int id)
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
                            // Удаляем процедуры, назначенные пациенту
                            string deleteProcedureAppointments = "DELETE FROM ProcedureAppointments WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deleteProcedureAppointments, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаляем описания пациента
                            string deleteDescriptions = "DELETE FROM PatientDescriptions WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deleteDescriptions, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаляем связи пациента с врачами
                            string deleteAssignments = "DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deleteAssignments, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаляем пациента
                            string deletePatient = "DELETE FROM Patients WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(deletePatient, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    tran.Rollback();
                                    return NotFound(new { success = false, message = "Пациент не найден." });
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
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/chief/doctor/{id}
        [HttpDelete("doctor/{id}")]
        public IActionResult DeleteDoctor(int id)
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
                            // Удаляем назначенные процедуры
                            string deleteAppointments = "DELETE FROM ProcedureAppointments WHERE DoctorID = @DoctorID";
                            using (SqlCommand cmd = new SqlCommand(deleteAppointments, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@DoctorID", id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаляем описания пациентов, созданные этим врачом
                            string deleteDescriptions = "DELETE FROM PatientDescriptions WHERE DoctorID = @DoctorID";
                            using (SqlCommand cmd = new SqlCommand(deleteDescriptions, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@DoctorID", id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаляем связи врача с пациентами
                            string deleteAssignments = "DELETE FROM PatientDoctorAssignments WHERE DoctorID = @DoctorID";
                            using (SqlCommand cmd = new SqlCommand(deleteAssignments, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@DoctorID", id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаляем процедуры врача
                            string deleteProcedures = "DELETE FROM Procedures WHERE DoctorID = @DoctorID";
                            using (SqlCommand cmd = new SqlCommand(deleteProcedures, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@DoctorID", id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Удаляем врача
                            string deleteDoctor = "DELETE FROM Doctors WHERE DoctorID = @DoctorID";
                            using (SqlCommand cmd = new SqlCommand(deleteDoctor, con, tran))
                            {
                                cmd.Parameters.AddWithValue("@DoctorID", id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    tran.Rollback();
                                    return NotFound(new { success = false, message = "Врач не найден." });
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
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Вспомогательный метод для преобразования DataTable в список словарей
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
    }
}
