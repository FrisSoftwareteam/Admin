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

(function () {
    var rotation = 0;
    var preview = document.getElementById('img_doc_preview');
    var modalEl = document.getElementById('diag_doc_preview');
    if (!preview || !modalEl) return;

    function applyRotation() {
        preview.style.transform = 'rotate(' + rotation + 'deg)';
    }

    $('.js-doc-zoom').on('click', function () {
        rotation = 0;
        applyRotation();
        preview.src = this.getAttribute('src');
        var title = this.getAttribute('alt') || 'Document';
        $('#diag_doc_preview_title').text(title);
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    });

    $('#bt_doc_rotate_left').on('click', function () {
        rotation = (rotation + 270) % 360;
        applyRotation();
    });

    $('#bt_doc_rotate_right').on('click', function () {
        rotation = (rotation + 90) % 360;
        applyRotation();
    });
})();

$('.bt_h_review').on('click', function () {
    $('#sp_h_register').html(this.getAttribute('data-reg'));
    $('#sp_h_accno').html(this.getAttribute('data-accno'));

    $('#tx_h_register').val(this.getAttribute('data-reg'));
    $('#tx_h_accno').val(this.getAttribute('data-accno'));
    $('.tx_h_id').val(this.getAttribute('data-id'));
});