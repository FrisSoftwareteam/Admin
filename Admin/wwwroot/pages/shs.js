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
    var id = this.getAttribute('data-id');
    var reg = this.getAttribute('data-reg');
    var accno = this.getAttribute('data-accno');
    var url = this.getAttribute('data-review-url') || ('/shareholders/holding/review/' + id);

    $('#sp_h_register').html(reg);
    $('#sp_h_accno').html(accno);
    $('#tx_h_register').val(reg);
    $('#tx_h_accno').val(accno);
    $('#tx_h_name').val('');
    $('.tx_h_id').val(id);
    $('#sp_h_lookup').html('Searching <span class="fw-bolder">' + reg + '</span> for account <span class="fw-bolder">' + accno + '</span>…');

    $.ajax({
        type: 'GET',
        url: url,
        cache: false,
        success: function (data) {
            if (!data || !data.found) {
                $('#tx_h_name').val('Not found');
                $('#sp_h_lookup').html(
                    'Account <span class="fw-bolder">' + accno + '</span> was not found in <span class="fw-bolder">' + reg + '</span>. ' +
                    'The name will not come up. Do not verify — delete it if they selected the wrong registrar.'
                );
                return;
            }

            $('#tx_h_name').val(data.accountName || '');
            var units = (data.units || 0).toLocaleString();
            if (data.nameMatches) {
                $('#sp_h_lookup').html(
                    'Found <span class="fw-bolder">' + data.accountName + '</span> in ' + reg +
                    ' with ' + units + ' units. This name matches the applicant.'
                );
            } else {
                $('#sp_h_lookup').html(
                    'Found <span class="fw-bolder">' + data.accountName + '</span> in ' + reg +
                    ' with ' + units + ' units. This name does not match the applicant. Do not verify — delete it.'
                );
            }
        },
        error: function () {
            $('#tx_h_name').val('');
            $('#sp_h_lookup').html(
                'Could not search ' + reg + ' for ' + accno + '. Confirm on estock before you continue.'
            );
        }
    });
});

$('#bt_add_accno_row').on('click', function () {
    var row = $('#accno_row_template .accno-row').first().clone();
    row.find('select').val('');
    row.find('input').val('');
    $('#accno_rows').append(row);
});

$('#accno_rows').on('click', '.bt-remove-accno-row', function () {
    if ($('#accno_rows .accno-row').length < 2)
        return;
    $(this).closest('.accno-row').remove();
});