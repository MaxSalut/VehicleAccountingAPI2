document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('currentYear').textContent = new Date().getFullYear();

    const queryCategorySelect = document.getElementById('queryCategorySelect');
    const querySelect = document.getElementById('querySelect');
    const queryDetailsContainer = document.getElementById('queryDetailsContainer');
    const queryTitle = document.getElementById('queryTitle');
    const queryDescription = document.getElementById('queryDescription');
    const queryParametersForm = document.getElementById('queryParametersForm');
    const executeQueryButton = document.getElementById('executeQueryButton');
    const resultsContainer = document.getElementById('resultsContainer');
    const resultsTableHead = document.getElementById('resultsTableHead');
    const resultsTableBody = document.getElementById('resultsTableBody');
    const resultsSeparator = document.getElementById('resultsSeparator');
    const noResultsMessage = document.getElementById('noResultsMessage');
    const errorMessage = document.getElementById('errorMessage');
    const resultsTitle = document.getElementById('resultsTitle');

    const baseQueryApiUrl = 'https://localhost:44322/api/Queries';
    const baseDataApiUrl = 'https://localhost:44322/api';

    const headerTranslations = {
        "driverId": "ID Водія",
        "name": "Ім'я",
        "email": "Email",
        "licenseNumber": "Номер посвідчення",
        "vehicleId": "ID ТЗ",
        "licensePlate": "Номерний знак",
        "model": "Модель",
        "year": "Рік",
        "vehicleTypeName": "Тип ТЗ",
        "vehicleTypeId": "ID Типу ТЗ",
        "assignmentId": "ID Призначення",
        "startDate": "Дата початку",
        "endDate": "Дата завершення",
        "maintenanceRecordId": "ID Запису ТО",
        "maintenanceDate": "Дата ТО",
        "description": "Опис",
        "cost": "Вартість",
        "tripId": "ID Поїздки",
        "routeDescription": "Маршрут",
        "cargoDescription": "Вантаж",
        "plannedStartDateTime": "План. початок",
        "plannedEndDateTime": "План. завершення",
        "actualStartDateTime": "Факт. початок",
        "actualEndDateTime": "Факт. завершення",
        "status": "Статус",
        "tripLogId": "ID Журналу",
        "logDate": "Дата запису",
        "driverName": "Водій",
        "vehicleLicensePlate": "Номер ТЗ",
        "tripRoute": "Маршрут поїздки",
        "startMileage": "Поч. пробіг (км)",
        "endMileage": "Кінц. пробіг (км)",
        "calculatedMileage": "Пробіг (км)",
        "fuelConsumedLiters": "Пальне (л)",
        "notes": "Примітки",
        "driver1_Id": "ID Водія 1",
        "driver1_Name": "Ім'я Водія 1",
        "driver2_Id": "ID Водія 2",
        "driver2_Name": "Ім'я Водія 2",
        "vehicleTypeCount": "К-ть ТЗ цього типу",
        "totalMaintenanceCost": "Загальна вартість ТО"
    };

    function translateHeader(headerKey) {
        return headerTranslations[headerKey] || headerKey.replace(/([A-Z])/g, ' $1').trim();
    }

    const queries = {
        simple: [
            {
                id: 'S1',
                name: 'Водії за типом ТЗ',
                description: 'Знайти всіх водіїв, які були призначені на транспортні засоби вказаного типу.',
                endpoint: `${baseQueryApiUrl}/drivers-by-vehicle-type`, // Цей ендпоінт на сервері має очікувати vehicleTypeName (string)
                params: [
                    {
                        id: 'vehicleTypeName', // Повертаємо до vehicleTypeName
                        label: 'Назва типу ТЗ',
                        type: 'text', // Повертаємо до text
                        required: true,
                        placeholder: 'Наприклад, Вантажівка'
                        // dataEndpoint, dataValueField, dataTextField видалені
                    }
                ]
            },
            {
                id: 'S2',
                name: 'ТЗ за вартістю та датою ТО',
                description: 'Вивести ТЗ, що проходили ТО після вказаної дати, і вартість ТО яких перевищила певну суму.',
                endpoint: `${baseQueryApiUrl}/vehicles-by-maintenance`,
                params: [
                    { id: 'minCost', label: 'Мінімальна вартість ТО (грн)', type: 'number', step: '0.01', required: true, placeholder: 'Наприклад, 1000.00' },
                    { id: 'dateAfter', label: 'ТО після дати', type: 'date', required: true }
                ]
            },
            {
                id: 'S3',
                name: 'Поїздки водія за період',
                description: 'Знайти поїздки певного водія, що фактично розпочалися у вказаний період.',
                endpoint: `${baseQueryApiUrl}/trips-by-driver-period`,
                params: [
                    {
                        id: 'driverId',
                        label: 'Водій',
                        type: 'select', // Залишаємо select для водіїв
                        required: true,
                        dataEndpoint: `${baseDataApiUrl}/Drivers`,
                        dataValueField: 'driverId',
                        dataTextField: 'name',
                        placeholder: 'Оберіть водія...'
                    },
                    { id: 'periodStartDate', label: 'Дата початку періоду', type: 'datetime-local', required: true },
                    { id: 'periodEndDate', label: 'Дата кінця періоду', type: 'datetime-local', required: true }
                ]
            },
            {
                id: 'S4',
                name: 'Активно призначені ТЗ (старші за рік)',
                description: 'Вивести ТЗ з активними призначеннями, старші за вказаний рік випуску.',
                endpoint: `${baseQueryApiUrl}/active-vehicles-older-than`,
                params: [
                    { id: 'yearOlderThan', label: 'Рік випуску до (не включно)', type: 'number', required: true, placeholder: 'Наприклад, 2020' }
                ]
            },
            {
                id: 'S5',
                name: 'Журнал поїздок (пробіг > X для ТЗ)',
                description: 'Записи журналу для ТЗ, де пробіг (EndMileage - StartMileage) перевищив X км.',
                endpoint: `${baseQueryApiUrl}/triplogs-by-mileage`,
                params: [
                    {
                        id: 'vehicleId',
                        label: 'Транспортний засіб',
                        type: 'select', // Залишаємо select для ТЗ
                        required: true,
                        dataEndpoint: `${baseDataApiUrl}/Vehicles`,
                        dataValueField: 'vehicleId',
                        dataTextField: 'licensePlate',
                        placeholder: 'Оберіть ТЗ...'
                    },
                    { id: 'minMileage', label: 'Мінімальний пробіг (км)', type: 'number', required: true, placeholder: 'Наприклад, 100' }
                ]
            }
        ],
        complex: [
            {
                id: 'C1',
                name: 'Водії, що керували ВСІМА ТЗ типу',
                description: 'Знайти водіїв, які мали призначення на кожен ТЗ вказаного типу.',
                endpoint: `${baseQueryApiUrl}/drivers-all-vehicles-of-type`, // Цей ендпоінт на сервері має очікувати vehicleTypeName (string)
                params: [
                    {
                        id: 'vehicleTypeName', // Повертаємо до vehicleTypeName
                        label: 'Назва типу ТЗ',
                        type: 'text', // Повертаємо до text
                        required: true,
                        placeholder: 'Наприклад, Автобус'
                        // dataEndpoint, dataValueField, dataTextField видалені
                    }
                ]
            },
            {
                id: 'C2',
                name: 'ТЗ з ТО ТІЛЬКИ у вказаному році',
                description: 'Знайти ТЗ, всі записи ТО яких припадають виключно на вказаний рік.',
                endpoint: `${baseQueryApiUrl}/vehicles-maintenance-only-in-year`,
                params: [
                    { id: 'maintenanceYear', label: 'Рік ТО', type: 'number', required: true, placeholder: 'Наприклад, 2024' }
                ]
            },
            {
                id: 'C3',
                name: 'Пари водіїв з однаковими наборами ТЗ',
                description: 'Знайти унікальні пари водіїв, призначених на точно таку саму множину ТЗ.',
                endpoint: `${baseQueryApiUrl}/driver-pairs-same-vehicles`,
                params: []
            }
        ]
    };

    queryCategorySelect.addEventListener('change', function () {
        const selectedCategory = this.value;
        querySelect.innerHTML = '<option value="">Оберіть запит...</option>';
        querySelect.disabled = true;
        queryDetailsContainer.style.display = 'none';
        hideResults();

        if (selectedCategory && queries[selectedCategory]) {
            queries[selectedCategory].forEach(query => {
                const option = document.createElement('option');
                option.value = query.id;
                option.textContent = query.name;
                querySelect.appendChild(option);
            });
            querySelect.disabled = false;
        }
    });

    querySelect.addEventListener('change', async function () {
        const selectedQueryId = this.value;
        const selectedCategory = queryCategorySelect.value;
        hideResults();

        if (selectedQueryId && selectedCategory && queries[selectedCategory]) {
            const selectedQuery = queries[selectedCategory].find(q => q.id === selectedQueryId);
            if (selectedQuery) {
                queryTitle.textContent = selectedQuery.name;
                queryDescription.innerHTML = selectedQuery.description.replace(/\n/g, '<br>');
                queryParametersForm.innerHTML = '';

                if (selectedQuery.params.length > 0) {
                    const paramPromises = selectedQuery.params.map(async param => {
                        const paramGroup = document.createElement('div');
                        paramGroup.className = 'param-group mb-3';

                        const labelEl = document.createElement('label');
                        labelEl.htmlFor = param.id;
                        labelEl.className = 'form-label';
                        labelEl.textContent = param.label + (param.required ? ' *' : '');
                        paramGroup.appendChild(labelEl);

                        if (param.type === 'select') {
                            const selectEl = document.createElement('select');
                            selectEl.id = param.id;
                            selectEl.name = param.id;
                            selectEl.className = 'form-select';
                            if (param.required) selectEl.required = true;

                            const defaultOption = document.createElement('option');
                            defaultOption.value = '';
                            defaultOption.textContent = param.placeholder || `Оберіть ${param.label.toLowerCase().replace(' *', '')}...`;
                            if (param.required) {
                                defaultOption.disabled = true;
                            }
                            defaultOption.selected = true;
                            selectEl.appendChild(defaultOption);

                            paramGroup.appendChild(selectEl);

                            if (param.dataEndpoint) {
                                try {
                                    const response = await fetch(param.dataEndpoint);
                                    if (!response.ok) {
                                        throw new Error(`Помилка ${response.status} для ${param.dataEndpoint}`);
                                    }
                                    const data = await response.json();
                                    data.forEach(item => {
                                        const option = document.createElement('option');
                                        const valueField = param.dataValueField || 'id';
                                        const textField = param.dataTextField || 'name';

                                        if (item.hasOwnProperty(valueField) && item.hasOwnProperty(textField)) {
                                            option.value = item[valueField];
                                            option.textContent = item[textField];
                                        } else {
                                            const keys = Object.keys(item);
                                            if (keys.length > 0) option.value = item[keys[0]];
                                            if (keys.length > 1) option.textContent = `${item[keys[0]]} - ${item[keys[1]]}`;
                                            else if (keys.length === 1) option.textContent = item[keys[0]];
                                            else option.textContent = "Невідомий елемент";
                                            console.warn(`Для списку "${param.label}": не знайдено поля ${valueField} або ${textField} в елементі:`, item, `Використано значення: ${option.value}, текст: ${option.textContent}`);
                                        }
                                        selectEl.appendChild(option);
                                    });
                                } catch (error) {
                                    console.error(`Помилка завантаження даних для списку "${param.label}":`, error);
                                    const errorOption = document.createElement('option');
                                    errorOption.value = '';
                                    errorOption.textContent = 'Помилка завантаження списку';
                                    errorOption.disabled = true;
                                    selectEl.appendChild(errorOption);
                                }
                            }
                        } else { // Для інших типів полів (text, number, date, etc.)
                            const input = document.createElement('input');
                            input.type = param.type;
                            input.id = param.id;
                            input.name = param.id;
                            input.className = 'form-control';
                            if (param.required) input.required = true;
                            if (param.placeholder) input.placeholder = param.placeholder;
                            if (param.type === 'number' && param.step) input.step = param.step;
                            if (param.type === 'date' || param.type === 'datetime-local') {
                                input.min = "2000-01-01" + (param.type === 'datetime-local' ? "T00:00" : "");
                            }
                            paramGroup.appendChild(input);
                        }
                        return paramGroup;
                    });

                    const resolvedParamGroups = await Promise.all(paramPromises);
                    resolvedParamGroups.forEach(group => queryParametersForm.appendChild(group));

                } else {
                    queryParametersForm.innerHTML = '<p>Для цього запиту параметри не потрібні.</p>';
                }
                queryDetailsContainer.style.display = 'block';
            }
        } else {
            queryDetailsContainer.style.display = 'none';
        }
    });

    executeQueryButton.addEventListener('click', async function () {
        const selectedQueryId = querySelect.value;
        const selectedCategory = queryCategorySelect.value;
        if (!selectedQueryId || !selectedCategory) {
            alert('Будь ласка, оберіть запит.');
            return;
        }

        const selectedQuery = queries[selectedCategory].find(q => q.id === selectedQueryId);
        if (!selectedQuery) {
            alert('Обраний запит не знайдено.');
            return;
        }

        hideResults();
        let queryParams = new URLSearchParams();
        let formIsValid = true;

        selectedQuery.params.forEach(param => {
            const inputField = document.getElementById(param.id);
            if (inputField) inputField.classList.remove('is-invalid');
        });

        for (const param of selectedQuery.params) {
            const inputElement = document.getElementById(param.id);
            if (inputElement) {
                if (param.required && !inputElement.value) {
                    alert(`Будь ласка, заповніть або оберіть значення для обов'язкового поля: ${param.label}`);
                    inputElement.focus();
                    inputElement.classList.add('is-invalid');
                    formIsValid = false;
                    break;
                }
                if (inputElement.value) {
                    queryParams.append(param.id, inputElement.value);
                }
            }
        }

        if (!formIsValid) return;

        const fullUrl = selectedQuery.params.length > 0 ? `${selectedQuery.endpoint}?${queryParams.toString()}` : selectedQuery.endpoint;

        if (resultsTitle) resultsTitle.textContent = `Результати запиту: "${selectedQuery.name}"`;
        resultsContainer.style.display = 'block';
        resultsSeparator.style.display = 'block';

        try {
            const response = await fetch(fullUrl);

            if (response.status === 404) {
                noResultsMessage.textContent = `Даних за запитом "${selectedQuery.name}" не знайдено (статус 404).`;
                noResultsMessage.style.display = 'block';
                resultsTableHead.innerHTML = '';
                resultsTableBody.innerHTML = '';
                return;
            }

            if (!response.ok) {
                let errorData;
                let errorText = `HTTP помилка! Статус: ${response.status} ${response.statusText}`;
                try {
                    errorData = await response.json();
                    errorText = errorData.message || errorData.title || errorText;
                } catch (e) {
                    // Залишаємо errorText як є
                }
                throw new Error(errorText);
            }

            const data = await response.json();
            displayResults(data, selectedQuery.name);

        } catch (err) {
            console.error('Помилка виконання запиту:', err);
            errorMessage.textContent = `Помилка: ${err.message}`;
            errorMessage.style.display = 'block';
            resultsTableHead.innerHTML = '';
            resultsTableBody.innerHTML = '';
            noResultsMessage.style.display = 'none';
        }
    });

    function hideResults() {
        resultsContainer.style.display = 'none';
        resultsTableHead.innerHTML = '';
        resultsTableBody.innerHTML = '';
        noResultsMessage.style.display = 'none';
        errorMessage.style.display = 'none';
        resultsSeparator.style.display = 'none';
        if (resultsTitle) resultsTitle.textContent = '';
    }

    function displayResults(data, queryName) {
        resultsTableHead.innerHTML = '';
        resultsTableBody.innerHTML = '';

        if (!data || data.length === 0) {
            noResultsMessage.textContent = `Даних за запитом "${queryName}" не знайдено.`;
            noResultsMessage.style.display = 'block';
            return;
        }
        noResultsMessage.style.display = 'none';

        const headers = Object.keys(data[0]);
        const headerRow = document.createElement('tr');
        headers.forEach(headerText => {
            const th = document.createElement('th');
            th.textContent = translateHeader(headerText);
            headerRow.appendChild(th);
        });
        resultsTableHead.appendChild(headerRow);

        data.forEach(item => {
            const row = document.createElement('tr');
            headers.forEach(header => {
                const td = document.createElement('td');
                let value = item[header];
                const currentParam = queries.simple.concat(queries.complex)
                    .flatMap(q => q.params)
                    .find(p => p.id === header || header.toLowerCase().includes(p.id.toLowerCase()));


                if (typeof value === 'string' && (value.includes('T') || value.match(/^\d{4}-\d{2}-\d{2}$/))) {
                    try {
                        const dateObj = new Date(value);
                        if (!isNaN(dateObj)) {
                            const isOnlyDate = (dateObj.getUTCHours() === 0 && dateObj.getUTCMinutes() === 0 && dateObj.getUTCSeconds() === 0 && dateObj.getUTCMilliseconds() === 0 && !value.includes('T')) || value.length === 10;
                            if (isOnlyDate) {
                                value = dateObj.toLocaleDateString('uk-UA');
                            } else {
                                value = dateObj.toLocaleString('uk-UA', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
                            }
                        }
                    } catch (e) { /* залишити як є */ }
                } else if (typeof value === 'number' && !Number.isInteger(value)) {
                    if (header.toLowerCase().includes('cost') || header.toLowerCase().includes('price') || (currentParam && currentParam.step === '0.01')) {
                        value = value.toFixed(2);
                    } else {
                        value = parseFloat(value.toFixed(3));
                    }
                }
                td.textContent = (value === null || value === undefined) ? '' : value;
                row.appendChild(td);
            });
            resultsTableBody.appendChild(row);
        });
    }
});