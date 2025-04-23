CREATE DATABASE PomoshnikPolicliniki2

USE PomoshnikPolicliniki2

-- Создание таблицы для главврачей
CREATE TABLE ChiefDoctors (
    ChiefDoctorID INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100),
    Password NVARCHAR(100)
);

-- Создание таблицы для менеджеров
CREATE TABLE Managers (
    ManagerID INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100),
    Password NVARCHAR(100)
);

-- Создание таблицы для врачей
CREATE TABLE Doctors (
    DoctorID INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100),
    Specialty NVARCHAR(100),
    OfficeNumber NVARCHAR(10),
    Password NVARCHAR(100),
    WorkExperience INT
);

-- Создание таблицы для пациентов
CREATE TABLE Patients (
    PatientID INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    RecordDate DATETIME,
    DischargeDate DATETIME
);

-- Создание таблицы для связи пациентов и врачей (многие ко многим)
CREATE TABLE PatientDoctorAssignments (
    PatientID INT,
    DoctorID INT,
    PRIMARY KEY (PatientID, DoctorID),
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
    FOREIGN KEY (DoctorID) REFERENCES Doctors(DoctorID)
);

-- Создание таблицы описаний пациентов
CREATE TABLE PatientDescriptions (
    PatientDescriptionID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    DoctorID INT NOT NULL,
    Description NVARCHAR(1000),
    DescriptionDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
    FOREIGN KEY (DoctorID) REFERENCES Doctors(DoctorID)
);

-- Создание таблицы для симптомов
CREATE TABLE Symptoms (
    SymptomID INT IDENTITY PRIMARY KEY,
    SymptomName NVARCHAR(100)
);

-- Создание таблицы для наводящих вопросов
CREATE TABLE FollowUpQuestions (
    QuestionID INT IDENTITY PRIMARY KEY,
    SymptomID INT,
    Question NVARCHAR(255),
    FOREIGN KEY (SymptomID) REFERENCES Symptoms(SymptomID)
);

-- Создание таблицы для ответов на наводящие вопросы
CREATE TABLE Answers (
    AnswerID INT IDENTITY PRIMARY KEY,
    QuestionID INT,
    Answer NVARCHAR(255),
    FOREIGN KEY (QuestionID) REFERENCES FollowUpQuestions(QuestionID)
);

-- Создание таблицы для диагнозов
CREATE TABLE Diagnoses (
    DiagnosisID INT IDENTITY PRIMARY KEY,
    DiagnosisName NVARCHAR(100)
);

-- Создание таблицы для связи между диагнозами пациентов
CREATE TABLE PatientDiagnoses (
    PatientID INT NOT NULL,
    DiagnosisID INT NOT NULL,
    PercentageOfDiagnosis INT NULL,
    PRIMARY KEY (PatientID, DiagnosisID),
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
    FOREIGN KEY (DiagnosisID) REFERENCES Diagnoses(DiagnosisID)
);

-- Связь между ответами и диагнозами
CREATE TABLE AnswerDiagnoses (
    AnswerID INT,
    DiagnosisID INT,
    FOREIGN KEY (AnswerID) REFERENCES Answers(AnswerID),
    FOREIGN KEY (DiagnosisID) REFERENCES Diagnoses(DiagnosisID)
);

-- Связь между врачами и диагнозами
CREATE TABLE DoctorDiagnoses (
    DoctorID INT,
    DiagnosisID INT,
    FOREIGN KEY (DoctorID) REFERENCES Doctors(DoctorID),
    FOREIGN KEY (DiagnosisID) REFERENCES Diagnoses(DiagnosisID)
);

-- Создание таблицы для новых пациентов
CREATE TABLE NewPatients (
    NewPatientID INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(100),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    PredictedDoctorID INT NULL,
    FOREIGN KEY (PredictedDoctorID) REFERENCES Doctors(DoctorID)
);

-- Таблица для хранения симптомов пациентов
CREATE TABLE NewPatientSymptoms (
    NewPatientID INT,
    SymptomID INT,
    FOREIGN KEY (NewPatientID) REFERENCES NewPatients(NewPatientID),
    FOREIGN KEY (SymptomID) REFERENCES Symptoms(SymptomID)
);

-- Таблица для хранения ответов пациентов
CREATE TABLE NewPatientAnswers (
    NewPatientID INT,
    AnswerID INT,
    FOREIGN KEY (NewPatientID) REFERENCES NewPatients(NewPatientID),
    FOREIGN KEY (AnswerID) REFERENCES Answers(AnswerID)
);

-- Таблица для хранения диагнозов пациентов
CREATE TABLE NewPatientDiagnoses (
    NewPatientID INT,
    DiagnosisID INT,
    PercentageOfDiagnosis INT,
    FOREIGN KEY (NewPatientID) REFERENCES NewPatients(NewPatientID),
    FOREIGN KEY (DiagnosisID) REFERENCES Diagnoses(DiagnosisID)
);

-- Создание таблицы для процедур
CREATE TABLE Procedures (
    ProcedureID INT IDENTITY PRIMARY KEY,
    DoctorID INT,
    ProcedureName NVARCHAR(100),
    Duration INT,
    FOREIGN KEY (DoctorID) REFERENCES Doctors(DoctorID)
);

-- Создание таблицы для назначений процедур
CREATE TABLE ProcedureAppointments (
    AppointmentID INT IDENTITY PRIMARY KEY,
    PatientID INT,
    DoctorID INT,
    ProcedureID INT,
    AppointmentDateTime DATETIME,
    Status NVARCHAR(50),
    Description NVARCHAR(1000) NULL,
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
    FOREIGN KEY (DoctorID) REFERENCES Doctors(DoctorID),
    FOREIGN KEY (ProcedureID) REFERENCES Procedures(ProcedureID)
);

-- Таблица для типов документов
CREATE TABLE DocumentTypes (
    DocumentTypeID INT IDENTITY PRIMARY KEY,
    DocumentName NVARCHAR(255) NOT NULL,
    Description NVARCHAR(500) NULL,
    IsRequired BIT DEFAULT 1 NOT NULL,
    MinimumAge INT DEFAULT 0, -- Минимальный возраст для требования документа
    MaximumAge INT DEFAULT 200, -- Максимальный возраст (если документ не нужен после определенного возраста)
    ForAccompanyingPerson BIT DEFAULT 0 NOT NULL, -- Флаг, указывающий, является ли документ для сопровождающего лица
    ValidityDays INT NULL -- Срок действия документа в днях (NULL если нет ограничений)
);

-- Таблица для документов пациентов
CREATE TABLE PatientDocuments (
    DocumentID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    DocumentTypeID INT NOT NULL,
    DocumentPath NVARCHAR(500) NOT NULL, -- Путь к файлу
    UploadDate DATETIME DEFAULT GETDATE(),
    ExpiryDate DATETIME NULL, -- Дата истечения срока действия
    IsVerified BIT DEFAULT 0, -- Проверен ли документ
    VerifiedBy INT NULL, -- Кто проверил (ID пользователя из Managers или ChiefDoctors)
    Notes NVARCHAR(500) NULL, -- Примечания
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
    FOREIGN KEY (DocumentTypeID) REFERENCES DocumentTypes(DocumentTypeID)
);

-- Таблица для документов сопровождающих лиц
CREATE TABLE AccompanyingPersons (
    AccompanyingPersonID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE NULL,
    Relationship NVARCHAR(50) NULL, -- Отношение к пациенту (родитель, опекун и т.д.)
    HasPowerOfAttorney BIT DEFAULT 0, -- Имеет ли доверенность, если не является законным представителем
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID)
);

-- Таблица для документов сопровождающих лиц
CREATE TABLE AccompanyingPersonDocuments (
    DocumentID INT IDENTITY PRIMARY KEY,
    AccompanyingPersonID INT NOT NULL,
    DocumentTypeID INT NOT NULL,
    DocumentPath NVARCHAR(500) NOT NULL,
    UploadDate DATETIME DEFAULT GETDATE(),
    ExpiryDate DATETIME NULL,
    IsVerified BIT DEFAULT 0,
    VerifiedBy INT NULL,
    Notes NVARCHAR(500) NULL,
    FOREIGN KEY (AccompanyingPersonID) REFERENCES AccompanyingPersons(AccompanyingPersonID),
    FOREIGN KEY (DocumentTypeID) REFERENCES DocumentTypes(DocumentTypeID)
);

-- Заполнение таблицы типов документов
INSERT INTO DocumentTypes (DocumentName, Description, IsRequired, MinimumAge, MaximumAge, ForAccompanyingPerson, ValidityDays)
VALUES
-- Документы для пациентов
('Направление (форма 057-У)', 'Направление на госпитализацию в системе ЕЦП по программе ОМС', 1, 0, 200, 0, NULL),
('Справка об отсутствии противопоказаний', 'Заверенная ВК', 1, 0, 200, 0, NULL),
('Страховой полис', 'Ксерокопия', 1, 0, 200, 0, NULL),
('Свидетельство о рождении', 'Ксерокопия (для детей до 14 лет)', 1, 0, 14, 0, NULL),
('Паспорт', 'Ксерокопия (с 14 лет)', 1, 14, 200, 0, NULL),
('СНИЛС', 'Ксерокопия', 1, 0, 200, 0, NULL),
('Прививочный сертификат', 'Ксерокопия', 1, 0, 200, 0, NULL),
('Справка МСЭ', 'Ксерокопия', 1, 0, 200, 0, NULL),
('Выписка из амбулаторной карты (форма 112-У)', 'О состоянии здоровья в настоящее время', 1, 0, 200, 0, NULL),
('Выписка из истории болезни', 'Последняя госпитализация', 1, 0, 200, 0, NULL),
('Выписка из истории болезни при повторной госпитализации', 'Ксерокопия', 0, 0, 200, 0, NULL),
('Заключение специалиста по основному заболеванию', 'Травматолог, кардиолог, невролог и др.', 1, 0, 200, 0, NULL),
('Клинический анализ крови', NULL, 1, 0, 200, 0, 10),
('Анализ мочи', NULL, 1, 0, 200, 0, 10),
('Анализ кала на яйца глистов', NULL, 1, 0, 200, 0, 30),
('Соскоб на я/г', 'Альтернатива анализу кала на яйца глистов', 0, 0, 200, 0, 10),
('Электрокардиограмма', NULL, 1, 0, 200, 0, 90),
('Электроэнцефалограмма', 'Для детей с неврологической патологией', 0, 0, 200, 0, 90),
('Справка от дерматолога', NULL, 1, 0, 200, 0, 10),
('Справка об отсутствии контактов с инфекционными больными', 'Выдается педиатром или эпидемиологом', 1, 0, 200, 0, 3),
('Результат бактериологического посева кала', 'Для детей до 2-х лет', 1, 0, 2, 0, 14),
('Анализ крови на RW', 'Для детей с 14 лет', 1, 14, 200, 0, 30),
('Результат флюорографии', 'Для детей с 15 лет', 1, 15, 200, 0, 365),

-- Документы для сопровождающих лиц
('Документ, удостоверяющий личность', 'Для сопровождающего лица', 1, 0, 200, 1, NULL),
('Полис ОМС', 'Для сопровождающего лица (ксерокопия)', 1, 0, 200, 1, NULL),
('СНИЛС', 'Для сопровождающего лица (ксерокопия)', 1, 0, 200, 1, NULL),
('Результат бактериологического посева кала на дизентерийную группу и сальмонеллёз', 'Для сопровождающего лица', 1, 0, 200, 1, 14),
('Результат флюорографии или заключение фтизиатра', 'Для сопровождающего лица', 1, 0, 200, 1, 365),
('Анализ крови на RW', 'Для сопровождающего лица', 1, 0, 200, 1, 30),
('Доверенность от законных представителей', 'Если сопровождающий не является законным представителем', 0, 0, 200, 1, NULL); 