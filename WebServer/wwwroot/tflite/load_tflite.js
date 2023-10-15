let busy = false;
let model = null;
let _model_name = "none"
let isLoading = false;

async function inference(input, model_name) {

    if (busy || isLoading) {
        return;
    }

    if (!model || _model_name != model_name) {
        model = null;
        await initializeModel(model_name);
        _model_name = model_name;
    }

    busy = true;

    const output = model.predict(input);

    busy = false;
    return output;
}


async function initializeModel(model_name) {
    isLoading = true;
    model = await tflite.loadTFLiteModel("/models/yolov8n_" + model_name + "_224.tflite", { numThreads: navigator.hardwareConcurrency / 2 });  
    isLoading = false;
}