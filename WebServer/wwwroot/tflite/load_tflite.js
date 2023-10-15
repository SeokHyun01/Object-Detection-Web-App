let busy = false;
let model = null;
let _model_name = "none"

async function inference(input, model_name) {
    if (!model || _model_name != model_name) {
        model = null;
        await initializeModel(model_name);
        _model_name = model_name;
    }

    if (busy) {
        return;
    }

    busy = true;

    const output = model.predict(input);

    busy = false;
    return output;
}


async function initializeModel(model_name) {
    // 시간 체크 
    const t0 = performance.now();
    model = await tflite.loadTFLiteModel("/models/yolov8n_" + model_name + "_224.tflite", { numThreads: navigator.hardwareConcurrency / 2 });  
    const t1 = performance.now();
    console.log("Model Load Time: " + (t1 - t0) + " milliseconds.");
}