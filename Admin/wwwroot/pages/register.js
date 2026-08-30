$('.a_create_acc').click(function () {
    $('#hd_id').val(this.getAttribute('data-id'));
    $('#tx_name').val(this.getAttribute('data-name'));
    $('#tx_email').val(this.getAttribute('data-email'));
    $('#tx_phone').val(this.getAttribute('data-phone'));
});

$('.a_view_details').click(function () {
    $('#div_name').html(this.getAttribute('data-name'));
    $('#sp_rc').html(this.getAttribute('data-rc'));
    $('#div_email').html(this.getAttribute('data-email'));
    $('#div_phone').html(this.getAttribute('data-phone'));
    $('#div_address').html(this.getAttribute('data-address'));
});