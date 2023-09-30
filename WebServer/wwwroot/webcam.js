async function start_video(id) {
    if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
            const video = document.getElementById(id);
            if (`srcObject` in video) {
                video.srcObject = stream;
            } else {
                video.src = window.URL.createObjectURL(stream);
            }
            video.onloadedmetadata = _ => video.play();

        } catch (err) {
            console.log(err)
        }
    }
}

function stop_video(id) {
    const video = document.getElementById(id);
    const stream = video.srcObject;

    if (stream) {
        const tracks = stream.getTracks();
        for (const track of tracks) {
            track.stop();
        }

        video.srcObject = null;
    }

    clearInterval(interval_id);
}
