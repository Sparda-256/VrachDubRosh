USE PomoshnikPolicliniki2;

-- Check if the PatientDiagnoses table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PatientDiagnoses')
BEGIN
    -- Backup existing data if needed
    SELECT * INTO PatientDiagnoses_Backup FROM PatientDiagnoses;
    
    -- Drop the existing table
    DROP TABLE PatientDiagnoses;
    
    -- Create a new PatientDiagnoses table with a simplified structure
    CREATE TABLE PatientDiagnoses (
        PatientID INT NOT NULL,
        DiagnosisID INT NOT NULL,
        PercentageOfDiagnosis INT NULL,
        PRIMARY KEY (PatientID, DiagnosisID),
        FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
        FOREIGN KEY (DiagnosisID) REFERENCES Diagnoses(DiagnosisID)
    );
    
    PRINT 'PatientDiagnoses table has been recreated with a simplified structure.';
END
ELSE
BEGIN
    -- Create the PatientDiagnoses table
    CREATE TABLE PatientDiagnoses (
        PatientID INT NOT NULL,
        DiagnosisID INT NOT NULL,
        PercentageOfDiagnosis INT NULL,
        PRIMARY KEY (PatientID, DiagnosisID),
        FOREIGN KEY (PatientID) REFERENCES Patients(PatientID),
        FOREIGN KEY (DiagnosisID) REFERENCES Diagnoses(DiagnosisID)
    );
    
    PRINT 'PatientDiagnoses table has been created with the new structure.';
END 