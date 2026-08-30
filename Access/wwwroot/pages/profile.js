var canvas = document.getElementById('signature-pad');

function resizeCanvas() {

    var ratio = Math.max(window.devicePixelRatio || 1, 1);

    var winwidth = window.innerWidth;

    if (winwidth > 700) {
        canvas.width = 600;
        canvas.height = 300;
    }
    else {
        winwidth = winwidth - 100;
        canvas.width = winwidth;
        canvas.height = winwidth / 2;
    }

    canvas.getContext("2d").scale(1, 1);
}

window.onresize = resizeCanvas;
resizeCanvas();

var signaturePad = new SignaturePad(canvas, {
    backgroundColor: 'rgb(255, 255, 255)' // necessary for saving image as JPEG; can be removed is only saving as PNG or SVG
});

document.getElementById('bt_clear_sign').addEventListener('click', function () {
    signaturePad.clear();
});

document.getElementById('bt_save_sign').addEventListener('click', function () {
    if (signaturePad.isEmpty()) {
        toastr.warning('Please provide a signature first.');
        return;
    }

    $('input#hd_sign').val(signaturePad.toDataURL());
    $('#frm_sign').submit();
});

//document.getElementById('save-png').addEventListener('click', function () {
//    if (signaturePad.isEmpty()) {
//        return alert("Please provide a signature first.");
//    }

//    var data = signaturePad.toDataURL('image/png');
//    console.log(data);
//    window.open(data);
//});

//document.getElementById('save-jpeg').addEventListener('click', function () {
//    if (signaturePad.isEmpty()) {
//        return alert("Please provide a signature first.");
//    }

//    var data = signaturePad.toDataURL('image/jpeg');
//    console.log(data);
//    window.open(data);
//});

//document.getElementById('save-svg').addEventListener('click', function () {
//    if (signaturePad.isEmpty()) {
//        return alert("Please provide a signature first.");
//    }

//    var data = signaturePad.toDataURL('image/svg+xml');
//    console.log(data);
//    console.log(atob(data.split(',')[1]));
//    window.open(data);
//});

//document.getElementById('draw').addEventListener('click', function () {
//    var ctx = canvas.getContext('2d');
//    console.log(ctx.globalCompositeOperation);
//    ctx.globalCompositeOperation = 'source-over'; // default value
//});

//document.getElementById('erase').addEventListener('click', function () {
//    var ctx = canvas.getContext('2d');
//    ctx.globalCompositeOperation = 'destination-out';
//});