(function () {
    var el = document.getElementById('diag_docs_reminder');
    if (!el) return;

    var INTERVAL_MS = 7 * 60 * 1000;
    var labels = { photo: 'Profile picture', passport: 'Passport / NIN', signature: 'Signature' };
    var pending = { photo: '', passport: '', signature: '' };
    var missing = {
        photo: el.getAttribute('data-missing-photo') === 'true',
        passport: el.getAttribute('data-missing-passport') === 'true',
        signature: el.getAttribute('data-missing-signature') === 'true'
    };
    var uploadUrl = el.getAttribute('data-upload-url');
    var cameraStream = null;
    var cameraDoc = null;
    var allowClose = false;

    function stillMissing() {
        return missing.photo || missing.passport || missing.signature;
    }

    function getModal() {
        if (!window.bootstrap || !bootstrap.Modal) return null;
        return bootstrap.Modal.getOrCreateInstance(el, { backdrop: 'static', keyboard: false });
    }

    function showReminder() {
        if (!stillMissing()) return;
        var modal = getModal();
        if (modal) modal.show();
    }

    function applyDoc(key, dataUrl, isPdf) {
        if (!dataUrl) {
            pending[key] = '';
            $('#docs_file_' + key).val('');
            $('#docs_file_status_' + key).text('');
            $('#docs_preview_' + key).attr('src', '');
            if (key === 'passport') $('#docs_preview_passport_file').hide();
            $('#docs_preview_' + key + '_wrp').hide();
            return;
        }

        pending[key] = dataUrl;
        $('#docs_file_status_' + key).text(isPdf ? 'File selected' : 'Photo captured');
        $('#docs_preview_' + key + '_wrp').show();
        if (key === 'passport') {
            if (isPdf) {
                $('#docs_preview_passport').hide();
                $('#docs_preview_passport_file').show();
            } else {
                $('#docs_preview_passport').attr('src', dataUrl).show();
                $('#docs_preview_passport_file').hide();
            }
        } else {
            $('#docs_preview_' + key).attr('src', dataUrl);
        }
    }

    function resizeImageFile(file, callback) {
        var reader = new FileReader();
        reader.onerror = function () { callback(null); };
        reader.onload = function (e) {
            var img = new Image();
            img.onerror = function () { callback(null); };
            img.onload = function () {
                var maxW = 900;
                var scale = Math.min(1, maxW / img.width);
                var canvas = document.createElement('canvas');
                canvas.width = Math.max(1, Math.round(img.width * scale));
                canvas.height = Math.max(1, Math.round(img.height * scale));
                canvas.getContext('2d').drawImage(img, 0, 0, canvas.width, canvas.height);
                callback(canvas.toDataURL('image/jpeg', 0.72));
            };
            img.src = e.target.result;
        };
        reader.readAsDataURL(file);
    }

    function handleFile(key, file) {
        if (!file) {
            applyDoc(key, '');
            return;
        }

        var isPdf = file.type === 'application/pdf' || /\.pdf$/i.test(file.name || '');
        if (isPdf && key !== 'passport') {
            toastr.error('Please upload an image for ' + labels[key]);
            applyDoc(key, '');
            return;
        }

        if (isPdf) {
            var reader = new FileReader();
            reader.onload = function (e) { applyDoc(key, e.target.result, true); };
            reader.onerror = function () {
                toastr.error('Could not read that file');
                applyDoc(key, '');
            };
            reader.readAsDataURL(file);
            return;
        }

        if (!file.type || file.type.indexOf('image/') !== 0) {
            toastr.error('Please upload an image for ' + labels[key]);
            applyDoc(key, '');
            return;
        }

        resizeImageFile(file, function (dataUrl) {
            if (!dataUrl) {
                toastr.error('Could not read that file');
                applyDoc(key, '');
                return;
            }
            applyDoc(key, dataUrl, false);
        });
    }

    function stopCamera() {
        if (cameraStream) {
            cameraStream.getTracks().forEach(function (track) { track.stop(); });
            cameraStream = null;
        }
        var video = document.getElementById('docs_camera_video');
        if (video) video.srcObject = null;
        $('#docs_camera_panel').hide();
        $('#docs_upload_cards').show();
        cameraDoc = null;
    }

    function openCamera(key) {
        cameraDoc = key;
        $('#docs_camera_title').text('Take photo — ' + labels[key]);
        $('#docs_upload_cards').hide();
        $('#docs_camera_panel').show();

        var facing = key === 'photo' ? 'user' : 'environment';
        navigator.mediaDevices.getUserMedia({ video: { facingMode: facing }, audio: false }).catch(function () {
            return navigator.mediaDevices.getUserMedia({ video: true, audio: false });
        }).then(function (stream) {
            cameraStream = stream;
            var video = document.getElementById('docs_camera_video');
            video.srcObject = stream;
        }).catch(function () {
            stopCamera();
            var input = document.getElementById('docs_file_' + key);
            if (input && key !== 'photo') {
                input.setAttribute('capture', 'environment');
                input.click();
            } else if (input && key === 'photo') {
                input.setAttribute('capture', 'user');
                input.click();
            } else {
                toastr.error('Could not open the camera. Please allow camera access and try again.');
            }
        });
    }

    function markProvided(key) {
        missing[key] = false;
        pending[key] = '';
        $('#docs_card_' + key).hide();
        $('#docs_status_' + key).text('Provided').removeClass('text-danger').addClass('text-success');
    }

    $('#docs_file_photo, #docs_file_passport, #docs_file_signature').on('change', function () {
        handleFile($(this).data('doc'), this.files && this.files[0]);
    });

    $('.docs-take-photo').on('click', function (e) {
        e.preventDefault();
        openCamera($(this).data('doc'));
    });

    $(document).on('click', '.docs-delete-doc', function (e) {
        e.preventDefault();
        applyDoc($(this).data('doc'), '');
    });

    $('#docs_camera_cancel').on('click', function () {
        stopCamera();
    });

    $('#docs_camera_capture').on('click', function () {
        var video = document.getElementById('docs_camera_video');
        var canvas = document.getElementById('docs_camera_canvas');
        if (!video || !video.videoWidth || !cameraDoc) {
            toastr.error('Camera is not ready yet');
            return;
        }
        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;
        canvas.getContext('2d').drawImage(video, 0, 0);
        applyDoc(cameraDoc, canvas.toDataURL('image/jpeg', 0.72), false);
        stopCamera();
    });

    $('#diag_docs_reminder').on('hide.bs.modal', function (e) {
        if (stillMissing() && !allowClose) {
            e.preventDefault();
            return;
        }
        stopCamera();
    });

    $('#docs_save').on('click', function () {
        if (!pending.photo && !pending.passport && !pending.signature) {
            toastr.error('Please choose a file or take a photo for the missing documents');
            return;
        }

        var button = this;
        button.setAttribute('data-kt-indicator', 'on');
        button.disabled = true;

        $.ajax({
            type: 'POST',
            url: uploadUrl,
            contentType: 'application/json',
            data: JSON.stringify({
                photo: pending.photo || null,
                passport: pending.passport || null,
                signature: pending.signature || null
            }),
            success: function (json) {
                if (!json || !json.ok) {
                    toastr.error((json && json.error) || 'Could not save your documents');
                    return;
                }

                missing.photo = !!(json.missingPhoto);
                missing.passport = !!(json.missingPassport);
                missing.signature = !!(json.missingSignature);

                if (json.missingPhoto === false) markProvided('photo');
                if (json.missingPassport === false) markProvided('passport');
                if (json.missingSignature === false) markProvided('signature');

                toastr.success('Your documents have been updated');

                if (!stillMissing()) {
                    allowClose = true;
                    var modal = getModal();
                    if (modal) modal.hide();
                }
            },
            error: function (xhr) {
                var json = xhr && xhr.responseJSON;
                toastr.error((json && json.error) || 'Could not save your documents. Please try again.');
            },
            complete: function () {
                button.removeAttribute('data-kt-indicator');
                button.disabled = false;
            }
        });
    });

    showReminder();
    setInterval(showReminder, INTERVAL_MS);
})();
