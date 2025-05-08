using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace WebDubRosh.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerController : ControllerBase
    {
        // Строка подключения с TrustServerCertificate=True для работы через localtunnel
        private readonly string _connectionString = "data source=localhost;initial catalog=PomoshnikPolicliniki2;integrated security=True;encrypt=False;MultipleActiveResultSets=True;App=EntityFramework;TrustServerCertificate=True";

        #region Пациенты

        // GET: api/manager/patients
        [HttpGet("patients")]
        public IActionResult GetPatients()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"SELECT p.PatientID, p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate,
                                   CASE 
                                       WHEN (SELECT COUNT(pd.DocumentID) FROM PatientDocuments pd 
                                            INNER JOIN DocumentTypes dt ON pd.DocumentTypeID = dt.DocumentTypeID 
                                            WHERE pd.PatientID = p.PatientID AND dt.IsRequired = 1) =
                                            (SELECT COUNT(DocumentTypeID) FROM DocumentTypes WHERE IsRequired = 1 AND 
                                            ((p.DateOfBirth IS NOT NULL AND DATEDIFF(YEAR, p.DateOfBirth, GETDATE()) BETWEEN MinimumAge AND MaximumAge) OR p.DateOfBirth IS NULL)
                                            AND ForAccompanyingPerson = 0)
                                       THEN 'Полный комплект'
                                       WHEN (SELECT COUNT(pd.DocumentID) FROM PatientDocuments pd 
                                            INNER JOIN DocumentTypes dt ON pd.DocumentTypeID = dt.DocumentTypeID 
                                            WHERE pd.PatientID = p.PatientID AND dt.IsRequired = 1) > 0
                                       THEN 'Частично'
                                       ELSE 'Отсутствуют'
                                   END AS DocumentsStatus
                               FROM Patients p";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/manager/patient/{id}
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
                            // 1. Проверяем, существуют ли зависимые записи
                            string checkQuery = @"SELECT COUNT(*) FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                            int dependentCount = 0;
                            
                            using (SqlCommand checkCmd = new SqlCommand(checkQuery, con, tran))
                            {
                                checkCmd.Parameters.AddWithValue("@PatientID", id);
                                dependentCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            }
                            
                            if (dependentCount > 0)
                            {
                                tran.Rollback();
                                return BadRequest(new { success = false, message = "Нельзя удалить пациента, так как у него есть назначенные врачи. Сначала отмените назначения." });
                            }

                            // 2. Удаляем документы пациента
                            string deleteDocumentsQuery = "DELETE FROM PatientDocuments WHERE PatientID = @PatientID";
                            using (SqlCommand cmdDocuments = new SqlCommand(deleteDocumentsQuery, con, tran))
                            {
                                cmdDocuments.Parameters.AddWithValue("@PatientID", id);
                                cmdDocuments.ExecuteNonQuery();
                            }

                            // 3. Удаляем описания пациента
                            string deleteDescriptionsQuery = "DELETE FROM PatientDescriptions WHERE PatientID = @PatientID";
                            using (SqlCommand cmdDescriptions = new SqlCommand(deleteDescriptionsQuery, con, tran))
                            {
                                cmdDescriptions.Parameters.AddWithValue("@PatientID", id);
                                cmdDescriptions.ExecuteNonQuery();
                            }

                            // 4. Удаляем диагнозы пациента
                            string deleteDiagnosesQuery = "DELETE FROM PatientDiagnoses WHERE PatientID = @PatientID";
                            using (SqlCommand cmdDiagnoses = new SqlCommand(deleteDiagnosesQuery, con, tran))
                            {
                                cmdDiagnoses.Parameters.AddWithValue("@PatientID", id);
                                cmdDiagnoses.ExecuteNonQuery();
                            }

                            // 5. Удаляем назначения процедур
                            string deleteProcedureAppointmentsQuery = "DELETE FROM ProcedureAppointments WHERE PatientID = @PatientID";
                            using (SqlCommand cmdAppointments = new SqlCommand(deleteProcedureAppointmentsQuery, con, tran))
                            {
                                cmdAppointments.Parameters.AddWithValue("@PatientID", id);
                                cmdAppointments.ExecuteNonQuery();
                            }

                            // 6. Удаляем медикаменты пациента
                            string deleteMedicationsQuery = "DELETE FROM PatientMedications WHERE PatientID = @PatientID";
                            using (SqlCommand cmdMedications = new SqlCommand(deleteMedicationsQuery, con, tran))
                            {
                                cmdMedications.Parameters.AddWithValue("@PatientID", id);
                                cmdMedications.ExecuteNonQuery();
                            }

                            // 7. Удаляем измерения пациента
                            string deleteMeasurementsQuery = "DELETE FROM PatientMeasurements WHERE PatientID = @PatientID";
                            using (SqlCommand cmdMeasurements = new SqlCommand(deleteMeasurementsQuery, con, tran))
                            {
                                cmdMeasurements.Parameters.AddWithValue("@PatientID", id);
                                cmdMeasurements.ExecuteNonQuery();
                            }

                            // 8. Удаляем выписные документы
                            string deleteDischargeDocsQuery = "DELETE FROM DischargeDocuments WHERE PatientID = @PatientID";
                            using (SqlCommand cmdDischargeDocs = new SqlCommand(deleteDischargeDocsQuery, con, tran))
                            {
                                cmdDischargeDocs.Parameters.AddWithValue("@PatientID", id);
                                cmdDischargeDocs.ExecuteNonQuery();
                            }

                            // 9. Выселяем пациента из комнаты, если он где-то проживает
                            string updateAccommodationsQuery = @"
                            UPDATE Accommodations
                            SET CheckOutDate = GETDATE()
                            WHERE PatientID = @PatientID AND CheckOutDate IS NULL";
                            using (SqlCommand cmdAccommodations = new SqlCommand(updateAccommodationsQuery, con, tran))
                            {
                                cmdAccommodations.Parameters.AddWithValue("@PatientID", id);
                                cmdAccommodations.ExecuteNonQuery();
                            }

                            // 10. Удаляем самого пациента
                            string deletePatientQuery = "DELETE FROM Patients WHERE PatientID = @PatientID";
                            using (SqlCommand cmdPatient = new SqlCommand(deletePatientQuery, con, tran))
                            {
                                cmdPatient.Parameters.AddWithValue("@PatientID", id);
                                int rowsAffected = cmdPatient.ExecuteNonQuery();
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

                return Ok(new { success = true, message = "Пациент успешно удален." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/manager/patient
        [HttpPost("patient")]
        [Consumes("application/json")]
        public IActionResult AddPatient([FromBody] PatientModel patient)
        {
            try
            {
                // Логирование полученных данных
                Console.WriteLine($"Получены данные пациента: {patient?.FullName}, Тип стационара: {patient?.StayType}");
                
                if (patient == null)
                    return BadRequest(new { success = false, message = "Получен пустой объект пациента" });
                    
                if (string.IsNullOrEmpty(patient.FullName))
                    return BadRequest(new { success = false, message = "ФИО пациента обязательно для заполнения" });

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Добавление пациента
                            string insertQuery;
                            if (patient.DischargeDate.HasValue)
                            {
                                insertQuery = @"
                                    INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, DischargeDate, StayType)
                                    VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @DischargeDate, @StayType);
                                    SELECT SCOPE_IDENTITY();";
                            }
                            else
                            {
                                insertQuery = @"
                                    INSERT INTO Patients (FullName, DateOfBirth, Gender, RecordDate, StayType)
                                    VALUES (@FullName, @DateOfBirth, @Gender, @RecordDate, @StayType);
                                    SELECT SCOPE_IDENTITY();";
                            }

                            int newPatientId;
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@FullName", patient.FullName);
                                cmd.Parameters.AddWithValue("@DateOfBirth", patient.DateOfBirth);
                                cmd.Parameters.AddWithValue("@Gender", patient.Gender);
                                cmd.Parameters.AddWithValue("@RecordDate", patient.RecordDate ?? DateTime.Now);
                                cmd.Parameters.AddWithValue("@StayType", patient.StayType ?? "Дневной");
                                
                                if (patient.DischargeDate.HasValue)
                                    cmd.Parameters.AddWithValue("@DischargeDate", patient.DischargeDate.Value);
                                
                                newPatientId = Convert.ToInt32(cmd.ExecuteScalar());
                                Console.WriteLine($"Добавлен пациент с ID: {newPatientId}");
                            }

                            // Если пациент размещается в стационаре и информация о размещении присутствует
                            if (patient.StayType == "Круглосуточный" && patient.AccommodationInfo != null)
                            {
                                Console.WriteLine($"Размещение пациента: Комната {patient.AccommodationInfo.RoomID}, Кровать {patient.AccommodationInfo.BedNumber}");
                                
                                // Проверка доступности кровати
                                string checkBedQuery = @"
                                    SELECT COUNT(*) FROM Accommodations 
                                    WHERE RoomID = @RoomID AND BedNumber = @BedNumber AND CheckOutDate IS NULL";
                                    
                                using (SqlCommand cmd = new SqlCommand(checkBedQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@RoomID", patient.AccommodationInfo.RoomID);
                                    cmd.Parameters.AddWithValue("@BedNumber", patient.AccommodationInfo.BedNumber);
                                    int occupiedCount = Convert.ToInt32(cmd.ExecuteScalar());
                                    
                                    if (occupiedCount > 0)
                                    {
                                        transaction.Rollback();
                                        return BadRequest(new { success = false, message = "Выбранное место уже занято" });
                                    }
                                }

                                // Добавление размещения
                                string insertAccommodationQuery = @"
                                    INSERT INTO Accommodations (RoomID, PatientID, BedNumber, CheckInDate)
                                    VALUES (@RoomID, @PatientID, @BedNumber, @CheckInDate)";
                                    
                                using (SqlCommand cmd = new SqlCommand(insertAccommodationQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@RoomID", patient.AccommodationInfo.RoomID);
                                    cmd.Parameters.AddWithValue("@PatientID", newPatientId);
                                    cmd.Parameters.AddWithValue("@BedNumber", patient.AccommodationInfo.BedNumber);
                                    cmd.Parameters.AddWithValue("@CheckInDate", DateTime.Now);
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();
                            
                            return Ok(new { 
                                success = true, 
                                message = "Пациент успешно добавлен", 
                                patientId = newPatientId 
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Ошибка при добавлении пациента: {ex.Message}");
                            throw new Exception("Ошибка при добавлении пациента: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Глобальная ошибка при добавлении пациента: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/rooms/{buildingId}
        [HttpGet("rooms/{buildingId}")]
        public IActionResult GetAvailableRooms(int buildingId)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT r.RoomID, r.RoomNumber, r.IsAvailable,
                        (SELECT COUNT(*) FROM Accommodations a WHERE a.RoomID = r.RoomID AND a.CheckOutDate IS NULL) AS OccupiedBeds
                        FROM Rooms r
                        WHERE r.BuildingID = @BuildingID AND r.IsAvailable = 1
                        ORDER BY r.RoomNumber";
                        
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@BuildingID", buildingId);
                    da.Fill(dt);
                    
                    var rooms = new List<object>();
                    foreach (DataRow row in dt.Rows)
                    {
                        int occupiedBeds = Convert.ToInt32(row["OccupiedBeds"]);
                        
                        // Включаем только комнаты с доступными местами
                        if (occupiedBeds < 2)
                        {
                            var room = new {
                                RoomID = Convert.ToInt32(row["RoomID"]),
                                RoomNumber = row["RoomNumber"].ToString(),
                                AvailableBeds = new List<int>()
                            };
                            
                            // Определяем доступные кровати
                            if (occupiedBeds > 0)
                            {
                                // Запрос для определения, какие конкретно кровати заняты
                                string bedQuery = @"
                                    SELECT BedNumber FROM Accommodations 
                                    WHERE RoomID = @RoomID AND CheckOutDate IS NULL";
                                
                                using (SqlCommand bedCmd = new SqlCommand(bedQuery, con))
                                {
                                    bedCmd.Parameters.AddWithValue("@RoomID", room.RoomID);
                                    
                                    var occupiedBedNumbers = new List<int>();
                                    using (SqlDataReader bedReader = bedCmd.ExecuteReader())
                                    {
                                        while (bedReader.Read())
                                        {
                                            occupiedBedNumbers.Add(Convert.ToInt32(bedReader["BedNumber"]));
                                        }
                                    }
                                    
                                    // Добавляем только свободные кровати
                                    for (int i = 1; i <= 2; i++)
                                    {
                                        if (!occupiedBedNumbers.Contains(i))
                                        {
                                            ((List<int>)room.AvailableBeds).Add(i);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Если комната полностью свободна, добавляем обе кровати
                                ((List<int>)room.AvailableBeds).Add(1);
                                ((List<int>)room.AvailableBeds).Add(2);
                            }
                            
                            // Добавляем комнату в результат только если есть свободные кровати
                            if (((List<int>)room.AvailableBeds).Count > 0)
                            {
                                rooms.Add(room);
                            }
                        }
                    }
                    
                    return Ok(rooms);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Сопровождающие лица

        // GET: api/manager/accompanyingpersons
        [HttpGet("accompanyingpersons")]
        public IActionResult GetAccompanyingPersons()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"SELECT ap.AccompanyingPersonID, ap.FullName, ap.DateOfBirth, ap.Relationship, 
                               ap.HasPowerOfAttorney, p.FullName AS PatientName, p.PatientID,
                               CASE 
                                   WHEN (SELECT COUNT(apd.DocumentID) FROM AccompanyingPersonDocuments apd 
                                        INNER JOIN DocumentTypes dt ON apd.DocumentTypeID = dt.DocumentTypeID 
                                        WHERE apd.AccompanyingPersonID = ap.AccompanyingPersonID AND dt.IsRequired = 1) =
                                        (SELECT COUNT(DocumentTypeID) FROM DocumentTypes WHERE IsRequired = 1 AND 
                                        ((ap.DateOfBirth IS NOT NULL AND DATEDIFF(YEAR, ap.DateOfBirth, GETDATE()) BETWEEN MinimumAge AND MaximumAge) OR ap.DateOfBirth IS NULL)
                                        AND ForAccompanyingPerson = 1)
                                   THEN 'Полный комплект'
                                   WHEN (SELECT COUNT(apd.DocumentID) FROM AccompanyingPersonDocuments apd 
                                        INNER JOIN DocumentTypes dt ON apd.DocumentTypeID = dt.DocumentTypeID 
                                        WHERE apd.AccompanyingPersonID = ap.AccompanyingPersonID AND dt.IsRequired = 1) > 0
                                   THEN 'Частично'
                                   ELSE 'Отсутствуют'
                               END AS DocumentsStatus
                           FROM AccompanyingPersons ap
                           INNER JOIN Patients p ON ap.PatientID = p.PatientID";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/manager/accompanyingperson/{id}
        [HttpDelete("accompanyingperson/{id}")]
        public IActionResult DeleteAccompanyingPerson(int id)
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
                            // 1. Удаляем документы сопровождающего лица
                            string deleteDocumentsQuery = "DELETE FROM AccompanyingPersonDocuments WHERE AccompanyingPersonID = @AccompanyingPersonID";
                            using (SqlCommand cmdDocuments = new SqlCommand(deleteDocumentsQuery, con, tran))
                            {
                                cmdDocuments.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                cmdDocuments.ExecuteNonQuery();
                            }

                            // 2. Выселяем сопровождающее лицо из комнаты, если оно где-то проживает
                            string updateAccommodationsQuery = @"
                            UPDATE Accommodations
                            SET CheckOutDate = GETDATE()
                            WHERE AccompanyingPersonID = @AccompanyingPersonID AND CheckOutDate IS NULL";
                            using (SqlCommand cmdAccommodations = new SqlCommand(updateAccommodationsQuery, con, tran))
                            {
                                cmdAccommodations.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                cmdAccommodations.ExecuteNonQuery();
                            }

                            // 3. Удаляем само сопровождающее лицо
                            string deletePersonQuery = "DELETE FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID";
                            using (SqlCommand cmdPerson = new SqlCommand(deletePersonQuery, con, tran))
                            {
                                cmdPerson.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                int rowsAffected = cmdPerson.ExecuteNonQuery();
                                if (rowsAffected == 0)
                                {
                                    tran.Rollback();
                                    return NotFound(new { success = false, message = "Сопровождающее лицо не найдено." });
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

                return Ok(new { success = true, message = "Сопровождающее лицо успешно удалено." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Размещение

        // GET: api/manager/buildings
        [HttpGet("buildings")]
        public IActionResult GetBuildings()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT BuildingID, BuildingNumber, TotalRooms, Description FROM Buildings";
                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.Fill(dt);
                }
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/accommodations
        [HttpGet("accommodations")]
        public IActionResult GetAccommodations([FromQuery] int? buildingId = null, [FromQuery] string status = null)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                    SELECT 
                        a.AccommodationID,
                        r.RoomID,
                        r.RoomNumber,
                        b.BuildingID,
                        b.BuildingNumber,
                        ISNULL(a.BedNumber, beds.BedNumber) AS BedNumber,
                        CASE 
                            WHEN a.PatientID IS NULL AND a.AccompanyingPersonID IS NULL THEN 'Свободно'
                            ELSE 'Занято'
                        END AS Status,
                        a.PatientID,
                        a.AccompanyingPersonID,
                        CASE 
                            WHEN a.PatientID IS NOT NULL THEN p.FullName
                            WHEN a.AccompanyingPersonID IS NOT NULL THEN ap.FullName
                            ELSE NULL
                        END AS PersonName,
                        CASE 
                            WHEN a.PatientID IS NOT NULL THEN 'Пациент'
                            WHEN a.AccompanyingPersonID IS NOT NULL THEN 'Сопровождающий'
                            ELSE NULL
                        END AS PersonType,
                        a.CheckInDate
                    FROM Rooms r
                    CROSS JOIN (SELECT 1 AS BedNumber UNION SELECT 2) AS beds
                    INNER JOIN Buildings b ON r.BuildingID = b.BuildingID
                    LEFT JOIN (
                        SELECT * FROM Accommodations
                        WHERE CheckOutDate IS NULL
                    ) a ON r.RoomID = a.RoomID AND beds.BedNumber = a.BedNumber
                    LEFT JOIN Patients p ON a.PatientID = p.PatientID
                    LEFT JOIN AccompanyingPersons ap ON a.AccompanyingPersonID = ap.AccompanyingPersonID
                    WHERE (@BuildingId IS NULL OR r.BuildingID = @BuildingId)";

                    // Добавляем фильтр по статусу, если он указан
                    if (!string.IsNullOrEmpty(status))
                    {
                        switch (status.ToLower())
                        {
                            case "free":
                                query += " AND (a.PatientID IS NULL AND a.AccompanyingPersonID IS NULL)";
                                break;
                            case "full":
                                query += @" AND r.RoomID IN (
                                    SELECT r.RoomID
                                    FROM Rooms r
                                    CROSS JOIN (SELECT 1 AS BedNumber UNION SELECT 2) AS beds
                                    LEFT JOIN (SELECT * FROM Accommodations WHERE CheckOutDate IS NULL) a 
                                        ON r.RoomID = a.RoomID AND beds.BedNumber = a.BedNumber
                                    GROUP BY r.RoomID
                                    HAVING COUNT(CASE WHEN a.PatientID IS NOT NULL OR a.AccompanyingPersonID IS NOT NULL THEN 1 END) = 2
                                )";
                                break;
                            case "partial":
                                query += @" AND r.RoomID IN (
                                    SELECT r.RoomID
                                    FROM Rooms r
                                    CROSS JOIN (SELECT 1 AS BedNumber UNION SELECT 2) AS beds
                                    LEFT JOIN (SELECT * FROM Accommodations WHERE CheckOutDate IS NULL) a 
                                        ON r.RoomID = a.RoomID AND beds.BedNumber = a.BedNumber
                                    GROUP BY r.RoomID
                                    HAVING COUNT(CASE WHEN a.PatientID IS NOT NULL OR a.AccompanyingPersonID IS NOT NULL THEN 1 END) = 1
                                )";
                                break;
                        }
                    }

                    query += " ORDER BY b.BuildingNumber, r.RoomNumber, beds.BedNumber";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@BuildingId", buildingId ?? (object)DBNull.Value);
                    da.Fill(dt);
                }
                
                // Явно преобразуем значения для BedNumber в числа перед возвратом
                var result = DataTableToList(dt);
                foreach (var item in (List<Dictionary<string, object>>)result)
                {
                    if (item.ContainsKey("BedNumber") && item["BedNumber"] != DBNull.Value)
                    {
                        item["BedNumber"] = Convert.ToInt32(item["BedNumber"]);
                    }
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/manager/accommodation/{id}/checkout
        [HttpPost("accommodation/{id}/checkout")]
        public IActionResult CheckOutPerson(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                    UPDATE Accommodations
                    SET CheckOutDate = GETDATE()
                    WHERE AccommodationID = @AccommodationID AND CheckOutDate IS NULL";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@AccommodationID", id);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return BadRequest(new { success = false, message = "Не удалось выселить. Возможно, проживание не найдено или человек уже выселен." });
                        }
                    }
                }

                return Ok(new { success = true, message = "Выселение выполнено успешно." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Документы

        // GET: api/manager/documents
        [HttpGet("documents")]
        public IActionResult GetDocuments([FromQuery] string category = null)
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                    SELECT DocumentID, DocumentName, Category, FileType, 
                           CASE 
                               WHEN FileSizeBytes < 1024 THEN CAST(FileSizeBytes AS NVARCHAR) + ' байт'
                               WHEN FileSizeBytes < 1048576 THEN CAST(CAST(FileSizeBytes / 1024.0 AS DECIMAL(10, 2)) AS NVARCHAR) + ' КБ'
                               ELSE CAST(CAST(FileSizeBytes / 1048576.0 AS DECIMAL(10, 2)) AS NVARCHAR) + ' МБ'
                           END AS FileSize,
                           UploadDate, UploadedBy, FilePath
                    FROM ManagerDocuments
                    WHERE (@Category IS NULL OR Category = @Category)
                    ORDER BY UploadDate DESC";

                    SqlDataAdapter da = new SqlDataAdapter(query, con);
                    da.SelectCommand.Parameters.AddWithValue("@Category", string.IsNullOrEmpty(category) ? (object)DBNull.Value : category);
                    da.Fill(dt);
                }
                
                return Ok(DataTableToList(dt));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/manager/document/{id}
        [HttpDelete("document/{id}")]
        public IActionResult DeleteDocument(int id)
        {
            try
            {
                string filePath = string.Empty;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // Сначала получаем путь к файлу
                    string getPathQuery = "SELECT FilePath FROM ManagerDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand getPathCmd = new SqlCommand(getPathQuery, con))
                    {
                        getPathCmd.Parameters.AddWithValue("@DocumentID", id);
                        object pathResult = getPathCmd.ExecuteScalar();
                        if (pathResult != null && pathResult != DBNull.Value)
                        {
                            filePath = pathResult.ToString();
                        }
                    }
                    
                    // Затем удаляем запись из базы данных
                    string deleteQuery = "DELETE FROM ManagerDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand deleteCmd = new SqlCommand(deleteQuery, con))
                    {
                        deleteCmd.Parameters.AddWithValue("@DocumentID", id);
                        int rowsAffected = deleteCmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Документ не найден." });
                        }
                    }
                }

                // Удаляем физический файл, если путь найден
                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                return Ok(new { success = true, message = "Документ успешно удален." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/document/{id}/view
        [HttpGet("document/{id}/view")]
        public IActionResult ViewDocument(int id)
        {
            try
            {
                string filePath = string.Empty;
                string fileType = string.Empty;
                string fileName = string.Empty;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT FilePath, FileType, DocumentName FROM ManagerDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                filePath = reader["FilePath"].ToString();
                                fileType = reader["FileType"].ToString();
                                fileName = reader["DocumentName"].ToString();
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Документ не найден." });
                            }
                        }
                    }
                }

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "Файл не найден на сервере." });
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                string mimeType = GetMimeType(fileType);
                
                // Для просмотра добавляем расширение к имени файла и устанавливаем inline disposition
                if (!fileName.ToLower().EndsWith($".{fileType.ToLower()}"))
                {
                    fileName = $"{fileName}.{fileType}";
                }
                
                // Кодируем имя файла для HTTP-заголовка
                string encodedFileName = Uri.EscapeDataString(fileName);
                
                // Добавляем заголовок для просмотра с правильно закодированным именем файла
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}");
                
                return File(fileBytes, mimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/document/{id}/download
        [HttpGet("document/{id}/download")]
        public IActionResult DownloadDocument(int id)
        {
            try
            {
                string filePath = string.Empty;
                string fileName = string.Empty;
                string fileType = string.Empty;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT FilePath, DocumentName, FileType FROM ManagerDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                filePath = reader["FilePath"].ToString();
                                fileName = reader["DocumentName"].ToString();
                                fileType = reader["FileType"].ToString();
                                
                                // Добавляем расширение к имени файла, если его нет
                                // Используем стандартные расширения для файлов
                                if (!fileName.ToLower().EndsWith($".{fileType.ToLower()}"))
                                {
                                    fileName = $"{fileName}.{GetStandardExtension(fileType)}";
                                }
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Документ не найден." });
                            }
                        }
                    }
                }

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "Файл не найден на сервере." });
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                string mimeType = GetMimeType(fileType);
                
                // Кодируем имя файла для HTTP-заголовка
                string encodedFileName = Uri.EscapeDataString(fileName);
                
                // Явно указываем, что файл должен быть скачан, а не просмотрен
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}");
                
                return File(fileBytes, mimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/document/{id}/print
        [HttpGet("document/{id}/print")]
        public IActionResult PrintDocument(int id)
        {
            try
            {
                string filePath = string.Empty;
                string fileType = string.Empty;
                string fileName = string.Empty;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT FilePath, FileType, DocumentName FROM ManagerDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                filePath = reader["FilePath"].ToString();
                                fileType = reader["FileType"].ToString();
                                fileName = reader["DocumentName"].ToString();
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Документ не найден." });
                            }
                        }
                    }
                }

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { success = false, message = "Файл не найден на сервере." });
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                string mimeType = GetMimeType(fileType);
                
                // Добавляем расширение к имени файла если его нет
                if (!fileName.ToLower().EndsWith($".{fileType.ToLower()}"))
                {
                    fileName = $"{fileName}.{fileType}";
                }
                
                // Кодируем имя файла для HTTP-заголовка
                string encodedFileName = Uri.EscapeDataString(fileName);
                
                // Добавляем заголовок для печати с правильно закодированным именем файла
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{encodedFileName}\"; filename*=UTF-8''{encodedFileName}");
                
                return File(fileBytes, mimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/manager/document/upload
        [HttpPost("document/upload")]
        public IActionResult UploadDocument([FromForm] IFormFile file, [FromForm] string documentName, [FromForm] string category, [FromForm] string description = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Файл не выбран или пустой." });
                }

                if (string.IsNullOrEmpty(documentName))
                {
                    return BadRequest(new { success = false, message = "Название документа не указано." });
                }

                // Создаем директорию для документов, если ее нет
                string baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Documents");
                if (!Directory.Exists(baseDirectory))
                {
                    Directory.CreateDirectory(baseDirectory);
                }

                // Создаем директорию для категории, если ее нет
                string categoryDirectory = Path.Combine(baseDirectory, category);
                if (!Directory.Exists(categoryDirectory))
                {
                    Directory.CreateDirectory(categoryDirectory);
                }

                // Получаем расширение файла
                string fileExtension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
                
                // Определяем понятный тип файла для отображения в интерфейсе
                string displayFileType = GetDisplayFileType(fileExtension);
                
                // Генерируем уникальное имя файла
                string fileName = $"{Guid.NewGuid()}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.{fileExtension}";
                string filePath = Path.Combine(categoryDirectory, fileName);
                
                // Получаем относительный путь для сохранения в БД
                string relativeFilePath = Path.Combine("Documents", category, fileName);
                
                // Сохраняем файл
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                // Добавляем запись в БД
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                    INSERT INTO ManagerDocuments (DocumentName, Category, FileType, FilePath, FileSizeBytes, UploadDate, UploadedBy, Description)
                    VALUES (@DocumentName, @Category, @FileType, @FilePath, @FileSizeBytes, GETDATE(), @UploadedBy, @Description);
                    SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentName", documentName);
                        cmd.Parameters.AddWithValue("@Category", category);
                        cmd.Parameters.AddWithValue("@FileType", fileExtension);
                        cmd.Parameters.AddWithValue("@FilePath", filePath);
                        cmd.Parameters.AddWithValue("@FileSizeBytes", file.Length);
                        cmd.Parameters.AddWithValue("@UploadedBy", "Web User");
                        cmd.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value);

                        int documentId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        return Ok(new { 
                            success = true, 
                            message = "Документ успешно загружен.", 
                            documentId = documentId,
                            documentName = documentName,
                            category = category,
                            fileType = fileExtension,
                            fileSize = FormatFileSize(file.Length),
                            uploadDate = DateTime.Now
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // Метод для получения понятного типа файла для отображения
        private string GetDisplayFileType(string fileExtension)
        {
            switch (fileExtension.ToLower())
            {
                case "pdf":
                    return "PDF";
                case "doc":
                case "docx":
                    return "Word";
                case "xls":
                case "xlsx":
                    return "Excel";
                case "ppt":
                case "pptx":
                    return "PowerPoint";
                case "txt":
                    return "Text";
                case "jpg":
                case "jpeg":
                    return "JPEG";
                case "png":
                    return "PNG";
                case "gif":
                    return "GIF";
                default:
                    return fileExtension.ToUpper();
            }
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} байт";
            if (bytes < 1048576)
                return $"{bytes / 1024.0:F2} КБ";
            
            return $"{bytes / 1048576.0:F2} МБ";
        }

        // Метод для получения стандартного расширения файла
        private string GetStandardExtension(string fileType)
        {
            // Преобразуем текстовое описание типа файла в стандартное расширение
            string extension = fileType.ToLower();
            
            switch (extension)
            {
                case "изображение":
                case "image":
                    return "jpg";
                case "документ":
                case "word":
                    return "docx";
                case "таблица":
                case "excel":
                case "spreadsheet":
                    return "xlsx";
                case "презентация":
                case "powerpoint":
                    return "pptx";
                case "текст":
                case "text":
                    return "txt";
                case "pdf":
                    return "pdf";
                case "png":
                    return "png";
                case "jpg":
                case "jpeg":
                    return "jpg";
                case "gif":
                    return "gif";
                default:
                    // Если тип выглядит как уже стандартное расширение (pdf, doc и т.д.), возвращаем его
                    if (extension.Length <= 5 && !extension.Contains(" "))
                        return extension;
                    else
                        return "dat"; // Общее неспецифическое расширение
            }
        }

        #endregion

        #region Вспомогательные методы

        private string GetMimeType(string fileType)
        {
            switch (fileType.ToLower())
            {
                case "pdf":
                    return "application/pdf";
                case "docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case "doc":
                    return "application/msword";
                case "xls":
                    return "application/vnd.ms-excel";
                case "xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case "png":
                    return "image/png";
                case "jpg":
                case "jpeg":
                    return "image/jpeg";
                case "gif":
                    return "image/gif";
                case "txt":
                    return "text/plain";
                default:
                    return "application/octet-stream";
            }
        }

        private object DataTableToList(DataTable dt)
        {
            var list = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();
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

    #region Модели данных

    public class AccommodationCheckOutRequest
    {
        public int AccommodationID { get; set; }
    }

    public class PatientModel
    {
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string StayType { get; set; }
        public AccommodationInfoModel? AccommodationInfo { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? DischargeDate { get; set; }
    }

    public class AccommodationInfoModel
    {
        public int RoomID { get; set; }
        public int BedNumber { get; set; }
    }

    #endregion
} 