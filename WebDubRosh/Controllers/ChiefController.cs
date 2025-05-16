using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

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
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate, p.StayType,
                               CONCAT('Корпус ', b.BuildingNumber, ', комната ', r.RoomNumber, ', кровать ', a.BedNumber) AS Accommodation
                        FROM Patients p
                        LEFT JOIN Accommodations a ON p.PatientID = a.PatientID
                        LEFT JOIN Rooms r ON a.RoomID = r.RoomID
                        LEFT JOIN Buildings b ON r.BuildingID = b.BuildingID
                        WHERE p.PatientID = @PatientID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var patientInfo = new
                                {
                                    FullName = reader["FullName"].ToString(),
                                    DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfBirth"]) : (DateTime?)null,
                                    RecordDate = reader["RecordDate"] != DBNull.Value ? Convert.ToDateTime(reader["RecordDate"]) : (DateTime?)null,
                                    DischargeDate = reader["DischargeDate"] != DBNull.Value ? Convert.ToDateTime(reader["DischargeDate"]) : (DateTime?)null,
                                    StayType = reader["StayType"].ToString(),
                                    Accommodation = reader["Accommodation"].ToString()
                                };

                                return Ok(patientInfo);
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Пациент не найден" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении информации о пациенте: " + ex.Message });
            }
        }

        // GET: api/chief/patient/{id}/diagnoses
        [HttpGet("patient/{id}/diagnoses")]
        public IActionResult GetPatientDiagnoses(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT d.DiagnosisID, d.DiagnosisName, pd.DiagnosisType
                        FROM PatientDiagnoses pd
                        JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                        WHERE pd.PatientID = @PatientID
                        ORDER BY pd.DiagnosisType, d.DiagnosisName";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            var diagnoses = new List<object>();

                            while (reader.Read())
                            {
                                diagnoses.Add(new
                                {
                                    DiagnosisId = Convert.ToInt32(reader["DiagnosisID"]),
                                    DiagnosisName = reader["DiagnosisName"].ToString(),
                                    DiagnosisType = reader["DiagnosisType"].ToString()
                                });
                            }

                            return Ok(diagnoses);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении диагнозов: " + ex.Message });
            }
        }

        // GET: api/chief/diagnoses
        [HttpGet("diagnoses")]
        public IActionResult GetAllDiagnoses()
        {
            try
            {
                List<object> diagnoses = new List<object>();

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT DiagnosisID, DiagnosisName FROM Diagnoses ORDER BY DiagnosisName";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                diagnoses.Add(new
                                {
                                    DiagnosisId = Convert.ToInt32(reader["DiagnosisID"]),
                                    DiagnosisName = reader["DiagnosisName"].ToString()
                                });
                            }
                        }
                    }
                }

                return Ok(diagnoses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении списка диагнозов: " + ex.Message });
            }
        }

        // POST: api/chief/patient-diagnosis
        [HttpPost("patient-diagnosis")]
        public IActionResult AddPatientDiagnosis([FromBody] PatientDiagnosisModel model)
        {
            try
            {
                if (model == null || model.PatientId <= 0 || model.DiagnosisId <= 0)
                {
                    return BadRequest(new { success = false, message = "Некорректные данные" });
                }

                // Проверяем, что диагноз еще не добавлен пациенту
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string checkQuery = @"
                        SELECT COUNT(*) 
                        FROM PatientDiagnoses 
                        WHERE PatientID = @PatientID AND DiagnosisID = @DiagnosisID";
                    
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@PatientID", model.PatientId);
                        checkCmd.Parameters.AddWithValue("@DiagnosisID", model.DiagnosisId);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        
                        if (count > 0)
                        {
                            return Conflict(new { success = false, message = "Этот диагноз уже добавлен пациенту" });
                        }
                    }

                    // Добавляем диагноз пациенту
                    string insertQuery = @"
                        INSERT INTO PatientDiagnoses (PatientID, DiagnosisID, DiagnosisType)
                        VALUES (@PatientID, @DiagnosisID, @DiagnosisType)";

                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", model.PatientId);
                        cmd.Parameters.AddWithValue("@DiagnosisID", model.DiagnosisId);
                        cmd.Parameters.AddWithValue("@DiagnosisType", model.DiagnosisType ?? "Сопутствующий");
                        
                        cmd.ExecuteNonQuery();
                    }
                }

                return Ok(new { success = true, message = "Диагноз успешно добавлен" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при добавлении диагноза: " + ex.Message });
            }
        }

        // DELETE: api/chief/patient-diagnosis/{patientId}/{diagnosisId}
        [HttpDelete("patient-diagnosis/{patientId}/{diagnosisId}")]
        public IActionResult RemovePatientDiagnosis(int patientId, int diagnosisId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        DELETE FROM PatientDiagnoses 
                        WHERE PatientID = @PatientID AND DiagnosisID = @DiagnosisID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientId);
                        cmd.Parameters.AddWithValue("@DiagnosisID", diagnosisId);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Диагноз не найден" });
                        }
                    }
                }

                return Ok(new { success = true, message = "Диагноз успешно удален" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при удалении диагноза: " + ex.Message });
            }
        }

        // GET: api/chief/patient/{id}/measurements
        [HttpGet("patient/{id}/measurements")]
        public IActionResult GetPatientMeasurements(int id)
        {
            try
            {
                List<object> measurements = new List<object>();

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            MeasurementType,
                            Height,
                            Weight,
                            BloodPressure,
                            MeasurementDate
                        FROM PatientMeasurements
                        WHERE PatientID = @PatientID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                measurements.Add(new
                                {
                                    MeasurementType = reader["MeasurementType"].ToString(),
                                    Height = reader["Height"] != DBNull.Value ? Convert.ToDecimal(reader["Height"]) : (decimal?)null,
                                    Weight = reader["Weight"] != DBNull.Value ? Convert.ToDecimal(reader["Weight"]) : (decimal?)null,
                                    BloodPressure = reader["BloodPressure"].ToString(),
                                    MeasurementDate = reader["MeasurementDate"] != DBNull.Value ? Convert.ToDateTime(reader["MeasurementDate"]) : (DateTime?)null
                                });
                            }
                        }
                    }
                }

                return Ok(measurements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении измерений: " + ex.Message });
            }
        }

        // POST: api/chief/patient-measurements
        [HttpPost("patient-measurements")]
        public IActionResult SavePatientMeasurements([FromBody] PatientMeasurementsModel model)
        {
            try
            {
                if (model == null || model.PatientId <= 0 || model.Measurements == null || model.Measurements.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Некорректные данные измерений" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            foreach (var measurement in model.Measurements)
                            {
                                if (string.IsNullOrWhiteSpace(measurement.MeasurementType))
                                {
                                    continue;
                                }
                                
                                // Проверяем, существует ли запись для этого типа
                                string checkQuery = @"
                                    SELECT COUNT(*) 
                                    FROM PatientMeasurements 
                                    WHERE PatientID = @PatientID AND MeasurementType = @MeasurementType";
                                
                                bool recordExists = false;
                                using (SqlCommand checkCmd = new SqlCommand(checkQuery, con, transaction))
                                {
                                    checkCmd.Parameters.AddWithValue("@PatientID", model.PatientId);
                                    checkCmd.Parameters.AddWithValue("@MeasurementType", measurement.MeasurementType);
                                    int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                                    recordExists = count > 0;
                                }
                                
                                if (recordExists)
                                {
                                    // Обновляем существующую запись
                                    string updateQuery = @"
                                        UPDATE PatientMeasurements 
                                        SET Height = @Height,
                                            Weight = @Weight,
                                            BloodPressure = @BloodPressure,
                                            MeasurementDate = @MeasurementDate
                                        WHERE PatientID = @PatientID AND MeasurementType = @MeasurementType";
                                    
                                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, con, transaction))
                                    {
                                        updateCmd.Parameters.AddWithValue("@PatientID", model.PatientId);
                                        updateCmd.Parameters.AddWithValue("@MeasurementType", measurement.MeasurementType);
                                        updateCmd.Parameters.AddWithValue("@Height", measurement.Height.HasValue ? (object)measurement.Height.Value : DBNull.Value);
                                        updateCmd.Parameters.AddWithValue("@Weight", measurement.Weight.HasValue ? (object)measurement.Weight.Value : DBNull.Value);
                                        updateCmd.Parameters.AddWithValue("@BloodPressure", string.IsNullOrWhiteSpace(measurement.BloodPressure) ? DBNull.Value : (object)measurement.BloodPressure);
                                        updateCmd.Parameters.AddWithValue("@MeasurementDate", measurement.MeasurementDate.HasValue ? (object)measurement.MeasurementDate.Value : DBNull.Value);
                                        
                                        updateCmd.ExecuteNonQuery();
                                    }
                                }
                                else
                                {
                                    // Создаем новую запись
                                    string insertQuery = @"
                                        INSERT INTO PatientMeasurements 
                                            (PatientID, MeasurementType, Height, Weight, BloodPressure, MeasurementDate) 
                                        VALUES 
                                            (@PatientID, @MeasurementType, @Height, @Weight, @BloodPressure, @MeasurementDate)";
                                    
                                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, con, transaction))
                                    {
                                        insertCmd.Parameters.AddWithValue("@PatientID", model.PatientId);
                                        insertCmd.Parameters.AddWithValue("@MeasurementType", measurement.MeasurementType);
                                        insertCmd.Parameters.AddWithValue("@Height", measurement.Height.HasValue ? (object)measurement.Height.Value : DBNull.Value);
                                        insertCmd.Parameters.AddWithValue("@Weight", measurement.Weight.HasValue ? (object)measurement.Weight.Value : DBNull.Value);
                                        insertCmd.Parameters.AddWithValue("@BloodPressure", string.IsNullOrWhiteSpace(measurement.BloodPressure) ? DBNull.Value : (object)measurement.BloodPressure);
                                        insertCmd.Parameters.AddWithValue("@MeasurementDate", measurement.MeasurementDate.HasValue ? (object)measurement.MeasurementDate.Value : DBNull.Value);
                                        
                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            
                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }

                return Ok(new { success = true, message = "Измерения успешно сохранены" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при сохранении измерений: " + ex.Message });
            }
        }

        // GET: api/chief/patient/{id}/medications
        [HttpGet("patient/{id}/medications")]
        public IActionResult GetPatientMedications(int id)
        {
            try
            {
                List<object> medications = new List<object>();

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            MedicationID,
                            MedicationName,
                            Dosage,
                            Instructions,
                            PrescribedDate
                        FROM PatientMedications
                        WHERE PatientID = @PatientID
                        ORDER BY PrescribedDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                medications.Add(new
                                {
                                    MedicationId = Convert.ToInt32(reader["MedicationID"]),
                                    MedicationName = reader["MedicationName"].ToString(),
                                    Dosage = reader["Dosage"] != DBNull.Value ? reader["Dosage"].ToString() : null,
                                    Instructions = reader["Instructions"] != DBNull.Value ? reader["Instructions"].ToString() : null,
                                    PrescribedDate = reader["PrescribedDate"] != DBNull.Value ? Convert.ToDateTime(reader["PrescribedDate"]) : (DateTime?)null
                                });
                            }
                        }
                    }
                }

                return Ok(medications);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении медикаментов: " + ex.Message });
            }
        }

        // POST: api/chief/patient-medication
        [HttpPost("patient-medication")]
        public IActionResult AddPatientMedication([FromBody] PatientMedicationModel model)
        {
            try
            {
                if (model == null || model.PatientId <= 0 || string.IsNullOrWhiteSpace(model.MedicationName))
                {
                    return BadRequest(new { success = false, message = "Некорректные данные медикамента" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        INSERT INTO PatientMedications 
                            (PatientID, MedicationName, Dosage, Instructions, PrescribedDate) 
                        VALUES 
                            (@PatientID, @MedicationName, @Dosage, @Instructions, @PrescribedDate);
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", model.PatientId);
                        cmd.Parameters.AddWithValue("@MedicationName", model.MedicationName);
                        cmd.Parameters.AddWithValue("@Dosage", string.IsNullOrWhiteSpace(model.Dosage) ? DBNull.Value : (object)model.Dosage);
                        cmd.Parameters.AddWithValue("@Instructions", string.IsNullOrWhiteSpace(model.Instructions) ? DBNull.Value : (object)model.Instructions);
                        cmd.Parameters.AddWithValue("@PrescribedDate", model.PrescribedDate.HasValue ? (object)model.PrescribedDate.Value : (object)DateTime.Now);
                        
                        int medicationId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        return Ok(new { success = true, message = "Медикамент успешно добавлен", medicationId = medicationId });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при добавлении медикамента: " + ex.Message });
            }
        }

        // PUT: api/chief/patient-medication
        [HttpPut("patient-medication")]
        public IActionResult UpdatePatientMedication([FromBody] PatientMedicationModel model)
        {
            try
            {
                if (model == null || model.MedicationId <= 0 || string.IsNullOrWhiteSpace(model.MedicationName))
                {
                    return BadRequest(new { success = false, message = "Некорректные данные медикамента" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        UPDATE PatientMedications 
                        SET MedicationName = @MedicationName,
                            Dosage = @Dosage,
                            Instructions = @Instructions,
                            PrescribedDate = @PrescribedDate
                        WHERE MedicationID = @MedicationID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@MedicationID", model.MedicationId);
                        cmd.Parameters.AddWithValue("@MedicationName", model.MedicationName);
                        cmd.Parameters.AddWithValue("@Dosage", string.IsNullOrWhiteSpace(model.Dosage) ? DBNull.Value : (object)model.Dosage);
                        cmd.Parameters.AddWithValue("@Instructions", string.IsNullOrWhiteSpace(model.Instructions) ? DBNull.Value : (object)model.Instructions);
                        cmd.Parameters.AddWithValue("@PrescribedDate", model.PrescribedDate.HasValue ? (object)model.PrescribedDate.Value : (object)DateTime.Now);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Медикамент не найден" });
                        }
                        
                        return Ok(new { success = true, message = "Медикамент успешно обновлен" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при обновлении медикамента: " + ex.Message });
            }
        }

        // DELETE: api/chief/patient-medication/{id}
        [HttpDelete("patient-medication/{id}")]
        public IActionResult RemovePatientMedication(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        DELETE FROM PatientMedications 
                        WHERE MedicationID = @MedicationID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@MedicationID", id);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Медикамент не найден" });
                        }
                    }
                }

                return Ok(new { success = true, message = "Медикамент успешно удален" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при удалении медикамента: " + ex.Message });
            }
        }

        // GET: api/chief/patient/{id}/doctor-procedures
        [HttpGet("patient/{id}/doctor-procedures")]
        public IActionResult GetPatientDoctorProcedures(int id)
        {
            try
            {
                var doctorProceduresList = new List<object>();

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // Сначала получаем список врачей, назначенных данному пациенту
                    string doctorsQuery = @"
                        SELECT DISTINCT 
                            d.DoctorID,
                            d.FullName,
                            d.Specialty,
                            d.GeneralName,
                            d.OfficeNumber
                        FROM Doctors d
                        JOIN PatientDoctorAssignments pda ON d.DoctorID = pda.DoctorID
                        WHERE pda.PatientID = @PatientID
                        ORDER BY d.GeneralName";

                    using (SqlCommand cmd = new SqlCommand(doctorsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int doctorId = Convert.ToInt32(reader["DoctorID"]);
                                string fullName = reader["FullName"].ToString();
                                string specialty = reader["Specialty"].ToString();
                                string generalName = reader["GeneralName"].ToString();
                                string officeNumber = reader["OfficeNumber"].ToString();
                                
                                // Get procedures for this doctor
                                List<object> procedures = GetProceduresForDoctor(con, id, doctorId);
                                
                                // Add doctor with procedures to the list
                                doctorProceduresList.Add(new
                                {
                                    DoctorId = doctorId,
                                    FullName = fullName,
                                    Specialty = specialty,
                                    GeneralName = generalName,
                                    OfficeNumber = officeNumber,
                                    Procedures = procedures
                                });
                            }
                        }
                    }
                }

                return Ok(doctorProceduresList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении процедур по врачам: " + ex.Message });
            }
        }
        
        // Helper method to get procedures for a doctor
        private List<object> GetProceduresForDoctor(SqlConnection con, int patientId, int doctorId)
        {
            var procedures = new List<object>();
            
            string proceduresQuery = @"
                SELECT 
                    pa.AppointmentID,
                    pr.ProcedureName,
                    pa.AppointmentDateTime,
                    pa.Status,
                    pa.Description
                FROM ProcedureAppointments pa
                JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE pa.PatientID = @PatientID AND pa.DoctorID = @DoctorID
                ORDER BY pa.AppointmentDateTime DESC";

            using (SqlCommand cmd = new SqlCommand(proceduresQuery, con))
            {
                cmd.Parameters.AddWithValue("@PatientID", patientId);
                cmd.Parameters.AddWithValue("@DoctorID", doctorId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        procedures.Add(new
                        {
                            AppointmentId = Convert.ToInt32(reader["AppointmentID"]),
                            ProcedureName = reader["ProcedureName"].ToString(),
                            AppointmentDateTime = reader["AppointmentDateTime"] != DBNull.Value ? Convert.ToDateTime(reader["AppointmentDateTime"]) : (DateTime?)null,
                            Status = reader["Status"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null
                        });
                    }
                }
            }
            
            return procedures;
        }

        // GET: api/chief/doctor/{id}/procedures
        [HttpGet("doctor/{id}/procedures")]
        public IActionResult GetDoctorProcedures(int id)
        {
            try
            {
                var procedures = new List<object>();
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT ProcedureID, ProcedureName, Duration 
                        FROM Procedures 
                        WHERE DoctorID = @DoctorID
                        ORDER BY ProcedureName";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                procedures.Add(new
                                {
                                    ProcedureID = reader.GetInt32(0),
                                    ProcedureName = reader.GetString(1),
                                    Duration = reader.GetInt32(2)
                                });
                            }
                        }
                    }
                }

                return Ok(procedures);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка при получении процедур врача: {ex.Message}" });
            }
        }

        // POST: api/chief/doctor
        [HttpPost("doctor")]
        public IActionResult AddDoctor([FromBody] DoctorModel doctor)
        {
            try
            {
                if (doctor == null)
                {
                    return BadRequest(new { success = false, message = "Данные врача не предоставлены" });
                }

                if (string.IsNullOrEmpty(doctor.FullName) || string.IsNullOrEmpty(doctor.Specialty))
                {
                    return BadRequest(new { success = false, message = "ФИО и специальность врача обязательны" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        INSERT INTO Doctors (FullName, Specialty, GeneralName, OfficeNumber, WorkExperience, Password)
                        VALUES (@FullName, @Specialty, @GeneralName, @OfficeNumber, @WorkExperience, @Password);
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@FullName", doctor.FullName);
                        cmd.Parameters.AddWithValue("@Specialty", doctor.Specialty);
                        cmd.Parameters.AddWithValue("@GeneralName", string.IsNullOrEmpty(doctor.GeneralName) ? DBNull.Value : (object)doctor.GeneralName);
                        cmd.Parameters.AddWithValue("@OfficeNumber", doctor.OfficeNumber);
                        cmd.Parameters.AddWithValue("@WorkExperience", doctor.WorkExperience);
                        cmd.Parameters.AddWithValue("@Password", string.IsNullOrEmpty(doctor.Password) ? "1" : doctor.Password);

                        int newDoctorId = Convert.ToInt32(cmd.ExecuteScalar());
                        return Ok(new { success = true, doctorId = newDoctorId });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/chief/doctor/{id}
        [HttpPut("doctor/{id}")]
        public IActionResult UpdateDoctor(int id, [FromBody] DoctorModel doctor)
        {
            try
            {
                if (doctor == null)
                {
                    return BadRequest(new { success = false, message = "Данные врача не предоставлены" });
                }

                if (string.IsNullOrEmpty(doctor.FullName) || string.IsNullOrEmpty(doctor.Specialty))
                {
                    return BadRequest(new { success = false, message = "ФИО и специальность врача обязательны" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        UPDATE Doctors
                        SET FullName = @FullName,
                            Specialty = @Specialty,
                            GeneralName = @GeneralName,
                            OfficeNumber = @OfficeNumber,
                            WorkExperience = @WorkExperience";

                    // Add password update only if a new password is provided
                    if (!string.IsNullOrEmpty(doctor.Password))
                    {
                        query += ", Password = @Password";
                    }
                
                    query += " WHERE DoctorID = @DoctorID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", id);
                        cmd.Parameters.AddWithValue("@FullName", doctor.FullName);
                        cmd.Parameters.AddWithValue("@Specialty", doctor.Specialty);
                        cmd.Parameters.AddWithValue("@GeneralName", string.IsNullOrEmpty(doctor.GeneralName) ? DBNull.Value : (object)doctor.GeneralName);
                        cmd.Parameters.AddWithValue("@OfficeNumber", doctor.OfficeNumber);
                        cmd.Parameters.AddWithValue("@WorkExperience", doctor.WorkExperience);
                        
                        if (!string.IsNullOrEmpty(doctor.Password))
                        {
                            cmd.Parameters.AddWithValue("@Password", doctor.Password);
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Врач не найден" });
                        }

                        return Ok(new { success = true });
                    }
                }
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
                    
                    // Проверяем, есть ли назначения этого врача пациентам
                    string checkQuery = @"
                        SELECT COUNT(*) FROM PatientDoctorAssignments 
                        WHERE DoctorID = @DoctorID";
                    
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@DoctorID", id);
                        int assignmentsCount = (int)checkCmd.ExecuteScalar();
                        
                        if (assignmentsCount > 0)
                        {
                            // Есть пациенты с назначениями к этому врачу, удаляем сначала их
                            string deleteAssignmentsQuery = @"
                                DELETE FROM PatientDoctorAssignments 
                                WHERE DoctorID = @DoctorID";
                            
                            using (SqlCommand deleteAssignmentsCmd = new SqlCommand(deleteAssignmentsQuery, con))
                            {
                                deleteAssignmentsCmd.Parameters.AddWithValue("@DoctorID", id);
                                deleteAssignmentsCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    
                    // Проверяем, есть ли процедуры у врача
                    string checkProceduresQuery = @"
                        SELECT COUNT(*) FROM Procedures 
                        WHERE DoctorID = @DoctorID";
                    
                    using (SqlCommand checkProcCmd = new SqlCommand(checkProceduresQuery, con))
                    {
                        checkProcCmd.Parameters.AddWithValue("@DoctorID", id);
                        int proceduresCount = (int)checkProcCmd.ExecuteScalar();
                        
                        if (proceduresCount > 0)
                        {
                            // Есть процедуры у этого врача, нужно проверить нет ли назначений этих процедур
                            string checkAppointmentsQuery = @"
                                SELECT COUNT(*) FROM ProcedureAppointments pa
                                JOIN Procedures p ON pa.ProcedureID = p.ProcedureID
                                WHERE p.DoctorID = @DoctorID";
                            
                            using (SqlCommand checkAppCmd = new SqlCommand(checkAppointmentsQuery, con))
                            {
                                checkAppCmd.Parameters.AddWithValue("@DoctorID", id);
                                int appointmentsCount = (int)checkAppCmd.ExecuteScalar();
                                
                                if (appointmentsCount > 0)
                                {
                                    // Есть назначения процедур этого врача, удаляем сначала их
                                    string deleteAppointmentsQuery = @"
                                        DELETE FROM ProcedureAppointments
                                        WHERE ProcedureID IN (SELECT ProcedureID FROM Procedures WHERE DoctorID = @DoctorID)";
                                    
                                    using (SqlCommand deleteAppCmd = new SqlCommand(deleteAppointmentsQuery, con))
                                    {
                                        deleteAppCmd.Parameters.AddWithValue("@DoctorID", id);
                                        deleteAppCmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            
                            // Удаляем процедуры врача
                            string deleteProceduresQuery = @"
                                DELETE FROM Procedures 
                                WHERE DoctorID = @DoctorID";
                            
                            using (SqlCommand deleteProcCmd = new SqlCommand(deleteProceduresQuery, con))
                            {
                                deleteProcCmd.Parameters.AddWithValue("@DoctorID", id);
                                deleteProcCmd.ExecuteNonQuery();
                            }
                        }
                    }
                    
                    // Теперь удаляем самого врача
                    string deleteQuery = "DELETE FROM Doctors WHERE DoctorID = @DoctorID";
                    
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DoctorID", id);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Врач не найден" });
                        }
                        
                        return Ok(new { success = true, message = "Врач успешно удален" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/chief/patient/{id}/doctors
        [HttpGet("patient/{id}/doctors")]
        public IActionResult GetPatientDoctors(int id)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT d.DoctorID, d.FullName, d.Specialty 
                        FROM Doctors d
                        JOIN PatientDoctorAssignments pda ON d.DoctorID = pda.DoctorID
                        WHERE pda.PatientID = @PatientID";
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
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

        // POST: api/chief/assignments
        [HttpPost("assignments")]
        public IActionResult AssignDoctorsToPatient([FromBody] MultiAssignmentModel assignment)
        {
            try
            {
                if (assignment == null)
                {
                    return BadRequest(new { success = false, message = "Не получены данные назначения" });
                }
                
                if (assignment.PatientId <= 0)
                {
                    return BadRequest(new { success = false, message = "Неверный ID пациента" });
                }
                
                if (assignment.DoctorIds == null)
                {
                    return BadRequest(new { success = false, message = "Не указан список врачей" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // Проверяем существование пациента
                    string checkQuery = "SELECT COUNT(*) FROM Patients WHERE PatientID = @PatientID";
                    
                    using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", assignment.PatientId);
                        int patientCount = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        if (patientCount == 0)
                        {
                            return NotFound(new { success = false, message = "Пациент не найден" });
                        }
                    }
                    
                    // Проверяем существование всех выбранных врачей
                    if (assignment.DoctorIds.Count > 0)
                    {
                        foreach (int doctorId in assignment.DoctorIds)
                        {
                            string checkDoctorQuery = "SELECT COUNT(*) FROM Doctors WHERE DoctorID = @DoctorID";
                            using (SqlCommand cmd = new SqlCommand(checkDoctorQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@DoctorID", doctorId);
                                int doctorCount = Convert.ToInt32(cmd.ExecuteScalar());
                                
                                if (doctorCount == 0)
                                {
                                    return NotFound(new { success = false, message = $"Врач с ID {doctorId} не найден" });
                                }
                            }
                        }
                    }
                    
                    // Начинаем транзакцию для операции обновления
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Удаляем все текущие назначения для пациента
                            string deleteQuery = "DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                            
                            using (SqlCommand cmd = new SqlCommand(deleteQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", assignment.PatientId);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Создаем новые назначения
                            if (assignment.DoctorIds.Count > 0)
                            {
                                string insertQuery = @"
                                    INSERT INTO PatientDoctorAssignments (PatientID, DoctorID)
                                    VALUES (@PatientID, @DoctorID)";
                                
                                foreach (int doctorId in assignment.DoctorIds)
                                {
                                    using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@PatientID", assignment.PatientId);
                                        cmd.Parameters.AddWithValue("@DoctorID", doctorId);
                                cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            
                            transaction.Commit();
                            return Ok(new { success = true, message = $"Назначено врачей: {assignment.DoctorIds.Count}" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(500, new { success = false, message = $"Ошибка при обновлении назначений: {ex.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Ошибка: {ex.Message}" });
            }
        }

        // POST: api/chief/assignment
        [HttpPost("assignment")]
        public IActionResult AssignDoctorToPatient([FromBody] AssignmentModel assignment)
        {
            try
            {
                if (assignment == null || assignment.PatientId <= 0 || assignment.DoctorId <= 0)
                {
                    return BadRequest(new { success = false, message = "Некорректные данные для назначения" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // Проверяем существование пациента и врача
                    string checkQuery = @"
                        SELECT 
                            (SELECT COUNT(*) FROM Patients WHERE PatientID = @PatientID) AS PatientExists,
                            (SELECT COUNT(*) FROM Doctors WHERE DoctorID = @DoctorID) AS DoctorExists";
                    
                    bool patientExists = false;
                    bool doctorExists = false;
                    
                    using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", assignment.PatientId);
                        cmd.Parameters.AddWithValue("@DoctorID", assignment.DoctorId);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                patientExists = Convert.ToInt32(reader["PatientExists"]) > 0;
                                doctorExists = Convert.ToInt32(reader["DoctorExists"]) > 0;
                            }
                        }
                    }
                    
                    if (!patientExists)
                    {
                        return NotFound(new { success = false, message = "Пациент не найден" });
                    }
                    
                    if (!doctorExists)
                    {
                        return NotFound(new { success = false, message = "Врач не найден" });
                    }
                    
                    // Проверяем, существует ли уже такое назначение
                    string checkAssignmentQuery = @"
                        SELECT COUNT(*) FROM PatientDoctorAssignments 
                        WHERE PatientID = @PatientID AND DoctorID = @DoctorID";
                    
                    bool assignmentExists = false;
                    
                    using (SqlCommand cmd = new SqlCommand(checkAssignmentQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", assignment.PatientId);
                        cmd.Parameters.AddWithValue("@DoctorID", assignment.DoctorId);
                        
                        assignmentExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }
                    
                    if (assignmentExists)
                    {
                        return Ok(new { success = true, message = "Назначение уже существует" });
                    }
                    
                    // Создаем новое назначение
                    string insertQuery = @"
                        INSERT INTO PatientDoctorAssignments (PatientID, DoctorID)
                        VALUES (@PatientID, @DoctorID)";
                    
                    using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", assignment.PatientId);
                        cmd.Parameters.AddWithValue("@DoctorID", assignment.DoctorId);
                        
                        cmd.ExecuteNonQuery();
                    }
                    
                return Ok(new { success = true });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/chief/patient/{id}/discharge
        [HttpGet("patient/{id}/discharge")]
        public IActionResult GetPatientDischargeDocument(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT 
                            DischargeID,
                            Complaints,
                            DiseaseHistory,
                            InitialCondition,
                            RehabilitationGoal,
                            GoalAchieved,
                            Recommendations,
                            DischargeDate,
                            CreatedDate
                        FROM DischargeDocuments
                        WHERE PatientID = @PatientID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var document = new
                                {
                                    DischargeId = Convert.ToInt32(reader["DischargeID"]),
                                    Complaints = reader["Complaints"] != DBNull.Value ? reader["Complaints"].ToString() : null,
                                    DiseaseHistory = reader["DiseaseHistory"] != DBNull.Value ? reader["DiseaseHistory"].ToString() : null,
                                    InitialCondition = reader["InitialCondition"] != DBNull.Value ? reader["InitialCondition"].ToString() : null,
                                    RehabilitationGoal = reader["RehabilitationGoal"] != DBNull.Value ? reader["RehabilitationGoal"].ToString() : null,
                                    GoalAchieved = reader["GoalAchieved"] != DBNull.Value && Convert.ToBoolean(reader["GoalAchieved"]),
                                    Recommendations = reader["Recommendations"] != DBNull.Value ? reader["Recommendations"].ToString() : null,
                                    DischargeDate = reader["DischargeDate"] != DBNull.Value ? Convert.ToDateTime(reader["DischargeDate"]) : (DateTime?)null,
                                    CreatedDate = Convert.ToDateTime(reader["CreatedDate"])
                                };

                                return Ok(document);
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Выписной эпикриз не найден" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении выписного эпикриза: " + ex.Message });
            }
        }

        // POST: api/chief/discharge
        [HttpPost("discharge")]
        public IActionResult CreateDischargeDocument([FromBody] DischargeModel discharge)
                    {
                        try
                        {
                if (discharge == null || discharge.PatientId <= 0)
                {
                    return BadRequest(new { success = false, message = "Некорректные данные для выписного эпикриза" });
                }

                if (string.IsNullOrEmpty(discharge.Complaints) || string.IsNullOrEmpty(discharge.DiseaseHistory) ||
                    string.IsNullOrEmpty(discharge.InitialCondition) || string.IsNullOrEmpty(discharge.RehabilitationGoal) ||
                    string.IsNullOrEmpty(discharge.Recommendations) || string.IsNullOrEmpty(discharge.DischargeDate))
                {
                    return BadRequest(new { success = false, message = "Все поля выписного эпикриза обязательны" });
                }

                // Parse discharge date with proper error handling
                DateTime dischargeDate;
                if (!DateTime.TryParse(discharge.DischargeDate, out dischargeDate))
                {
                    return BadRequest(new { success = false, message = "Некорректный формат даты выписки" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Проверяем существование пациента
                            string checkQuery = "SELECT COUNT(*) FROM Patients WHERE PatientID = @PatientID";
                            using (SqlCommand cmd = new SqlCommand(checkQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", discharge.PatientId);
                                int patientCount = Convert.ToInt32(cmd.ExecuteScalar());
                                if (patientCount == 0)
                                {
                                    transaction.Rollback();
                                    return NotFound(new { success = false, message = "Пациент не найден" });
                                }
                            }

                            // Проверяем существует ли уже эпикриз
                            string checkDocumentQuery = "SELECT COUNT(*) FROM DischargeDocuments WHERE PatientID = @PatientID";
                            bool documentExists = false;
                            using (SqlCommand cmd = new SqlCommand(checkDocumentQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", discharge.PatientId);
                                documentExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                            }

                            string query;
                            if (documentExists)
                            {
                                // Обновляем существующий документ
                                query = @"
                                    UPDATE DischargeDocuments
                                    SET Complaints = @Complaints,
                                        DiseaseHistory = @DiseaseHistory,
                                        InitialCondition = @InitialCondition,
                                        RehabilitationGoal = @RehabilitationGoal,
                                        GoalAchieved = @GoalAchieved,
                                        Recommendations = @Recommendations,
                                        DischargeDate = @DischargeDate,
                                        LastUpdated = GETDATE()
                                    WHERE PatientID = @PatientID;
                                    SELECT DischargeID FROM DischargeDocuments WHERE PatientID = @PatientID;";
                            }
                            else
                            {
                                // Создаем новый документ
                                query = @"
                                    INSERT INTO DischargeDocuments (
                                        PatientID, Complaints, DiseaseHistory, InitialCondition, RehabilitationGoal, 
                                        GoalAchieved, Recommendations, DischargeDate, CreatedDate, LastUpdated
                                    ) VALUES (
                                        @PatientID, @Complaints, @DiseaseHistory, @InitialCondition, @RehabilitationGoal, 
                                        @GoalAchieved, @Recommendations, @DischargeDate, GETDATE(), GETDATE()
                                    );
                                    SELECT SCOPE_IDENTITY();";
                            }

                            int documentId;
                            using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", discharge.PatientId);
                                cmd.Parameters.AddWithValue("@Complaints", discharge.Complaints);
                                cmd.Parameters.AddWithValue("@DiseaseHistory", discharge.DiseaseHistory);
                                cmd.Parameters.AddWithValue("@InitialCondition", discharge.InitialCondition);
                                cmd.Parameters.AddWithValue("@RehabilitationGoal", discharge.RehabilitationGoal);
                                cmd.Parameters.AddWithValue("@GoalAchieved", discharge.GoalAchieved);
                                cmd.Parameters.AddWithValue("@Recommendations", discharge.Recommendations);
                                cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate);

                                documentId = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            // Обновляем дату выписки пациента, если указано
                            if (discharge.SetDischargeDate)
                            {
                                string updatePatientQuery = @"
                                    UPDATE Patients
                                    SET DischargeDate = @DischargeDate
                                    WHERE PatientID = @PatientID";

                                using (SqlCommand cmd = new SqlCommand(updatePatientQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", discharge.PatientId);
                                    cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate);
                                cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            return Ok(new { success = true, documentId });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(500, new { success = false, message = "Ошибка при создании выписного эпикриза: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при создании выписного эпикриза: " + ex.Message });
            }
        }

        // POST: api/chief/discharge/save
        [HttpPost("discharge/save")]
        public IActionResult SaveDischargeDocument([FromBody] DischargeModel discharge)
        {
            try
            {
                if (discharge == null || discharge.PatientId <= 0)
                {
                    return BadRequest(new { success = false, message = "Некорректные данные для выписного эпикриза" });
                }

                if (string.IsNullOrEmpty(discharge.Complaints) || string.IsNullOrEmpty(discharge.DiseaseHistory) ||
                    string.IsNullOrEmpty(discharge.InitialCondition) || string.IsNullOrEmpty(discharge.RehabilitationGoal) ||
                    string.IsNullOrEmpty(discharge.Recommendations) || string.IsNullOrEmpty(discharge.DischargeDate))
                {
                    return BadRequest(new { success = false, message = "Все поля выписного эпикриза обязательны" });
                }

                // Parse discharge date with proper error handling
                DateTime dischargeDate;
                if (!DateTime.TryParse(discharge.DischargeDate, out dischargeDate))
                {
                    return BadRequest(new { success = false, message = "Некорректный формат даты выписки" });
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    // Проверяем существование пациента
                    string checkQuery = "SELECT COUNT(*) FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", discharge.PatientId);
                        int patientCount = Convert.ToInt32(cmd.ExecuteScalar());
                        if (patientCount == 0)
                        {
                            return NotFound(new { success = false, message = "Пациент не найден" });
                        }
                    }

                    // Проверяем существует ли уже эпикриз
                    string checkDocumentQuery = "SELECT COUNT(*) FROM DischargeDocuments WHERE PatientID = @PatientID";
                    bool documentExists = false;
                    using (SqlCommand cmd = new SqlCommand(checkDocumentQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", discharge.PatientId);
                        documentExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                    }

                    string query;
                    if (documentExists)
                    {
                        // Обновляем существующий документ
                        query = @"
                            UPDATE DischargeDocuments
                            SET Complaints = @Complaints,
                                DiseaseHistory = @DiseaseHistory,
                                InitialCondition = @InitialCondition,
                                RehabilitationGoal = @RehabilitationGoal,
                                GoalAchieved = @GoalAchieved,
                                Recommendations = @Recommendations,
                                DischargeDate = @DischargeDate,
                                LastUpdated = GETDATE()
                            WHERE PatientID = @PatientID;
                            SELECT DischargeID FROM DischargeDocuments WHERE PatientID = @PatientID;";
                    }
                    else
                    {
                        // Создаем новый документ
                        query = @"
                            INSERT INTO DischargeDocuments (
                                PatientID, Complaints, DiseaseHistory, InitialCondition, RehabilitationGoal, 
                                GoalAchieved, Recommendations, DischargeDate, CreatedDate, LastUpdated
                            ) VALUES (
                                @PatientID, @Complaints, @DiseaseHistory, @InitialCondition, @RehabilitationGoal, 
                                @GoalAchieved, @Recommendations, @DischargeDate, GETDATE(), GETDATE()
                            );
                            SELECT SCOPE_IDENTITY();";
                    }

                    int documentId;
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", discharge.PatientId);
                        cmd.Parameters.AddWithValue("@Complaints", discharge.Complaints);
                        cmd.Parameters.AddWithValue("@DiseaseHistory", discharge.DiseaseHistory);
                        cmd.Parameters.AddWithValue("@InitialCondition", discharge.InitialCondition);
                        cmd.Parameters.AddWithValue("@RehabilitationGoal", discharge.RehabilitationGoal);
                        cmd.Parameters.AddWithValue("@GoalAchieved", discharge.GoalAchieved);
                        cmd.Parameters.AddWithValue("@Recommendations", discharge.Recommendations);
                        cmd.Parameters.AddWithValue("@DischargeDate", dischargeDate);

                        documentId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    return Ok(new { success = true, documentId });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при сохранении выписного эпикриза: " + ex.Message });
            }
        }

        // GET: api/chief/discharge/{id}/print
        [HttpGet("discharge/{id}/print")]
        public IActionResult PrintDischargeDocument(int id)
        {
            try
            {
                int patientId = 0;
                string fullName = "";
                DateTime dateOfBirth = DateTime.MinValue;
                DateTime recordDate = DateTime.MinValue;
                DateTime? dischargeDate = null;
                string complaints = "";
                string diseaseHistory = "";
                string initialCondition = "";
                string rehabilitationGoal = "";
                bool goalAchieved = false;
                string recommendations = "";
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // 1. Получаем основные данные выписного эпикриза
                    string query = @"
                        SELECT 
                            d.PatientID,
                            p.FullName,
                            p.DateOfBirth,
                            p.RecordDate,
                            p.DischargeDate,
                            d.Complaints,
                            d.DiseaseHistory,
                            d.InitialCondition,
                            d.RehabilitationGoal,
                            d.GoalAchieved,
                            d.Recommendations
                        FROM DischargeDocuments d
                        JOIN Patients p ON d.PatientID = p.PatientID
                        WHERE d.DischargeID = @DischargeID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DischargeID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                patientId = reader["PatientID"] != DBNull.Value ? Convert.ToInt32(reader["PatientID"]) : 0;
                                fullName = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : "Нет данных";
                                dateOfBirth = reader["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfBirth"]) : DateTime.MinValue;
                                recordDate = reader["RecordDate"] != DBNull.Value ? Convert.ToDateTime(reader["RecordDate"]) : DateTime.MinValue;
                                dischargeDate = reader["DischargeDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DischargeDate"]) : null;
                                
                                complaints = reader["Complaints"] != DBNull.Value ? reader["Complaints"].ToString() : "Нет данных";
                                diseaseHistory = reader["DiseaseHistory"] != DBNull.Value ? reader["DiseaseHistory"].ToString() : "Нет данных";
                                initialCondition = reader["InitialCondition"] != DBNull.Value ? reader["InitialCondition"].ToString() : "Нет данных";
                                rehabilitationGoal = reader["RehabilitationGoal"] != DBNull.Value ? reader["RehabilitationGoal"].ToString() : "Нет данных";
                                goalAchieved = reader["GoalAchieved"] != DBNull.Value && Convert.ToBoolean(reader["GoalAchieved"]);
                                recommendations = reader["Recommendations"] != DBNull.Value ? reader["Recommendations"].ToString() : "Нет данных";
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Выписной эпикриз не найден" });
                            }
                        }
                    }
                    
                    // Генерируем HTML для печати
                    List<string> diagnoses = new List<string>();
                    List<string> doctors = new List<string>();
                    
                    // 2. Получаем диагнозы
                    string diagnosesQuery = @"
                        SELECT d.DiagnosisName
                        FROM PatientDiagnoses pd
                        JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                        WHERE pd.PatientID = @PatientID";
                    
                    using (SqlCommand cmd = new SqlCommand(diagnosesQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string diagnosisName = reader["DiagnosisName"] != DBNull.Value ? reader["DiagnosisName"].ToString() : "Не указан";
                                diagnoses.Add(diagnosisName);
                            }
                        }
                    }
                    
                    // 3. Получаем врачей напрямую из представления с проверкой каждого поля
                    string doctorsQuery = @"
                        SELECT d.FullName, d.Specialty
                        FROM Doctors d
                        INNER JOIN PatientDoctorAssignments pda ON d.DoctorID = pda.DoctorID
                        WHERE pda.PatientID = @PatientID
                        ORDER BY d.FullName";
                    
                    using (SqlCommand cmd = new SqlCommand(doctorsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string doctorName = "Не указано";
                                string specialty = "Не указана специальность";
                                
                                if (reader["FullName"] != DBNull.Value && reader["FullName"] != null)
                                {
                                    doctorName = reader["FullName"].ToString();
                                }
                                
                                if (reader["Specialty"] != DBNull.Value && reader["Specialty"] != null)
                                {
                                    specialty = reader["Specialty"].ToString();
                                }
                                
                                doctors.Add($"{doctorName} ({specialty})");
                            }
                        }
                    }
                    
                    // 4. Формируем HTML документ
                    string html = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset=""UTF-8"">
                            <title>Выписной эпикриз - {fullName}</title>
                            <style>
                                body {{
                                    font-family: Arial, sans-serif;
                                    margin: 40px;
                                    line-height: 1.6;
                                    color: #333;
                                }}
                                h1, h2 {{
                                    text-align: center;
                                    color: #2c3e50;
                                }}
                                .section {{
                                    margin-bottom: 20px;
                                }}
                                .label {{
                                    font-weight: bold;
                                    margin-right: 10px;
                                }}
                                .patient-info {{
                                    margin-bottom: 30px;
                                }}
                                .doctors-list {{
                                    margin-left: 20px;
                                }}
                                .footer {{
                                    margin-top: 50px;
                                    text-align: right;
                                }}
                                @media print {{
                                    body {{
                                        margin: 0.5cm;
                                    }}
                                    .no-print {{
                                        display: none;
                                    }}
                                }}
                            </style>
                        </head>
                        <body>
                            <div class=""no-print"" style=""text-align: center; margin-bottom: 20px;"">
                                <button onclick=""window.print()"" style=""padding: 10px 20px; font-size: 16px; cursor: pointer;"">Печать</button>
                            </div>
                            
                            <h1>ВЫПИСНОЙ ЭПИКРИЗ</h1>
                            
                            <div class=""patient-info"">
                                <p><span class=""label"">ФИО:</span> {fullName}, {dateOfBirth:dd.MM.yyyy}</p>
                                <p><span class=""label"">Находился на лечении в отделении реабилитации с:</span> {recordDate:dd.MM.yyyy} {(dischargeDate.HasValue ? $"по {dischargeDate.Value:dd.MM.yyyy}" : "")}</p>
                                <p><span class=""label"">С:</span> {(diagnoses.Count > 0 ? string.Join(", ", diagnoses) : "Нет установленных диагнозов")}</p>
                            </div>
                            
                            <div class=""section"">
                                <p><span class=""label"">Поступил с жалобами на:</span></p>
                                <p>{complaints}</p>
                            </div>
                            
                            <div class=""section"">
                                <p><span class=""label"">Из анамнеза заболевания:</span></p>
                                <p>{diseaseHistory}</p>
                            </div>
                            
                            <div class=""section"">
                                <p><span class=""label"">Общее состояние при поступлении:</span></p>
                                <p>{initialCondition}</p>
                            </div>
                            
                            <div class=""section"">
                                <p><span class=""label"">В составе мультидисциплинарной команды консультирован специалистами:</span></p>
                                <div class=""doctors-list"">
                                    {(doctors.Count > 0 ? string.Join("<br>", doctors) : "Не назначены")}
                                </div>
                            </div>
                            
                            <div class=""section"">
                                <p><span class=""label"">Цель реабилитации:</span></p>
                                <p>{rehabilitationGoal}</p>
                            </div>
                            
                            <div class=""section"">
                                <p><span class=""label"">{(goalAchieved ? "Цель реабилитации достигнута. Лечение закончено." : "Цель реабилитации не достигнута.")}</span></p>
                            </div>
                            
                            <div class=""section"">
                                <p><span class=""label"">Рекомендации:</span></p>
                                <p>{recommendations}</p>
                            </div>
                            
                            <div class=""footer"">
                                <p>Главный врач</p>
                                <p style=""margin-top: 30px;"">М.П.</p>
                            </div>
                        </body>
                        </html>
                    ";
                    
                    return Content(html, "text/html");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при печати выписного эпикриза: " + ex.Message });
            }
        }

        // GET: api/chief/discharge/{id}/download
        [HttpGet("discharge/{id}/download")]
        public IActionResult DownloadDischargeDocument(int id)
        {
            try
            {
                int patientId = 0;
                string fullName = "";
                DateTime dateOfBirth = DateTime.MinValue;
                DateTime recordDate = DateTime.MinValue;
                DateTime? dischargeDate = null;
                string complaints = "";
                string diseaseHistory = "";
                string initialCondition = "";
                string rehabilitationGoal = "";
                bool goalAchieved = false;
                string recommendations = "";
                string chiefDoctorName = "";
                List<string> doctorsList = new List<string>();
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // 1. Получаем основные данные выписного эпикриза
                    string query = @"
                        SELECT 
                            d.PatientID,
                            p.FullName,
                            p.DateOfBirth,
                            p.RecordDate,
                            p.DischargeDate,
                            d.Complaints,
                            d.DiseaseHistory,
                            d.InitialCondition,
                            d.RehabilitationGoal,
                            d.GoalAchieved,
                            d.Recommendations
                        FROM DischargeDocuments d
                        JOIN Patients p ON d.PatientID = p.PatientID
                        WHERE d.DischargeID = @DischargeID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DischargeID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                patientId = reader["PatientID"] != DBNull.Value ? Convert.ToInt32(reader["PatientID"]) : 0;
                                fullName = reader["FullName"] != DBNull.Value ? reader["FullName"].ToString() : "Нет данных";
                                dateOfBirth = reader["DateOfBirth"] != DBNull.Value ? Convert.ToDateTime(reader["DateOfBirth"]) : DateTime.MinValue;
                                recordDate = reader["RecordDate"] != DBNull.Value ? Convert.ToDateTime(reader["RecordDate"]) : DateTime.MinValue;
                                dischargeDate = reader["DischargeDate"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(reader["DischargeDate"]) : null;
                                
                                complaints = reader["Complaints"] != DBNull.Value ? reader["Complaints"].ToString() : "Нет данных";
                                diseaseHistory = reader["DiseaseHistory"] != DBNull.Value ? reader["DiseaseHistory"].ToString() : "Нет данных";
                                initialCondition = reader["InitialCondition"] != DBNull.Value ? reader["InitialCondition"].ToString() : "Нет данных";
                                rehabilitationGoal = reader["RehabilitationGoal"] != DBNull.Value ? reader["RehabilitationGoal"].ToString() : "Нет данных";
                                goalAchieved = reader["GoalAchieved"] != DBNull.Value && Convert.ToBoolean(reader["GoalAchieved"]);
                                recommendations = reader["Recommendations"] != DBNull.Value ? reader["Recommendations"].ToString() : "Нет данных";
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Выписной эпикриз не найден" });
                            }
                        }
                    }
                    
                    // 2. Получаем диагнозы
                    string diagnosesQuery = @"
                        SELECT d.DiagnosisName
                        FROM PatientDiagnoses pd
                        JOIN Diagnoses d ON pd.DiagnosisID = d.DiagnosisID
                        WHERE pd.PatientID = @PatientID";
                    
                    List<string> diagnoses = new List<string>();
                    
                    using (SqlCommand cmd = new SqlCommand(diagnosesQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string diagnosisName = reader["DiagnosisName"] != DBNull.Value ? reader["DiagnosisName"].ToString() : "Не указан";
                                diagnoses.Add(diagnosisName);
                            }
                        }
                    }
                    
                    // Получение основной информации о главвраче
                    string chiefQuery = "SELECT TOP 1 FullName FROM ChiefDoctors";
                    using (SqlCommand cmd = new SqlCommand(chiefQuery, con))
                    {
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            chiefDoctorName = result.ToString();
                        }
                        else
                        {
                            chiefDoctorName = "Главный врач";
                        }
                    }
                    
                    // 3. Получаем врачей для списка
                    string doctorsQuery = @"
                        SELECT d.FullName, d.Specialty
                        FROM Doctors d
                        INNER JOIN PatientDoctorAssignments pda ON d.DoctorID = pda.DoctorID
                        WHERE pda.PatientID = @PatientID
                        ORDER BY d.FullName";
                    
                    using (SqlCommand cmd = new SqlCommand(doctorsQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", patientId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string doctorName = "Не указано";
                                string specialty = "Не указана специальность";
                                
                                if (reader["FullName"] != DBNull.Value && reader["FullName"] != null)
                                {
                                    doctorName = reader["FullName"].ToString();
                                }
                                
                                if (reader["Specialty"] != DBNull.Value && reader["Specialty"] != null)
                                {
                                    specialty = reader["Specialty"].ToString();
                                }
                                
                                doctorsList.Add($"{doctorName} ({specialty})");
                            }
                        }
                    }
                    
                    // 4. Создаем RTF документ
                    StringBuilder rtf = new StringBuilder();
                    
                    // RTF заголовок
                    rtf.Append(@"{\rtf1\ansi\ansicpg1251\deff0\deflang1049");
                    
                    // Определяем шрифты
                    rtf.Append(@"{\fonttbl{\f0\froman\fprq2\fcharset204 Times New Roman;}}");
                    
                    // Начало документа - убираем \sa200 (отступ снизу) и уменьшаем размер заголовка до 28 (14pt)
                    rtf.Append(@"\viewkind4\uc1\pard\sl276\slmult1\qc\b\f0\fs28 ВЫПИСНОЙ ЭПИКРИЗ\par");
                    
                    // Добавляем пустую строку после заголовка
                    rtf.Append(@"\par");
                    
                    // Основной текст - размер 24 (12pt)
                    // Информация о пациенте (жирный шрифт, выравнивание по левому краю)
                    rtf.Append(@"\ql\b\fs24 ");
                    rtf.Append(EscapeRtf($"{fullName}, {dateOfBirth:dd.MM.yyyy}"));
                    rtf.Append(@"\par");
                    
                    // Сведения о пребывании (обычный шрифт)
                    rtf.Append(@"\b0 ");
                    rtf.Append(EscapeRtf($"Находился на лечении в отделении реабилитации с {recordDate:dd.MM.yyyy} {(dischargeDate.HasValue ? $"по {dischargeDate.Value:dd.MM.yyyy}" : "")}"));
                    rtf.Append(@"\par");
                    
                    // Диагнозы
                    rtf.Append(EscapeRtf($"с: {(diagnoses.Count > 0 ? string.Join(", ", diagnoses) : "Нет установленных диагнозов")}"));
                    rtf.Append(@"\par");
                    
                    // Жалобы
                    rtf.Append(@"\b Поступил с жалобами на:\b0\par ");
                    rtf.Append(EscapeRtf(complaints));
                    rtf.Append(@"\par");
                    
                    // Анамнез
                    rtf.Append(@"\b Из анамнеза заболевания:\b0\par ");
                    rtf.Append(EscapeRtf(diseaseHistory));
                    rtf.Append(@"\par");
                    
                    // Состояние при поступлении
                    rtf.Append(@"\b Общее состояние при поступлении:\b0\par ");
                    rtf.Append(EscapeRtf(initialCondition));
                    rtf.Append(@"\par");

                    // Врачи
                    rtf.Append(@"\b В составе мультидисциплинарной команды консультирован специалистами:\b0\par");
                    if (doctorsList.Count > 0)
                    {
                        foreach (string doctor in doctorsList)
                        {
                            rtf.Append(@"\li300 "); // Левый отступ 300 твипов
                            rtf.Append(EscapeRtf(doctor));
                            rtf.Append(@"\par");
                        }
                        // Сброс отступа после списка
                        rtf.Append(@"\li0 ");
                    }
                    else
                    {
                        rtf.Append(@"\li300 Не назначены\par");
                        rtf.Append(@"\li0 ");
                    }

                    // После списка специалистов добавляем:
                    rtf.Append(@"\pard"); // Сбрасываем все форматирования абзаца

                    // Затем продолжаем с обычным форматированием:
                    rtf.Append(@"\ql\b Цель реабилитации:\b0\par ");
                    rtf.Append(EscapeRtf(rehabilitationGoal));
                    rtf.Append(@"\par");
                    
                    // Достижение цели
                    string goalAchievedText = goalAchieved 
                        ? "Цель реабилитации достигнута. Лечение закончено"
                        : "Цель реабилитации не достигнута";
                    rtf.Append(@"\b ");
                    rtf.Append(EscapeRtf(goalAchievedText));
                    rtf.Append(@"\b0\par");
                    
                    // Рекомендации
                    rtf.Append(@"\b Рекомендации:\b0\par ");
                    rtf.Append(EscapeRtf(recommendations));
                    rtf.Append(@"\par");
                    
                    // Пустая строка перед подписью
                    rtf.Append(@"\par");
                    
                    // Подпись (выравнивание по правому краю, жирный шрифт)
                    rtf.Append(@"\qr\b ");
                    rtf.Append(EscapeRtf(chiefDoctorName));
                    rtf.Append(@"\par");
                    
                    // Дополнительная пустая строка после ФИО главврача
                    rtf.Append(@"\par");
                    
                    // М.П. (курсив)
                    rtf.Append(@"\i М.П.\i0\par");
                    
                    // Закрытие RTF документа
                    rtf.Append("}");
                    
                    // Формируем имя файла для скачивания
                    string fileName = $"Выписной_эпикриз_{fullName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.rtf";
                    
                    // Возвращаем RTF файл клиенту для скачивания
                    return File(System.Text.Encoding.GetEncoding(1251).GetBytes(rtf.ToString()), "application/rtf", fileName);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Ошибка при создании RTF документа: " + ex.Message });
            }
        }

        // Вспомогательный метод для экранирования символов RTF
        private string EscapeRtf(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            
            return text
                .Replace("\\", "\\\\")
                .Replace("{", "\\{")
                .Replace("}", "\\}")
                .Replace("\r\n", "\\par ")
                .Replace("\n", "\\par ")
                .Replace("\r", "\\par ");
        }

        // GET: api/chief/reports/patients
        [HttpGet("reports/patients")]
        public IActionResult GetPatientReport([FromQuery] string startDate, [FromQuery] string endDate)
        {
            try
            {
                if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
                {
                    return BadRequest(new { success = false, message = "Необходимо указать начальную и конечную даты" });
                }

                DateTime start = DateTime.Parse(startDate);
                DateTime end = DateTime.Parse(endDate);

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    int totalPatients = 0;
                    int newPatients = 0;
                    int dischargedPatients = 0;
                    
                    // Общее количество пациентов
                    string totalQuery = "SELECT COUNT(*) FROM Patients";
                    using (SqlCommand cmd = new SqlCommand(totalQuery, con))
                    {
                        totalPatients = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Количество новых пациентов за период
                    string newQuery = @"
                        SELECT COUNT(*) FROM Patients 
                        WHERE RecordDate BETWEEN @StartDate AND @EndDate";
                    using (SqlCommand cmd = new SqlCommand(newQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        cmd.Parameters.AddWithValue("@EndDate", end);
                        newPatients = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    // Количество выписанных пациентов за период
                    string dischargedQuery = @"
                        SELECT COUNT(*) FROM Patients 
                        WHERE DischargeDate BETWEEN @StartDate AND @EndDate";
                    using (SqlCommand cmd = new SqlCommand(dischargedQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", start);
                        cmd.Parameters.AddWithValue("@EndDate", end);
                        dischargedPatients = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    
                    var report = new
                    {
                        totalPatients,
                        newPatients,
                        dischargedPatients,
                        periodStart = start.ToString("yyyy-MM-dd"),
                        periodEnd = end.ToString("yyyy-MM-dd")
                    };
                    
                    return Ok(new { success = true, report });
                }
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

        // GET: api/chief/test
        [HttpGet("test")]
        public IActionResult TestEndpoint()
        {
            return Ok(new { success = true, message = "API endpoint is working!" });
        }
    }

    // Модели для запросов
    public class DoctorModel
    {
        public string FullName { get; set; }
        public string Specialty { get; set; }
        public string GeneralName { get; set; }
        public int OfficeNumber { get; set; }
        public int WorkExperience { get; set; }
        public string Password { get; set; }
    }

    public class AssignmentModel
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
    }

    public class DischargeModel
    {
        public int PatientId { get; set; }
        public string Complaints { get; set; }
        public string DiseaseHistory { get; set; }
        public string InitialCondition { get; set; }
        public string RehabilitationGoal { get; set; }
        public bool GoalAchieved { get; set; }
        public string Recommendations { get; set; }
        public string DischargeDate { get; set; }
        public bool SetDischargeDate { get; set; }
    }

    public class MultiAssignmentModel
    {
        public int PatientId { get; set; }
        public List<int> DoctorIds { get; set; }
    }

    // Модели данных для работы с санаторной картой
    public class PatientDiagnosisModel
    {
        public int PatientId { get; set; }
        public int DiagnosisId { get; set; }
        public string DiagnosisType { get; set; }
    }

    public class MeasurementModel
    {
        public string MeasurementType { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string BloodPressure { get; set; }
        public DateTime? MeasurementDate { get; set; }
    }

    public class PatientMeasurementsModel
    {
        public int PatientId { get; set; }
        public List<MeasurementModel> Measurements { get; set; }
    }

    public class PatientMedicationModel
    {
        public int MedicationId { get; set; }
        public int PatientId { get; set; }
        public string MedicationName { get; set; }
        public string Dosage { get; set; }
        public string Instructions { get; set; }
        public DateTime? PrescribedDate { get; set; }
    }
}