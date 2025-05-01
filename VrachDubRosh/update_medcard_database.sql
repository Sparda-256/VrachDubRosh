-- SQL-скрипт для обновления существующей базы данных
USE PomoshnikPolicliniki2;

-- Таблица для медикаментов пациентов (заполняет главврач)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientMedications')
BEGIN
    CREATE TABLE PatientMedications (
        MedicationID INT IDENTITY PRIMARY KEY,
        PatientID INT NOT NULL,
        MedicationName NVARCHAR(200) NOT NULL,
        Dosage NVARCHAR(100),
        Instructions NVARCHAR(500),
        PrescribedDate DATETIME DEFAULT GETDATE(),
        PrescribedBy INT, -- Ссылка на ChiefDoctorID
        FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
        FOREIGN KEY (PrescribedBy) REFERENCES ChiefDoctors(ChiefDoctorID)
    );
    PRINT 'Таблица PatientMedications создана';
END
ELSE
BEGIN
    PRINT 'Таблица PatientMedications уже существует';
END

-- Таблица для антропометрических измерений
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientMeasurements')
BEGIN
    CREATE TABLE PatientMeasurements (
        MeasurementID INT IDENTITY PRIMARY KEY,
        PatientID INT NOT NULL,
        MeasurementType NVARCHAR(50) NOT NULL, -- 'При поступлении', 'В процессе лечения', 'При выписке'
        Height DECIMAL(5,2), -- Рост в см
        Weight DECIMAL(5,2), -- Вес в кг
        BloodPressure NVARCHAR(20), -- Кровяное давление (например, '120/80')
        MeasurementDate DATETIME DEFAULT GETDATE(),
        MeasuredBy INT, -- Ссылка на ChiefDoctorID
        FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
        FOREIGN KEY (MeasuredBy) REFERENCES ChiefDoctors(ChiefDoctorID)
    );
    PRINT 'Таблица PatientMeasurements создана';
END
ELSE
BEGIN
    PRINT 'Таблица PatientMeasurements уже существует';
END

-- Проверяем, существует ли столбец DiagnosisType в таблице PatientDiagnoses
IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = 'DiagnosisType' AND Object_ID = Object_ID('PatientDiagnoses'))
BEGIN
    ALTER TABLE PatientDiagnoses
    ADD DiagnosisType NVARCHAR(20) DEFAULT 'Сопутствующий'; -- 'Основной' или 'Сопутствующий'
    
    PRINT 'Столбец DiagnosisType добавлен в таблицу PatientDiagnoses';
END
ELSE
BEGIN
    PRINT 'Столбец DiagnosisType уже существует в таблице PatientDiagnoses';
END 