$('#chk_allow_group').change(function () {

    var name = this.getAttribute('data-name');

    $.ajax({
        type: "POST",
        url: '/shareholders/switch-group',
        data: {
            id: this.getAttribute('data-id'),
            status: this.checked
        },
        cache: false,
        success: function (json) {
            toastr.success(`Update was successful for ${name}`);
        },
        error: function () {
            toastr.error('could not update status');
        }
    });

});

$('.bt_h_review').on('click', function () {
    $('#sp_h_register').html(this.getAttribute('data-reg'));
    $('#sp_h_accno').html(this.getAttribute('data-accno'));

    $('#tx_h_register').val(this.getAttribute('data-reg'));
    $('#tx_h_accno').val(this.getAttribute('data-accno'));
    $('.tx_h_id').val(this.getAttribute('data-id'));
});