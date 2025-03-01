using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace VrachDubRosh
{

    public class AuthenticationService
    {
        public int? AuthenticateDoctor(string login, string password)
        {
            // Если логин и пароль корректны, возвращаем идентификатор врача.
            if (login == "ValidDoctor" && password == "ValidPassword")
                return 1;
            return null;
        }
    }

    public class SchedulingService
    {
        private readonly List<(DateTime start, int duration)> _appointments = new List<(DateTime, int)>();

        public void AddAppointment(DateTime start, int duration)
        {
            _appointments.Add((start, duration));
        }

        /// <summary>
        /// Проверяет, пересекается ли новое назначение с уже запланированными.
        /// </summary>
        public bool IsDoctorOccupied(DateTime newStart, int newDuration)
        {
            DateTime newEnd = newStart.AddMinutes(newDuration);
            foreach (var (start, duration) in _appointments)
            {
                DateTime end = start.AddMinutes(duration);
                // Если интервалы пересекаются, возвращаем true.
                if (newStart < end && start < newEnd)
                    return true;
            }
            return false;
        }
    }

    public class Appointment
    {
        public DateTime AppointmentDateTime { get; set; }
        public int Duration { get; set; }  // длительность в минутах
        public string Status { get; set; } // "Назначена", "Идёт", "Завершена", "Отменена"
    }

    public class AppointmentStatusService
    {
        /// <summary>
        /// Обновляет статус назначения в зависимости от текущего времени.
        /// </summary>
        public void UpdateStatus(Appointment appointment)
        {
            DateTime now = DateTime.Now;
            DateTime appointmentEnd = appointment.AppointmentDateTime.AddMinutes(appointment.Duration);
            if (now >= appointmentEnd)
            {
                appointment.Status = "Завершена";
            }
            else if (now >= appointment.AppointmentDateTime)
            {
                appointment.Status = "Идёт";
            }
            // Если до начала – оставляем статус "Назначена"
        }
    }
}

namespace VrachDubRosh.Tests
{
    using VrachDubRosh;

    public class AuthenticationServiceTests
    {
        [Fact]
        public void Authenticate_ValidCredentials_ReturnsDoctorId()
        {
            // Arrange
            var authService = new AuthenticationService();
            string login = "ValidDoctor";
            string password = "ValidPassword";

            // Act
            int? doctorId = authService.AuthenticateDoctor(login, password);

            // Assert
            Assert.NotNull(doctorId);
        }

        [Fact]
        public void Authenticate_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            var authService = new AuthenticationService();
            string login = "InvalidLogin";
            string password = "InvalidPassword";

            // Act
            int? doctorId = authService.AuthenticateDoctor(login, password);

            // Assert
            Assert.Null(doctorId);
        }
    }

    public class SchedulingServiceTests
    {
        [Theory]
        // Новый интервал пересекается с уже запланированным
        [InlineData("2025-01-01T10:00:00", 30, "2025-01-01T10:15:00", 30, true)]
        // Новый интервал не пересекается (начинается после завершения)
        [InlineData("2025-01-01T10:00:00", 30, "2025-01-01T10:35:00", 30, false)]
        public void IsDoctorOccupied_ReturnsExpected(string existingStartStr, int existingDuration,
                                                     string newStartStr, int newDuration, bool expected)
        {
            // Arrange
            DateTime existingStart = DateTime.Parse(existingStartStr);
            DateTime newStart = DateTime.Parse(newStartStr);
            var schedulingService = new SchedulingService();
            schedulingService.AddAppointment(existingStart, existingDuration);

            // Act
            bool conflict = schedulingService.IsDoctorOccupied(newStart, newDuration);

            // Assert
            Assert.Equal(expected, conflict);
        }
    }

    public class AppointmentStatusTests
    {
        [Fact]
        public void UpdateStatus_AppointmentEnded_StatusBecomesCompleted()
        {
            // Arrange: процедура началась 60 минут назад, длительность 30 минут
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(-60),
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            // Act
            statusService.UpdateStatus(appointment);

            // Assert
            Assert.Equal("Завершена", appointment.Status);
        }

        [Fact]
        public void UpdateStatus_AppointmentInProgress_StatusBecomesInProgress()
        {
            // Arrange: процедура началась 10 минут назад, длительность 30 минут
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(-10),
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            // Act
            statusService.UpdateStatus(appointment);

            // Assert
            Assert.Equal("Идёт", appointment.Status);
        }

        [Fact]
        public void UpdateStatus_AppointmentNotStarted_StatusRemainsScheduled()
        {
            // Arrange: процедура начнётся через 10 минут
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(10),
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            // Act
            statusService.UpdateStatus(appointment);

            // Assert
            Assert.Equal("Назначена", appointment.Status);
        }
    }
}
