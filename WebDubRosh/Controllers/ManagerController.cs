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
                Console.WriteLine($"Запрос на удаление пациента с ID: {id}");
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    Console.WriteLine("Соединение с базой данных открыто");
                    
                    using (SqlTransaction tran = con.BeginTransaction())
                    {
                        try
                        {
                            // 1. Проверяем, существует ли пациент
                            string checkPatientQuery = "SELECT COUNT(*) FROM Patients WHERE PatientID = @PatientID";
                            int patientCount = 0;
                            
                            using (SqlCommand checkCmd = new SqlCommand(checkPatientQuery, con, tran))
                            {
                                checkCmd.Parameters.AddWithValue("@PatientID", id);
                                patientCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            }
                            
                            if (patientCount == 0)
                            {
                                tran.Rollback();
                                Console.WriteLine($"Пациент с ID {id} не найден");
                                return NotFound(new { success = false, message = $"Пациент с ID {id} не найден" });
                            }
                            
                            // 2. Удаляем зависимые записи назначенных врачей, если они есть
                            string deleteAssignedDoctorsQuery = @"DELETE FROM PatientDoctorAssignments WHERE PatientID = @PatientID";
                            int deletedAssignments = 0;
                            
                            using (SqlCommand deleteCmd = new SqlCommand(deleteAssignedDoctorsQuery, con, tran))
                            {
                                deleteCmd.Parameters.AddWithValue("@PatientID", id);
                                deletedAssignments = deleteCmd.ExecuteNonQuery();
                            }
                            
                            if (deletedAssignments > 0)
                            {
                                Console.WriteLine($"Удалено {deletedAssignments} назначений врачей для пациента с ID {id}");
                            }

                            // 3. Проверяем наличие записей о размещении пациента и удаляем их
                            string deleteAccommodationsQuery = "DELETE FROM Accommodations WHERE PatientID = @PatientID";
                            int accommodationsDeleted = 0;
                            
                            using (SqlCommand cmdDeleteAccommodations = new SqlCommand(deleteAccommodationsQuery, con, tran))
                            {
                                cmdDeleteAccommodations.Parameters.AddWithValue("@PatientID", id);
                                accommodationsDeleted = cmdDeleteAccommodations.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено записей о размещении пациента: {accommodationsDeleted}");

                            // 4. Проверяем наличие сопровождающих лиц у пациента
                            string checkAccompanyingQuery = @"SELECT COUNT(*) FROM AccompanyingPersons WHERE PatientID = @PatientID";
                            int accompanyingCount = 0;
                            
                            using (SqlCommand checkCmd = new SqlCommand(checkAccompanyingQuery, con, tran))
                            {
                                checkCmd.Parameters.AddWithValue("@PatientID", id);
                                accompanyingCount = Convert.ToInt32(checkCmd.ExecuteScalar());
                            }
                            
                            // Если у пациента есть сопровождающие, удаляем их данные
                            if (accompanyingCount > 0)
                            {
                                Console.WriteLine($"Обнаружено {accompanyingCount} сопровождающих лиц для пациента с ID {id}");
                                
                                // 4.1. Получаем список ID сопровождающих лиц
                                List<int> accompanyingIDs = new List<int>();
                                using (SqlCommand getIDsCmd = new SqlCommand("SELECT AccompanyingPersonID FROM AccompanyingPersons WHERE PatientID = @PatientID", con, tran))
                                {
                                    getIDsCmd.Parameters.AddWithValue("@PatientID", id);
                                    using (SqlDataReader reader = getIDsCmd.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            accompanyingIDs.Add(reader.GetInt32(0));
                                        }
                                    }
                                }
                                
                                // 4.2. Для каждого сопровождающего удаляем его документы и размещение
                                foreach (int accompanyingID in accompanyingIDs)
                                {
                                    // Удаляем записи размещения сопровождающего
                                    string deleteAccompanyingAccommodationsQuery = "DELETE FROM Accommodations WHERE AccompanyingPersonID = @AccompanyingPersonID";
                                    int accomDeleted = 0;
                                    
                                    using (SqlCommand cmd = new SqlCommand(deleteAccompanyingAccommodationsQuery, con, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
                                        accomDeleted = cmd.ExecuteNonQuery();
                                    }
                                    
                                    Console.WriteLine($"Удалено записей о размещении сопровождающего {accompanyingID}: {accomDeleted}");
                                    
                                    // Удаляем документы сопровождающего лица
                                    string deleteAccompanyingDocsQuery = "DELETE FROM AccompanyingPersonDocuments WHERE AccompanyingPersonID = @AccompanyingPersonID";
                                    int accompanyingDocsDeleted = 0;
                                    
                                    using (SqlCommand cmd = new SqlCommand(deleteAccompanyingDocsQuery, con, tran))
                                    {
                                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingID);
                                        accompanyingDocsDeleted = cmd.ExecuteNonQuery();
                                    }
                                    
                                    Console.WriteLine($"Удалено документов сопровождающего {accompanyingID}: {accompanyingDocsDeleted}");
                                }
                                
                                // 4.3. Удаляем самих сопровождающих
                                string deleteAccompanyingQuery = "DELETE FROM AccompanyingPersons WHERE PatientID = @PatientID";
                                int accompanyingDeleted = 0;
                                
                                using (SqlCommand cmd = new SqlCommand(deleteAccompanyingQuery, con, tran))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", id);
                                    accompanyingDeleted = cmd.ExecuteNonQuery();
                                }
                                
                                Console.WriteLine($"Удалено сопровождающих лиц: {accompanyingDeleted}");
                            }

                            Console.WriteLine("Продолжаем удаление связанных данных пациента");
                            
                            // 5. Удаляем документы пациента
                            string deleteDocumentsQuery = "DELETE FROM PatientDocuments WHERE PatientID = @PatientID";
                            int docsDeleted = 0;
                            
                            using (SqlCommand cmdDocuments = new SqlCommand(deleteDocumentsQuery, con, tran))
                            {
                                cmdDocuments.Parameters.AddWithValue("@PatientID", id);
                                docsDeleted = cmdDocuments.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено документов: {docsDeleted}");

                            // 6. Удаляем диагнозы пациента
                            string deleteDiagnosesQuery = "DELETE FROM PatientDiagnoses WHERE PatientID = @PatientID";
                            int diagnosesDeleted = 0;
                            
                            using (SqlCommand cmdDiagnoses = new SqlCommand(deleteDiagnosesQuery, con, tran))
                            {
                                cmdDiagnoses.Parameters.AddWithValue("@PatientID", id);
                                diagnosesDeleted = cmdDiagnoses.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено диагнозов: {diagnosesDeleted}");

                            // 7. Удаляем записи из ScheduleGeneratedAppointments, связанные с назначениями процедур
                            string findScheduleAppointmentsQuery = @"
                                SELECT sga.GeneratedAppointmentID 
                                FROM ScheduleGeneratedAppointments sga
                                JOIN ProcedureAppointments pa ON sga.AppointmentID = pa.AppointmentID
                                WHERE pa.PatientID = @PatientID";
                                
                            List<int> scheduleGeneratedAppointmentsIDs = new List<int>();
                            
                            using (SqlCommand cmdFindSchedule = new SqlCommand(findScheduleAppointmentsQuery, con, tran))
                            {
                                cmdFindSchedule.Parameters.AddWithValue("@PatientID", id);
                                using (SqlDataReader reader = cmdFindSchedule.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        scheduleGeneratedAppointmentsIDs.Add(reader.GetInt32(0));
                                    }
                                }
                            }
                            
                            if (scheduleGeneratedAppointmentsIDs.Count > 0)
                            {
                                string deleteScheduleGeneratedQuery = "DELETE FROM ScheduleGeneratedAppointments WHERE GeneratedAppointmentID = @GeneratedAppointmentID";
                                int totalDeleted = 0;
                                
                                foreach (int genAppointmentID in scheduleGeneratedAppointmentsIDs)
                                {
                                    using (SqlCommand cmdDeleteScheduleGen = new SqlCommand(deleteScheduleGeneratedQuery, con, tran))
                                    {
                                        cmdDeleteScheduleGen.Parameters.AddWithValue("@GeneratedAppointmentID", genAppointmentID);
                                        totalDeleted += cmdDeleteScheduleGen.ExecuteNonQuery();
                                    }
                                }
                                
                                Console.WriteLine($"Удалено {totalDeleted} связей с регулярными назначениями");
                            }

                            // 8. Удаляем еженедельные назначения
                            string deleteWeeklyScheduleQuery = "DELETE FROM WeeklyScheduleAppointments WHERE PatientID = @PatientID";
                            int weeklySchedulesDeleted = 0;
                            
                            using (SqlCommand cmdDeleteWeekly = new SqlCommand(deleteWeeklyScheduleQuery, con, tran))
                            {
                                cmdDeleteWeekly.Parameters.AddWithValue("@PatientID", id);
                                weeklySchedulesDeleted = cmdDeleteWeekly.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено еженедельных расписаний: {weeklySchedulesDeleted}");

                            // 9. Удаляем назначения процедур
                            string deleteProcedureAppointmentsQuery = "DELETE FROM ProcedureAppointments WHERE PatientID = @PatientID";
                            int proceduresDeleted = 0;
                            
                            using (SqlCommand cmdAppointments = new SqlCommand(deleteProcedureAppointmentsQuery, con, tran))
                            {
                                cmdAppointments.Parameters.AddWithValue("@PatientID", id);
                                proceduresDeleted = cmdAppointments.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено назначений процедур: {proceduresDeleted}");

                            // 10. Удаляем медикаменты пациента
                            string deleteMedicationsQuery = "DELETE FROM PatientMedications WHERE PatientID = @PatientID";
                            int medicationsDeleted = 0;
                            
                            using (SqlCommand cmdMedications = new SqlCommand(deleteMedicationsQuery, con, tran))
                            {
                                cmdMedications.Parameters.AddWithValue("@PatientID", id);
                                medicationsDeleted = cmdMedications.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено медикаментов: {medicationsDeleted}");

                            // 11. Удаляем измерения пациента
                            string deleteMeasurementsQuery = "DELETE FROM PatientMeasurements WHERE PatientID = @PatientID";
                            int measurementsDeleted = 0;
                            
                            using (SqlCommand cmdMeasurements = new SqlCommand(deleteMeasurementsQuery, con, tran))
                            {
                                cmdMeasurements.Parameters.AddWithValue("@PatientID", id);
                                measurementsDeleted = cmdMeasurements.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено измерений: {measurementsDeleted}");

                            // 12. Удаляем выписные документы
                            string deleteDischargeDocumentsQuery = "DELETE FROM DischargeDocuments WHERE PatientID = @PatientID";
                            int dischargeDocsDeleted = 0;
                            
                            using (SqlCommand cmdDischargeDocuments = new SqlCommand(deleteDischargeDocumentsQuery, con, tran))
                            {
                                cmdDischargeDocuments.Parameters.AddWithValue("@PatientID", id);
                                dischargeDocsDeleted = cmdDischargeDocuments.ExecuteNonQuery();
                            }
                            
                            Console.WriteLine($"Удалено выписных документов: {dischargeDocsDeleted}");

                            // 13. Удаляем пациента
                            string deletePatientQuery = "DELETE FROM Patients WHERE PatientID = @PatientID";
                            int patientsDeleted = 0;
                            
                            using (SqlCommand cmdPatient = new SqlCommand(deletePatientQuery, con, tran))
                            {
                                cmdPatient.Parameters.AddWithValue("@PatientID", id);
                                patientsDeleted = cmdPatient.ExecuteNonQuery();
                            }
                                
                            if (patientsDeleted == 0)
                                {
                                    tran.Rollback();
                                return NotFound(new { success = false, message = $"Пациент с ID {id} не найден (возможно, был удален ранее)" });
                            }
                            
                            Console.WriteLine($"Пациент с ID {id} успешно удален");

                            tran.Commit();
                            return Ok(new { success = true, message = $"Пациент с ID {id} успешно удален" });
                        }
                        catch (SqlException ex)
                        {
                            tran.Rollback();
                            Console.WriteLine($"Ошибка SQL при удалении пациента с ID {id}: {ex.Message}");
                            return StatusCode(500, new { success = false, message = $"Ошибка при удалении пациента: {ex.Message}" });
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            Console.WriteLine($"Общая ошибка при удалении пациента с ID {id}: {ex.Message}");
                            return StatusCode(500, new { success = false, message = $"Ошибка при удалении пациента: {ex.Message}" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка при удалении пациента с ID {id}: {ex.Message}");
                return StatusCode(500, new { success = false, message = $"Критическая ошибка при удалении пациента: {ex.Message}" });
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
                Console.WriteLine($"Даты: Дата рождения = {patient?.DateOfBirth:yyyy-MM-dd}, " +
                                 $"Дата записи = {patient?.RecordDate:yyyy-MM-dd}, " +
                                 $"Дата выписки = {(patient?.DischargeDate.HasValue == true ? patient.DischargeDate.Value.ToString("yyyy-MM-dd") : "не указана")}");
                
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
                                    
                                    if (patient.AccommodationInfo.CurrentPatientID.HasValue)
                                    {
                                        // Если это режим редактирования, исключаем текущую кровать пациента из проверки
                                        checkBedQuery += " AND (PatientID IS NULL OR PatientID != @CurrentPatientID)";
                                    }
                                        
                                    using (SqlCommand cmd = new SqlCommand(checkBedQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@RoomID", patient.AccommodationInfo.RoomID);
                                        cmd.Parameters.AddWithValue("@BedNumber", patient.AccommodationInfo.BedNumber);
                                        
                                        if (patient.AccommodationInfo.CurrentPatientID.HasValue)
                                        {
                                            cmd.Parameters.AddWithValue("@CurrentPatientID", patient.AccommodationInfo.CurrentPatientID.Value);
                                        }
                                        
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
        public IActionResult GetAvailableRooms(int buildingId, [FromQuery] int? patientID = null, [FromQuery] int? accompanyingPersonID = null)
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
                        int roomID = Convert.ToInt32(row["RoomID"]);
                        
                        // Включаем комнаты, даже если они полностью заняты, когда указан ID пациента или сопровождающего
                        if (occupiedBeds < 2 || patientID.HasValue || accompanyingPersonID.HasValue)
                        {
                            var room = new {
                                RoomID = roomID,
                                RoomNumber = row["RoomNumber"].ToString(),
                                AvailableBeds = new List<int>()
                            };
                            
                            // Определяем доступные кровати
                            if (occupiedBeds > 0)
                            {
                                // Запрос для определения, какие конкретно кровати заняты
                                string bedQuery = @"
                                    SELECT a.BedNumber, a.PatientID, a.AccompanyingPersonID
                                    FROM Accommodations a
                                    WHERE a.RoomID = @RoomID AND a.CheckOutDate IS NULL";
                                
                                using (SqlCommand bedCmd = new SqlCommand(bedQuery, con))
                                {
                                    bedCmd.Parameters.AddWithValue("@RoomID", room.RoomID);
                                    
                                    var occupiedBedInfo = new Dictionary<int, Tuple<int?, int?>>(); // кровать -> (ID пациента, ID сопровождающего)
                                    using (SqlDataReader bedReader = bedCmd.ExecuteReader())
                                    {
                                        while (bedReader.Read())
                                        {
                                            int bedNumber = Convert.ToInt32(bedReader["BedNumber"]);
                                            int? patientIDInBed = bedReader["PatientID"] != DBNull.Value 
                                                ? (int?)Convert.ToInt32(bedReader["PatientID"]) 
                                                : null;
                                            int? accompanyingIDInBed = bedReader["AccompanyingPersonID"] != DBNull.Value 
                                                ? (int?)Convert.ToInt32(bedReader["AccompanyingPersonID"]) 
                                                : null;
                                            
                                            occupiedBedInfo[bedNumber] = new Tuple<int?, int?>(patientIDInBed, accompanyingIDInBed);
                                        }
                                    }
                                    
                                    // Добавляем свободные кровати или занятые текущим пациентом/сопровождающим
                                    for (int i = 1; i <= 2; i++)
                                    {
                                        if (!occupiedBedInfo.ContainsKey(i) || 
                                            (patientID.HasValue && occupiedBedInfo[i].Item1 == patientID.Value) ||
                                            (accompanyingPersonID.HasValue && occupiedBedInfo[i].Item2 == accompanyingPersonID.Value))
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

        // GET: api/manager/patient/{id}
        [HttpGet("patient/{id}")]
        public IActionResult GetPatient(int id)
        {
            try
            {
                Console.WriteLine($"Запрос на получение данных пациента с ID: {id}");
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // Получаем основные данные пациента
                    string patientQuery = @"
                        SELECT p.PatientID, p.FullName, p.DateOfBirth, p.Gender, p.RecordDate, p.DischargeDate, p.StayType
                        FROM Patients p
                        WHERE p.PatientID = @PatientID";
                    
                    PatientModel patient = null;
                    
                    using (SqlCommand cmd = new SqlCommand(patientQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                patient = new PatientModel
                                {
                                    FullName = reader["FullName"].ToString(),
                                    DateOfBirth = Convert.ToDateTime(reader["DateOfBirth"]),
                                    Gender = reader["Gender"].ToString(),
                                    StayType = reader["StayType"].ToString(),
                                    RecordDate = reader["RecordDate"] != DBNull.Value ? Convert.ToDateTime(reader["RecordDate"]) : (DateTime?)null,
                                    DischargeDate = reader["DischargeDate"] != DBNull.Value ? Convert.ToDateTime(reader["DischargeDate"]) : (DateTime?)null
                                };
                            }
                            else
                            {
                                return NotFound(new { success = false, message = $"Пациент с ID {id} не найден" });
                            }
                        }
                    }
                    
                    // Если пациент в круглосуточном стационаре, получаем данные о его размещении
                    if (patient.StayType == "Круглосуточный")
                    {
                        string accommodationQuery = @"
                            SELECT a.RoomID, a.BedNumber
                            FROM Accommodations a
                            WHERE a.PatientID = @PatientID AND a.CheckOutDate IS NULL";
                        
                        using (SqlCommand cmd = new SqlCommand(accommodationQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", id);
                            
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    patient.AccommodationInfo = new AccommodationInfoModel
                                    {
                                        RoomID = Convert.ToInt32(reader["RoomID"]),
                                        BedNumber = Convert.ToInt32(reader["BedNumber"]),
                                        CurrentPatientID = id
                                    };
                                }
                            }
                        }
                    }
                    
                    return Ok(new { success = true, patient });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных пациента: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/manager/patient/{id}
        [HttpPut("patient/{id}")]
        public IActionResult UpdatePatient(int id, [FromBody] PatientModel patient)
        {
            try
            {
                if (patient == null)
                    return BadRequest(new { success = false, message = "Получен пустой объект пациента" });
                    
                if (string.IsNullOrEmpty(patient.FullName))
                    return BadRequest(new { success = false, message = "ФИО пациента обязательно для заполнения" });

                Console.WriteLine($"Запрос на обновление пациента с ID: {id}");
                Console.WriteLine($"Новые данные: {patient.FullName}, Стационар: {patient.StayType}");
                Console.WriteLine($"Даты: Дата рождения = {patient.DateOfBirth:yyyy-MM-dd}, " +
                                  $"Дата записи = {patient.RecordDate:yyyy-MM-dd}, " +
                                  $"Дата выписки = {(patient.DischargeDate.HasValue ? patient.DischargeDate.Value.ToString("yyyy-MM-dd") : "не указана")}");
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Проверяем существование пациента
                            string checkQuery = "SELECT COUNT(*) FROM Patients WHERE PatientID = @PatientID";
                            int patientCount = 0;
                            
                            using (SqlCommand cmd = new SqlCommand(checkQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                patientCount = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            
                            if (patientCount == 0)
                            {
                                transaction.Rollback();
                                return NotFound(new { success = false, message = $"Пациент с ID {id} не найден" });
                            }
                            
                            // Обновляем основные данные пациента
                            string updateQuery;
                            if (patient.DischargeDate.HasValue)
                            {
                                updateQuery = @"
                                    UPDATE Patients
                                    SET FullName = @FullName, 
                                        DateOfBirth = @DateOfBirth, 
                                        Gender = @Gender, 
                                        RecordDate = @RecordDate, 
                                        DischargeDate = @DischargeDate,
                                        StayType = @StayType
                                    WHERE PatientID = @PatientID";
                            }
                            else
                            {
                                updateQuery = @"
                                    UPDATE Patients
                                    SET FullName = @FullName, 
                                        DateOfBirth = @DateOfBirth, 
                                        Gender = @Gender, 
                                        RecordDate = @RecordDate, 
                                        DischargeDate = NULL,
                                        StayType = @StayType
                                    WHERE PatientID = @PatientID";
                            }

                            using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                cmd.Parameters.AddWithValue("@FullName", patient.FullName);
                                cmd.Parameters.AddWithValue("@DateOfBirth", patient.DateOfBirth);
                                cmd.Parameters.AddWithValue("@Gender", patient.Gender);
                                cmd.Parameters.AddWithValue("@RecordDate", patient.RecordDate ?? DateTime.Now);
                                cmd.Parameters.AddWithValue("@StayType", patient.StayType ?? "Дневной");
                                
                                if (patient.DischargeDate.HasValue)
                                    cmd.Parameters.AddWithValue("@DischargeDate", patient.DischargeDate.Value);
                                
                                int rowsUpdated = cmd.ExecuteNonQuery();
                                Console.WriteLine($"Обновлено записей пациента: {rowsUpdated}");
                            }

                            // Если изменился тип стационара или данные размещения
                            // Сначала получим текущий тип стационара
                            string currentStayTypeQuery = "SELECT StayType FROM Patients WHERE PatientID = @PatientID";
                            string currentStayType = string.Empty;
                            
                            using (SqlCommand cmd = new SqlCommand(currentStayTypeQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                object result = cmd.ExecuteScalar();
                                if (result != null && result != DBNull.Value)
                                {
                                    currentStayType = result.ToString();
                                }
                            }
                            
                            // Проверяем наличие размещения у пациента
                            bool hasAccommodation = false;
                            using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Accommodations WHERE PatientID = @PatientID AND CheckOutDate IS NULL", con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                hasAccommodation = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                            }
                            
                            // Обработка размещения в зависимости от ситуации
                            if (patient.StayType == "Круглосуточный")
                            {
                                Console.WriteLine($"Обрабатываем круглосуточный стационар: hasAccommodation = {hasAccommodation}");
                                if (patient.AccommodationInfo != null)
                                {
                                    Console.WriteLine($"Данные размещения: RoomID={patient.AccommodationInfo.RoomID}, BedNumber={patient.AccommodationInfo.BedNumber}");
                                    
                                    // Проверяем, изменилось ли размещение пациента
                                    bool accommodationChanged = false;
                                    
                                    if (hasAccommodation)
                                    {
                                        string getCurrentAccommodationQuery = @"
                                            SELECT RoomID, BedNumber FROM Accommodations 
                                            WHERE PatientID = @PatientID AND CheckOutDate IS NULL";
                                        
                                        int currentRoomID = 0;
                                        int currentBedNumber = 0;
                                        
                                        using (SqlCommand cmd = new SqlCommand(getCurrentAccommodationQuery, con, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@PatientID", id);
                                            using (SqlDataReader reader = cmd.ExecuteReader())
                                            {
                                                if (reader.Read())
                                                {
                                                    currentRoomID = Convert.ToInt32(reader["RoomID"]);
                                                    currentBedNumber = Convert.ToInt32(reader["BedNumber"]);
                                                }
                                            }
                                        }
                                        
                                        Console.WriteLine($"Текущее размещение: RoomID={currentRoomID}, BedNumber={currentBedNumber}");
                                        
                                        // Если комната или кровать изменились
                                        accommodationChanged = currentRoomID != patient.AccommodationInfo.RoomID || 
                                                             currentBedNumber != patient.AccommodationInfo.BedNumber;
                                    }
                                    else
                                    {
                                        accommodationChanged = true; // Раньше не было размещения, теперь есть
                                    }

                                    Console.WriteLine($"Размещение изменилось: {accommodationChanged}");
                                    
                                    // Если переход с дневного на круглосуточный ИЛИ изменение размещения
                                    if ((currentStayType != "Круглосуточный" || !hasAccommodation) || accommodationChanged)
                                    {
                                        if (accommodationChanged && hasAccommodation)
                                        {
                                            // Закрываем текущее размещение
                                            string closeCurrentAccommodationQuery = @"
                                                UPDATE Accommodations 
                                                SET CheckOutDate = GETDATE() 
                                                WHERE PatientID = @PatientID AND CheckOutDate IS NULL";
                                            
                                            using (SqlCommand cmd = new SqlCommand(closeCurrentAccommodationQuery, con, transaction))
                                            {
                                                cmd.Parameters.AddWithValue("@PatientID", id);
                                                int rowsAffected = cmd.ExecuteNonQuery();
                                                Console.WriteLine($"Закрыто текущее размещение: {rowsAffected} строк обновлено");
                                            }
                                        }
                                        
                                        // Проверка доступности кровати
                                        string checkBedQuery = @"
                                            SELECT COUNT(*) FROM Accommodations 
                                            WHERE RoomID = @RoomID AND BedNumber = @BedNumber AND CheckOutDate IS NULL";
                                        
                                        if (patient.AccommodationInfo.CurrentPatientID.HasValue)
                                        {
                                            // Если это режим редактирования, исключаем текущую кровать пациента из проверки
                                            checkBedQuery += " AND (PatientID IS NULL OR PatientID != @CurrentPatientID)";
                                        }
                                            
                                        using (SqlCommand cmd = new SqlCommand(checkBedQuery, con, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@RoomID", patient.AccommodationInfo.RoomID);
                                            cmd.Parameters.AddWithValue("@BedNumber", patient.AccommodationInfo.BedNumber);
                                            
                                            if (patient.AccommodationInfo.CurrentPatientID.HasValue)
                                            {
                                                cmd.Parameters.AddWithValue("@CurrentPatientID", patient.AccommodationInfo.CurrentPatientID.Value);
                                            }
                                            
                                            int occupiedCount = Convert.ToInt32(cmd.ExecuteScalar());
                                            
                                            if (occupiedCount > 0)
                                            {
                                                transaction.Rollback();
                                                return BadRequest(new { success = false, message = "Выбранное место уже занято" });
                                            }
                                        }
                                        
                                        // Добавление нового размещения
                                        string insertAccommodationQuery = @"
                                            INSERT INTO Accommodations (RoomID, PatientID, BedNumber, CheckInDate)
                                            VALUES (@RoomID, @PatientID, @BedNumber, @CheckInDate)";
                                            
                                        using (SqlCommand cmd = new SqlCommand(insertAccommodationQuery, con, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@RoomID", patient.AccommodationInfo.RoomID);
                                            cmd.Parameters.AddWithValue("@PatientID", id);
                                            cmd.Parameters.AddWithValue("@BedNumber", patient.AccommodationInfo.BedNumber);
                                            cmd.Parameters.AddWithValue("@CheckInDate", DateTime.Now);
                                            int rowsAffected = cmd.ExecuteNonQuery();
                                            Console.WriteLine($"Добавлено новое размещение: {rowsAffected} строк вставлено");
                                        }
                                    }
                                }
                            }
                            else if (currentStayType == "Круглосуточный" && patient.StayType == "Дневной")
                            {
                                // Если переход с круглосуточного на дневной - удаляем размещение
                                using (SqlCommand cmd = new SqlCommand("UPDATE Accommodations SET CheckOutDate = GETDATE() WHERE PatientID = @PatientID AND CheckOutDate IS NULL", con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@PatientID", id);
                                    int deleted = cmd.ExecuteNonQuery();
                                    Console.WriteLine($"Удалено записей размещения: {deleted}");
                                }
                            }
                            
                            transaction.Commit();
                            
                            return Ok(new { 
                                success = true, 
                                message = "Пациент успешно обновлен", 
                                patientId = id 
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Ошибка при обновлении пациента: {ex.Message}");
                            throw new Exception("Ошибка при обновлении пациента: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка при обновлении пациента: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/rooms/{roomId}/building
        [HttpGet("rooms/{roomId}/building")]
        public IActionResult GetRoomBuilding(int roomId)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT r.BuildingID
                        FROM Rooms r
                        WHERE r.RoomID = @RoomID";
                    
                    int buildingId = 0;
                    
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@RoomID", roomId);
                        object result = cmd.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            buildingId = Convert.ToInt32(result);
                        }
                        else
                        {
                            return NotFound(new { success = false, message = $"Комната с ID {roomId} не найдена" });
                        }
                    }
                    
                    return Ok(new { success = true, buildingID = buildingId });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении данных о здании: {ex.Message}");
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

        // GET: api/manager/accompanyingperson/{id}
        [HttpGet("accompanyingperson/{id}")]
        public IActionResult GetAccompanyingPerson(int id)
        {
            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT ap.AccompanyingPersonID, ap.PatientID, ap.FullName, ap.DateOfBirth, 
                               ap.Relationship, ap.HasPowerOfAttorney
                        FROM AccompanyingPersons ap
                        WHERE ap.AccompanyingPersonID = @AccompanyingPersonID";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                            
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    var accompanyingPerson = new
                                    {
                                        AccompanyingPersonID = reader.GetInt32(0),
                                        PatientID = reader.GetInt32(1),
                                        FullName = reader.GetString(2),
                                        DateOfBirth = reader["DateOfBirth"] != DBNull.Value ? reader.GetDateTime(3) : (DateTime?)null,
                                        Relationship = reader["Relationship"] != DBNull.Value ? reader.GetString(4) : null,
                                        HasPowerOfAttorney = reader.GetBoolean(5)
                                    };

                                    // Если сопровождающий привязан к пациенту на круглосуточном стационаре, получаем данные о размещении
                                    var accommodationInfo = GetAccompanyingAccommodation(id);
                                    
                                    return Ok(new { success = true, accompanyingPerson, accommodationInfo });
                                }
                                else
                                {
                                    return NotFound(new { success = false, message = "Сопровождающий не найден" });
                                }
                            }
                        }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/patients/list
        [HttpGet("patients/list")]
        public IActionResult GetPatientsList()
        {
            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT PatientID, FullName, StayType FROM Patients ORDER BY FullName";
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

        // POST: api/manager/accompanyingperson
        [HttpPost("accompanyingperson")]
        public IActionResult AddAccompanyingPerson([FromBody] AccompanyingPersonModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { success = false, message = "Не указаны данные сопровождающего" });
                    
                if (string.IsNullOrEmpty(model.FullName))
                    return BadRequest(new { success = false, message = "ФИО сопровождающего обязательно для заполнения" });
                    
                if (model.PatientID <= 0)
                    return BadRequest(new { success = false, message = "Необходимо выбрать пациента" });
                    
                // Проверка наличия доверенности, если она требуется
                if (model.Relationship != "Родитель" && model.Relationship != "Опекун" && !model.HasPowerOfAttorney)
                {
                    return BadRequest(new { success = false, message = "Для выбранного отношения требуется наличие доверенности" });
                }
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Добавляем сопровождающего
                            string insertQuery = @"
                                INSERT INTO AccompanyingPersons (PatientID, FullName, DateOfBirth, Relationship, HasPowerOfAttorney)
                                VALUES (@PatientID, @FullName, @DateOfBirth, @Relationship, @HasPowerOfAttorney);
                                SELECT SCOPE_IDENTITY();";
                                
                            int newAccompanyingPersonID;
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", model.PatientID);
                                cmd.Parameters.AddWithValue("@FullName", model.FullName);
                                
                                if (model.DateOfBirth.HasValue)
                                    cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth.Value);
                                else
                                    cmd.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);
                                    
                                cmd.Parameters.AddWithValue("@Relationship", model.Relationship);
                                cmd.Parameters.AddWithValue("@HasPowerOfAttorney", model.HasPowerOfAttorney);
                                
                                newAccompanyingPersonID = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            
                            // Если нужно размещение сопровождающего
                            if (model.NeedAccommodation && model.AccommodationInfo != null)
                            {
                                // Проверяем, доступно ли выбранное место
                                string checkBedQuery = @"
                                    SELECT COUNT(*) FROM Accommodations 
                                    WHERE RoomID = @RoomID AND BedNumber = @BedNumber AND CheckOutDate IS NULL";
                                    
                                using (SqlCommand cmd = new SqlCommand(checkBedQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@RoomID", model.AccommodationInfo.RoomID);
                                    cmd.Parameters.AddWithValue("@BedNumber", model.AccommodationInfo.BedNumber);
                                    
                                    int occupiedCount = Convert.ToInt32(cmd.ExecuteScalar());
                                    
                                    if (occupiedCount > 0)
                                    {
                                        transaction.Rollback();
                                        return BadRequest(new { success = false, message = "Выбранное место уже занято" });
                                    }
                                }
                                
                                // Добавляем размещение сопровождающего
                                string insertAccommodationQuery = @"
                                    INSERT INTO Accommodations (RoomID, AccompanyingPersonID, BedNumber, CheckInDate)
                                    VALUES (@RoomID, @AccompanyingPersonID, @BedNumber, @CheckInDate)";
                                    
                                using (SqlCommand cmd = new SqlCommand(insertAccommodationQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@RoomID", model.AccommodationInfo.RoomID);
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", newAccompanyingPersonID);
                                    cmd.Parameters.AddWithValue("@BedNumber", model.AccommodationInfo.BedNumber);
                                    cmd.Parameters.AddWithValue("@CheckInDate", DateTime.Now);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            
                            transaction.Commit();
                            
                            return Ok(new { 
                                success = true, 
                                message = "Сопровождающий успешно добавлен", 
                                accompanyingPersonID = newAccompanyingPersonID 
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Ошибка при добавлении сопровождающего: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // PUT: api/manager/accompanyingperson/{id}
        [HttpPut("accompanyingperson/{id}")]
        public IActionResult UpdateAccompanyingPerson(int id, [FromBody] AccompanyingPersonModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { success = false, message = "Не указаны данные сопровождающего" });
                    
                if (string.IsNullOrEmpty(model.FullName))
                    return BadRequest(new { success = false, message = "ФИО сопровождающего обязательно для заполнения" });
                    
                if (model.PatientID <= 0)
                    return BadRequest(new { success = false, message = "Необходимо выбрать пациента" });
                    
                // Проверка наличия доверенности, если она требуется
                if (model.Relationship != "Родитель" && model.Relationship != "Опекун" && !model.HasPowerOfAttorney)
                {
                    return BadRequest(new { success = false, message = "Для выбранного отношения требуется наличие доверенности" });
                }
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Проверяем существование сопровождающего
                            string checkQuery = "SELECT COUNT(*) FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID";
                            int accompanyingPersonCount = 0;
                            
                            using (SqlCommand cmd = new SqlCommand(checkQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                accompanyingPersonCount = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                            
                            if (accompanyingPersonCount == 0)
                            {
                                transaction.Rollback();
                                return NotFound(new { success = false, message = $"Сопровождающий с ID {id} не найден" });
                            }
                            
                            // Обновляем данные сопровождающего
                            string updateQuery = @"
                                UPDATE AccompanyingPersons
                                SET PatientID = @PatientID,
                                    FullName = @FullName,
                                    DateOfBirth = @DateOfBirth,
                                    Relationship = @Relationship,
                                    HasPowerOfAttorney = @HasPowerOfAttorney
                                WHERE AccompanyingPersonID = @AccompanyingPersonID";
                                
                            using (SqlCommand cmd = new SqlCommand(updateQuery, con, transaction))
                            {
                                cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                cmd.Parameters.AddWithValue("@PatientID", model.PatientID);
                                cmd.Parameters.AddWithValue("@FullName", model.FullName);
                                
                                if (model.DateOfBirth.HasValue)
                                    cmd.Parameters.AddWithValue("@DateOfBirth", model.DateOfBirth.Value);
                                else
                                    cmd.Parameters.AddWithValue("@DateOfBirth", DBNull.Value);
                                    
                                cmd.Parameters.AddWithValue("@Relationship", model.Relationship);
                                cmd.Parameters.AddWithValue("@HasPowerOfAttorney", model.HasPowerOfAttorney);
                                
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Проверяем, нужно ли обновить размещение
                            if (model.NeedAccommodation && model.AccommodationInfo != null)
                            {
                                // Проверяем текущее размещение
                                int? currentRoomID = null;
                                int? currentBedNumber = null;
                                
                                string checkAccommodationQuery = @"
                                    SELECT RoomID, BedNumber
                                    FROM Accommodations
                                    WHERE AccompanyingPersonID = @AccompanyingPersonID AND CheckOutDate IS NULL";
                                    
                                using (SqlCommand cmd = new SqlCommand(checkAccommodationQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                    using (SqlDataReader reader = cmd.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            currentRoomID = reader.GetInt32(0);
                                            currentBedNumber = reader.GetInt32(1);
                                        }
                                    }
                                }
                                
                                bool accommodationChanged = currentRoomID != model.AccommodationInfo.RoomID || 
                                                           currentBedNumber != model.AccommodationInfo.BedNumber;
                                
                                // Если размещение изменилось или его не было
                                if (accommodationChanged || currentRoomID == null)
                                {
                                    // Если есть текущее размещение, закрываем его
                                    if (currentRoomID.HasValue)
                                    {
                                        string closeAccommodationQuery = @"
                                            UPDATE Accommodations
                                            SET CheckOutDate = GETDATE()
                                            WHERE AccompanyingPersonID = @AccompanyingPersonID AND CheckOutDate IS NULL";
                                            
                                        using (SqlCommand cmd = new SqlCommand(closeAccommodationQuery, con, transaction))
                                        {
                                            cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                            cmd.ExecuteNonQuery();
                                        }
                                    }
                                    
                                    // Проверяем, доступно ли выбранное место
                                    string checkBedQuery = @"
                                        SELECT COUNT(*) FROM Accommodations 
                                        WHERE RoomID = @RoomID AND BedNumber = @BedNumber AND CheckOutDate IS NULL";
                                        
                                    using (SqlCommand cmd = new SqlCommand(checkBedQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@RoomID", model.AccommodationInfo.RoomID);
                                        cmd.Parameters.AddWithValue("@BedNumber", model.AccommodationInfo.BedNumber);
                                        
                                        int occupiedCount = Convert.ToInt32(cmd.ExecuteScalar());
                                        
                                        if (occupiedCount > 0)
                                        {
                                            transaction.Rollback();
                                            return BadRequest(new { success = false, message = "Выбранное место уже занято" });
                                        }
                                    }
                                    
                                    // Добавляем новое размещение
                                    string insertAccommodationQuery = @"
                                        INSERT INTO Accommodations (RoomID, AccompanyingPersonID, BedNumber, CheckInDate)
                                        VALUES (@RoomID, @AccompanyingPersonID, @BedNumber, @CheckInDate)";
                                        
                                    using (SqlCommand cmd = new SqlCommand(insertAccommodationQuery, con, transaction))
                                    {
                                        cmd.Parameters.AddWithValue("@RoomID", model.AccommodationInfo.RoomID);
                                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                        cmd.Parameters.AddWithValue("@BedNumber", model.AccommodationInfo.BedNumber);
                                        cmd.Parameters.AddWithValue("@CheckInDate", DateTime.Now);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            else if (!model.NeedAccommodation)
                            {
                                // Если размещение больше не нужно, выселяем сопровождающего
                                string closeAccommodationQuery = @"
                                    UPDATE Accommodations
                                    SET CheckOutDate = GETDATE()
                                    WHERE AccompanyingPersonID = @AccompanyingPersonID AND CheckOutDate IS NULL";
                                    
                                using (SqlCommand cmd = new SqlCommand(closeAccommodationQuery, con, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            
                            transaction.Commit();
                            
                            return Ok(new { 
                                success = true, 
                                message = "Сопровождающий успешно обновлен"
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Ошибка при обновлении сопровождающего: " + ex.Message);
                        }
                    }
                }
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

                            // 2. Удаляем записи размещения вместо обновления CheckOutDate
                            string deleteAccommodationsQuery = @"DELETE FROM Accommodations 
                                                                WHERE AccompanyingPersonID = @AccompanyingPersonID";
                            using (SqlCommand cmdAccommodations = new SqlCommand(deleteAccommodationsQuery, con, tran))
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
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            Console.WriteLine($"Ошибка при удалении сопровождающего лица (ID: {id}): {ex.Message}");
                            // Перебрасываем исключение выше с более информативным сообщением
                            throw new Exception($"Не удалось удалить сопровождающее лицо: {ex.Message}");
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

        // Вспомогательный метод для получения информации о размещении сопровождающего
        private object GetAccompanyingAccommodation(int accompanyingPersonID)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                con.Open();
                string query = @"
                    SELECT a.AccommodationID, a.RoomID, r.RoomNumber, b.BuildingID, b.BuildingNumber, a.BedNumber
                    FROM Accommodations a
                    JOIN Rooms r ON a.RoomID = r.RoomID
                    JOIN Buildings b ON r.BuildingID = b.BuildingID
                    WHERE a.AccompanyingPersonID = @AccompanyingPersonID AND a.CheckOutDate IS NULL";
                    
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@AccompanyingPersonID", accompanyingPersonID);
                    
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new
                            {
                                AccommodationID = reader.GetInt32(0),
                                RoomID = reader.GetInt32(1),
                                RoomNumber = reader.GetString(2),
                                BuildingID = reader.GetInt32(3),
                                BuildingNumber = reader.GetInt32(4),
                                BedNumber = reader.GetInt32(5)
                            };
                        }
                    }
                }
            }
            
            return null;
        }

        // POST: api/manager/accompanyingperson/{id}/powerofattorney
        [HttpPost("{id}/powerofattorney")]
        public IActionResult UploadPowerOfAttorney(int id, [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Файл доверенности не выбран или пустой." });
                }
        
                // Проверяем существование сопровождающего
                string accompanyingPersonName = string.Empty;
                using (SqlConnection conCheck = new SqlConnection(_connectionString))
                {
                    conCheck.Open();
                    string checkQuery = "SELECT FullName FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conCheck))
                    {
                        checkCmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                        object result = checkCmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return NotFound(new { success = false, message = "Сопровождающее лицо не найдено." });
                        }
                        accompanyingPersonName = result.ToString();
                    }
                }
        
        
                // Получаем ID типа документа "Доверенность от законных представителей"
                int powerOfAttorneyDocTypeID = -1;
                using (SqlConnection conType = new SqlConnection(_connectionString))
                {
                    conType.Open();
                    // Исправляем поиск: ищем без тройных кавычек
                    string docTypeQuery = "SELECT DocumentTypeID FROM DocumentTypes WHERE DocumentName = 'Доверенность от законных представителей'";
                    using (SqlCommand typeCmd = new SqlCommand(docTypeQuery, conType))
                    {
                        object result = typeCmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            // Тип документа не найден - возвращаем ошибку, не создаем автоматически
                            return BadRequest(new { success = false, message = "Тип документа 'Доверенность от законных представителей' не найден в базе данных. Обратитесь к администратору." });
                        }
                        powerOfAttorneyDocTypeID = Convert.ToInt32(result);
                    }
                }
        
                // Убедимся, что ID найден корректно
                 if(powerOfAttorneyDocTypeID <= 0) {
                     return StatusCode(500, new { success = false, message = "Не удалось определить тип документа для доверенности." });
                 }
        
        
                // Определяем путь для сохранения
                string baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Documents", "PowerOfAttorney");
                Directory.CreateDirectory(baseDirectory); // Создаем папку, если ее нет
        
                string personFolder = Path.Combine(baseDirectory, id.ToString());
                Directory.CreateDirectory(personFolder);
        
                // Создаем уникальное имя файла
                string fileExtension = Path.GetExtension(file.FileName);
                string uniqueFileName = $"poa_{DateTime.Now:yyyyMMddHHmmssfff}{fileExtension}";
                string destinationPath = Path.Combine(personFolder, uniqueFileName);
                string relativePath = Path.Combine("Documents", "PowerOfAttorney", id.ToString(), uniqueFileName).Replace('\\', '/'); // Относительный путь для веб-доступа
        
        
                // Сохраняем файл
                using (var stream = new FileStream(destinationPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
        
                // Сохраняем информацию в БД
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            // Проверяем, есть ли уже загруженная доверенность
                            int existingDocID = -1;
                            string oldFilePath = null;
                            string checkDocQuery = @"
                                SELECT DocumentID, DocumentPath FROM AccompanyingPersonDocuments
                                WHERE AccompanyingPersonID = @AccompanyingPersonID
                                AND DocumentTypeID = @DocumentTypeID";
        
                            using (SqlCommand checkCmd = new SqlCommand(checkDocQuery, con, transaction))
                            {
                                checkCmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                checkCmd.Parameters.AddWithValue("@DocumentTypeID", powerOfAttorneyDocTypeID);
                                using(SqlDataReader reader = checkCmd.ExecuteReader())
                                {
                                    if(reader.Read())
                                    {
                                        existingDocID = reader.GetInt32(0);
                                        oldFilePath = reader.IsDBNull(1) ? null : reader.GetString(1);
                                    }
                                } // SqlDataReader закроется здесь
                            }
        
        
                            if (existingDocID > 0)
                            {
                                // Удаляем старый файл, если он существует и путь отличается
                                if (!string.IsNullOrEmpty(oldFilePath) && oldFilePath != destinationPath && System.IO.File.Exists(oldFilePath))
                                {
                                     try { System.IO.File.Delete(oldFilePath); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить старый файл доверенности: {ex.Message}");}
                                }
        
                                // Обновляем существующий документ
                                string updateDocQuery = @"
                                    UPDATE AccompanyingPersonDocuments
                                    SET DocumentPath = @DocumentPath, UploadDate = GETDATE(), IsVerified = 0
                                    WHERE DocumentID = @DocumentID";
        
                                using (SqlCommand updateCmd = new SqlCommand(updateDocQuery, con, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@DocumentPath", destinationPath); // Сохраняем полный путь
                                    updateCmd.Parameters.AddWithValue("@DocumentID", existingDocID);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Добавляем новый документ
                                string insertDocQuery = @"
                                    INSERT INTO AccompanyingPersonDocuments
                                    (AccompanyingPersonID, DocumentTypeID, DocumentPath, UploadDate, IsVerified)
                                    VALUES (@AccompanyingPersonID, @DocumentTypeID, @DocumentPath, GETDATE(), 0)";
        
                                using (SqlCommand insertCmd = new SqlCommand(insertDocQuery, con, transaction))
                                {
                                    insertCmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                    insertCmd.Parameters.AddWithValue("@DocumentTypeID", powerOfAttorneyDocTypeID);
                                    insertCmd.Parameters.AddWithValue("@DocumentPath", destinationPath); // Сохраняем полный путь
                                    insertCmd.ExecuteNonQuery();
                                }
                            }
        
                            // Обновляем флаг HasPowerOfAttorney в основной таблице
                            string updatePersonQuery = @"
                                UPDATE AccompanyingPersons
                                SET HasPowerOfAttorney = 1
                                WHERE AccompanyingPersonID = @AccompanyingPersonID";
                            using (SqlCommand updatePersonCmd = new SqlCommand(updatePersonQuery, con, transaction))
                            {
                                updatePersonCmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                                updatePersonCmd.ExecuteNonQuery();
                            }
        
        
                            transaction.Commit();
                             return Ok(new { success = true, message = "Доверенность успешно загружена.", filePath = relativePath });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            // Попытка удалить загруженный файл в случае ошибки БД
                            try { if (System.IO.File.Exists(destinationPath)) System.IO.File.Delete(destinationPath); } catch { }
                            Console.WriteLine($"Ошибка при сохранении доверенности в БД: {ex.Message}");
                            return StatusCode(500, new { success = false, message = "Ошибка сервера при сохранении доверенности: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка при загрузке доверенности: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Ошибка сервера при загрузке доверенности: " + ex.Message });
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
                
                return new FileContentResult(fileBytes, mimeType);
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
                
                return new FileContentResult(fileBytes, mimeType);
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
                
                return new FileContentResult(fileBytes, mimeType);
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

        #region Документы пациентов

        // GET: api/manager/patient/{id}/documents
        [HttpGet("patient/{id}/documents")]
        public IActionResult GetPatientDocuments(int id)
        {
            try
            {
                // Сначала получаем возраст пациента
                int patientAge = 0;
                DateTime? dateOfBirth = null;
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string ageQuery = "SELECT DateOfBirth FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(ageQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            dateOfBirth = (DateTime)result;
                            patientAge = DateTime.Today.Year - dateOfBirth.Value.Year;
                            if (dateOfBirth.Value.Date > DateTime.Today.AddYears(-patientAge)) patientAge--;
                        }
                    }
                }
                
                // Затем получаем список требуемых документов и их статус
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT dt.DocumentTypeID, dt.DocumentName, dt.IsRequired,
                               pd.DocumentID, pd.DocumentPath, pd.UploadDate, pd.IsVerified, pd.Notes,
                               CASE 
                                   WHEN pd.DocumentID IS NULL THEN 'Не загружен'
                                   WHEN pd.IsVerified = 1 THEN 'Проверен'
                                   ELSE 'Загружен'
                               END AS Status
                        FROM DocumentTypes dt
                        LEFT JOIN PatientDocuments pd ON dt.DocumentTypeID = pd.DocumentTypeID AND pd.PatientID = @PatientID
                        WHERE dt.ForAccompanyingPerson = 0
                          AND @PatientAge BETWEEN dt.MinimumAge AND dt.MaximumAge
                        ORDER BY dt.IsRequired DESC, dt.DocumentName";
                        
                        SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                        adapter.SelectCommand.Parameters.AddWithValue("@PatientID", id);
                        adapter.SelectCommand.Parameters.AddWithValue("@PatientAge", patientAge);
                        adapter.Fill(dt);
                }
                
                // Рассчитываем статус документов (полный/неполный комплект)
                int totalRequired = 0;
                int uploadedRequired = 0;
                string documentStatus = "Неполный комплект";
                
                foreach (DataRow row in dt.Rows)
                {
                    bool isRequired = Convert.ToBoolean(row["IsRequired"]);
                    string status = row["Status"].ToString();
                    
                    if (isRequired)
                    {
                        totalRequired++;
                        if (status == "Загружен" || status == "Проверен")
                        {
                            uploadedRequired++;
                        }
                    }
                }
                
                if (totalRequired == 0)
                {
                    documentStatus = "Нет обязательных документов";
                }
                else if (uploadedRequired == totalRequired)
                {
                    documentStatus = "Полный комплект";
                }
                else
                {
                    documentStatus = $"Неполный комплект ({uploadedRequired}/{totalRequired})";
                }
                
                return Ok(new { 
                    success = true, 
                    documents = DataTableToList(dt), 
                    patientAge = patientAge,
                    dateOfBirth = dateOfBirth,
                    documentStatus = documentStatus,
                    totalRequired = totalRequired,
                    uploadedRequired = uploadedRequired
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении документов пациента: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/manager/patient/{id}/document
        [HttpPost("patient/{id}/document")]
        public IActionResult UploadPatientDocument(int id, [FromForm] IFormFile file, [FromForm] int documentTypeID, [FromForm] string notes = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Файл не выбран или пустой." });
                }
                
                // Проверяем, существует ли пациент
                string patientName = string.Empty;
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string patientQuery = "SELECT FullName FROM Patients WHERE PatientID = @PatientID";
                    using (SqlCommand cmd = new SqlCommand(patientQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@PatientID", id);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return NotFound(new { success = false, message = "Пациент не найден." });
                        }
                        patientName = result.ToString();
                    }
                }
                
                // Получаем название типа документа
                string documentName = string.Empty;
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string docTypeQuery = "SELECT DocumentName FROM DocumentTypes WHERE DocumentTypeID = @DocumentTypeID";
                    using (SqlCommand cmd = new SqlCommand(docTypeQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentTypeID", documentTypeID);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return NotFound(new { success = false, message = "Тип документа не найден." });
                        }
                        documentName = result.ToString();
                    }
                }
                
                // Проверяем, есть ли уже загруженный документ
                string existingDocumentPath = null;
                int? existingDocumentID = null;
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string checkQuery = @"
                        SELECT DocumentID, DocumentPath 
                        FROM PatientDocuments 
                        WHERE PatientID = @PatientID AND DocumentTypeID = @DocumentTypeID";
                        
                        using (SqlCommand cmd = new SqlCommand(checkQuery, con))
                        {
                            cmd.Parameters.AddWithValue("@PatientID", id);
                            cmd.Parameters.AddWithValue("@DocumentTypeID", documentTypeID);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    existingDocumentID = reader.GetInt32(0);
                                    existingDocumentPath = reader.GetString(1);
                                }
                            }
                        }
                }
                
                // Получаем расширение файла и создаем безопасное имя файла
                string fileExtension = Path.GetExtension(file.FileName);
                string safeDocumentName = documentName.Replace('/', '_').Replace('\\', '_')
                    .Replace(':', '_').Replace('*', '_').Replace('?', '_')
                    .Replace('"', '_').Replace('<', '_').Replace('>', '_')
                    .Replace('|', '_');
                
                // Создаем директорию для документов пациентов
                string baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Documents", "Пациенты");
                Directory.CreateDirectory(baseDirectory);
                
                // Создаем директорию для конкретного пациента
                string patientFolder = Path.Combine(baseDirectory, id.ToString() + "_" + patientName.Replace(' ', '_'));
                Directory.CreateDirectory(patientFolder);
                
                // Создаем уникальное имя файла
                string newFileName = $"{safeDocumentName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}{fileExtension}";
                string destinationPath = Path.Combine(patientFolder, newFileName);
                
                // Сохраняем файл
                using (var stream = new FileStream(destinationPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                
                // Сохраняем данные в БД
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    if (existingDocumentID.HasValue)
                    {
                        // Удаляем старый файл
                        if (!string.IsNullOrEmpty(existingDocumentPath) && System.IO.File.Exists(existingDocumentPath))
                        {
                            try
                            {
                                System.IO.File.Delete(existingDocumentPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Ошибка при удалении старого файла: {ex.Message}");
                            }
                        }
                        
                        // Обновляем запись в БД
                        string updateQuery = @"
                            UPDATE PatientDocuments 
                            SET DocumentPath = @DocumentPath, 
                                UploadDate = GETDATE(), 
                                IsVerified = 0,
                                Notes = @Notes
                            WHERE DocumentID = @DocumentID";
                            
                            using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.Parameters.AddWithValue("@DocumentID", existingDocumentID.Value);
                                cmd.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                                cmd.ExecuteNonQuery();
                            }
                    }
                    else
                    {
                        // Создаем новую запись в БД
                        string insertQuery = @"
                            INSERT INTO PatientDocuments (PatientID, DocumentTypeID, DocumentPath, UploadDate, IsVerified, Notes)
                            VALUES (@PatientID, @DocumentTypeID, @DocumentPath, GETDATE(), 0, @Notes);
                            SELECT SCOPE_IDENTITY();";
                            
                            int newDocumentID;
                            using (SqlCommand cmd = new SqlCommand(insertQuery, con))
                            {
                                cmd.Parameters.AddWithValue("@PatientID", id);
                                cmd.Parameters.AddWithValue("@DocumentTypeID", documentTypeID);
                                cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                                cmd.Parameters.AddWithValue("@Notes", string.IsNullOrEmpty(notes) ? (object)DBNull.Value : notes);
                                newDocumentID = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                    }
                }
                
                return Ok(new { success = true, message = "Документ успешно загружен." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке документа пациента: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/patient/document/{id}/view
        [HttpGet("patient/document/{id}/view")]
        public IActionResult ViewPatientDocument(int id)
        {
            try
            {
                string documentPath = string.Empty;
                string documentName = string.Empty;
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT pd.DocumentPath, dt.DocumentName 
                        FROM PatientDocuments pd
                        JOIN DocumentTypes dt ON pd.DocumentTypeID = dt.DocumentTypeID
                        WHERE pd.DocumentID = @DocumentID";
                        
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@DocumentID", id);
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    documentPath = reader.GetString(0);
                                    documentName = reader.GetString(1);
                                }
                                else
                                {
                                    return NotFound(new { success = false, message = "Документ не найден." });
                                }
                            }
                        }
                }
                
                if (!System.IO.File.Exists(documentPath))
                {
                    return NotFound(new { success = false, message = "Файл документа не найден." });
                }
                
                var fileExtension = Path.GetExtension(documentPath).ToLowerInvariant();
                string contentType;
                
                switch (fileExtension)
                {
                    case ".pdf":
                        contentType = "application/pdf";
                        break;
                    case ".jpg":
                    case ".jpeg":
                        contentType = "image/jpeg";
                        break;
                    case ".png":
                        contentType = "image/png";
                        break;
                    default:
                        contentType = "application/octet-stream";
                        break;
                }
                
                var fileBytes = System.IO.File.ReadAllBytes(documentPath);
                string fileName = Path.GetFileName(documentPath);
                
                // Создаем FileContentResult вместо использования метода File
                return new FileContentResult(fileBytes, contentType)
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при просмотре документа пациента: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/manager/patient/document/{id}
        [HttpDelete("patient/document/{id}")]
        public IActionResult DeletePatientDocument(int id)
        {
            try
            {
                string documentPath = string.Empty;
                
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    
                    // Получаем путь к файлу
                    string pathQuery = "SELECT DocumentPath FROM PatientDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(pathQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", id);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                        {
                            return NotFound(new { success = false, message = "Документ не найден." });
                        }
                        documentPath = result.ToString();
                    }
                    
                    // Удаляем запись из БД
                    string deleteQuery = "DELETE FROM PatientDocuments WHERE DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(deleteQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", id);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            return NotFound(new { success = false, message = "Документ не найден." });
                        }
                    }
                }
                
                // Удаляем файл, если он существует
                if (!string.IsNullOrEmpty(documentPath) && System.IO.File.Exists(documentPath))
                {
                    System.IO.File.Delete(documentPath);
                }
                
                return Ok(new { success = true, message = "Документ успешно удален." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении документа пациента: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Документы сопровождающих лиц

        // GET: api/manager/accompanyingperson/{id}/documents
        [HttpGet("accompanyingperson/{id}/documents")]
        public IActionResult GetAccompanyingPersonDocuments(int id)
        {
            try
            {
                // Получаем информацию о сопровождающем и связанном пациенте
                string accompanyingPersonName = string.Empty;
                string patientName = string.Empty;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string personInfoQuery = @"
                        SELECT ap.FullName AS AccompanyingPersonName, p.FullName AS PatientName
                        FROM AccompanyingPersons ap
                        JOIN Patients p ON ap.PatientID = p.PatientID
                        WHERE ap.AccompanyingPersonID = @AccompanyingPersonID";
                    using (SqlCommand cmd = new SqlCommand(personInfoQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                accompanyingPersonName = reader["AccompanyingPersonName"].ToString();
                                patientName = reader["PatientName"].ToString();
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Сопровождающее лицо не найдено." });
                            }
                        }
                    }
                }

                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT dt.DocumentTypeID, dt.DocumentName, dt.IsRequired,
                               apd.DocumentID, apd.DocumentPath, apd.UploadDate, apd.IsVerified, apd.Notes,
                               CASE 
                                   WHEN apd.DocumentID IS NULL THEN 'Не загружен'
                                   WHEN apd.IsVerified = 1 THEN 'Проверен'
                                   ELSE 'Загружен'
                               END AS Status
                        FROM DocumentTypes dt
                        LEFT JOIN AccompanyingPersonDocuments apd ON dt.DocumentTypeID = apd.DocumentTypeID AND apd.AccompanyingPersonID = @AccompanyingPersonID
                        WHERE dt.ForAccompanyingPerson = 1
                        ORDER BY dt.IsRequired DESC, dt.DocumentName";
                        
                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue("@AccompanyingPersonID", id);
                    adapter.Fill(dt);
                }
                
                int totalRequired = 0;
                int uploadedRequired = 0;
                string documentStatus = "Неполный комплект";
                
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToBoolean(row["IsRequired"]))
                    {
                        totalRequired++;
                        if (row["Status"].ToString() == "Загружен" || row["Status"].ToString() == "Проверен")
                        {
                            uploadedRequired++;
                        }
                    }
                }
                
                if (totalRequired == 0) documentStatus = "Нет обязательных документов";
                else if (uploadedRequired == totalRequired) documentStatus = "Полный комплект";
                else documentStatus = $"Неполный комплект ({uploadedRequired}/{totalRequired})";
                
                return Ok(new { 
                    success = true, 
                    documents = DataTableToList(dt), 
                    accompanyingPersonName,
                    patientName,
                    documentStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: api/manager/accompanyingperson/{id}/document
        [HttpPost("accompanyingperson/{id}/document")]
        public IActionResult UploadAccompanyingPersonDocument(int id, [FromForm] IFormFile file, [FromForm] int documentTypeID, [FromForm] string notes = null)
        {
            try
            {
                if (file == null || file.Length == 0) return BadRequest(new { success = false, message = "Файл не выбран." });

                string accompanyingPersonName = string.Empty;
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT FullName FROM AccompanyingPersons WHERE AccompanyingPersonID = @AccompanyingPersonID", con))
                    {
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                        object result = cmd.ExecuteScalar();
                        if (result == null) return NotFound(new { success = false, message = "Сопровождающее лицо не найдено." });
                        accompanyingPersonName = result.ToString();
                    }
                }

                string documentTypeName = string.Empty;
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DocumentName FROM DocumentTypes WHERE DocumentTypeID = @DocumentTypeID AND ForAccompanyingPerson = 1", con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentTypeID", documentTypeID);
                        object result = cmd.ExecuteScalar();
                        if (result == null) return NotFound(new { success = false, message = "Тип документа не найден или не предназначен для сопровождающих." });
                        documentTypeName = result.ToString();
                    }
                }

                string baseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Documents", "Сопровождающие");
                Directory.CreateDirectory(baseDirectory);
                string personFolder = Path.Combine(baseDirectory, id.ToString() + "_" + accompanyingPersonName.Replace(' ', '_').Replace(".", ""));
                Directory.CreateDirectory(personFolder);

                string fileExtension = Path.GetExtension(file.FileName);
                string safeDocumentTypeName = documentTypeName.Replace('/', '_').Replace('\\', '_');
                string newFileName = $"{safeDocumentTypeName}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                string destinationPath = Path.Combine(personFolder, newFileName);

                using (var stream = new FileStream(destinationPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string existingDocumentPath = null;
                    int? existingDocumentID = null;

                    using (SqlCommand cmd = new SqlCommand("SELECT DocumentID, DocumentPath FROM AccompanyingPersonDocuments WHERE AccompanyingPersonID = @AccompanyingPersonID AND DocumentTypeID = @DocumentTypeID", con))
                    {
                        cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                        cmd.Parameters.AddWithValue("@DocumentTypeID", documentTypeID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                existingDocumentID = reader.GetInt32(0);
                                existingDocumentPath = reader.IsDBNull(1) ? null : reader.GetString(1);
                            }
                        }
                    }

                    if (existingDocumentID.HasValue)
                    {
                        if (!string.IsNullOrEmpty(existingDocumentPath) && System.IO.File.Exists(existingDocumentPath) && existingDocumentPath != destinationPath)
                        {
                            try { System.IO.File.Delete(existingDocumentPath); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить старый файл: {ex.Message}"); }
                        }
                        using (SqlCommand cmd = new SqlCommand("UPDATE AccompanyingPersonDocuments SET DocumentPath = @DocumentPath, UploadDate = GETDATE(), IsVerified = 0, Notes = @Notes WHERE DocumentID = @DocumentID", con))
                        {
                            cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                            cmd.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DocumentID", existingDocumentID.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (SqlCommand cmd = new SqlCommand("INSERT INTO AccompanyingPersonDocuments (AccompanyingPersonID, DocumentTypeID, DocumentPath, UploadDate, IsVerified, Notes) VALUES (@AccompanyingPersonID, @DocumentTypeID, @DocumentPath, GETDATE(), 0, @Notes)", con))
                        {
                            cmd.Parameters.AddWithValue("@AccompanyingPersonID", id);
                            cmd.Parameters.AddWithValue("@DocumentTypeID", documentTypeID);
                            cmd.Parameters.AddWithValue("@DocumentPath", destinationPath);
                            cmd.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                return Ok(new { success = true, message = "Документ успешно загружен." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // GET: api/manager/accompanyingperson/document/{documentId}/view
        [HttpGet("accompanyingperson/document/{documentId}/view")]
        public IActionResult ViewAccompanyingPersonDocument(int documentId)
        {
            try
            {
                string documentPath = string.Empty;
                string documentName = string.Empty;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT apd.DocumentPath, dt.DocumentName 
                        FROM AccompanyingPersonDocuments apd
                        JOIN DocumentTypes dt ON apd.DocumentTypeID = dt.DocumentTypeID
                        WHERE apd.DocumentID = @DocumentID";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", documentId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                documentPath = reader.GetString(0);
                                documentName = reader.GetString(1);
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Документ не найден." });
                            }
                        }
                    }
                }

                if (!System.IO.File.Exists(documentPath))
                {
                    return NotFound(new { success = false, message = "Файл документа не найден на сервере." });
                }

                var fileExtension = Path.GetExtension(documentPath).ToLowerInvariant().TrimStart('.');
                string contentType = GetMimeType(fileExtension);
                var fileBytes = System.IO.File.ReadAllBytes(documentPath);
                string fileName = Path.GetFileName(documentPath); 

                return new FileContentResult(fileBytes, contentType)
                {
                    FileDownloadName = fileName // Suggests download but browser can display inline if capable
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // DELETE: api/manager/accompanyingperson/document/{documentId}
        [HttpDelete("accompanyingperson/document/{documentId}")]
        public IActionResult DeleteAccompanyingPersonDocument(int documentId)
        {
            try
            {
                string documentPath = string.Empty;
                int accompanyingPersonID = 0;

                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DocumentPath, AccompanyingPersonID FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID", con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", documentId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                documentPath = reader.GetString(0);
                                accompanyingPersonID = reader.GetInt32(1);
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "Документ не найден." });
                            }
                        }
                    }

                    using (SqlCommand cmd = new SqlCommand("DELETE FROM AccompanyingPersonDocuments WHERE DocumentID = @DocumentID", con))
                    {
                        cmd.Parameters.AddWithValue("@DocumentID", documentId);
                        cmd.ExecuteNonQuery();
                    }
                }

                if (!string.IsNullOrEmpty(documentPath) && System.IO.File.Exists(documentPath))
                {
                    try { System.IO.File.Delete(documentPath); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить файл: {ex.Message}");}
                }
                
                // Проверяем, остались ли другие документы у этого сопровождающего в этой же папке
                // Если папка пуста, удаляем её
                if (accompanyingPersonID > 0 && !string.IsNullOrEmpty(documentPath))
                {
                    string personFolderPath = Path.GetDirectoryName(documentPath);
                    if (Directory.Exists(personFolderPath) && !Directory.EnumerateFileSystemEntries(personFolderPath).Any())
                    {
                        try { Directory.Delete(personFolderPath); } catch (Exception ex) { Console.WriteLine($"Не удалось удалить папку сопровождающего: {ex.Message}");}
                    }
                }

                return Ok(new { success = true, message = "Документ успешно удален." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
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
        public int? CurrentPatientID { get; set; }
    }

    public class AccompanyingPersonModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Relationship { get; set; }
        public bool HasPowerOfAttorney { get; set; }
        public bool NeedAccommodation { get; set; }
        public AccommodationInfoModel AccommodationInfo { get; set; }
    }

    #endregion
} 