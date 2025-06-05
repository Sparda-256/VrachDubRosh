using System;
using System.Collections.Generic;
using System.Linq;

namespace TestDubRosh
{
    public class BuildingModel
    {
        public int BuildingID { get; set; }
        public string BuildingName { get; set; }
    }

    public class RoomModel
    {
        public int RoomID { get; set; }
        public int BuildingID { get; set; }
        public string BuildingName { get; set; }
        public string RoomName { get; set; }
    }

    public class BedModel
    {
        public int BedID { get; set; }
        public int RoomID { get; set; }
        public string BedName { get; set; }
        public bool IsOccupied { get; set; }
    }

    public class AccommodationModel
    {
        public int AccommodationID { get; set; }
        public int? PatientID { get; set; }
        public int? AccompanyingID { get; set; }
        public string BuildingName { get; set; }
        public string RoomName { get; set; }
        public string BedName { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
    }

    public class OccupancyStatistics
    {
        public int TotalBedCount { get; set; }
        public int OccupiedBedCount { get; set; }
        public double OccupancyRate { get; set; }
        public Dictionary<string, int> OccupancyByBuilding { get; set; }
    }

    public class AccommodationService
    {
        private List<BuildingModel> _buildings;
        private List<RoomModel> _rooms;
        private List<BedModel> _beds;
        private List<AccommodationModel> _accommodations;
        private int _nextAccommodationId = 3;

        public AccommodationService()
        {
            // Инициализация зданий
            _buildings = new List<BuildingModel>
            {
                new BuildingModel { BuildingID = 1, BuildingName = "Корпус 1" },
                new BuildingModel { BuildingID = 2, BuildingName = "Корпус 2" }
            };

            // Инициализация комнат
            _rooms = new List<RoomModel>
            {
                new RoomModel { RoomID = 1, BuildingID = 1, BuildingName = "Корпус 1", RoomName = "Комната 1" },
                new RoomModel { RoomID = 2, BuildingID = 1, BuildingName = "Корпус 1", RoomName = "Комната 2" },
                new RoomModel { RoomID = 3, BuildingID = 2, BuildingName = "Корпус 2", RoomName = "Комната 1А" },
                new RoomModel { RoomID = 4, BuildingID = 2, BuildingName = "Корпус 2", RoomName = "Комната 1Б" }
            };

            // Инициализация кроватей
            _beds = new List<BedModel>
            {
                new BedModel { BedID = 1, RoomID = 1, BedName = "Кровать 1", IsOccupied = false },
                new BedModel { BedID = 2, RoomID = 1, BedName = "Кровать 2", IsOccupied = false },
                new BedModel { BedID = 3, RoomID = 2, BedName = "Кровать 1", IsOccupied = false },
                new BedModel { BedID = 4, RoomID = 2, BedName = "Кровать 2", IsOccupied = false },
                new BedModel { BedID = 5, RoomID = 3, BedName = "Кровать 1", IsOccupied = true },  // Занята
                new BedModel { BedID = 6, RoomID = 3, BedName = "Кровать 2", IsOccupied = false },
                new BedModel { BedID = 7, RoomID = 3, BedName = "Кровать 3", IsOccupied = false },
                new BedModel { BedID = 8, RoomID = 4, BedName = "Кровать 1", IsOccupied = false },
                new BedModel { BedID = 9, RoomID = 4, BedName = "Кровать 2", IsOccupied = false }
            };

            // Инициализация размещений
            _accommodations = new List<AccommodationModel>
            {
                new AccommodationModel
                {
                    AccommodationID = 1,
                    PatientID = 1,
                    BuildingName = "Корпус 2",
                    RoomName = "Комната 1А",
                    BedName = "Кровать 1",
                    CheckInDate = DateTime.Now.AddDays(-5)
                },
                new AccommodationModel
                {
                    AccommodationID = 2,
                    AccompanyingID = 1,
                    BuildingName = "Корпус 2",
                    RoomName = "Комната 1А",
                    BedName = "Кровать 1",
                    CheckInDate = DateTime.Now.AddDays(-5)
                }
            };
        }

        public List<BuildingModel> GetAllBuildings()
        {
            return _buildings.ToList();
        }

        public List<RoomModel> GetRoomsByBuilding(string buildingName)
        {
            var building = _buildings.FirstOrDefault(b => b.BuildingName == buildingName);
            if (building == null)
                return new List<RoomModel>();

            return _rooms.Where(r => r.BuildingID == building.BuildingID).ToList();
        }

        public List<BedModel> GetAvailableBeds(string buildingName, string roomName)
        {
            var room = _rooms.FirstOrDefault(r => r.BuildingName == buildingName && r.RoomName == roomName);
            if (room == null)
                return new List<BedModel>();

            return _beds.Where(b => b.RoomID == room.RoomID && !b.IsOccupied).ToList();
        }

        public bool AssignPatientToBed(int patientId, string buildingName, string roomName, string bedName)
        {
            // Найдем нужную кровать
            var room = _rooms.FirstOrDefault(r => r.BuildingName == buildingName && r.RoomName == roomName);
            if (room == null)
                return false;

            var bed = _beds.FirstOrDefault(b => b.RoomID == room.RoomID && b.BedName == bedName);
            if (bed == null || bed.IsOccupied)
                return false;

            // Отметим кровать как занятую
            bed.IsOccupied = true;

            // Добавим запись о размещении
            _accommodations.Add(new AccommodationModel
            {
                AccommodationID = _nextAccommodationId++,
                PatientID = patientId,
                BuildingName = buildingName,
                RoomName = roomName,
                BedName = bedName,
                CheckInDate = DateTime.Now
            });

            return true;
        }

        public bool AssignAccompanyingToBed(int accompanyingId, string buildingName, string roomName, string bedName)
        {
            // Найдем нужную кровать
            var room = _rooms.FirstOrDefault(r => r.BuildingName == buildingName && r.RoomName == roomName);
            if (room == null)
                return false;

            var bed = _beds.FirstOrDefault(b => b.RoomID == room.RoomID && b.BedName == bedName);
            if (bed == null || bed.IsOccupied)
                return false;

            // Отметим кровать как занятую
            bed.IsOccupied = true;

            // Добавим запись о размещении
            _accommodations.Add(new AccommodationModel
            {
                AccommodationID = _nextAccommodationId++,
                AccompanyingID = accompanyingId,
                BuildingName = buildingName,
                RoomName = roomName,
                BedName = bedName,
                CheckInDate = DateTime.Now
            });

            return true;
        }

        public bool CheckoutPatient(int patientId)
        {
            var accommodation = _accommodations.FirstOrDefault(a => a.PatientID == patientId && !a.CheckOutDate.HasValue);
            if (accommodation == null)
                return false;

            // Отмечаем дату выселения
            accommodation.CheckOutDate = DateTime.Now;

            // Освобождаем кровать
            var room = _rooms.FirstOrDefault(r => r.BuildingName == accommodation.BuildingName && r.RoomName == accommodation.RoomName);
            if (room != null)
            {
                var bed = _beds.FirstOrDefault(b => b.RoomID == room.RoomID && b.BedName == accommodation.BedName);
                if (bed != null)
                {
                    bed.IsOccupied = false;
                }
            }

            return true;
        }

        public bool CheckoutAccompanying(int accompanyingId)
        {
            var accommodation = _accommodations.FirstOrDefault(a => a.AccompanyingID == accompanyingId && !a.CheckOutDate.HasValue);
            if (accommodation == null)
                return false;

            // Отмечаем дату выселения
            accommodation.CheckOutDate = DateTime.Now;

            // Освобождаем кровать
            var room = _rooms.FirstOrDefault(r => r.BuildingName == accommodation.BuildingName && r.RoomName == accommodation.RoomName);
            if (room != null)
            {
                var bed = _beds.FirstOrDefault(b => b.RoomID == room.RoomID && b.BedName == accommodation.BedName);
                if (bed != null)
                {
                    bed.IsOccupied = false;
                }
            }

            return true;
        }

        public OccupancyStatistics GetOccupancyStatistics()
        {
            int totalBeds = _beds.Count;
            int occupiedBeds = _beds.Count(b => b.IsOccupied);
            
            var occupancyByBuilding = new Dictionary<string, int>();
            foreach (var building in _buildings)
            {
                var roomsInBuilding = _rooms.Where(r => r.BuildingID == building.BuildingID).Select(r => r.RoomID).ToList();
                int occupiedBedsInBuilding = _beds.Count(b => roomsInBuilding.Contains(b.RoomID) && b.IsOccupied);
                occupancyByBuilding[building.BuildingName] = occupiedBedsInBuilding;
            }

            return new OccupancyStatistics
            {
                TotalBedCount = totalBeds,
                OccupiedBedCount = occupiedBeds,
                OccupancyRate = (double)occupiedBeds / totalBeds * 100,
                OccupancyByBuilding = occupancyByBuilding
            };
        }

        public List<AccommodationModel> GetAllAccommodations()
        {
            return _accommodations.Where(a => !a.CheckOutDate.HasValue).ToList();
        }
    }
} 