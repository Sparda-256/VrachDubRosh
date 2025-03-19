document.addEventListener('DOMContentLoaded', function() {
    // Переключение вкладок
    const tabLinks = document.querySelectorAll('.tablinks');
    tabLinks.forEach(btn => {
      btn.addEventListener('click', function() {
        document.querySelectorAll('.tabcontent').forEach(tab => tab.style.display = 'none');
        tabLinks.forEach(link => link.classList.remove('active'));
        const tabId = this.getAttribute('data-tab');
        document.getElementById(tabId).style.display = 'block';
        this.classList.add('active');
      });
    });
    
    // Загрузка данных
    loadNewPatients();
    loadPatients();
    loadDoctors();
    loadDoctorSelect();
    
    // События поиска
    document.getElementById('searchNewPatients').addEventListener('input', filterNewPatients);
    document.getElementById('searchPatients').addEventListener('input', filterPatients);
    document.getElementById('searchDoctors').addEventListener('input', filterDoctors);
    
    // События кнопок
    document.getElementById('assignPatientBtn').addEventListener('click', assignPatient);
    document.getElementById('reportsBtn').addEventListener('click', () => {
      window.location.href = '/chief/reports.html';
    });
    
    // Кнопки "Выход" — переход на страницу логина
    document.getElementById('exitNewPatients').addEventListener('click', () => {
      window.location.href = '/login.html';
    });
    document.getElementById('exitPatients').addEventListener('click', () => {
      window.location.href = '/login.html';
    });
    document.getElementById('exitDoctors').addEventListener('click', () => {
      window.location.href = '/login.html';
    });
    
    // Заглушки для остальных кнопок (дополните логику при необходимости)
    document.getElementById('addPatientBtn').addEventListener('click', () => {
      alert('Добавление пациента. Реализуйте модальное окно или отдельную страницу.');
    });
    document.getElementById('editPatientBtn').addEventListener('click', () => {
      const selected = document.querySelector('#patientsTable tbody tr.selected');
      if (!selected) {
        alert('Выберите пациента для редактирования.');
        return;
      }
      alert('Редактирование пациента. Реализуйте модальное окно или отдельную страницу.');
    });
    document.getElementById('assignDoctorsBtn').addEventListener('click', () => {
      const selected = document.querySelector('#patientsTable tbody tr.selected');
      if (!selected) {
        alert('Выберите пациента для назначения врача.');
        return;
      }
      alert('Назначение врача. Реализуйте модальное окно или отдельную страницу.');
    });
    document.getElementById('addDoctorBtn').addEventListener('click', () => {
      alert('Добавление врача. Реализуйте модальное окно или отдельную страницу.');
    });
    document.getElementById('editDoctorBtn').addEventListener('click', () => {
      const selected = document.querySelector('#doctorsTable tbody tr.selected');
      if (!selected) {
        alert('Выберите врача для редактирования.');
        return;
      }
      alert('Редактирование врача. Реализуйте модальное окно или отдельную страницу.');
    });
    
    // Реализация удаления пациента
    document.getElementById('deletePatientBtn').addEventListener('click', () => {
      const selected = document.querySelector('#patientsTable tbody tr.selected');
      if (!selected) {
        alert('Выберите пациента для удаления.');
        return;
      }
      const patientID = selected.cells[0].textContent;
      if (confirm('Вы уверены, что хотите удалить этого пациента?')) {
        fetch(`http://localhost:8080/api/chief/patient/${patientID}`, {
           method: 'DELETE'
        })
        .then(response => response.json())
        .then(result => {
           if(result.success) {
              alert('Пациент удалён.');
              loadPatients();
           } else {
              alert('Ошибка при удалении пациента: ' + result.message);
           }
        })
        .catch(err => {
           console.error(err);
           alert('Ошибка при соединении с сервером.');
        });
      }
    });
    
    // Реализация удаления врача
    document.getElementById('deleteDoctorBtn').addEventListener('click', () => {
      const selected = document.querySelector('#doctorsTable tbody tr.selected');
      if (!selected) {
        alert('Выберите врача для удаления.');
        return;
      }
      const doctorID = selected.cells[0].textContent;
      if (confirm('Вы уверены, что хотите удалить этого врача?')) {
        fetch(`http://localhost:8080/api/chief/doctor/${doctorID}`, {
           method: 'DELETE'
        })
        .then(response => response.json())
        .then(result => {
           if(result.success) {
              alert('Врач удалён.');
              loadDoctors();
              loadDoctorSelect();
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
  
  let newPatientsData = [];
  let patientsData = [];
  let doctorsData = [];
  
  function loadNewPatients() {
    fetch('http://localhost:8080/api/chief/newpatients')
      .then(response => response.json())
      .then(data => {
        newPatientsData = data;
        renderNewPatientsTable();
      })
      .catch(err => console.error('Ошибка загрузки новых пациентов', err));
  }
  
  function renderNewPatientsTable() {
    const tbody = document.getElementById('newPatientsTable').querySelector('tbody');
    tbody.innerHTML = '';
    newPatientsData.forEach(patient => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${patient.NewPatientID || patient.newPatientID}</td>
        <td>${patient.FullName || patient.fullName}</td>
        <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
        <td>${patient.Gender || patient.gender}</td>
      `;
      tr.addEventListener('click', () => {
        tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
        tr.classList.add('selected');
      });
      tbody.appendChild(tr);
    });
  }
  
  function loadPatients() {
    fetch('http://localhost:8080/api/chief/patients')
      .then(response => response.json())
      .then(data => {
        patientsData = data;
        renderPatientsTable();
      })
      .catch(err => console.error('Ошибка загрузки пациентов', err));
  }
  
  function renderPatientsTable() {
    const tbody = document.getElementById('patientsTable').querySelector('tbody');
    tbody.innerHTML = '';
    patientsData.forEach(patient => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${patient.PatientID || patient.patientID}</td>
        <td>${patient.FullName || patient.fullName}</td>
        <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
        <td>${patient.Gender || patient.gender}</td>
        <td>${formatDate(patient.RecordDate || patient.recordDate)}</td>
        <td>${formatDate(patient.DischargeDate || patient.dischargeDate)}</td>
      `;
      tr.addEventListener('dblclick', () => {
        alert(`Открыть процедуры для пациента ID ${patient.PatientID || patient.patientID}`);
      });
      tr.addEventListener('click', () => {
        tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
        tr.classList.add('selected');
      });
      tbody.appendChild(tr);
    });
  }
  
  function loadDoctors() {
    fetch('http://localhost:8080/api/chief/doctors')
      .then(response => response.json())
      .then(data => {
        doctorsData = data;
        renderDoctorsTable();
      })
      .catch(err => console.error('Ошибка загрузки врачей', err));
  }
  
  function renderDoctorsTable() {
    const tbody = document.getElementById('doctorsTable').querySelector('tbody');
    tbody.innerHTML = '';
    doctorsData.forEach(doctor => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${doctor.DoctorID || doctor.doctorID}</td>
        <td>${doctor.FullName || doctor.fullName}</td>
        <td>${doctor.Specialty || doctor.specialty}</td>
        <td>${doctor.OfficeNumber || doctor.officeNumber}</td>
        <td>${doctor.WorkExperience || doctor.workExperience}</td>
      `;
      tr.addEventListener('dblclick', () => {
        alert(`Открыть процедуры для врача ID ${doctor.DoctorID || doctor.doctorID}`);
      });
      tr.addEventListener('click', () => {
        tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
        tr.classList.add('selected');
      });
      tbody.appendChild(tr);
    });
  }
  
  function loadDoctorSelect() {
    fetch('http://localhost:8080/api/chief/doctors')
      .then(response => response.json())
      .then(data => {
        const select = document.getElementById('doctorSelect');
        select.innerHTML = '';
        data.forEach(doctor => {
          const option = document.createElement('option');
          option.value = doctor.DoctorID || doctor.doctorID;
          option.textContent = doctor.FullName || doctor.fullName;
          select.appendChild(option);
        });
      })
      .catch(err => console.error('Ошибка загрузки списка врачей', err));
  }
  
  function filterNewPatients() {
    const search = document.getElementById('searchNewPatients').value.toLowerCase();
    const filtered = newPatientsData.filter(p => (p.FullName || p.fullName).toLowerCase().includes(search));
    const tbody = document.getElementById('newPatientsTable').querySelector('tbody');
    tbody.innerHTML = '';
    filtered.forEach(patient => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${patient.NewPatientID || patient.newPatientID}</td>
        <td>${patient.FullName || patient.fullName}</td>
        <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
        <td>${patient.Gender || patient.gender}</td>
      `;
      tr.addEventListener('click', () => {
        tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
        tr.classList.add('selected');
      });
      tbody.appendChild(tr);
    });
  }
  
  function filterPatients() {
    const search = document.getElementById('searchPatients').value.toLowerCase();
    const filtered = patientsData.filter(p => (p.FullName || p.fullName).toLowerCase().includes(search));
    const tbody = document.getElementById('patientsTable').querySelector('tbody');
    tbody.innerHTML = '';
    filtered.forEach(patient => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${patient.PatientID || patient.patientID}</td>
        <td>${patient.FullName || patient.fullName}</td>
        <td>${formatDate(patient.DateOfBirth || patient.dateOfBirth)}</td>
        <td>${patient.Gender || patient.gender}</td>
        <td>${formatDate(patient.RecordDate || patient.recordDate)}</td>
        <td>${formatDate(patient.DischargeDate || patient.dischargeDate)}</td>
      `;
      tr.addEventListener('dblclick', () => {
        alert(`Открыть процедуры для пациента ID ${patient.PatientID || patient.patientID}`);
      });
      tr.addEventListener('click', () => {
        tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
        tr.classList.add('selected');
      });
      tbody.appendChild(tr);
    });
  }
  
  function filterDoctors() {
    const search = document.getElementById('searchDoctors').value.toLowerCase();
    const filtered = doctorsData.filter(d => (d.FullName || d.fullName).toLowerCase().includes(search));
    const tbody = document.getElementById('doctorsTable').querySelector('tbody');
    tbody.innerHTML = '';
    filtered.forEach(doctor => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${doctor.DoctorID || doctor.doctorID}</td>
        <td>${doctor.FullName || doctor.fullName}</td>
        <td>${doctor.Specialty || doctor.specialty}</td>
        <td>${doctor.OfficeNumber || doctor.officeNumber}</td>
        <td>${doctor.WorkExperience || doctor.workExperience}</td>
      `;
      tr.addEventListener('dblclick', () => {
        alert(`Открыть процедуры для врача ID ${doctor.DoctorID || doctor.doctorID}`);
      });
      tr.addEventListener('click', () => {
        tbody.querySelectorAll('tr').forEach(row => row.classList.remove('selected'));
        tr.classList.add('selected');
      });
      tbody.appendChild(tr);
    });
  }
  
  function assignPatient() {
    const newPatientsTbody = document.getElementById('newPatientsTable').querySelector('tbody');
    const selectedRow = newPatientsTbody.querySelector('tr.selected');
    if (!selectedRow) {
      alert('Выберите пациента из списка новых пациентов.');
      return;
    }
    const newPatientID = selectedRow.cells[0].textContent;
    const doctorSelect = document.getElementById('doctorSelect');
    const doctorID = doctorSelect.value;
    if (!doctorID) {
      alert('Выберите врача для назначения.');
      return;
    }
    const recordDate = document.getElementById('recordDate').value;
    const dischargeDate = document.getElementById('dischargeDate').value;
    if (!recordDate || !dischargeDate) {
      alert('Выберите дату записи и дату выписки.');
      return;
    }
    
    const payload = {
      newPatientID: parseInt(newPatientID),
      doctorID: parseInt(doctorID),
      recordDate: recordDate,
      dischargeDate: dischargeDate
    };
    
    fetch('http://localhost:8080/api/chief/assignPatient', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
    .then(response => response.json())
    .then(result => {
      if(result.success) {
        alert('Пациент успешно назначен.');
        loadNewPatients();
        loadPatients();
      } else {
        alert('Ошибка при назначении пациента: ' + result.message);
      }
    })
    .catch(err => {
      console.error(err);
      alert('Ошибка при соединении с сервером.');
    });
  }
  
  function formatDate(dateString) {
    if(!dateString) return '';
    const date = new Date(dateString);
    const day = ('0' + date.getDate()).slice(-2);
    const month = ('0' + (date.getMonth() + 1)).slice(-2);
    const year = date.getFullYear();
    return `${day}.${month}.${year}`;
  }