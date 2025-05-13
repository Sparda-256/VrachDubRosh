document.addEventListener('DOMContentLoaded', function() {
  // Инициализация
  initThemeToggle();
  initTabs();
  initTableSorting();
  initModals();
  loadPatients();
  loadDoctors();
  
  // События поиска
  document.getElementById('searchPatients').addEventListener('input', filterPatients);
  document.getElementById('searchDoctors').addEventListener('input', filterDoctors);
  
  // События кнопок
  document.getElementById('reportsBtn').addEventListener('click', showReportsModal);
  document.getElementById('exitBtn').addEventListener('click', function() {
    window.location.href = 'Login.html';
  });
  
  document.getElementById('createDischargeBtn').addEventListener('click', showDischargeModal);
  document.getElementById('assignDoctorsBtn').addEventListener('click', showAssignDoctorModal);
  document.getElementById('addDoctorBtn').addEventListener('click', function() {
    showDoctorModal('add');
  });
  document.getElementById('editDoctorBtn').addEventListener('click', function() {
    const selectedRow = document.querySelector('#doctorsTable tbody tr.selected');
    if (!selectedRow) {
      showNotification('Выберите врача для редактирования', 'error');
      return;
    }
    
    showDoctorModal('edit', selectedRow.dataset.id);
  });
  
  document.getElementById('deleteDoctorBtn').addEventListener('click', deleteDoctor);
  
  // Формы
  document.getElementById('doctorForm').addEventListener('submit', saveDoctor);
  document.getElementById('dischargeForm').addEventListener('submit', createDischargeDocument);
  document.getElementById('confirmAssignBtn').addEventListener('click', assignDoctor);
  document.getElementById('generatePatientReportBtn').addEventListener('click', generatePatientReport);
  
  // Закрытие модальных окон
  const closeBtns = document.querySelectorAll('.close-btn, .cancel-btn');
  closeBtns.forEach(btn => {
    btn.addEventListener('click', function() {
      const modal = this.closest('.modal');
      if (modal) {
        modal.style.display = 'none';
      }
    });
  });
});

// Глобальные переменные для хранения данных
let patientsData = [];
let doctorsData = [];
let currentSortColumn = null;
let currentSortDirection = 'asc';

  // Инициализация переключателя темы
function initThemeToggle() {
  const themeToggle = document.getElementById('themeToggle');
  
  // Проверка сохраненной темы
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
}
  
// Инициализация вкладок
function initTabs() {
  const tabButtons = document.querySelectorAll('.tab-btn');
  tabButtons.forEach(btn => {
    btn.addEventListener('click', function() {
      // Деактивация всех вкладок
      tabButtons.forEach(b => b.classList.remove('active'));
      document.querySelectorAll('.tab-content').forEach(tab => tab.classList.remove('active'));
      
      // Активация выбранной вкладки
      this.classList.add('active');
      const tabId = this.getAttribute('data-tab');
      document.getElementById(tabId).classList.add('active');
    });
  });
  
  // Вкладки в модальном окне отчетов
  const reportTabButtons = document.querySelectorAll('.report-tab-btn');
  reportTabButtons.forEach(btn => {
    btn.addEventListener('click', function() {
      reportTabButtons.forEach(b => b.classList.remove('active'));
      this.classList.add('active');
      // В этой версии у нас только одна вкладка отчетов реализована
    });
  });
}

// Инициализация сортировки таблиц
function initTableSorting() {
  const patientTableHeaders = document.querySelectorAll('#patientsTable th[data-sort]');
  const doctorTableHeaders = document.querySelectorAll('#doctorsTable th[data-sort]');
  
  patientTableHeaders.forEach(header => {
    header.addEventListener('click', function() {
      const sortBy = this.getAttribute('data-sort');
      
      // Если сортируем по тому же столбцу, меняем направление
      if (currentSortColumn === sortBy) {
        currentSortDirection = currentSortDirection === 'asc' ? 'desc' : 'asc';
      } else {
        currentSortColumn = sortBy;
        currentSortDirection = 'asc';
      }
      
      // Удаляем индикаторы сортировки со всех заголовков
      patientTableHeaders.forEach(h => {
        h.classList.remove('sort-asc', 'sort-desc');
      });
      
      // Добавляем индикатор на текущий заголовок
      this.classList.add(currentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');
      
      // Сортируем и обновляем таблицу
      sortAndRenderPatients();
    });
  });
  
  doctorTableHeaders.forEach(header => {
    header.addEventListener('click', function() {
      const sortBy = this.getAttribute('data-sort');
      
      if (currentSortColumn === sortBy) {
        currentSortDirection = currentSortDirection === 'asc' ? 'desc' : 'asc';
      } else {
        currentSortColumn = sortBy;
        currentSortDirection = 'asc';
      }
      
      doctorTableHeaders.forEach(h => {
        h.classList.remove('sort-asc', 'sort-desc');
      });
      
      this.classList.add(currentSortDirection === 'asc' ? 'sort-asc' : 'sort-desc');
      
      sortAndRenderDoctors();
    });
  });
}

// Инициализация модальных окон
function initModals() {
  window.addEventListener('click', function(event) {
    if (event.target.classList.contains('modal')) {
      event.target.style.display = 'none';
    }
  });
  
  // Предотвращаем отправку форм по умолчанию
  document.querySelectorAll('.modal form').forEach(form => {
    form.addEventListener('submit', function(e) {
      e.preventDefault();
      // Обработка отправки формы осуществляется через отдельные обработчики
    });
  });
  
  // Закрытие модальных окон по кнопке закрытия
  document.querySelectorAll('.close-btn').forEach(btn => {
    btn.addEventListener('click', function() {
      const modal = this.closest('.modal');
      if (modal) {
        modal.style.display = 'none';
      }
    });
  });
}

// Загрузка списка пациентов
function loadPatients() {
  fetch('/api/chief/patients')
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки данных пациентов');
      }
      return response.json();
    })
    .then(data => {
      patientsData = data;
      renderPatientsTable();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить список пациентов', 'error');
    });
}

// Загрузка списка врачей
function loadDoctors() {
  fetch('/api/chief/doctors')
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки данных врачей');
      }
      return response.json();
    })
    .then(data => {
      doctorsData = data;
      renderDoctorsTable();
      populateDoctorSelect();
    })
    .catch(error => {
      console.error('Ошибка:', error);
      showNotification('Не удалось загрузить список врачей', 'error');
    });
}

// Отображение таблицы пациентов
function renderPatientsTable() {
  const tbody = document.querySelector('#patientsTable tbody');
  tbody.innerHTML = '';
  
  if (patientsData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Нет данных</td></tr>';
    return;
  }
  
  patientsData.forEach(patient => {
    const tr = document.createElement('tr');
    tr.dataset.id = patient.PatientID || patient.patientId;
    
    tr.innerHTML = `
      <td>${patient.FullName || patient.fullName}</td>
      <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
      <td>${patient.Gender || patient.gender}</td>
      <td>${formatDate(patient.RecordDate || patient.recordDate)}</td>
      <td>${formatDate(patient.DischargeDate || patient.dischargeDate)}</td>
    `;
    
    tr.addEventListener('click', handlePatientRowClick);
    tr.addEventListener('dblclick', openPatientMedCard);
    
    tbody.appendChild(tr);
  });
}

// Отображение таблицы врачей
function renderDoctorsTable() {
  const tbody = document.querySelector('#doctorsTable tbody');
  tbody.innerHTML = '';
  
  if (doctorsData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Нет данных</td></tr>';
    return;
  }
  
  doctorsData.forEach(doctor => {
    const tr = document.createElement('tr');
    tr.dataset.id = doctor.DoctorID || doctor.doctorId;
    
    tr.innerHTML = `
      <td>${doctor.FullName || doctor.fullName}</td>
      <td>${doctor.Specialty || doctor.specialty}</td>
      <td>${doctor.GeneralName || doctor.generalName || ''}</td>
      <td>${doctor.OfficeNumber || doctor.officeNumber}</td>
      <td>${doctor.WorkExperience || doctor.workExperience}</td>
    `;
    
    tr.addEventListener('click', handleDoctorRowClick);
    tr.addEventListener('dblclick', openDoctorProcedures);
    
    tbody.appendChild(tr);
  });
}

// Заполнение списка врачей в модальном окне назначения
function populateDoctorSelect() {
  const doctorsList = document.getElementById('doctorsAssignmentList');
  doctorsList.innerHTML = '';
  
  doctorsData.forEach(doctor => {
    const doctorId = doctor.DoctorID || doctor.doctorId;
    const fullName = doctor.FullName || doctor.fullName;
    const specialty = doctor.Specialty || doctor.specialty;
    
    const item = document.createElement('div');
    item.className = 'doctor-item';
    item.dataset.id = doctorId;
    
    item.innerHTML = `
      <input type="checkbox" class="doctor-checkbox" id="doctor-${doctorId}">
      <label for="doctor-${doctorId}" class="doctor-name">${fullName}</label>
      <span class="doctor-specialty">(${specialty})</span>
    `;
    
    item.addEventListener('click', function(e) {
      // Пропускаем клик по самому чекбоксу, чтобы избежать двойного переключения
      if (e.target.type === 'checkbox') return;
      
      const checkbox = this.querySelector('input[type="checkbox"]');
      checkbox.checked = !checkbox.checked;
    });
    
    doctorsList.appendChild(item);
  });
}

// Обработчик клика по строке таблицы пациентов
function handlePatientRowClick(event) {
  const table = document.querySelector('#patientsTable');
  const rows = table.querySelectorAll('tbody tr');
  
  rows.forEach(row => {
    row.classList.remove('selected');
  });
  
  this.classList.add('selected');
}

// Обработчик клика по строке таблицы врачей
function handleDoctorRowClick(event) {
  const table = document.querySelector('#doctorsTable');
  const rows = table.querySelectorAll('tbody tr');
  
  rows.forEach(row => {
    row.classList.remove('selected');
  });
  
  this.classList.add('selected');
}

// Открытие медкарты пациента
function openPatientMedCard() {
  const patientId = this.dataset.id;
  window.location.href = `medcard.html?id=${patientId}`;
}

// Открытие списка процедур врача
function openDoctorProcedures() {
  const doctorId = this.dataset.id;
  const doctorName = this.querySelector('td:first-child').textContent;
  
  // Обновляем заголовок модального окна
  document.getElementById('doctorProceduresTitle').textContent = `Процедуры врача: ${doctorName}`;
  
  // Очищаем таблицу процедур
  const proceduresTable = document.getElementById('doctorProceduresTable').querySelector('tbody');
  proceduresTable.innerHTML = '';
  
  // Показываем модальное окно
  const modal = document.getElementById('doctorProceduresModal');
  modal.style.display = 'block';
  
  // Загружаем процедуры с сервера
  fetch(`/api/chief/doctor/${doctorId}/procedures`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка при загрузке процедур');
      }
      return response.json();
    })
    .then(data => {
      if (data && data.length > 0) {
        data.forEach(procedure => {
          const row = document.createElement('tr');
          
          // Создаем ячейки для названия и длительности
          const nameCell = document.createElement('td');
          nameCell.textContent = procedure.procedureName || procedure.ProcedureName;
          
          const durationCell = document.createElement('td');
          durationCell.textContent = procedure.duration || procedure.Duration;
          
          // Добавляем ячейки в строку и строку в таблицу
          row.appendChild(nameCell);
          row.appendChild(durationCell);
          proceduresTable.appendChild(row);
        });
      } else {
        // Если процедур нет, показываем сообщение
        const row = document.createElement('tr');
        const cell = document.createElement('td');
        cell.colSpan = 2;
        cell.textContent = 'Процедуры не найдены';
        cell.style.textAlign = 'center';
        row.appendChild(cell);
        proceduresTable.appendChild(row);
      }
    })
    .catch(error => {
      console.error('Ошибка при загрузке процедур:', error);
      showNotification(`Ошибка при загрузке процедур: ${error.message}`, 'error');
    });
}

// Сортировка пациентов
function sortAndRenderPatients() {
  if (!currentSortColumn) return;
  
  patientsData.sort((a, b) => {
    const aValue = a[currentSortColumn] || a[capitalizeFirstLetter(currentSortColumn)];
    const bValue = b[currentSortColumn] || b[capitalizeFirstLetter(currentSortColumn)];
    
    if (currentSortColumn.includes('date')) {
      // Для дат
      const dateA = aValue ? new Date(aValue) : new Date(0);
      const dateB = bValue ? new Date(bValue) : new Date(0);
      
      if (currentSortDirection === 'asc') {
        return dateA - dateB;
      } else {
        return dateB - dateA;
      }
    } else {
      // Для строк и других типов данных
      if (currentSortDirection === 'asc') {
        return String(aValue).localeCompare(String(bValue));
      } else {
        return String(bValue).localeCompare(String(aValue));
      }
    }
  });
  
  renderPatientsTable();
}

// Сортировка врачей
function sortAndRenderDoctors() {
  if (!currentSortColumn) return;
  
  doctorsData.sort((a, b) => {
    const aValue = a[currentSortColumn] || a[capitalizeFirstLetter(currentSortColumn)];
    const bValue = b[currentSortColumn] || b[capitalizeFirstLetter(currentSortColumn)];
    
    if (currentSortColumn === 'workExperience' || currentSortColumn === 'officeNumber') {
      // Для числовых значений
      const numA = parseInt(aValue) || 0;
      const numB = parseInt(bValue) || 0;
      
      if (currentSortDirection === 'asc') {
        return numA - numB;
      } else {
        return numB - numA;
      }
    } else {
      // Для строк
      if (currentSortDirection === 'asc') {
        return String(aValue).localeCompare(String(bValue));
      } else {
        return String(bValue).localeCompare(String(aValue));
      }
    }
  });
  
  renderDoctorsTable();
}

// Фильтрация пациентов по поиску
function filterPatients() {
  const searchText = document.getElementById('searchPatients').value.toLowerCase().trim();
  
  if (!searchText) {
    // Если поиск пустой, показываем все данные
    renderPatientsTable();
    return;
  }
  
  const filteredData = patientsData.filter(patient => {
    const fullName = (patient.FullName || patient.fullName || '').toLowerCase();
    return fullName.includes(searchText);
  });
  
  const tbody = document.querySelector('#patientsTable tbody');
  tbody.innerHTML = '';
  
  if (filteredData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Нет совпадений</td></tr>';
    return;
  }
  
  filteredData.forEach(patient => {
    const tr = document.createElement('tr');
    tr.dataset.id = patient.PatientID || patient.patientId;
    
    tr.innerHTML = `
      <td>${patient.FullName || patient.fullName}</td>
      <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
      <td>${patient.Gender || patient.gender}</td>
      <td>${formatDate(patient.RecordDate || patient.recordDate)}</td>
      <td>${formatDate(patient.DischargeDate || patient.dischargeDate)}</td>
    `;
    
    tr.addEventListener('click', handlePatientRowClick);
    tr.addEventListener('dblclick', openPatientMedCard);
    
    tbody.appendChild(tr);
  });
}

// Фильтрация врачей по поиску
function filterDoctors() {
  const searchText = document.getElementById('searchDoctors').value.toLowerCase().trim();
  
  if (!searchText) {
    renderDoctorsTable();
    return;
  }
  
  const filteredData = doctorsData.filter(doctor => {
    const fullName = (doctor.FullName || doctor.fullName || '').toLowerCase();
    const specialty = (doctor.Specialty || doctor.specialty || '').toLowerCase();
    return fullName.includes(searchText) || specialty.includes(searchText);
  });
  
  const tbody = document.querySelector('#doctorsTable tbody');
  tbody.innerHTML = '';
  
  if (filteredData.length === 0) {
    tbody.innerHTML = '<tr><td colspan="5" class="text-center">Нет совпадений</td></tr>';
    return;
  }
  
  filteredData.forEach(doctor => {
    const tr = document.createElement('tr');
    tr.dataset.id = doctor.DoctorID || doctor.doctorId;
    
    tr.innerHTML = `
      <td>${doctor.FullName || doctor.fullName}</td>
      <td>${doctor.Specialty || doctor.specialty}</td>
      <td>${doctor.GeneralName || doctor.generalName || ''}</td>
      <td>${doctor.OfficeNumber || doctor.officeNumber}</td>
      <td>${doctor.WorkExperience || doctor.workExperience}</td>
    `;
    
    tr.addEventListener('click', handleDoctorRowClick);
    tr.addEventListener('dblclick', openDoctorProcedures);
    
    tbody.appendChild(tr);
  });
}

// Показать модальное окно врача (добавление/редактирование)
function showDoctorModal(mode, doctorId) {
  const modal = document.getElementById('doctorModal');
  const form = document.getElementById('doctorForm');
  const title = document.getElementById('doctorModalTitle');
  
  // Сбрасываем форму
  form.reset();
  
  if (mode === 'add') {
    title.textContent = 'Добавление врача';
    document.getElementById('doctorId').value = '';
  } else if (mode === 'edit' && doctorId) {
    title.textContent = 'Редактирование врача';
    document.getElementById('doctorId').value = doctorId;
    
    // Загружаем данные врача
    const doctor = doctorsData.find(d => (d.DoctorID || d.doctorId) == doctorId);
    if (doctor) {
      document.getElementById('doctorFullName').value = doctor.FullName || doctor.fullName || '';
      document.getElementById('doctorSpecialty').value = doctor.Specialty || doctor.specialty || '';
      document.getElementById('doctorGeneralName').value = doctor.GeneralName || doctor.generalName || '';
      document.getElementById('doctorOfficeNumber').value = doctor.OfficeNumber || doctor.officeNumber || '';
      document.getElementById('doctorWorkExperience').value = doctor.WorkExperience || doctor.workExperience || '';
    }
  }
  
  modal.style.display = 'block';
}

// Показать модальное окно назначения врача
function showAssignDoctorModal() {
  const selectedRow = document.querySelector('#patientsTable tbody tr.selected');
  if (!selectedRow) {
    showNotification('Выберите пациента для назначения врача', 'error');
    return;
  }
  
  const patientId = selectedRow.dataset.id;
  const patientName = selectedRow.querySelector('td:first-child').textContent;
  
  document.getElementById('assignPatientName').textContent = `Пациент: ${patientName}`;
  document.getElementById('doctorsAssignmentList').dataset.patientId = patientId;
  
  // Загрузка текущих назначений для пациента
  fetch(`/api/chief/patient/${patientId}/doctors`)
    .then(response => {
      if (!response.ok) {
        throw new Error('Ошибка загрузки назначенных врачей');
      }
      return response.json();
    })
    .then(data => {
      // Отмечаем чекбоксы для уже назначенных врачей
      const checkboxes = document.querySelectorAll('#doctorsAssignmentList .doctor-checkbox');
      checkboxes.forEach(checkbox => {
        checkbox.checked = false;
      });
      
      if (data && data.length > 0) {
        data.forEach(assignedDoctor => {
          const doctorId = assignedDoctor.doctorId || assignedDoctor.DoctorID;
          const checkbox = document.getElementById(`doctor-${doctorId}`);
          if (checkbox) {
            checkbox.checked = true;
          }
        });
      }
      
      const modal = document.getElementById('assignDoctorModal');
      modal.style.display = 'block';
    })
    .catch(error => {
      console.error('Ошибка:', error);
      
      // В случае ошибки все равно показываем модальное окно, но без предварительно отмеченных врачей
      const modal = document.getElementById('assignDoctorModal');
      modal.style.display = 'block';
    });
}

// Показать модальное окно выписного эпикриза
function showDischargeModal() {
  const selectedRow = document.querySelector('#patientsTable tbody tr.selected');
  if (!selectedRow) {
    showNotification('Выберите пациента для создания выписного эпикриза', 'error');
    return;
  }
  
  const patientId = selectedRow.dataset.id;
  const patientName = selectedRow.querySelector('td:first-child').textContent;
  
  document.getElementById('dischargePatientName').textContent = `Пациент: ${patientName}`;
  document.getElementById('dischargeForm').dataset.patientId = patientId;
  document.getElementById('dischargeDate').valueAsDate = new Date();
  
  const modal = document.getElementById('dischargeModal');
  modal.style.display = 'block';
}

// Показать модальное окно отчетов
function showReportsModal() {
  const modal = document.getElementById('reportsModal');
  
  // Устанавливаем диапазон дат: последний месяц
  const today = new Date();
  const lastMonth = new Date(today);
  lastMonth.setMonth(today.getMonth() - 1);
  
  document.getElementById('reportStartDate').valueAsDate = lastMonth;
  document.getElementById('reportEndDate').valueAsDate = today;
  
  modal.style.display = 'block';
}

// Сохранение врача (добавление или обновление)
function saveDoctor(e) {
  e.preventDefault();
  
  const doctorId = document.getElementById('doctorId').value.trim();
  const fullName = document.getElementById('doctorFullName').value.trim();
  const specialty = document.getElementById('doctorSpecialty').value.trim();
  const generalName = document.getElementById('doctorGeneralName').value.trim();
  const officeNumber = document.getElementById('doctorOfficeNumber').value.trim();
  const workExperience = document.getElementById('doctorWorkExperience').value.trim();
  
  if (!fullName || !specialty || !officeNumber || !workExperience) {
    showNotification('Пожалуйста, заполните все обязательные поля', 'error');
    return;
  }
  
  const doctorData = {
    fullName,
    specialty,
    generalName,
    officeNumber: parseInt(officeNumber),
    workExperience: parseInt(workExperience)
  };
  
  let url, method;
  
  if (doctorId) {
    // Обновление существующего врача
    url = `/api/chief/doctor/${doctorId}`;
    method = 'PUT';
    doctorData.doctorId = parseInt(doctorId);
  } else {
    // Добавление нового врача
    url = '/api/chief/doctor';
    method = 'POST';
  }
  
  fetch(url, {
    method,
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(doctorData)
  })
  .then(response => {
    if (!response.ok) {
      throw new Error('Ошибка при сохранении данных врача');
    }
    return response.json();
  })
  .then(data => {
    if (data.success) {
      const modal = document.getElementById('doctorModal');
      modal.style.display = 'none';
      
      showNotification(doctorId ? 'Врач успешно обновлен' : 'Врач успешно добавлен', 'success');
      loadDoctors(); // Перезагружаем список врачей
    } else {
      showNotification(data.message || 'Произошла ошибка', 'error');
    }
  })
  .catch(error => {
    console.error('Ошибка:', error);
    showNotification('Не удалось сохранить данные врача', 'error');
  });
}

// Удаление врача
function deleteDoctor() {
  const selectedRow = document.querySelector('#doctorsTable tbody tr.selected');
  if (!selectedRow) {
    showNotification('Выберите врача для удаления', 'error');
    return;
  }
  
  const doctorId = selectedRow.dataset.id;
  const doctorName = selectedRow.querySelector('td:first-child').textContent;
  
  if (!confirm(`Вы действительно хотите удалить врача "${doctorName}"?`)) {
    return;
  }
  
  fetch(`/api/chief/doctor/${doctorId}`, {
    method: 'DELETE'
  })
  .then(response => {
    if (!response.ok) {
      throw new Error('Ошибка при удалении врача');
    }
    return response.json();
  })
  .then(data => {
    if (data.success) {
      showNotification('Врач успешно удален', 'success');
      loadDoctors(); // Перезагружаем список врачей
    } else {
      showNotification(data.message || 'Произошла ошибка', 'error');
    }
  })
  .catch(error => {
    console.error('Ошибка:', error);
    showNotification('Не удалось удалить врача', 'error');
  });
}

// Назначение врача пациенту
function assignDoctor() {
  const doctorsList = document.getElementById('doctorsAssignmentList');
  const patientId = doctorsList.dataset.patientId;
  
  // Собираем ID отмеченных врачей
  const selectedDoctors = [];
  const checkboxes = document.querySelectorAll('#doctorsAssignmentList .doctor-checkbox:checked');
  
  checkboxes.forEach(checkbox => {
    const doctorId = checkbox.id.replace('doctor-', '');
    selectedDoctors.push(parseInt(doctorId));
  });
  
  const assignmentData = {
    patientId: parseInt(patientId),
    doctorIds: selectedDoctors
  };
  
  console.log('Sending assignment data:', assignmentData);
  
  fetch('/api/chief/assignments', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(assignmentData)
  })
  .then(response => {
    if (!response.ok) {
      // Получаем детали ошибки
      return response.text().then(text => {
        try {
          // Пробуем распарсить как JSON
          const data = JSON.parse(text);
          throw new Error(data.message || `Ошибка сервера: ${response.status}`);
        } catch (e) {
          // Если не JSON, используем текст ошибки или статус
          throw new Error(`Ошибка сервера: ${text || response.status}`);
        }
      });
    }
    return response.json();
  })
  .then(data => {
    if (data.success) {
      const modal = document.getElementById('assignDoctorModal');
      modal.style.display = 'none';
      
      showNotification('Врачи успешно назначены пациенту', 'success');
    } else {
      showNotification(data.message || 'Произошла ошибка', 'error');
    }
  })
  .catch(error => {
    console.error('Ошибка при назначении врачей:', error);
    showNotification(`Не удалось назначить врачей пациенту: ${error.message}`, 'error');
  });
}

// Создание выписного эпикриза
function createDischargeDocument(e) {
  e.preventDefault();
  
  const form = document.getElementById('dischargeForm');
  const patientId = form.dataset.patientId;
  const diagnosis = document.getElementById('dischargeDiagnosis').value.trim();
  const recommendations = document.getElementById('dischargeRecommendations').value.trim();
  const results = document.getElementById('dischargeResults').value.trim();
  const dischargeDate = document.getElementById('dischargeDate').value;
  
  if (!diagnosis || !recommendations || !results || !dischargeDate) {
    showNotification('Пожалуйста, заполните все поля', 'error');
    return;
  }
  
  const documentData = {
    patientId: parseInt(patientId),
    diagnosis,
    recommendations,
    results,
    dischargeDate
  };
  
  fetch('/api/chief/discharge', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(documentData)
  })
  .then(response => {
    if (!response.ok) {
      throw new Error('Ошибка при создании выписного эпикриза');
    }
    return response.json();
  })
  .then(data => {
    if (data.success) {
      const modal = document.getElementById('dischargeModal');
      modal.style.display = 'none';
      
      showNotification('Выписной эпикриз успешно создан', 'success');
      loadPatients(); // Обновляем список пациентов, чтобы отобразить дату выписки
    } else {
      showNotification(data.message || 'Произошла ошибка', 'error');
    }
  })
  .catch(error => {
    console.error('Ошибка:', error);
    showNotification('Не удалось создать выписной эпикриз', 'error');
  });
}

// Генерация отчета по пациентам
function generatePatientReport() {
  const startDate = document.getElementById('reportStartDate').value;
  const endDate = document.getElementById('reportEndDate').value;
  
  if (!startDate || !endDate) {
    showNotification('Укажите период для отчета', 'error');
    return;
  }
  
  fetch(`/api/chief/reports/patients?startDate=${startDate}&endDate=${endDate}`)
  .then(response => {
    if (!response.ok) {
      throw new Error('Ошибка при формировании отчета');
    }
    return response.json();
  })
  .then(data => {
    if (data.success) {
      const reportResult = document.querySelector('.report-result');
      reportResult.innerHTML = '';
      
      if (data.report) {
        // Создаем HTML для отчета
        let reportHTML = `
          <h3>Отчет по пациентам за период ${formatDate(startDate)} - ${formatDate(endDate)}</h3>
          <div class="report-stats">
            <p><strong>Всего пациентов:</strong> ${data.report.totalPatients}</p>
            <p><strong>Новых пациентов:</strong> ${data.report.newPatients}</p>
            <p><strong>Выписано пациентов:</strong> ${data.report.dischargedPatients}</p>
          </div>
        `;
        
        reportResult.innerHTML = reportHTML;
      } else {
        reportResult.innerHTML = '<p>Нет данных для отображения</p>';
      }
    } else {
      showNotification(data.message || 'Произошла ошибка', 'error');
    }
  })
  .catch(error => {
    console.error('Ошибка:', error);
    showNotification('Не удалось сформировать отчет', 'error');
  });
}

// Вспомогательные функции
function formatDate(dateStr) {
  if (!dateStr) return '—';
  
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '—';
  
  const day = String(date.getDate()).padStart(2, '0');
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const year = date.getFullYear();
  
  return `${day}.${month}.${year}`;
}

function capitalizeFirstLetter(string) {
  return string.charAt(0).toUpperCase() + string.slice(1);
}

// Уведомления
function showNotification(message, type = 'info') {
  // Проверяем, есть ли уже контейнер для уведомлений
  let notificationContainer = document.querySelector('.notification-container');
  
  if (!notificationContainer) {
    notificationContainer = document.createElement('div');
    notificationContainer.className = 'notification-container';
    document.body.appendChild(notificationContainer);
    
    // Добавляем стили для контейнера уведомлений
    const style = document.createElement('style');
    style.textContent = `
      .notification-container {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
      }
      .notification {
        padding: 15px;
        margin-bottom: 10px;
        border-radius: 4px;
        color: white;
        opacity: 0;
        transform: translateX(50px);
        transition: all 0.3s ease;
        max-width: 300px;
      }
      .notification.show {
        opacity: 1;
        transform: translateX(0);
      }
      .notification.success { background-color: #4caf50; }
      .notification.error { background-color: #f44336; }
      .notification.info { background-color: #2196f3; }
      .notification.warning { background-color: #ff9800; }
    `;
    document.head.appendChild(style);
  }
  
  // Создаем уведомление
  const notification = document.createElement('div');
  notification.className = `notification ${type}`;
  notification.textContent = message;
  
  notificationContainer.appendChild(notification);
  
  // Анимация появления
  setTimeout(() => {
    notification.classList.add('show');
  }, 10);
  
  // Автоматически скрываем через 3 секунды
  setTimeout(() => {
    notification.classList.remove('show');
    setTimeout(() => {
      notification.remove();
    }, 300);
  }, 3000);
}

// При заполнении таблицы
function populatePatientRow(patient, row) {
  row.querySelector('.patient-id').textContent = patient.patientID;
  row.querySelector('.patient-name').textContent = patient.fullName;
  row.querySelector('.patient-gender').textContent = patient.gender;
  row.querySelector('.patient-dateofbirth').textContent = formatDate(patient.dateOfBirth);
  row.querySelector('.patient-recorddate').textContent = formatDate(patient.recordDate);
  row.querySelector('.patient-dischargedate').textContent = formatDate(patient.dischargeDate);
}
