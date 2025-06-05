using System;
using Xunit;
using System.Collections.Generic;

namespace TestDubRosh
{
    public class ProcedureManagementTests
    {
        [Fact]
        public void GetAllProcedures_ShouldReturnProcedures()
        {
            // Arrange
            var procedureService = new ProcedureService();

            // Act
            var procedures = procedureService.GetAllProcedures();

            // Assert
            Assert.NotEmpty(procedures);
            Assert.Equal(3, procedures.Count); // Ожидается 3 тестовые процедуры
        }

        [Fact]
        public void GetProceduresByDoctor_ShouldReturnProcedures()
        {
            // Arrange
            int doctorId = 1;
            var procedureService = new ProcedureService();

            // Act
            var procedures = procedureService.GetProceduresByDoctorId(doctorId);

            // Assert
            Assert.NotEmpty(procedures);
            Assert.Contains(procedures, p => p.ProcedureName == "ЛФК");
        }

        [Fact]
        public void AddProcedure_ShouldAddSuccessfully()
        {
            // Arrange
            var newProcedure = new ProcedureModel
            {
                ProcedureName = "Новая тестовая процедура",
                DurationMinutes = 45
            };
            var procedureService = new ProcedureService();

            // Act
            int procedureId = procedureService.AddProcedure(newProcedure);

            // Assert
            Assert.True(procedureId > 0);
        }

        [Fact]
        public void AssignProcedureToPatient_ShouldAssignSuccessfully()
        {
            // Arrange
            int patientId = 1;
            int procedureId = 1;
            int doctorId = 1;
            var appointmentDate = DateTime.Now.Date.AddDays(1);
            var appointmentTime = new TimeSpan(14, 30, 0); // 14:30
            
            var procedureService = new ProcedureService();

            // Act
            bool result = procedureService.AssignProcedureToPatient(patientId, procedureId, doctorId, appointmentDate, appointmentTime);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetAppointmentsByPatient_ShouldReturnAppointments()
        {
            // Arrange
            int patientId = 1;
            var procedureService = new ProcedureService();

            // Сначала добавим процедуру пациенту
            procedureService.AssignProcedureToPatient(
                patientId, 
                1,  // procedureId
                1,  // doctorId
                DateTime.Now.Date, 
                new TimeSpan(10, 0, 0)); // 10:00

            // Act
            var appointments = procedureService.GetAppointmentsByPatientId(patientId);

            // Assert
            Assert.NotEmpty(appointments);
            Assert.Equal(patientId, appointments[0].PatientID);
        }

        [Fact]
        public void CreateWeeklySchedule_ShouldCreateSuccessfully()
        {
            // Arrange
            int patientId = 1;
            int procedureId = 1;
            int doctorId = 1;
            var startDate = DateTime.Now.Date;
            var endDate = startDate.AddDays(14);
            var selectedDays = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
            var appointmentTime = new TimeSpan(10, 0, 0); // 10:00
            
            var procedureService = new ProcedureService();

            // Act
            bool result = procedureService.CreateWeeklySchedule(
                patientId, procedureId, doctorId,
                startDate, endDate, selectedDays, appointmentTime);

            // Assert
            Assert.True(result);
            
            // Проверим, что назначения были созданы
            var appointments = procedureService.GetAppointmentsByPatientId(patientId);
            Assert.NotEmpty(appointments);
        }
    }
} 