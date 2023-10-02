const threshold_move = 100;   // 달라진 픽셀 값 기준치 설정 (defalut=50)
const diff_compare = 15;      // 달라진 픽셀 갯수 기준치 설정 (defalut=7) 
let isFirst = true;

let img_first = null;
let img_second = null;
let img_third = null;

function motion_detect(input, image_width, image_height) {
    const boxes = [];
    const rgb = tf.mul(tf.squeeze(input), tf.scalar(255));

    if (isFirst) {
        img_first = rgb;
        img_second = rgb;
        img_third = rgb;
        isFirst = false;
    } else {
        imgThird = rgb;
    }

    const box = tf.tidy(() => {
        // rgbToGrayscale
        const imgFirstGray = tf.squeeze(tf.image.rgbToGrayscale(img_first));
        const imgSecondGray = tf.squeeze(tf.image.rgbToGrayscale(img_second));
        const imgThirdGray = tf.squeeze(tf.image.rgbToGrayscale(img_third));

        // difference image
        const imgDiff1 = tf.abs(tf.sub(imgFirstGray, imgSecondGray));
        const imgDiff2 = tf.abs(tf.sub(imgSecondGray, imgThirdGray));
        const mask1 = tf.greater(imgDiff1, tf.scalar(threshold_move));
        const mask2 = tf.greater(imgDiff2, tf.scalar(threshold_move));

        // and 연산
        const diff = tf.logicalAnd(mask1, mask2);
        const diff_sum = tf.sum(diff).arraySync();

        if (diff_sum > diff_compare) {

            // 가장 먼저 오는 true와 마지막 true의 위치를 구함
            const flatten = tf.util.flatten(diff.arraySync());
            const start = flatten.indexOf(1);
            const end = flatten.lastIndexOf(1);

            const div_x = image_width / 224;
            const div_y = image_height / 224;

            const start_x = (start % 224) * div_x;
            const start_y = (Math.floor(start / 224)) * div_y;
            const end_x = (end % 224) * div_x;
            const end_y = (Math.floor(end / 224)) * div_y;
            const box = [start_x, start_y, end_x, end_y, "motion", 1];
            return box;
        }
    });

    if (box) {
        // box의 좌표 값 중 너비가  canvas.width - 140 ~ canvas.width 사이라면 box를 그리지 않음
        // box의 좌표 값 중 높이가 0 ~ 30 사이라면 box를 그리지 않음
        if (box[0] < (canvas.width - 140) && box[2] < (canvas.width - 140) && box[1] > 30 && box[3] > 30) {
            boxes.push(box);
        }
    }

    img_first = img_second;
    img_second = rgb;

    return boxes;
}