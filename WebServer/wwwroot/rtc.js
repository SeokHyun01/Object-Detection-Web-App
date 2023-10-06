let _connection_id, my_connection;


function build_rtc() {
    my_connection = new RTCPeerConnection({
        iceServers: [
            {
                urls: [
                    'stun:stun.l.google.com:19302',
                    'stun:stun1.l.google.com:19302',
                    'stun:stun2.l.google.com:19302',
                    'stun:stun3.l.google.com:19302',
                    'stun:stun4.l.google.com:19302',
                ],
            },
        ],
    });
    my_connection.addEventListener('icecandidate', handle_ice);
    my_connection.addEventListener('addstream', handle_addStream);
    const camera = document.getElementById('video');
    camera.getTracks().forEach((track) => my_connection.addTrack(track, myStream));
}

async function init_rtc(connection_id) {0
    _connection_id = connection_id;
    build_rtc();
}

async function send_offer() {
    try {
        const offer = await my_connection.createOffer();
        await my_connection.setLocalDescription(offer);

        return JSON.stringify(offer);

    } catch (error) {
        console.log(error);
    }
}

async function send_answer(offer) {
    const obj = JSON.parse(offer);
    const received_offer = new RTCSessionDescription(obj);

    await my_connection.setRemoteDescription(received_offer);

    const answer = await my_connection.createAnswer();
    await my_connection.setLocalDescription(answer);

    return JSON.stringify(answer);
}

async function receive_answer(answer) {
    try {
        console.log(answer);
        const obj = JSON.parse(answer);
        const received_answer = new RTCSessionDescription(obj);
        await my_connection.setRemoteDescription(received_answer);

    } catch (error) {
        console.log(error);
    }
}

async function receive_ice(ice) {
    try {
        const received_ice = JSON.parse(ice);
        await my_connection.addIceCandidate(received_ice)

    } catch (error) {
        console.log(error)
    }
}

async function handle_ice(data) {
    console.log("handle_ice");

    if (data && data.candidate) {
        const connection = new signalR.HubConnectionBuilder().withUrl('/hub/rtc').build();
        await connection.start();

        const ice = JSON.stringify(data.candidate);
        connection.send('SendIce', ice, _connection_id);

        await connection.stop();
    }
}

function handle_addStream(data) {
    if (data && data.stream) {
        peer_video = document.getElementById('peer_video');

        if (peer_video) {
            peer_video.srcObject = data.stream;
        }
    }
}
