using System;
using System.Collections.Generic;
using System.Linq;

namespace TestDubRosh
{
    public class ProcedureModel
    {
        public int ProcedureID { get; set; }
        public string ProcedureName { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class ProcedureAppointmentModel
    {
        public int AppointmentID { get; set; }
        public int PatientID { get; set; }
        public int ProcedureID { get; set; }
        public int DoctorID { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string Status { get; set; } // Запланирована, Выполняется, Выполнена, Отменена
    }

    public class WeeklyScheduleModel
    {
        public int ScheduleID { get; set; }
        public int PatientID { get; set; }
        public int ProcedureID { get; set; }
        public int DoctorID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<DayOfWeek> SelectedDays { get; set; }
        public TimeSpan AppointmentTime { get; set; }
    }

    public class ProcedureService
    {
        private List<ProcedureModel> _procedures;
        private List<ProcedureAppointmentModel> _appointments;
        private List<WeeklyScheduleModel> _weeklySchedules;
        private int _nextProcedureId = 4;
        private int _nextAppointmentId = 1;
        private int _nextScheduleId = 1;

        public ProcedureService()
        {
            _procedures = new List<ProcedureModel>
            {
                new ProcedureModel { ProcedureID = 1, ProcedureName = "ЛФК", DurationMinutes = 30 },
                new ProcedureModel { ProcedureID = 2, ProcedureName = "Массаж", DurationMinutes = 45 },
                new ProcedureModel { ProcedureID = 3, ProcedureName = "Жемчужные ванны", DurationMinutes = 20 }
            };
            
            _appointments = new List<ProcedureAppointmentModel>();
            _weeklySchedules = new List<WeeklyScheduleModel>();
        }

        public List<ProcedureModel> GetAllProcedures()
        {
            return _procedures.ToList();
        }

        public List<ProcedureModel> GetProceduresByDoctorId(int doctorId)
        {
            // Для простоты: врач с ID=1 делает ЛФК и массаж
            if (doctorId == 1)
                return _procedures.Where(p => p.ProcedureID == 1 || p.ProcedureID == 2).ToList();
            
            // Врач с ID=2 делает жемчужные ванны
            if (doctorId == 2)
                return _procedures.Where(p => p.ProcedureID == 3).ToList();
            
            return new List<ProcedureModel>();
        }

        public int AddProcedure(ProcedureModel procedure)
        {
            procedure.ProcedureID = _nextProcedureId++;
            _procedures.Add(procedure);
            return procedure.ProcedureID;
        }

        public bool AssignProcedureToPatient(int patientId, int procedureId, int doctorId, 
            DateTime appointmentDate, TimeSpan appointmentTime)
        {
            // Проверка на конфликт расписания
            var conflictingAppointment = _appointments.FirstOrDefault(a => 
                a.DoctorID == doctorId && 
                a.AppointmentDate == appointmentDate &&
                a.AppointmentTime == appointmentTime);

            if (conflictingAppointment != null)
                return false;

            var procedure = _procedures.FirstOrDefault(p => p.ProcedureID == procedureId);
            if (procedure == null)
                return false;

            var appointment = new ProcedureAppointmentModel
            {
                AppointmentID = _nextAppointmentId++,
                PatientID = patientId,
                ProcedureID = procedureId,
                DoctorID = doctorId,
                AppointmentDate = appointmentDate,
                AppointmentTime = appointmentTime,
                Status = "Запланирована"
            };

            _appointments.Add(appointment);
            return true;
        }

        public List<ProcedureAppointmentModel> GetAppointmentsByPatientId(int patientId)
        {
            return _appointments.Where(a => a.PatientID == patientId).ToList();
        }

        public bool CreateWeeklySchedule(
            int patientId, int procedureId, int doctorId,
            DateTime startDate, DateTime endDate,
            List<DayOfWeek> selectedDays, TimeSpan appointmentTime)
        {
            try
            {
                var weeklySchedule = new WeeklyScheduleModel
                {
                    ScheduleID = _nextScheduleId++,
                    PatientID = patientId,
                    ProcedureID = procedureId,
                    DoctorID = doctorId,
                    StartDate = startDate,
                    EndDate = endDate,
                    SelectedDays = selectedDays,
                    AppointmentTime = appointmentTime
                };

                _weeklySchedules.Add(weeklySchedule);

                // Создаем отдельные назначения на основе расписания
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (selectedDays.Contains(date.DayOfWeek))
                    {
                        AssignProcedureToPatient(patientId, procedureId, doctorId, date, appointmentTime);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UpdateAppointmentStatus(int appointmentId, string newStatus)
        {
            var appointment = _appointments.FirstOrDefault(a => a.AppointmentID == appointmentId);
            if (appointment == null)
                return false;

            appointment.Status = newStatus;
            return true;
        }
    }
} 