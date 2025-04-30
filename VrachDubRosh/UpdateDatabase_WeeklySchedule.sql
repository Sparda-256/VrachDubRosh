USE PomoshnikPolicliniki2;

-- Таблица для регулярных назначений процедур (недельный график)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WeeklyScheduleAppointments')
BEGIN
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
    PRINT 'Таблица WeeklyScheduleAppointments создана успешно';
END
ELSE
BEGIN
    PRINT 'Таблица WeeklyScheduleAppointments уже существует';
END

-- Таблица для экземпляров процедур, созданных на основе недельного графика
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScheduleGeneratedAppointments')
BEGIN
    CREATE TABLE ScheduleGeneratedAppointments (
        GeneratedAppointmentID INT IDENTITY PRIMARY KEY,
        ScheduleID INT NOT NULL,
        AppointmentID INT NOT NULL, -- ID в таблице ProcedureAppointments
        FOREIGN KEY (ScheduleID) REFERENCES WeeklyScheduleAppointments(ScheduleID),
        FOREIGN KEY (AppointmentID) REFERENCES ProcedureAppointments(AppointmentID)
    );
    PRINT 'Таблица ScheduleGeneratedAppointments создана успешно';
END
ELSE
BEGIN
    PRINT 'Таблица ScheduleGeneratedAppointments уже существует';
END

-- Создадим хранимую процедуру для генерации назначений из недельного графика
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GenerateAppointmentsFromSchedule')
BEGIN
    DROP PROCEDURE GenerateAppointmentsFromSchedule;
    PRINT 'Процедура GenerateAppointmentsFromSchedule удалена для обновления';
END

GO

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

PRINT 'Процедура GenerateAppointmentsFromSchedule создана успешно';

-- Создадим триггер для автоматического обновления назначений при изменении расписания
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_UpdateScheduleAppointments')
BEGIN
    DROP TRIGGER trg_UpdateScheduleAppointments;
    PRINT 'Триггер trg_UpdateScheduleAppointments удален для обновления';
END

GO

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

PRINT 'Триггер trg_UpdateScheduleAppointments создан успешно';

-- Создадим триггер для генерации назначений при создании нового расписания
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'trg_CreateScheduleAppointments')
BEGIN
    DROP TRIGGER trg_CreateScheduleAppointments;
    PRINT 'Триггер trg_CreateScheduleAppointments удален для обновления';
END

GO

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

GO

PRINT 'Триггер trg_CreateScheduleAppointments создан успешно';

PRINT 'Обновление базы данных завершено успешно';