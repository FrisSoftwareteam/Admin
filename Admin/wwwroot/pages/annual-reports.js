$('.a_add').on('click', function () {
    $('#fl_file').attr('required', 'required');

    $('#hd_id').val('');
    $('#hd_file').val('');
    $('#tx_desc').val('');
});

$('.a_edit').on('click', function () {
    $('#fl_file').removeAttr('required');

    let id = this.getAttribute('data-id');
    let code = this.getAttribute('data-file');
    let desc = this.getAttribute('data-desc');

    $('#hd_id').val(id);
    $('#hd_file').val(code);
    $('#tx_desc').val(desc);
});