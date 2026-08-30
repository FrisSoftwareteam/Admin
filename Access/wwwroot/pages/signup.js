var expdate;
var emailValidated = false;
var signupDocs = { photo: false, passport: false, signature: false };
var signupCameraStream = null;
var signupCameraDoc = null;
var signupDocFields = { photo: 'Photo', passport: 'Passport', signature: 'Signature' };
var signupDocLabels = { photo: 'Profile picture', passport: 'Passport / NIN', signature: 'Signature' };

function documentsSaved() {
    return signupDocs.photo && signupDocs.passport && signupDocs.signature
        && $('input#Photo').val() && $('input#Passport').val() && $('input#Signature').val();
}

function updateContinueButton() {
    var ready = emailValidated && documentsSaved();
    $('#btn-submit-validation').prop('disabled', !ready);
    if (ready) {
        $('#btn-submit-validation').attr('title', 'Continue');
    } else if (!emailValidated && !documentsSaved()) {
        $('#btn-submit-validation').attr('title', 'Validate email and upload all three documents before continuing');
    } else if (!emailValidated) {
        $('#btn-submit-validation').attr('title', 'Validate your email before continuing');
    } else {
        $('#btn-submit-validation').attr('title', 'Upload profile picture, passport/NIN and signature before continuing');
    }
}

function applySignupDoc(key, dataUrl, isPdf) {
    var field = signupDocFields[key];
    if (!field) return;

    if (!dataUrl) {
        signupDocs[key] = false;
        $('input#' + field).val('');
        $('#signup_file_' + key).val('');
        $('#signup_status_' + key).text('');
        $('#signup_preview_' + key).attr('src', '');
        if (key === 'passport') {
            $('#signup_preview_passport_file').hide();
        }
        $('#signup_preview_' + key + '_wrp').hide();
        updateContinueButton();
        return;
    }

    $('input#' + field).val(dataUrl);
    signupDocs[key] = true;
    $('#signup_status_' + key).text(isPdf ? 'File selected' : 'Photo captured');
    $('#signup_preview_' + key + '_wrp').show();
    if (key === 'passport') {
        if (isPdf) {
            $('#signup_preview_passport').hide();
            $('#signup_preview_passport_file').show();
        } else {
            $('#signup_preview_passport').attr('src', dataUrl).show();
            $('#signup_preview_passport_file').hide();
        }
    } else {
        $('#signup_preview_' + key).attr('src', dataUrl);
    }
    updateContinueButton();
}

function resizeImageFile(file, callback) {
    var reader = new FileReader();
    reader.onerror = function () {
        callback(null);
    };
    reader.onload = function (e) {
        var img = new Image();
        img.onerror = function () {
            callback(null);
        };
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

function handleSignupFile(key, file) {
    if (!file) {
        applySignupDoc(key, '');
        return;
    }

    var isPdf = file.type === 'application/pdf' || /\.pdf$/i.test(file.name || '');
    if (isPdf && key !== 'passport') {
        toastr.error('Please upload an image for ' + signupDocLabels[key]);
        applySignupDoc(key, '');
        return;
    }

    if (isPdf) {
        var reader = new FileReader();
        reader.onload = function (e) {
            applySignupDoc(key, e.target.result, true);
        };
        reader.onerror = function () {
            toastr.error('Could not read that file');
            applySignupDoc(key, '');
        };
        reader.readAsDataURL(file);
        return;
    }

    if (!file.type || file.type.indexOf('image/') !== 0) {
        toastr.error('Please upload an image for ' + signupDocLabels[key]);
        applySignupDoc(key, '');
        return;
    }

    resizeImageFile(file, function (dataUrl) {
        if (!dataUrl) {
            toastr.error('Could not read that file');
            applySignupDoc(key, '');
            return;
        }
        applySignupDoc(key, dataUrl, false);
    });
}

function stopSignupCamera() {
    if (signupCameraStream) {
        signupCameraStream.getTracks().forEach(function (track) {
            track.stop();
        });
        signupCameraStream = null;
    }
    var video = document.getElementById('signup_camera_video');
    if (video) video.srcObject = null;
}

function showSignupCameraModal(show) {
    var el = document.getElementById('signup_camera_modal');
    if (window.bootstrap && bootstrap.Modal) {
        var modal = bootstrap.Modal.getOrCreateInstance(el);
        if (show) modal.show();
        else modal.hide();
        return;
    }
    $(el).modal(show ? 'show' : 'hide');
}

function openSignupCamera(key) {
    signupCameraDoc = key;
    $('#signup_camera_title').text('Take photo — ' + signupDocLabels[key]);
    showSignupCameraModal(true);

    var facing = key === 'photo' ? 'user' : 'environment';
    var constraints = { video: { facingMode: facing }, audio: false };
    navigator.mediaDevices.getUserMedia(constraints).catch(function () {
        return navigator.mediaDevices.getUserMedia({ video: true, audio: false });
    }).then(function (stream) {
        signupCameraStream = stream;
        var video = document.getElementById('signup_camera_video');
        video.srcObject = stream;
    }).catch(function () {
        showSignupCameraModal(false);
        var input = document.getElementById('signup_file_' + key);
        if (input) {
            input.setAttribute('capture', key === 'photo' ? 'user' : 'environment');
            input.click();
        } else {
            toastr.error('Could not open the camera. Please choose a file instead.');
        }
    });
}

var sendEmailCode = function () {

    if ($('#bt_resend_link').html() === 'Sending...')
        return;

    // Resending a code means previous email validation no longer counts
    emailValidated = false;
    $('input[type="hidden"]#EmailConfirmed').val(false);
    $('#sp-email-confirmed').html('No');
    updateContinueButton();

    $('#bt_resend_link').html('Sending...');

    xhr = $.ajax({
        type: "POST",
        url: $('#hd_generatevalidateemailurl').val(),
        data: {
            email: $('input#Email').val(),
            name: $('input#FirstName').val(),
            phone: $('input#MobileNo').val()
        },
        cache: false,
        success: function (json) {
            expdate = json.date;
            toastr.info('Please check your mail, we just send you an email validation.', '', {
                positionClass: 'toast-center-xl',
                timeOut: 12000,
                extendedTimeOut: 4000,
                closeButton: true
            });
        },
        error: function () {
            toastr.error(`System could not generate validation requests to ${$('input#Email').val()}`);
        },
        complete: function () {
            $('#bt_resend_link').html('Resend Code');
        }
    });

}

var element = document.querySelector("#signup_stepper");
var wizard = new KTStepper(element);

function getPasswordRules(password, confirm) {
    return {
        length: password.length >= 8,
        letter: /[A-Za-z]/.test(password),
        number: /\d/.test(password),
        symbol: /[^A-Za-z0-9]/.test(password),
        match: password.length > 0 && password === confirm
    };
}

function passwordMeetsRules() {
    var password = $('input#Password').val() || '';
    var confirm = $('input#RePassword').val() || '';
    var rules = getPasswordRules(password, confirm);
    return rules.length && rules.letter && rules.number && rules.symbol && rules.match;
}

function updatePasswordLights() {
    var password = $('input#Password').val() || '';
    var confirm = $('input#RePassword').val() || '';
    var started = password.length > 0 || confirm.length > 0;
    var rules = getPasswordRules(password, confirm);

    $('.signup-rule').each(function () {
        var key = $(this).data('rule');
        $(this).removeClass('is-valid is-invalid');
        if (!started) return;
        $(this).addClass(rules[key] ? 'is-valid' : 'is-invalid');
    });

    var ready = passwordMeetsRules();
    $('#form_final .bt-submit').prop('disabled', !ready);
    if (!ready) {
        $('#form_final .bt-submit').attr('title', 'Enter a password that meets all the conditions');
    } else {
        $('#form_final .bt-submit').attr('title', 'Submit');
        $('#signup_password_error').hide().text('');
    }
}

function restoreSignupStateFromForm() {
    var confirmed = ($('input[type="hidden"]#EmailConfirmed').val() || '').toString().toLowerCase();
    emailValidated = confirmed === 'true' || confirmed === 'True';

    if ($('input#Photo').val()) applySignupDoc('photo', $('input#Photo').val(), false);
    if ($('input#Passport').val()) {
        var passport = $('input#Passport').val();
        applySignupDoc('passport', passport, passport.indexOf('application/pdf') !== -1);
    }
    if ($('input#Signature').val()) applySignupDoc('signature', $('input#Signature').val(), false);

    $('#sp-fname').html($('input[type="hidden"]#FirstName').val() || '');
    $('#sp-lname').html($('input[type="hidden"]#LastName').val() || '');
    $('#sp-email').html($('input[type="hidden"]#Email').val() || '');
    $('#sp-phone').html($('input[type="hidden"]#MobileNo').val() || '');
    $('#sp-street').html($('input[type="hidden"]#Street').val() || '');
    $('#sp-city').html($('input[type="hidden"]#City').val() || '');
    $('#sp-state').html($('input[type="hidden"]#State').val() || '');
    $('#sp-postcode').html($('input[type="hidden"]#PostCode').val() || '');
    $('#sp-country').html($('input[type="hidden"]#Country').val() || '');

    if ($('input[type="hidden"]#FirstName').val())
        $('form#form_basic input#FirstName').val($('input[type="hidden"]#FirstName').val());
    if ($('input[type="hidden"]#LastName').val())
        $('form#form_basic input#LastName').val($('input[type="hidden"]#LastName').val());
    if ($('input[type="hidden"]#Email').val())
        $('form#form_basic input#Email').val($('input[type="hidden"]#Email').val());
    if ($('input[type="hidden"]#MobileNo').val())
        $('form#form_basic input#MobileNo').val($('input[type="hidden"]#MobileNo').val());
    if ($('input[type="hidden"]#Street').val())
        $('form#form_address input#Street').val($('input[type="hidden"]#Street').val());
    if ($('input[type="hidden"]#City').val())
        $('form#form_address input#City').val($('input[type="hidden"]#City').val());
    if ($('input[type="hidden"]#State').val())
        $('form#form_address input#State').val($('input[type="hidden"]#State').val());
    if ($('input[type="hidden"]#PostCode').val())
        $('form#form_address input#PostCode').val($('input[type="hidden"]#PostCode').val());
    if ($('input[type="hidden"]#Country').val())
        $('form#form_address input#Country').val($('input[type="hidden"]#Country').val());
}

$(document).ready(function () {
    updateContinueButton();
    refreshSignupAccountButtons();
    updatePasswordLights();

    var restoreStep = parseInt($('#RegisterStep').val() || '0', 10);
    if (restoreStep === 5) {
        restoreSignupStateFromForm();
        wizard.goTo(5);
        updatePasswordLights();
    } else {
        wizard.goTo(1);
    }
});

$('input#Password, input#RePassword').on('input', function () {
    updatePasswordLights();
});

$('#signup_file_photo, #signup_file_passport, #signup_file_signature').on('change', function () {
    handleSignupFile($(this).data('doc'), this.files && this.files[0]);
});

$('.signup-take-photo').on('click', function (e) {
    e.preventDefault();
    openSignupCamera($(this).data('doc'));
});

$(document).on('click', '.signup-delete-doc', function (e) {
    e.preventDefault();
    var key = $(this).data('doc');
    applySignupDoc(key, '');
});

$('#signup_camera_capture').on('click', function () {
    var video = document.getElementById('signup_camera_video');
    var canvas = document.getElementById('signup_camera_canvas');
    if (!video || !video.videoWidth || !signupCameraDoc) {
        toastr.error('Camera is not ready yet');
        return;
    }
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    canvas.getContext('2d').drawImage(video, 0, 0);
    applySignupDoc(signupCameraDoc, canvas.toDataURL('image/jpeg', 0.72), false);
    showSignupCameraModal(false);
});

$('#signup_camera_modal').on('hidden.bs.modal', function () {
    stopSignupCamera();
});

$('#form_type').submit(function (e) {
    e.preventDefault();

    var type = $("input[type='radio'][name='Type']:checked").val();
    $('input[type="hidden"]#Type').val(type);

        if (type === 'Shareholder') {
        $('#dv_clearing').show();
    }

    if (type === 'StockBroker') {
        $('#dv_clearing').hide();
        $('.signup-chn, .signup-accno, .signup-register').removeAttr('required');
    }

    wizard.goTo(2);
});

$('#bt_cancel_basic').click(function (e) {
    wizard.goTo(1);
});

function collectSignupAccounts() {
    var rows = [];
    $('#signup_accounts .signup-account-row').each(function () {
        rows.push({
            registerId: ($(this).find('.signup-register').val() || '').trim(),
            clearingNo: ($(this).find('.signup-chn').val() || '').trim(),
            accountNo: ($(this).find('.signup-accno').val() || '').trim()
        });
    });
    return rows;
}

function validateSignupAccounts() {
    if ($('input[type="hidden"]#Type').val() !== 'Shareholder')
        return true;

    var rows = collectSignupAccounts();
    if (!rows.some(function (row) { return row.clearingNo || row.accountNo; })) {
        toastr.error('Please enter a Clearing House Number or a Share Account Number before continuing');
        return false;
    }

    for (var i = 0; i < rows.length; i++) {
        if (rows[i].accountNo && !rows[i].registerId) {
            toastr.error('Please select a register for each Share Account Number');
            return false;
        }
    }

    return true;
}

function syncSignupAccountsToFinal() {
    var rows = collectSignupAccounts();
    var firstChn = '';
    var summary = [];

    rows.forEach(function (row) {
        if (!firstChn && row.clearingNo)
            firstChn = row.clearingNo;
        var parts = [];
        var registerText = '';
        $('#signup_accounts .signup-register').first();
        if (row.clearingNo) parts.push(row.clearingNo);
        if (row.accountNo) parts.push('Acc ' + row.accountNo);
        if (parts.length)
            summary.push(parts.join(' / '));
    });

    $('input[type="hidden"]#ClearingNo').val(firstChn);
    $('#sp-clearing').html(summary.length ? ' — ' + summary.join(', ') : '');

    var wrap = $('#signup_accounts_hidden');
    wrap.empty();
    rows.forEach(function (row, i) {
        $('<input>', { type: 'hidden', name: 'Accounts[' + i + '].RegisterId', value: row.registerId }).appendTo(wrap);
        $('<input>', { type: 'hidden', name: 'Accounts[' + i + '].ClearingNo', value: row.clearingNo }).appendTo(wrap);
        $('<input>', { type: 'hidden', name: 'Accounts[' + i + '].AccountNo', value: row.accountNo }).appendTo(wrap);
    });
}

function refreshSignupAccountButtons() {
    var rows = $('#signup_accounts .signup-account-row');
    rows.find('.signup-remove-account').toggle(rows.length > 1);
}

$('#bt_add_signup_account').on('click', function () {
    var first = $('#signup_accounts .signup-account-row').first();
    if (!first.length) return;
    var clone = first.clone();
    clone.find('.signup-register').val('');
    clone.find('.signup-chn, .signup-accno').val('');
    $('#signup_accounts').append(clone);
    refreshSignupAccountButtons();
});

$(document).on('click', '.signup-remove-account', function () {
    if ($('#signup_accounts .signup-account-row').length <= 1)
        return;
    $(this).closest('.signup-account-row').remove();
    refreshSignupAccountButtons();
});

$('#form_basic').submit(function (e) {
    e.preventDefault();

    if (!validateSignupAccounts())
        return;

    var button = document.querySelector("#bt_submit_basic");
    button.setAttribute("data-kt-indicator", "on");

    $('#sp-fname').html($('input#FirstName').val());
    $('#sp-lname').html($('input#LastName').val());
    $('#sp-email').html($('input#Email').val());
    $('#sp-phone').html($('input#MobileNo').val());
    $('#sp-home').html($('input#HomePhone').val());

    $('input[type="hidden"]#FirstName').val($('input#FirstName').val());
    $('input[type="hidden"]#LastName').val($('input#LastName').val());
    $('input[type="hidden"]#Email').val($('input#Email').val());
    $('input[type="hidden"]#MobileNo').val($('input#MobileNo').val());
    $('input[type="hidden"]#HomePhone').val($('input#HomePhone').val());
    syncSignupAccountsToFinal();

    $('input#Username').val($('input#Email').val());

    xhr = $.ajax({
        type: "POST",
        url: $('#hd_checkemailurl').val(),
        data: {
            email: $('input#Email').val()
        },
        cache: false,
        success: function (json) {
            if (json.ok) {
                sendEmailCode();
                wizard.goTo(3);
            } else {
                toastr.error('Email address already exists, please choose a different email address');
            }
        },
        error: function () {
            toastr.error('Could not check email availability, please try again');
        },
        complete: function () {
            button.removeAttribute("data-kt-indicator");
        }
    });

});

$('#bt_resend_link').click(function () {
    sendEmailCode();
});

$('#bt_cancel_address').click(function (e) {
    wizard.goTo(2);
});

$('#form_address').submit(function (e) {
    e.preventDefault();

    $('#sp-street').html($('input#Street').val());
    $('#sp-city').html($('input#City').val());
    $('#sp-state').html($('input#State').val());
    $('#sp-postcode').html($('input#PostCode').val());
    $('#sp-country').html($('input#Country').val());

    $('input[type="hidden"]#Street').val($('input#Street').val());
    $('input[type="hidden"]#City').val($('input#City').val());
    $('input[type="hidden"]#State').val($('input#State').val());
    $('input[type="hidden"]#PostCode').val($('input#PostCode').val());
    $('input[type="hidden"]#Country').val($('input#Country').val());

    wizard.goTo(4);
});

$('#bt_validate_email').click(function () {
    var code = $('#tx_email_code').val();

    if (!code) {
        toastr.error('Please enter email validation code');
        return;
    }

    var wrp = $('#tx_email_code_wrp');

    wrp.addClass('spinner spinner-sm spinner-success spinner-right');

    xhr = $.ajax({
        type: "POST",
        url: $('#hd_validateemailurl').val(),
        data: {
            code: code,
            email: $('input#Email').val(),
            date: expdate
        },
        cache: false,
        success: function (json) {
            if (json.valid) {
                emailValidated = true;
                toastr.info('Email validation was successful');
                $('input[type="hidden"]#EmailConfirmed').val(true);
                $('#sp-email-confirmed').html('Yes');
                updateContinueButton();
            } else {
                emailValidated = false;
                updateContinueButton();
                toastr.error('Could not validate email code, please obtain a new code');
            }
        },
        error: function () {
            emailValidated = false;
            updateContinueButton();
            toastr.error('System could not validate the email code, please try again');
        },
        complete: function () {
            wrp.removeClass('spinner spinner-sm spinner-success spinner-right');
        }
    });
});

$('#bt_validate_phone').click(function () {
    //$('#sp-phone-confirmed').html('Yes');
});

$('#bt_cancel_validate').click(function (e) {
    wizard.goTo(3);
});

$('#form_validate').submit(function (e) {
    e.preventDefault();
    if (!emailValidated) {
        toastr.error('Please validate your email before continuing');
        updateContinueButton();
        return;
    }
    if (!documentsSaved()) {
        toastr.error('Please upload your profile picture, passport/NIN and signature before continuing');
        updateContinueButton();
        return;
    }
    wizard.goTo(5);
});

$('#bt_cancel_final').click(function (e) {
    wizard.goTo(4);
});

$('#form_final').submit(function (e) {
    $('#RegisterStep').val('5');

    if (!emailValidated) {
        e.preventDefault();
        toastr.error('Please validate your email before finishing account creation');
        wizard.goTo(4);
        return;
    }

    if (!documentsSaved()) {
        e.preventDefault();
        toastr.error('Please upload your profile picture, passport/NIN and signature before finishing account creation');
        wizard.goTo(4);
        return;
    }

    if (!passwordMeetsRules()) {
        e.preventDefault();
        updatePasswordLights();
        var message = 'Please enter a password that meets all the conditions';
        if (($('input#Password').val() || '') !== ($('input#RePassword').val() || ''))
            message = 'Your passwords do not match';
        $('#signup_password_error').text(message).show();
        toastr.error(message);
        var blocked = document.querySelector('#form_final .bt-submit');
        if (blocked) blocked.removeAttribute('data-kt-indicator');
        wizard.goTo(5);
        return;
    }

    if (!validateSignupAccounts()) {
        e.preventDefault();
        wizard.goTo(2);
        return;
    }
    syncSignupAccountsToFinal();

    var button = document.querySelector(".bt-submit");
    button.setAttribute("data-kt-indicator", "on");
});
