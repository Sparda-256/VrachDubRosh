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
    DischargeDate DATETIME,
    StayType NVARCHAR(20) DEFAULT 'Дневной' -- Тип стационара: 'Дневной' или 'Круглосуточный'
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
    IsRequired BIT DEFAULT 1 NOT NULL,
    MinimumAge INT DEFAULT 0, -- Минимальный возраст для требования документа
    MaximumAge INT DEFAULT 200, -- Максимальный возраст (если документ не нужен после определенного возраста)
    ForAccompanyingPerson BIT DEFAULT 0 NOT NULL -- Флаг, указывающий, является ли документ для сопровождающего лица
);

-- Таблица для документов пациентов
CREATE TABLE PatientDocuments (
    DocumentID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    DocumentTypeID INT NOT NULL,
    DocumentPath NVARCHAR(500) NOT NULL, -- Путь к файлу
    UploadDate DATETIME DEFAULT GETDATE(),
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
    IsVerified BIT DEFAULT 0,
    VerifiedBy INT NULL,
    Notes NVARCHAR(500) NULL,
    FOREIGN KEY (AccompanyingPersonID) REFERENCES AccompanyingPersons(AccompanyingPersonID),
    FOREIGN KEY (DocumentTypeID) REFERENCES DocumentTypes(DocumentTypeID)
);

-- Таблица для корпусов
CREATE TABLE Buildings (
    BuildingID INT IDENTITY PRIMARY KEY,
    BuildingNumber INT NOT NULL,
    TotalRooms INT NOT NULL,
    Description NVARCHAR(500) NULL
);

-- Таблица для комнат
CREATE TABLE Rooms (
    RoomID INT IDENTITY PRIMARY KEY,
    BuildingID INT NOT NULL,
    RoomNumber NVARCHAR(10) NOT NULL, -- Например "1А", "1Б", и т.д.
    MaxCapacity INT DEFAULT 2, -- Каждая комната двухместная
    IsAvailable BIT DEFAULT 1, -- Доступна ли комната
    FOREIGN KEY (BuildingID) REFERENCES Buildings(BuildingID),
    CONSTRAINT UQ_BuildingRoom UNIQUE (BuildingID, RoomNumber)
);

-- Таблица для размещения (проживания)
CREATE TABLE Accommodations (
    AccommodationID INT IDENTITY PRIMARY KEY,
    RoomID INT NOT NULL,
    PatientID INT NULL, -- Может быть NULL, если это место занимает сопровождающий
    AccompanyingPersonID INT NULL, -- Может быть NULL, если это место занимает пациент
    CheckInDate DATETIME NOT NULL DEFAULT GETDATE(),
    CheckOutDate DATETIME NULL, -- NULL означает, что еще проживает
    BedNumber INT NOT NULL, -- 1 или 2, в двухместной комнате
    FOREIGN KEY (RoomID) REFERENCES Rooms(RoomID),
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
    FOREIGN KEY (AccompanyingPersonID) REFERENCES AccompanyingPersons(AccompanyingPersonID),
    -- Проверка: должен быть указан либо пациент, либо сопровождающий, но не оба одновременно
    CONSTRAINT CK_PersonType CHECK ((PatientID IS NULL AND AccompanyingPersonID IS NOT NULL) OR 
                                   (PatientID IS NOT NULL AND AccompanyingPersonID IS NULL)),
    -- Проверка: в одной комнате на одной кровати может быть только один человек в один момент времени
    CONSTRAINT UQ_RoomBed UNIQUE (RoomID, BedNumber, CheckOutDate)
);

-- Заполнение таблицы типов документов
INSERT INTO DocumentTypes (DocumentName, IsRequired, MinimumAge, MaximumAge, ForAccompanyingPerson)
VALUES
-- Документы для пациентов
('Направление (форма 057-У)', 1, 0, 200, 0),
('Справка об отсутствии противопоказаний', 1, 0, 200, 0),
('Страховой полис', 1, 0, 200, 0),
('Свидетельство о рождении', 1, 0, 14, 0),
('Паспорт', 1, 14, 200, 0),
('СНИЛС', 1, 0, 200, 0),
('Прививочный сертификат', 1, 0, 200, 0),
('Справка МСЭ', 1, 0, 200, 0),
('Выписка из амбулаторной карты (форма 112-У)', 1, 0, 200, 0),
('Выписка из истории болезни', 1, 0, 200, 0),
('Выписка из истории болезни при повторной госпитализации', 0, 0, 200, 0),
('Заключение специалиста по основному заболеванию', 1, 0, 200, 0),
('Клинический анализ крови', 1, 0, 200, 0),
('Анализ мочи', 1, 0, 200, 0),
('Анализ кала на яйца глистов', 1, 0, 200, 0),
('Соскоб на я/г', 0, 0, 200, 0),
('Электрокардиограмма', 1, 0, 200, 0),
('Электроэнцефалограмма', 0, 0, 200, 0),
('Справка от дерматолога', 1, 0, 200, 0),
('Справка об отсутствии контактов с инфекционными больными', 1, 0, 200, 0),
('Результат бактериологического посева кала', 1, 0, 2, 0),
('Анализ крови на RW', 1, 14, 200, 0),
('Результат флюорографии', 1, 15, 200, 0),

-- Документы для сопровождающих лиц
('Документ, удостоверяющий личность', 1, 0, 200, 1),
('Полис ОМС', 1, 0, 200, 1),
('СНИЛС', 1, 0, 200, 1),
('Результат бактериологического посева кала на дизентерийную группу и сальмонеллёз', 1, 0, 200, 1),
('Результат флюорографии или заключение фтизиатра', 1, 0, 200, 1),
('Анализ крови на RW', 1, 0, 200, 1),
('Доверенность от законных представителей', 0, 0, 200, 1);

-- Заполнение таблицы корпусов
INSERT INTO Buildings (BuildingNumber, TotalRooms, Description) VALUES
(2, 20, 'Корпус 2'),
(5, 20, 'Корпус 5'),
(6, 20, 'Корпус 6');

-- Заполнение таблицы комнат
-- Скрипт для генерации комнат для всех корпусов
DECLARE @BuildingID INT = 1;
WHILE @BuildingID <= 3
BEGIN
    DECLARE @RoomCounter INT = 1;
    WHILE @RoomCounter <= 10
    BEGIN
        -- Добавляем комнату А для текущего номера
        INSERT INTO Rooms (BuildingID, RoomNumber) 
        VALUES (@BuildingID, CAST(@RoomCounter AS NVARCHAR) + 'А');
        
        -- Добавляем комнату Б для текущего номера
        INSERT INTO Rooms (BuildingID, RoomNumber) 
        VALUES (@BuildingID, CAST(@RoomCounter AS NVARCHAR) + 'Б');
        
        SET @RoomCounter = @RoomCounter + 1;
    END
    
    SET @BuildingID = @BuildingID + 1;
END; 