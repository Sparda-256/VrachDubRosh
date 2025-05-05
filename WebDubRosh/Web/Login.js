document.addEventListener("DOMContentLoaded", function() {
  const loginForm = document.getElementById("loginForm");
  const loginMessage = document.getElementById("loginMessage");

  loginForm.addEventListener("submit", async function(event) {
      event.preventDefault();

      const username = document.getElementById("username").value.trim();
      const password = document.getElementById("password").value.trim();
      loginMessage.textContent = "";

      try {
          // Используем относительный путь для работы с localtunnel
          const response = await fetch("/api/auth/login", {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ username, password })
          });

          const data = await response.json();
          if (data.success) {
              // Перенаправление в зависимости от роли
              if (data.role === "ChiefDoctor") {
                  window.location.href = "chief.html";
              } else if (data.role === "Doctor") {
                  window.location.href = "doctor.html?id=" + data.doctorID;
              } else if (data.role === "Manager") {
                  window.location.href = "manager.html";
              } else {
                  loginMessage.textContent = "Неизвестная роль пользователя.";
              }
          } else {
              loginMessage.textContent = data.message || "Неверный логин или пароль.";
          }
      } catch (error) {
          console.error(error);
          loginMessage.textContent = "Ошибка соединения с сервером.";
      }
  });
});
