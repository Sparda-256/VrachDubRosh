using System;
using System.Collections.Generic;

namespace TestDubRosh
{
    public class PatientModel
    {
        public int PatientID { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public DateTime RecordDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string StayType { get; set; }
    }

    public class PatientService
    {
        private Dictionary<int, PatientModel> _patients;

        public PatientService()
        {
            _patients = new Dictionary<int, PatientModel>();

            // Предварительное заполнение тестовыми данными
            _patients[1] = new PatientModel
            {
                PatientID = 1,
                FullName = "Смирнов Алексей Петрович",
                DateOfBirth = new DateTime(2006, 8, 18),
                Gender = "Мужской",
                RecordDate = new DateTime(2023, 5, 18),
                StayType = "Круглосуточный"
            };

            _patients[2] = new PatientModel
            {
                PatientID = 2,
                FullName = "Иванова Мария Сергеевна",
                DateOfBirth = new DateTime(2010, 3, 25),
                Gender = "Женский",
                RecordDate = new DateTime(2023, 4, 10),
                DischargeDate = new DateTime(2023, 4, 25),
                StayType = "Дневной"
            };
        }

        public PatientModel GetPatientById(int patientId)
        {
            if (patientId <= 0 || !_patients.ContainsKey(patientId))
                return null;

            return _patients[patientId];
        }

        public List<PatientModel> GetPatientsByDoctorId(int doctorId)
        {
            // Для простоты: врач с ID=1 лечит пациента с ID=1
            if (doctorId == 1)
                return new List<PatientModel> { _patients[1] };
            
            return new List<PatientModel>();
        }

        public int AddPatient(PatientModel patient)
        {
            int newId = _patients.Count + 1;
            
            patient.PatientID = newId;
            patient.RecordDate = DateTime.Now;
            
            _patients[newId] = patient;
            
            return newId;
        }

        public bool UpdatePatient(PatientModel updatedPatient)
        {
            if (!_patients.ContainsKey(updatedPatient.PatientID))
                return false;

            _patients[updatedPatient.PatientID] = updatedPatient;
            
            return true;
        }

        public bool DischargePatient(int patientId)
        {
            if (!_patients.ContainsKey(patientId))
                return false;

            _patients[patientId].DischargeDate = DateTime.Now;
            
            return true;
        }
    }
} 