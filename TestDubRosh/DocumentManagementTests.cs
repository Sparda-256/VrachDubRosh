using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace TestDubRosh
{
    public class DocumentManagementTests
    {
        [Fact]
        public void GetRequiredDocumentsByAge_Should_ReturnCorrectDocuments_ForChild()
        {
            // Arrange
            DateTime birthDate = DateTime.Now.AddYears(-12); // 12-летний ребенок
            var documentService = new DocumentService();

            // Act
            var requiredDocs = documentService.GetRequiredDocumentsByAge(birthDate);

            // Assert
            Assert.Contains(requiredDocs, d => d.DocumentTypeName == "Свидетельство о рождении");
            Assert.Contains(requiredDocs, d => d.DocumentTypeName == "Прививочный сертификат");
        }

        [Fact]
        public void GetRequiredDocumentsByAge_Should_ReturnCorrectDocuments_ForAdult()
        {
            // Arrange
            DateTime birthDate = DateTime.Now.AddYears(-25); // 25-летний взрослый
            var documentService = new DocumentService();

            // Act
            var requiredDocs = documentService.GetRequiredDocumentsByAge(birthDate);

            // Assert
            Assert.Contains(requiredDocs, d => d.DocumentTypeName == "Паспорт");
            Assert.DoesNotContain(requiredDocs, d => d.DocumentTypeName == "Прививочный сертификат");
        }

        [Fact]
        public void GetPatientDocuments_ShouldReturnExistingDocuments()
        {
            // Arrange
            int patientId = 1;
            var documentService = new DocumentService();

            // Act
            var documents = documentService.GetPatientDocuments(patientId);

            // Assert
            Assert.NotEmpty(documents);
        }

        [Fact]
        public void UploadPatientDocument_ShouldUploadSuccessfully()
        {
            // Arrange
            int patientId = 1;
            string documentType = "Паспорт";
            string filePath = "test_document.pdf"; // Имитируем путь к файлу
            var documentService = new DocumentService();

            // Act
            bool result = documentService.UploadPatientDocument(patientId, documentType, filePath);

            // Assert
            Assert.True(result);
            
            // Проверим, что документ добавился
            var documents = documentService.GetPatientDocuments(patientId);
            Assert.Contains(documents, d => d.DocumentTypeName == documentType);
        }

        [Fact]
        public void UploadAccompanyingDocument_ShouldUploadSuccessfully()
        {
            // Arrange
            int accompanyingId = 1;
            string documentType = "Паспорт";
            string filePath = "test_document.pdf"; // Имитируем путь к файлу
            var documentService = new DocumentService();

            // Act
            bool result = documentService.UploadAccompanyingDocument(accompanyingId, documentType, filePath);

            // Assert
            Assert.True(result);
            
            // Проверим, что документ добавился
            var documents = documentService.GetAccompanyingDocuments(accompanyingId);
            Assert.Contains(documents, d => d.DocumentTypeName == documentType);
        }

        [Fact]
        public void VerifyDocument_ShouldVerifySuccessfully()
        {
            // Arrange
            int documentId = 1;
            var documentService = new DocumentService();

            // Act
            bool result = documentService.VerifyDocument(documentId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DeleteDocument_ShouldDeleteSuccessfully()
        {
            // Arrange
            int documentId = 2;
            var documentService = new DocumentService();

            // Act
            bool result = documentService.DeleteDocument(documentId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetDocumentCompletionStatus_ShouldReturnCorrectStatus()
        {
            // Arrange
            int patientId = 1;
            var documentService = new DocumentService();
            
            // Добавляем все необходимые документы для полноты статуса (для возраста 17 лет)
            documentService.UploadPatientDocument(patientId, "Свидетельство о рождении", "test_birth.pdf");
            documentService.UploadPatientDocument(patientId, "Паспорт", "test_passport.pdf");
            documentService.UploadPatientDocument(patientId, "СНИЛС", "test_snils.pdf");
            documentService.UploadPatientDocument(patientId, "Полис ОМС", "test_polis.pdf");
            documentService.UploadPatientDocument(patientId, "Прививочный сертификат", "test_vaccine.pdf");
            documentService.UploadPatientDocument(patientId, "Направление формы 057-У", "test_form.pdf");
            documentService.UploadPatientDocument(patientId, "Справка от дерматолога", "test_derma.pdf");
            documentService.UploadPatientDocument(patientId, "Справка об отсутствии противопоказаний", "test_contra.pdf");

            // Act
            var status = documentService.GetDocumentCompletionStatus(patientId);

            // Assert
            Assert.True(status.IsComplete);
            Assert.Equal(100, status.CompletionPercentage);
        }
    }
} 