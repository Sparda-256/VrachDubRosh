using System;

namespace TestDubRosh
{
    public class AuthenticationService
    {
        // Имитация подключения к базе данных
        public bool AuthenticateManager(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Для тестирования считаем, что только один менеджер действителен
            return username == "Петрова Ольга Николаевна" && password == "manager2023";
        }

        public bool AuthenticateChiefDoctor(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Для тестирования считаем, что только один главврач действителен
            return username == "Соколов Михаил Андреевич" && password == "Doc$2023";
        }

        public bool AuthenticateDoctor(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Для тестирования считаем, что только один врач действителен
            return username == "Иванов Иван Иванович" && password == "LFK_2023!";
        }
    }
} 