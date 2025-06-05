using System;
using Xunit;

namespace TestDubRosh
{
    public class PatientManagementTests
    {
        [Fact]
        public void GetPatientById_ShouldReturnCorrectData()
        {
            // Arrange
            int patientId = 1;
            var patientService = new PatientService();

            // Act
            var patient = patientService.GetPatientById(patientId);

            // Assert
            Assert.NotNull(patient);
            Assert.Equal(patientId, patient.PatientID);
            Assert.Equal("Смирнов Алексей Петрович", patient.FullName);
        }

        [Fact]
        public void GetPatientById_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            int invalidPatientId = -1;
            var patientService = new PatientService();

            // Act
            var patient = patientService.GetPatientById(invalidPatientId);

            // Assert
            Assert.Null(patient);
        }

        [Fact]
        public void GetPatientsByDoctorId_ShouldReturnPatients()
        {
            // Arrange
            int doctorId = 1; // ID врача "Иванов Иван Иванович"
            var patientService = new PatientService();

            // Act
            var patients = patientService.GetPatientsByDoctorId(doctorId);

            // Assert
            Assert.NotEmpty(patients);
            Assert.Equal("Смирнов Алексей Петрович", patients[0].FullName);
        }

        [Fact]
        public void AddNewPatient_ShouldAddPatientAndReturnId()
        {
            // Arrange
            var newPatient = new PatientModel
            {
                FullName = "Новый Пациент Тестовый",
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = "Мужской",
                StayType = "Круглосуточный"
            };
            var patientService = new PatientService();

            // Act
            int newPatientId = patientService.AddPatient(newPatient);

            // Assert
            Assert.True(newPatientId > 0);
        }

        [Fact]
        public void UpdatePatient_ShouldUpdatePatientData()
        {
            // Arrange
            int patientId = 1;
            var updatedInfo = new PatientModel
            {
                PatientID = patientId,
                FullName = "Смирнов Алексей Петрович",
                Gender = "Мужской",
                DateOfBirth = new DateTime(2006, 8, 18),
                StayType = "Дневной" // Изменено с "Круглосуточный"
            };
            var patientService = new PatientService();

            // Act
            bool result = patientService.UpdatePatient(updatedInfo);

            // Assert
            Assert.True(result);

            // Verify the update
            var patient = patientService.GetPatientById(patientId);
            Assert.Equal("Дневной", patient.StayType);
        }
    }
} 