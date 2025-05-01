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

-- Создание таблицы для диагнозов
CREATE TABLE Diagnoses (
    DiagnosisID INT IDENTITY PRIMARY KEY,
    DiagnosisName NVARCHAR(100)
);

-- Создание таблицы для связи между диагнозами пациентов
CREATE TABLE PatientDiagnoses (
    PatientID INT NOT NULL,
    DiagnosisID INT NOT NULL,
    PRIMARY KEY (PatientID, DiagnosisID),
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
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

-- Создание таблицы для документов менеджера
CREATE TABLE ManagerDocuments (
    DocumentID INT IDENTITY(1,1) PRIMARY KEY,
    DocumentName NVARCHAR(255) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    FileType NVARCHAR(50) NOT NULL,
    FileSizeBytes BIGINT NOT NULL,
    UploadDate DATETIME NOT NULL,
    UploadedBy NVARCHAR(100) NOT NULL,
    FilePath NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX) NULL
);

-- Таблица для выписных эпикризов
CREATE TABLE DischargeDocuments (
    DischargeID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    Complaints NVARCHAR(MAX),
    DiseaseHistory NVARCHAR(MAX),
    InitialCondition NVARCHAR(MAX),
    RehabilitationGoal NVARCHAR(MAX),
    GoalAchieved BIT,
    Recommendations NVARCHAR(MAX),
    LastUpdated DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID)
); 

INSERT INTO Managers(FullName, Password)
VALUES
('3', '3');

INSERT INTO ChiefDoctors(FullName, Password)
VALUES
('admin', 'admin'),
('Admin', 'Admin'),
('Админ', 'Админ'),
('2', '2'),
('админ', 'админ');

-- Заполнение таблицы врачей
INSERT INTO Doctors (FullName, Specialty, OfficeNumber, Password, WorkExperience)
VALUES
('Иванов Иван Иванович', 'Терапевт', '101', '1', 15),
('Петрова Мария Сергеевна', 'Педиатр', '102', '1', 8),
('Сидоров Алексей Петрович', 'Физиотерапевт', '103', '1', 12),
('Кузнецова Елена Александровна', 'Дерматолог', '104', '1', 14),
('Смирнов Дмитрий Андреевич', 'Травматолог', '105', '1', 9),
('Новикова Ольга Викторовна', 'Кардиолог', '106', '1', 18),
('Морозов Сергей Николаевич', 'Невролог', '107', '1', 16),
('Волкова Анна Игоревна', 'Реабилитолог', '108', '1', 7),
('Соколов Максим Владимирович', 'Гинеколог', '109', '1', 13),
('Лебедева Наталья Михайловна', 'Стоматолог', '110', '1', 10),
('Козлов Андрей Юрьевич', 'Врач функциональной диагностики', '111', '1', 17),
('Павлова Ирина Васильевна', 'Врач ультразвуковой диагностики', '112', '1', 11);

-- Заполнение таблицы диагнозов
INSERT INTO Diagnoses (DiagnosisName)
VALUES
-- Заболевания органов дыхания
('Хронический бронхит'),
('Бронхиальная астма'),
('Хроническая обструктивная болезнь легких (ХОБЛ)'),
('Пневмония в стадии реабилитации'),
('Бронхоэктатическая болезнь'),
('Плеврит в стадии реабилитации'),
('Эмфизема легких'),

-- Заболевания костно-мышечной системы
('Остеохондроз позвоночника'),
('Ревматоидный артрит'),
('Остеоартрит'),
('Периартрит'),
('Подагра'),
('Фибромиалгия'),
('Сколиоз'),
('Спондилит'),
('Плечелопаточный периартрит'),

-- Заболевания кожи и подкожной клетчатки
('Псориаз'),
('Экзема'),
('Атопический дерматит'),
('Крапивница'),
('Акне (угревая болезнь)'),
('Рожистое воспаление'),
('Себорейный дерматит'),

-- Заболевания периферической нервной системы
('Полиневропатия'),
('Радикулопатия'),
('Невралгия тройничного нерва'),
('Мононевропатия'),
('Плексопатия'),
('Туннельные синдромы'),
('Постгерпетическая невралгия'),

-- Заболевания эндокринной системы
('Сахарный диабет 2 типа'),
('Гипотиреоз'),
('Тиреоидит'),
('Ожирение'),
('Метаболический синдром'),
('Остеопороз'),
('Тиреотоксикоз'),

-- Заболевания сердечно-сосудистой системы
('Гипертоническая болезнь'),
('Ишемическая болезнь сердца'),
('Кардиомиопатия'),
('Хроническая сердечная недостаточность'),
('Постинфарктный кардиосклероз'),
('Атеросклероз сосудов'),
('Варикозное расширение вен нижних конечностей'),
('Нарушения ритма сердца');

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
('Полис ОМС', 1, 0, 200, 1),
('СНИЛС', 1, 0, 200, 1),
('Результат бактериологического посева кала на дизентерийную группу и сальмонеллёз', 1, 0, 200, 1),
('Результат флюорографии или заключение фтизиатра', 1, 0, 200, 1),
('Анализ крови на RW', 1, 0, 200, 1),
('Доверенность от законных представителей', 0, 0, 200, 1);

-- Таблица для регулярных назначений процедур (недельный график)
CREATE TABLE WeeklyScheduleAppointments (
    ScheduleID INT IDENTITY PRIMARY KEY,
    PatientID INT NOT NULL,
    DoctorID INT NOT NULL,
    ProcedureID INT NOT NULL,
    DayOfWeek INT NOT NULL, -- 1 = Понедельник, 2 = Вторник, и т.д.
    AppointmentTime TIME NOT NULL, -- Время назначения
    StartDate DATE NOT NULL, -- Дата начала повторений
    EndDate DATE NOT NULL, -- Дата окончания повторений
    IsActive BIT DEFAULT 1, -- Активно ли расписание
    CreatedDateTime DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
    FOREIGN KEY (DoctorID) REFERENCES Doctors(DoctorID),
    FOREIGN KEY (ProcedureID) REFERENCES Procedures(ProcedureID)
);

-- Таблица для экземпляров процедур, созданных на основе недельного графика
CREATE TABLE ScheduleGeneratedAppointments (
    GeneratedAppointmentID INT IDENTITY PRIMARY KEY,
    ScheduleID INT NOT NULL,
    AppointmentID INT NOT NULL, -- ID в таблице ProcedureAppointments
    FOREIGN KEY (ScheduleID) REFERENCES WeeklyScheduleAppointments(ScheduleID),
    FOREIGN KEY (AppointmentID) REFERENCES ProcedureAppointments(AppointmentID)
);

GO

-- Хранимая процедура для генерации назначений из недельного графика
CREATE PROCEDURE GenerateAppointmentsFromSchedule
    @ScheduleID INT,
    @GenerateUntilDate DATE = NULL -- Опционально: до какой даты генерировать (по умолчанию до EndDate)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @PatientID INT, @DoctorID INT, @ProcedureID INT;
    DECLARE @DayOfWeek INT, @AppointmentTime TIME;
    DECLARE @StartDate DATE, @EndDate DATE;
    DECLARE @CurrentDate DATE;
    DECLARE @AppointmentDateTime DATETIME;
    DECLARE @NewAppointmentID INT;
    
    -- Получаем информацию о расписании
    SELECT 
        @PatientID = PatientID,
        @DoctorID = DoctorID,
        @ProcedureID = ProcedureID,
        @DayOfWeek = DayOfWeek,
        @AppointmentTime = AppointmentTime,
        @StartDate = StartDate,
        @EndDate = CASE WHEN @GenerateUntilDate IS NULL THEN EndDate
                        WHEN @GenerateUntilDate > EndDate THEN EndDate
                        ELSE @GenerateUntilDate END
    FROM WeeklyScheduleAppointments
    WHERE ScheduleID = @ScheduleID AND IsActive = 1;
    
    IF @@ROWCOUNT = 0
    BEGIN
        PRINT 'Расписание не найдено или неактивно';
        RETURN;
    END
    
    -- Устанавливаем начальную дату
    SET @CurrentDate = @StartDate;
    
    -- Корректируем начальную дату, чтобы соответствовать DayOfWeek
    WHILE (DATEPART(WEEKDAY, @CurrentDate) - 1) % 7 + 1 != @DayOfWeek
    BEGIN
        SET @CurrentDate = DATEADD(DAY, 1, @CurrentDate);
    END
    
    -- Генерируем назначения
    WHILE @CurrentDate <= @EndDate
    BEGIN
        -- Проверяем, не выписан ли пациент к этой дате
        IF NOT EXISTS (
            SELECT 1 FROM Patients 
            WHERE PatientID = @PatientID 
              AND DischargeDate IS NOT NULL 
              AND DischargeDate <= @CurrentDate)
        BEGIN
            -- Создаем дату и время назначения
            SET @AppointmentDateTime = CAST(@CurrentDate AS DATETIME) + CAST(@AppointmentTime AS DATETIME);
            
            -- Проверяем занятость врача
            IF NOT EXISTS (
                SELECT 1
                FROM ProcedureAppointments pa
                INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                WHERE pa.DoctorID = @DoctorID
                  AND pa.Status NOT IN ('Отменена')
                  AND pa.AppointmentDateTime < DATEADD(MINUTE, (SELECT Duration FROM Procedures WHERE ProcedureID = @ProcedureID), @AppointmentDateTime)
                  AND DATEADD(MINUTE, pr.Duration, pa.AppointmentDateTime) > @AppointmentDateTime)
            BEGIN
                -- Проверяем занятость пациента
                IF NOT EXISTS (
                    SELECT 1
                    FROM ProcedureAppointments pa
                    INNER JOIN Procedures pr ON pa.ProcedureID = pr.ProcedureID
                    WHERE pa.PatientID = @PatientID
                      AND pa.Status NOT IN ('Отменена')
                      AND pa.AppointmentDateTime < DATEADD(MINUTE, (SELECT Duration FROM Procedures WHERE ProcedureID = @ProcedureID), @AppointmentDateTime)
                      AND DATEADD(MINUTE, pr.Duration, pa.AppointmentDateTime) > @AppointmentDateTime)
                BEGIN
                    -- Если время не прошло, а также нет конфликтов, создаем назначение
                    IF @AppointmentDateTime > GETDATE()
                    BEGIN
                        -- Вставляем запись в ProcedureAppointments
                        INSERT INTO ProcedureAppointments (PatientID, DoctorID, ProcedureID, AppointmentDateTime, Status)
                        VALUES (@PatientID, @DoctorID, @ProcedureID, @AppointmentDateTime, 'Назначена');
                        
                        -- Получаем ID созданного назначения
                        SET @NewAppointmentID = SCOPE_IDENTITY();
                        
                        -- Связываем с расписанием
                        INSERT INTO ScheduleGeneratedAppointments (ScheduleID, AppointmentID)
                        VALUES (@ScheduleID, @NewAppointmentID);
                    END
                END
            END
        END
        
        -- Переходим к следующей неделе
        SET @CurrentDate = DATEADD(DAY, 7, @CurrentDate);
    END
    
    PRINT 'Назначения по расписанию сгенерированы успешно';
END

GO

-- Триггер для автоматического обновления назначений при изменении расписания
CREATE TRIGGER trg_UpdateScheduleAppointments
ON WeeklyScheduleAppointments
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Если расписание деактивировано, отменяем будущие назначения
    IF EXISTS (SELECT 1 FROM inserted i WHERE i.IsActive = 0)
    BEGIN
        UPDATE pa
        SET pa.Status = 'Отменена'
        FROM ProcedureAppointments pa
        JOIN ScheduleGeneratedAppointments sga ON pa.AppointmentID = sga.AppointmentID
        JOIN inserted i ON sga.ScheduleID = i.ScheduleID
        WHERE i.IsActive = 0
          AND pa.AppointmentDateTime > GETDATE()
          AND pa.Status = 'Назначена';
    END
    
    -- Если изменились параметры расписания, отменяем будущие назначения 
    -- и создаем новые согласно обновленному расписанию
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN deleted d ON i.ScheduleID = d.ScheduleID
        WHERE i.IsActive = 1 AND
            (i.ProcedureID != d.ProcedureID OR
             i.DayOfWeek != d.DayOfWeek OR
             i.AppointmentTime != d.AppointmentTime OR
             i.EndDate != d.EndDate)
    )
    BEGIN
        -- Отменяем будущие назначения
        UPDATE pa
        SET pa.Status = 'Отменена'
        FROM ProcedureAppointments pa
        JOIN ScheduleGeneratedAppointments sga ON pa.AppointmentID = sga.AppointmentID
        JOIN inserted i ON sga.ScheduleID = i.ScheduleID
        WHERE i.IsActive = 1
          AND pa.AppointmentDateTime > GETDATE()
          AND pa.Status = 'Назначена';
        
        -- Для каждого измененного расписания генерируем новые назначения
        DECLARE @ScheduleID INT;
        
        DECLARE schedule_cursor CURSOR FOR
        SELECT i.ScheduleID
        FROM inserted i
        JOIN deleted d ON i.ScheduleID = d.ScheduleID
        WHERE i.IsActive = 1 AND
            (i.ProcedureID != d.ProcedureID OR
             i.DayOfWeek != d.DayOfWeek OR
             i.AppointmentTime != d.AppointmentTime OR
             i.EndDate != d.EndDate);
        
        OPEN schedule_cursor;
        FETCH NEXT FROM schedule_cursor INTO @ScheduleID;
        
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC GenerateAppointmentsFromSchedule @ScheduleID;
            FETCH NEXT FROM schedule_cursor INTO @ScheduleID;
        END
        
        CLOSE schedule_cursor;
        DEALLOCATE schedule_cursor;
    END
END

GO

-- Триггер для генерации назначений при создании нового расписания
CREATE TRIGGER trg_CreateScheduleAppointments
ON WeeklyScheduleAppointments
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Для каждого нового расписания генерируем назначения
    DECLARE @ScheduleID INT;
    
    DECLARE schedule_cursor CURSOR FOR
    SELECT ScheduleID FROM inserted WHERE IsActive = 1;
    
    OPEN schedule_cursor;
    FETCH NEXT FROM schedule_cursor INTO @ScheduleID;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC GenerateAppointmentsFromSchedule @ScheduleID;
        FETCH NEXT FROM schedule_cursor INTO @ScheduleID;
    END
    
    CLOSE schedule_cursor;
    DEALLOCATE schedule_cursor;
END