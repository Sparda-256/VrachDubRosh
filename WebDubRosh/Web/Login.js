document.addEventListener("DOMContentLoaded", function() {
  const loginForm = document.getElementById("loginForm");
  const loginMessage = document.getElementById("loginMessage");

  // Theme toggle functionality
  const themeToggle = document.getElementById('themeToggle');
  const htmlElement = document.documentElement;
  
  // Initialize theme based on system preference if no saved theme
  const savedTheme = localStorage.getItem('theme');
  if (savedTheme === 'dark') {
    htmlElement.classList.add('dark-theme');
    themeToggle.checked = true;
  } else if (savedTheme === null) {
    // Check system preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    if (prefersDark) {
      htmlElement.classList.add('dark-theme');
      themeToggle.checked = true;
      localStorage.setItem('theme', 'dark');
    }
  }
  
  // Handle theme toggle with animation
  themeToggle.addEventListener('change', function() {
    if (this.checked) {
      htmlElement.classList.add('dark-theme');
      localStorage.setItem('theme', 'dark');
    } else {
      htmlElement.classList.remove('dark-theme');
      localStorage.setItem('theme', 'light');
    }
  });

  // Add visual feedback for input fields
  const inputFields = document.querySelectorAll('input[type="text"], input[type="password"]');
  inputFields.forEach(input => {
    input.addEventListener('focus', () => {
      input.parentElement.classList.add('input-focused');
    });
    input.addEventListener('blur', () => {
      input.parentElement.classList.remove('input-focused');
    });
  });

  loginForm.addEventListener("submit", async function(event) {
      event.preventDefault();

      const username = document.getElementById("username").value.trim();
      const password = document.getElementById("password").value.trim();
      loginMessage.textContent = "";
      
      // Visual feedback before sending request
      const loginButton = document.querySelector('.login-button');
      loginButton.disabled = true;
      loginButton.innerHTML = '<div class="spinner"></div><span>Вход...</span>';

      try {
          // Используем относительный путь для работы с localtunnel
          const response = await fetch("/api/auth/login", {
              method: "POST",
              headers: { "Content-Type": "application/json" },
              body: JSON.stringify({ username, password })
          });

          const data = await response.json();
          if (data.success) {
              loginMessage.textContent = "Вход выполнен успешно!";
              loginMessage.style.color = 'var(--accent-color)';
              
              // Перенаправление через короткую задержку
              setTimeout(() => {
                  // Перенаправление в зависимости от роли
                  if (data.role === "ChiefDoctor") {
                      window.location.href = "chief.html";
                  } else if (data.role === "Doctor") {
                      window.location.href = "doctor.html?id=" + data.doctorID;
                  } else if (data.role === "Manager") {
                      window.location.href = "manager.html?id=" + data.managerID;
                  } else {
                      loginMessage.textContent = "Неизвестная роль пользователя.";
                      loginMessage.style.color = '#dc3545';
                      resetButton();
                  }
              }, 500);
          } else {
              loginMessage.textContent = data.message || "Неверный логин или пароль.";
              loginMessage.style.color = '#dc3545';
              resetButton();
          }
      } catch (error) {
          console.error(error);
          loginMessage.textContent = "Ошибка соединения с сервером.";
          loginMessage.style.color = '#dc3545';
          resetButton();
      }
      
      function resetButton() {
          loginButton.disabled = false;
          loginButton.innerHTML = '<svg class="login-icon" viewBox="0 0 24 24"><path d="M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16.42,14 20,15.79 20,18V20H4V18C4,15.79 7.58,14 12,14M12,2A6,6 0 0,0 6,8A6,6 0 0,0 12,14A6,6 0 0,0 18,8A6,6 0 0,0 12,2Z" /></svg><span>Войти в систему</span>';
      }
  });
});
