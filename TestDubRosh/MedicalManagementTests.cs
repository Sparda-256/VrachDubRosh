using System;
using System.Collections.Generic;
using Xunit;

namespace TestDubRosh
{
    public class MedicalManagementTests
    {
        [Fact]
        public void AddDiagnosis_ShouldReturnSuccess()
        {
            // Arrange
            int patientId = 1;
            string diagnosisName = "J00 Острый назофарингит (насморк)";
            var medicalService = new MedicalService();

            // Act
            bool result = medicalService.AddDiagnosis(patientId, diagnosisName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AddDuplicateDiagnosis_ShouldReturnFalse()
        {
            // Arrange
            int patientId = 1;
            string diagnosisName = "J00 Острый назофарингит (насморк)";
            var medicalService = new MedicalService();
            
            // Сначала добавляем диагноз
            medicalService.AddDiagnosis(patientId, diagnosisName);

            // Act - пытаемся добавить тот же диагноз снова
            bool result = medicalService.AddDiagnosis(patientId, diagnosisName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetPatientDiagnoses_ShouldReturnDiagnoses()
        {
            // Arrange
            int patientId = 1;
            var medicalService = new MedicalService();
            
            // Добавляем диагноз
            medicalService.AddDiagnosis(patientId, "J00 Острый назофарингит (насморк)");

            // Act
            var diagnoses = medicalService.GetPatientDiagnoses(patientId);

            // Assert
            Assert.NotEmpty(diagnoses);
            Assert.Contains(diagnoses, d => d.DiagnosisName == "J00 Острый назофарингит (насморк)");
        }

        [Fact]
        public void RecordMeasurements_ShouldReturnSuccess()
        {
            // Arrange
            int patientId = 1;
            var measurements = new MeasurementModel
            {
                PatientID = patientId,
                Height = 170,
                Weight = 65,
                SystolicPressure = 120,
                DiastolicPressure = 80,
                MeasurementType = "Поступление"
            };
            var medicalService = new MedicalService();

            // Act
            bool result = medicalService.RecordMeasurements(measurements);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetPatientMeasurements_ShouldReturnAllTypes()
        {
            // Arrange
            int patientId = 1;
            var medicalService = new MedicalService();
            
            // Добавляем измерения при поступлении
            medicalService.RecordMeasurements(new MeasurementModel 
            { 
                PatientID = patientId,
                Height = 170,
                Weight = 65,
                SystolicPressure = 120,
                DiastolicPressure = 80,
                MeasurementType = "Поступление" 
            });
            
            // Добавляем измерения при выписке
            medicalService.RecordMeasurements(new MeasurementModel 
            { 
                PatientID = patientId,
                Height = 170,
                Weight = 64,
                SystolicPressure = 118,
                DiastolicPressure = 78,
                MeasurementType = "Выписка" 
            });

            // Act
            var measurements = medicalService.GetPatientMeasurements(patientId);

            // Assert
            Assert.Equal(2, measurements.Count);
            Assert.Contains(measurements, m => m.MeasurementType == "Поступление");
            Assert.Contains(measurements, m => m.MeasurementType == "Выписка");
        }

        [Fact]
        public void CreateDischargeDocument_ShouldReturnSuccess()
        {
            // Arrange
            int patientId = 1;
            var dischargeData = new DischargeDocumentModel
            {
                PatientID = patientId,
                Complaints = "Боль в коленном суставе",
                DiseaseHistory = "Травма во время занятий спортом",
                InitialCondition = "Удовлетворительное",
                RehabilitationGoal = "Восстановление двигательной функции",
                GoalAchieved = true,
                Recommendations = "Продолжить щадящие физические упражнения дома"
            };
            var medicalService = new MedicalService();

            // Act
            bool result = medicalService.CreateDischargeDocument(dischargeData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetDischargeDocument_AfterCreation_ShouldReturnDocument()
        {
            // Arrange
            int patientId = 1;
            var dischargeData = new DischargeDocumentModel
            {
                PatientID = patientId,
                Complaints = "Боль в коленном суставе",
                DiseaseHistory = "Травма во время занятий спортом",
                InitialCondition = "Удовлетворительное",
                RehabilitationGoal = "Восстановление двигательной функции",
                GoalAchieved = true,
                Recommendations = "Продолжить щадящие физические упражнения дома"
            };
            var medicalService = new MedicalService();
            
            // Создаем выписной эпикриз
            medicalService.CreateDischargeDocument(dischargeData);

            // Act
            var document = medicalService.GetDischargeDocument(patientId);

            // Assert
            Assert.NotNull(document);
            Assert.Equal("Боль в коленном суставе", document.Complaints);
            Assert.True(document.GoalAchieved);
        }

        [Fact]
        public void AssignProcedureToMedCard_ShouldReturnSuccess()
        {
            // Arrange
            int patientId = 1;
            int doctorId = 1;
            string procedureName = "ЛФК";
            string procedureDetails = "10 занятий, с интервалом 2 дня";
            var medicalService = new MedicalService();

            // Act
            bool result = medicalService.AssignProcedureToMedCard(patientId, doctorId, procedureName, procedureDetails);

            // Assert
            Assert.True(result);
        }
    }
} 