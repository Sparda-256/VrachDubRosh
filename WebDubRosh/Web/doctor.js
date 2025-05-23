document.addEventListener('DOMContentLoaded', function() {
  // Переменные для хранения данных
  let doctorID = null;
  let patients = [];
  let appointments = [];
  let procedures = [];
  let selectedAppointmentIDs = [];
  let selectedProcedureIDs = [];
  let hideCompleted = true;

  // Элементы DOM
  const themeToggle = document.getElementById('themeToggle');
  const patientsTable = document.getElementById('patientsTable');
  const appointmentsTable = document.getElementById('appointmentsTable');
  const proceduresTable = document.getElementById('proceduresTable');
  const searchPatients = document.getElementById('searchPatients');
  const searchAppointments = document.getElementById('searchAppointments');
  const searchProcedures = document.getElementById('searchProcedures');
  const hideCompletedCheckbox = document.getElementById('hideCompleted');
  const patientSelect = document.getElementById('patientSelect');
  const procedureSelect = document.getElementById('procedureSelect');
  const appointmentDate = document.getElementById('appointmentDate');
  const appointmentTime = document.getElementById('appointmentTime');

  // Кнопки
  const exitBtn = document.getElementById('exitBtn');
  const assignProcedureBtn = document.getElementById('assignProcedureBtn');
  const cancelAppointmentBtn = document.getElementById('cancelAppointmentBtn');
  const updateStatusBtn = document.getElementById('updateStatusBtn');
  const weeklyScheduleBtn = document.getElementById('weeklyScheduleBtn');
  const addProcedureBtn = document.getElementById('addProcedureBtn');
  const deleteProcedureBtn = document.getElementById('deleteProcedureBtn');
  const saveProcedureBtn = document.getElementById('saveProcedureBtn');
  const saveDescriptionBtn = document.getElementById('saveDescriptionBtn');
  const saveProcedureDescriptionBtn = document.getElementById('saveProcedureDescriptionBtn');

  // Модальные окна
  const addProcedureModal = document.getElementById('addProcedureModal');
  const addDescriptionModal = document.getElementById('addDescriptionModal');
  const procedureDescriptionModal = document.getElementById('procedureDescriptionModal');
  const procedureName = document.getElementById('procedureName');
  const procedureDuration = document.getElementById('procedureDuration');
  const patientDescription = document.getElementById('patientDescription');
  const procedureDescription = document.getElementById('procedureDescription');
  const patientNameDisplay = document.getElementById('patientNameDisplay');
  const appointmentDetailsDisplay = document.getElementById('appointmentDetailsDisplay');

  // Инициализация
  init();

  // Функция инициализации
  function init() {
    // Получаем ID врача из URL
    const urlParams = new URLSearchParams(window.location.search);
    doctorID = urlParams.get('id');
    
    if (!doctorID) {
      showNotification('Ошибка: ID врача не найден в URL.', 'error');
      window.location.href = 'login.html';
      return;
    }

    // Инициализация переключателя темы
    initThemeToggle();

    // Инициализация вкладок
    initTabs();

    // Инициализация модальных окон
    initModals();

    // Загрузка данных
    loadData();

    // Инициализация обработчиков событий
    initEventListeners();

    // Устанавливаем сегодняшнюю дату в поле даты
    appointmentDate.valueAsDate = new Date();
  }

  // Функция для отображения уведомлений
  function showNotification(message, type = 'info') {
    // Создаем элемент уведомления
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.textContent = message;
    
    // Добавляем уведомление на страницу
    document.body.appendChild(notification);
    
    // Устанавливаем таймер для автоматического скрытия
    setTimeout(() => {
      notification.style.animation = 'fadeOut 0.5s';
      notification.addEventListener('animationend', () => {
        document.body.removeChild(notification);
      });
    }, 3000);
  }

  // Инициализация переключателя темы
  function initThemeToggle() {
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
    const tabButtons = document.querySelectorAll('.tab-button');
    const tabPanes = document.querySelectorAll('.tab-pane');

    tabButtons.forEach(button => {
      button.addEventListener('click', () => {
        // Удаляем активный класс со всех кнопок и вкладок
        tabButtons.forEach(btn => btn.classList.remove('active'));
        tabPanes.forEach(pane => pane.classList.remove('active'));

        // Добавляем активный класс на выбранную кнопку
        button.classList.add('active');

        // Показываем соответствующую вкладку
        const tabId = button.dataset.tab;
        document.getElementById(tabId).classList.add('active');
      });
    });
  }

  // Инициализация модальных окон
  function initModals() {
    const closeButtons = document.querySelectorAll('.close-modal');

    closeButtons.forEach(button => {
      button.addEventListener('click', () => {
        const modal = button.closest('.modal');
        modal.style.display = 'none';
      });
    });

    // Закрытие модальных окон при клике вне содержимого
    window.addEventListener('click', (event) => {
      if (event.target.classList.contains('modal')) {
        event.target.style.display = 'none';
      }
    });
  }

  // Инициализация обработчиков событий
  function initEventListeners() {
    // Обработчики поиска
    searchPatients.addEventListener('input', filterPatients);
    searchAppointments.addEventListener('input', filterAppointments);
    searchProcedures.addEventListener('input', filterProcedures);

    // Обработчик чекбокса "Скрыть завершённые и отменённые"
    hideCompletedCheckbox.addEventListener('change', function() {
      hideCompleted = this.checked;
      renderAppointments();
    });

    // Обработчики кнопок
    exitBtn.addEventListener('click', () => {
      window.location.href = 'login.html';
    });

    assignProcedureBtn.addEventListener('click', assignProcedure);
    cancelAppointmentBtn.addEventListener('click', cancelAppointment);
    updateStatusBtn.addEventListener('click', updateAppointmentsStatus);
    addProcedureBtn.addEventListener('click', () => {
      procedureName.value = '';
      procedureDuration.value = '';
      // Очищаем атрибуты режима и ID процедуры при добавлении новой
      delete addProcedureModal.dataset.mode;
      delete addProcedureModal.dataset.procedureId;
      document.getElementById('procedureModalTitle').textContent = 'Добавление процедуры';
      addProcedureModal.style.display = 'block';
    });
    
    deleteProcedureBtn.addEventListener('click', deleteProcedure);
    saveProcedureBtn.addEventListener('click', saveProcedure);
    saveDescriptionBtn.addEventListener('click', savePatientDescription);
    saveProcedureDescriptionBtn.addEventListener('click', saveProcedureDescriptionHandler);
    
    // Двойной щелчок по строке пациентов
    patientsTable.addEventListener('click', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      
      // Снимаем выделение со всех строк
      patientsTable.querySelectorAll('tbody tr').forEach(r => r.classList.remove('selected'));
      
      // Выделяем текущую строку
      row.classList.add('selected');
    });
    
    patientsTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      
      const patientID = parseInt(row.dataset.id);
      const patient = patients.find(p => p.PatientID === patientID);
      
      if (patient) {
        // Перенаправляем на страницу медкарты с передачей ID пациента и источника
        window.location.href = `medcard.html?id=${patientID}&source=doctor&source_id=${doctorID}`;
      }
    });
    
    // Выделение строк в таблице назначений
    appointmentsTable.addEventListener('click', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      
      const appointmentID = parseInt(row.dataset.id);
      
      // Если нажата клавиша Ctrl или Shift, добавляем/удаляем из выбранных
      if (e.ctrlKey || e.shiftKey) {
        row.classList.toggle('selected');
        if (row.classList.contains('selected')) {
          if (!selectedAppointmentIDs.includes(appointmentID)) {
            selectedAppointmentIDs.push(appointmentID);
          }
        } else {
          selectedAppointmentIDs = selectedAppointmentIDs.filter(id => id !== appointmentID);
        }
      } else {
        // Иначе выбираем только текущую строку
        appointmentsTable.querySelectorAll('tbody tr').forEach(r => r.classList.remove('selected'));
        row.classList.add('selected');
        selectedAppointmentIDs = [appointmentID];
      }
    });
    
    // Двойной щелчок по строке назначений
    appointmentsTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      
      const appointmentID = parseInt(row.dataset.id);
      const appointment = appointments.find(a => a.AppointmentID === appointmentID);
      
      if (appointment) {
        appointmentDetailsDisplay.textContent = `Пациент: ${appointment.PatientName}, Процедура: ${appointment.ProcedureName}, Дата: ${formatDateTime(new Date(appointment.AppointmentDateTime))}`;
        // Проверяем, что Description является строкой и не пустой
        procedureDescription.value = typeof appointment.Description === 'string' ? appointment.Description : '';
        procedureDescriptionModal.style.display = 'block';
        
        // Сохраняем ID назначения для использования в saveProcedureDescription
        procedureDescriptionModal.dataset.appointmentId = appointmentID;
      }
    });
    
    // Выделение строк в таблице процедур
    proceduresTable.addEventListener('click', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      
      const procedureID = parseInt(row.dataset.id);
      
      // Если нажата клавиша Ctrl или Shift, добавляем/удаляем из выбранных
      if (e.ctrlKey || e.shiftKey) {
        row.classList.toggle('selected');
        if (row.classList.contains('selected')) {
          if (!selectedProcedureIDs.includes(procedureID)) {
            selectedProcedureIDs.push(procedureID);
          }
        } else {
          selectedProcedureIDs = selectedProcedureIDs.filter(id => id !== procedureID);
        }
      } else {
        // Иначе выбираем только текущую строку
        proceduresTable.querySelectorAll('tbody tr').forEach(r => r.classList.remove('selected'));
        row.classList.add('selected');
        selectedProcedureIDs = [procedureID];
      }
    });
    
    // Двойной щелчок по строке процедур
    proceduresTable.addEventListener('dblclick', function(e) {
      const row = e.target.closest('tr');
      if (!row || row.parentElement.tagName === 'THEAD') return;
      
      const procedureID = parseInt(row.dataset.id);
      const procedure = procedures.find(p => p.ProcedureID === procedureID);
      
      if (procedure) {
        procedureName.value = procedure.ProcedureName;
        procedureDuration.value = procedure.Duration;
        addProcedureModal.dataset.procedureId = procedureID;
        addProcedureModal.dataset.mode = 'edit';
        document.getElementById('procedureModalTitle').textContent = 'Редактирование процедуры';
        addProcedureModal.style.display = 'block';
      }
    });
  }

  // Загрузка данных
  function loadData() {
    Promise.all([
      fetch(`/api/doctor/${doctorID}/patients`),
      fetch(`/api/doctor/${doctorID}/appointments`),
      fetch(`/api/doctor/${doctorID}/procedures`),
      fetch(`/api/doctor/${doctorID}/patientsforappointment`),
      fetch(`/api/doctor/${doctorID}/proceduresforappointment`)
    ])
    .then(responses => Promise.all(responses.map(r => r.json())))
    .then(([patientsData, appointmentsData, proceduresData, patientsForAppointment, proceduresForAppointment]) => {
      patients = patientsData;
      appointments = appointmentsData;
      procedures = proceduresData;
      
      renderPatients();
      renderAppointments();
      renderProcedures();
      
      // Заполняем выпадающие списки
      fillSelect(patientSelect, patientsForAppointment, 'PatientID', 'FullName');
      fillSelect(procedureSelect, proceduresForAppointment, 'ProcedureID', 'DisplayText');
    })
    .catch(error => {
      console.error('Error loading data:', error);
      showNotification('Ошибка загрузки данных. Пожалуйста, обновите страницу или повторите попытку позже.', 'error');
    });
  }

  // Заполнение выпадающего списка
  function fillSelect(selectElement, items, valueField, textField) {
    selectElement.innerHTML = '';
    
    // Добавляем пустой элемент
    const defaultOption = document.createElement('option');
    defaultOption.value = '';
    defaultOption.textContent = '-- Выберите --';
    selectElement.appendChild(defaultOption);
    
    // Добавляем элементы из списка
    items.forEach(item => {
      const option = document.createElement('option');
      option.value = item[valueField];
      option.textContent = item[textField];
      selectElement.appendChild(option);
    });
  }

  // Отображение пациентов
  function renderPatients() {
    const tbody = patientsTable.querySelector('tbody');
    tbody.innerHTML = '';
    
    patients.forEach(patient => {
      const row = document.createElement('tr');
      row.dataset.id = patient.PatientID;
      
      row.innerHTML = `
        <td>${patient.FullName}</td>
        <td>${formatDate(new Date(patient.DateOfBirth))}</td>
        <td>${patient.Gender}</td>
        <td>${formatDate(new Date(patient.RecordDate))}</td>
        <td>${patient.DischargeDate ? formatDate(new Date(patient.DischargeDate)) : '-'}</td>
      `;
      
      tbody.appendChild(row);
    });
  }

  // Отображение назначений
  function renderAppointments() {
    const tbody = appointmentsTable.querySelector('tbody');
    tbody.innerHTML = '';
    
    let filteredAppointments = appointments;
    
    // Применяем фильтр "Скрыть завершённые и отменённые"
    if (hideCompleted) {
      filteredAppointments = appointments.filter(a => !['Завершена', 'Отменена'].includes(a.Status));
    }
    
    filteredAppointments.forEach(appointment => {
      const row = document.createElement('tr');
      row.dataset.id = appointment.AppointmentID;
      
      row.innerHTML = `
        <td>${appointment.PatientName}</td>
        <td>${appointment.ProcedureName}</td>
        <td>${formatDateTime(new Date(appointment.AppointmentDateTime))}</td>
        <td>${getStatusBadge(appointment.Status)}</td>
      `;
      
      tbody.appendChild(row);
    });
  }

  // Отображение процедур
  function renderProcedures() {
    const tbody = proceduresTable.querySelector('tbody');
    tbody.innerHTML = '';
    
    procedures.forEach(procedure => {
      const row = document.createElement('tr');
      row.dataset.id = procedure.ProcedureID;
      
      row.innerHTML = `
        <td>${procedure.ProcedureName}</td>
        <td>${procedure.Duration}</td>
      `;
      
      tbody.appendChild(row);
    });
  }

  // Фильтрация пациентов
  function filterPatients() {
    const searchValue = searchPatients.value.toLowerCase();
    const tbody = patientsTable.querySelector('tbody');
    const rows = tbody.querySelectorAll('tr');
    
    rows.forEach(row => {
      const text = row.textContent.toLowerCase();
      if (text.includes(searchValue)) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  // Фильтрация назначений
  function filterAppointments() {
    const searchValue = searchAppointments.value.toLowerCase();
    const tbody = appointmentsTable.querySelector('tbody');
    const rows = tbody.querySelectorAll('tr');
    
    rows.forEach(row => {
      const text = row.textContent.toLowerCase();
      if (text.includes(searchValue)) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  // Фильтрация процедур
  function filterProcedures() {
    const searchValue = searchProcedures.value.toLowerCase();
    const tbody = proceduresTable.querySelector('tbody');
    const rows = tbody.querySelectorAll('tr');
    
    rows.forEach(row => {
      const text = row.textContent.toLowerCase();
      if (text.includes(searchValue)) {
        row.style.display = '';
      } else {
        row.style.display = 'none';
      }
    });
  }

  // Назначение процедуры
  function assignProcedure() {
    const patientID = patientSelect.value;
    const procedureID = procedureSelect.value;
    const date = appointmentDate.value;
    const time = appointmentTime.value;
    
    // Проверка заполнения всех полей
    if (!patientID || !procedureID || !date || !time) {
      showNotification('Пожалуйста, заполните все поля для назначения процедуры.', 'error');
      return;
    }
    
    const appointmentDateTime = new Date(`${date}T${time}`);
    
    // Проверка, что назначение не на прошедшее время
    const now = new Date();
    if (appointmentDateTime < now) {
      showNotification('Нельзя назначить процедуру на прошедшее время.', 'error');
      return;
    }
    
    // Проверка формата времени
    if (!/^\d{2}:\d{2}$/.test(time)) {
      showNotification('Неверный формат времени. Используйте ЧЧ:ММ.', 'error');
      return;
    }
    
    // Все валидации пройдены, отправляем запрос на сервер
    // Серверная часть выполнит дополнительные проверки:
    // - проверка на выписку пациента
    // - проверка занятости врача
    // - проверка занятости пациента
    fetch('/api/doctor/assignprocedure', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        PatientID: parseInt(patientID),
        DoctorID: parseInt(doctorID),
        ProcedureID: parseInt(procedureID),
        AppointmentDateTime: appointmentDateTime
      })
    })
    .then(response => response.json())
    .then(data => {
      if (data.success) {
        showNotification(data.message, 'success');
        
        // Очищаем поля формы после успешного добавления
        patientSelect.selectedIndex = 0;
        procedureSelect.selectedIndex = 0;
        appointmentTime.value = '';
        
        // Обновляем список назначений
        fetch(`/api/doctor/${doctorID}/appointments`)
          .then(response => response.json())
          .then(appointmentsData => {
            appointments = appointmentsData;
            renderAppointments();
          });
      } else {
        showNotification(data.message, 'error');
      }
    })
    .catch(error => {
      console.error('Error assigning procedure:', error);
      showNotification('Ошибка при назначении процедуры.', 'error');
    });
  }

  // Отмена назначения
  function cancelAppointment() {
    if (selectedAppointmentIDs.length === 0) {
      showNotification('Пожалуйста, выберите назначение для отмены.', 'info');
      return;
    }
    
    if (!confirm(`Вы уверены, что хотите отменить ${selectedAppointmentIDs.length} назначение(й)?`)) {
      return;
    }
    
    // Отменяем назначения по очереди
    const promises = selectedAppointmentIDs.map(appointmentID => {
      return fetch('/api/doctor/cancelappointment', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          AppointmentID: appointmentID
        })
      })
      .then(response => response.json());
    });
    
    Promise.all(promises)
      .then(results => {
        const successCount = results.filter(r => r.success).length;
        
        if (successCount > 0) {
          showNotification(`Успешно отменено ${successCount} назначение(й).`, 'success');
          
          // Обновляем список назначений
          fetch(`/api/doctor/${doctorID}/appointments`)
            .then(response => response.json())
            .then(appointmentsData => {
              appointments = appointmentsData;
              renderAppointments();
              selectedAppointmentIDs = [];
            });
        } else {
          showNotification('Не удалось отменить назначения.', 'error');
        }
      })
      .catch(error => {
        console.error('Error canceling appointments:', error);
        showNotification('Ошибка при отмене назначений.', 'error');
      });
  }

  // Обновление статусов назначений
  function updateAppointmentsStatus() {
    fetch('/api/doctor/updateappointmentsstatus', {
      method: 'POST'
    })
    .then(response => response.json())
    .then(data => {
      if (data.success) {
        // Обновляем список назначений
        fetch(`/api/doctor/${doctorID}/appointments`)
          .then(response => response.json())
          .then(appointmentsData => {
            appointments = appointmentsData;
            renderAppointments();
          });
      } else {
        showNotification(data.message, 'error');
      }
    })
    .catch(error => {
      console.error('Error updating appointments status:', error);
      showNotification('Ошибка при обновлении статусов назначений.', 'error');
    });
  }

  // Сохранение процедуры
  function saveProcedure() {
    const name = procedureName.value.trim();
    const duration = parseInt(procedureDuration.value);
    
    if (!name || isNaN(duration) || duration <= 0) {
      showNotification('Пожалуйста, введите корректное название и длительность процедуры.', 'error');
      return;
    }
    
    // Проверяем, режим редактирования или добавления
    const isEdit = addProcedureModal.dataset.mode === 'edit';
    const procedureID = isEdit ? parseInt(addProcedureModal.dataset.procedureId) : null;
    
    if (isEdit) {
      // Реализуем редактирование процедуры
      // Так как специального API нет, используем удаление старой и создание новой процедуры
      fetch(`/api/doctor/procedure/${procedureID}`, {
        method: 'DELETE'
      })
      .then(response => response.json())
      .then(deleteResult => {
        if (deleteResult.success) {
          // После успешного удаления создаем новую процедуру
          return fetch('/api/doctor/addprocedure', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json'
            },
            body: JSON.stringify({
              DoctorID: parseInt(doctorID),
              ProcedureName: name,
              Duration: duration
            })
          }).then(response => response.json());
        } else {
          throw new Error(deleteResult.message || 'Ошибка при удалении старой процедуры');
        }
      })
      .then(addResult => {
        if (addResult.success) {
          showNotification('Процедура успешно обновлена.', 'success');
          addProcedureModal.style.display = 'none';
          
          // Обновляем список процедур
          fetch(`/api/doctor/${doctorID}/procedures`)
            .then(response => response.json())
            .then(proceduresData => {
              procedures = proceduresData;
              renderProcedures();
            });
            
          // Обновляем список процедур для назначения
          fetch(`/api/doctor/${doctorID}/proceduresforappointment`)
            .then(response => response.json())
            .then(proceduresForAppointment => {
              fillSelect(procedureSelect, proceduresForAppointment, 'ProcedureID', 'DisplayText');
            });
        } else {
          showNotification(addResult.message || 'Ошибка при создании новой процедуры', 'error');
        }
      })
      .catch(error => {
        console.error('Error updating procedure:', error);
        showNotification('Ошибка при обновлении процедуры: ' + error.message, 'error');
      });
    } else {
      // Добавление новой процедуры
      fetch('/api/doctor/addprocedure', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          DoctorID: parseInt(doctorID),
          ProcedureName: name,
          Duration: duration
        })
      })
      .then(response => response.json())
      .then(data => {
        if (data.success) {
          showNotification(data.message, 'success');
          addProcedureModal.style.display = 'none';
          
          // Обновляем список процедур
          fetch(`/api/doctor/${doctorID}/procedures`)
            .then(response => response.json())
            .then(proceduresData => {
              procedures = proceduresData;
              renderProcedures();
            });
            
          // Обновляем список процедур для назначения
          fetch(`/api/doctor/${doctorID}/proceduresforappointment`)
            .then(response => response.json())
            .then(proceduresForAppointment => {
              fillSelect(procedureSelect, proceduresForAppointment, 'ProcedureID', 'DisplayText');
            });
        } else {
          showNotification(data.message, 'error');
        }
      })
      .catch(error => {
        console.error('Error saving procedure:', error);
        showNotification('Ошибка при сохранении процедуры.', 'error');
      });
    }
  }

  // Удаление процедуры
  function deleteProcedure() {
    if (selectedProcedureIDs.length === 0) {
      showNotification('Пожалуйста, выберите процедуру для удаления.', 'info');
      return;
    }
    
    if (!confirm(`Вы уверены, что хотите удалить ${selectedProcedureIDs.length} процедуру(ы)?`)) {
      return;
    }
    
    // Удаляем процедуры по очереди
    const promises = selectedProcedureIDs.map(procedureID => {
      return fetch(`/api/doctor/procedure/${procedureID}`, {
        method: 'DELETE'
      })
      .then(response => response.json());
    });
    
    Promise.all(promises)
      .then(results => {
        const successCount = results.filter(r => r.success).length;
        
        if (successCount > 0) {
          showNotification(`Успешно удалено ${successCount} процедуру(ы).`, 'success');
          
          // Обновляем список процедур
          fetch(`/api/doctor/${doctorID}/procedures`)
            .then(response => response.json())
            .then(proceduresData => {
              procedures = proceduresData;
              renderProcedures();
              selectedProcedureIDs = [];
            });
            
          // Обновляем список процедур для назначения
          fetch(`/api/doctor/${doctorID}/proceduresforappointment`)
            .then(response => response.json())
            .then(proceduresForAppointment => {
              fillSelect(procedureSelect, proceduresForAppointment, 'ProcedureID', 'DisplayText');
            });
        } else {
          showNotification('Не удалось удалить процедуры.', 'error');
        }
      })
      .catch(error => {
        console.error('Error deleting procedures:', error);
        showNotification('Ошибка при удалении процедур.', 'error');
      });
  }

  // Сохранение описания для пациента
  function savePatientDescription() {
    const patientID = parseInt(addDescriptionModal.dataset.patientId);
    const description = patientDescription.value.trim();
    
    if (!patientID || !description) {
      showNotification('Пожалуйста, введите описание.', 'error');
      return;
    }
    
    fetch('/api/doctor/adddescription', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        PatientID: patientID,
        DoctorID: parseInt(doctorID),
        Description: description
      })
    })
    .then(response => response.json())
    .then(data => {
      if (data.success) {
        showNotification(data.message, 'success');
        addDescriptionModal.style.display = 'none';
      } else {
        showNotification(data.message, 'error');
      }
    })
    .catch(error => {
      console.error('Error saving description:', error);
      showNotification('Ошибка при сохранении описания.', 'error');
    });
  }

  // Сохранение описания для процедуры
  function saveProcedureDescriptionHandler() {
    const appointmentID = parseInt(procedureDescriptionModal.dataset.appointmentId);
    const description = procedureDescription.value.trim();
    
    if (!appointmentID || !description) {
      showNotification('Пожалуйста, введите описание.', 'error');
      return;
    }
    
    // Отправляем запрос на сохранение описания процедуры
    fetch('/api/doctor/addproceduredescription', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        AppointmentID: appointmentID,
        Description: description
      })
    })
      .then(response => response.json())
      .then(data => {
        if (data.success) {
          showNotification(data.message, 'success');
          procedureDescriptionModal.style.display = 'none';
          
          // Обновляем список назначений
          fetch(`/api/doctor/${doctorID}/appointments`)
            .then(response => response.json())
            .then(appointmentsData => {
              appointments = appointmentsData;
              renderAppointments();
            });
        } else {
          showNotification(data.message, 'error');
        }
      })
      .catch(error => {
        console.error('Error saving procedure description:', error);
        showNotification('Ошибка при сохранении описания процедуры.', 'error');
      });
  }

  // Вспомогательные функции
  function formatDate(date) {
    if (!(date instanceof Date) || isNaN(date)) return '-';
    return date.toLocaleDateString('ru-RU');
  }
  
  function formatDateTime(date) {
    if (!(date instanceof Date) || isNaN(date)) return '-';
    return `${date.toLocaleDateString('ru-RU')} ${date.toLocaleTimeString('ru-RU', {hour: '2-digit', minute:'2-digit'})}`;
  }
  
  function getStatusBadge(status) {
    let color;
    switch (status) {
      case 'Назначена':
        color = 'blue';
        break;
      case 'Идёт':
        color = 'orange';
        break;
      case 'Завершена':
        color = 'green';
        break;
      case 'Отменена':
        color = 'red';
        break;
      default:
        color = 'gray';
    }
    
    return `<span class="status-badge" style="background-color: var(--${color}-color); color: white; padding: 3px 8px; border-radius: 4px;">${status}</span>`;
  }
}); 