
let _characteristic;

function set_bluetooth() {
    if (!navigator.bluetooth) {
        alert("Your browser does not support Bluetooth")
        return;
    } else {
        connect_bluetooth();
    }
}

async function connect_bluetooth() {
    navigator.bluetooth.requestDevice({
        filters: [{ services: ["0000ffe0-0000-1000-8000-00805f9b34fb"] }]
    }).then(device => {
        return device.gatt.connect();
    }).then(server => {
        return server.getPrimaryService("0000ffe0-0000-1000-8000-00805f9b34fb");
    }).then(service => {
        return service.getCharacteristic("0000ffe1-0000-1000-8000-00805f9b34fb");
    }).then(characteristic => {
        _characteristic = characteristic;
        receive_bluetooth(characteristic);
    })
}

function receive_bluetooth(characteristic) {
    characteristic.startNotifications().then(() => {
        characteristic.addEventListener('characteristicvaluechanged', (event) => {
            const value = new TextDecoder().decode(event.target.value);
            // value = "degree/ack"의 형태 
            const value_split = value.split("/");
            const json = {};
            json["Id"] = camera_id;
            json["Angle"] = Number(value_split[0]);
            send_mqtt(JSON.stringify(json), TOPIC_MOTOR_ACK);
        });
    });
}

function send_bluetooth(data) {
    if (_characteristic) {
        _characteristic.writeValue(new TextEncoder().encode(data));
    } else {
        try {
            set_bluetooth();
            send_bluetooth(data);
        } catch (e) {
            console.log(e);
        }
    }
}