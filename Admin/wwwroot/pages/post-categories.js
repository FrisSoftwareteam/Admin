$('.a_add').on('click', function () {
    $('#hd_id').val('');
    $('#hd_code').val('');
    $('#tx_desc').val('');
});

$('.a_edit').on('click', function () {
    let id = this.getAttribute('data-id');
    let code = this.getAttribute('data-code');
    let desc = this.getAttribute('data-desc');

    $('#hd_id').val(id);
    $('#hd_code').val(code);
    $('#tx_desc').val(desc);
});