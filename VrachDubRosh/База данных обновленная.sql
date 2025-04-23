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