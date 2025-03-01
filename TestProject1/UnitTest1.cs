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
            // ���� ����� � ������ ���������, ���������� ������������� �����.
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
        /// ���������, ������������ �� ����� ���������� � ��� ����������������.
        /// </summary>
        public bool IsDoctorOccupied(DateTime newStart, int newDuration)
        {
            DateTime newEnd = newStart.AddMinutes(newDuration);
            foreach (var (start, duration) in _appointments)
            {
                DateTime end = start.AddMinutes(duration);
                // ���� ��������� ������������, ���������� true.
                if (newStart < end && start < newEnd)
                    return true;
            }
            return false;
        }
    }

    public class Appointment
    {
        public DateTime AppointmentDateTime { get; set; }
        public int Duration { get; set; }  // ������������ � �������
        public string Status { get; set; } // "���������", "���", "���������", "��������"
    }

    public class AppointmentStatusService
    {
        /// <summary>
        /// ��������� ������ ���������� � ����������� �� �������� �������.
        /// </summary>
        public void UpdateStatus(Appointment appointment)
        {
            DateTime now = DateTime.Now;
            DateTime appointmentEnd = appointment.AppointmentDateTime.AddMinutes(appointment.Duration);
            if (now >= appointmentEnd)
            {
                appointment.Status = "���������";
            }
            else if (now >= appointment.AppointmentDateTime)
            {
                appointment.Status = "���";
            }
            // ���� �� ������ � ��������� ������ "���������"
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
        // ����� �������� ������������ � ��� ���������������
        [InlineData("2025-01-01T10:00:00", 30, "2025-01-01T10:15:00", 30, true)]
        // ����� �������� �� ������������ (���������� ����� ����������)
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
            // Arrange: ��������� �������� 60 ����� �����, ������������ 30 �����
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(-60),
                Duration = 30,
                Status = "���������"
            };
            var statusService = new AppointmentStatusService();

            // Act
            statusService.UpdateStatus(appointment);

            // Assert
            Assert.Equal("���������", appointment.Status);
        }

        [Fact]
        public void UpdateStatus_AppointmentInProgress_StatusBecomesInProgress()
        {
            // Arrange: ��������� �������� 10 ����� �����, ������������ 30 �����
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(-10),
                Duration = 30,
                Status = "���������"
            };
            var statusService = new AppointmentStatusService();

            // Act
            statusService.UpdateStatus(appointment);

            // Assert
            Assert.Equal("���", appointment.Status);
        }

        [Fact]
        public void UpdateStatus_AppointmentNotStarted_StatusRemainsScheduled()
        {
            // Arrange: ��������� ������� ����� 10 �����
            var appointment = new Appointment
            {
                AppointmentDateTime = DateTime.Now.AddMinutes(10),
                Duration = 30,
                Status = "���������"
            };
            var statusService = new AppointmentStatusService();

            // Act
            statusService.UpdateStatus(appointment);

            // Assert
            Assert.Equal("���������", appointment.Status);
        }
    }
}
