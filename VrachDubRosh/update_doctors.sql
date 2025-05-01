-- SQL-скрипт для добавления новых врачей в существующую базу данных
USE PomoshnikPolicliniki2;

-- Добавляем нового врача с общим наименованием "Души"
INSERT INTO Doctors (FullName, Specialty, OfficeNumber, Password, WorkExperience, GeneralName)
VALUES ('Григорьев Илья Дмитриевич', 'Специалист по гидротерапии', '113', '1', 14, 'Души');

-- Добавляем нового врача с общим наименованием "Ванны"
INSERT INTO Doctors (FullName, Specialty, OfficeNumber, Password, WorkExperience, GeneralName)
VALUES ('Соловьева Алина Викторовна', 'Специалист по бальнеотерапии', '114', '1', 12, 'Ванны');

-- Проверяем добавление
SELECT * FROM Doctors WHERE GeneralName IN ('Души', 'Ванны'); 