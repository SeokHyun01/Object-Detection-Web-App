// 카메라 ID를 얻어서 MQTT로  전송 
//SERV_ADDR **************************
const SERV_ADDR = "ictrobot.hknu.ac.kr";
const SERV_PORT = 8090;

const TOPIC_MOTOR = "camera/update/angle/syn";
const TOPIC_MOTOR_ACK = "camera/update/angle/ack";
const TOPIC_IMAGE = "camera/image";
const TOPIC_EVENT = "event";
const TOPIC_EVENT_ACK = "event/create";
const TOPIC_MAKE_VIDEO = "event/video/create";

let camera_id;
let user_id;
let client;

let listUpdateInterval;
let sendInterval;
let list = [];
let count = 1;
let prev_time = 0;

// 없어질 예정
const TOPIC_WEBRTC = "start_webRTC";
const TOPIC_WEBRTC_FIN = "stop_webRTC";

async function mqtt(userId, cameraId) {
    user_id = userId;
    camera_id = cameraId;

    if (client != null && client.isConnected()) {
        clearInterval(sendInterval);
        clearInterval(listUpdateInterval);
        client.disconnect();

        list = [];
        count = 1;
        prev_time = 0;
    }

    const client_id = Math.random().toString(36).substring(2, 12);
    client = new Paho.MQTT.Client(SERV_ADDR, Number(SERV_PORT), client_id);
    client.connect({ useSSL: true, onSuccess: onConnect });

    function onConnect() {
        client.subscribe(TOPIC_EVENT_ACK);
        client.subscribe(TOPIC_MOTOR);
        client.subscribe(TOPIC_WEBRTC);
        client.subscribe(TOPIC_WEBRTC_FIN);
        client.onMessageArrived = onMessageArrived;

        // 3초에 한번씩 list의 개수를 확인해서 10개 이상이면 전송
        sendInterval = setInterval(function () {
            if (list.length >= 10 * count && client.isConnected()) {
                send_list();
                list = [];
                count += 1;
                console.log("send list!!");
            }
        }, 3000);

        // 5초에 한번씩 list의 개수 확인
        listUpdateInterval = setInterval(function () {
            const now_time = new Date().getTime();
            // 만약 최근에 list에 추가된 시간과 현재 시간의 차이가 30초 이상이고 list에 데이터가 있다면
            if (now_time - prev_time > 30000 && list.length > 0 && client.isConnected()) {
                // list 전송
                send_list();
                prev_time = now_time;

                list = [];
                count = 1;
            }
        }, 5000);
    }
}


function onMessageArrived(message) {
    console.log("onMessageArrived : " + message.payloadString + " " + message.destinationName)
    try {
        if (message.destinationName == TOPIC_EVENT_ACK) {
            const json = JSON.parse(message.payloadString);
            const destination_camera_id = json["CameraId"];
            const id = json["Id"];
            if (camera_id == destination_camera_id) {
                list.push(id);
                prev_time = new Date().getTime();
            }
        } else if (message.destinationName == TOPIC_MOTOR) {
            const json = JSON.parse(message.payloadString);
            const destination_camera_id = json["Id"];
            if (camera_id == destination_camera_id) {
                const degree = json["Angle"] + ".";
                send_bluetooth(degree);
            }
        }
    } catch (e) {
        console.log(e);
    }
}

async function send_mqtt(message, destinationName) {
    if (client == null) await mqtt(user_id, camera_id);
    const msg = new Paho.MQTT.Message(message);
    msg.destinationName = destinationName;
    client.send(msg);
}

function send_list() {
    // json 안에 list와 userId 추가
    const message = {};
    message["EventIds"] = Array.from(list);
    message["UserId"] = user_id;
    message["CameraId"] = camera_id;
    const msg = new Paho.MQTT.Message(JSON.stringify(message));
    msg.destinationName = TOPIC_MAKE_VIDEO;
    client.send(msg);
}

function disconncet_mqtt() {
    if (client != null && client.isConnected()) {
        if (list.length > 0) {
            send_list();
            
            list = [];
            count = 1;
            prev_time = 0;
        }
        client.disconnect();
    }
}

// function send_mqtt() {

//     get_cameraId();

//     if (camera_id == null) {
//         return;
//     }

//     if (client != null && client.isConnected()) {
//         clearInterval(sendInterval);
//         clearInterval(listUpdateInterval);
//         client.disconnect();

//         list = [];
//         all_list = [];
//         count = 1;
//         prev_time = 0;
//     }

//     const client_id = Math.random().toString(36).substring(2, 12);
//     client = new Paho.MQTT.Client(SERV_ADDR, Number(SERV_PORT), client_id);
//     client.connect({ useSSL: true, onSuccess: onConnect });

//     function onConnect() {
//         let cnt = 0;
//         // 1초에 한번씩 정수형 숫자 1을 전송
//         sendInterval = setInterval(function () {
//             const msg = new Paho.MQTT.Message(cnt.toString());
//             msg.destinationName = "test";
//             client.send(msg);
//             cnt++;
//         }, 1000);

//         client.subscribe("test_repeat");
//         // //블투 확인용 - 성공!
//         // client.subscribe("test_bluetooth");

//         client.onMessageArrived = onMessageArrived;
//     }

// // 5초에 한번씩 list의 개수 확인
// listUpdateInterval = setInterval(function () {
//     const now_time = new Date().getTime();
//     // 만약 최근에 list에 추가된 시간과 현재 시간의 차이가 30초 이상이고 all_list에 데이터가 있다면
//     if (now_time - prev_time > 30000 && all_list.length > 0 && client.isConnected()) {
//         // all_list를 전송
//         const msg = new Paho.MQTT.Message(all_list.join(','));
//         msg.destinationName = "all_test";
//         client.send(msg);
//         prev_time = now_time;

//         list = [];
//         all_list = [];
//         count = 1;
//         cnt = 0;
//     }
// }, 5000);
// }


// function onMessageArrived(message) {

//     // // 블투 확인용 - 성공!
//     // if (message.destinationName == "test_bluetooth") {
//     //     send_bluetooth(message.payloadString + ".");
//     //     return;
//     // }

//     prev_time = new Date().getTime();
//     const msg = message.payloadString;
//     all_list.push(msg);

//     if (list.length < 10 * count) {
//         list.push(msg);
//         console.log(msg);
//     } else {

//         if (client.isConnected()) {
//             //10개의 list를 하나의 문자열로 만들어서 전송
//             const new_msg = new Paho.MQTT.Message(list.join(','));
//             new_msg.destinationName = "real_test";
//             client.send(new_msg);
//         };

//         list = [];
//         list.push(msg);
//         count++;
//     }
// }

// // ** 여기서부터는 test를 위해서 만든 확인용 코드  **
// function test_connect() {

//     const chatMessages = document.getElementById("messages");

//     const test_client_id = Math.random().toString(36).substring(2, 12);
//     const test_client = new Paho.MQTT.Client(SERV_ADDR, Number(SERV_PORT), test_client_id);
//     test_client.connect({ useSSL: true, onSuccess: onTestConnect });

//     function onTestConnect() {
//         chatMessages.innerHTML += "<br>서버에 연결되었습니다.";

//         test_client.subscribe("test");
//         test_client.subscribe("real_test");
//         test_client.subscribe("all_test");
//         test_client.onMessageArrived = onTESTMessageArrived;
//     }

//     function onTESTMessageArrived(message) {
//         console.log("onTESTMessageArrived : " + message.payloadString + " " + message.destinationName)
//         if (message.destinationName == "test") {
//             // chatMessages.innerHTML += "<br>내용 : " + message.payloadString + ", 목적지: " + message.destinationName;
//             const msg = new Paho.MQTT.Message(message.payloadString);
//             msg.destinationName = "test_repeat";
//             test_client.send(msg);

//         } else {
//             chatMessages.innerHTML += "<br>내용 : " + message.payloadString + ", 목적지: " + message.destinationName;
//         }
//     }

//     // // 블투 확인용 - 성공!
//     // // 2초에 한번씩 180~0 값을 10씩 증가하면서 전송
//     // let test_cnt = 0;
//     // setInterval(function () {
//     //     const msg = new Paho.MQTT.Message(test_cnt.toString());
//     //     msg.destinationName = "test_bluetooth";
//     //     test_client.send(msg);
//     //     test_cnt -= 10;
//     //     if (test_cnt < 0) {
//     //         test_cnt = 180;
//     //     }
//     // }, 2000);
// }

// function stop_mqtt() {
//     clearInterval(sendInterval);
// }
