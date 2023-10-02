function process_output_pose(output, image_width, image_height) {
    let boxes = [];
    let poses = [];

    for (let i = 0; i < 1029; i++) {
        const prob = output[1029 * 4 + i];
        if (prob < confidence_threshold) {
            continue;
        }
        const xc = output[i];
        const yc = output[1029 + i];
        const w = output[2 * 1029 + i];
        const h = output[3 * 1029 + i];
        const x1 = (xc - w / 2) * image_width;
        const y1 = (yc - h / 2) * image_height;
        const x2 = (xc + w / 2) * image_width;
        const y2 = (yc + h / 2) * image_height;

        const keys = [];
        for (let j = 0; j < 51; j++) {
            if (j % 3 == 0) {
                keys.push((output[1029 * (5 + j) + i]) / 224 * image_width);
            } else if (j % 3 == 1) {
                keys.push((output[1029 * (5 + j) + i]) / 224 * image_height);
            } else {
                keys.push((output[1029 * (5 + j) + i]));
            }
        }
        keys.push(i);

        // x1, y1, x2, y2, label, prob, index
        boxes.push([x1, y1, x2, y2, "person", prob, i]);
        // x, y, prob, ... , index
        poses.push(keys);
    }
    boxes = boxes.sort((box1, box2) => box2[5] - box1[5]);

    const result = [];
    while (boxes.length > 0) {
        result.push(boxes[0]);
        boxes = boxes.filter(box => iou(boxes[0], box) < iou_threshold);
    }

    // i 에 맞게 keypoints 일부 추출
    const result_poses = [];
    for (let i = 0; i < result.length; i++) {
        const index = result[i][6];
        // poses의 마지막 index와 i가 같은 것만 추출
        const pose = poses.filter(pose => pose[51] == index);
        result_poses.push(pose[0]);
    }

    return [result, result_poses];
}


function draw_keypoints(ctx, kepts) {

    kepts.forEach(kept => {

        const points = new Array(17 * 2);

        for (let i = 0; i < 17; i++) {

            const x = kept[i * 3];
            const y = kept[i * 3 + 1];
            const prob = kept[i * 3 + 2];

            if (prob > confidence_threshold) {
                points[i * 2] = x;
                points[i * 2 + 1] = y;
            }
            draw_point(ctx, points);
            draw_lines(ctx, points);
        }
    });
}


function draw_point(ctx, points) {

    for (let i = 0; i < 17; i++) {

        const x = points[i * 2];
        const y = points[i * 2 + 1];

        if (x == null || y == null) {
            continue;
        }

        ctx.beginPath();
        ctx.arc(x, y, 3, 0, 2 * Math.PI);
        ctx.fillStyle = "red";
        ctx.fill();
    }
}

function draw_lines(ctx, points) {
    //오른쪽 어깨 팔꿈치 연결
    start_x = points[10]
    start_y = points[11]
    end_x = points[14]
    end_y = points[15]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //왼쪽 어깨 팔꿈치 연결
    start_x = points[12]
    start_y = points[13]
    end_x = points[16]
    end_y = points[17]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //오른쪽 어깨 골반 연결
    start_x = points[10]
    start_y = points[11]
    end_x = points[22]
    end_y = points[23]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //왼쪽 어깨 골반 연결
    start_x = points[12]
    start_y = points[13]
    end_x = points[24]
    end_y = points[25]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //오른쪽 팔꿈치 손목 연결
    start_x = points[14]
    start_y = points[15]
    end_x = points[18]
    end_y = points[19]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //왼쪽 팔꿈치 손목 연결
    start_x = points[16]
    start_y = points[17]
    end_x = points[20]
    end_y = points[21]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //오른쪽 골반 무릎 연결
    start_x = points[22]
    start_y = points[23]
    end_x = points[26]
    end_y = points[27]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //왼쪽 골반 무릎 연결
    start_x = points[24]
    start_y = points[25]
    end_x = points[28]
    end_y = points[29]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //오른쪽 무릎 발 연결
    start_x = points[26]
    start_y = points[27]
    end_x = points[30]
    end_y = points[31]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //왼쪽 무릎 발 연결
    start_x = points[28]
    start_y = points[29]
    end_x = points[32]
    end_y = points[33]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //어깨 좌우 연결
    start_x = points[10]
    start_y = points[11]
    end_x = points[12]
    end_y = points[13]
    draw_line(ctx, start_x, start_y, end_x, end_y)
    //골반 좌우 연결
    start_x = points[22]
    start_y = points[23]
    end_x = points[24]
    end_y = points[25]
    draw_line(ctx, start_x, start_y, end_x, end_y)
}

function draw_line(ctx, start_x, start_y, end_x, end_y) {
    ctx.beginPath();
    ctx.moveTo(start_x, start_y);
    ctx.lineTo(end_x, end_y);
    ctx.strokeStyle = "yellow";
    ctx.lineWidth = 2;
    ctx.stroke();
}

// keypoint 순서
// 0번 == 코
// 1번 == 오른쪽 눈
// 2번 == 왼쪽 눈
// 3번 == 오른쪽 귀
// 4번 == 왼쪽 귀
// 5번 == 오른쪽 어깨
// 6번 == 왼쪽 어깨
// 7번 == 오른쪽 팔꿈치
// 8번 == 왼쪽 팔꿈치
// 9번 == 오른쪽 손목
// 10번 == 왼쪽 손목
// 11번 == 오른쪽 골반
// 12번 == 왼쪽 골반
// 13번 == 오른쪽 무릎
// 14번 == 왼쪽 무릎
// 15번 == 오른쪽 발
// 16번 == 왼쪽 발