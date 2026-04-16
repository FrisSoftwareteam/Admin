var subform = $('#frm_sub');
var subbtn = $('#bt_sub');

subform.submit(function (e) {
    e.preventDefault();

    subbtn.val(subbtn.attr('data-wait'));

    $.ajax({
        type: "POST",
        url: subform.attr('action'),
        data: subform.serialize(),
        cache: false,
        success: function () {
            subbtn.val('Done!');
            subbtn.addClass('d-none');
            subform.addClass('d-none');

            $('#p_submsg').html('Thank you for subscribing to our mailing list. You will now get updated with latest news, articles, and resources.')
        },
        error: function () {
            $('#p_submsg').html('We could not complete your request to join out mailing list, please try again.')
            subbtn.val('Subscribe');
        },
        complete: function () {
            // done
        }
    });
});