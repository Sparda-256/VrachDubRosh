using System;
using Xunit;
using System.Collections.Generic;

namespace TestDubRosh
{
    public class AccommodationTests
    {
        [Fact]
        public void GetAvailableBeds_ShouldReturnAvailableBeds()
        {
            // Arrange
            string buildingName = "Корпус 2";
            string roomName = "Комната 1А";
            var accommodationService = new AccommodationService();

            // Act
            var availableBeds = accommodationService.GetAvailableBeds(buildingName, roomName);

            // Assert
            Assert.NotEmpty(availableBeds);
            Assert.Contains(availableBeds, b => b.BedName == "Кровать 2");
        }

        [Fact]
        public void GetAllBuildings_ShouldReturnAllBuildings()
        {
            // Arrange
            var accommodationService = new AccommodationService();

            // Act
            var buildings = accommodationService.GetAllBuildings();

            // Assert
            Assert.NotEmpty(buildings);
            Assert.Equal(2, buildings.Count);
            Assert.Contains(buildings, b => b.BuildingName == "Корпус 1");
            Assert.Contains(buildings, b => b.BuildingName == "Корпус 2");
        }

        [Fact]
        public void GetRoomsByBuilding_ShouldReturnRooms()
        {
            // Arrange
            string buildingName = "Корпус 1";
            var accommodationService = new AccommodationService();

            // Act
            var rooms = accommodationService.GetRoomsByBuilding(buildingName);

            // Assert
            Assert.NotEmpty(rooms);
            Assert.Contains(rooms, r => r.RoomName == "Комната 1");
            Assert.Contains(rooms, r => r.RoomName == "Комната 2");
        }

        [Fact]
        public void AssignPatientToBed_ShouldAssignSuccessfully()
        {
            // Arrange
            int patientId = 2; // Используем другого пациента для нового размещения
            string buildingName = "Корпус 1";
            string roomName = "Комната 2";
            string bedName = "Кровать 1";
            var accommodationService = new AccommodationService();

            // Act
            bool result = accommodationService.AssignPatientToBed(patientId, buildingName, roomName, bedName);

            // Assert
            Assert.True(result);
            
            // Проверяем, что кровать занята
            var accommodations = accommodationService.GetAllAccommodations();
            Assert.Contains(accommodations, a => 
                a.PatientID == patientId && 
                a.BuildingName == buildingName && 
                a.RoomName == roomName && 
                a.BedName == bedName);
        }

        [Fact]
        public void AssignPatientToOccupiedBed_ShouldReturnFalse()
        {
            // Arrange
            int patientId = 3;
            string buildingName = "Корпус 2";
            string roomName = "Комната 1А";
            string bedName = "Кровать 1"; // Эта кровать уже занята
            var accommodationService = new AccommodationService();

            // Act
            bool result = accommodationService.AssignPatientToBed(patientId, buildingName, roomName, bedName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CheckoutPatient_ShouldCheckoutSuccessfully()
        {
            // Arrange
            int patientId = 1;
            var accommodationService = new AccommodationService();

            // Act
            bool result = accommodationService.CheckoutPatient(patientId);

            // Assert
            Assert.True(result);
            
            // Проверяем, что размещение удалено
            var accommodations = accommodationService.GetAllAccommodations();
            Assert.DoesNotContain(accommodations, a => a.PatientID == patientId);
        }

        [Fact]
        public void GetOccupancyStatistics_ShouldReturnCorrectStats()
        {
            // Arrange
            var accommodationService = new AccommodationService();

            // Act
            var stats = accommodationService.GetOccupancyStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.TotalBedCount > 0);
            Assert.True(stats.OccupiedBedCount <= stats.TotalBedCount);
            
            // Проверяем процент занятости
            double expectedOccupancyRate = (double)stats.OccupiedBedCount / stats.TotalBedCount * 100;
            Assert.Equal(expectedOccupancyRate, stats.OccupancyRate);
        }

        [Fact]
        public void AssignAccompanyingToBed_ShouldAssignSuccessfully()
        {
            // Arrange
            int accompanyingId = 2;
            string buildingName = "Корпус 2";
            string roomName = "Комната 1А";
            string bedName = "Кровать 3";
            var accommodationService = new AccommodationService();

            // Act
            bool result = accommodationService.AssignAccompanyingToBed(accompanyingId, buildingName, roomName, bedName);

            // Assert
            Assert.True(result);
            
            // Проверяем, что кровать занята сопровождающим
            var accommodations = accommodationService.GetAllAccommodations();
            Assert.Contains(accommodations, a => 
                a.AccompanyingID == accompanyingId && 
                a.BuildingName == buildingName && 
                a.RoomName == roomName && 
                a.BedName == bedName);
        }
    }
} 