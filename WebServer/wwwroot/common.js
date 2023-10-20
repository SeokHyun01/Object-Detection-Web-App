function set_img_src(id, source) {
    const view = document.getElementById(id);
    view.src = source;
}

function set_input_disabled(id, value) {
    const view = document.getElementById(id);
    view.disabled = value;
}

function set_display(id, value) {
    const view = document.getElementById(id);
    view.style.display = value;
}

function set_checkbox_value(id, value) {
    const view = document.getElementById(id);
    view.checked = value;
}
