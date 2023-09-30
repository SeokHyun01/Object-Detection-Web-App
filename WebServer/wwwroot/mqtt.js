function get_current_time() {
    const date = new Date();

    const year = date.getFullYear();
    const month = ('0' + (date.getMonth() + 1)).slice(-2);
    const day = ('0' + date.getDate()).slice(-2);
    const hours = ('0' + date.getHours()).slice(-2);
    const minutes = ('0' + date.getMinutes()).slice(-2);
    const seconds = ('0' + date.getSeconds()).slice(-2);

    return year + month + day + 'T' + hours + minutes + seconds;
}

let interval_id;

function execute() {
    const video = document.getElementById('video');
    const canvas = document.querySelector('canvas');
    const context = canvas.getContext('2d');

    video.addEventListener('play', () => {
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        interval_id = setInterval(() => {
            context.drawImage(video, 0, 0);

            const now = get_current_time();
            const image = canvas.toDataURL('image/jpeg');
            send_image(now, image);
        }, 1000);
    });
}

function send_image(now, image) {
    const content = {
        Id: camera_id,
        UserId: user_id,
        Date: now,
        Image: image
    };

    message = new Paho.MQTT.Message(JSON.stringify(content));
    message.destinationName = `camera/image`;
    if (mqtt_client && mqtt_client.isConnected()) {
        mqtt_client.send(message);
        console.log(`${message.destinationName}으로 메시지를 전송했습니다`);

    } else {
        console.log(`연결된 클라이언트가 없습니다.`);
    }
}

let user_id;
let camera_id;
let mqtt_client;

function connect_mqtt_client(_user_id, _camera_id) {
    user_id = _user_id;
    camera_id = _camera_id;
    host = `ictrobot.hknu.ac.kr`;
    port = 8090;

    mqtt_client = new Paho.MQTT.Client(host, Number(port), `${user_id}-${camera_id}`);
    mqtt_client.connect({
        useSSL: true,
        onSuccess: () => console.log(`${user_id}(이)가 연결됐습니다.`)
    });
}
