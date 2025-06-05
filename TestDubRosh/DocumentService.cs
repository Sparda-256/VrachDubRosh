using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestDubRosh
{
    public class DocumentTypeModel
    {
        public int DocumentTypeID { get; set; }
        public string DocumentTypeName { get; set; }
        public bool IsRequired { get; set; }
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
    }

    public class DocumentModel
    {
        public int DocumentID { get; set; }
        public int? PatientID { get; set; }
        public int? AccompanyingID { get; set; }
        public string DocumentTypeName { get; set; }
        public string FilePath { get; set; }
        public bool IsVerified { get; set; }
        public DateTime UploadDate { get; set; }
    }

    public class DocumentCompletionStatus
    {
        public bool IsComplete { get; set; }
        public int CompletionPercentage { get; set; }
        public List<string> MissingDocuments { get; set; }
    }

    public class DocumentService
    {
        private List<DocumentTypeModel> _documentTypes;
        private List<DocumentModel> _documents;
        private int _nextDocumentId = 3;

        public DocumentService()
        {
            // Инициализация типов документов
            _documentTypes = new List<DocumentTypeModel>
            {
                new DocumentTypeModel { DocumentTypeID = 1, DocumentTypeName = "Паспорт", IsRequired = true, MinAge = 14, MaxAge = 150 },
                new DocumentTypeModel { DocumentTypeID = 2, DocumentTypeName = "Свидетельство о рождении", IsRequired = true, MinAge = 0, MaxAge = 14 },
                new DocumentTypeModel { DocumentTypeID = 3, DocumentTypeName = "Полис ОМС", IsRequired = true, MinAge = 0, MaxAge = 150 },
                new DocumentTypeModel { DocumentTypeID = 4, DocumentTypeName = "СНИЛС", IsRequired = true, MinAge = 0, MaxAge = 150 },
                new DocumentTypeModel { DocumentTypeID = 5, DocumentTypeName = "Прививочный сертификат", IsRequired = true, MinAge = 0, MaxAge = 18 },
                new DocumentTypeModel { DocumentTypeID = 6, DocumentTypeName = "Направление формы 057-У", IsRequired = true, MinAge = 0, MaxAge = 150 },
                new DocumentTypeModel { DocumentTypeID = 7, DocumentTypeName = "Справка от дерматолога", IsRequired = true, MinAge = 0, MaxAge = 150 },
                new DocumentTypeModel { DocumentTypeID = 8, DocumentTypeName = "Справка об отсутствии противопоказаний", IsRequired = true, MinAge = 0, MaxAge = 150 },
                new DocumentTypeModel { DocumentTypeID = 9, DocumentTypeName = "Доверенность", IsRequired = false, MinAge = 0, MaxAge = 150 }
            };

            // Начальные документы
            _documents = new List<DocumentModel>
            {
                new DocumentModel
                {
                    DocumentID = 1,
                    PatientID = 1,
                    DocumentTypeName = "Свидетельство о рождении",
                    FilePath = "Documents/Пациенты/Смирнов Алексей Петрович/birth_certificate.pdf",
                    IsVerified = true,
                    UploadDate = DateTime.Now.AddDays(-5)
                },
                new DocumentModel
                {
                    DocumentID = 2,
                    AccompanyingID = 1,
                    DocumentTypeName = "Паспорт",
                    FilePath = "Documents/Сопровождающие/Смирнова Екатерина Алексеевна/passport.pdf",
                    IsVerified = false,
                    UploadDate = DateTime.Now.AddDays(-5)
                }
            };
        }

        public List<DocumentTypeModel> GetRequiredDocumentsByAge(DateTime birthDate)
        {
            int age = CalculateAge(birthDate);
            return _documentTypes
                .Where(dt => dt.IsRequired && age >= dt.MinAge && age <= dt.MaxAge)
                .ToList();
        }

        public List<DocumentModel> GetPatientDocuments(int patientId)
        {
            return _documents
                .Where(d => d.PatientID.HasValue && d.PatientID.Value == patientId)
                .ToList();
        }

        public List<DocumentModel> GetAccompanyingDocuments(int accompanyingId)
        {
            return _documents
                .Where(d => d.AccompanyingID.HasValue && d.AccompanyingID.Value == accompanyingId)
                .ToList();
        }

        public bool UploadPatientDocument(int patientId, string documentType, string filePath)
        {
            try
            {
                var document = new DocumentModel
                {
                    DocumentID = _nextDocumentId++,
                    PatientID = patientId,
                    DocumentTypeName = documentType,
                    FilePath = filePath,
                    IsVerified = false,
                    UploadDate = DateTime.Now
                };

                _documents.Add(document);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool UploadAccompanyingDocument(int accompanyingId, string documentType, string filePath)
        {
            try
            {
                var document = new DocumentModel
                {
                    DocumentID = _nextDocumentId++,
                    AccompanyingID = accompanyingId,
                    DocumentTypeName = documentType,
                    FilePath = filePath,
                    IsVerified = false,
                    UploadDate = DateTime.Now
                };

                _documents.Add(document);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool VerifyDocument(int documentId)
        {
            var document = _documents.FirstOrDefault(d => d.DocumentID == documentId);
            if (document == null)
                return false;

            document.IsVerified = true;
            return true;
        }

        public bool DeleteDocument(int documentId)
        {
            var document = _documents.FirstOrDefault(d => d.DocumentID == documentId);
            if (document == null)
                return false;

            _documents.Remove(document);
            return true;
        }

        public DocumentCompletionStatus GetDocumentCompletionStatus(int patientId)
        {
            var patient = new PatientModel { PatientID = patientId, DateOfBirth = new DateTime(2006, 8, 18) };
            var requiredDocs = GetRequiredDocumentsByAge(patient.DateOfBirth);
            var existingDocs = GetPatientDocuments(patientId);

            var missingDocs = new List<string>();
            foreach (var requiredDoc in requiredDocs)
            {
                if (!existingDocs.Any(d => d.DocumentTypeName == requiredDoc.DocumentTypeName))
                {
                    missingDocs.Add(requiredDoc.DocumentTypeName);
                }
            }

            int completionPercentage = requiredDocs.Count > 0
                ? (int)(100 * (requiredDocs.Count - missingDocs.Count) / (double)requiredDocs.Count)
                : 100;

            return new DocumentCompletionStatus
            {
                IsComplete = missingDocs.Count == 0,
                CompletionPercentage = completionPercentage,
                MissingDocuments = missingDocs
            };
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
                age--;
            return age;
        }
    }
} 