using System;
using System.Collections.Generic;

namespace VrachDubRosh
{
    public class Building
    {
        public int BuildingID { get; set; }
        public int BuildingNumber { get; set; }
        public string Description { get; set; }
        
        public override string ToString()
        {
            return $"Корпус {BuildingNumber}";
        }
    }

    public class Room
    {
        public int RoomID { get; set; }
        public string RoomNumber { get; set; }
        public bool IsAvailable { get; set; }
        public List<int> AvailableBeds { get; set; } = new List<int>();
        
        public override string ToString()
        {
            return $"Комната {RoomNumber}";
        }
    }
} 