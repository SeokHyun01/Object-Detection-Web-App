let confidence_threshold = 0.3;
const iou_threshold = 0.7;

const coco_classes = [
    'person', 'bicycle', 'car', 'motorcycle', 'airplane', 'bus', 'train', 'truck', 'boat',
    'traffic light', 'fire hydrant', 'stop sign', 'parking meter', 'bench', 'bird', 'cat', 'dog', 'horse',
    'sheep', 'cow', 'elephant', 'bear', 'zebra', 'giraffe', 'backpack', 'umbrella', 'handbag', 'tie', 'suitcase',
    'frisbee', 'skis', 'snowboard', 'sports ball', 'kite', 'baseball bat', 'baseball glove', 'skateboard',
    'surfboard', 'tennis racket', 'bottle', 'wine glass', 'cup', 'fork', 'knife', 'spoon', 'bowl', 'banana', 'apple',
    'sandwich', 'orange', 'broccoli', 'carrot', 'hot dog', 'pizza', 'donut', 'cake', 'chair', 'couch', 'potted plant',
    'bed', 'dining table', 'toilet', 'tv', 'laptop', 'mouse', 'remote', 'keyboard', 'cell phone', 'microwave', 'oven',
    'toaster', 'sink', 'refrigerator', 'book', 'clock', 'vase', 'scissors', 'teddy bear', 'hair drier', 'toothbrush'
];

const fire_classes = [
    'fire', 'smoke'
];

const face_claaes = [
    'face'
];


function get_name(model_name) {
    if (model_name == "coco") {
        return coco_classes;
    } else if (model_name == "fire") {
        return fire_classes;
    } else if (model_name == "face") {
        return face_claaes;
    }
}


function preprocess_input(image) {
    const [image_width, image_height] = [224, 224];
    const canvas = document.createElement("canvas");
    canvas.width = image_width;
    canvas.height = image_height;
    const context = canvas.getContext("2d");
    context.drawImage(image, 0, 0, canvas.width, canvas.height);
    const imgData = context.getImageData(0, 0, canvas.width, canvas.height);

    const img = tf.browser.fromPixels(imgData);
    return tf.div(tf.expandDims(img), 255);
}

function process_output(output, image_width, image_height, model_name) {

    let boxes = [];
    const classes = get_name(model_name);

    for (let i = 0; i < 1029; i++) {
        const [class_id, prob] = [...Array(classes.length).keys()]
            .map(col => [col, output[1029 * (col + 4) + i]])
            .reduce((accum, item) => item[1] > accum[1] ? item : accum, [0, 0]);

        if (prob < confidence_threshold) {
            continue;
        }

        const label = classes[class_id];
        const xc = output[i];
        const yc = output[1029 + i];
        const w = output[2 * 1029 + i];
        const h = output[3 * 1029 + i];
        const x1 = (xc - w / 2) / 224 * image_width;
        const y1 = (yc - h / 2) / 224 * image_height;
        const x2 = (xc + w / 2) / 224 * image_width;
        const y2 = (yc + h / 2) / 224 * image_height;
        boxes.push([x1, y1, x2, y2, label, prob]);
    }
    boxes = boxes.sort((box1, box2) => box2[5] - box1[5]);

    const result = [];
    while (boxes.length > 0) {
        result.push(boxes[0]);
        boxes = boxes.filter(box => iou(boxes[0], box) < iou_threshold || boxes[0][4] !== box[4]);
    }

    return result;
}

function iou(box1, box2) {
    return intersection(box1, box2) / union(box1, box2);
}

function union(box1, box2) {
    const [box1_x1, box1_y1, box1_x2, box1_y2] = box1;
    const [box2_x1, box2_y1, box2_x2, box2_y2] = box2;
    const box1_area = (box1_x2 - box1_x1) * (box1_y2 - box1_y1);
    const box2_area = (box2_x2 - box2_x1) * (box2_y2 - box2_y1);

    return box1_area + box2_area - intersection(box1, box2);
}

function intersection(box1, box2) {
    const [box1_x1, box1_y1, box1_x2, box1_y2] = box1;
    const [box2_x1, box2_y1, box2_x2, box2_y2] = box2;
    const x1 = Math.max(box1_x1, box2_x1);
    const y1 = Math.max(box1_y1, box2_y1);
    const x2 = Math.min(box1_x2, box2_x2);
    const y2 = Math.min(box1_y2, box2_y2);

    return (x2 - x1) * (y2 - y1);
}

function draw_boxes(context, boxes) {
    context.strokeStyle = "#00FF00";
    context.lineWidth = 3;
    context.font = "18px serif";

    boxes.forEach(([x1, y1, x2, y2, label]) => {
        // 기존의 bbox를 1.25배로 늘려서 가우시안 블러 적용
        if (label == 'face') {
            roi_x1 = x1 - (x2 - x1) * 0.125;
            roi_y1 = y1 - (y2 - y1) * 0.125;
            roi_x2 = x2 + (x2 - x1) * 0.125;
            roi_y2 = y2 + (y2 - y1) * 0.125;

            const face_roi = context.getImageData(roi_x1, roi_y1, roi_x2 - roi_x1, roi_y2 - roi_y1);
            const face_roi_tensor = tf.cast(tf.browser.fromPixels(face_roi), 'float32');

            const kernel = getGaussianKernel(15, 15);
            const blurImg = blur(face_roi_tensor, kernel);
            const blurred_face_roi = tf.cast(blurImg, 'int32');
            const blurred_face_roi_canvas = document.createElement('canvas');
            blurred_face_roi_canvas.width = roi_x2 - roi_x1;
            blurred_face_roi_canvas.height = roi_y2 - roi_y1;
            tf.browser.draw(blurred_face_roi, blurred_face_roi_canvas);
            context.drawImage(blurred_face_roi_canvas, roi_x1, roi_y1);
        }

        context.strokeRect(x1, y1, x2 - x1, y2 - y1);
        context.fillStyle = "#00ff00";
        const width = context.measureText(label).width;
        context.fillRect(x1, y1, width + 10, 25);
        context.fillStyle = "#000000";
        context.fillText(label, x1, y1 + 18);
    });
}



function change_confidence_threshold() {
    const conf = document.getElementById("conf").value;
    // 만약 0.1~0.9 사이가 아니라면 경고창을 띄우고 기존 값으로 돌아감, 숫자가 아니여도 경고창 띄움
    if (conf < 0.1 || conf > 0.9 || isNaN(conf)) {
        alert("0.1~0.9 사이의 값을 입력해주세요.");
        document.getElementById("conf").value = 0.3;
        return;
    }
    confidence_threshold = conf;
}
