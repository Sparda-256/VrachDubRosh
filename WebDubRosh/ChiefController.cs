using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Data.SqlClient;

namespace PomoshnikPolicliniki.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChiefController : ControllerBase
    {
        // Обновлённая строка подключения с TrustServerCertificate=True для работы через localtunnel
        private readonly string _connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True";

        // GET: api/chief/newpatients
        [HttpGet("newpatients")]
        public IActionResult GetNewPatients()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT NewPatientID, FullName, DateOfBirth, Gender FROM NewPatients";
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
                    string query = "SELECT DoctorID, FullName, Specialty, OfficeNumber, WorkExperience FROM Doctors";
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

        // POST: api/chief/assignPatient
        [HttpPost("assignPatient")]
        public IActionResult AssignPatient([FromBody] AssignPatientRequest request)
        {
            if (request == null || request.NewPatientID <= 0 || request.DoctorID <= 0 || request.RecordDate == null || request.DischargeDate == null)
            {
                return BadRequest(new { success = false, message = "Некорректные данные." });
            }

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        // 1. Перенос пациента в таблицу Patients
                        string insertQuery = @"
                            INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, DischargeDate)
                            SELECT FullName, DateOfBirth, Gender, @RecordDate, @DischargeDate FROM NewPatients
                            WHERE NewPatientID = @NewPatientID;
                            SELECT SCOPE_IDENTITY();";
                        int newPatientInsertedID;
                        using (SqlCommand cmdInsert = new SqlCommand(insertQuery, con, tran))
                        {
                            cmdInsert.Parameters.AddWithValue("@RecordDate", request.RecordDate);
                            cmdInsert.Parameters.AddWithValue("@DischargeDate", request.DischargeDate);
                            cmdInsert.Parameters.AddWithValue("@NewPatientID", request.NewPatientID);
                            object result = cmdInsert.ExecuteScalar();
                            newPatientInsertedID = Convert.ToInt32(result);
                        }

                        // 2. Связь пациента с выбранным врачом
                        string assignQuery = "INSERT INTO PatientDoctorAssignments (PatientID, DoctorID) VALUES (@PatientID, @DoctorID)";
                        using (SqlCommand cmdAssign = new SqlCommand(assignQuery, con, tran))
                        {
                            cmdAssign.Parameters.AddWithValue("@PatientID", newPatientInsertedID);
                            cmdAssign.Parameters.AddWithValue("@DoctorID", request.DoctorID);
                            cmdAssign.ExecuteNonQuery();
                        }

                        // 3. Удаление записей из таблиц NewPatientSymptoms, NewPatientAnswers, NewPatientDiagnoses
                        string deleteSymptomsQuery = "DELETE FROM NewPatientSymptoms WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelSymptoms = new SqlCommand(deleteSymptomsQuery, con, tran))
                        {
                            cmdDelSymptoms.Parameters.AddWithValue("@NewPatientID", request.NewPatientID);
                            cmdDelSymptoms.ExecuteNonQuery();
                        }

                        string deleteAnswersQuery = "DELETE FROM NewPatientAnswers WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelAnswers = new SqlCommand(deleteAnswersQuery, con, tran))
                        {
                            cmdDelAnswers.Parameters.AddWithValue("@NewPatientID", request.NewPatientID);
                            cmdDelAnswers.ExecuteNonQuery();
                        }

                        string deleteDiagnosesQuery = "DELETE FROM NewPatientDiagnoses WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelDiagnoses = new SqlCommand(deleteDiagnosesQuery, con, tran))
                        {
                            cmdDelDiagnoses.Parameters.AddWithValue("@NewPatientID", request.NewPatientID);
                            cmdDelDiagnoses.ExecuteNonQuery();
                        }

                        // 4. Удаление записи из NewPatients
                        string deleteNewPatientQuery = "DELETE FROM NewPatients WHERE NewPatientID = @NewPatientID";
                        using (SqlCommand cmdDelete = new SqlCommand(deleteNewPatientQuery, con, tran))
                        {
                            cmdDelete.Parameters.AddWithValue("@NewPatientID", request.NewPatientID);
                            cmdDelete.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                }
                return Ok(new { success = true });
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
                    // Удаляем связи пациента с врачами
                    string deleteAssignmentsQuery = "DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(deleteAssignmentsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        cmd.ExecuteNonQuery();
                    }
                    // Удаляем пациента
                    string deletePatientQuery = "DELETE FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(deletePatientQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Пациент не найден." });
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
                    // Удаляем связи врача с пациентами
                    string deleteAssignmentsQuery = "DELETE FROM PatientDoctorAssignments WHERE DoctorID = @DoctorID";
                    using (SqlCommand cmd = new SqlCommand(deleteAssignmentsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", id);
                        cmd.ExecuteNonQuery();
                    }
                    // Удаляем врача
                    string deleteDoctorQuery = "DELETE FROM Doctors WHERE DoctorID = @DoctorID";
                    using (SqlCommand cmd = new SqlCommand(deleteDoctorQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", id);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Врач не найден." });
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

    public class AssignPatientRequest
    {
        public int NewPatientID { get; set; }
        public int DoctorID { get; set; }
        public DateTime RecordDate { get; set; }
        public DateTime DischargeDate { get; set; }
    }
}
