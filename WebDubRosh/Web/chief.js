document.addEventListener('DOMContentLoaded', function() {
  // Инициализация переключателя темы
  const themeToggle = document.getElementById('themeToggle');
  
  // Проверка, сохранена ли тема в localStorage
  const savedTheme = localStorage.getItem('theme');
  if (savedTheme === 'dark') {
    document.body.classList.add('dark-theme');
    themeToggle.checked = true;
  }
  
  // Обработчик переключения темы
  themeToggle.addEventListener('change', function() {
    if (this.checked) {
      document.body.classList.add('dark-theme');
      localStorage.setItem('theme', 'dark');
    } else {
      document.body.classList.remove('dark-theme');
      localStorage.setItem('theme', 'light');
    }
  });
  
  // Переключение вкладок
  const tabButtons = document.querySelectorAll('.tab-btn');
  tabButtons.forEach(btn => {
    btn.addEventListener('click', function() {
      // Деактивировать все вкладки
      tabButtons.forEach(b => b.classList.remove('active'));
      document.querySelectorAll('.tab-content').forEach(tab => tab.classList.remove('active'));
      
      // Активировать выбранную вкладку
      this.classList.add('active');
      const tabId = this.getAttribute('data-tab');
      document.getElementById(tabId).classList.add('active');
    });
  });
  
  // Загрузка данных
  loadPatients();
  loadDoctors();
  
  // События поиска
  document.getElementById('searchPatients').addEventListener('input', filterPatients);
  document.getElementById('searchDoctors').addEventListener('input', filterDoctors);
  
  // События кнопок
  document.getElementById('reportsBtn').addEventListener('click', function() {
    alert('Отчеты и аналитика. Функция будет реализована позже.');
  });
  
  document.getElementById('exitBtn').addEventListener('click', function() {
    window.location.href = 'login.html';
  });
  
  document.getElementById('createDischargeBtn').addEventListener('click', function() {
    const selectedRow = document.querySelector('#patientsTable tbody tr.selected');
    if (!selectedRow) {
      alert('Выберите пациента для создания выписного эпикриза.');
      return;
    }
    
    alert('Функция создания выписного эпикриза будет реализована позже.');
  });
  
  document.getElementById('assignDoctorsBtn').addEventListener('click', function() {
    const selectedRow = document.querySelector('#patientsTable tbody tr.selected');
    if (!selectedRow) {
      alert('Выберите пациента для назначения врача.');
      return;
    }
    
    alert('Функция назначения врача пациенту будет реализована позже.');
  });
  
  document.getElementById('addDoctorBtn').addEventListener('click', function() {
    alert('Функция добавления врача будет реализована позже.');
  });
  
  document.getElementById('editDoctorBtn').addEventListener('click', function() {
    const selectedRow = document.querySelector('#doctorsTable tbody tr.selected');
    if (!selectedRow) {
      alert('Выберите врача для редактирования.');
      return;
    }
    
    alert('Функция редактирования врача будет реализована позже.');
  });
  
  document.getElementById('deleteDoctorBtn').addEventListener('click', function() {
    const selectedRow = document.querySelector('#doctorsTable tbody tr.selected');
    if (!selectedRow) {
      alert('Выберите врача для удаления.');
      return;
    }
    
    const doctorID = selectedRow.dataset.doctorId;
    const doctorName = selectedRow.querySelector('td:first-child').textContent;
    
    if (confirm(`Вы уверены, что хотите удалить врача "${doctorName}"?`)) {
      // Используем относительный путь для запроса
      fetch(`/api/chief/doctor/${doctorID}`, {
        method: 'DELETE'
      })
      .then(response => response.json())
      .then(result => {
        if(result.success) {
          alert('Врач удалён.');
          loadDoctors();
        } else {
          alert('Ошибка при удалении врача: ' + result.message);
        }
      })
      .catch(err => {
        console.error(err);
        alert('Ошибка при соединении с сервером.');
      });
    }
  });
});

// Глобальные переменные для хранения данных
let patientsData = [];
let doctorsData = [];

// Загрузка пациентов
function loadPatients() {
  fetch('/api/chief/patients')
    .then(response => response.json())
    .then(data => {
      patientsData = data;
      renderPatientsTable();
    })
    .catch(err => {
      console.error('Ошибка загрузки пациентов', err);
      alert('Не удалось загрузить список пациентов.');
    });
}

// Отрисовка таблицы пациентов
function renderPatientsTable() {
  const tbody = document.querySelector('#patientsTable tbody');
  tbody.innerHTML = '';
  
  patientsData.forEach(patient => {
    const tr = document.createElement('tr');
    tr.dataset.patientId = patient.PatientID || patient.patientID;
    
    tr.innerHTML = `
      <td>${patient.FullName || patient.fullName}</td>
      <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
      <td>${patient.Gender || patient.gender}</td>
      <td>${formatDate(patient.RecordDate || patient.recordDate)}</td>
      <td>${formatDate(patient.DischargeDate || patient.dischargeDate)}</td>
    `;
    
    // Обработчик клика для выделения строки
    tr.addEventListener('click', function() {
      tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
      this.classList.add('selected');
    });
    
    // Обработчик двойного клика для открытия медкарты
    tr.addEventListener('dblclick', function() {
      alert(`Открытие медицинской карты пациента "${patient.FullName || patient.fullName}" будет реализовано позже.`);
    });
    
    tbody.appendChild(tr);
  });
}

// Загрузка врачей
function loadDoctors() {
  fetch('/api/chief/doctors')
    .then(response => response.json())
    .then(data => {
      doctorsData = data;
      renderDoctorsTable();
    })
    .catch(err => {
      console.error('Ошибка загрузки врачей', err);
      alert('Не удалось загрузить список врачей.');
    });
}

// Отрисовка таблицы врачей
function renderDoctorsTable() {
  const tbody = document.querySelector('#doctorsTable tbody');
  tbody.innerHTML = '';
  
  doctorsData.forEach(doctor => {
    const tr = document.createElement('tr');
    tr.dataset.doctorId = doctor.DoctorID || doctor.doctorID;
    
    tr.innerHTML = `
      <td>${doctor.FullName || doctor.fullName}</td>
      <td>${doctor.Specialty || doctor.specialty}</td>
      <td>${doctor.GeneralName || doctor.generalName || ''}</td>
      <td>${doctor.OfficeNumber || doctor.officeNumber}</td>
      <td>${doctor.WorkExperience || doctor.workExperience}</td>
    `;
    
    // Обработчик клика для выделения строки
    tr.addEventListener('click', function() {
      tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
      this.classList.add('selected');
    });
    
    // Обработчик двойного клика для открытия списка процедур
    tr.addEventListener('dblclick', function() {
      alert(`Открытие списка процедур врача "${doctor.FullName || doctor.fullName}" будет реализовано позже.`);
    });
    
    tbody.appendChild(tr);
  });
}

// Фильтрация пациентов
function filterPatients() {
  const searchText = document.getElementById('searchPatients').value.toLowerCase();
  const filteredData = patientsData.filter(patient => 
    (patient.FullName || patient.fullName).toLowerCase().includes(searchText)
  );
  
  const tbody = document.querySelector('#patientsTable tbody');
  tbody.innerHTML = '';
  
  filteredData.forEach(patient => {
    const tr = document.createElement('tr');
    tr.dataset.patientId = patient.PatientID || patient.patientID;
    
    tr.innerHTML = `
      <td>${patient.FullName || patient.fullName}</td>
      <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
      <td>${patient.Gender || patient.gender}</td>
      <td>${formatDate(patient.RecordDate || patient.recordDate)}</td>
      <td>${formatDate(patient.DischargeDate || patient.dischargeDate)}</td>
    `;
    
    tr.addEventListener('click', function() {
      tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
      this.classList.add('selected');
    });
    
    tr.addEventListener('dblclick', function() {
      alert(`Открытие медицинской карты пациента "${patient.FullName || patient.fullName}" будет реализовано позже.`);
    });
    
    tbody.appendChild(tr);
  });
}

// Фильтрация врачей
function filterDoctors() {
  const searchText = document.getElementById('searchDoctors').value.toLowerCase();
  const filteredData = doctorsData.filter(doctor => 
    (doctor.FullName || doctor.fullName).toLowerCase().includes(searchText)
  );
  
  const tbody = document.querySelector('#doctorsTable tbody');
  tbody.innerHTML = '';
  
  filteredData.forEach(doctor => {
    const tr = document.createElement('tr');
    tr.dataset.doctorId = doctor.DoctorID || doctor.doctorID;
    
    tr.innerHTML = `
      <td>${doctor.FullName || doctor.fullName}</td>
      <td>${doctor.Specialty || doctor.specialty}</td>
      <td>${doctor.GeneralName || doctor.generalName || ''}</td>
      <td>${doctor.OfficeNumber || doctor.officeNumber}</td>
      <td>${doctor.WorkExperience || doctor.workExperience}</td>
    `;
    
    tr.addEventListener('click', function() {
      tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
      this.classList.add('selected');
    });
    
    tr.addEventListener('dblclick', function() {
      alert(`Открытие списка процедур врача "${doctor.FullName || doctor.fullName}" будет реализовано позже.`);
    });
    
    tbody.appendChild(tr);
  });
}

// Форматирование даты
function formatDate(dateString) {
  if (!dateString) return '';
  
  const date = new Date(dateString);
  if (isNaN(date.getTime())) return dateString; // Если не удалось преобразовать
  
  const day = ('0' + date.getDate()).slice(-2);
  const month = ('0' + (date.getMonth() + 1)).slice(-2);
  const year = date.getFullYear();
  
  return `${day}.${month}.${year}`;
}
