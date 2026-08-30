var expdate;
var emailValidated = false;
var signatureSaved = false;
var signupSignaturePad = null;

function updateContinueButton() {
    var ready = emailValidated && signatureSaved;
    $('#btn-submit-validation').prop('disabled', !ready);
    if (ready) {
        $('#btn-submit-validation').attr('title', 'Continue');
    } else if (!emailValidated && !signatureSaved) {
        $('#btn-submit-validation').attr('title', 'Validate email and upload signature before continuing');
    } else if (!emailValidated) {
        $('#btn-submit-validation').attr('title', 'Validate your email before continuing');
    } else {
        $('#btn-submit-validation').attr('title', 'Upload or draw your signature before continuing');
    }
}

function applySignature(dataUrl) {
    if (!dataUrl) {
        signatureSaved = false;
        $('input#Signature').val('');
        $('#signup_signature_preview_wrp').hide();
        updateContinueButton();
        return;
    }

    $('input#Signature').val(dataUrl);
    signatureSaved = true;
    $('#signup_signature_preview').attr('src', dataUrl);
    $('#signup_signature_preview_wrp').show();
    $('#signup_signature_status').text('Signature saved');
    toastr.success('Signature saved');
    updateContinueButton();
}

function initSignupSignaturePad() {
    var canvas = document.getElementById('signup_signature_pad');
    if (!canvas || typeof SignaturePad === 'undefined') return;

    var ratio = Math.max(window.devicePixelRatio || 1, 1);
    var width = Math.min(600, (canvas.parentElement?.clientWidth || 600));
    canvas.width = width * ratio;
    canvas.height = Math.max(160, width / 3) * ratio;
    canvas.style.width = width + 'px';
    canvas.style.height = Math.max(160, width / 3) + 'px';
    canvas.getContext('2d').scale(ratio, ratio);

    if (signupSignaturePad) {
        signupSignaturePad.clear();
    }

    signupSignaturePad = new SignaturePad(canvas, {
        backgroundColor: 'rgb(255, 255, 255)'
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

$(document).ready(function () {
    wizard.goTo(1);
    updateContinueButton();
    initSignupSignaturePad();
    refreshSignupAccountButtons();
});

$(window).on('resize', function () {
    if (document.getElementById('signup_signature_pad')) {
        initSignupSignaturePad();
    }
});

$('a[data-bs-toggle="tab"][href="#signup_tab_signature_draw"]').on('shown.bs.tab', function () {
    initSignupSignaturePad();
});

$('#signup_signature_file').on('change', function () {
    var file = this.files && this.files[0];
    if (!file) {
        applySignature('');
        return;
    }

    if (!file.type || file.type.indexOf('image/') !== 0) {
        toastr.error('Please upload an image file for your signature');
        this.value = '';
        applySignature('');
        return;
    }

    var reader = new FileReader();
    reader.onload = function (e) {
        applySignature(e.target.result);
    };
    reader.onerror = function () {
        toastr.error('Could not read signature file');
        applySignature('');
    };
    reader.readAsDataURL(file);
});

$('#bt_signup_clear_sign').on('click', function () {
    if (signupSignaturePad) signupSignaturePad.clear();
});

$('#bt_signup_save_sign').on('click', function () {
    if (!signupSignaturePad || signupSignaturePad.isEmpty()) {
        toastr.warning('Please draw your signature first');
        return;
    }
    applySignature(signupSignaturePad.toDataURL('image/png'));
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
    setTimeout(initSignupSignaturePad, 50);
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
    if (!signatureSaved || !$('input#Signature').val()) {
        toastr.error('Please upload or draw your signature before continuing');
        updateContinueButton();
        return;
    }
    wizard.goTo(5);
});

$('#bt_cancel_final').click(function (e) {
    wizard.goTo(4);
});

$('#form_final').submit(function (e) {
    if (!emailValidated) {
        e.preventDefault();
        toastr.error('Please validate your email before finishing account creation');
        wizard.goTo(4);
        return;
    }

    if (!signatureSaved || !$('input#Signature').val()) {
        e.preventDefault();
        toastr.error('Please upload or draw your signature before finishing account creation');
        wizard.goTo(4);
        return;
    }

    if ($('input#Password').val() !== $('input#RePassword').val()) {
        e.preventDefault();
        toastr.error('Your passwords do not match');
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
