using System;
using System.Collections.Generic;
using System.Linq;

namespace TestDubRosh
{
    public class DiagnosisModel
    {
        public int DiagnosisID { get; set; }
        public string DiagnosisName { get; set; }
    }

    public class PatientDiagnosisModel
    {
        public int PatientID { get; set; }
        public int DiagnosisID { get; set; }
        public string DiagnosisName { get; set; }
        public DateTime AssignmentDate { get; set; }
    }

    public class MeasurementModel
    {
        public int MeasurementID { get; set; }
        public int PatientID { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public int SystolicPressure { get; set; }
        public int DiastolicPressure { get; set; }
        public string MeasurementType { get; set; }  // Поступление, В процессе лечения, Выписка
        public DateTime MeasurementDate { get; set; }
    }

    public class DischargeDocumentModel
    {
        public int DocumentID { get; set; }
        public int PatientID { get; set; }
        public string Complaints { get; set; }
        public string DiseaseHistory { get; set; }
        public string InitialCondition { get; set; }
        public string RehabilitationGoal { get; set; }
        public bool GoalAchieved { get; set; }
        public string Recommendations { get; set; }
        public DateTime CreationDate { get; set; }
    }

    public class MedCardProcedureModel
    {
        public int EntryID { get; set; }
        public int PatientID { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; }
        public string ProcedureName { get; set; }
        public string ProcedureDetails { get; set; }
        public DateTime AssignmentDate { get; set; }
    }

    public class MedicalService
    {
        private List<DiagnosisModel> _diagnosesLibrary;
        private List<PatientDiagnosisModel> _patientDiagnoses;
        private List<MeasurementModel> _measurements;
        private List<DischargeDocumentModel> _dischargeDocuments;
        private List<MedCardProcedureModel> _medCardProcedures;
        
        private int _nextMeasurementId = 1;
        private int _nextDischargeDocId = 1;
        private int _nextMedCardProcId = 1;

        public MedicalService()
        {
            // Библиотека диагнозов
            _diagnosesLibrary = new List<DiagnosisModel>
            {
                new DiagnosisModel { DiagnosisID = 1, DiagnosisName = "J00 Острый назофарингит (насморк)" },
                new DiagnosisModel { DiagnosisID = 2, DiagnosisName = "J02 Острый фарингит" },
                new DiagnosisModel { DiagnosisID = 3, DiagnosisName = "J06.9 Острая инфекция верхних дыхательных путей" },
                new DiagnosisModel { DiagnosisID = 4, DiagnosisName = "M54.5 Боль внизу спины" },
                new DiagnosisModel { DiagnosisID = 5, DiagnosisName = "S83 Вывих, растяжение и повреждение суставов колена" }
            };

            _patientDiagnoses = new List<PatientDiagnosisModel>();
            _measurements = new List<MeasurementModel>();
            _dischargeDocuments = new List<DischargeDocumentModel>();
            _medCardProcedures = new List<MedCardProcedureModel>();
        }

        public bool AddDiagnosis(int patientId, string diagnosisName)
        {
            // Проверка на дубликат
            if (_patientDiagnoses.Any(pd => pd.PatientID == patientId && pd.DiagnosisName == diagnosisName))
                return false;

            var diagnosis = _diagnosesLibrary.FirstOrDefault(d => d.DiagnosisName == diagnosisName);
            
            if (diagnosis == null)
                return false;

            _patientDiagnoses.Add(new PatientDiagnosisModel
            {
                PatientID = patientId,
                DiagnosisID = diagnosis.DiagnosisID,
                DiagnosisName = diagnosis.DiagnosisName,
                AssignmentDate = DateTime.Now
            });

            return true;
        }

        public List<PatientDiagnosisModel> GetPatientDiagnoses(int patientId)
        {
            return _patientDiagnoses.Where(pd => pd.PatientID == patientId).ToList();
        }

        public bool RecordMeasurements(MeasurementModel measurements)
        {
            try
            {
                measurements.MeasurementID = _nextMeasurementId++;
                measurements.MeasurementDate = DateTime.Now;
                _measurements.Add(measurements);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<MeasurementModel> GetPatientMeasurements(int patientId)
        {
            return _measurements.Where(m => m.PatientID == patientId).ToList();
        }

        public bool CreateDischargeDocument(DischargeDocumentModel dischargeData)
        {
            try
            {
                dischargeData.DocumentID = _nextDischargeDocId++;
                dischargeData.CreationDate = DateTime.Now;
                _dischargeDocuments.Add(dischargeData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public DischargeDocumentModel GetDischargeDocument(int patientId)
        {
            return _dischargeDocuments.FirstOrDefault(d => d.PatientID == patientId);
        }

        public bool AssignProcedureToMedCard(int patientId, int doctorId, string procedureName, string procedureDetails)
        {
            try
            {
                string doctorName = doctorId == 1 ? "Иванов Иван Иванович" : "Петров Петр Петрович";

                _medCardProcedures.Add(new MedCardProcedureModel
                {
                    EntryID = _nextMedCardProcId++,
                    PatientID = patientId,
                    DoctorID = doctorId,
                    DoctorName = doctorName,
                    ProcedureName = procedureName,
                    ProcedureDetails = procedureDetails,
                    AssignmentDate = DateTime.Now
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<MedCardProcedureModel> GetMedCardProcedures(int patientId)
        {
            return _medCardProcedures.Where(p => p.PatientID == patientId).ToList();
        }
    }
} 