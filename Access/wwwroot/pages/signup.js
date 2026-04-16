var expdate;

var sendEmailCode = function () {

    if ($('#bt_resend_link').html() === 'Sending...')
        return;

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
            toastr.info('Please check your mail, we just send you an email validation.');
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
});


$('#form_type').submit(function (e) {
    e.preventDefault();

    var type = $("input[type='radio'][name='Type']:checked").val();
    $('input[type="hidden"]#Type').val(type);

    if (type === 'Shareholder') {
        $('#dv_clearing').show();
        $('input#ClearingNo').attr('required', 'required');
    }

    if (type === 'StockBroker') {
        $('#dv_clearing').hide();
        $('input#ClearingNo').removeAttr('required');
    }

    wizard.goTo(2);
});

$('#bt_cancel_basic').click(function (e) {
    wizard.goTo(1);
});

$('#form_basic').submit(function (e) {
    e.preventDefault();

    var button = document.querySelector("#bt_submit_basic");
    button.setAttribute("data-kt-indicator", "on");

    $('#sp-fname').html($('input#FirstName').val());
    $('#sp-lname').html($('input#LastName').val());
    $('#sp-email').html($('input#Email').val());
    $('#sp-phone').html($('input#MobileNo').val());
    $('#sp-home').html($('input#HomePhone').val());
    $('#sp-clearing').html($('input#ClearingNo').val());

    $('input[type="hidden"]#FirstName').val($('input#FirstName').val());
    $('input[type="hidden"]#LastName').val($('input#LastName').val());
    $('input[type="hidden"]#Email').val($('input#Email').val());
    $('input[type="hidden"]#MobileNo').val($('input#MobileNo').val());
    $('input[type="hidden"]#HomePhone').val($('input#HomePhone').val());
    $('input[type="hidden"]#ClearingNo').val($('input#ClearingNo').val());

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

    if (!code && code === '') {
        toastr.error('Please enter email validation code');
        return;
    }

    var wrp = $('#tx_email_code_wrp');
    var btn = $('#btn-submit-validation');

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
                btn.removeAttr('disabled');
                toastr.info('Email validation was successful');
                $('input[type="hidden"]#EmailConfirmed').val(true);
                $('#sp-email-confirmed').html('Yes');

                $('#form_validate').submit();
            } else {
                toastr.error('Could not validate email code, please obtain a new code');
            }
        },
        error: function () {
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
    wizard.goTo(5);
});

$('#bt_cancel_final').click(function (e) {
    wizard.goTo(4);
});

$('#form_final').submit(function (e) {
    if ($('input#Password').val() !== $('input#RePassword').val()) {
        e.preventDefault();
        toastr.error('Your passwords do not match');
        return;
    }

    var button = document.querySelector(".bt-submit");
    button.setAttribute("data-kt-indicator", "on");
});