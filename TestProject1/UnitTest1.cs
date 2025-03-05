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
        public bool IsDoctorOccupied(DateTime newStart, int newDuration)
        {
            DateTime newEnd = newStart.AddMinutes(newDuration);
            foreach (var (start, duration) in _appointments)
            {
                DateTime end = start.AddMinutes(duration);
                if (newStart < end && start < newEnd)
                    return true;
            }
            return false;
        }
    }

    public class Appointment
    {
        public DateTime AppointmentDateTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
    }

    public class AppointmentStatusService
    {
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
        }
    }

    public class AuthenticationServiceTests
    {
        [Fact]
        public void Authenticate_ValidCredentials_ReturnsDoctorId()
        {
            var authService = new AuthenticationService();
            string login = "ValidDoctor";
            string password = "ValidPassword";

            int? doctorId = authService.AuthenticateDoctor(login, password);

            Assert.NotNull(doctorId);
        }

        [Fact]
        public void Authenticate_InvalidCredentials_ReturnsNull()
        {
            var authService = new AuthenticationService();
            string login = "InvalidLogin";
            string password = "InvalidPassword";

            int? doctorId = authService.AuthenticateDoctor(login, password);

            Assert.Null(doctorId);
        }
    }

    public class SchedulingServiceTests
    {
        [Theory]
        [InlineData("2025-01-01T10:00:00", 30, "2025-01-01T10:15:00", 30, true)]
        [InlineData("2025-01-01T10:00:00", 30, "2025-01-01T10:35:00", 30, false)]
        public void IsDoctorOccupied_ReturnsExpected(string existingStartStr, int existingDuration,
                                                     string newStartStr, int newDuration, bool expected)
        {
            DateTime existingStart = DateTime.Parse(existingStartStr);
            DateTime newStart = DateTime.Parse(newStartStr);
            var schedulingService = new SchedulingService();
            schedulingService.AddAppointment(existingStart, existingDuration);

            bool conflict = schedulingService.IsDoctorOccupied(newStart, newDuration);

            Assert.Equal(expected, conflict);
        }
    }

    public class AppointmentStatusTests
    {
        [Fact]
        public void UpdateStatus_AppointmentEnded_StatusBecomesCompleted()
        {
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(-60),
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            statusService.UpdateStatus(appointment);

            Assert.Equal("Завершена", appointment.Status);
        }

        [Fact]
        public void UpdateStatus_AppointmentInProgress_StatusBecomesInProgress()
        {
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(-10),
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            statusService.UpdateStatus(appointment);

            Assert.Equal("Идёт", appointment.Status);
        }

        [Fact]
        public void UpdateStatus_AppointmentNotStarted_StatusRemainsScheduled()
        {
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(10),
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            statusService.UpdateStatus(appointment);

            Assert.Equal("Назначена", appointment.Status);
        }
    }
    public class AdditionalSchedulingServiceTests
    {
        [Theory]
        [InlineData("2025-01-01T10:00:00", 30, "2025-01-01T10:30:00", 30, false)]
        [InlineData("2025-01-01T10:30:00", 30, "2025-01-01T10:00:00", 30, false)]
        public void IsDoctorOccupied_AdjacentAppointments_NoConflict(string existingStartStr, int existingDuration,
            string newStartStr, int newDuration, bool expected)
        {
            DateTime existingStart = DateTime.Parse(existingStartStr);
            DateTime newStart = DateTime.Parse(newStartStr);
            var schedulingService = new SchedulingService();
            schedulingService.AddAppointment(existingStart, existingDuration);

            bool conflict = schedulingService.IsDoctorOccupied(newStart, newDuration);

            Assert.Equal(expected, conflict);
        }

        [Fact]
        public void IsDoctorOccupied_MultipleAppointments_NoConflict()
        {
            var schedulingService = new SchedulingService();
            schedulingService.AddAppointment(new DateTime(2025, 1, 1, 9, 0, 0), 30);
            schedulingService.AddAppointment(new DateTime(2025, 1, 1, 10, 0, 0), 30);
            DateTime newStart = new DateTime(2025, 1, 1, 9, 30, 0);
            int newDuration = 30;

            bool conflict = schedulingService.IsDoctorOccupied(newStart, newDuration);

            Assert.False(conflict);
        }
    }

    public class AdditionalAppointmentStatusTests
    {
        [Fact]
        public void UpdateStatus_AppointmentStartsNow_StatusBecomesInProgress()
        {
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now,
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            statusService.UpdateStatus(appointment);

            Assert.Equal("Идёт", appointment.Status);
        }

        [Fact]
        public void UpdateStatus_AppointmentEndsNow_StatusBecomesCompleted()
        {
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(-30),
                Duration = 30,
                Status = "Назначена"
            };
            var statusService = new AppointmentStatusService();

            statusService.UpdateStatus(appointment);

            Assert.Equal("Завершена", appointment.Status);
        }
    }
}